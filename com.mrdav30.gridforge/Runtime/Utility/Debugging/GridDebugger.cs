#if UNITY_EDITOR
using FixedMathSharp;
using GridForge.Diagnostics;
using GridForge.Grids;
using GridForge.Grids.Storage;
using GridForge.Grids.Topology;
using GridForge.Unity;
using SwiftCollections;
using UnityEngine;

namespace GridForge.Utility
{
    /// <summary>
    /// Unity Scene View debugger backed by GridForge diagnostic descriptors.
    /// </summary>
    [ExecuteAlways]
    public class GridDebugger : MonoBehaviour
    {
        #region Inspector Fields

        [SerializeField] private GridWorldComponent _gridWorldComponent;

        [Tooltip("Enable to visualize diagnostic grid cells.")]
        [SerializeField] private bool _showGrid;

        [SerializeField] private bool _debugAllGrids = true;

        [Tooltip("Grid index to debug when all grids are disabled.")]
        [SerializeField] private ushort _gridIndex;

        [SerializeField] private bool _filterTopologyKind;
        [SerializeField] private GridTopologyKind _topologyKind = GridTopologyKind.RectangularPrism;

        [SerializeField] private bool _filterStorageKind;
        [SerializeField] private GridStorageKind _storageKind = GridStorageKind.Dense;

        [SerializeField] private GridDiagnosticAddressMode _addressMode = GridDiagnosticAddressMode.PhysicalOnly;
        [SerializeField] private GridDiagnosticCellState _requiredStates = GridDiagnosticCellState.None;
        [SerializeField] private GridDiagnosticCellState _excludedStates = GridDiagnosticCellState.None;

        [SerializeField] private bool _limitQueryBounds;
        [SerializeField] private Vector3 _queryBoundsMin;
        [SerializeField] private Vector3 _queryBoundsMax = Vector3.one;
        [SerializeField] private int _maxCells = GridDiagnosticQuery.DefaultMaxCells;
        [SerializeField] private bool _allowFullSparseAddressScan;

        [Tooltip("Enable to click on physical cells to inspect them in Play Mode.")]
        [SerializeField] private bool _enableVoxelSelection;

        [Tooltip("Color for the highlighted voxel.")]
        [SerializeField] private Color _highlightColor = Color.green;

        [Tooltip("The world position of the voxel to highlight.")]
        [SerializeField] private Vector3 _highlightedVoxelPosition;

        [SerializeField] private Color _emptyCellColor = new(0.78f, 0.16f, 1f, 0.75f);
        [SerializeField] private Color _occupiedCellColor = new(1f, 0.86f, 0.18f, 0.85f);
        [SerializeField] private Color _blockedCellColor = new(1f, 0.18f, 0.14f, 0.9f);
        [SerializeField] private Color _boundaryCellColor = new(0.25f, 0.65f, 1f, 0.85f);
        [SerializeField] private Color _partitionedCellColor = new(0.28f, 1f, 0.48f, 0.85f);
        [SerializeField] private Color _missingSparseAddressColor = new(1f, 0.56f, 0.1f, 0.42f);

        #endregion

        private readonly GridDiagnosticScratch _scratch = new();
        private readonly SwiftList<GridDiagnosticChange> _dirtyChanges = new();
        private GridDiagnosticSession _diagnosticSession;
        private GridWorld _sessionWorld;
        private bool _missingWorldWarningLogged;

        public ushort GridIndex
        {
            get => _gridIndex;
            set => _gridIndex = value;
        }

        public bool DebugAllGrids
        {
            get => _debugAllGrids;
            set => _debugAllGrids = value;
        }

        public bool EnableVoxelSelection => _enableVoxelSelection;

        public Voxel SelectedVoxel { get; private set; }

        public GridDiagnosticQueryStatus LastQueryStatus { get; private set; } =
            GridDiagnosticQueryStatus.InactiveWorld;

        public int LastQueryCellCount { get; private set; }

        public int LastQuerySkippedCellCount { get; private set; }

        public int LastVisitedCellCount { get; private set; }

        public int LastDirtyChangeCount { get; private set; }

        public GridWorldComponent GridWorldComponent => ResolveGridWorldComponent();

        public GridWorld World
        {
            get
            {
                GridWorldComponent worldComp = ResolveGridWorldComponent();
                return worldComp != null ? worldComp.World : null;
            }
        }

        #region Unity Lifecycle

        public void Update()
        {
            if (_enableVoxelSelection && Application.isPlaying)
                HandleVoxelSelection();
        }

        public void OnDrawGizmos()
        {
            if (!_showGrid || !Application.isPlaying)
                return;

            GridWorld world = World;
            if (world == null || !world.IsActive)
            {
                ResetQueryStatus(GridDiagnosticQueryStatus.InactiveWorld);
                WarnMissingWorld();
                return;
            }

            _missingWorldWarningLogged = false;
            EnsureDiagnosticSession(world);
            DrainDirtyChanges();

            GridDiagnosticQuery query = BuildDiagnosticQuery();
            GridDiagnosticUnityVisitor visitor = new(
                drawGizmos: true,
                _emptyCellColor,
                _occupiedCellColor,
                _blockedCellColor,
                _boundaryCellColor,
                _partitionedCellColor,
                _missingSparseAddressColor);

            GridDiagnosticQueryResult result = GridDiagnostics.VisitCells(
                world,
                in query,
                ref visitor,
                _scratch);

            LastQueryStatus = result.Status;
            LastQueryCellCount = result.CellCount;
            LastQuerySkippedCellCount = result.SkippedCellCount;
            LastVisitedCellCount = visitor.VisitedCellCount;

            if (_enableVoxelSelection && SelectedVoxel != null)
            {
                Gizmos.color = _highlightColor;
                Gizmos.DrawWireSphere(_highlightedVoxelPosition, 0.2f);
            }
        }

