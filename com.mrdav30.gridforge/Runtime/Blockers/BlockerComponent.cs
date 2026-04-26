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
    /// Unity component that allows selection of a blocker type in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class BlockerComponent : MonoBehaviour
    {
        [SerializeField] private BlockerType _blockerType;

        [SerializeField] private bool _isActive;

        [SerializeField] private bool _cacheCoveredVoxels;

        [SerializeField] private BoundingArea _manualBlockArea;

        [SerializeField] private GridWorldComponent _gridWorldComponent;

        private IBlocker _blocker;
        private GridWorld _resolvedWorld;
        private bool _hasStarted;
        private bool _missingWorldWarningLogged;

        public bool IsSet;

        public GridWorld World
        {
            get
            {
                var worldComp = ResolveGridWorldComponent();
                return worldComp != null ? worldComp.World : null;
            }
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
        public BoundingArea ManualBlockArea => _manualBlockArea;
    }

    public static class BlockerFactory
    {
        public static IBlocker CreateBlocker(BlockerType type, BlockerComponent component, GridWorld world)
        {
            return type switch
            {
                BlockerType.Bounds => new BoundsBlocker(
                    world,
                    component.ManualBlockArea,
                    component.IsActive,
                    component.CacheCoveredVoxels),
                _ => null
            };
        }
    }
}
