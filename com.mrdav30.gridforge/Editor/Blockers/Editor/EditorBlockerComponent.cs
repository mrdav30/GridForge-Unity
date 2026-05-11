#if UNITY_EDITOR
using FixedMathSharp;
using UnityEditor;
using UnityEngine;

namespace GridForge.Blockers.Editor
{
    /// <summary>
    /// Custom Unity Editor inspector for <see cref="BlockerComponent"/>.
    /// Dynamically displays relevant fields based on the selected blocker type.
    /// </summary>
    [CustomEditor(typeof(BlockerComponent))]
    public class EditorBlockerComponent : UnityEditor.Editor
    {
        private SerializedProperty _gridWorldComponent;
        private SerializedProperty _blockerType;
        private SerializedProperty _isActive;
        private SerializedProperty _cacheCoveredVoxels;
        private SerializedProperty _blockAreaSource;
        private SerializedProperty _includeChildrenInBlockArea;
        private SerializedProperty _manualBlockArea;

        public void OnEnable()
        {
            _gridWorldComponent = serializedObject.FindProperty("_gridWorldComponent");
            _blockerType = serializedObject.FindProperty("_blockerType");
            _isActive = serializedObject.FindProperty("_isActive");
            _cacheCoveredVoxels = serializedObject.FindProperty("_cacheCoveredVoxels");
            _blockAreaSource = serializedObject.FindProperty("_blockAreaSource");
            _includeChildrenInBlockArea = serializedObject.FindProperty("_includeChildrenInBlockArea");
            _manualBlockArea = serializedObject.FindProperty("_manualBlockArea");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_gridWorldComponent, new GUIContent("Grid World"));
            EditorGUILayout.PropertyField(_blockerType);

            BlockerType selectedType = (BlockerType)_blockerType.enumValueIndex;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_isActive, new GUIContent("Is Active?"));
            EditorGUILayout.PropertyField(_cacheCoveredVoxels, new GUIContent("Cache Covered Voxels?"));

            switch (selectedType)
            {
                case BlockerType.Bounds:
                    EditorGUILayout.LabelField("Bounds Blocker Settings", EditorStyles.boldLabel);
                    DrawBlockAreaSettings();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBlockAreaSettings()
        {
            EditorGUILayout.PropertyField(_blockAreaSource, new GUIContent("Block Area Source"));

            BlockAreaSource selectedSource = (BlockAreaSource)_blockAreaSource.enumValueIndex;
            if (selectedSource == BlockAreaSource.Collider || selectedSource == BlockAreaSource.Renderer)
                EditorGUILayout.PropertyField(_includeChildrenInBlockArea, new GUIContent("Include Children"));

            if (selectedSource == BlockAreaSource.Manual)
            {
                EditorGUILayout.PropertyField(_manualBlockArea, new GUIContent("Block Area"));
                return;
            }

            serializedObject.ApplyModifiedProperties();
            DrawCalculatedBlockAreaPreview();
        }

        private void DrawCalculatedBlockAreaPreview()
        {
            if (target is not BlockerComponent blocker)
                return;

            BoundingArea blockArea = blocker.CalculateBlockArea(
                out BlockAreaSource resolvedSource,
                out string fallbackReason);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Resolved Source", resolvedSource);
                EditorGUILayout.Vector3Field("Block Area Min", blockArea.Min.ToVector3());
                EditorGUILayout.Vector3Field("Block Area Max", blockArea.Max.ToVector3());
            }

            if (!string.IsNullOrEmpty(fallbackReason))
            {
                EditorGUILayout.HelpBox(
                    $"{fallbackReason} Falling back to {nameof(BlockAreaSource.Transform)} block area.",
                    MessageType.Warning);
            }
        }
    }
}
#endif
