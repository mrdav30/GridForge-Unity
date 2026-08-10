//=======================================================================
// GridWorldComponent.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

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
        [SerializeField] private int _spatialGridCellSize = GridWorld.DefaultSpatialGridCellSize;

        [SerializeField] private bool _initializeOnAwake = true;

        [SerializeField] private bool _disposeOnDestroy = true;

        /// <summary>Gets the runtime grid world owned by this component.</summary>
        public GridWorld World { get; private set; }

        /// <summary>Gets the configured spatial grid cell size.</summary>
        public int SpatialGridCellSize => World?.SpatialGridCellSize ?? _spatialGridCellSize;

        /// <summary>Gets whether the owned grid world is active.</summary>
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
            if (_spatialGridCellSize < 1)
                _spatialGridCellSize = 1;
        }

        /// <summary>
        /// Creates the runtime world if it does not already exist or is inactive.
        /// </summary>
        /// <returns>The active runtime world.</returns>
        public GridWorld EnsureWorld()
        {
            if (World != null && World.IsActive)
                return World;

            DisposeWorld();
            World = new GridWorld(_spatialGridCellSize);
            return World;
        }

        /// <summary>
        /// Recreates the runtime world with a new configuration.
        /// </summary>
        /// <param name="spatialGridCellSize">The spatial hash cell size, clamped to at least one.</param>
        /// <returns>The rebuilt active runtime world.</returns>
        public GridWorld RebuildWorld(int spatialGridCellSize)
        {
            _spatialGridCellSize = Mathf.Max(1, spatialGridCellSize);

            DisposeWorld();
            World = new GridWorld(_spatialGridCellSize);
            return World;
        }

        /// <summary>
        /// Resets the current world instance.
        /// </summary>
        /// <param name="deactivate">Whether to deactivate and release the current world reference.</param>
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
        /// <summary>Resolves an assigned, local, or parent world component in that order.</summary>
        /// <param name="context">The component whose GameObject hierarchy is searched.</param>
        /// <param name="assignedComponent">An explicitly assigned world component.</param>
        /// <returns>The resolved component, or <see langword="null"/> when none is explicitly related.</returns>
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

            return null;
        }
    }
}
