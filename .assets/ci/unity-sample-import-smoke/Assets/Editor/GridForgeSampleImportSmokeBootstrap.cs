using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;

[InitializeOnLoad]
internal static class GridForgeSampleImportSmokeBootstrap
{
    private const string ConfigAssetPath = "GridForgeSampleImportSmokeConfig.json";
    private const string SampleDisplayName = "Demo Scene";
    private const string ImportedPathKey = "GridForge.SampleImportSmoke.ImportedPath";
    private const string ImportAttemptedKey = "GridForge.SampleImportSmoke.ImportAttempted";

    static GridForgeSampleImportSmokeBootstrap()
    {
        if (!ImportSampleOnce())
            EditorApplication.delayCall += () => ImportSampleOnce();
    }

    private static bool ImportSampleOnce()
    {
        if (SessionState.GetBool(ImportAttemptedKey, false))
            return true;

        if (!TryLoadConfig(out var config))
        {
            SessionState.SetBool(ImportAttemptedKey, true);
            return true;
        }

        var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{config.packageName}/package.json");
        if (packageInfo == null)
        {
            UnityEngine.Debug.Log($"GRIDFORGE_SAMPLE_SMOKE package not ready yet: {config.packageName}");
            return false;
        }

        var sample = Sample.FindByPackage(packageInfo.name, packageInfo.version)
            .FirstOrDefault(candidate => string.Equals(candidate.displayName, SampleDisplayName, StringComparison.Ordinal));
        if (string.IsNullOrEmpty(sample.displayName))
        {
            UnityEngine.Debug.LogError($"GRIDFORGE_SAMPLE_SMOKE sample was not found: {SampleDisplayName}");
            SessionState.SetBool(ImportAttemptedKey, true);
            return true;
        }

        UnityEngine.Debug.Log($"GRIDFORGE_SAMPLE_SMOKE importing {sample.displayName} from {sample.resolvedPath}");
        sample.Import(Sample.ImportOptions.OverridePreviousImports);
        SessionState.SetString(ImportedPathKey, sample.importPath);
        SessionState.SetBool(ImportAttemptedKey, true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        UnityEngine.Debug.Log($"GRIDFORGE_SAMPLE_SMOKE imported sample to {sample.importPath}");
        return true;
    }

    private static bool TryLoadConfig(out SmokeConfig config)
    {
        config = null;

        var configPath = Path.Combine(UnityEngine.Application.dataPath, ConfigAssetPath);
        if (!File.Exists(configPath))
        {
            UnityEngine.Debug.LogError($"GRIDFORGE_SAMPLE_SMOKE config was not found: {configPath}");
            return false;
        }

        config = UnityEngine.JsonUtility.FromJson<SmokeConfig>(File.ReadAllText(configPath));
        if (config == null || string.IsNullOrWhiteSpace(config.packageName))
        {
            UnityEngine.Debug.LogError($"GRIDFORGE_SAMPLE_SMOKE config must define packageName: {configPath}");
            return false;
        }

        return true;
    }

    [Serializable]
    private sealed class SmokeConfig
    {
        public string packageName;
    }
}
