//=======================================================================
// SerializableSparseVoxelSet.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using GridForge.Spatial;
using SwiftCollections;
using System;
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
        [SerializeField] private SerializableVoxelIndex[] _indices;

        public readonly int Count => Indices.Length;
        public readonly bool HasConfiguredVoxels => Count > 0;
        public readonly SerializableVoxelIndex[] Indices => _indices ?? Array.Empty<SerializableVoxelIndex>();

        public static SerializableSparseVoxelSet Empty => new(Array.Empty<SerializableVoxelIndex>());

        public SerializableSparseVoxelSet(System.Collections.Generic.IEnumerable<SerializableVoxelIndex> indices)
        {
            _indices = ToArray(indices);
        }

        public readonly bool TryToVoxelIndices(out VoxelIndex[] indices, out string failureReason)
        {
            failureReason = string.Empty;
            SerializableVoxelIndex[] serializedIndices = Indices;
            if (serializedIndices.Length == 0)
            {
                indices = Array.Empty<VoxelIndex>();
                return true;
            }

            indices = new VoxelIndex[serializedIndices.Length];
            for (int i = 0; i < serializedIndices.Length; i++)
            {
                SerializableVoxelIndex serialized = serializedIndices[i];
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

        public static SerializableSparseVoxelSet FromVoxelIndices(System.Collections.Generic.IEnumerable<VoxelIndex> indices)
        {
            if (indices == null)
                return Empty;

            SwiftList<SerializableVoxelIndex> serialized = new();
            foreach (VoxelIndex index in indices)
                serialized.Add(SerializableVoxelIndex.FromVoxelIndex(index));

            return new SerializableSparseVoxelSet(serialized);
        }

        private static SerializableVoxelIndex[] ToArray(System.Collections.Generic.IEnumerable<SerializableVoxelIndex> indices)
        {
            if (indices == null)
                return Array.Empty<SerializableVoxelIndex>();

            if (indices is SerializableVoxelIndex[] array)
                return array;

            SwiftList<SerializableVoxelIndex> serialized = new();
            foreach (SerializableVoxelIndex index in indices)
                serialized.Add(index);

            return serialized.ToArray();
        }
    }
}
