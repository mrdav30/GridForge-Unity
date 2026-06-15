//=======================================================================
// BlockerComponent.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using FixedMathSharp;
using FixedMathSharp.Bounds;
using GridForge.Grids;
using GridForge.Spatial;
using GridForge.Unity;
using UnityEngine;

namespace GridForge.Blockers
{
    /// <summary>
    /// Available blocker types for Unity selection.
    /// </summary>
    public enum BlockerType
    {
        None = 0,
        Bounds = 1
    }

    /// <summary>
    /// Sources that can define the world-space area covered by a bounds blocker.
    /// </summary>
    public enum BlockAreaSource
    {
        Manual = 0,
        Transform = 1,
        Collider = 2,
        Renderer = 3
    }

    /// <summary>
    /// Chooses whether bounds are evaluated as full 3D bounds or as XZ-plane bounds on one world Y layer.
    /// </summary>
    public enum BlockAreaMode
    {
        Bounds3D = 0,
        XzLayer = 1
    }

    /// <summary>
    /// Unity component that allows selection of a blocker type in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class BlockerComponent : MonoBehaviour
    {
        private const BlockAreaSource InvalidBlockAreaSource = (BlockAreaSource)(-1);

        [SerializeField] private BlockerType _blockerType;

        [SerializeField] private bool _isActive;

        [SerializeField] private bool _cacheCoveredVoxels;

        [SerializeField] private BlockAreaSource _blockAreaSource;

        [SerializeField] private BlockAreaMode _blockAreaMode;

        [SerializeField] private bool _includeChildrenInBlockArea;

        [SerializeField] private FixedBoundArea _manualBlockArea;

        [SerializeField] private Vector2d _manualXzBlockAreaMin;

        [SerializeField] private Vector2d _manualXzBlockAreaMax = Vector2d.One;

        [SerializeField] private Fixed64 _layerY = Fixed64.Zero;

        [SerializeField] private bool _showCoveragePreview;

        [SerializeField] private Color _coveragePreviewColor = new(1f, 0.35f, 0.1f, 0.8f);

        [SerializeField] private GridWorldComponent _gridWorldComponent;

        private IBlocker _blocker;
        private GridWorld _resolvedWorld;
        private bool _hasStarted;
        private bool _missingWorldWarningLogged;
        private BlockAreaSource _lastWarnedBlockAreaSource = InvalidBlockAreaSource;

        public bool IsSet;

        public GridWorld World
        {
            get
            {
                var worldComp = ResolveGridWorldComponent();
                return worldComp != null ? worldComp.World : null;
            }
        }

        private void Reset()
        {
            _blockAreaSource = BlockAreaSource.Transform;
        }

        public void Start()
        {
            _hasStarted = true;
            TryApplyBlockage();
        }

        public void OnEnable()
        {
            if (_hasStarted)
                TryApplyBlockage();
        }

        public void OnDisable()
        {
            _blocker?.RemoveBlockage();
            IsSet = false;
        }

#if UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            if (!_showCoveragePreview || !Application.isPlaying)
                return;

            GridWorld world = World;
            if (world == null || !world.IsActive)
                return;

            foreach (GridVoxelSet covered in GetPreviewCoverage(world))
            {
                foreach (Voxel voxel in covered.Voxels)
                {
                    var cell = GridForge.Utility.GridDiagnosticGizmoDrawer.CreatePhysicalCell(world, covered.Grid, voxel);
                    GridForge.Utility.GridDiagnosticGizmoDrawer.DrawWireCell(in cell, _coveragePreviewColor);
                }
            }
        }
#endif

        private void TryApplyBlockage()
        {
            GridWorld world = World;
            if (world == null || !world.IsActive)
            {
                WarnMissingWorld();
                return;
            }

            _missingWorldWarningLogged = false;

            if (IsSet && ReferenceEquals(_resolvedWorld, world))
                return;

            if (!TryCreateBlocker(world))
                return;

            _blocker.ApplyBlockage();
            IsSet = true;
        }

        private bool TryCreateBlocker(GridWorld world)
        {
            if (_blocker != null && ReferenceEquals(_resolvedWorld, world))
                return true;

            if (_blocker != null)
            {
                _blocker.RemoveBlockage();
                IsSet = false;
            }

            _blocker = BlockerFactory.CreateBlocker(_blockerType, this, world);
            _resolvedWorld = world;

            if (_blocker != null)
                return true;

            Debug.LogWarning($"{nameof(BlockerComponent)} on {name} has no valid blocker type selected.", this);
            return false;
        }

