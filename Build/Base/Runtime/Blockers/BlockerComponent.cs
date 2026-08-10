//=======================================================================
// BlockerComponent.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using FixedMathSharp;
using FixedMathSharp.Geometry;
using GridForge.Grids;
using GridForge.Unity;
using GridForge.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace GridForge.Blockers
{
    /// <summary>
    /// Unity component that allows selection of a blocker type in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class BlockerComponent : MonoBehaviour
    {
        #region Fields and Properties

        private const BlockAreaSource InvalidBlockAreaSource = (BlockAreaSource)(-1);

        [SerializeField] private bool _isActive;

        [SerializeField] private bool _cacheCoveredVoxels;

        [SerializeField] private BlockAreaSource _blockAreaSource;

        [SerializeField] private bool _includeChildrenInBlockArea;

        [SerializeField] private FixedBoundBox _manualBlockBox;

        [SerializeField] private bool _showCoveragePreview;

        [SerializeField] private Color _coveragePreviewColor = new(1f, 0.35f, 0.1f, 0.8f);

        [SerializeField] private GridWorldComponent _gridWorldComponent;

        private IBlocker _blocker;
        private GridWorld _resolvedWorld;
        private bool _hasStarted;
        private bool _missingWorldWarningLogged;
        private BlockAreaSource _lastWarnedBlockAreaSource = InvalidBlockAreaSource;

        /// <summary>Gets whether this component has applied its blockage.</summary>
        public bool IsSet { get; private set; }

        /// <summary>Gets the resolved runtime world.</summary>
        public GridWorld World
        {
            get
            {
                var worldComp = ResolveGridWorldComponent();
                return worldComp != null ? worldComp.World : null;
            }
        }

        /// <summary>Gets whether the blocker marks covered voxels as active.</summary>
        public bool IsActive => _isActive;
        /// <summary>Gets whether covered voxels are cached by the blocker.</summary>
        public bool CacheCoveredVoxels => _cacheCoveredVoxels;
        /// <summary>Gets the source used to calculate blocker bounds.</summary>
        public BlockAreaSource BlockAreaSource => _blockAreaSource;
        /// <summary>Gets whether child colliders or renderers contribute bounds.</summary>
        public bool IncludeChildrenInBlockArea => _includeChildrenInBlockArea;
        /// <summary>Gets the explicitly authored fixed-point bounds.</summary>
        public FixedBoundBox ManualBlockBox => _manualBlockBox;
        /// <summary>Gets the currently resolved blocker bounds.</summary>
        public FixedBoundBox BlockBox => ResolveBlockBox();

        #endregion

        private void Reset()
        {
            _blockAreaSource = BlockAreaSource.Transform;
        }

        private void Start()
        {
            _hasStarted = true;
            TryApplyBlockage();
        }

        private void OnEnable()
        {
            if (_hasStarted)
                TryApplyBlockage();
        }

        private void OnDisable()
        {
            _blocker?.RemoveBlockage();
            IsSet = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
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
                    var cell = GridDiagnosticGizmoDrawer.CreatePhysicalCell(world, covered.Grid, voxel);
                    GridDiagnosticGizmoDrawer.DrawWireCell(in cell, _coveragePreviewColor);
                }
            }
        }
