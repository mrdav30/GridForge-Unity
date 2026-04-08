#if UNITY_EDITOR
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace GridForge.Editor
{
    [InitializeOnLoad]
    public static class GitDependencyInstaller
    {
        #region Nested Types

        private struct Dependency
        {
            public string Name;
            public string GitUrl;
            public string Version;

            public readonly string Value => !string.IsNullOrEmpty(GitUrl)
                ? $"{GitUrl}#{Version}"
                : Version;

            public Dependency(string name, string gitUrl, string version)
            {
                Name = name;
                GitUrl = gitUrl;
                Version = version;
            }
        }

        #endregion

        #region Configuration

        private const string Key = "MRDAV30_DEPENDENCY_CHECK";

        private const string ManifestPath = "Packages/manifest.json";

        private const string CollectionsPackage = "com.mrdav30.gridforge";

        private static readonly Dependency[] RequiredDependencies =
        {
            new(
                "com.mrdav30.fixedmathsharp",
                "https://github.com/mrdav30/FixedMathSharp-Unity.git",
                "v2.2.0"
            ),
			new(
                "com.mrdav30.swiftcollections",
                "https://github.com/mrdav30/SwiftCollections-Unity.git",
                "v3.0.0"
            )
        };

        #endregion

        static GitDependencyInstaller()
        {
            EditorApplication.delayCall += Install;
        }

        [MenuItem("Tools/mrdav30/Repair " + CollectionsPackage + " Dependencies")]
        private static void RepairDependenciesMenu()
        {
            SessionState.SetBool(Key, false); // allow reinstall
            Install();
        }

        private static void Install()
        {
            if (SessionState.GetBool(Key, false))
                return;

            Debug.Log($"Checking for required {CollectionsPackage} dependencies...");
            SessionState.SetBool(Key, true);

            if (!File.Exists(ManifestPath))
            {
                Debug.Log("manifest.json not found. Cannot install dependencies.");
                return;
            }

            var json = File.ReadAllText(ManifestPath);
            var manifest = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);

            if (!manifest.ContainsKey("dependencies") || manifest["dependencies"] is not Dictionary<string, object> dependencies)
            {
                Debug.LogWarning("manifest.json dependencies block missing.");
                return;
            }

            bool modified = false;

            foreach (var dep in RequiredDependencies)
                modified |= AddDependency(dependencies, dep);

            if (modified)
            {
                var updated = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(ManifestPath, updated);

                // Force Unity to re-resolve packages
                Debug.Log("Resolving Unity packages...");
                Client.Resolve();
                AssetDatabase.Refresh();

                Debug.Log($"Installed required {CollectionsPackage} dependencies.");
            }
            else
                Debug.Log("All required dependencies are already installed.");
        }

        private static bool AddDependency(Dictionary<string, object> deps, Dependency dep)
        {
            if (deps.ContainsKey(dep.Name) && deps[dep.Name]?.ToString() == dep.Value)
                return false;

            deps[dep.Name] = dep.Value;

            Debug.Log($"Dependency installed/updated: {dep.Name} → {dep.Value}");

            return true;
        }
    }
}
#endif
