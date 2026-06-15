using FixedMathSharp;
using GridForge.Configuration;
using GridForge.Diagnostics;
using GridForge.Grids;
using GridForge.Grids.Storage;
using GridForge.Grids.Topology;
using GridForge.Spatial;
using GridForge.Utility;
using NUnit.Framework;
using UnityEngine;

namespace GridForge.Unity.Tests.EditMode
{
    public sealed class GridDebuggerDiagnosticsEditModeTests
    {
        [Test]
        public void DenseRectangularDiagnosticVisitorUsesEightVertexCells()
        {
            using GridWorld world = CreateWorldWithGrid(
                RectangularConfig(new Vector3d(0, 0, 0), new Vector3d(1, 0, 1), GridStorageKind.Dense),
                configuredVoxels: null,
                out _);

            GridDiagnosticUnityVisitor visitor = GridDiagnosticUnityVisitor.CreateCountingOnly();
            GridDiagnosticQuery query = new(maxCells: 16);

            GridDiagnosticQueryResult result = GridDiagnostics.VisitCells(
                world,
                in query,
                ref visitor,
                new GridDiagnosticScratch());

            Assert.AreEqual(GridDiagnosticQueryStatus.Completed, result.Status);
            Assert.Greater(visitor.PhysicalCellCount, 0);
            Assert.AreEqual(0, visitor.MissingSparseAddressCellCount);
            Assert.AreEqual(GridDiagnosticGeometry.RectangularPrismVertexCount, visitor.LastVertexCount);
            Assert.AreEqual(GridDiagnosticGeometry.RectangularPrismEdgeCount, visitor.LastEdgeCount);
        }

        [Test]
        public void DenseHexDiagnosticVisitorUsesTwelveVertexCells()
        {
            using GridWorld world = CreateWorldWithGrid(
                HexConfig(new Vector3d(0, 0, 0), new Vector3d(6, 0, 6), GridStorageKind.Dense),
                configuredVoxels: null,
                out _);

            GridDiagnosticUnityVisitor visitor = GridDiagnosticUnityVisitor.CreateCountingOnly();
            GridDiagnosticQuery query = new(
                topologyKind: GridTopologyKind.HexPrism,
                maxCells: 16);

            GridDiagnosticQueryResult result = GridDiagnostics.VisitCells(
                world,
                in query,
                ref visitor,
                new GridDiagnosticScratch());

            Assert.AreEqual(GridDiagnosticQueryStatus.Completed, result.Status);
            Assert.Greater(visitor.PhysicalCellCount, 0);
            Assert.AreEqual(GridDiagnosticGeometry.HexPrismVertexCount, visitor.LastVertexCount);
            Assert.AreEqual(GridDiagnosticGeometry.HexPrismEdgeCount, visitor.LastEdgeCount);
        }

        [Test]
        public void SparsePhysicalOnlyVisitorSkipsMissingAddressCells()
        {
            using GridWorld world = CreateWorldWithGrid(
                RectangularConfig(new Vector3d(0, 0, 0), new Vector3d(3, 0, 3), GridStorageKind.Sparse),
                new[]
                {
                    new VoxelIndex(0, 0, 0),
                    new VoxelIndex(3, 0, 3)
                },
                out _);

            GridDiagnosticUnityVisitor visitor = GridDiagnosticUnityVisitor.CreateCountingOnly();
            GridDiagnosticQuery query = new(
                storageKind: GridStorageKind.Sparse,
                addressMode: GridDiagnosticAddressMode.PhysicalOnly,
                boundsMin: new Vector3d(0, 0, 0),
                boundsMax: new Vector3d(3, 0, 3),
                maxCells: 16);

            GridDiagnosticQueryResult result = GridDiagnostics.VisitCells(
                world,
                in query,
                ref visitor,
                new GridDiagnosticScratch());

            Assert.AreEqual(GridDiagnosticQueryStatus.Completed, result.Status);
            Assert.AreEqual(2, visitor.PhysicalCellCount);
            Assert.AreEqual(0, visitor.MissingSparseAddressCellCount);
        }

