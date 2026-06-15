//=======================================================================
// SerializableGridTopologyMetrics.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

using FixedMathSharp;
using GridForge.Grids.Topology;
using System;
using UnityEngine;

namespace GridForge.Configuration
{
    /// <summary>
    /// Unity-serializable authoring data for <see cref="GridTopologyMetrics"/>.
    /// </summary>
    [Serializable]
    public struct SerializableGridTopologyMetrics
    {
        [SerializeField] private Fixed64 _rectangularCellWidth;
        [SerializeField] private Fixed64 _rectangularLayerHeight;
        [SerializeField] private Fixed64 _rectangularCellLength;
        [SerializeField] private Fixed64 _hexRadius;
        [SerializeField] private Fixed64 _hexLayerHeight;
        [SerializeField] private HexOrientation _hexOrientation;

        public readonly Fixed64 RectangularCellWidth => ResolveUnsetMetric(_rectangularCellWidth);
        public readonly Fixed64 RectangularLayerHeight => ResolveUnsetMetric(_rectangularLayerHeight);
        public readonly Fixed64 RectangularCellLength => ResolveUnsetMetric(_rectangularCellLength);
        public readonly Fixed64 HexRadius => ResolveUnsetMetric(_hexRadius);
        public readonly Fixed64 HexLayerHeight => ResolveUnsetMetric(_hexLayerHeight);
        public readonly HexOrientation HexOrientation => _hexOrientation;

        public static SerializableGridTopologyMetrics DefaultRectangular =>
            Rectangular(Fixed64.One, Fixed64.One, Fixed64.One);

        public static SerializableGridTopologyMetrics DefaultHex =>
            Hex(Fixed64.One, Fixed64.One, HexOrientation.PointyTop);

        public static SerializableGridTopologyMetrics Rectangular(
            Fixed64 cellWidth,
            Fixed64 layerHeight,
            Fixed64 cellLength)
        {
            return new SerializableGridTopologyMetrics
            {
                _rectangularCellWidth = cellWidth,
                _rectangularLayerHeight = layerHeight,
                _rectangularCellLength = cellLength,
                _hexRadius = Fixed64.One,
                _hexLayerHeight = Fixed64.One,
                _hexOrientation = HexOrientation.PointyTop
            };
        }

        public static SerializableGridTopologyMetrics Hex(
            Fixed64 radius,
            Fixed64 layerHeight,
            HexOrientation orientation)
        {
            return new SerializableGridTopologyMetrics
            {
                _rectangularCellWidth = Fixed64.One,
                _rectangularLayerHeight = Fixed64.One,
                _rectangularCellLength = Fixed64.One,
                _hexRadius = radius,
                _hexLayerHeight = layerHeight,
                _hexOrientation = orientation
            };
        }

        public static SerializableGridTopologyMetrics FromGridTopologyMetrics(
            GridTopologyKind topologyKind,
            GridTopologyMetrics metrics)
        {
            return topologyKind == GridTopologyKind.HexPrism
                ? Hex(metrics.CellRadius, metrics.LayerHeight, metrics.HexOrientation)
                : Rectangular(metrics.CellWidth, metrics.LayerHeight, metrics.CellLength);
        }

        public readonly bool TryToGridTopologyMetrics(
            GridTopologyKind topologyKind,
            out GridTopologyMetrics metrics,
            out string failureReason)
        {
            metrics = default;
            failureReason = string.Empty;

            switch (topologyKind)
            {
                case GridTopologyKind.RectangularPrism:
                    Fixed64 cellWidth = RectangularCellWidth;
                    Fixed64 layerHeight = RectangularLayerHeight;
                    Fixed64 cellLength = RectangularCellLength;
                    if (cellWidth <= Fixed64.Zero || layerHeight <= Fixed64.Zero || cellLength <= Fixed64.Zero)
                    {
                        failureReason = "Rectangular-prism topology requires positive cell width, layer height, and cell length.";
                        return false;
                    }

                    metrics = GridTopologyMetrics.Rectangular(cellWidth, layerHeight, cellLength);
                    return true;

                case GridTopologyKind.HexPrism:
                    Fixed64 radius = HexRadius;
                    Fixed64 hexLayerHeight = HexLayerHeight;
                    if (radius <= Fixed64.Zero || hexLayerHeight <= Fixed64.Zero)
                    {
                        failureReason = "Hex-prism topology requires positive cell radius and layer height.";
                        return false;
                    }

                    metrics = GridTopologyMetrics.Hex(radius, hexLayerHeight, _hexOrientation);
                    return true;

                default:
                    failureReason = $"Grid topology '{topologyKind}' is not supported.";
                    return false;
            }
        }

        private static Fixed64 ResolveUnsetMetric(Fixed64 value)
        {
            return value == Fixed64.Zero ? Fixed64.One : value;
        }
    }
}
