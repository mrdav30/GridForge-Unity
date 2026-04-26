using GridForge.Configuration;
using GridForge.Unity;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GridConfigurationSaver))]
public class SceneGridManager : MonoBehaviour
{
    private GridConfigurationSaver _configurationSaver;
    private GridWorldComponent _gridWorldComponent;

    private void Awake()
    {
        _configurationSaver = GetComponent<GridConfigurationSaver>();
        _gridWorldComponent = GetComponent<GridWorldComponent>();

        if (_gridWorldComponent == null)
        {
            _gridWorldComponent = gameObject.AddComponent<GridWorldComponent>();
        }

        _configurationSaver.EarlyApply(
            _gridWorldComponent.RebuildWorld(
                _configurationSaver.VoxelSize,
                _configurationSaver.SpatialGridCellSize));
    }
}
