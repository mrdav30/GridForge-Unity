//=======================================================================
// EditorGridForgeUnityLogger.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

#if UNITY_EDITOR
using GridForge;
using SwiftCollections.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace GridForge.Utility.Debugging.Editor
{
    /// <summary>
    /// Custom Unity Editor inspector for <see cref="GridForgeUnityLogger"/>.
    /// </summary>
    [CustomEditor(typeof(GridForgeUnityLogger))]
    public sealed class EditorGridForgeUnityLogger : UnityEditor.Editor
    {
        private GridForgeUnityLogger _logger;
        private SerializedProperty _enableOnComponentEnableProperty;
        private SerializedProperty _minimumLevelProperty;

        public void OnEnable()
        {
            _logger = (GridForgeUnityLogger)target;
            _enableOnComponentEnableProperty = serializedObject.FindProperty("_enableOnComponentEnable");
            _minimumLevelProperty = serializedObject.FindProperty("_minimumLevel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("GridForge Unity Logger", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableOnComponentEnableProperty);
            EditorGUILayout.PropertyField(_minimumLevelProperty);
            EditorGUILayout.HelpBox(
                "GridForgeLogger forwards runtime messages into Unity logs. GridDiagnostics is the separate cell descriptor API used by debuggers and overlays.",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();

            DrawLoggerStatus();
            DrawLoggerControls();
        }

        private void DrawLoggerStatus()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("This Component", _logger.IsLoggingEnabled ? "Enabled" : "Disabled");
            EditorGUILayout.LabelField("GridForge Minimum Level", GridForgeLogger.MinimumLevel.ToString());

            if (GridForgeUnityLogger.HasActiveLogger && !_logger.IsLoggingEnabled)
            {
                EditorGUILayout.HelpBox(
                    $"Another {nameof(GridForgeUnityLogger)} is currently forwarding GridForge logs.",
                    MessageType.Warning);
            }
        }

        private void DrawLoggerControls()
        {
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_logger.IsLoggingEnabled))
            {
                if (GUILayout.Button("Enable Logging"))
                {
                    ApplyMinimumLevelFromSerializedProperty();
                    _logger.EnableLogging();
                }
            }

            using (new EditorGUI.DisabledScope(!_logger.IsLoggingEnabled))
            {
                if (GUILayout.Button("Apply Minimum Level"))
                {
                    ApplyMinimumLevelFromSerializedProperty();
                }

                if (GUILayout.Button("Disable Logging"))
                {
                    _logger.DisableLogging();
                }
            }
        }

        private void ApplyMinimumLevelFromSerializedProperty()
        {
            _logger.MinimumLevel = (DiagnosticLevel)_minimumLevelProperty.enumValueIndex;
        }
    }
}
#endif
