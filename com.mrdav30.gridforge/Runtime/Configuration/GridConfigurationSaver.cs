//=======================================================================
// GridConfigurationSaver.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using FixedMathSharp;
using GridForge.Grids;
using GridForge.Grids.Storage;
using GridForge.Spatial;
using GridForge.Unity;
using SwiftCollections;
using SwiftCollections.Unity;
using UnityEngine;

namespace GridForge.Configuration
{
    /// <summary>
    /// Unity Editor utility for saving and applying grid configurations.
    /// Allows multiple grid configurations to be saved and applied in the scene editor.
    /// </summary>
    [ExecuteAlways]
    public class GridConfigurationSaver : MonoBehaviour
    {
        #region Properties

        /// <summary>
        /// The size of a spatial hash cell used for grid lookup.
        /// </summary>
        [SerializeField] private int _spatialGridCellSize = 50;

        /// <inheritdoc cref="_spatialGridCellSize"/>
        public int SpatialGridCellSize => _spatialGridCellSize;

        /// <summary>
        /// Saved grid configurations.
        /// </summary>
        [SerializeField] private SerializedSwiftList<SerializableGridConfiguration> _savedGridConfigurations = new();

        public SwiftList<SerializableGridConfiguration> SavedGridConfigurations => SavedConfigurationAdapter.Runtime;

        /// <summary>
        /// Enables visualization of grid bounds in the editor.
        /// </summary>
        public bool Show = true;

        #endregion

        private SerializedSwiftList<SerializableGridConfiguration> SavedConfigurationAdapter
        {
            get
            {
                _savedGridConfigurations ??= new SerializedSwiftList<SerializableGridConfiguration>();
                return _savedGridConfigurations;
            }
        }

        private void OnValidate()
        {
            if (_spatialGridCellSize < 1)
                _spatialGridCellSize = 1;
        }

        #region Grid Management

        /// <summary>
        /// Saves the current grid configuration into the list.
        /// </summary>
        public void Save(Vector3d boundsMin, Vector3d boundsMax, int scanCellSize)
        {
            Save(new SerializableGridConfiguration(boundsMin, boundsMax, scanCellSize));
        }

        /// <summary>
        /// Saves the supplied grid configuration into the list.
        /// </summary>
        public void Save(SerializableGridConfiguration configuration)
        {
            SavedConfigurationAdapter.Add(configuration);
        }

        /// <summary>
        /// Applies all saved grid configurations to the resolved scene world.
        /// </summary>
        public void EarlyApply()
        {
            GridWorldComponent worldComponent = GridWorldComponentUtility.Resolve(this, null);
            if (worldComponent == null)
            {
                Debug.LogWarning($"Unable to resolve a {nameof(GridWorldComponent)} for {nameof(GridConfigurationSaver)} on {name}.", this);
                return;
            }

            EarlyApply(worldComponent.EnsureWorld());
        }

        /// <summary>
        /// Applies all saved grid configurations to the supplied <see cref="GridWorld"/>.
        /// </summary>
        public void EarlyApply(GridWorld world)
        {
            if (world == null || !world.IsActive)
            {
                Debug.LogWarning($"Cannot apply saved grids because no active {nameof(GridWorld)} is available.", this);
                return;
            }

            foreach (SerializableGridConfiguration serializedConfig in SavedGridConfigurations)
                TryApplyConfiguration(world, serializedConfig);
        }

        private bool TryApplyConfiguration(GridWorld world, SerializableGridConfiguration serializedConfig)
        {
            if (serializedConfig.BoundsMax < serializedConfig.BoundsMin)
            {
                Debug.LogWarning("Invalid Grid Bounds: GridMax must be greater than or equal to GridMin.", this);
                return false;
            }

            if (!serializedConfig.TryToGridConfiguration(out GridConfiguration config, out string configFailure))
            {
                Debug.LogWarning($"Invalid Grid Configuration: {configFailure}", this);
                return false;
            }

            if (config.StorageKind == GridStorageKind.Sparse)
            {
                if (!serializedConfig.TryGetConfiguredSparseVoxels(out VoxelIndex[] configuredVoxels, out string sparseFailure))
                {
                    Debug.LogWarning($"Invalid Sparse Voxel Configuration: {sparseFailure}", this);
                    return false;
                }

                if (!world.TryAddGrid(config, configuredVoxels, out _))
                {
                    Debug.LogWarning(
                        $"Failed to add sparse grid to {nameof(GridWorld)}: {config.BoundsMin} - {config.BoundsMax}. " +
                        "Check that configured sparse indices are within normalized grid dimensions.",
                        this);
                    return false;
                }

                return true;
            }

            if (!world.TryAddGrid(config, out _))
            {
                Debug.LogWarning(
                    $"Failed to add grid to {nameof(GridWorld)}: {config.BoundsMin} - {config.BoundsMax}",
                    this);
                return false;
            }

            return true;
        }

        #endregion

        #region Editor Visualization

        public void OnDrawGizmos()
        {
            if (!Show || Application.isPlaying)
                return;

            foreach (SerializableGridConfiguration serializedConfig in SavedGridConfigurations)
            {
                if (!serializedConfig.TryToGridConfiguration(out GridConfiguration config, out _))
                    continue;

                Gizmos.color = config.StorageKind == GridStorageKind.Sparse
                    ? new Color(1f, 0.78f, 0.16f, 0.9f)
                    : Color.green;
                Gizmos.DrawWireCube(config.GridCenter.ToVector3(), CalculateBoundsSize(config));
            }
        }

        private static Vector3 CalculateBoundsSize(GridConfiguration config)
        {
            Vector3d size = config.BoundsMax - config.BoundsMin;
            return new Vector3(
                Mathf.Max((float)size.X, 0.05f),
                Mathf.Max((float)size.Y, 0.05f),
                Mathf.Max((float)size.Z, 0.05f));
        }

        #endregion
    }
}
