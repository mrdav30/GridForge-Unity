//=======================================================================
// EditorBlockerComponent.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

#if UNITY_EDITOR
using FixedMathSharp;
using FixedMathSharp.Bounds;
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
        private SerializedProperty _isActive;
        private SerializedProperty _cacheCoveredVoxels;
        private SerializedProperty _blockAreaSource;
        private SerializedProperty _includeChildrenInBlockArea;
        private SerializedProperty _manualBlockBox;
        private SerializedProperty _showCoveragePreview;
        private SerializedProperty _coveragePreviewColor;

        public void OnEnable()
        {
            _gridWorldComponent = serializedObject.FindProperty("_gridWorldComponent");
            _isActive = serializedObject.FindProperty("_isActive");
            _cacheCoveredVoxels = serializedObject.FindProperty("_cacheCoveredVoxels");
            _blockAreaSource = serializedObject.FindProperty("_blockAreaSource");
            _includeChildrenInBlockArea = serializedObject.FindProperty("_includeChildrenInBlockArea");
            _manualBlockBox = serializedObject.FindProperty("_manualBlockBox");
            _showCoveragePreview = serializedObject.FindProperty("_showCoveragePreview");
            _coveragePreviewColor = serializedObject.FindProperty("_coveragePreviewColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_gridWorldComponent, new GUIContent("Grid World"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_isActive, new GUIContent("Is Active?"));
            EditorGUILayout.PropertyField(_cacheCoveredVoxels, new GUIContent("Cache Covered Voxels?"));

            EditorGUILayout.LabelField("Bounds Blocker Settings", EditorStyles.boldLabel);
            DrawBlockAreaSettings();
            DrawCoveragePreviewSettings();

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
                EditorGUILayout.PropertyField(_manualBlockBox, new GUIContent("Fixed Bound Box"));
                return;
            }

            serializedObject.ApplyModifiedProperties();
            DrawCalculatedBlockBoxPreview();
        }

        private void DrawCoveragePreviewSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Coverage Preview", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_showCoveragePreview, new GUIContent("Show Preview"));
            if (_showCoveragePreview.boolValue)
                EditorGUILayout.PropertyField(_coveragePreviewColor, new GUIContent("Preview Color"));

            EditorGUILayout.HelpBox(
                "Coverage preview queries physical voxels only. Missing sparse address cells are never materialized or blocked.",
                MessageType.Info);
        }

        private void DrawCalculatedBlockBoxPreview()
        {
            if (target is not BlockerComponent blocker)
                return;

            FixedBoundBox blockArea = blocker.CalculateBlockBox(
                out BlockAreaSource resolvedSource,
                out string fallbackReason);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Resolved Source", resolvedSource);
                EditorGUILayout.Vector3Field("Fixed Bound Box Min", blockArea.Min.ToVector3());
                EditorGUILayout.Vector3Field("Fixed Bound Box Max", blockArea.Max.ToVector3());
            }

            if (!string.IsNullOrEmpty(fallbackReason))
            {
                EditorGUILayout.HelpBox(
                    $"{fallbackReason} Falling back to {nameof(BlockAreaSource.Transform)} block box.",
                    MessageType.Warning);
            }
        }
    }
}
#endif
