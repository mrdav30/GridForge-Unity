//=======================================================================
// GridTraceVisualizer.cs
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
using UnityEngine.Serialization;

namespace GridForge.Utility
{
    /// <summary>Trace projections supported by <see cref="GridTraceVisualizer"/>.</summary>
    public enum GridTraceMode
    {
        /// <summary>Traces through the full three-dimensional world.</summary>
        World3D = 0,
        /// <summary>Projects the trace onto a fixed XZ layer.</summary>
        XzLayer = 1
    }

    /// <summary>
    /// Unity Scene View visualizer for topology-aware GridForge trace coverage.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("GridForge/Debugging/Grid Trace Visualizer")]
    public class GridTraceVisualizer : MonoBehaviour
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
        [FormerlySerializedAs("startTransform")]
        [SerializeField] private Transform _startTransform;

        [Tooltip("Ending position of the traced line.")]
        [FormerlySerializedAs("endTransform")]
        [SerializeField] private Transform _endTransform;

        [SerializeField] private GridWorldComponent _gridWorldComponent;

        #endregion

        [SerializeField] private Color _traceCellColor = Color.red;
        [SerializeField] private Color _traceLineColor = Color.white;

        /// <summary>Gets the active trace projection mode.</summary>
        public GridTraceMode TraceMode => _traceMode;
        /// <summary>Gets the Y layer used for XZ tracing.</summary>
        public Fixed64 LayerY => _layerY;
        /// <summary>Gets the grid count from the most recent gizmo trace.</summary>
        public int LastTracedGridCount { get; private set; }
        /// <summary>Gets the voxel count from the most recent gizmo trace.</summary>
        public int LastTracedVoxelCount { get; private set; }

        /// <summary>Gets or sets the trace start transform.</summary>
        public Transform StartTransform
        {
            get => _startTransform;
            set => _startTransform = value;
        }

        /// <summary>Gets or sets the trace end transform.</summary>
        public Transform EndTransform
        {
            get => _endTransform;
            set => _endTransform = value;
        }

        #region Gizmo Rendering

        private void OnDrawGizmos()
        {
            GridWorld world = ResolveWorld();

            if (!Application.isPlaying
                || world == null
                || !world.IsActive
                || !_startTransform
                || !_endTransform
                || _startTransform == _endTransform)
            {
                return;
            }

            Vector3d startPos = _startTransform.position.ToVector3d();
            Vector3d endPos = _endTransform.position.ToVector3d();

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
                    ResolveLineStart(startPos).ToVector3XZ(adjustedY),
                    ResolveLineEnd(endPos).ToVector3XZ(adjustedY));
            }
        }

        private GridWorld ResolveWorld()
        {
            GridWorldComponent worldComponent = GridWorldComponentUtility.Resolve(this, _gridWorldComponent);
            return worldComponent != null ? worldComponent.World : null;
        }

        #endregion

        /// <summary>Configures the trace projection and optional XZ layer.</summary>
        /// <param name="mode">The trace projection mode.</param>
        /// <param name="layerY">The Y layer used by XZ tracing.</param>
        public void ConfigureTraceMode(GridTraceMode mode, Fixed64 layerY = default)
        {
            _traceMode = mode;
            _layerY = layerY;
        }

        /// <summary>Collects the voxels covered by a trace into an existing list.</summary>
        /// <param name="world">The active world to trace.</param>
        /// <param name="start">The trace start position.</param>
        /// <param name="end">The trace end position.</param>
        /// <param name="results">The destination list, which is cleared before use.</param>
        /// <returns>The number of collected voxels.</returns>
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

        /// <summary>Tries to create a diagnostic descriptor for the first traced voxel.</summary>
        /// <param name="world">The active world to trace.</param>
        /// <param name="start">The trace start position.</param>
        /// <param name="end">The trace end position.</param>
        /// <param name="cell">Receives the first diagnostic cell.</param>
        /// <returns><see langword="true"/> when the trace covers a voxel.</returns>
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
