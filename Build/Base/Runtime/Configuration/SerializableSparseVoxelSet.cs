using GridForge.Spatial;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridForge.Configuration
{
    /// <summary>
    /// Unity-serializable topology-local voxel index.
    /// </summary>
    [Serializable]
    public struct SerializableVoxelIndex
    {
        [SerializeField] private int _x;
        [SerializeField] private int _y;
        [SerializeField] private int _z;

        public readonly int X => _x;
        public readonly int Y => _y;
        public readonly int Z => _z;

        public SerializableVoxelIndex(int x, int y, int z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        public readonly VoxelIndex ToVoxelIndex()
        {
            return new VoxelIndex(_x, _y, _z);
        }

        public static SerializableVoxelIndex FromVoxelIndex(VoxelIndex index)
        {
            return new SerializableVoxelIndex(index.x, index.y, index.z);
        }
    }

    /// <summary>
    /// Unity-serializable sparse voxel authoring data.
    /// </summary>
    [Serializable]
    public struct SerializableSparseVoxelSet
    {
        [SerializeField] private List<SerializableVoxelIndex> _indices;

        public readonly int Count => _indices?.Count ?? 0;
        public readonly bool HasConfiguredVoxels => Count > 0;
        public readonly IReadOnlyList<SerializableVoxelIndex> Indices
        {
            get
            {
                if (_indices != null)
                    return _indices;

                return Array.Empty<SerializableVoxelIndex>();
            }
        }

        public static SerializableSparseVoxelSet Empty => new(Array.Empty<SerializableVoxelIndex>());

        public SerializableSparseVoxelSet(IEnumerable<SerializableVoxelIndex> indices)
        {
            _indices = indices == null
                ? new List<SerializableVoxelIndex>()
                : new List<SerializableVoxelIndex>(indices);
        }

        public readonly bool TryToVoxelIndices(out VoxelIndex[] indices, out string failureReason)
        {
            failureReason = string.Empty;
            if (_indices == null || _indices.Count == 0)
            {
                indices = Array.Empty<VoxelIndex>();
                return true;
            }

            indices = new VoxelIndex[_indices.Count];
            for (int i = 0; i < _indices.Count; i++)
            {
                SerializableVoxelIndex serialized = _indices[i];
                if (serialized.X < 0 || serialized.Y < 0 || serialized.Z < 0)
                {
                    failureReason = $"Sparse voxel index ({serialized.X}, {serialized.Y}, {serialized.Z}) must be non-negative.";
                    indices = Array.Empty<VoxelIndex>();
                    return false;
                }

                indices[i] = serialized.ToVoxelIndex();
            }

            return true;
        }

        public static SerializableSparseVoxelSet FromVoxelIndices(IEnumerable<VoxelIndex> indices)
        {
            if (indices == null)
                return Empty;

            List<SerializableVoxelIndex> serialized = new();
            foreach (VoxelIndex index in indices)
                serialized.Add(SerializableVoxelIndex.FromVoxelIndex(index));

            return new SerializableSparseVoxelSet(serialized);
        }
    }
}
