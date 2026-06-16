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

        [TestCase("com.mrdav30.gridforge", "V7Workflows.unity")]
        [TestCase("com.mrdav30.gridforge.lean", "V7Workflows.Lean.unity")]
        public void PackagesContainV7WorkflowSceneAndPrefabs(string packageRoot, string sceneName)
        {
            string sampleRoot = $"Assets/Packages/{packageRoot}/Samples/GridforgeDemo";

            AssertAssetExists<SceneAsset>($"{sampleRoot}/Scenes/{sceneName}");
            AssertWorkflowPrefabExists(sampleRoot, "DenseRectangular");
            AssertWorkflowPrefabExists(sampleRoot, "DenseHex");
            AssertWorkflowPrefabExists(sampleRoot, "SparseRectangular");
            AssertWorkflowPrefabExists(sampleRoot, "SparseHex");
            AssertWorkflowPrefabExists(sampleRoot, "MixedTopologyDiagnostics");
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

        private static void AssertWorkflowPrefabExists(string sampleRoot, string workflowName)
        {
            AssertAssetExists<GameObject>($"{sampleRoot}/Prefabs/{workflowName}.prefab");
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

            SerializedObject serializedSaver = new(saver);
            SerializedProperty configs = serializedSaver.FindProperty("_savedGridConfigurations");
            Assert.AreEqual(expectedGridCount, configs.arraySize);

            if (!expectedTopologyKind.HasValue || !expectedStorageKind.HasValue)
                return;

            SerializedProperty config = configs.GetArrayElementAtIndex(0);
            Assert.AreEqual(expectedTopologyKind.Value, config.FindPropertyRelative("_topologyKind").enumValueIndex);
            Assert.AreEqual(expectedStorageKind.Value, config.FindPropertyRelative("_storageKind").enumValueIndex);

            SerializedProperty sparseIndices = config
                .FindPropertyRelative("_configuredSparseVoxels")
                .FindPropertyRelative("_indices");
            Assert.AreEqual(expectedSparseCount.GetValueOrDefault(), sparseIndices.arraySize);
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
    }
}
