#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace GridForge.Build.Editor
{
    /// <summary>
    /// Unity batchmode entry point for importing and reserializing staged package content.
    /// </summary>
    public static class GridForgePackageBuild
    {
        private const string StagePackagePathArg = "-gridforgeStagePackagePath";
        private static string _stagePackagePath;
        private static bool _isRunning;

        public static void ImportAndPrepare()
        {
            try
            {
                _stagePackagePath = NormalizeAndValidateStagePath(GetArgumentValue(StagePackagePathArg));

                if (_isRunning)
                {
                    return;
                }

                _isRunning = true;
                UnityEngine.Debug.Log($"Importing staged GridForge package: {_stagePackagePath}");

                EditorApplication.update -= WaitForStageImport;
                EditorApplication.update += WaitForStageImport;
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        private static string GetArgumentValue(string argumentName)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], argumentName, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static string NormalizeAndValidateStagePath(string stagePackagePath)
        {
            if (string.IsNullOrWhiteSpace(stagePackagePath))
            {
                throw new InvalidOperationException($"Missing required command line argument: {StagePackagePathArg}");
            }

            string normalizedStagePath = stagePackagePath.Replace('\\', '/');
            if (!normalizedStagePath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Stage package path must be project-relative and start with Assets/: {normalizedStagePath}");
            }

            string absoluteStagePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), normalizedStagePath));
            if (!Directory.Exists(absoluteStagePath))
            {
                throw new DirectoryNotFoundException($"Stage package folder not found: {absoluteStagePath}");
            }

            return normalizedStagePath;
        }

        private static void WaitForStageImport()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
            {
                return;
            }

            EditorApplication.update -= WaitForStageImport;

            try
            {
                PrepareAssets(_stagePackagePath);
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        private static void PrepareAssets(string stagePackagePath)
        {
            string absoluteStagePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), stagePackagePath));
            List<string> assetPaths = new();
            foreach (string filePath in Directory.EnumerateFiles(absoluteStagePath, "*", SearchOption.AllDirectories))
            {
                if (filePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                assetPaths.Add(ToAssetPath(filePath));
            }

            if (assetPaths.Count > 0)
            {
                AssetDatabase.ForceReserializeAssets(assetPaths);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            UnityEngine.Debug.Log($"Prepared {assetPaths.Count} staged assets for package export.");
        }

        private static string ToAssetPath(string absolutePath)
        {
            string projectAssetsPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Assets"));
            string normalizedAbsolutePath = Path.GetFullPath(absolutePath).Replace('\\', '/');
            string normalizedAssetsPath = projectAssetsPath.Replace('\\', '/');

            if (!normalizedAbsolutePath.StartsWith(normalizedAssetsPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Path is not inside the Assets folder: {absolutePath}");
            }

            return "Assets" + normalizedAbsolutePath.Substring(normalizedAssetsPath.Length);
        }
    }
}
#endif
