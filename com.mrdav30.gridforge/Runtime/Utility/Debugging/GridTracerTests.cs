#if UNITY_EDITOR
using FixedMathSharp;
using GridForge.Grids;
using GridForge.Unity;
using UnityEngine;

namespace GridForge.Utility
{
    /// <summary>
    /// Unity MonoBehaviour for testing and visualizing grid-aligned line tracing.
    /// Draws lines and highlights voxels along a path in the Scene View.
    /// </summary>
    [ExecuteAlways]
    public class GridTracerTests : MonoBehaviour
    {
        #region Inspector Fields

        [Tooltip("Enable to display the grid voxels along the traced path.")]
        [SerializeField] private bool _showVoxelTrail = true;

        [Tooltip("Enable to draw a direct line between start and end points.")]
        [SerializeField] private bool _showLine = true;

        [Tooltip("Adjusts the height offset for the traced line.")]
        [SerializeField] private Fixed64 _lineHeight = Fixed64.One;

        [Tooltip("Starting position of the traced line.")]
        public Transform startTransform;

        [Tooltip("Ending position of the traced line.")]
        public Transform endTransform;

        [SerializeField] private GridWorldComponent _gridWorldComponent;

        #endregion

        #region Visualization Parameters

        private Vector3 FillSize;
        private Vector3 WireSize;

        #endregion

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

            FillSize = Vector3.one * (float)world.VoxelSize;
            WireSize = FillSize * 1.02f;

            Vector3d startPos = startTransform.position.ToVector3d();
            Vector3d endPos = endTransform.position.ToVector3d();

            if (_showVoxelTrail)
            {
                Gizmos.color = Color.red;

                foreach (GridVoxelSet covered in GridTracer.TraceLine(world, startPos, endPos))
                {
                    foreach (Voxel voxel in covered.Voxels)
                    {
                        Vector3 drawPos = voxel.WorldPosition.ToVector3();

                        Gizmos.DrawCube(drawPos, FillSize);
                        Gizmos.color = Color.black;
                        Gizmos.DrawWireCube(drawPos, WireSize);
                        Gizmos.color = Color.red;
                    }
                }
            }

            if (_showLine)
            {
                Gizmos.color = Color.white;
                float adjustedY = (float)(_lineHeight + Fixed64.Half);

                Gizmos.DrawLine(startPos.ToVector3(adjustedY), endPos.ToVector3(adjustedY));
            }
        }

        private GridWorld ResolveWorld()
        {
            GridWorldComponent worldComponent = GridWorldComponentUtility.Resolve(this, _gridWorldComponent);
            return worldComponent != null ? worldComponent.World : null;
        }

        #endregion
    }
}
#endif
