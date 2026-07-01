//=======================================================================
// GitDependencyInstaller.cs
//=======================================================================
// MIT License, Copyright (c) 2024-present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace GridForge.Editor
{
    [InitializeOnLoad]
    public static class GitDependencyInstaller
    {
        private struct Dependency
        {
            public readonly string Name;
            public readonly string GitUrl;
            public readonly string Version;

            public string Value => !string.IsNullOrEmpty(GitUrl)
                ? $"{GitUrl}#{Version}"
                : Version;

            public Dependency(string name, string gitUrl, string version)
            {
                Name = name;
                GitUrl = gitUrl;
                Version = version;
            }
        }

        private struct ManifestDependency
        {
            public string Name;
            public string Value;

            public ManifestDependency(string name, string value)
            {
                Name = name;
                Value = value;
            }
        }

        private const string SessionKey = "MRDAV30_GRIDFORGE_LEAN_DEPENDENCY_CHECK";
        private const string ManifestPath = "Packages/manifest.json";
        private const string PackageDisplayName = "GridForge.Lean";

        private static readonly Dependency[] RequiredDependencies =
        {
            new(
                "com.mrdav30.fixedmathsharp.lean",
                "https://github.com/mrdav30/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp.lean",
                "v6.0.0"
            ),
            new(
                "com.mrdav30.swiftcollections.lean",
                "https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.lean",
                "v6.0.0"
            ),
            new(
                "com.mrdav30.swiftcollections.fixedmathsharp.lean",
                "https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.fixedmathsharp.lean",
                "v6.0.0"
            )
        };

        static GitDependencyInstaller()
        {
            Install();
        }

        [MenuItem("Tools/ " + PackageDisplayName + " /Repair Dependencies")]
        private static void RepairDependenciesMenu()
        {
            SessionState.SetBool(SessionKey, false);
            Install();
        }

        private static void Install()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;

            SessionState.SetBool(SessionKey, true);
            Debug.Log($"Checking required {PackageDisplayName} dependencies in {ManifestPath}.");

            try
            {
                InstallDependencies();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{PackageDisplayName} dependency repair failed. " +
                    $"Use Tools > {PackageDisplayName} > Repair Dependencies after fixing the manifest. " +
                    exception);
            }
        }

        private static void InstallDependencies()
        {
            if (!File.Exists(ManifestPath))
            {
                Debug.LogWarning(
                    $"{PackageDisplayName} dependency repair skipped because {ManifestPath} was not found. " +
                    "Open the package from a Unity project with a Packages/manifest.json file.");
                return;
            }

            string manifestJson = File.ReadAllText(ManifestPath);
            string updatedJson = UpdateManifestDependencies(manifestJson, out bool modified, out bool success);
            if (!success)
                return;

            if (!modified)
            {
                Debug.Log($"All required {PackageDisplayName} dependencies are already satisfied.");
                return;
            }

            File.WriteAllText(ManifestPath, updatedJson);

            Debug.Log($"{PackageDisplayName} dependency manifest updated. Resolving Unity packages.");
            Client.Resolve();
            AssetDatabase.Refresh();
        }

        private static string UpdateManifestDependencies(
            string manifestJson,
            out bool modified,
            out bool success)
        {
            modified = false;
            success = false;

            if (string.IsNullOrWhiteSpace(manifestJson))
            {
                Debug.LogWarning($"{PackageDisplayName} dependency repair skipped because {ManifestPath} is empty.");
                return manifestJson;
            }

            ManifestDependency[] dependencies;
            if (TryFindDependenciesBlock(manifestJson, out int blockStart, out int blockEnd))
            {
                string dependencyBody = manifestJson.Substring(blockStart + 1, blockEnd - blockStart - 1);
                dependencies = ParseDependencies(dependencyBody);
            }
            else if (TryFindRootCloseBrace(manifestJson, out blockEnd))
            {
                dependencies = Array.Empty<ManifestDependency>();
                blockStart = -1;
                modified = true;
                Debug.Log($"{PackageDisplayName} dependency repair created a missing manifest dependencies block.");
            }
            else
            {
                Debug.LogWarning($"{PackageDisplayName} dependency repair skipped because {ManifestPath} is not a JSON object.");
                return manifestJson;
            }

            foreach (Dependency dependency in RequiredDependencies)
                AddOrUpdateDependency(ref dependencies, dependency, ref modified);

            success = true;
            if (!modified)
                return manifestJson;

            string dependenciesBlock = BuildDependenciesBlock(dependencies);
            if (blockStart >= 0)
                return manifestJson.Remove(blockStart, blockEnd - blockStart + 1).Insert(blockStart, dependenciesBlock);

            string insertionPrefix = RootHasProperties(manifestJson, blockEnd) ? ",\n" : "\n";
            string insertion = $"{insertionPrefix}  \"dependencies\": {dependenciesBlock}\n";
            return manifestJson.Insert(blockEnd, insertion);
        }

        private static bool TryFindDependenciesBlock(string manifestJson, out int blockStart, out int blockEnd)
        {
            blockStart = -1;
            blockEnd = -1;

            Match match = Regex.Match(manifestJson, "\"dependencies\"\\s*:\\s*\\{");
            if (!match.Success)
                return false;

            blockStart = manifestJson.IndexOf('{', match.Index + match.Length - 1);
            return blockStart >= 0 && TryFindMatchingBrace(manifestJson, blockStart, out blockEnd);
        }

        private static bool TryFindRootCloseBrace(string manifestJson, out int closeBrace)
        {
            closeBrace = -1;
            int openBrace = manifestJson.IndexOf('{');
            return openBrace >= 0 && TryFindMatchingBrace(manifestJson, openBrace, out closeBrace);
        }

        private static bool TryFindMatchingBrace(string text, int openBrace, out int closeBrace)
        {
            closeBrace = -1;
            bool inString = false;
            bool escaped = false;
            int depth = 0;

            for (int i = openBrace; i < text.Length; i++)
            {
                char c = text[i];
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (c == '"')
                        inString = false;

                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    continue;
                }

                if (c == '{')
                {
                    depth++;
                    continue;
                }

                if (c != '}')
                    continue;

                depth--;
                if (depth != 0)
                    continue;

                closeBrace = i;
                return true;
            }

            return false;
        }

        private static ManifestDependency[] ParseDependencies(string dependencyBody)
        {
            MatchCollection matches = Regex.Matches(
                dependencyBody,
                "\"(?<name>[^\"\\\\]+)\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"\\\\])*)\"");
            ManifestDependency[] dependencies = new ManifestDependency[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                dependencies[i] = new ManifestDependency(
                    UnescapeJson(match.Groups["name"].Value),
                    UnescapeJson(match.Groups["value"].Value));
            }

            return dependencies;
        }

        private static void AddOrUpdateDependency(
            ref ManifestDependency[] dependencies,
            Dependency dependency,
            ref bool modified)
        {
            string desiredValue = dependency.Value;
            for (int i = 0; i < dependencies.Length; i++)
            {
                if (dependencies[i].Name != dependency.Name)
                    continue;

                if (dependencies[i].Value == desiredValue)
                {
                    Debug.Log($"Dependency already satisfied: {dependency.Name} -> {desiredValue}");
                    return;
                }

                Debug.Log($"Dependency updated: {dependency.Name} {dependencies[i].Value} -> {desiredValue}");
                dependencies[i].Value = desiredValue;
                modified = true;
                return;
            }

            Array.Resize(ref dependencies, dependencies.Length + 1);
            dependencies[^1] = new ManifestDependency(dependency.Name, desiredValue);
            modified = true;
            Debug.Log($"Dependency added: {dependency.Name} -> {desiredValue}");
        }

        private static string BuildDependenciesBlock(ManifestDependency[] dependencies)
        {
            if (dependencies.Length == 0)
                return "{}";

            string[] lines = new string[dependencies.Length + 2];
            lines[0] = "{";
            for (int i = 0; i < dependencies.Length; i++)
            {
                string comma = i == dependencies.Length - 1 ? string.Empty : ",";
                lines[i + 1] =
                    $"    \"{EscapeJson(dependencies[i].Name)}\": \"{EscapeJson(dependencies[i].Value)}\"{comma}";
            }

            lines[^1] = "  }";
            return string.Join("\n", lines);
        }

        private static bool RootHasProperties(string manifestJson, int rootCloseBrace)
        {
            int rootOpenBrace = manifestJson.IndexOf('{');
            if (rootOpenBrace < 0 || rootCloseBrace <= rootOpenBrace)
                return false;

            string body = manifestJson.Substring(rootOpenBrace + 1, rootCloseBrace - rootOpenBrace - 1);
            return !string.IsNullOrWhiteSpace(body);
        }

        private static string EscapeJson(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private static string UnescapeJson(string value)
        {
            return Regex.Unescape(value ?? string.Empty);
        }
    }
}
#endif
