using FixedMathSharp;
using GridForge.Grids;
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

        [SerializeField] private bool _includeChildrenInBlockArea;

        [SerializeField] private BoundingArea _manualBlockArea;

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

        public BoundingArea CalculateBlockArea(
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

        private BoundingArea ResolveBlockArea()
        {
            BoundingArea blockArea = CalculateBlockArea(out _, out string fallbackReason);
            WarnBlockAreaFallback(fallbackReason);
            return blockArea;
        }

        private BoundingArea CreateTransformBlockArea()
        {
            Vector3 center = transform.position;
            Vector3 size = transform.lossyScale;
            size.x = Mathf.Abs(size.x);
            size.y = Mathf.Abs(size.y);
            size.z = Mathf.Abs(size.z);

            Vector3 extents = size * 0.5f;
            return CreateBlockArea(center - extents, center + extents);
        }

        private static BoundingArea CreateBoundsBlockArea(Bounds bounds)
        {
            return CreateBlockArea(bounds.min, bounds.max);
        }

        private static BoundingArea CreateBlockArea(Vector3 min, Vector3 max)
        {
            return new BoundingArea(
                new Vector3d(min.x, min.y, min.z),
                new Vector3d(max.x, max.y, max.z));
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

        public bool IsActive => _isActive;
        public bool CacheCoveredVoxels => _cacheCoveredVoxels;
        public BlockAreaSource BlockAreaSource => _blockAreaSource;
        public bool IncludeChildrenInBlockArea => _includeChildrenInBlockArea;
        public BoundingArea ManualBlockArea => _manualBlockArea;
        public BoundingArea BlockArea => ResolveBlockArea();
    }

    public static class BlockerFactory
    {
        public static IBlocker CreateBlocker(BlockerType type, BlockerComponent component, GridWorld world)
        {
            return type switch
            {
                BlockerType.Bounds => new BoundsBlocker(
                    world,
                    component.BlockArea,
                    component.IsActive,
                    component.CacheCoveredVoxels),
                _ => null
            };
        }
    }
}
