//=======================================================================
// GridTracerTests.cs
//=======================================================================
// MIT License, Copyright (c) 2024–present David Oravsky (mrdav30)
// See LICENSE file in the project root for full license information.
//=======================================================================

#if UNITY_EDITOR
using FixedMathSharp;
using GridForge.Diagnostics;
using GridForge.Grids;
using GridForge.Unity;
using SwiftCollections;
using UnityEngine;

namespace GridForge.Utility
{
    public enum GridTraceMode
    {
        World3D = 0,
        XzLayer = 1
    }

    /// <summary>
    /// Unity Scene View visualizer for topology-aware GridForge trace coverage.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("GridForge/Debugging/Grid Trace Visualizer")]
    public class GridTracerTests : MonoBehaviour
    {
        #region Inspector Fields

        [Tooltip("Enable to display the grid cells along the traced path.")]
        [SerializeField] private bool _showVoxelTrail = true;

        [Tooltip("Enable to draw a direct line between start and end points.")]
        [SerializeField] private bool _showLine = true;

        [SerializeField] private GridTraceMode _traceMode = GridTraceMode.World3D;

        [Tooltip("World Y layer used by XZ layer tracing.")]
        [SerializeField] private Fixed64 _layerY = Fixed64.Zero;

        [Tooltip("Optional positive padding applied before snapping trace endpoints.")]
        [SerializeField] private Fixed64 _padding = Fixed64.Zero;

        [SerializeField] private bool _includeEnd = true;

        [Tooltip("Adjusts the height offset for the traced line.")]
        [SerializeField] private Fixed64 _lineHeight = Fixed64.One;

        [Tooltip("Starting position of the traced line.")]
        public Transform startTransform;

        [Tooltip("Ending position of the traced line.")]
        public Transform endTransform;

        [SerializeField] private GridWorldComponent _gridWorldComponent;

        #endregion

        [SerializeField] private Color _traceCellColor = Color.red;
        [SerializeField] private Color _traceLineColor = Color.white;

        public GridTraceMode TraceMode => _traceMode;
        public Fixed64 LayerY => _layerY;
        public int LastTracedGridCount { get; private set; }
        public int LastTracedVoxelCount { get; private set; }

        #region Gizmo Rendering

        public void OnDrawGizmos()
        {
            GridWorld world = ResolveWorld();

            if (!Application.isPlaying
                || world == null
                || !world.IsActive
                || !startTransform
                || !endTransform
                || startTransform == endTransform)
            {
                return;
            }

            Vector3d startPos = startTransform.position.ToVector3d();
            Vector3d endPos = endTransform.position.ToVector3d();

            if (_showVoxelTrail)
            {
                LastTracedGridCount = 0;
                LastTracedVoxelCount = 0;

                foreach (GridVoxelSet covered in TraceLine(world, startPos, endPos))
                {
                    LastTracedGridCount++;
                    foreach (Voxel voxel in covered.Voxels)
                    {
                        GridDiagnosticCell cell = GridDiagnosticGizmoDrawer.CreatePhysicalCell(world, covered.Grid, voxel);
                        GridDiagnosticGizmoDrawer.DrawWireCell(in cell, _traceCellColor);
                        LastTracedVoxelCount++;
                    }
                }
            }

            if (_showLine)
            {
                Gizmos.color = _traceLineColor;
                float adjustedY = (float)(ResolveLineLayerY(startPos, endPos) + _lineHeight + Fixed64.Half);

                Gizmos.DrawLine(
                    ResolveLineStart(startPos).ToVector3(adjustedY),
                    ResolveLineEnd(endPos).ToVector3(adjustedY));
            }
        }

        private GridWorld ResolveWorld()
        {
            GridWorldComponent worldComponent = GridWorldComponentUtility.Resolve(this, _gridWorldComponent);
            return worldComponent != null ? worldComponent.World : null;
        }

        #endregion

        public void ConfigureTraceMode(GridTraceMode mode, Fixed64 layerY = default)
        {
            _traceMode = mode;
            _layerY = layerY;
        }

        public int GetTraceVoxelsInto(
            GridWorld world,
            Vector3d start,
            Vector3d end,
            SwiftList<Voxel> results)
        {
            if (results == null)
                return 0;

            results.Clear();
            if (world == null || !world.IsActive)
                return 0;

            foreach (GridVoxelSet covered in TraceLine(world, start, end))
            {
                foreach (Voxel voxel in covered.Voxels)
                    results.Add(voxel);
            }

            return results.Count;
        }

        public bool TryGetFirstTraceDiagnosticCell(
            GridWorld world,
            Vector3d start,
            Vector3d end,
            out GridDiagnosticCell cell)
        {
            cell = default;
            if (world == null || !world.IsActive)
                return false;

            foreach (GridVoxelSet covered in TraceLine(world, start, end))
            {
                foreach (Voxel voxel in covered.Voxels)
                {
                    cell = GridDiagnosticGizmoDrawer.CreatePhysicalCell(world, covered.Grid, voxel);
                    return true;
                }
            }

            return false;
        }

        private System.Collections.Generic.IEnumerable<GridVoxelSet> TraceLine(
            GridWorld world,
            Vector3d start,
            Vector3d end)
        {
            Fixed64? padding = _padding > Fixed64.Zero ? _padding : null;
            if (_traceMode == GridTraceMode.XzLayer)
            {
                return GridTracer.TraceLine(
                    world,
                    new Vector2d(start.X, start.Z),
                    new Vector2d(end.X, end.Z),
                    padding,
                    _includeEnd,
                    layerY: _layerY);
            }

            return GridTracer.TraceLine(world, start, end, padding, _includeEnd);
        }

        private Vector3d ResolveLineStart(Vector3d start) =>
            _traceMode == GridTraceMode.XzLayer
                ? new Vector3d(start.X, _layerY, start.Z)
                : start;

        private Vector3d ResolveLineEnd(Vector3d end) =>
            _traceMode == GridTraceMode.XzLayer
                ? new Vector3d(end.X, _layerY, end.Z)
                : end;

        private Fixed64 ResolveLineLayerY(Vector3d start, Vector3d end) =>
            _traceMode == GridTraceMode.XzLayer
                ? _layerY
                : FixedMath.Min(start.Y, end.Y);
    }
}
#endif
