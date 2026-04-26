#if UNITY_EDITOR
using FixedMathSharp;
using GridForge.Grids;
using System;
using System.Linq;
using UnityEditor;

namespace GridForge.Utility.Debugging.Editor
{
    /// <summary>
    /// Custom Unity Editor inspector for <see cref="GridDebugger"/>.
    /// Displays detailed voxel metadata in the Inspector when a voxel is selected.
    /// </summary>
    [CustomEditor(typeof(GridDebugger))]
    public class EditorGridDebugger : UnityEditor.Editor
    {
        private GridDebugger _debugger;
        private ushort[] _availableGridIndexes = Array.Empty<ushort>();
        private string[] _gridIndexLabels = Array.Empty<string>();

        public void OnEnable()
        {
            _debugger = (GridDebugger)target;
        }

        private void UpdateGridIndexes()
        {
            GridWorld world = _debugger.World;
            if (world == null || !world.IsActive)
            {
                _availableGridIndexes = Array.Empty<ushort>();
                _gridIndexLabels = Array.Empty<string>();
                return;
            }

            _availableGridIndexes = world.ActiveGrids
                .Where(grid => grid != null)
                .Select(grid => grid.GridIndex)
                .ToArray();
            _gridIndexLabels = _availableGridIndexes.Select(index => $"Grid {index}").ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Grid Debugger Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_gridWorldComponent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_showGrid"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_voxelFilter"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_enableVoxelSelection"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_highlightColor"));

            if (!EditorApplication.isPlaying)
            {
                serializedObject.ApplyModifiedProperties();
                EditorGUILayout.LabelField("Play the application before debugging.", EditorStyles.boldLabel);
                return;
            }

            UpdateGridIndexes();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Selection", EditorStyles.boldLabel);

            if (_availableGridIndexes.Length > 0)
            {
                int currentGridIndex = Array.IndexOf(_availableGridIndexes, _debugger.GridIndex);
                if (currentGridIndex < 0)
                {
                    currentGridIndex = 0;
                }

                int selectedGridIndex = EditorGUILayout.Popup("Active Grid", currentGridIndex, _gridIndexLabels);
                _debugger.GridIndex = _availableGridIndexes[selectedGridIndex];
            }
            else
            {
                EditorGUILayout.HelpBox("No active grids found in the resolved GridWorld.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Select Voxel", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_highlightedVoxelPosition"));

            if (_debugger.EnableVoxelSelection)
            {
                if (_debugger.SelectedVoxel != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Selected Voxel Information", EditorStyles.boldLabel);
                    EditorGUILayout.Vector3Field("World Position", _debugger.SelectedVoxel.WorldPosition.ToVector3());
                    EditorGUILayout.Toggle("Occupied", _debugger.SelectedVoxel.IsOccupied);
                    EditorGUILayout.Toggle("Blocked", _debugger.SelectedVoxel.IsBlocked);
                    EditorGUILayout.LabelField("Spawn Token", _debugger.SelectedVoxel.SpawnToken.ToString());
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "No voxel selected. Click on a voxel in the Scene View while in Play Mode, or enter a position.",
                        MessageType.Info);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