        private void OnDisable()
        {
            DisposeDiagnosticSession();
        }

        private void OnDestroy()
        {
            DisposeDiagnosticSession();
        }

        private void OnValidate()
        {
            if (_maxCells < 1)
                _maxCells = GridDiagnosticQuery.DefaultMaxCells;
        }

        #endregion

        #region Query

        public GridDiagnosticQuery BuildDiagnosticQuery()
        {
            ushort? gridIndex = _debugAllGrids ? null : _gridIndex;
            GridTopologyKind? topologyKind = _filterTopologyKind ? _topologyKind : null;
            GridStorageKind? storageKind = _filterStorageKind ? _storageKind : null;
            Vector3d? boundsMin = _limitQueryBounds ? _queryBoundsMin.ToVector3d() : null;
            Vector3d? boundsMax = _limitQueryBounds ? _queryBoundsMax.ToVector3d() : null;

            return new GridDiagnosticQuery(
                gridIndex,
                topologyKind,
                storageKind,
                _addressMode,
                _requiredStates,
                _excludedStates,
                boundsMin,
                boundsMax,
                _maxCells,
                _allowFullSparseAddressScan);
        }

        public bool TryResolveDiagnosticPhysicalCell(in GridDiagnosticCell cell, out Voxel voxel)
        {
            voxel = null;
            GridWorld world = World;
            if (world == null || !world.IsActive)
                return false;

            if (!GridDiagnostics.TryResolvePhysicalCell(world, in cell, out _, out Voxel resolvedVoxel))
                return false;

            voxel = resolvedVoxel;
            return voxel != null;
        }

        private void ResetQueryStatus(GridDiagnosticQueryStatus status)
        {
            LastQueryStatus = status;
            LastQueryCellCount = 0;
            LastQuerySkippedCellCount = 0;
            LastVisitedCellCount = 0;
            LastDirtyChangeCount = 0;
        }

        #endregion

        #region Voxel Selection

        private void HandleVoxelSelection()
        {
            if (!Input.GetMouseButtonDown(0))
                return;

            GridWorld world = World;
            if (world == null || !world.IsActive || Camera.main == null)
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f)
                && TryResolveDiagnosticPhysicalCellAt(hit.point.ToVector3d(), out Voxel voxel))
            {
                _highlightedVoxelPosition = voxel.WorldPosition.ToVector3();
                SelectedVoxel = voxel;
                Debug.Log($"Voxel Selected: {voxel}");
            }
        }

        private bool TryResolveDiagnosticPhysicalCellAt(Vector3d worldPosition, out Voxel voxel)
        {
            voxel = null;

            GridWorld world = World;
            if (world == null || !world.IsActive)
                return false;

            GridDiagnosticQuery query = new(
                gridIndex: _debugAllGrids ? null : _gridIndex,
                topologyKind: _filterTopologyKind ? _topologyKind : null,
                storageKind: _filterStorageKind ? _storageKind : null,
                addressMode: GridDiagnosticAddressMode.PhysicalOnly,
                boundsMin: worldPosition,
                boundsMax: worldPosition,
                maxCells: 1);

            ResolvingVisitor visitor = new(world);
            GridDiagnostics.VisitCells(world, in query, ref visitor, _scratch);

            if (!visitor.HasVoxel)
                return false;

            voxel = visitor.Voxel;
            return voxel != null;
        }

        private struct ResolvingVisitor : IGridDiagnosticCellVisitor
        {
            private readonly GridWorld _world;

            public bool HasVoxel;
            public Voxel Voxel;

            public ResolvingVisitor(GridWorld world)
            {
                _world = world;
                HasVoxel = false;
                Voxel = null;
            }

            public bool Visit(in GridDiagnosticCell cell)
            {
                if (!GridDiagnostics.TryResolvePhysicalCell(_world, in cell, out _, out Voxel voxel))
                    return true;

                HasVoxel = voxel != null;
                Voxel = voxel;
                return false;
            }
        }

        #endregion

        #region Diagnostic Session

        private void EnsureDiagnosticSession(GridWorld world)
        {
            if (_diagnosticSession != null && ReferenceEquals(_sessionWorld, world))
                return;

            DisposeDiagnosticSession();
            _sessionWorld = world;
            _diagnosticSession = new GridDiagnosticSession(world);
        }

        private void DrainDirtyChanges()
        {
            if (_diagnosticSession == null)
            {
                LastDirtyChangeCount = 0;
                return;
            }

            LastDirtyChangeCount = _diagnosticSession.GetDirtyChangesInto(_dirtyChanges);
        }

        private void DisposeDiagnosticSession()
        {
            _diagnosticSession?.Dispose();
            _diagnosticSession = null;
            _sessionWorld = null;
            LastDirtyChangeCount = 0;
        }

        #endregion

        private GridWorldComponent ResolveGridWorldComponent()
        {
            _gridWorldComponent = GridWorldComponentUtility.Resolve(this, _gridWorldComponent);
            return _gridWorldComponent;
        }

        private void WarnMissingWorld()
        {
            if (_missingWorldWarningLogged)
                return;

            Debug.LogWarning(
                $"{nameof(GridDebugger)} on {name} could not resolve an active {nameof(GridWorldComponent)}. " +
                $"Assign one explicitly when working with multiple worlds.",
                this);
            _missingWorldWarningLogged = true;
        }
    }
}
#endif