        private GridWorldComponent ResolveGridWorldComponent()
        {
            _gridWorldComponent = GridWorldComponentUtility.Resolve(this, _gridWorldComponent);
            return _gridWorldComponent;
        }

        public FixedBoundArea CalculateBlockArea(
            out BlockAreaSource resolvedSource,
            out string fallbackReason)
        {
            if (_blockAreaMode == BlockAreaMode.XzLayer)
                return CalculateXzBlockArea(out resolvedSource, out fallbackReason);

            return Calculate3dBlockArea(out resolvedSource, out fallbackReason);
        }

        private FixedBoundArea Calculate3dBlockArea(
            out BlockAreaSource resolvedSource,
            out string fallbackReason)
        {
            switch (_blockAreaSource)
            {
                case BlockAreaSource.Manual:
                    resolvedSource = BlockAreaSource.Manual;
                    fallbackReason = string.Empty;
                    return _manualBlockArea;

                case BlockAreaSource.Transform:
                    resolvedSource = BlockAreaSource.Transform;
                    fallbackReason = string.Empty;
                    return CreateTransformBlockArea();

                case BlockAreaSource.Collider:
                    if (TryGetColliderBounds(out Bounds colliderBounds))
                    {
                        resolvedSource = BlockAreaSource.Collider;
                        fallbackReason = string.Empty;
                        return CreateBoundsBlockArea(colliderBounds);
                    }

                    resolvedSource = BlockAreaSource.Transform;
                    fallbackReason =
                        $"could not resolve an enabled {nameof(Collider)} for {nameof(BlockAreaSource.Collider)} source.";
                    return CreateTransformBlockArea();

                case BlockAreaSource.Renderer:
                    if (TryGetRendererBounds(out Bounds rendererBounds))
                    {
                        resolvedSource = BlockAreaSource.Renderer;
                        fallbackReason = string.Empty;
                        return CreateBoundsBlockArea(rendererBounds);
                    }

                    resolvedSource = BlockAreaSource.Transform;
                    fallbackReason =
                        $"could not resolve an enabled {nameof(Renderer)} for {nameof(BlockAreaSource.Renderer)} source.";
                    return CreateTransformBlockArea();

                default:
                    resolvedSource = BlockAreaSource.Transform;
                    fallbackReason = $"has unsupported block area source {_blockAreaSource}.";
                    return CreateTransformBlockArea();
            }
        }

        private FixedBoundArea CalculateXzBlockArea(
            out BlockAreaSource resolvedSource,
            out string fallbackReason)
        {
            switch (_blockAreaSource)
            {
                case BlockAreaSource.Manual:
                    resolvedSource = BlockAreaSource.Manual;
                    fallbackReason = string.Empty;
                    return CreateXzBlockArea(_manualXzBlockAreaMin, _manualXzBlockAreaMax);

                case BlockAreaSource.Transform:
                    resolvedSource = BlockAreaSource.Transform;
                    fallbackReason = string.Empty;
                    return CreateTransformXzBlockArea();

                case BlockAreaSource.Collider:
                    if (TryGetColliderBounds(out Bounds colliderBounds))
                    {
                        resolvedSource = BlockAreaSource.Collider;
                        fallbackReason = string.Empty;
                        return CreateBoundsXzBlockArea(colliderBounds);
                    }

                    resolvedSource = BlockAreaSource.Transform;
                    fallbackReason =
                        $"could not resolve an enabled {nameof(Collider)} for {nameof(BlockAreaSource.Collider)} source.";
                    return CreateTransformXzBlockArea();

                case BlockAreaSource.Renderer:
                    if (TryGetRendererBounds(out Bounds rendererBounds))
                    {
                        resolvedSource = BlockAreaSource.Renderer;
                        fallbackReason = string.Empty;
                        return CreateBoundsXzBlockArea(rendererBounds);
                    }

                    resolvedSource = BlockAreaSource.Transform;
                    fallbackReason =
                        $"could not resolve an enabled {nameof(Renderer)} for {nameof(BlockAreaSource.Renderer)} source.";
                    return CreateTransformXzBlockArea();

                default:
                    resolvedSource = BlockAreaSource.Transform;
                    fallbackReason = $"has unsupported block area source {_blockAreaSource}.";
                    return CreateTransformXzBlockArea();
            }
        }

