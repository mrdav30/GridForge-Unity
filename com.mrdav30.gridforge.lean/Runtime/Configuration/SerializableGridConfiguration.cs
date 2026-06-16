//=======================================================================
// SerializableGridConfiguration.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using FixedMathSharp;
using GridForge.Grids.Storage;
using GridForge.Grids.Topology;
using GridForge.Spatial;
using System;
using UnityEngine;

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
        [SerializeField] private GridTopologyKind _topologyKind;
        [SerializeField] private SerializableGridTopologyMetrics _topologyMetrics;
        [SerializeField] private GridStorageKind _storageKind;
        [SerializeField] private SerializableSparseVoxelSet _configuredSparseVoxels;

        public readonly Vector3d BoundsMin => _boundsMin;
        public readonly Vector3d BoundsMax => _boundsMax;
        public readonly int ScanCellSize => _scanCellSize;
        public readonly GridTopologyKind TopologyKind => _topologyKind;
        public readonly SerializableGridTopologyMetrics TopologyMetrics => _topologyMetrics;
        public readonly GridStorageKind StorageKind => _storageKind;
        public readonly SerializableSparseVoxelSet ConfiguredSparseVoxels => _configuredSparseVoxels;

        public SerializableGridConfiguration(Vector3d boundsMin, Vector3d boundsMax, int scanCellSize)
            : this(
                  boundsMin,
                  boundsMax,
                  scanCellSize,
                  GridTopologyKind.RectangularPrism,
                  SerializableGridTopologyMetrics.DefaultRectangular,
                  GridStorageKind.Dense,
                  SerializableSparseVoxelSet.Empty)
        {
        }

        public SerializableGridConfiguration(
            Vector3d boundsMin,
            Vector3d boundsMax,
            int scanCellSize,
            GridTopologyKind topologyKind,
            SerializableGridTopologyMetrics topologyMetrics,
            GridStorageKind storageKind,
            SerializableSparseVoxelSet configuredSparseVoxels)
        {
            _boundsMin = boundsMin;
            _boundsMax = boundsMax;
            _scanCellSize = scanCellSize > 0 ? scanCellSize : GridConfiguration.DefaultScanCellSize;
            _topologyKind = topologyKind;
            _topologyMetrics = topologyMetrics;
            _storageKind = storageKind;
            _configuredSparseVoxels = configuredSparseVoxels;
        }

        /// <summary>
        /// Converts this Unity-serializable struct to the GridForge <see cref="GridConfiguration"/>.
        /// </summary>
        public readonly GridConfiguration ToGridConfiguration()
        {
            if (!TryToGridConfiguration(out GridConfiguration config, out string failureReason))
                throw new InvalidOperationException(failureReason);

            return config;
        }

        /// <summary>
        /// Tries to convert this Unity-serializable struct to the GridForge <see cref="GridConfiguration"/>.
        /// </summary>
        public readonly bool TryToGridConfiguration(out GridConfiguration config, out string failureReason)
        {
            config = default;
            failureReason = string.Empty;

            if (!IsSupportedTopologyKind(_topologyKind))
            {
                failureReason = $"Grid topology '{_topologyKind}' is not supported.";
                return false;
            }

            if (!IsSupportedStorageKind(_storageKind))
            {
                failureReason = $"Grid storage kind '{_storageKind}' is not supported.";
                return false;
            }

            if (!_topologyMetrics.TryToGridTopologyMetrics(
                    _topologyKind,
                    out GridTopologyMetrics metrics,
                    out failureReason))
            {
                return false;
            }

            config = new GridConfiguration(
                BoundsMin,
                BoundsMax,
                _scanCellSize > 0 ? _scanCellSize : GridConfiguration.DefaultScanCellSize,
                _topologyKind,
                metrics,
                _storageKind);
            return true;
        }

        /// <summary>
        /// Converts configured sparse authoring data to topology-local voxel indices.
        /// </summary>
        public readonly bool TryGetConfiguredSparseVoxels(out VoxelIndex[] configuredVoxels, out string failureReason)
        {
            if (_storageKind != GridStorageKind.Sparse)
            {
                configuredVoxels = Array.Empty<VoxelIndex>();
                failureReason = string.Empty;
                return true;
            }

            return _configuredSparseVoxels.TryToVoxelIndices(out configuredVoxels, out failureReason);
        }

        /// <summary>
        /// Creates a serializable version from a <see cref="GridConfiguration"/>.
        /// </summary>
        public static SerializableGridConfiguration FromGridConfiguration(GridConfiguration config)
        {
            return new SerializableGridConfiguration(
                config.BoundsMin,
                config.BoundsMax,
                config.ScanCellSize > 0 ? config.ScanCellSize : GridConfiguration.DefaultScanCellSize,
                config.TopologyKind,
                SerializableGridTopologyMetrics.FromGridTopologyMetrics(config.TopologyKind, config.TopologyMetrics),
                config.StorageKind,
                SerializableSparseVoxelSet.Empty);
        }

        private static bool IsSupportedTopologyKind(GridTopologyKind topologyKind)
        {
            return topologyKind == GridTopologyKind.RectangularPrism
                || topologyKind == GridTopologyKind.HexPrism;
        }

        private static bool IsSupportedStorageKind(GridStorageKind storageKind)
        {
            return storageKind == GridStorageKind.Dense
                || storageKind == GridStorageKind.Sparse;
        }
    }
}
