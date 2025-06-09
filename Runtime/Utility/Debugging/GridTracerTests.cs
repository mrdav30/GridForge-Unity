#if UNITY_EDITOR
using FixedMathSharp;
using GridForge.Grids;
using UnityEngine;

namespace GridForge.Utility
{
    /// <summary>
    /// Unity MonoBehaviour for testing and visualizing grid-aligned line tracing.
    /// Draws lines and highlights nodes along a path in the Scene View.
    /// </summary>
    [ExecuteAlways] // Allows visualization in edit mode
    public class GridTracerTests : MonoBehaviour
    {
        #region Inspector Fields

        /// <summary>
        /// Enables visualization of grid nodes along the traced path.
        /// </summary>
        [Tooltip("Enable to display the grid nodes along the traced path.")]
        [SerializeField]
        private bool _showNodeTrail = true;

        /// <summary>
        /// Enables visualization of the traced line.
        /// </summary>
        [Tooltip("Enable to draw a direct line between start and end points.")]
        [SerializeField]
        private bool _showLine = true;

        /// <summary>
        /// Controls the Y-axis offset of the grid visualization.
        /// </summary>
        [Tooltip("Adjusts the height offset for the traced grid nodes.")]
        [SerializeField]
        private Fixed64 _gridHeight = Fixed64.One;

        /// <summary>
        /// The starting position for the traced line.
        /// </summary>
        [Tooltip("Starting position of the traced line.")]
        public Transform startTransform;

        /// <summary>
        /// The ending position for the traced line.
        /// </summary>
        [Tooltip("Ending position of the traced line.")]
        public Transform endTransform;

        #endregion

        #region Visualization Parameters

        /// <summary>
        /// Size of filled cubes drawn at each grid node.
        /// </summary>
        private Vector3 FillSize;

        /// <summary>
        /// Size of wireframe cubes drawn around each grid node.
        /// </summary>
        private Vector3 WireSize;

        #endregion

        #region Gizmo Rendering

        public void Start()
        {
            FillSize = Vector3.one * (float)GlobalGridManager.NodeSize;
            WireSize = FillSize * 1.02f;
        }

        /// <summary>
        /// Unity's Gizmo drawing callback. 
        /// This method visualizes the traced line and the grid nodes in the Scene View.
        /// </summary>
        public void OnDrawGizmos()
        {
            // Ensure that start and end Transforms are valid and not identical
            if (!Application.isPlaying
                || !startTransform
                || !endTransform
                || startTransform == endTransform)
            {
                return;
            }

            // Get world positions as Vector3d (fixed-point precision)
            Vector3d startPos = startTransform.position.ToVector3d();
            Vector3d endPos = endTransform.position.ToVector3d();

            // If grid visualization is enabled, trace the line and draw the grid nodes
            if (_showNodeTrail)
            {
                Gizmos.color = Color.red;

                foreach (GridNodeSet covered in GridTracer.TraceLine(startPos, endPos))
                {
                    foreach (Node node in covered.Nodes)
                    {
                        Vector3 drawPos = node.WorldPosition.ToVector3();
                        drawPos.y += (float)_gridHeight; // Adjust for visualization height

                        // Draw a filled red cube for the grid node
                        Gizmos.DrawCube(drawPos, FillSize);

                        // Draw a black wireframe around the node
                        Gizmos.color = Color.black;
                        Gizmos.DrawWireCube(drawPos, WireSize);
                        Gizmos.color = Color.red; // Reset color
                    }
                }
            }

            // If line visualization is enabled, draw the direct line between start and end points
            if (_showLine)
            {
                Gizmos.color = Color.white;
                float adjustedY = (float)(_gridHeight + Fixed64.Half); // Slight height offset for visibility

                Gizmos.DrawLine(startPos.ToVector3(adjustedY), endPos.ToVector3(adjustedY));
            }
        }

        #endregion
    }
}
#endif
