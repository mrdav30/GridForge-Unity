using FixedMathSharp;
using GridForge.Configuration;
using GridForge.Diagnostics;
using GridForge.Grids;
using GridForge.Grids.Storage;
using GridForge.Grids.Topology;
using GridForge.Spatial;
using GridForge.Utility;
using NUnit.Framework;
using SwiftCollections;
using UnityEngine;

namespace GridForge.Unity.Tests.EditMode
{
    public sealed class GridTraceVisualizerEditModeTests
    {
        private static readonly VoxelIndex[] RectangularSparseIndices =
        {
            new(0, 0, 0),
            new(1, 0, 0),
            new(1, 0, 1),
            new(2, 0, 1),
            new(3, 0, 2)
        };

        private static readonly VoxelIndex[] HexSparseIndices =
        {
            new(0, 0, 0),
            new(1, 0, 0),
            new(0, 0, 1),
            new(1, 0, 1),
            new(2, 0, 0)
        };

        [Test]
        public void XzLayerTraceUsesVector2dOverloadWithExplicitLayerY()
        {
            using GridWorld world = CreateWorldWithGrid(
                RectangularConfig(new Vector3d(0, 0, 0), new Vector3d(4, 1, 4), GridStorageKind.Dense),
                configuredVoxels: null,
                out _);
            GameObject owner = new("Grid trace visualizer edit mode test");

            try
            {
                GridTracerTests visualizer = owner.AddComponent<GridTracerTests>();
                visualizer.ConfigureTraceMode(GridTraceMode.XzLayer, layerY: Fixed64.One);

                SwiftList<Voxel> traced = new();
                int count = visualizer.GetTraceVoxelsInto(
                    world,
                    new Vector3d(0, 99, 0),
                    new Vector3d(4, 99, 4),
                    traced);

                Assert.Greater(count, 0);
                Assert.AreEqual(count, traced.Count);
                for (int i = 0; i < traced.Count; i++)
                    Assert.AreEqual(1, traced[i].Index.y);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void XzLayerTraceSkipsMixedTopologyGridsOutsideLineSegment()
        {
            using GridWorld world = new();
            AddGrid(
                world,
                RectangularConfig(new Vector3d(-12, 0, -6), new Vector3d(-6, 1, 0), GridStorageKind.Dense),
                null,
                out ushort denseRectangularIndex);
            AddGrid(
                world,
                HexConfig(new Vector3d(0, 0, -6), new Vector3d(8, 1, 2), GridStorageKind.Dense),
                null,
                out ushort denseHexIndex);
            AddGrid(
                world,
                RectangularConfig(new Vector3d(-12, 0, 6), new Vector3d(-6, 1, 12), GridStorageKind.Sparse),
                RectangularSparseIndices,
                out ushort sparseRectangularIndex);
            AddGrid(
                world,
                HexConfig(new Vector3d(0, 0, 6), new Vector3d(8, 1, 14), GridStorageKind.Sparse),
                HexSparseIndices,
                out _);
            GameObject owner = new("Grid trace visualizer edit mode test");

            try
            {
                GridTracerTests visualizer = owner.AddComponent<GridTracerTests>();
                visualizer.ConfigureTraceMode(GridTraceMode.XzLayer);

                SwiftList<Voxel> traced = new();
                visualizer.GetTraceVoxelsInto(
                    world,
                    new Vector3d(-11, 0, -5),
                    new Vector3d(7, 0, 13),
                    traced);

                Assert.IsTrue(ContainsGrid(traced, denseRectangularIndex));
                Assert.IsFalse(ContainsGrid(traced, denseHexIndex));
                Assert.IsFalse(ContainsGrid(traced, sparseRectangularIndex));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void XzLayerTraceContinuesSegmentWhenEnteringLaterHexGrid()
        {
            GridTopologyMetrics metrics = GridTopologyMetrics.Hex(
                new Fixed64(2),
                Fixed64.One,
                HexOrientation.PointyTop);
            GridConfiguration rectangularConfiguration = RectangularConfig(
                new Vector3d(0, 0, -1),
                new Vector3d(4, 0, 3),
                GridStorageKind.Dense);
            Vector3d hexBoundsMin = new(8, 0, 0);
            Vector3d hexBoundsMax = hexBoundsMin + PointyTopHexOffset(new VoxelIndex(3, 0, 3), metrics);
            GridConfiguration hexConfiguration = HexConfig(
                hexBoundsMin,
                hexBoundsMax,
                GridStorageKind.Dense,
                metrics);
            using GridWorld world = new();
            AddGrid(world, rectangularConfiguration, null, out _);
            AddGrid(world, hexConfiguration, null, out ushort hexGridIndex);
            VoxelGrid hexGrid = world.ActiveGrids[hexGridIndex];
            GameObject owner = new("Grid trace visualizer edit mode test");

            try
            {
                Assert.IsTrue(hexGrid.TryGetVoxel(
                    new VoxelIndex(hexGrid.Width - 1, 0, hexGrid.Length - 1),
                    out Voxel endVoxel));

                Vector3d start = rectangularConfiguration.BoundsMin;
                Vector3d end = endVoxel.WorldPosition;
                Fixed64 entryT = (hexGrid.BoundsMin.X - start.X) / (end.X - start.X);
                Vector3d expectedHexEntry = new(
                    hexGrid.BoundsMin.X,
                    start.Y + (end.Y - start.Y) * entryT,
                    start.Z + (end.Z - start.Z) * entryT);

                Assert.IsTrue(hexGrid.TryGetClosestVoxel(expectedHexEntry, out Voxel expectedFirstHexVoxel));

                GridTracerTests visualizer = owner.AddComponent<GridTracerTests>();
                visualizer.ConfigureTraceMode(GridTraceMode.XzLayer);

                SwiftList<Voxel> traced = new();
                visualizer.GetTraceVoxelsInto(world, start, end, traced);

                SwiftList<Voxel> tracedHexVoxels = CollectGridVoxels(traced, hexGridIndex);
                Assert.Greater(tracedHexVoxels.Count, 0);
                Assert.AreEqual(expectedFirstHexVoxel.Index, tracedHexVoxels[0].Index);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void HexTraceIncludesBoundaryVoxelWhenEndFallsPastGridEdge()
        {
            GridTopologyMetrics metrics = GridTopologyMetrics.Hex(
                new Fixed64(2),
                Fixed64.One,
                HexOrientation.PointyTop);
            VoxelIndex originIndex = new(0, 0, 0);
            GridConfiguration configuration = HexConfig(
                Vector3d.Zero,
                new Vector3d(12, 0, 12),
                GridStorageKind.Dense,
                metrics);
            using GridWorld world = CreateWorldWithGrid(
                configuration,
                null,
                out VoxelGrid grid);
            GameObject owner = new("Grid trace visualizer edit mode test");

            try
            {
                VoxelIndex boundaryIndex = new(grid.Width - 1, 0, 0);
                Assert.IsTrue(grid.TryGetVoxel(originIndex, out Voxel originVoxel));
                Assert.IsTrue(grid.TryGetVoxel(boundaryIndex, out Voxel boundaryVoxel));

                GridTracerTests visualizer = owner.AddComponent<GridTracerTests>();
                visualizer.ConfigureTraceMode(GridTraceMode.XzLayer);

                Vector3d end = boundaryVoxel.WorldPosition + (boundaryVoxel.WorldPosition - originVoxel.WorldPosition);
                SwiftList<Voxel> traced = new();
                visualizer.GetTraceVoxelsInto(
                    world,
                    originVoxel.WorldPosition,
                    end,
                    traced);

                Assert.IsTrue(ContainsVoxel(traced, originIndex));
                Assert.IsTrue(ContainsVoxel(traced, boundaryIndex));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void HexTraceDiagnosticsUseHexGeometry()
        {
            using GridWorld world = CreateWorldWithGrid(
                HexConfig(new Vector3d(0, 0, 0), new Vector3d(12, 0, 12), GridStorageKind.Dense),
                configuredVoxels: null,
                out VoxelGrid grid);
            GameObject owner = new("Grid trace visualizer edit mode test");

            try
            {
                Assert.IsTrue(grid.TryGetVoxel(new VoxelIndex(0, 0, 0), out Voxel startVoxel));
                Assert.IsTrue(grid.TryGetVoxel(new VoxelIndex(1, 0, 0), out Voxel endVoxel));

                GridTracerTests visualizer = owner.AddComponent<GridTracerTests>();
                visualizer.ConfigureTraceMode(GridTraceMode.World3D);

                Assert.IsTrue(visualizer.TryGetFirstTraceDiagnosticCell(
                    world,
                    startVoxel.WorldPosition,
                    endVoxel.WorldPosition,
                    out GridDiagnosticCell cell));

                Assert.AreEqual(GridTopologyKind.HexPrism, cell.TopologyKind);
                Assert.AreEqual(GridDiagnosticGeometry.HexPrismVertexCount, GridDiagnosticGizmoDrawer.GetVertexCount(in cell));
                Assert.AreEqual(GridDiagnosticGeometry.HexPrismEdgeCount, GridDiagnosticGizmoDrawer.GetEdgeCount(in cell));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static GridWorld CreateWorldWithGrid(
            GridConfiguration config,
            VoxelIndex[] configuredVoxels,
            out VoxelGrid grid)
        {
            GridWorld world = new();
            bool added = configuredVoxels == null
                ? world.TryAddGrid(config, out ushort gridIndex)
                : world.TryAddGrid(config, configuredVoxels, out gridIndex);

            Assert.IsTrue(added);
            grid = world.ActiveGrids[gridIndex];
            return world;
        }

        private static void AddGrid(
            GridWorld world,
            GridConfiguration config,
            VoxelIndex[] configuredVoxels,
            out ushort gridIndex)
        {
            bool added = configuredVoxels == null
                ? world.TryAddGrid(config, out gridIndex)
                : world.TryAddGrid(config, configuredVoxels, out gridIndex);

            Assert.IsTrue(added);
        }

        private static GridConfiguration RectangularConfig(
            Vector3d min,
            Vector3d max,
            GridStorageKind storageKind) =>
            new(
                min,
                max,
                scanCellSize: 2,
                GridTopologyKind.RectangularPrism,
                GridTopologyMetrics.Rectangular(Fixed64.One, Fixed64.One, Fixed64.One),
                storageKind);

        private static GridConfiguration HexConfig(
            Vector3d min,
            Vector3d max,
            GridStorageKind storageKind) =>
            HexConfig(
                min,
                max,
                storageKind,
                GridTopologyMetrics.Hex(Fixed64.One, Fixed64.One, HexOrientation.PointyTop));

        private static GridConfiguration HexConfig(
            Vector3d min,
            Vector3d max,
            GridStorageKind storageKind,
            GridTopologyMetrics metrics) =>
            new(
                min,
                max,
                scanCellSize: 2,
                GridTopologyKind.HexPrism,
                metrics,
                storageKind);

        private static Vector3d PointyTopHexOffset(VoxelIndex index, GridTopologyMetrics metrics)
        {
            Fixed64 sqrt3 = Fixed64.FromRaw(7439101574L);
            Fixed64 q = new(index.x);
            Fixed64 r = new(index.z);

            return new Vector3d(
                metrics.CellRadius * sqrt3 * (q + r * Fixed64.Half),
                index.y * metrics.LayerHeight,
                metrics.CellRadius * Fixed64.Three * Fixed64.Half * r);
        }

        private static bool ContainsGrid(SwiftList<Voxel> voxels, ushort gridIndex)
        {
            for (int i = 0; i < voxels.Count; i++)
            {
                if (voxels[i].WorldIndex.GridIndex == gridIndex)
                    return true;
            }

            return false;
        }

        private static bool ContainsVoxel(SwiftList<Voxel> voxels, VoxelIndex index)
        {
            for (int i = 0; i < voxels.Count; i++)
            {
                if (voxels[i].Index == index)
                    return true;
            }

            return false;
        }

        private static SwiftList<Voxel> CollectGridVoxels(SwiftList<Voxel> voxels, ushort gridIndex)
        {
            SwiftList<Voxel> result = new();
            for (int i = 0; i < voxels.Count; i++)
            {
                Voxel voxel = voxels[i];
                if (voxel.WorldIndex.GridIndex == gridIndex)
                    result.Add(voxel);
            }

            return result;
        }
    }
}
