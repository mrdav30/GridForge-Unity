using GridForge.Configuration;
using GridForge.Unity;
using UnityEngine;

public enum GridForgeSampleWorkflow
{
    DenseRectangular,
    DenseHex,
    SparseRectangular,
    SparseHex,
    MixedTopologyDiagnostics
}

[DisallowMultipleComponent]
[RequireComponent(typeof(GridConfigurationSaver))]
[RequireComponent(typeof(GridWorldComponent))]
public class SceneGridManager : MonoBehaviour
{
    [SerializeField] private GridForgeSampleWorkflow _workflow;

    private GridConfigurationSaver _configurationSaver;
    private GridWorldComponent _gridWorldComponent;

    public GridForgeSampleWorkflow Workflow => _workflow;

    private void Awake()
    {
        ApplyAuthoringToWorld();
    }

    public void ApplyAuthoringToWorld()
    {
        _configurationSaver = GetComponent<GridConfigurationSaver>();
        _gridWorldComponent = GetComponent<GridWorldComponent>();

        if (_configurationSaver == null)
            _configurationSaver = gameObject.AddComponent<GridConfigurationSaver>();

        if (_gridWorldComponent == null)
            _gridWorldComponent = gameObject.AddComponent<GridWorldComponent>();

        _configurationSaver.EarlyApply(
            _gridWorldComponent.RebuildWorld(
                _configurationSaver.SpatialGridCellSize));
    }
}