        private FixedBoundArea ResolveBlockArea()
        {
            FixedBoundArea blockArea = CalculateBlockArea(out _, out string fallbackReason);
            WarnBlockAreaFallback(fallbackReason);
            return blockArea;
        }

        private FixedBoundArea CreateTransformBlockArea()
        {
            Vector3 center = transform.position;
            Vector3 size = transform.lossyScale;
            size.x = Mathf.Abs(size.x);
            size.y = Mathf.Abs(size.y);
            size.z = Mathf.Abs(size.z);

            Vector3 extents = size * 0.5f;
            return CreateBlockArea(center - extents, center + extents);
        }

        private FixedBoundArea CreateTransformXzBlockArea()
        {
            Vector3 center = transform.position;
            Vector3 size = transform.lossyScale;
            size.x = Mathf.Abs(size.x);
            size.z = Mathf.Abs(size.z);

            Vector3 extents = size * 0.5f;
            return CreateXzBlockArea(
                Vector2d.FromDouble(center.x - extents.x, center.z - extents.z),
                Vector2d.FromDouble(center.x + extents.x, center.z + extents.z));
        }

        private static FixedBoundArea CreateBoundsBlockArea(Bounds bounds)
        {
            return CreateBlockArea(bounds.min, bounds.max);
        }

        private FixedBoundArea CreateBoundsXzBlockArea(Bounds bounds)
        {
            return CreateXzBlockArea(
                Vector2d.FromDouble(bounds.min.x, bounds.min.z),
                Vector2d.FromDouble(bounds.max.x, bounds.max.z));
        }

        private static FixedBoundArea CreateBlockArea(Vector3 min, Vector3 max)
        {
            return new FixedBoundArea(
                Vector3d.FromDouble(min.x, min.y, min.z),
                Vector3d.FromDouble(max.x, max.y, max.z));
        }

        private FixedBoundArea CreateXzBlockArea(Vector2d min, Vector2d max)
        {
            (Vector3d worldMin, Vector3d worldMax) = GridPlane2d.ToWorldBounds(min, max, _layerY);
            return new FixedBoundArea(worldMin, worldMax);
        }

        private bool TryGetColliderBounds(out Bounds bounds)
        {
            if (!_includeChildrenInBlockArea)
            {
                if (TryGetComponent(out Collider collider) && IsUsable(collider))
                {
                    bounds = collider.bounds;
                    return true;
                }

                bounds = default;
                return false;
            }

            return TryGetColliderBoundsFromChildren(out bounds);
        }

        private bool TryGetColliderBoundsFromChildren(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            foreach (Collider collider in GetComponentsInChildren<Collider>(false))
            {
                if (!IsUsable(collider))
                    continue;

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(collider.bounds);
            }

            return hasBounds;
        }

        private bool TryGetRendererBounds(out Bounds bounds)
        {
            if (!_includeChildrenInBlockArea)
            {
                if (TryGetComponent(out Renderer blockRenderer) && IsUsable(blockRenderer))
                {
                    bounds = blockRenderer.bounds;
                    return true;
                }

                bounds = default;
                return false;
            }

            return TryGetRendererBoundsFromChildren(out bounds);
        }