        [Test]
        public void SparseMissingOnlyVisitorEmitsMissingDescriptorsThatDoNotResolveToVoxels()
        {
            using GridWorld world = CreateWorldWithGrid(
                RectangularConfig(new Vector3d(0, 0, 0), new Vector3d(2, 0, 2), GridStorageKind.Sparse),
                new[]
                {
                    new VoxelIndex(0, 0, 0)
                },
                out _);

            GridDiagnosticUnityVisitor visitor = GridDiagnosticUnityVisitor.CreateCountingOnly();
            GridDiagnosticQuery query = new(
                storageKind: GridStorageKind.Sparse,
                addressMode: GridDiagnosticAddressMode.MissingOnly,
                boundsMin: new Vector3d(0, 0, 0),
                boundsMax: new Vector3d(2, 0, 2),
                maxCells: 16);

            GridDiagnosticQueryResult result = GridDiagnostics.VisitCells(
                world,
                in query,
                ref visitor,
                new GridDiagnosticScratch());

            Assert.AreEqual(GridDiagnosticQueryStatus.Completed, result.Status);
            Assert.AreEqual(0, visitor.PhysicalCellCount);
            Assert.Greater(visitor.MissingSparseAddressCellCount, 0);
            GridDiagnosticCell lastCell = visitor.LastCell;
            Assert.AreEqual(GridDiagnosticCellKind.MissingSparseAddress, lastCell.Kind);
            Assert.IsFalse(GridDiagnostics.TryResolvePhysicalCell(world, in lastCell, out _, out _));
        }

        [Test]
        public void MaxCellOverflowSurfacesQueryStatus()
        {
            using GridWorld world = CreateWorldWithGrid(
                RectangularConfig(new Vector3d(0, 0, 0), new Vector3d(3, 0, 3), GridStorageKind.Dense),
                configuredVoxels: null,
                out _);

            GridDiagnosticUnityVisitor visitor = GridDiagnosticUnityVisitor.CreateCountingOnly();
            GridDiagnosticQuery query = new(maxCells: 1);

            GridDiagnosticQueryResult result = GridDiagnostics.VisitCells(
                world,
                in query,
                ref visitor,
                new GridDiagnosticScratch());

            Assert.AreEqual(GridDiagnosticQueryStatus.MaxCellsExceeded, result.Status);
            Assert.AreEqual(1, visitor.VisitedCellCount);
            Assert.Greater(result.SkippedCellCount, 0);
        }

        [Test]
        public void DebuggerResolvesPhysicalDiagnosticsAndRejectsMissingSparseDescriptors()
        {
            GameObject owner = new("Grid debugger diagnostics test");

            try
            {
                GridWorldComponent worldComponent = owner.AddComponent<GridWorldComponent>();
                GridWorld world = worldComponent.RebuildWorld(16);
                Assert.IsTrue(world.TryAddGrid(
                    RectangularConfig(new Vector3d(0, 0, 0), new Vector3d(2, 0, 2), GridStorageKind.Sparse),
                    new[] { new VoxelIndex(0, 0, 0) },
                    out _));

                GridDebugger debugger = owner.AddComponent<GridDebugger>();
                GridDiagnosticUnityVisitor physicalVisitor = GridDiagnosticUnityVisitor.CreateCountingOnly();
                GridDiagnosticQuery physicalQuery = new(
                    storageKind: GridStorageKind.Sparse,
                    addressMode: GridDiagnosticAddressMode.PhysicalOnly,
                    boundsMin: new Vector3d(0, 0, 0),
                    boundsMax: new Vector3d(2, 0, 2),
                    maxCells: 16);
                GridDiagnostics.VisitCells(world, in physicalQuery, ref physicalVisitor, new GridDiagnosticScratch());

                GridDiagnosticUnityVisitor missingVisitor = GridDiagnosticUnityVisitor.CreateCountingOnly();
                GridDiagnosticQuery missingQuery = new(
                    storageKind: GridStorageKind.Sparse,
                    addressMode: GridDiagnosticAddressMode.MissingOnly,
                    boundsMin: new Vector3d(0, 0, 0),
                    boundsMax: new Vector3d(2, 0, 2),
                    maxCells: 16);
                GridDiagnostics.VisitCells(world, in missingQuery, ref missingVisitor, new GridDiagnosticScratch());

                GridDiagnosticCell physicalCell = physicalVisitor.LastCell;
                GridDiagnosticCell missingCell = missingVisitor.LastCell;

                Assert.IsTrue(debugger.TryResolveDiagnosticPhysicalCell(in physicalCell, out Voxel resolvedVoxel));
                Assert.NotNull(resolvedVoxel);
                Assert.IsFalse(debugger.TryResolveDiagnosticPhysicalCell(in missingCell, out _));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static GridWorld CreateWorldWithGrid(
            GridConfiguration config,
            VoxelIndex[] configuredVoxels,
            out ushort gridIndex)
        {
            GridWorld world = new();
            bool added = configuredVoxels == null
                ? world.TryAddGrid(config, out gridIndex)
                : world.TryAddGrid(config, configuredVoxels, out gridIndex);

            Assert.IsTrue(added);
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
