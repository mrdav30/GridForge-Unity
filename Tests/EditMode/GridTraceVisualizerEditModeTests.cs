using FixedMathSharp;
using GridForge.Configuration;
using GridForge.Diagnostics;
using GridForge.Grids;
using GridForge.Grids.Storage;
using GridForge.Grids.Topology;
using GridForge.Spatial;
using GridForge.Utility;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace GridForge.Unity.Tests.EditMode
{
    public sealed class GridTraceVisualizerEditModeTests
    {
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

                List<Voxel> traced = new();
                int count = visualizer.GetTraceVoxelsInto(
                    world,
                    new Vector3d(0, 99, 0),
                    new Vector3d(4, 99, 4),
                    traced);

                Assert.Greater(count, 0);
                Assert.AreEqual(count, traced.Count);
                Assert.IsTrue(traced.TrueForAll(voxel => voxel.Index.y == 1));
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
            new(
                min,
                max,
                scanCellSize: 2,
                GridTopologyKind.HexPrism,
                GridTopologyMetrics.Hex(Fixed64.One, Fixed64.One, HexOrientation.PointyTop),
                storageKind);
    }
}
