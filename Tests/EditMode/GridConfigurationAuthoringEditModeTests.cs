using FixedMathSharp;
using GridForge.Configuration;
using GridForge.Grids;
using GridForge.Grids.Storage;
using GridForge.Grids.Topology;
using GridForge.Spatial;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace GridForge.Unity.Tests.EditMode
{
    public sealed class GridConfigurationAuthoringEditModeTests
    {
        [Test]
        public void DefaultAuthoringCreatesDenseRectangularConfiguration()
        {
            SerializableGridConfiguration serialized = new(
                new Vector3d(0, 0, 0),
                new Vector3d(2, 0, 2),
                scanCellSize: 0);

            Assert.IsTrue(serialized.TryToGridConfiguration(out GridConfiguration config, out string failure), failure);

            Assert.AreEqual(GridConfiguration.DefaultScanCellSize, config.ScanCellSize);
            Assert.AreEqual(GridTopologyKind.RectangularPrism, config.TopologyKind);
            Assert.AreEqual(GridStorageKind.Dense, config.StorageKind);
            Assert.AreEqual(Fixed64.One, config.TopologyMetrics.CellWidth);
            Assert.AreEqual(Fixed64.One, config.TopologyMetrics.LayerHeight);
            Assert.AreEqual(Fixed64.One, config.TopologyMetrics.CellLength);
        }

        [Test]
        public void RectangularMetricsRoundTripThroughGridConfiguration()
        {
            SerializableGridConfiguration serialized = new(
                new Vector3d(0, 0, 0),
                new Vector3d(8, 6, 12),
                scanCellSize: 4,
                GridTopologyKind.RectangularPrism,
                SerializableGridTopologyMetrics.Rectangular(new Fixed64(2), new Fixed64(3), new Fixed64(4)),
                GridStorageKind.Dense,
                SerializableSparseVoxelSet.Empty);

            Assert.IsTrue(serialized.TryToGridConfiguration(out GridConfiguration config, out string failure), failure);

            SerializableGridConfiguration roundTrip = SerializableGridConfiguration.FromGridConfiguration(config);
            Assert.IsTrue(roundTrip.TryToGridConfiguration(out GridConfiguration result, out string roundTripFailure), roundTripFailure);

            Assert.AreEqual(GridTopologyKind.RectangularPrism, result.TopologyKind);
            Assert.AreEqual(new Fixed64(2), result.TopologyMetrics.CellWidth);
            Assert.AreEqual(new Fixed64(3), result.TopologyMetrics.LayerHeight);
            Assert.AreEqual(new Fixed64(4), result.TopologyMetrics.CellLength);
        }

        [TestCase(HexOrientation.PointyTop)]
        [TestCase(HexOrientation.FlatTop)]
        public void HexMetricsRoundTripThroughGridConfiguration(HexOrientation orientation)
        {
            SerializableGridConfiguration serialized = new(
                new Vector3d(0, 0, 0),
                new Vector3d(24, 2, 16),
                scanCellSize: 4,
                GridTopologyKind.HexPrism,
                SerializableGridTopologyMetrics.Hex(new Fixed64(2), new Fixed64(3), orientation),
                GridStorageKind.Dense,
                SerializableSparseVoxelSet.Empty);

            Assert.IsTrue(serialized.TryToGridConfiguration(out GridConfiguration config, out string failure), failure);

            SerializableGridConfiguration roundTrip = SerializableGridConfiguration.FromGridConfiguration(config);
            Assert.IsTrue(roundTrip.TryToGridConfiguration(out GridConfiguration result, out string roundTripFailure), roundTripFailure);

            Assert.AreEqual(GridTopologyKind.HexPrism, result.TopologyKind);
            Assert.AreEqual(new Fixed64(2), result.TopologyMetrics.CellRadius);
            Assert.AreEqual(new Fixed64(3), result.TopologyMetrics.LayerHeight);
            Assert.AreEqual(orientation, result.TopologyMetrics.HexOrientation);
        }

        [Test]
        public void SparseRectangularAuthoringRegistersConfiguredIndices()
        {
            using GridWorld world = new();
            GameObject owner = new("Sparse rectangular authoring test");

            try
            {
                GridConfigurationSaver saver = owner.AddComponent<GridConfigurationSaver>();
                saver.Save(new SerializableGridConfiguration(
                    new Vector3d(0, 0, 0),
                    new Vector3d(3, 0, 3),
                    scanCellSize: 2,
                    GridTopologyKind.RectangularPrism,
                    SerializableGridTopologyMetrics.Rectangular(Fixed64.One, Fixed64.One, Fixed64.One),
                    GridStorageKind.Sparse,
                    new SerializableSparseVoxelSet(new[]
                    {
                        new SerializableVoxelIndex(0, 0, 0),
                        new SerializableVoxelIndex(3, 0, 3)
                    })));

                saver.EarlyApply(world);

                Assert.AreEqual(1, world.ActiveGrids.Count);
                VoxelGrid grid = world.ActiveGrids[0];
                Assert.AreEqual(GridStorageKind.Sparse, grid.StorageKind);
                Assert.AreEqual(2, grid.ConfiguredVoxelCount);
                Assert.IsTrue(grid.ContainsVoxel(new VoxelIndex(0, 0, 0)));
                Assert.IsTrue(grid.ContainsVoxel(new VoxelIndex(3, 0, 3)));
                Assert.IsFalse(grid.ContainsVoxel(new VoxelIndex(1, 0, 1)));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void SparseHexAuthoringRegistersAxialConfiguredIndices()
        {
            using GridWorld world = new();
            GameObject owner = new("Sparse hex authoring test");

            try
            {
                GridConfigurationSaver saver = owner.AddComponent<GridConfigurationSaver>();
                saver.Save(new SerializableGridConfiguration(
                    new Vector3d(0, 0, 0),
                    new Vector3d(24, 0, 16),
                    scanCellSize: 4,
                    GridTopologyKind.HexPrism,
                    SerializableGridTopologyMetrics.Hex(new Fixed64(2), Fixed64.One, HexOrientation.PointyTop),
                    GridStorageKind.Sparse,
                    new SerializableSparseVoxelSet(new[]
                    {
                        new SerializableVoxelIndex(0, 0, 0),
                        new SerializableVoxelIndex(1, 0, 0)
                    })));

                saver.EarlyApply(world);

                Assert.AreEqual(1, world.ActiveGrids.Count);
                VoxelGrid grid = world.ActiveGrids[0];
                Assert.AreEqual(GridTopologyKind.HexPrism, grid.Configuration.TopologyKind);
                Assert.AreEqual(GridStorageKind.Sparse, grid.StorageKind);
                Assert.AreEqual(2, grid.ConfiguredVoxelCount);
                Assert.IsTrue(grid.ContainsVoxel(new VoxelIndex(0, 0, 0)));
                Assert.IsTrue(grid.ContainsVoxel(new VoxelIndex(1, 0, 0)));
                Assert.IsFalse(grid.ContainsVoxel(new VoxelIndex(2, 0, 0)));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void AuthoringComponentsDoNotRetainLegacyVoxelSizeCompatibilityFields()
        {
            AssertNoLegacyVoxelSizeFields(typeof(GridConfigurationSaver));
            AssertNoLegacyVoxelSizeFields(typeof(GridWorldComponent));
        }

        [Test]
        public void InvalidMetricsAreRejectedBeforeWorldRegistration()
        {
            using GridWorld world = new();
            GameObject owner = new("Invalid metrics authoring test");

            try
            {
                GridConfigurationSaver saver = owner.AddComponent<GridConfigurationSaver>();
                saver.Save(new SerializableGridConfiguration(
                    new Vector3d(0, 0, 0),
                    new Vector3d(2, 0, 2),
                    scanCellSize: 8,
                    GridTopologyKind.RectangularPrism,
                    SerializableGridTopologyMetrics.Rectangular(new Fixed64(-1), Fixed64.One, Fixed64.One),
                    GridStorageKind.Dense,
                    SerializableSparseVoxelSet.Empty));

                LogAssert.Expect(LogType.Warning, "Invalid Grid Configuration: Rectangular-prism topology requires positive cell width, layer height, and cell length.");

                saver.EarlyApply(world);

                Assert.AreEqual(0, world.ActiveGrids.Count);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static void AssertNoLegacyVoxelSizeFields(System.Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                Assert.IsFalse(
                    field.Name.ToLowerInvariant().Contains("legacy"),
                    $"{type.Name}.{field.Name} should not preserve legacy serialized compatibility state.");

                foreach (object attribute in field.GetCustomAttributes(inherit: false))
                {
                    Assert.AreNotEqual(
                        "FormerlySerializedAsAttribute",
                        attribute.GetType().Name,
                        $"{type.Name}.{field.Name} should not migrate old serialized field names.");
                }
            }
        }
    }
}
