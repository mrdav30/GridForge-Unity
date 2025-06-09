#if UNITY_EDITOR
using FixedMathSharp;
using GridForge.Grids;
using System.Linq;
using UnityEditor;

namespace GridForge.Utility.Debugging.Unity_Editor
{
    /// <summary>
    /// Custom Unity Editor inspector for <see cref="GridDebugger"/>.
    /// Displays detailed voxel metadata in the Inspector when a voxel is selected.
    /// </summary>
    [CustomEditor(typeof(GridDebugger))]
    public class EditorGridDebugger : Editor
    {
        private GridDebugger _debugger;
        private ushort[] _availableGridIndexes = new ushort[0];
        private string[] _gridIndexLabels = new string[0];

        public void OnEnable()
        {
            if (!EditorApplication.isPlaying)
                return;

            _debugger = (GridDebugger)target;
            UpdateGridIndexes();
        }

        private void UpdateGridIndexes()
        {
            _availableGridIndexes = new ushort[GlobalGridManager.ActiveGrids.Count];
            int count = 0;
            foreach (VoxelGrid grid in GlobalGridManager.ActiveGrids)
            {
                _availableGridIndexes[count] = grid.GlobalIndex;
                count++;
            }

            _gridIndexLabels = _availableGridIndexes.Select(index => $"Grid {index}").ToArray();
        }

        public override void OnInspectorGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("Play the application before debugging.", EditorStyles.boldLabel);
                return;
            }

            serializedObject.Update();

            EditorGUILayout.LabelField("Grid Debugger Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_showGrid"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_voxelFilter"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Selection", EditorStyles.boldLabel);

            if (_availableGridIndexes.Length > 0)
            {
                ushort selectedGridIndex = (ushort)EditorGUILayout.Popup(
                    "Active Grid",
                    System.Array.IndexOf(_availableGridIndexes, _debugger.GridIndex),
                    _gridIndexLabels);
                if (selectedGridIndex >= 0)
                    _debugger.GridIndex = _availableGridIndexes[selectedGridIndex];
            }
            else
            {
                EditorGUILayout.HelpBox("No active grids found.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Voxel Selection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_enableVoxelSelection"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_highlightColor"));

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
                    EditorGUILayout.HelpBox("No voxel selected. Click on a voxel in the Scene View while in Play Mode, or enter a position.", MessageType.Info);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
