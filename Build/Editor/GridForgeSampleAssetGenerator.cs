#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GridForge.Build.Editor
{
    /// <summary>
    /// Generates GridForge sample prefabs and scenes through Unity serialization APIs.
    /// </summary>
    public static class GridForgeSampleAssetGenerator
    {
        private const int RectangularPrism = 0;
        private const int HexPrism = 1;
        private const int Dense = 0;
        private const int Sparse = 1;
        private const int PointyTop = 1;
        private const int XzLayer = 1;
        private const int BoundsBlocker = 1;
        private const int TransformBlockArea = 1;
        private const int PhysicalAndMissing = 1;
        private const int SpatialGridCellSize = 16;
        private static readonly Regex TrailingWhitespacePattern = new(@"[ \t]+(\r?\n)", RegexOptions.Compiled);

        private static readonly VoxelIndexDefinition[] RectangularSparseIndices =
        {
            new(0, 0, 0),
            new(1, 0, 0),
            new(1, 0, 1),
            new(2, 0, 1),
            new(3, 0, 2)
        };

        private static readonly VoxelIndexDefinition[] HexSparseIndices =
        {
            new(0, 0, 0),
            new(1, 0, 0),
            new(0, 0, 1),
            new(1, 0, 1),
            new(2, 0, 0)
        };

        private static readonly PackageVariant[] PackageVariants =
        {
            new(
                "Assets/Packages/com.mrdav30.gridforge",
                "GridForge.Runtime",
                "GridForge.Samples",
                "DemoScene.unity",
                "V7Workflows.unity"),
            new(
                "Assets/Packages/com.mrdav30.gridforge.lean",
                "GridForge.Lean.Runtime",
                "GridForge.Lean.Samples",
                "DemoScene.Lean.unity",
                "V7Workflows.Lean.unity")
        };

        private static readonly WorkflowDefinition[] Workflows =
        {
            new(
                "DenseRectangular",
                0,
                new[]
                {
                    RectangularGrid(new Vector3Int(-6, 0, -6), new Vector3Int(6, 1, 6), Dense)
                }),
            new(
                "DenseHex",
                1,
                new[]
                {
                    HexGrid(new Vector3Int(-8, 0, -8), new Vector3Int(8, 1, 8), Dense)
                }),
            new(
                "SparseRectangular",
                2,
                new[]
                {
                    RectangularGrid(
                        new Vector3Int(0, 0, 0),
                        new Vector3Int(6, 1, 6),
                        Sparse,
                        RectangularSparseIndices)
                }),
            new(
                "SparseHex",
                3,
                new[]
                {
                    HexGrid(
                        new Vector3Int(0, 0, 0),
                        new Vector3Int(10, 1, 10),
                        Sparse,
                        HexSparseIndices)
                }),
            new(
                "MixedTopologyDiagnostics",
                4,
                new[]
                {
                    RectangularGrid(new Vector3Int(-12, 0, -6), new Vector3Int(-6, 1, 0), Dense),
                    HexGrid(new Vector3Int(0, 0, -6), new Vector3Int(8, 1, 2), Dense),
                    RectangularGrid(
                        new Vector3Int(-12, 0, 6),
                        new Vector3Int(-6, 1, 12),
                        Sparse,
                        RectangularSparseIndices),
                    HexGrid(
                        new Vector3Int(0, 0, 6),
                        new Vector3Int(8, 1, 14),
                        Sparse,
                        HexSparseIndices)
                },
                includeDiagnostics: true)
        };

        [MenuItem("Tools/GridForge/Generate v7 Sample Assets")]
        public static void GenerateSamplesMenu()
        {
            GenerateSamples();
        }

        public static void GenerateSamplesBatchMode()
        {
            int exitCode = 0;

            try
            {
                GenerateSamples();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                exitCode = 1;
            }

            EditorApplication.Exit(exitCode);
        }

        public static void GenerateSamples()
        {
            foreach (PackageVariant variant in PackageVariants)
                GeneratePackageSamples(variant);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("GridForge v7 sample assets generated.");
        }

        private static void GeneratePackageSamples(PackageVariant variant)
        {
            SampleTypes types = SampleTypes.Resolve(variant);

            foreach (WorkflowDefinition workflow in Workflows)
                CreateWorkflowPrefab(variant, types, workflow);

            CreateSceneGridManagerPrefab(variant, types);
            CreateGridDebuggerPrefab(variant, types);
            CreateBlockerPrefab(variant, types);
            CreateDemoScene(variant);
            DeleteObsoleteAsset(variant.ScenePath(variant.ObsoleteWorkflowSceneName));
        }

        private static void CreateWorkflowPrefab(
            PackageVariant variant,
            SampleTypes types,
            WorkflowDefinition workflow)
        {
            GameObject root = new(workflow.Name);
            Component saver = AddRequiredComponent(root, types.GridConfigurationSaver);
            Component world = AddRequiredComponent(root, types.GridWorldComponent);
            Component manager = AddRequiredComponent(root, types.SceneGridManager);

            ConfigureGridWorldComponent(world);
            ConfigureSceneGridManager(manager, workflow.WorkflowIndex);
            ConfigureGridConfigurationSaver(saver, workflow.Configurations);
            CreateMarker(root, "Workflow Marker", Vector3.zero, new Vector3(0.8f, 0.15f, 0.8f));

            if (workflow.IncludeDiagnostics)
                ConfigureMixedDiagnostics(root, types, world);

            SavePrefab(root, variant.PrefabPath(workflow.Name));
        }

        private static void CreateSceneGridManagerPrefab(PackageVariant variant, SampleTypes types)
        {
            GameObject root = new("SceneGridManager");
            Component saver = AddRequiredComponent(root, types.GridConfigurationSaver);
            Component world = AddRequiredComponent(root, types.GridWorldComponent);
            Component manager = AddRequiredComponent(root, types.SceneGridManager);

            ConfigureGridWorldComponent(world);
            ConfigureSceneGridManager(manager, 0);
            ConfigureGridConfigurationSaver(saver, Workflows[0].Configurations);
            CreateMarker(root, "Dense Rectangular Marker", Vector3.zero, new Vector3(0.8f, 0.15f, 0.8f));

            SavePrefab(root, variant.PrefabPath("SceneGridManager"));
        }

        private static void CreateGridDebuggerPrefab(PackageVariant variant, SampleTypes types)
        {
            GameObject root = new("GridDebugger");
            Component debugger = AddRequiredComponent(root, types.GridDebugger);
            Component tracer = AddRequiredComponent(root, types.GridTracerTests);
            AddRequiredComponent(root, types.GridForgeUnityLogger);

            Transform start = CreateMarker(
                root,
                "Trace Start",
                new Vector3(-4f, 0.25f, -4f),
                new Vector3(0.4f, 0.4f, 0.4f)).transform;
            Transform end = CreateMarker(
                root,
                "Trace End",
                new Vector3(4f, 0.25f, 4f),
                new Vector3(0.4f, 0.4f, 0.4f)).transform;

            ConfigureGridDebugger(debugger, null);
            ConfigureGridTracer(tracer, null, start, end);

            SavePrefab(root, variant.PrefabPath("GridDebugger"));
        }

        private static void CreateBlockerPrefab(PackageVariant variant, SampleTypes types)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "Blocker";
            root.transform.localScale = new Vector3(3f, 0.35f, 3f);

            Component blocker = AddRequiredComponent(root, types.BlockerComponent);
            ConfigureBlocker(blocker, null);

            SavePrefab(root, variant.PrefabPath("Blocker"));
        }

        private static void CreateDemoScene(PackageVariant variant)
        {
            Scene scene = CreateEmptySampleScene("GridForge Demo");
            GameObject instance = InstantiatePrefab(variant.PrefabPath("MixedTopologyDiagnostics"));
            instance.name = "GridForge v7 Diagnostics Demo";
            instance.transform.position = Vector3.zero;

            SaveScene(scene, variant.ScenePath(variant.DemoSceneName));
        }

        private static Scene CreateEmptySampleScene(string rootName)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = rootName;

            GameObject cameraObject = new("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 12f, -18f);
            cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.Skybox;

            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            return scene;
        }

        private static void ConfigureMixedDiagnostics(
            GameObject root,
            SampleTypes types,
            Component world)
        {
            Component debugger = AddRequiredComponent(root, types.GridDebugger);
            Component tracer = AddRequiredComponent(root, types.GridTracerTests);
            AddRequiredComponent(root, types.GridForgeUnityLogger);

            Transform start = CreateMarker(
                root,
                "Trace Start",
                new Vector3(-11f, 0.25f, -5f),
                new Vector3(0.45f, 0.45f, 0.45f)).transform;
            Transform end = CreateMarker(
                root,
                "Trace End",
                new Vector3(7f, 0.25f, 13f),
                new Vector3(0.45f, 0.45f, 0.45f)).transform;

            ConfigureGridDebugger(debugger, world);
            ConfigureGridTracer(tracer, world, start, end);
            CreateBlockerChild(root, types, world);
        }

        private static void CreateBlockerChild(GameObject parent, SampleTypes types, Component world)
        {
            GameObject blockerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockerObject.name = "Sparse XZ Blocker";
            blockerObject.transform.SetParent(parent.transform, false);
            blockerObject.transform.localPosition = new Vector3(-8f, 0.25f, 9f);
            blockerObject.transform.localScale = new Vector3(2.5f, 0.35f, 2.5f);

            Component blocker = AddRequiredComponent(blockerObject, types.BlockerComponent);
            ConfigureBlocker(blocker, world);
        }

        private static void ConfigureSceneGridManager(Component manager, int workflowIndex)
        {
            SerializedObject serialized = new(manager);
            RequiredProperty(serialized, "_workflow").enumValueIndex = workflowIndex;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureGridWorldComponent(Component world)
        {
            SerializedObject serialized = new(world);
            RequiredProperty(serialized, "_spatialGridCellSize").intValue = SpatialGridCellSize;
            RequiredProperty(serialized, "_initializeOnAwake").boolValue = true;
            RequiredProperty(serialized, "_disposeOnDestroy").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureGridConfigurationSaver(
            Component saver,
            GridAuthoringDefinition[] configurations)
        {
            SerializedObject serialized = new(saver);
            RequiredProperty(serialized, "_spatialGridCellSize").intValue = SpatialGridCellSize;
            RequiredProperty(serialized, "Show").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            MethodInfo saveMethod = RequireInstanceMethod(saver.GetType(), "Save", 1);
            Type configurationType = saveMethod.GetParameters()[0].ParameterType;

            for (int i = 0; i < configurations.Length; i++)
                saveMethod.Invoke(saver, new[] { CreateSerializableGridConfiguration(configurationType, configurations[i]) });

            EditorUtility.SetDirty(saver);
        }

        private static void ConfigureGridDebugger(Component debugger, Component world)
        {
            SerializedObject serialized = new(debugger);
            RequiredProperty(serialized, "_gridWorldComponent").objectReferenceValue = world;
            RequiredProperty(serialized, "_showGrid").boolValue = true;
            RequiredProperty(serialized, "_debugAllGrids").boolValue = true;
            RequiredProperty(serialized, "_addressMode").enumValueIndex = PhysicalAndMissing;
            RequiredProperty(serialized, "_limitQueryBounds").boolValue = true;
            RequiredProperty(serialized, "_queryBoundsMin").vector3Value = new Vector3(-13f, -0.5f, -7f);
            RequiredProperty(serialized, "_queryBoundsMax").vector3Value = new Vector3(9f, 1.5f, 15f);
            RequiredProperty(serialized, "_maxCells").intValue = 512;
            RequiredProperty(serialized, "_allowFullSparseAddressScan").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureGridTracer(
            Component tracer,
            Component world,
            Transform start,
            Transform end)
        {
            SerializedObject serialized = new(tracer);
            RequiredProperty(serialized, "_gridWorldComponent").objectReferenceValue = world;
            RequiredProperty(serialized, "_showVoxelTrail").boolValue = true;
            RequiredProperty(serialized, "_showLine").boolValue = true;
            RequiredProperty(serialized, "_traceMode").enumValueIndex = XzLayer;
            RequiredProperty(serialized, "startTransform").objectReferenceValue = start;
            RequiredProperty(serialized, "endTransform").objectReferenceValue = end;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SetFixed64Field(tracer, "_layerY", 0);
            SetFixed64Field(tracer, "_lineHeight", 1);
        }

        private static void ConfigureBlocker(Component blocker, Component world)
        {
            SerializedObject serialized = new(blocker);
            RequiredProperty(serialized, "_blockerType").enumValueIndex = BoundsBlocker;
            RequiredProperty(serialized, "_isActive").boolValue = true;
            RequiredProperty(serialized, "_cacheCoveredVoxels").boolValue = true;
            RequiredProperty(serialized, "_blockAreaSource").enumValueIndex = TransformBlockArea;
            RequiredProperty(serialized, "_blockAreaMode").enumValueIndex = XzLayer;
            RequiredProperty(serialized, "_showCoveragePreview").boolValue = true;
            RequiredProperty(serialized, "_gridWorldComponent").objectReferenceValue = world;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SetFixed64Field(blocker, "_layerY", 0);
        }

        private static GameObject CreateMarker(
            GameObject parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = name;
            marker.transform.SetParent(parent.transform, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = localScale;

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);

            return marker;
        }

        private static Component AddRequiredComponent(GameObject owner, Type componentType)
        {
            Component component = owner.GetComponent(componentType);
            return component != null ? component : owner.AddComponent(componentType);
        }

        private static GameObject InstantiatePrefab(string prefabAssetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
            if (prefab == null)
                throw new FileNotFoundException($"Sample prefab not found: {prefabAssetPath}");

            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        private static void DeleteObsoleteAsset(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) == null)
                return;

            if (!AssetDatabase.DeleteAsset(assetPath))
                throw new InvalidOperationException($"Failed to delete obsolete sample asset: {assetPath}");
        }

        private static void SavePrefab(GameObject root, string assetPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ToAbsolutePath(assetPath)) ?? throw new InvalidOperationException());

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath, out bool success);
            if (!success || savedPrefab == null)
                throw new InvalidOperationException($"Failed to save prefab: {assetPath}");

            UnityEngine.Object.DestroyImmediate(root);
            NormalizeTrailingWhitespace(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void SaveScene(Scene scene, string assetPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ToAbsolutePath(assetPath)) ?? throw new InvalidOperationException());

            if (!EditorSceneManager.SaveScene(scene, assetPath))
                throw new InvalidOperationException($"Failed to save scene: {assetPath}");

            NormalizeTrailingWhitespace(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void NormalizeTrailingWhitespace(string assetPath)
        {
            string absolutePath = ToAbsolutePath(assetPath);
            string text = File.ReadAllText(absolutePath);
            string normalized = TrailingWhitespacePattern.Replace(text, "$1");

            if (!string.Equals(text, normalized, StringComparison.Ordinal))
                File.WriteAllText(absolutePath, normalized);
        }

        private static SerializedProperty RequiredProperty(SerializedObject serialized, string propertyPath)
        {
            SerializedProperty property = serialized.FindProperty(propertyPath);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Missing serialized property '{propertyPath}' on {serialized.targetObject.GetType().FullName}.");
            }

            return property;
        }

        private static object CreateSerializableGridConfiguration(
            Type configurationType,
            GridAuthoringDefinition definition)
        {
            ConstructorInfo constructor = RequireConstructor(configurationType, 7);
            ParameterInfo[] parameters = constructor.GetParameters();

            object boundsMin = CreateVector3d(parameters[0].ParameterType, definition.BoundsMin);
            object boundsMax = CreateVector3d(parameters[1].ParameterType, definition.BoundsMax);
            object topologyKind = Enum.ToObject(parameters[3].ParameterType, definition.TopologyKind);
            object topologyMetrics = CreateTopologyMetrics(parameters[4].ParameterType, definition.TopologyKind);
            object storageKind = Enum.ToObject(parameters[5].ParameterType, definition.StorageKind);
            object sparseVoxels = CreateSparseVoxelSet(parameters[6].ParameterType, definition.SparseIndices);

            return constructor.Invoke(new[]
            {
                boundsMin,
                boundsMax,
                definition.ScanCellSize,
                topologyKind,
                topologyMetrics,
                storageKind,
                sparseVoxels
            });
        }

        private static object CreateTopologyMetrics(Type metricsType, int topologyKind)
        {
            MethodInfo factory = topologyKind == HexPrism
                ? RequireMethod(metricsType, "Hex", 3)
                : RequireMethod(metricsType, "Rectangular", 3);
            ParameterInfo[] parameters = factory.GetParameters();

            object one = CreateFixed64(parameters[0].ParameterType, 1);
            object layerHeight = CreateFixed64(parameters[1].ParameterType, 1);

            if (topologyKind == HexPrism)
                return factory.Invoke(null, new[] { one, layerHeight, Enum.ToObject(parameters[2].ParameterType, PointyTop) });

            object cellLength = CreateFixed64(parameters[2].ParameterType, 1);
            return factory.Invoke(null, new[] { one, layerHeight, cellLength });
        }

        private static object CreateSparseVoxelSet(Type sparseVoxelSetType, VoxelIndexDefinition[] sparseIndices)
        {
            ConstructorInfo constructor = RequireConstructor(sparseVoxelSetType, 1);
            Type voxelIndexType = constructor.GetParameters()[0].ParameterType.GetGenericArguments()[0];
            Array typedIndices = Array.CreateInstance(voxelIndexType, sparseIndices.Length);
            ConstructorInfo voxelIndexConstructor = RequireConstructor(voxelIndexType, 3);

            for (int i = 0; i < sparseIndices.Length; i++)
            {
                VoxelIndexDefinition sparseIndex = sparseIndices[i];
                typedIndices.SetValue(
                    voxelIndexConstructor.Invoke(new object[] { sparseIndex.X, sparseIndex.Y, sparseIndex.Z }),
                    i);
            }

            return constructor.Invoke(new object[] { typedIndices });
        }

        private static object CreateVector3d(Type vectorType, Vector3Int value)
        {
            ConstructorInfo constructor = vectorType.GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });
            if (constructor == null)
                throw new InvalidOperationException($"Unable to resolve int constructor for {vectorType.FullName}.");

            return constructor.Invoke(new object[] { value.x, value.y, value.z });
        }

        private static void SetFixed64Field(Component component, string fieldName, int value)
        {
            FieldInfo field = RequireField(component.GetType(), fieldName);
            field.SetValue(component, CreateFixed64(field.FieldType, value));
            EditorUtility.SetDirty(component);
        }

        private static object CreateFixed64(Type fixed64Type, int value)
        {
            ConstructorInfo constructor = fixed64Type.GetConstructor(new[] { typeof(int) });
            if (constructor == null)
                throw new InvalidOperationException($"Unable to resolve int constructor for {fixed64Type.FullName}.");

            return constructor.Invoke(new object[] { value });
        }

        private static Type RequireType(string assemblyName, string fullName)
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                    continue;

                Type type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }

            throw new InvalidOperationException($"Unable to resolve type {fullName} in assembly {assemblyName}.");
        }

        private static FieldInfo RequireField(Type type, string fieldName)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    return field;
            }

            throw new InvalidOperationException($"Unable to resolve field {fieldName} on {type.FullName}.");
        }

        private static ConstructorInfo RequireConstructor(Type type, int parameterCount)
        {
            foreach (ConstructorInfo constructor in type.GetConstructors())
            {
                if (constructor.GetParameters().Length == parameterCount)
                    return constructor;
            }

            throw new InvalidOperationException(
                $"Unable to resolve {parameterCount}-argument constructor for {type.FullName}.");
        }

        private static MethodInfo RequireMethod(Type type, string methodName, int parameterCount)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name == methodName && method.GetParameters().Length == parameterCount)
                    return method;
            }

            throw new InvalidOperationException(
                $"Unable to resolve {type.FullName}.{methodName} with {parameterCount} arguments.");
        }

        private static MethodInfo RequireInstanceMethod(Type type, string methodName, int parameterCount)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name == methodName && method.GetParameters().Length == parameterCount)
                    return method;
            }

            throw new InvalidOperationException(
                $"Unable to resolve {type.FullName}.{methodName} with {parameterCount} arguments.");
        }

        private static GridAuthoringDefinition RectangularGrid(
            Vector3Int boundsMin,
            Vector3Int boundsMax,
            int storageKind,
            VoxelIndexDefinition[] sparseIndices = null)
        {
            return new GridAuthoringDefinition(
                boundsMin,
                boundsMax,
                RectangularPrism,
                storageKind,
                sparseIndices);
        }

        private static GridAuthoringDefinition HexGrid(
            Vector3Int boundsMin,
            Vector3Int boundsMax,
            int storageKind,
            VoxelIndexDefinition[] sparseIndices = null)
        {
            return new GridAuthoringDefinition(
                boundsMin,
                boundsMax,
                HexPrism,
                storageKind,
                sparseIndices);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal) &&
                !string.Equals(assetPath, "Assets", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Expected an Assets-relative path, got {assetPath}");
            }

            return Path.GetFullPath(Path.Combine(GetProjectRoot(), assetPath));
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private readonly struct PackageVariant
        {
            public readonly string RootAssetPath;
            public readonly string RuntimeAssemblyName;
            public readonly string SamplesAssemblyName;
            public readonly string DemoSceneName;
            public readonly string ObsoleteWorkflowSceneName;

            public PackageVariant(
                string rootAssetPath,
                string runtimeAssemblyName,
                string samplesAssemblyName,
                string demoSceneName,
                string obsoleteWorkflowSceneName)
            {
                RootAssetPath = rootAssetPath;
                RuntimeAssemblyName = runtimeAssemblyName;
                SamplesAssemblyName = samplesAssemblyName;
                DemoSceneName = demoSceneName;
                ObsoleteWorkflowSceneName = obsoleteWorkflowSceneName;
            }

            public string PrefabPath(string prefabName)
            {
                return $"{RootAssetPath}/Samples/GridforgeDemo/Prefabs/{prefabName}.prefab";
            }

            public string ScenePath(string sceneName)
            {
                return $"{RootAssetPath}/Samples/GridforgeDemo/Scenes/{sceneName}";
            }
        }

        private readonly struct SampleTypes
        {
            public readonly Type SceneGridManager;
            public readonly Type GridConfigurationSaver;
            public readonly Type GridWorldComponent;
            public readonly Type GridDebugger;
            public readonly Type GridTracerTests;
            public readonly Type GridForgeUnityLogger;
            public readonly Type BlockerComponent;

            private SampleTypes(
                Type sceneGridManager,
                Type gridConfigurationSaver,
                Type gridWorldComponent,
                Type gridDebugger,
                Type gridTracerTests,
                Type gridForgeUnityLogger,
                Type blockerComponent)
            {
                SceneGridManager = sceneGridManager;
                GridConfigurationSaver = gridConfigurationSaver;
                GridWorldComponent = gridWorldComponent;
                GridDebugger = gridDebugger;
                GridTracerTests = gridTracerTests;
                GridForgeUnityLogger = gridForgeUnityLogger;
                BlockerComponent = blockerComponent;
            }

            public static SampleTypes Resolve(PackageVariant variant)
            {
                return new SampleTypes(
                    RequireType(variant.SamplesAssemblyName, "SceneGridManager"),
                    RequireType(variant.RuntimeAssemblyName, "GridForge.Configuration.GridConfigurationSaver"),
                    RequireType(variant.RuntimeAssemblyName, "GridForge.Unity.GridWorldComponent"),
                    RequireType(variant.RuntimeAssemblyName, "GridForge.Utility.GridDebugger"),
                    RequireType(variant.RuntimeAssemblyName, "GridForge.Utility.GridTracerTests"),
                    RequireType(variant.RuntimeAssemblyName, "GridForge.Utility.GridForgeUnityLogger"),
                    RequireType(variant.RuntimeAssemblyName, "GridForge.Blockers.BlockerComponent"));
            }
        }

        private readonly struct WorkflowDefinition
        {
            public readonly string Name;
            public readonly int WorkflowIndex;
            public readonly GridAuthoringDefinition[] Configurations;
            public readonly bool IncludeDiagnostics;

            public WorkflowDefinition(
                string name,
                int workflowIndex,
                GridAuthoringDefinition[] configurations,
                bool includeDiagnostics = false)
            {
                Name = name;
                WorkflowIndex = workflowIndex;
                Configurations = configurations ?? Array.Empty<GridAuthoringDefinition>();
                IncludeDiagnostics = includeDiagnostics;
            }
        }

        private readonly struct GridAuthoringDefinition
        {
            public readonly Vector3Int BoundsMin;
            public readonly Vector3Int BoundsMax;
            public readonly int ScanCellSize;
            public readonly int TopologyKind;
            public readonly int StorageKind;
            public readonly VoxelIndexDefinition[] SparseIndices;

            public GridAuthoringDefinition(
                Vector3Int boundsMin,
                Vector3Int boundsMax,
                int topologyKind,
                int storageKind,
                VoxelIndexDefinition[] sparseIndices)
            {
                BoundsMin = boundsMin;
                BoundsMax = boundsMax;
                ScanCellSize = 4;
                TopologyKind = topologyKind;
                StorageKind = storageKind;
                SparseIndices = sparseIndices ?? Array.Empty<VoxelIndexDefinition>();
            }
        }

        private readonly struct VoxelIndexDefinition
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;

            public VoxelIndexDefinition(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }
    }
}
#endif
