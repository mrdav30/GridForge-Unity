using FixedMathSharp;
using GridForge.Configuration;
using GridForge.Grids;
using GridForge.Grids.Storage;
using GridForge.Grids.Topology;
using GridForge.Spatial;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

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
        public void SavedGridConfigurationsPersistFixedAuthoringValuesThroughUnitySerialization()
        {
            const string tempRoot = "Assets/Packages/Temp";
            const string testFolder = tempRoot + "/GridForgeSerializationTests";
            const string assetPath = testFolder + "/GridConfigurationSerializationRoundTrip.prefab";

            GameObject owner = new("Grid configuration serialization test");
            bool createdTempRoot = false;

            try
            {
                GridConfigurationSaver saver = owner.AddComponent<GridConfigurationSaver>();
                saver.Save(new SerializableGridConfiguration(
                    new Vector3d(-2, 1, 3),
                    new Vector3d(4, 5, 6),
                    scanCellSize: 7,
                    GridTopologyKind.HexPrism,
                    SerializableGridTopologyMetrics.Hex(new Fixed64(2), new Fixed64(3), HexOrientation.FlatTop),
                    GridStorageKind.Sparse,
                    new SerializableSparseVoxelSet(new[]
                    {
                        new SerializableVoxelIndex(0, 1, 2)
                    })));

                if (!AssetDatabase.IsValidFolder(tempRoot))
                {
                    AssetDatabase.CreateFolder("Assets/Packages", "Temp");
                    createdTempRoot = true;
                }

                AssetDatabase.CreateFolder(tempRoot, "GridForgeSerializationTests");
                PrefabUtility.SaveAsPrefabAsset(owner, assetPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                GameObject loaded = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                GridConfigurationSaver loadedSaver = loaded.GetComponent<GridConfigurationSaver>();
                SerializableGridConfiguration loadedConfig = loadedSaver.SavedGridConfigurations[0];

                Assert.AreEqual(new Vector3d(-2, 1, 3), loadedConfig.BoundsMin);
                Assert.AreEqual(new Vector3d(4, 5, 6), loadedConfig.BoundsMax);
                Assert.IsTrue(loadedConfig.TryToGridConfiguration(out GridConfiguration config, out string failure), failure);
                Assert.AreEqual(7, config.ScanCellSize);
                Assert.AreEqual(GridTopologyKind.HexPrism, config.TopologyKind);
                Assert.AreEqual(GridStorageKind.Sparse, config.StorageKind);
                Assert.AreEqual(new Fixed64(2), config.TopologyMetrics.CellRadius);
                Assert.AreEqual(new Fixed64(3), config.TopologyMetrics.LayerHeight);
                Assert.AreEqual(HexOrientation.FlatTop, config.TopologyMetrics.HexOrientation);
                Assert.IsTrue(loadedConfig.TryGetConfiguredSparseVoxels(out VoxelIndex[] voxels, out string sparseFailure), sparseFailure);
                Assert.AreEqual(new VoxelIndex(0, 1, 2), voxels[0]);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.DeleteAsset(testFolder);

                if (createdTempRoot)
                    AssetDatabase.DeleteAsset(tempRoot);
            }
        }

        [Test]
        public void FixedMathSharpAuthoringValuesPersistThroughUnitySerialization()
        {
            const string tempRoot = "Assets/Packages/Temp";
            const string testFolder = tempRoot + "/FixedMathSharpSerializationTests";
            const string assetPath = testFolder + "/FixedMathSharpSerializationRoundTrip.asset";

            FixedMathSharpSerializationProbe probe = ScriptableObject.CreateInstance<FixedMathSharpSerializationProbe>();
            bool createdTempRoot = false;

            try
            {
                probe.Configure(
                    new Fixed64(9),
                    new Vector2d(2, 3),
                    new Vector3d(-4, 5, 6),
                    new FixedMathSharpNestedSerializationProbe(
                        new Fixed64(7),
                        new Vector3d(8, -9, 10)));

                if (!AssetDatabase.IsValidFolder(tempRoot))
                {
                    AssetDatabase.CreateFolder("Assets/Packages", "Temp");
                    createdTempRoot = true;
                }

                AssetDatabase.CreateFolder(tempRoot, "FixedMathSharpSerializationTests");
                AssetDatabase.CreateAsset(probe, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                FixedMathSharpSerializationProbe loadedProbe = AssetDatabase.LoadAssetAtPath<FixedMathSharpSerializationProbe>(assetPath);

                Assert.AreEqual(new Fixed64(9), loadedProbe.Scalar);
                Assert.AreEqual(new Vector2d(2, 3), loadedProbe.Plane);
                Assert.AreEqual(new Vector3d(-4, 5, 6), loadedProbe.Space);
                Assert.AreEqual(new Fixed64(7), loadedProbe.Nested[0].Scalar);
                Assert.AreEqual(new Vector3d(8, -9, 10), loadedProbe.Nested[0].Space);
            }
            finally
            {
                if (probe != null && !AssetDatabase.Contains(probe))
                    Object.DestroyImmediate(probe);

                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.DeleteAsset(testFolder);

                if (createdTempRoot)
                    AssetDatabase.DeleteAsset(tempRoot);
            }
        }

        [Test]
        public void GridConfigurationAuthoringStoresFixedMathSharpTypesDirectly()
        {
            AssertSerializedFieldType(typeof(SerializableGridConfiguration), "_boundsMin", typeof(Vector3d));
            AssertSerializedFieldType(typeof(SerializableGridConfiguration), "_boundsMax", typeof(Vector3d));
            AssertSerializedFieldType(typeof(SerializableGridTopologyMetrics), "_rectangularCellWidth", typeof(Fixed64));
            AssertSerializedFieldType(typeof(SerializableGridTopologyMetrics), "_rectangularLayerHeight", typeof(Fixed64));
            AssertSerializedFieldType(typeof(SerializableGridTopologyMetrics), "_rectangularCellLength", typeof(Fixed64));
            AssertSerializedFieldType(typeof(SerializableGridTopologyMetrics), "_hexRadius", typeof(Fixed64));
            AssertSerializedFieldType(typeof(SerializableGridTopologyMetrics), "_hexLayerHeight", typeof(Fixed64));

            AssertNoRawFixedAuthoringFields(typeof(SerializableGridConfiguration));
            AssertNoRawFixedAuthoringFields(typeof(SerializableGridTopologyMetrics));
        }

        [Test]
        public void GridConfigurationSaverInspectorShowsOnlyRelevantAuthoringSections()
        {
            Type visibilityType = RequireEditorType("GridForge.Configuration.Editor.GridConfigurationEditorVisibility, GridForge.Editor");

            Assert.IsTrue(InvokeEditorPolicy(visibilityType, "ShouldDrawRectangularMetrics", GridTopologyKind.RectangularPrism));
            Assert.IsFalse(InvokeEditorPolicy(visibilityType, "ShouldDrawHexMetrics", GridTopologyKind.RectangularPrism));
            Assert.IsTrue(InvokeEditorPolicy(visibilityType, "ShouldDrawHexMetrics", GridTopologyKind.HexPrism));
            Assert.IsFalse(InvokeEditorPolicy(visibilityType, "ShouldDrawRectangularMetrics", GridTopologyKind.HexPrism));
            Assert.IsFalse(InvokeEditorPolicy(visibilityType, "ShouldDrawSparseVoxels", GridStorageKind.Dense));
            Assert.IsTrue(InvokeEditorPolicy(visibilityType, "ShouldDrawSparseVoxels", GridStorageKind.Sparse));
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

        private static Type RequireEditorType(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName);
            Assert.IsNotNull(type, $"{assemblyQualifiedName} should exist for editor authoring policy tests.");
            return type;
        }

        private static bool InvokeEditorPolicy(Type type, string methodName, object value)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"{type.Name}.{methodName} should exist.");
            return (bool)method.Invoke(null, new[] { value });
        }

        private static void AssertSerializedFieldType(Type type, string fieldName, Type expectedType)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{type.Name} should serialize {fieldName} directly.");
            Assert.AreEqual(expectedType, field.FieldType, $"{type.Name}.{fieldName} should use FixedMathSharp storage directly.");
            Assert.IsNotNull(field.GetCustomAttribute<SerializeField>(), $"{type.Name}.{fieldName} should be serialized.");
        }

        private static void AssertNoRawFixedAuthoringFields(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                StringAssert.DoesNotEndWith(
                    "Raw",
                    field.Name,
                    $"{type.Name}.{field.Name} should not preserve GridForge-owned FixedMathSharp raw storage.");
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

    public sealed class FixedMathSharpSerializationProbe : ScriptableObject
    {
        [SerializeField] private Fixed64 _scalar;
        [SerializeField] private Vector2d _plane;
        [SerializeField] private Vector3d _space;
        [SerializeField] private List<FixedMathSharpNestedSerializationProbe> _nested = new();

        public Fixed64 Scalar => _scalar;
        public Vector2d Plane => _plane;
        public Vector3d Space => _space;
        public IReadOnlyList<FixedMathSharpNestedSerializationProbe> Nested => _nested;

        public void Configure(
            Fixed64 scalar,
            Vector2d plane,
            Vector3d space,
            FixedMathSharpNestedSerializationProbe nested)
        {
            _scalar = scalar;
            _plane = plane;
            _space = space;
            _nested.Clear();
            _nested.Add(nested);
        }
    }

    [Serializable]
    public struct FixedMathSharpNestedSerializationProbe
    {
        [SerializeField] private Fixed64 _scalar;
        [SerializeField] private Vector3d _space;

        public FixedMathSharpNestedSerializationProbe(Fixed64 scalar, Vector3d space)
        {
            _scalar = scalar;
            _space = space;
        }

        public readonly Fixed64 Scalar => _scalar;
        public readonly Vector3d Space => _space;
    }
}
