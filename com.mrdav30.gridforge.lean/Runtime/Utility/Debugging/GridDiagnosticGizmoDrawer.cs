#if UNITY_EDITOR
using FixedMathSharp;
using GridForge.Diagnostics;
using GridForge.Grids;
using GridForge.Grids.Storage;
using System;
using UnityEngine;

namespace GridForge.Utility
{
    /// <summary>
    /// Converts GridForge diagnostic geometry into Unity gizmo primitives.
    /// </summary>
    public static class GridDiagnosticGizmoDrawer
    {
        public static int GetVertexCount(in GridDiagnosticCell cell) =>
            GridDiagnosticGeometry.GetVertexCount(cell.TopologyKind);

        public static int GetEdgeCount(in GridDiagnosticCell cell) =>
            GridDiagnosticGeometry.GetEdgeCount(cell.TopologyKind);

        public static GridDiagnosticCell CreatePhysicalCell(
            GridWorld world,
            VoxelGrid grid,
            Voxel voxel)
        {
            return new GridDiagnosticCell(
                GridDiagnosticCellKind.Physical,
                world.SpawnToken,
                grid.GridIndex,
                grid.SpawnToken,
                voxel.Index,
                voxel.WorldPosition,
                grid.Configuration.TopologyKind,
                grid.StorageKind,
                grid.Configuration.TopologyMetrics,
                ResolveState(voxel),
                voxel.WorldIndex);
        }

        public static int WriteUnityVertices(in GridDiagnosticCell cell, Span<Vector3> vertices)
        {
            int expectedVertexCount = GridDiagnosticGeometry.GetVertexCount(cell.TopologyKind);
            if (expectedVertexCount == 0 || vertices.Length < expectedVertexCount)
                return 0;

            Span<Vector3d> fixedVertices = stackalloc Vector3d[GridDiagnosticGeometry.HexPrismVertexCount];
            int vertexCount = GridDiagnosticGeometry.WriteVertices(in cell, fixedVertices);
            for (int i = 0; i < vertexCount; i++)
                vertices[i] = fixedVertices[i].ToVector3();

            return vertexCount;
        }

        public static void DrawWireCell(in GridDiagnosticCell cell, Color color)
        {
            Span<Vector3d> vertices = stackalloc Vector3d[GridDiagnosticGeometry.HexPrismVertexCount];
            int vertexCount = GridDiagnosticGeometry.WriteVertices(in cell, vertices);
            if (vertexCount == 0)
                return;

            ReadOnlySpan<GridDiagnosticEdge> edges = GridDiagnosticGeometry.GetEdges(cell.TopologyKind);
            Gizmos.color = color;
            for (int i = 0; i < edges.Length; i++)
            {
                GridDiagnosticEdge edge = edges[i];
                if (edge.Start >= vertexCount || edge.End >= vertexCount)
                    continue;

                Gizmos.DrawLine(vertices[edge.Start].ToVector3(), vertices[edge.End].ToVector3());
            }
        }

        private static GridDiagnosticCellState ResolveState(Voxel voxel)
        {
            GridDiagnosticCellState state = GridDiagnosticCellState.None;
            if (!voxel.IsOccupied && !voxel.IsBlocked)
                state |= GridDiagnosticCellState.Empty;

            if (voxel.IsOccupied)
                state |= GridDiagnosticCellState.Occupied;

            if (voxel.IsBlocked)
                state |= GridDiagnosticCellState.Blocked;

            if (voxel.IsBoundaryVoxel)
                state |= GridDiagnosticCellState.Boundary;

            if (voxel.IsPartioned)
                state |= GridDiagnosticCellState.Partitioned;

            return state;
        }
    }
}
#endif
