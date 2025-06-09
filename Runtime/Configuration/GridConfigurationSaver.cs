using FixedMathSharp;
using GridForge.Grids;
using System.Collections.Generic;
using UnityEngine;

namespace GridForge.Configuration
{
    /// <summary>
    /// Unity Editor utility for saving and applying grid configurations.
    /// Allows multiple grid configurations to be saved and applied in the scene editor.
    /// </summary>
    [ExecuteAlways] // Ensures it runs in edit mode
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
        /// The size of each grid voxel in world units.
        /// </summary>
        [SerializeField] private Fixed64 _voxelSize = Fixed64.One;

        /// <inheritdoc cref="_voxelSize"/>
        public Fixed64 VoxelSize => _voxelSize;

        /// <summary>
        /// List of saved grid configurations.
        /// </summary>
        [SerializeField] private List<SerializableGridConfiguration> _savedGridConfigurations = new();

        public IReadOnlyList<SerializableGridConfiguration> SavedGridConfigurations => _savedGridConfigurations;

        /// <summary>
        /// Enables visualization of grid bounds in the editor.
        /// </summary>
        public bool Show = true;

        #endregion

        #region Grid Management

        /// <summary>
        /// Saves the current grid configuration into the list.
        /// </summary>
        public void Save(Vector3d boundsMin, Vector3d boundsMax, int scanCellSize)
        {
            _savedGridConfigurations.Add(new SerializableGridConfiguration(boundsMin, boundsMax, scanCellSize));
        }

        /// <summary>
        /// Applies all saved grid configurations to the global grid manager.
        /// </summary>
        public void EarlyApply()
        {
            foreach (var serializedConfig in SavedGridConfigurations)
            {
                // Ensure grid bounds are valid before adding
                if (serializedConfig.BoundsMax < serializedConfig.BoundsMin)
                {
                    Debug.LogWarning("Invalid Grid Bounds: GridMax must be greater than or equal to GridMin.");
                    continue;
                }

                GridConfiguration config = serializedConfig.ToGridConfiguration();
                if(GlobalGridManager.TryAddGrid(config, out _) != GridAddResult.Success)
                {
                    Debug.LogWarning($"Failed to add grid to global state: " +
                        $"{config.BoundsMin} - {config.BoundsMax}");
                }
            }
        }

        #endregion

        #region Editor Visualization

        public void OnDrawGizmos()
        {
            if (!Show || Application.isPlaying)
                return;

            Gizmos.color = Color.green;
            Vector3 scale = Vector3.one * (float)_voxelSize;

            foreach (var serializedConfig in SavedGridConfigurations)
            {
                for (Fixed64 x = serializedConfig.BoundsMin.x; x <= serializedConfig.BoundsMax.x; x++)
                {
                    for (Fixed64 y = serializedConfig.BoundsMin.y; y <= serializedConfig.BoundsMax.y; y++)
                    {
                        for (Fixed64 z = serializedConfig.BoundsMin.z; z <= serializedConfig.BoundsMax.z; z++)
                        {
                            Vector3d drawPos = new Vector3d(x, y, z);
                            Gizmos.DrawCube(drawPos.ToVector3(), scale); // Draws the solid cube
                            Gizmos.color = Color.black; // Change color for wireframe
                            Gizmos.DrawWireCube(drawPos.ToVector3(), scale * 1.02f); // Slightly larger for visibility
                            Gizmos.color = Color.green; // Reset to original color
                        }
                    }
                }
            }
        }

        #endregion
    }
}