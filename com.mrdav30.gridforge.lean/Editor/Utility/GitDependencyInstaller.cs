#if UNITY_EDITOR
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
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

        private const string CollectionsPackage = "GridForge.Lean";

        private static readonly Dependency[] RequiredDependencies =
        {
            new(
                "com.mrdav30.fixedmathsharp.lean",
                "https://github.com/mrdav30/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp.lean",
                "v5.0.1"
            ),
            new(
                "com.mrdav30.swiftcollections.lean",
                "https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.lean",
                "v5.0.2"
            ),
            new(
                "com.mrdav30.swiftcollections.fixedmathsharp.lean",
                "https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.fixedmathsharp.lean",
                "v5.0.2"
            )
        };

        #endregion

        static GitDependencyInstaller()
        {
            EditorApplication.delayCall += Install;
        }

        [MenuItem("Tools/ " + CollectionsPackage + " /Repair Dependencies")]
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
            JsonNode manifest = JsonNode.Parse(json);

            if (manifest?["dependencies"] is not JsonObject dependencies)
            {
                Debug.LogWarning("manifest.json dependencies block missing.");
                return;
            }

            bool modified = false;

            foreach (var dep in RequiredDependencies)
                modified |= AddDependency(dependencies, dep);

            if (modified)
            {
                string updated = manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

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

        private static bool AddDependency(JsonObject deps, Dependency dep)
        {
            if (deps.TryGetPropertyValue(dep.Name, out JsonNode existing) && existing?.GetValue<string>() == dep.Value)
                return false;

            deps[dep.Name] = dep.Value;

            Debug.Log($"Dependency installed/updated: {dep.Name} -> {dep.Value}");

            return true;
        }
    }
}
#endif
