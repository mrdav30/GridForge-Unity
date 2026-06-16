# GridForge Unity v7 User Guide

This guide covers the Unity-facing workflow for GridForge v7. For engine-neutral
core behavior, use the GridForge wiki:

- [Getting Started](https://github.com/mrdav30/GridForge/wiki/Getting-Started)
- [Common Workflows](https://github.com/mrdav30/GridForge/wiki/Common-Workflows)
- [Sparse Grid Storage](https://github.com/mrdav30/GridForge/wiki/Sparse-Grid-Storage)
- [Grid Diagnostics and Geometry](https://github.com/mrdav30/GridForge/wiki/Grid-Diagnostics-and-Geometry)
- [Diagnostics and Logging](https://github.com/mrdav30/GridForge/wiki/Diagnostics-and-Logging)

## Mental Model

GridForge core owns deterministic grid behavior. Unity owns authoring,
inspection, scene references, gizmos, and logging adapters.

- `GridWorldComponent` owns the runtime `GridWorld` for a scene.
- `GridConfigurationSaver` stores authored `SerializableGridConfiguration`
  values and applies them to a `GridWorld`.
- `BlockerComponent`, `GridDebugger`, and `Grid Trace Visualizer` operate on the
  resolved `GridWorldComponent`.
- FixedMathSharp types are serialized directly through FixedMathSharp-Unity.
- SwiftCollections runtime collections are used through SwiftCollections-Unity
  serialized adapters such as `SerializedSwiftList<T>`.

When a scene has multiple worlds, assign the intended `GridWorldComponent`
explicitly on every helper component.

## Install One Variant

Standard package:

```text
https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge
```

Lean package:

```text
https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge.lean
```

The standard and lean variants define overlapping GridForge assemblies. Install
one variant only.

## Scene-Owned World

The simplest scene setup is one GameObject that holds both the world and the
grid authoring data:

1. Add `GridWorldComponent`.
2. Add `GridConfigurationSaver`.
3. Author one or more saved grid configurations.
4. Rebuild the world and apply the saved configurations on startup.

```csharp
using GridForge.Configuration;
using GridForge.Grids;
using GridForge.Unity;
using UnityEngine;

public sealed class GridBootstrap : MonoBehaviour
{
    [SerializeField] private GridWorldComponent _worldComponent;
    [SerializeField] private GridConfigurationSaver _configurationSaver;

    private void Awake()
    {
        GridWorld world = _worldComponent.RebuildWorld(_configurationSaver.SpatialGridCellSize);
        _configurationSaver.EarlyApply(world);
    }
}
```

The package sample uses `SceneGridManager` for this bootstrap and to switch
between the included v7 workflow demonstrations.

## Rectangular Grid Authoring

Inspector workflow:

1. Add a saved configuration in `GridConfigurationSaver`.
2. Set `Topology Kind` to `RectangularPrism`.
3. Set rectangular metrics: `Cell Width`, `Layer Height`, and `Cell Length`.
4. Set `Storage Kind` to `Dense` unless you are authoring a sparse grid.
5. Use `Show` before play mode to preview the authored bounds.

Code equivalent:

```csharp
using FixedMathSharp;
using GridForge.Configuration;
using GridForge.Grids.Storage;
using GridForge.Grids.Topology;

configurationSaver.Save(new SerializableGridConfiguration(
    boundsMin: new Vector3d(-10, 0, -10),
    boundsMax: new Vector3d(10, 0, 10),
    scanCellSize: 8,
    topologyKind: GridTopologyKind.RectangularPrism,
    topologyMetrics: SerializableGridTopologyMetrics.Rectangular(
        Fixed64.One,
        Fixed64.One,
        Fixed64.One),
    storageKind: GridStorageKind.Dense,
    configuredSparseVoxels: SerializableSparseVoxelSet.Empty));
```

## Hex Grid Authoring

Inspector workflow:

1. Add a saved configuration in `GridConfigurationSaver`.
2. Set `Topology Kind` to `HexPrism`.
3. Set hex metrics: `Radius`, `Layer Height`, and `Orientation`.
4. Choose `PointyTop` or `FlatTop` based on the scene layout.
5. Set `Storage Kind` to `Dense` or `Sparse`.

Code equivalent:

```csharp
using FixedMathSharp;
using GridForge.Configuration;
using GridForge.Grids.Storage;
using GridForge.Grids.Topology;

configurationSaver.Save(new SerializableGridConfiguration(
    boundsMin: new Vector3d(-12, 0, -12),
    boundsMax: new Vector3d(12, 1, 12),
    scanCellSize: 8,
    topologyKind: GridTopologyKind.HexPrism,
    topologyMetrics: SerializableGridTopologyMetrics.Hex(
        new Fixed64(2),
        Fixed64.One,
        HexOrientation.PointyTop),
    storageKind: GridStorageKind.Dense,
    configuredSparseVoxels: SerializableSparseVoxelSet.Empty));
```

## Sparse Grid Authoring

Sparse grids store only configured physical cells. This is useful for islands,
rooms, non-rectangular maps, or large bounds with a small authored footprint.

Inspector workflow:

1. Set `Storage Kind` to `Sparse`.
2. Add `Configured Voxels`.
3. Use non-negative topology-local indices.
4. Keep indices inside the normalized grid dimensions.

Code equivalent:

```csharp
using FixedMathSharp;
using GridForge.Configuration;
using GridForge.Grids.Storage;
using GridForge.Grids.Topology;

SerializableSparseVoxelSet sparseVoxels = new(new[]
{
    new SerializableVoxelIndex(0, 0, 0),
    new SerializableVoxelIndex(1, 0, 0),
    new SerializableVoxelIndex(1, 0, 1),
    new SerializableVoxelIndex(2, 0, 1)
});

configurationSaver.Save(new SerializableGridConfiguration(
    boundsMin: new Vector3d(0, 0, 0),
    boundsMax: new Vector3d(8, 1, 8),
    scanCellSize: 4,
    topologyKind: GridTopologyKind.RectangularPrism,
    topologyMetrics: SerializableGridTopologyMetrics.Rectangular(
        Fixed64.One,
        Fixed64.One,
        Fixed64.One),
    storageKind: GridStorageKind.Sparse,
    configuredSparseVoxels: sparseVoxels));
```

## Diagnostics Debugger

`GridDebugger` is an editor gizmo component backed by `GridForge.Diagnostics`.
It draws diagnostic cell descriptors instead of duplicating topology math in
Unity.

Useful settings:

- `Show Grid`: draw cells in play mode.
- `Debug All Grids`: show every active grid in the resolved world.
- `Filter Topology Kind`: limit the view to rectangular or hex grids.
- `Filter Storage Kind`: limit the view to dense or sparse grids.
- `Address Mode`:
  - `PhysicalOnly`: show configured physical cells.
  - `PhysicalAndMissing`: show physical cells and missing sparse addresses.
  - `MissingOnly`: show only missing sparse addresses.
- `Limit Query Bounds`: inspect a bounded area.
- `Max Cells`: protect the editor from unbounded diagnostic passes.
- `Allow Full Sparse Address Scan`: opt in when inspecting missing sparse
  addresses without query bounds.

The query status fields are read-only inspector output. They show whether a
diagnostic pass completed, hit the cell budget, found an inactive world, or
needed explicit sparse address bounds.

## Trace, Blockers, And Scene Tools

`Grid Trace Visualizer` shows topology-aware line coverage in the Scene view.
Use it to validate rectangular, hex, dense, sparse, and mixed-grid traces. Assign
start and end transforms, then choose `World3D` or `XzLayer` tracing.

`BlockerComponent` creates scene-authored blockers. It can resolve bounds from:

- manual `FixedBoundArea`
- transform scale
- collider bounds
- renderer bounds

For flat maps, use `XzLayer` mode so authoring uses deterministic fixed XZ
bounds on a single Y layer.

## Unity Logging Adapter

`GridForgeUnityLogger` forwards core `GridForgeLogger` messages into Unity logs.
It is optional and separate from `GridForge.Diagnostics`, which is the cell
descriptor API used by debuggers and overlays.

```csharp
using GridForge.Utility;
using SwiftCollections.Diagnostics;

GridForgeUnityLogger logger = gameObject.AddComponent<GridForgeUnityLogger>();
logger.MinimumLevel = DiagnosticLevel.Warning;
logger.EnableLogging();
```

Only one `GridForgeUnityLogger` can be active at a time. Disabling the component
restores the previous core logger handler and minimum level.

## Serialization Notes

- FixedMathSharp values should remain FixedMathSharp values. Do not create Unity
  shadow types for `Fixed64`, `Vector2d`, `Vector3d`, or bounds data.
- SwiftCollections runtime types are not serialized directly by Unity. Use the
  `SerializedSwift*` adapters from SwiftCollections-Unity for persisted Unity
  fields, then consume the runtime collection through `.Runtime`.
- `GridConfigurationSaver` persists saved configurations through
  `SerializedSwiftList<SerializableGridConfiguration>`.
- Arrays and `IEnumerable<T>` remain fine for API boundaries, fixtures, and
  conversion helpers.
