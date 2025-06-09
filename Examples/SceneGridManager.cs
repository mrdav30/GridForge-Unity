using GridForge.Configuration;
using GridForge.Grids;
using UnityEngine;

public class SceneGridManager : MonoBehaviour
{
    GridConfigurationSaver configurationSaver;

    // Start is called before the first frame update
    void Awake()
    {
        configurationSaver = gameObject.GetComponent<GridConfigurationSaver>();
        GlobalGridManager.Setup(configurationSaver.NodeSize, configurationSaver.SpatialGridCellSize);
        gameObject.GetComponent<GridConfigurationSaver>().EarlyApply();
    }

    private void OnApplicationQuit()
    {
        GlobalGridManager.Reset();
    }
}
