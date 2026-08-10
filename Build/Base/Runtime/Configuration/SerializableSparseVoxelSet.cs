//=======================================================================
// SerializableSparseVoxelSet.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using GridForge.Spatial;
using SwiftCollections;
using SwiftCollections.Unity;
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

        /// <summary>Gets the local X index.</summary>
        public readonly int X => _x;
        /// <summary>Gets the local Y index.</summary>
        public readonly int Y => _y;
        /// <summary>Gets the local Z index.</summary>
        public readonly int Z => _z;

        /// <summary>Creates a serializable topology-local voxel index.</summary>
        /// <param name="x">The local X index.</param>
        /// <param name="y">The local Y index.</param>
        /// <param name="z">The local Z index.</param>
        public SerializableVoxelIndex(int x, int y, int z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        /// <summary>Converts this value to a runtime voxel index.</summary>
        /// <returns>The runtime voxel index.</returns>
        public readonly VoxelIndex ToVoxelIndex()
        {
            return new VoxelIndex(_x, _y, _z);
        }

        /// <summary>Creates a serializable value from a runtime voxel index.</summary>
        /// <param name="index">The runtime voxel index.</param>
        /// <returns>The serializable index.</returns>
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
        [SerializeField] private SerializedSwiftList<SerializableVoxelIndex> _indices;

        /// <summary>Gets the number of configured indices.</summary>
        public readonly int Count => _indices?.Count ?? 0;
        /// <summary>Gets whether at least one voxel index is configured.</summary>
        public readonly bool HasConfiguredVoxels => Count > 0;
        /// <summary>Gets the runtime list of serializable indices.</summary>
        public readonly SwiftList<SerializableVoxelIndex> Indices => _indices?.Runtime ?? new SwiftList<SerializableVoxelIndex>();

        /// <summary>Gets an empty sparse voxel set.</summary>
        public static SerializableSparseVoxelSet Empty => new(Array.Empty<SerializableVoxelIndex>());

        /// <summary>Creates a sparse voxel set from serializable indices.</summary>
        /// <param name="indices">The indices to copy.</param>
        public SerializableSparseVoxelSet(System.Collections.Generic.IEnumerable<SerializableVoxelIndex> indices)
        {
            _indices = new SerializedSwiftList<SerializableVoxelIndex>();
            _indices.SetItems(ToArray(indices));
        }

        /// <summary>Validates and converts the configured values to runtime indices.</summary>
        /// <param name="indices">Receives the runtime indices.</param>
        /// <param name="failureReason">Receives the validation failure reason.</param>
        /// <returns><see langword="true"/> when all configured indices are valid.</returns>
        public readonly bool TryToVoxelIndices(out VoxelIndex[] indices, out string failureReason)
        {
            failureReason = string.Empty;
            SwiftList<SerializableVoxelIndex> serializedIndices = Indices;
            if (serializedIndices.Count == 0)
            {
                indices = Array.Empty<VoxelIndex>();
                return true;
            }

            indices = new VoxelIndex[serializedIndices.Count];
            for (int i = 0; i < serializedIndices.Count; i++)
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

        /// <summary>Creates serializable sparse data from runtime indices.</summary>
        /// <param name="indices">The runtime indices to copy.</param>
        /// <returns>The serializable sparse voxel set.</returns>
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
