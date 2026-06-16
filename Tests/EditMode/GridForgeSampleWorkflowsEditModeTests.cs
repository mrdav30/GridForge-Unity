using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GridForge.Unity.Tests.EditMode
{
    public sealed class GridForgeSampleWorkflowsEditModeTests
    {
        private const int RectangularPrism = 0;
        private const int HexPrism = 1;
        private const int Dense = 0;
        private const int Sparse = 1;
        private const int PhysicalAndMissing = 1;

        [TestCase("GridForge.Samples")]
        [TestCase("GridForge.Lean.Samples")]
        public void SceneGridManagerExposesV7WorkflowChoices(string sampleAssemblyName)
        {
            Type workflowType = FindType(sampleAssemblyName, "GridForgeSampleWorkflow");
            Type managerType = FindType(sampleAssemblyName, "SceneGridManager");

            CollectionAssert.AreEqual(
                new[]
                {
                    "DenseRectangular",
                    "DenseHex",
                    "SparseRectangular",
                    "SparseHex",
                    "MixedTopologyDiagnostics"
                },
                Enum.GetNames(workflowType));

            Assert.NotNull(managerType.GetProperty("Workflow", BindingFlags.Instance | BindingFlags.Public));
            Assert.NotNull(managerType.GetMethod("ApplyAuthoringToWorld", BindingFlags.Instance | BindingFlags.Public));
        }

        [TestCase("com.mrdav30.gridforge")]
        [TestCase("com.mrdav30.gridforge.lean")]
        public void WorkflowPrefabsUseV7AuthoringComponents(string packageRoot)
        {
            string prefabRoot = $"Assets/Packages/{packageRoot}/Samples/GridforgeDemo/Prefabs";

            AssertWorkflowConfig(
                $"{prefabRoot}/DenseRectangular.prefab",
                workflowIndex: 0,
                expectedGridCount: 1,
                expectedTopologyKind: RectangularPrism,
                expectedStorageKind: Dense,
                expectedSparseCount: 0);

            AssertWorkflowConfig(
                $"{prefabRoot}/DenseHex.prefab",
                workflowIndex: 1,
                expectedGridCount: 1,
                expectedTopologyKind: HexPrism,
                expectedStorageKind: Dense,
                expectedSparseCount: 0);

            AssertWorkflowConfig(
                $"{prefabRoot}/SparseRectangular.prefab",
                workflowIndex: 2,
                expectedGridCount: 1,
                expectedTopologyKind: RectangularPrism,
                expectedStorageKind: Sparse,
                expectedSparseCount: 5);

            AssertWorkflowConfig(
                $"{prefabRoot}/SparseHex.prefab",
                workflowIndex: 3,
                expectedGridCount: 1,
                expectedTopologyKind: HexPrism,
                expectedStorageKind: Sparse,
                expectedSparseCount: 5);

            AssertWorkflowConfig(
                $"{prefabRoot}/MixedTopologyDiagnostics.prefab",
                workflowIndex: 4,
                expectedGridCount: 4,
                expectedTopologyKind: null,
                expectedStorageKind: null,
                expectedSparseCount: null);
        }

        [TestCase("com.mrdav30.gridforge")]
        [TestCase("com.mrdav30.gridforge.lean")]
        public void MixedDiagnosticsPrefabShowsPhysicalAndMissingSparseCells(string packageRoot)
        {
            GameObject prefab = AssertAssetExists<GameObject>(
                $"Assets/Packages/{packageRoot}/Samples/GridforgeDemo/Prefabs/MixedTopologyDiagnostics.prefab");

            Component debugger = AssertComponent(prefab, "GridForge.Utility.GridDebugger");
            SerializedObject serializedDebugger = new(debugger);

            Assert.IsTrue(serializedDebugger.FindProperty("_showGrid").boolValue);
            Assert.AreEqual(PhysicalAndMissing, serializedDebugger.FindProperty("_addressMode").enumValueIndex);
            Assert.IsTrue(serializedDebugger.FindProperty("_limitQueryBounds").boolValue);
            Assert.AreEqual(512, serializedDebugger.FindProperty("_maxCells").intValue);

            AssertComponent(prefab, "GridForge.Utility.GridTracerTests");
            AssertComponent(prefab, "GridForge.Utility.GridForgeUnityLogger");
        }

        private static void AssertWorkflowConfig(
            string prefabPath,
            int workflowIndex,
            int expectedGridCount,
            int? expectedTopologyKind,
            int? expectedStorageKind,
            int? expectedSparseCount)
        {
            GameObject prefab = AssertAssetExists<GameObject>(prefabPath);
            Component manager = AssertComponent(prefab, "SceneGridManager");
            Component saver = AssertComponent(prefab, "GridForge.Configuration.GridConfigurationSaver");
            AssertComponent(prefab, "GridForge.Unity.GridWorldComponent");

            SerializedObject serializedManager = new(manager);
            Assert.AreEqual(workflowIndex, serializedManager.FindProperty("_workflow").enumValueIndex);

            object configs = GetPropertyValue(saver, "SavedGridConfigurations");
            Assert.AreEqual(expectedGridCount, GetCount(configs));

            if (!expectedTopologyKind.HasValue || !expectedStorageKind.HasValue)
                return;

            object config = GetIndexedValue(configs, 0);
            Assert.AreEqual(expectedTopologyKind.Value, ToEnumIndex(GetPropertyValue(config, "TopologyKind")));
            Assert.AreEqual(expectedStorageKind.Value, ToEnumIndex(GetPropertyValue(config, "StorageKind")));

            object sparseVoxels = GetPropertyValue(config, "ConfiguredSparseVoxels");
            object sparseIndices = GetPropertyValue(sparseVoxels, "Indices");
            Assert.AreEqual(expectedSparseCount.GetValueOrDefault(), GetCount(sparseIndices));
        }

        private static T AssertAssetExists<T>(string assetPath)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            Assert.NotNull(asset, $"Missing sample asset: {assetPath}");
            return asset;
        }

        private static Component AssertComponent(GameObject owner, string fullNameOrName)
        {
            Component component = owner
                .GetComponents<Component>()
                .FirstOrDefault(candidate =>
                    candidate != null &&
                    (candidate.GetType().FullName == fullNameOrName ||
                     candidate.GetType().Name == fullNameOrName));

            Assert.NotNull(component, $"{owner.name} is missing {fullNameOrName}.");
            return component;
        }

        private static Type FindType(string assemblyName, string typeName)
        {
            Assembly assembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(candidate => candidate.GetName().Name == assemblyName);

            Assert.NotNull(assembly, $"Unable to find loaded assembly {assemblyName}.");

            Type type = assembly.GetTypes().FirstOrDefault(candidate => candidate.Name == typeName);
            Assert.NotNull(type, $"Unable to find {typeName} in {assemblyName}.");
            return type;
        }

        private static object GetPropertyValue(object owner, string propertyName)
        {
            Type type = owner.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, $"{type.FullName} should expose {propertyName}.");
            return property.GetValue(owner);
        }

        private static object GetIndexedValue(object owner, int index)
        {
            if (owner is Array array)
                return array.GetValue(index);

            Type type = owner.GetType();
            PropertyInfo indexer = type.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(indexer, $"{type.FullName} should expose an integer indexer.");
            return indexer.GetValue(owner, new object[] { index });
        }

        private static int GetCount(object owner)
        {
            if (owner is Array array)
                return array.Length;

            Type type = owner.GetType();
            PropertyInfo count = type.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(count, $"{type.FullName} should expose Count.");
            return (int)count.GetValue(owner);
        }

        private static int ToEnumIndex(object enumValue)
        {
            return Convert.ToInt32(enumValue);
        }
    }
}