#endif

        /// <summary>Tries to apply this blocker's configured coverage to its resolved world.</summary>
        /// <returns><see langword="true"/> when blockage is applied or was already applied.</returns>
        public bool TryApplyBlockage()
        {
            GridWorld world = World;
            if (world == null || !world.IsActive)
            {
                WarnMissingWorld();
                return false;
            }

            _missingWorldWarningLogged = false;

            if (IsSet && ReferenceEquals(_resolvedWorld, world))
                return true;

            if (!TryCreateBlocker(world))
                return false;

            _blocker.ApplyBlockage();
            IsSet = true;
            return true;
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

            _blocker = new BoundsBlocker(
                world,
                BlockBox,
                _isActive,
                _cacheCoveredVoxels);
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

        /// <summary>Calculates the blocker bounds and reports any fallback to transform bounds.</summary>
        /// <param name="resolvedSource">Receives the source that produced the bounds.</param>
        /// <param name="fallbackReason">Receives the reason the configured source could not be used.</param>
        /// <returns>The calculated fixed-point bounds.</returns>
        public FixedBoundBox CalculateBlockBox(
            out BlockAreaSource resolvedSource,
            out string fallbackReason)
        {
            switch (_blockAreaSource)
            {
                case BlockAreaSource.Manual:
                    resolvedSource = BlockAreaSource.Manual;
                    fallbackReason = string.Empty;
                    return _manualBlockBox;

                case BlockAreaSource.Transform:
                    resolvedSource = BlockAreaSource.Transform;
                    fallbackReason = string.Empty;
                    return CreateTransformBlockBox();

                case BlockAreaSource.Collider:
                    if (TryGetColliderBounds(out Bounds colliderBounds))
                    {
                        resolvedSource = BlockAreaSource.Collider;
                        fallbackReason = string.Empty;
                        return CreateBoundsBlockBox(colliderBounds);
                    }

                    resolvedSource = BlockAreaSource.Transform;
                    fallbackReason =
                        $"could not resolve an enabled {nameof(Collider)} for {nameof(BlockAreaSource.Collider)} source.";
                    return CreateTransformBlockBox();

                case BlockAreaSource.Renderer:
                    if (TryGetRendererBounds(out Bounds rendererBounds))
                    {
                        resolvedSource = BlockAreaSource.Renderer;
                        fallbackReason = string.Empty;
                        return CreateBoundsBlockBox(rendererBounds);
                    }

                    resolvedSource = BlockAreaSource.Transform;
                    fallbackReason =
                        $"could not resolve an enabled {nameof(Renderer)} for {nameof(BlockAreaSource.Renderer)} source.";
                    return CreateTransformBlockBox();

                default:
                    resolvedSource = BlockAreaSource.Transform;
                    fallbackReason = $"has unsupported block area source {_blockAreaSource}.";
                    return CreateTransformBlockBox();
            }
        }

        private FixedBoundBox ResolveBlockBox()
        {
            FixedBoundBox blockBox = CalculateBlockBox(out _, out string fallbackReason);
            WarnBlockAreaFallback(fallbackReason);
            return blockBox;
        }

        private FixedBoundBox CreateTransformBlockBox()
        {
            Vector3 center = transform.position;
            Vector3 size = transform.lossyScale;
            size.x = Mathf.Abs(size.x);
            size.y = Mathf.Abs(size.y);
            size.z = Mathf.Abs(size.z);

            Vector3 extents = size * 0.5f;
            return CreateBlockBox(center - extents, center + extents);
        }

        private static FixedBoundBox CreateBoundsBlockBox(Bounds bounds) =>
            CreateBlockBox(bounds.min, bounds.max);

        private static FixedBoundBox CreateBlockBox(Vector3 min, Vector3 max) =>
            FixedBoundBox.FromMinMax(
                Vector3d.FromDouble(min.x, min.y, min.z),
                Vector3d.FromDouble(max.x, max.y, max.z));

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

        /// <summary>Configures the blocker state used when applying coverage.</summary>
        /// <param name="isActive">Whether covered voxels are marked active.</param>
        /// <param name="cacheCoveredVoxels">Whether the blocker caches its covered voxels.</param>
        public void ConfigureBoundsBlocker(bool isActive = true, bool cacheCoveredVoxels = false)
        {
            InvalidateBlocker();
            _isActive = isActive;
            _cacheCoveredVoxels = cacheCoveredVoxels;
        }

        /// <summary>Uses explicit fixed-point bounds for blockage.</summary>
        /// <param name="blockBox">The bounds to apply.</param>
        public void ConfigureManualFixedBoundBox(FixedBoundBox blockBox)
        {
            InvalidateBlocker();
            _blockAreaSource = BlockAreaSource.Manual;
            _manualBlockBox = blockBox;
        }

        private void InvalidateBlocker()
        {
            _blocker?.RemoveBlockage();
            _blocker = null;
            _resolvedWorld = null;
            IsSet = false;
        }

        /// <summary>Counts the grids and voxels covered without applying blockage.</summary>
        /// <param name="world">The active world to query.</param>
        /// <param name="gridCount">Receives the number of covered grids.</param>
        /// <param name="voxelCount">Receives the number of covered voxels.</param>
        /// <returns><see langword="true"/> when an active world was queried.</returns>
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

        private IEnumerable<GridVoxelSet> GetPreviewCoverage(GridWorld world)
        {
            FixedBoundBox area = ResolveBlockBox();
            return GridTracer.GetCoveredVoxels(world, area.Min, area.Max);
        }
    }
}
