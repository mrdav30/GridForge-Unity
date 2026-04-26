using FixedMathSharp;
using GridForge.Grids;
using UnityEngine;

namespace GridForge.Unity
{
    /// <summary>
    /// Unity host component that owns the explicit <see cref="GridWorld"/> runtime instance for a scene.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public class GridWorldComponent : MonoBehaviour
    {
        [SerializeField] private Fixed64 _voxelSize = Fixed64.One;

        [SerializeField] private int _spatialGridCellSize = GridWorld.DefaultSpatialGridCellSize;

        [SerializeField] private bool _initializeOnAwake = true;

        [SerializeField] private bool _disposeOnDestroy = true;

        public GridWorld World { get; private set; }

        public Fixed64 VoxelSize => World?.VoxelSize ?? _voxelSize;

        public int SpatialGridCellSize => World?.SpatialGridCellSize ?? _spatialGridCellSize;

        public bool IsWorldActive => World?.IsActive ?? false;

        private void Awake()
        {
            if (_initializeOnAwake)
                EnsureWorld();
        }

        private void OnDestroy()
        {
            if (_disposeOnDestroy)
                DisposeWorld();
        }

        private void OnValidate()
        {
            if (_voxelSize <= Fixed64.Zero)
                _voxelSize = GridWorld.DefaultVoxelSize;

            if (_spatialGridCellSize < 1)
                _spatialGridCellSize = 1;
        }

        /// <summary>
        /// Creates the runtime world if it does not already exist or is inactive.
        /// </summary>
        public GridWorld EnsureWorld()
        {
            if (World != null && World.IsActive)
                return World;

            DisposeWorld();
            World = new GridWorld(_voxelSize, _spatialGridCellSize);
            return World;
        }

        /// <summary>
        /// Recreates the runtime world with a new configuration.
        /// </summary>
        public GridWorld RebuildWorld(Fixed64 voxelSize, int spatialGridCellSize)
        {
            _voxelSize = voxelSize > Fixed64.Zero ? voxelSize : GridWorld.DefaultVoxelSize;
            _spatialGridCellSize = Mathf.Max(1, spatialGridCellSize);

            DisposeWorld();
            World = new GridWorld(_voxelSize, _spatialGridCellSize);
            return World;
        }

        /// <summary>
        /// Resets the current world instance.
        /// </summary>
        public void ResetWorld(bool deactivate = false)
        {
            if (World == null)
                return;

            World.Reset(deactivate);

            if (deactivate)
                World = null;
        }

        /// <summary>
        /// Disposes the current world instance and clears the cached reference.
        /// </summary>
        public void DisposeWorld()
        {
            if (World == null)
                return;

            World.Dispose();
            World = null;
        }
    }

    /// <summary>
    /// Helper for locating a <see cref="GridWorldComponent"/> relative to a Unity component.
    /// </summary>
    public static class GridWorldComponentUtility
    {
        public static GridWorldComponent Resolve(Component context, GridWorldComponent assignedComponent)
        {
            if (assignedComponent != null)
                return assignedComponent;

            if (context != null)
            {
                if (context.TryGetComponent(out GridWorldComponent localWorld))
                    return localWorld;

                GridWorldComponent parentWorld = context.GetComponentInParent<GridWorldComponent>();
                if (parentWorld != null)
                    return parentWorld;
            }

            return Object.FindFirstObjectByType<GridWorldComponent>();
        }
    }
}
