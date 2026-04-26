using System;
using UnityEngine;
using FixedMathSharp;

namespace GridForge.Configuration
{
    /// <summary>
    /// A Unity-serializable version of <see cref="GridConfiguration"/> for editor use.
    /// </summary>
    [Serializable]
    public struct SerializableGridConfiguration
    {
        [SerializeField] private Vector3d _boundsMin;
        [SerializeField] private Vector3d _boundsMax;
        [SerializeField]
        [Tooltip("Defaults to 8.")]
        private int _scanCellSize;

        public readonly Vector3d BoundsMin => _boundsMin;
        public readonly Vector3d BoundsMax => _boundsMax;
        public readonly int ScanCellSize => _scanCellSize;

        public SerializableGridConfiguration(Vector3d boundsMin, Vector3d boundsMax, int scanCellSize)
        {
            _boundsMin = boundsMin;
            _boundsMax = boundsMax;
            _scanCellSize = scanCellSize > 0 ? scanCellSize : GridConfiguration.DefaultScanCellSize;
        }

        /// <summary>
        /// Converts this Unity-serializable struct to the GridForge <see cref="GridConfiguration"/>.
        /// </summary>
        public readonly GridConfiguration ToGridConfiguration()
        {
            return new GridConfiguration(
                _boundsMin,
                _boundsMax,
                _scanCellSize > 0 ? _scanCellSize : GridConfiguration.DefaultScanCellSize
            );
        }

        /// <summary>
        /// Creates a serializable version from a <see cref="GridConfiguration"/>.
        /// </summary>
        public static SerializableGridConfiguration FromGridConfiguration(GridConfiguration config)
        {
            return new SerializableGridConfiguration(
                config.BoundsMin,
                config.BoundsMax,
                config.ScanCellSize > 0 ? config.ScanCellSize : GridConfiguration.DefaultScanCellSize
            );
        }
    }
}
