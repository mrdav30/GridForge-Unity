#if UNITY_EDITOR
using FixedMathSharp;
using FixedMathSharp.Editor;
using UnityEditor;

namespace GridForge.Configuration.Unity_Editor
{
    /// <summary>
    /// Custom Unity Editor inspector for <see cref="GridConfigurationSaver"/>.
    /// Provides UI controls for managing multiple grid configurations in the scene.
    /// </summary>
    [CustomEditor(typeof(GridConfigurationSaver))]
    public class EditorGridConfigurationSaver : Editor
    {
        SerializedProperty _voxelSize;
        SerializedProperty _spatialGridCellSize;
        SerializedProperty _savedGridConfigurations;

        private void OnEnable()
        {
            _voxelSize = serializedObject.FindProperty("_voxelSize");
            _spatialGridCellSize = serializedObject.FindProperty("_spatialGridCellSize");
            _savedGridConfigurations = serializedObject.FindProperty("_savedGridConfigurations");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            FMSEditorUtility.FixedNumberField("Voxel Size", ref _voxelSize, 0, 1);

            EditorGUILayout.PropertyField(_spatialGridCellSize);

            EditorGUILayout.PropertyField(_savedGridConfigurations, true);

            serializedObject.ApplyModifiedProperties();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("Show"));
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif