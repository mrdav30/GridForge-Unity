//=======================================================================
// EditorGridConfigurationSaver.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

#if UNITY_EDITOR
using GridForge.Grids.Storage;
using GridForge.Grids.Topology;
using System;
using UnityEditor;
using UnityEngine;

namespace GridForge.Configuration.Editor
{
    internal static class GridConfigurationEditorVisibility
    {
        internal static bool ShouldDrawRectangularMetrics(GridTopologyKind topologyKind)
        {
            return topologyKind == GridTopologyKind.RectangularPrism;
        }

        internal static bool ShouldDrawHexMetrics(GridTopologyKind topologyKind)
        {
            return topologyKind == GridTopologyKind.HexPrism;
        }

        internal static bool ShouldDrawSparseVoxels(GridStorageKind storageKind)
        {
            return storageKind == GridStorageKind.Sparse;
        }
    }

    [CustomPropertyDrawer(typeof(SerializableGridConfiguration))]
    internal sealed class SerializableGridConfigurationDrawer : PropertyDrawer
    {
        private const string BoundsMinPath = "_boundsMin";
        private const string BoundsMaxPath = "_boundsMax";
        private const string ScanCellSizePath = "_scanCellSize";
        private const string TopologyKindPath = "_topologyKind";
        private const string TopologyMetricsPath = "_topologyMetrics";
        private const string RectangularCellWidthPath = "_rectangularCellWidth";
        private const string RectangularLayerHeightPath = "_rectangularLayerHeight";
        private const string RectangularCellLengthPath = "_rectangularCellLength";
        private const string HexRadiusPath = "_hexRadius";
        private const string HexLayerHeightPath = "_hexLayerHeight";
        private const string HexOrientationPath = "_hexOrientation";
        private const string StorageKindPath = "_storageKind";
        private const string ConfiguredSparseVoxelsPath = "_configuredSparseVoxels";
        private const string SparseIndicesPath = "_indices";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect line = TakeLine(ref position, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, toggleOnLabelClick: true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            DrawProperty(ref position, property.FindPropertyRelative(BoundsMinPath), new GUIContent("Bounds Min"));
            DrawProperty(ref position, property.FindPropertyRelative(BoundsMaxPath), new GUIContent("Bounds Max"));
            DrawProperty(ref position, property.FindPropertyRelative(ScanCellSizePath), new GUIContent("Scan Cell Size"));

            SerializedProperty topologyKindProperty = property.FindPropertyRelative(TopologyKindPath);
            DrawProperty(ref position, topologyKindProperty, new GUIContent("Topology Kind"));

            GridTopologyKind topologyKind = ReadEnumValue(topologyKindProperty, GridTopologyKind.RectangularPrism);
            SerializedProperty topologyMetricsProperty = property.FindPropertyRelative(TopologyMetricsPath);
            DrawMetrics(ref position, topologyMetricsProperty, topologyKind);

            SerializedProperty storageKindProperty = property.FindPropertyRelative(StorageKindPath);
            DrawProperty(ref position, storageKindProperty, new GUIContent("Storage Kind"));

            GridStorageKind storageKind = ReadEnumValue(storageKindProperty, GridStorageKind.Dense);
            if (GridConfigurationEditorVisibility.ShouldDrawSparseVoxels(storageKind))
            {
                DrawHeader(ref position, "Sparse Voxels");
                SerializedProperty sparseIndicesProperty = property
                    .FindPropertyRelative(ConfiguredSparseVoxelsPath)
                    .FindPropertyRelative(SparseIndicesPath);
                DrawProperty(ref position, sparseIndicesProperty, new GUIContent("Configured Voxels"), includeChildren: true);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return height;

            AddPropertyHeight(ref height, property.FindPropertyRelative(BoundsMinPath));
            AddPropertyHeight(ref height, property.FindPropertyRelative(BoundsMaxPath));
            AddPropertyHeight(ref height, property.FindPropertyRelative(ScanCellSizePath));

            SerializedProperty topologyKindProperty = property.FindPropertyRelative(TopologyKindPath);
            AddPropertyHeight(ref height, topologyKindProperty);

            GridTopologyKind topologyKind = ReadEnumValue(topologyKindProperty, GridTopologyKind.RectangularPrism);
            SerializedProperty topologyMetricsProperty = property.FindPropertyRelative(TopologyMetricsPath);
            AddMetricsHeight(ref height, topologyMetricsProperty, topologyKind);

            SerializedProperty storageKindProperty = property.FindPropertyRelative(StorageKindPath);
            AddPropertyHeight(ref height, storageKindProperty);

            GridStorageKind storageKind = ReadEnumValue(storageKindProperty, GridStorageKind.Dense);
            if (GridConfigurationEditorVisibility.ShouldDrawSparseVoxels(storageKind))
            {
                AddHeaderHeight(ref height);
                SerializedProperty sparseIndicesProperty = property
                    .FindPropertyRelative(ConfiguredSparseVoxelsPath)
                    .FindPropertyRelative(SparseIndicesPath);
                AddPropertyHeight(ref height, sparseIndicesProperty, includeChildren: true);
            }

            return height;
        }

        private static void DrawMetrics(
            ref Rect position,
            SerializedProperty topologyMetricsProperty,
            GridTopologyKind topologyKind)
        {
            if (GridConfigurationEditorVisibility.ShouldDrawRectangularMetrics(topologyKind))
            {
                DrawHeader(ref position, "Rectangular Metrics");
                DrawProperty(
                    ref position,
                    topologyMetricsProperty.FindPropertyRelative(RectangularCellWidthPath),
                    new GUIContent("Cell Width"));
                DrawProperty(
                    ref position,
                    topologyMetricsProperty.FindPropertyRelative(RectangularLayerHeightPath),
                    new GUIContent("Layer Height"));
                DrawProperty(
                    ref position,
                    topologyMetricsProperty.FindPropertyRelative(RectangularCellLengthPath),
                    new GUIContent("Cell Length"));
                return;
            }

            if (GridConfigurationEditorVisibility.ShouldDrawHexMetrics(topologyKind))
            {
                DrawHeader(ref position, "Hex Metrics");
                DrawProperty(
                    ref position,
                    topologyMetricsProperty.FindPropertyRelative(HexRadiusPath),
                    new GUIContent("Radius"));
                DrawProperty(
                    ref position,
                    topologyMetricsProperty.FindPropertyRelative(HexLayerHeightPath),
                    new GUIContent("Layer Height"));
                DrawProperty(
                    ref position,
                    topologyMetricsProperty.FindPropertyRelative(HexOrientationPath),
                    new GUIContent("Orientation"));
            }
        }

        private static void AddMetricsHeight(
            ref float height,
            SerializedProperty topologyMetricsProperty,
            GridTopologyKind topologyKind)
        {
            if (GridConfigurationEditorVisibility.ShouldDrawRectangularMetrics(topologyKind))
            {
                AddHeaderHeight(ref height);
                AddPropertyHeight(ref height, topologyMetricsProperty.FindPropertyRelative(RectangularCellWidthPath));
                AddPropertyHeight(ref height, topologyMetricsProperty.FindPropertyRelative(RectangularLayerHeightPath));
                AddPropertyHeight(ref height, topologyMetricsProperty.FindPropertyRelative(RectangularCellLengthPath));
                return;
            }

            if (GridConfigurationEditorVisibility.ShouldDrawHexMetrics(topologyKind))
            {
                AddHeaderHeight(ref height);
                AddPropertyHeight(ref height, topologyMetricsProperty.FindPropertyRelative(HexRadiusPath));
                AddPropertyHeight(ref height, topologyMetricsProperty.FindPropertyRelative(HexLayerHeightPath));
                AddPropertyHeight(ref height, topologyMetricsProperty.FindPropertyRelative(HexOrientationPath));
            }
        }

        private static void DrawHeader(ref Rect position, string text)
        {
            Rect line = NextLine(ref position, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(line, text, EditorStyles.boldLabel);
        }

        private static void DrawProperty(
            ref Rect position,
            SerializedProperty property,
            GUIContent label,
            bool includeChildren = false)
        {
            float height = EditorGUI.GetPropertyHeight(property, label, includeChildren);
            Rect line = NextLine(ref position, height);
            EditorGUI.PropertyField(line, property, label, includeChildren);
        }

        private static Rect NextLine(ref Rect position, float height)
        {
            position.y += EditorGUIUtility.standardVerticalSpacing;
            return TakeLine(ref position, height);
        }

        private static Rect TakeLine(ref Rect position, float height)
        {
            Rect line = new(position.x, position.y, position.width, height);
            position.y += height;
            return line;
        }

        private static void AddHeaderHeight(ref float height)
        {
            height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
        }

        private static void AddPropertyHeight(ref float height, SerializedProperty property, bool includeChildren = false)
        {
            height += EditorGUIUtility.standardVerticalSpacing
                + EditorGUI.GetPropertyHeight(property, includeChildren);
        }

        private static TEnum ReadEnumValue<TEnum>(SerializedProperty property, TEnum fallback)
            where TEnum : struct, Enum
        {
            string[] enumNames = property.enumNames;
            int index = property.enumValueIndex;
            if ((uint)index < (uint)enumNames.Length && Enum.TryParse(enumNames[index], out TEnum value))
                return value;

            return fallback;
        }
    }

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
            for (int i = 0; i < saver.SavedGridConfigurations.Length; i++)
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