        private bool TryGetRendererBoundsFromChildren(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            foreach (Renderer blockRenderer in GetComponentsInChildren<Renderer>(false))
            {
                if (!IsUsable(blockRenderer))
                    continue;

                if (!hasBounds)
                {
                    bounds = blockRenderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(blockRenderer.bounds);
            }

            return hasBounds;
        }

        private static bool IsUsable(Collider collider)
        {
            return collider != null
                && collider.enabled
                && collider.gameObject.activeInHierarchy;
        }

        private static bool IsUsable(Renderer blockRenderer)
        {
            return blockRenderer != null
                && blockRenderer.enabled
                && blockRenderer.gameObject.activeInHierarchy;
        }

        private void WarnBlockAreaFallback(string fallbackReason)
        {
            if (string.IsNullOrEmpty(fallbackReason))
            {
                _lastWarnedBlockAreaSource = InvalidBlockAreaSource;
                return;
            }

            if (_lastWarnedBlockAreaSource == _blockAreaSource)
                return;

            Debug.LogWarning(
                $"{nameof(BlockerComponent)} on {name} {fallbackReason} " +
                $"Falling back to {nameof(BlockAreaSource.Transform)} block area.",
                this);
            _lastWarnedBlockAreaSource = _blockAreaSource;
        }

        private void WarnMissingWorld()
        {
            if (_missingWorldWarningLogged)
                return;

            Debug.LogWarning(
                $"{nameof(BlockerComponent)} on {name} could not resolve an active {nameof(GridWorldComponent)}. " +
                $"Assign one explicitly when working with multiple worlds.",
                this);
            _missingWorldWarningLogged = true;
        }

        public void ConfigureBoundsBlocker(bool isActive = true, bool cacheCoveredVoxels = false)
        {
            _blockerType = BlockerType.Bounds;
            _isActive = isActive;
            _cacheCoveredVoxels = cacheCoveredVoxels;
        }

        public void ConfigureManualFixedBoundArea(FixedBoundArea blockArea)
        {
            _blockAreaMode = BlockAreaMode.Bounds3D;
            _blockAreaSource = BlockAreaSource.Manual;
            _manualBlockArea = blockArea;
        }

        public void ConfigureManualXzArea(
            Vector2d min,
            Vector2d max,
            Fixed64 layerY = default)
        {
            _blockAreaMode = BlockAreaMode.XzLayer;
            _blockAreaSource = BlockAreaSource.Manual;
            _manualXzBlockAreaMin = min;
            _manualXzBlockAreaMax = max;
            _layerY = layerY;
        }

        public bool TryCountPreviewCoverage(
            GridWorld world,
            out int gridCount,
            out int voxelCount)
        {
            gridCount = 0;
            voxelCount = 0;

            if (world == null || !world.IsActive)
                return false;

            foreach (GridVoxelSet covered in GetPreviewCoverage(world))
            {
                gridCount++;
                voxelCount += covered.Voxels.Count;
            }

            return true;
        }

        private System.Collections.Generic.IEnumerable<GridVoxelSet> GetPreviewCoverage(GridWorld world)
        {
            if (_blockAreaMode == BlockAreaMode.XzLayer)
            {
                FixedBoundArea xzArea = ResolveBlockArea();
                return GridForge.Utility.GridTracer.GetCoveredVoxels(
                    world,
                    new Vector2d(xzArea.Min.X, xzArea.Min.Z),
                    new Vector2d(xzArea.Max.X, xzArea.Max.Z),
                    _layerY);
            }

            FixedBoundArea area = ResolveBlockArea();
            return GridForge.Utility.GridTracer.GetCoveredVoxels(world, area.Min, area.Max);
        }

        public IBlocker CreateBoundsBlocker(GridWorld world)
        {
            if (_blockAreaMode == BlockAreaMode.XzLayer)
            {
                FixedBoundArea xzArea = ResolveBlockArea();
                return new BoundsBlocker(
                    world,
                    new Vector2d(xzArea.Min.X, xzArea.Min.Z),
                    new Vector2d(xzArea.Max.X, xzArea.Max.Z),
                    _layerY,
                    _isActive,
                    _cacheCoveredVoxels);
            }

            return new BoundsBlocker(
                world,
                BlockArea,
                _isActive,
                _cacheCoveredVoxels);
        }

        public bool IsActive => _isActive;
        public bool CacheCoveredVoxels => _cacheCoveredVoxels;
        public BlockAreaSource BlockAreaSource => _blockAreaSource;
        public BlockAreaMode BlockAreaMode => _blockAreaMode;
        public bool IncludeChildrenInBlockArea => _includeChildrenInBlockArea;
        public FixedBoundArea ManualBlockArea => _manualBlockArea;
        public Vector2d ManualXzBlockAreaMin => _manualXzBlockAreaMin;
        public Vector2d ManualXzBlockAreaMax => _manualXzBlockAreaMax;
        public Fixed64 LayerY => _layerY;
        public FixedBoundArea BlockArea => ResolveBlockArea();
    }

    public static class BlockerFactory
    {
        public static IBlocker CreateBlocker(BlockerType type, BlockerComponent component, GridWorld world)
        {
            return type switch
            {
                BlockerType.Bounds => component.CreateBoundsBlocker(world),
                _ => null
            };
        }
    }
}
