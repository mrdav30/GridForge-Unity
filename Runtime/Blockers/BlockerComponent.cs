using FixedMathSharp;
using GridForge.Grids;
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

        private IBlocker _blocker;

        public bool IsSet;

        public void Awake()
        {
            _blocker = BlockerFactory.CreateBlocker(_blockerType, this);
        }

        public void Start()
        {
            if (!GlobalGridManager.IsActive)
                return;
            _blocker.ApplyBlockage();
            IsSet = true;
        }

        public void OnEnable()
        {
            if (!GlobalGridManager.IsActive || IsSet)
                return;
            _blocker.ApplyBlockage();
            IsSet = true;
        }

        public void OnDisable()
        {
            _blocker?.RemoveBlockage();
            IsSet = false;
        }

        public bool IsActive => _isActive;
        public bool CacheCoveredVoxels => _cacheCoveredVoxels;
        public BoundingArea ManualBlockArea => _manualBlockArea;
    }

    public static class BlockerFactory
    {
        public static IBlocker CreateBlocker(BlockerType type, BlockerComponent component)
        {
            return type switch
            {
                BlockerType.Bounds => new BoundsBlocker(
                    component.ManualBlockArea,
                    component.IsActive,
                    component.CacheCoveredVoxels),
                _ => null
            };
        }
    }
}