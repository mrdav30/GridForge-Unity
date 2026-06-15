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

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Grid Debugger", EditorStyles.boldLabel);
            DrawQuerySettings();
            DrawSelectionSettings();
            DrawColorSettings();

            if (EditorApplication.isPlaying)
            {
                DrawGridSelection();
                DrawQueryStatus();
                DrawSelectedVoxel();
            }
            else
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Play the application before debugging active grids.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawQuerySettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_gridWorldComponent"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_showGrid"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_debugAllGrids"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_filterTopologyKind"));
            if (serializedObject.FindProperty("_filterTopologyKind").boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_topologyKind"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_filterStorageKind"));
            if (serializedObject.FindProperty("_filterStorageKind").boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_storageKind"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_addressMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_requiredStates"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_excludedStates"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_limitQueryBounds"));
            if (serializedObject.FindProperty("_limitQueryBounds").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_queryBoundsMin"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_queryBoundsMax"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_maxCells"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_allowFullSparseAddressScan"));
        }

        private void DrawSelectionSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_enableVoxelSelection"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_highlightColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_highlightedVoxelPosition"));
        }

        private void DrawColorSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_emptyCellColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_occupiedCellColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_blockedCellColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_boundaryCellColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_partitionedCellColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_missingSparseAddressColor"));
        }

        private void DrawGridSelection()
        {
            if (_debugger.DebugAllGrids)
                return;

            UpdateGridIndexes();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Selection", EditorStyles.boldLabel);

            if (_availableGridIndexes.Length == 0)
            {
                EditorGUILayout.HelpBox("No active grids found in the resolved GridWorld.", MessageType.Warning);
                return;
            }

            int currentGridIndex = Array.IndexOf(_availableGridIndexes, _debugger.GridIndex);
            if (currentGridIndex < 0)
                currentGridIndex = 0;

            int selectedGridIndex = EditorGUILayout.Popup("Active Grid", currentGridIndex, _gridIndexLabels);
            _debugger.GridIndex = _availableGridIndexes[selectedGridIndex];
        }

        private void DrawQueryStatus()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Query Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Status", _debugger.LastQueryStatus.ToString());
            EditorGUILayout.IntField("Cells", _debugger.LastQueryCellCount);
            EditorGUILayout.IntField("Skipped", _debugger.LastQuerySkippedCellCount);
            EditorGUILayout.IntField("Visited", _debugger.LastVisitedCellCount);
            EditorGUILayout.IntField("Dirty Changes", _debugger.LastDirtyChangeCount);
        }

        private void DrawSelectedVoxel()
        {
            if (!_debugger.EnableVoxelSelection)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected Voxel", EditorStyles.boldLabel);
            if (_debugger.SelectedVoxel == null)
            {
                EditorGUILayout.HelpBox("No physical voxel selected.", MessageType.Info);
                return;
            }

            EditorGUILayout.Vector3Field("World Position", _debugger.SelectedVoxel.WorldPosition.ToVector3());
            EditorGUILayout.Toggle("Occupied", _debugger.SelectedVoxel.IsOccupied);
            EditorGUILayout.Toggle("Blocked", _debugger.SelectedVoxel.IsBlocked);
            EditorGUILayout.Toggle("Boundary", _debugger.SelectedVoxel.IsBoundaryVoxel);
            EditorGUILayout.LabelField("Spawn Token", _debugger.SelectedVoxel.SpawnToken.ToString());
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
    }
}
#endif
