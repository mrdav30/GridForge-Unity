#if UNITY_EDITOR
using UnityEngine;
using FixedMathSharp;
using GridForge.Grids;

namespace GridForge.Utility
{
    /// <summary>
    /// Defines types of voxels that can be visualized in the debugger.
    /// </summary>
    public enum VoxelFilterType
    {
        All,        // Show all voxels
        Empty,      // Show only empty voxels
        Occupied,   // Show only occupied voxels
        Blocked     // Show only blocked voxels
    }

    /// <summary>
    /// Unity tool for visualizing grids and their voxels in the Scene View.
    /// This debugger highlights grid voxels, occupied spaces, and obstacles.
    /// </summary>
    [ExecuteAlways]
    public class GridDebugger : MonoBehaviour
    {
        #region Inspector Fields

        [Tooltip("Enable to visualize grid voxels.")]
        [SerializeField] private bool _showGrid;

        [Tooltip("Filter voxels based on their occupancy state.")]
        [SerializeField] private VoxelFilterType _voxelFilter = VoxelFilterType.All;

        [Tooltip("Enable to click on voxels to inspect them in Play Mode.")]
        [SerializeField] private bool _enableVoxelSelection;

        [Tooltip("Color for the highlighted voxel.")]
        [SerializeField] private Color _highlightColor = Color.green;

        [Tooltip("The world position of the voxel to highlight.")]
        [SerializeField] private Vector3 _highlightedVoxelPosition;

        [Tooltip("Grid index to debug.")]
        [SerializeField] private ushort _gridIndex = 0;

        public ushort GridIndex
        {
            get => _gridIndex;
            set => _gridIndex = value;
        }

        private VoxelGrid _targetGrid;
        public bool EnableVoxelSelection => _enableVoxelSelection;
        public Voxel SelectedVoxel { get; private set; }

        private Vector3 Scale => Vector3.one * (float) GlobalGridManager.VoxelSize;

        #endregion

        #region Unity Lifecycle

        public void Update()
        {
            if (_enableVoxelSelection && Application.isPlaying)
                HandleVoxelSelection();
        }

        public void OnDrawGizmos()
        {
            if (!_showGrid || !Application.isPlaying)
                return;

            if (!GlobalGridManager.TryGetGrid(_gridIndex, out _targetGrid))
            {
                Debug.LogWarning($"Grid index {_gridIndex} is not available in GlobalGridManager.");
                return;
            }

            DrawGrid();
        }

        #endregion

        #region Voxel Selection Logic

        private void HandleVoxelSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    Vector3d hitPos = new Vector3d(hit.point.x, hit.point.y, hit.point.z);
                    if (GlobalGridManager.TryGetGridAndVoxel(hitPos, out _, out Voxel voxel))
                    {
                        _highlightedVoxelPosition = voxel.WorldPosition.ToVector3();

                        SelectedVoxel = voxel;
                        Debug.Log($"Voxel Selected: {voxel}");
                    }
                }
            }
        }

        #endregion

        #region Grid Visualization

        private void DrawGrid()
        {
            if (_targetGrid == null)
                return;

            int width = _targetGrid.Width;
            int height = _targetGrid.Height;
            int length = _targetGrid.Length;

            Gizmos.color = Color.magenta;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < length; z++)
                    {
                        if (!_targetGrid.TryGetVoxel(x, y, z, out Voxel voxel) || !ShouldRenderVoxel(voxel))
                            continue;

                        DrawVoxelGizmo(voxel);
                    }
                }
            }

            if (_enableVoxelSelection)
            {
                Gizmos.color = _highlightColor;
                Gizmos.DrawCube(_highlightedVoxelPosition, Scale);
            }
        }

        private bool ShouldRenderVoxel(Voxel voxel)
        {
            return _voxelFilter switch
            {
                VoxelFilterType.All => true,
                VoxelFilterType.Empty => !voxel.IsOccupied && !voxel.IsBlocked,
                VoxelFilterType.Occupied => voxel.IsOccupied,
                VoxelFilterType.Blocked => voxel.IsBlocked,
                _ => true
            };
        }

        private void DrawVoxelGizmo(Voxel voxel)
        {
            Vector3 voxelPos = voxel.WorldPosition.ToVector3();
            Color renderColor = voxel.IsBlocked ? Color.red : voxel.IsOccupied ? Color.yellow : Color.magenta;
            Gizmos.color = renderColor;
            Gizmos.DrawCube(voxelPos, Scale); // Draws the solid cube
            Gizmos.color = Color.black; // Change color for wireframe
            Gizmos.DrawWireCube(voxelPos, Scale * 1.02f); // Slightly larger for visibility
            Gizmos.color = renderColor; // Reset to original color
        }

        #endregion
    }
}
#endif
