#if UNITY_EDITOR
using FixedMathSharp;
using GridForge.Grids;
using GridForge.Unity;
using UnityEngine;

namespace GridForge.Utility
{
    /// <summary>
    /// Defines types of voxels that can be visualized in the debugger.
    /// </summary>
    public enum VoxelFilterType
    {
        All,
        Empty,
        Occupied,
        Blocked
    }

    /// <summary>
    /// Unity tool for visualizing grids and their voxels in the Scene View.
    /// This debugger highlights grid voxels, occupied spaces, and obstacles.
    /// </summary>
    [ExecuteAlways]
    public class GridDebugger : MonoBehaviour
    {
        #region Inspector Fields

        [Tooltip("Enable to visualize grid voxels.")]
        [SerializeField] private bool _showGrid;

        [Tooltip("Filter voxels based on their occupancy state.")]
        [SerializeField] private VoxelFilterType _voxelFilter = VoxelFilterType.All;

        [Tooltip("Enable to click on voxels to inspect them in Play Mode.")]
        [SerializeField] private bool _enableVoxelSelection;

        [Tooltip("Color for the highlighted voxel.")]
        [SerializeField] private Color _highlightColor = Color.green;

        [Tooltip("The world position of the voxel to highlight.")]
        [SerializeField] private Vector3 _highlightedVoxelPosition;

        [Tooltip("Grid index to debug.")]
        [SerializeField] private ushort _gridIndex;

        [SerializeField] private GridWorldComponent _gridWorldComponent;

        public ushort GridIndex
        {
            get => _gridIndex;
            set => _gridIndex = value;
        }

        public bool EnableVoxelSelection => _enableVoxelSelection;

        public Voxel SelectedVoxel { get; private set; }

        public GridWorldComponent GridWorldComponent => ResolveGridWorldComponent();

        public GridWorld World => ResolveGridWorldComponent()?.World;

        private VoxelGrid _targetGrid;
        private int _warnedMissingGridIndex = -1;
        private bool _missingWorldWarningLogged;
        private Vector3 Scale => Vector3.one * (float)(World?.VoxelSize ?? Fixed64.One);

        #endregion

        #region Unity Lifecycle

        public void Update()
        {
            if (_enableVoxelSelection && Application.isPlaying)
            {
                HandleVoxelSelection();
            }
        }

        public void OnDrawGizmos()
        {
            if (!_showGrid || !Application.isPlaying)
            {
                return;
            }

            if (!TryResolveGrid(out _targetGrid))
            {
                return;
            }

            DrawGrid();
        }

        #endregion

        #region Voxel Selection Logic

        private void HandleVoxelSelection()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            GridWorld world = World;
            if (world == null || !world.IsActive || Camera.main == null)
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector3d hitPos = new Vector3d(hit.point.x, hit.point.y, hit.point.z);
                if (world.TryGetGridAndVoxel(hitPos, out _, out Voxel voxel))
                {
                    _highlightedVoxelPosition = voxel.WorldPosition.ToVector3();

                    SelectedVoxel = voxel;
                    Debug.Log($"Voxel Selected: {voxel}");
                }
            }
        }

        private bool TryResolveGrid(out VoxelGrid targetGrid)
        {
            targetGrid = null;

            GridWorld world = World;
            if (world == null || !world.IsActive)
            {
                WarnMissingWorld();
                return false;
            }

            _missingWorldWarningLogged = false;

            if (!world.TryGetGrid(_gridIndex, out targetGrid))
            {
                if (_warnedMissingGridIndex != _gridIndex)
                {
                    Debug.LogWarning($"Grid index {_gridIndex} is not available in the active {nameof(GridWorld)}.", this);
                    _warnedMissingGridIndex = _gridIndex;
                }

                return false;
            }

            _warnedMissingGridIndex = -1;
            return true;
        }

        private GridWorldComponent ResolveGridWorldComponent()
        {
            _gridWorldComponent = GridWorldComponentUtility.Resolve(this, _gridWorldComponent);
            return _gridWorldComponent;
        }

        private void WarnMissingWorld()
        {
            if (_missingWorldWarningLogged)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(GridDebugger)} on {name} could not resolve an active {nameof(GridWorldComponent)}. " +
                $"Assign one explicitly when working with multiple worlds.",
                this);
            _missingWorldWarningLogged = true;
        }

        #endregion

        #region Grid Visualization

        private void DrawGrid()
        {
            if (_targetGrid == null)
            {
                return;
            }

            int width = _targetGrid.Width;
            int height = _targetGrid.Height;
            int length = _targetGrid.Length;

            Gizmos.color = Color.magenta;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < length; z++)
                    {
                        if (!_targetGrid.TryGetVoxel(x, y, z, out Voxel voxel) || !ShouldRenderVoxel(voxel))
                        {
                            continue;
                        }

                        DrawVoxelGizmo(voxel);
                    }
                }
            }

            if (_enableVoxelSelection)
            {
                Gizmos.color = _highlightColor;
                Gizmos.DrawCube(_highlightedVoxelPosition, Scale);
            }
        }

        private bool ShouldRenderVoxel(Voxel voxel)
        {
            return _voxelFilter switch
            {
                VoxelFilterType.All => true,
                VoxelFilterType.Empty => !voxel.IsOccupied && !voxel.IsBlocked,
                VoxelFilterType.Occupied => voxel.IsOccupied,
                VoxelFilterType.Blocked => voxel.IsBlocked,
                _ => true
            };
        }

        private void DrawVoxelGizmo(Voxel voxel)
        {
            Vector3 voxelPos = voxel.WorldPosition.ToVector3();
            Color renderColor = voxel.IsBlocked ? Color.red : voxel.IsOccupied ? Color.yellow : Color.magenta;
            Gizmos.color = renderColor;
            Gizmos.DrawCube(voxelPos, Scale);
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(voxelPos, Scale * 1.02f);
            Gizmos.color = renderColor;
        }

        #endregion
    }
}
#endif
