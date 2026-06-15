#if UNITY_EDITOR
using GridForge.Diagnostics;
using UnityEngine;

namespace GridForge.Utility
{
    /// <summary>
    /// Streams diagnostic cells into Unity gizmos while exposing query counts.
    /// </summary>
    public struct GridDiagnosticUnityVisitor : IGridDiagnosticCellVisitor
    {
        private readonly bool _drawGizmos;
        private readonly Color _emptyCellColor;
        private readonly Color _occupiedCellColor;
        private readonly Color _blockedCellColor;
        private readonly Color _boundaryCellColor;
        private readonly Color _partitionedCellColor;
        private readonly Color _missingSparseAddressColor;

        public int VisitedCellCount { get; private set; }
        public int PhysicalCellCount { get; private set; }
        public int MissingSparseAddressCellCount { get; private set; }
        public int LastVertexCount { get; private set; }
        public int LastEdgeCount { get; private set; }
        public GridDiagnosticCell LastCell { get; private set; }

        public GridDiagnosticUnityVisitor(
            bool drawGizmos,
            Color emptyCellColor,
            Color occupiedCellColor,
            Color blockedCellColor,
            Color boundaryCellColor,
            Color partitionedCellColor,
            Color missingSparseAddressColor)
        {
            _drawGizmos = drawGizmos;
            _emptyCellColor = emptyCellColor;
            _occupiedCellColor = occupiedCellColor;
            _blockedCellColor = blockedCellColor;
            _boundaryCellColor = boundaryCellColor;
            _partitionedCellColor = partitionedCellColor;
            _missingSparseAddressColor = missingSparseAddressColor;

            VisitedCellCount = 0;
            PhysicalCellCount = 0;
            MissingSparseAddressCellCount = 0;
            LastVertexCount = 0;
            LastEdgeCount = 0;
            LastCell = default;
        }

        public static GridDiagnosticUnityVisitor CreateCountingOnly() =>
            new(
                drawGizmos: false,
                Color.magenta,
                Color.yellow,
                Color.red,
                Color.cyan,
                Color.green,
                new Color(1f, 0.56f, 0.1f, 0.42f));

        public bool Visit(in GridDiagnosticCell cell)
        {
            VisitedCellCount++;
            LastCell = cell;
            LastVertexCount = GridDiagnosticGizmoDrawer.GetVertexCount(in cell);
            LastEdgeCount = GridDiagnosticGizmoDrawer.GetEdgeCount(in cell);

            if (cell.Kind == GridDiagnosticCellKind.MissingSparseAddress)
                MissingSparseAddressCellCount++;
            else
                PhysicalCellCount++;

            if (_drawGizmos)
                GridDiagnosticGizmoDrawer.DrawWireCell(in cell, ResolveColor(in cell));

            return true;
        }

        private Color ResolveColor(in GridDiagnosticCell cell)
        {
            if (cell.Kind == GridDiagnosticCellKind.MissingSparseAddress)
                return _missingSparseAddressColor;

            if ((cell.State & GridDiagnosticCellState.Blocked) != 0)
                return _blockedCellColor;

            if ((cell.State & GridDiagnosticCellState.Occupied) != 0)
                return _occupiedCellColor;

            if ((cell.State & GridDiagnosticCellState.Partitioned) != 0)
                return _partitionedCellColor;

            if ((cell.State & GridDiagnosticCellState.Boundary) != 0)
                return _boundaryCellColor;

            return _emptyCellColor;
        }
    }
}
#endif
