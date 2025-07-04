﻿#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GridForge.Blockers.Unity_Editor
{
    /// <summary>
    /// Custom Unity Editor inspector for <see cref="BlockerComponent"/>.
    /// Dynamically displays relevant fields based on the selected blocker type.
    /// </summary>
    [CustomEditor(typeof(BlockerComponent))]
    public class EditorBlockerComponent : Editor
    {
        SerializedProperty _blockerType;
        SerializedProperty _isActive;
        SerializedProperty _cacheCoveredVoxels;
        SerializedProperty _manualBlockArea;

        public void OnEnable()
        {
            _blockerType = serializedObject.FindProperty("_blockerType");
            _isActive = serializedObject.FindProperty("_isActive");
            _cacheCoveredVoxels = serializedObject.FindProperty("_cacheCoveredVoxels");
            _manualBlockArea = serializedObject.FindProperty("_manualBlockArea");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Display the blocker type dropdown
            EditorGUILayout.PropertyField(_blockerType);

            BlockerType selectedType = (BlockerType)_blockerType.enumValueIndex;

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_isActive, new GUIContent("Is Active?"));

            EditorGUILayout.PropertyField(_cacheCoveredVoxels, new GUIContent("Cache Covered Voxels?"));

            // Show fields based on selected blocker type
            switch (selectedType)
            {
                case BlockerType.Bounds:
                    EditorGUILayout.LabelField("Bounds Blocker Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_manualBlockArea, new GUIContent("Block Area"));
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
