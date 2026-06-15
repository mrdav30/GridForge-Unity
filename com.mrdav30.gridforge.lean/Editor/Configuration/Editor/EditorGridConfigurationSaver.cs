#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GridForge.Configuration.Editor
{
    /// <summary>
    /// Custom Unity Editor inspector for <see cref="GridConfigurationSaver"/>.
    /// Provides UI controls for managing multiple grid configurations in the scene.
    /// </summary>
    [CustomEditor(typeof(GridConfigurationSaver))]
    public class EditorGridConfigurationSaver : UnityEditor.Editor
    {
        private SerializedProperty _spatialGridCellSize;
        private SerializedProperty _savedGridConfigurations;
        private SerializedProperty _show;

        private void OnEnable()
        {
            _spatialGridCellSize = serializedObject.FindProperty("_spatialGridCellSize");
            _savedGridConfigurations = serializedObject.FindProperty("_savedGridConfigurations");
            _show = serializedObject.FindProperty("Show");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_spatialGridCellSize);
            EditorGUILayout.PropertyField(_savedGridConfigurations, true);

            if (!EditorApplication.isPlaying)
                EditorGUILayout.PropertyField(_show);

            serializedObject.ApplyModifiedProperties();

            DrawValidationMessages((GridConfigurationSaver)target);
        }

        private static void DrawValidationMessages(GridConfigurationSaver saver)
        {
            for (int i = 0; i < saver.SavedGridConfigurations.Count; i++)
            {
                SerializableGridConfiguration config = saver.SavedGridConfigurations[i];
                if (config.BoundsMax < config.BoundsMin)
                {
                    EditorGUILayout.HelpBox(
                        $"Grid {i}: bounds max must be greater than or equal to bounds min.",
                        MessageType.Warning);
                    continue;
                }

                if (!config.TryToGridConfiguration(out _, out string configFailure))
                {
                    EditorGUILayout.HelpBox($"Grid {i}: {configFailure}", MessageType.Warning);
                    continue;
                }

                if (!config.TryGetConfiguredSparseVoxels(out _, out string sparseFailure))
                {
                    EditorGUILayout.HelpBox($"Grid {i}: {sparseFailure}", MessageType.Warning);
                }
            }
        }
    }
}
#endif
