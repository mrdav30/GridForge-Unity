---
uid: GridForge.Unity.GridAuthoring
title: Grid authoring in Unity
description: Author rectangular, hex-prism, dense, and sparse GridForge configurations through Unity serialization.
---

# Grid authoring

<xref:GridForge.Configuration.GridConfigurationSaver> stores Unity-serializable
configuration values and applies them to an explicit core `GridWorld`. It does
not replace GridForge topology or storage behavior; it converts authoring data
into the core contracts.

## The authoring model

- A `GridWorldComponent` owns the scene's runtime world.
- A `GridConfigurationSaver` owns one or more saved configurations.
- Each configuration chooses bounds, scan-cell size, topology, metrics, and
  storage kind.
- Sparse configurations also carry the topology-local indices that should
  physically exist.

FixedMathSharp values stay as `Fixed64`, `Vector2d`, and `Vector3d` fields.
FixedMathSharp-Unity owns their Unity serialization and conversion support.

## Rectangular grids

In the Inspector:

1. Add a saved configuration.
2. Set **Topology Kind** to `RectangularPrism`.
3. Set **Cell Width**, **Layer Height**, and **Cell Length**.
4. Keep **Storage Kind** at `Dense` unless you want an explicit sparse shape.
5. Enable **Show** to preview authored bounds outside Play mode.

The equivalent code is:

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

Rectangular `VoxelIndex` values mean local `(x, y, z)` coordinates.

## Hex-prism grids

In the Inspector:

1. Set **Topology Kind** to `HexPrism`.
2. Set a positive **Radius** and **Layer Height**.
3. Choose `PointyTop` or `FlatTop` orientation.
4. Choose dense or sparse storage independently.

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

Hex-prism indices use axial coordinates in the XZ plane:

- `x` stores axial `q`
- `y` stores the vertical layer
- `z` stores axial `r`

Orientation changes the deterministic axial-to-world projection. It is not a
renderer setting.

## Sparse grids

Sparse storage uses normalized bounds as an address space but creates only the
configured physical voxels. It works well for islands, rooms, irregular maps,
or large regions with a small physical footprint.

In the Inspector:

1. Set **Storage Kind** to `Sparse`.
2. Add **Configured Voxels**.
3. Use non-negative topology-local indices.
4. Keep every index inside the normalized dimensions.

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

A missing sparse address is intentional absence. Core voxel lookup, tracing,
blockers, occupants, partitions, neighbors, and diagnostics preserve that
distinction.

## Scan-cell size and world lookup tuning

`scanCellSize` groups physical voxels for occupant queries; it is measured in
voxels, not world units. `GridConfigurationSaver.SpatialGridCellSize` separately
configures ordinary top-level grid lookup when the scene world is rebuilt.
Oversized core grids are routed automatically, so this value does not need to
match the largest streamed region.

## Serialization boundaries

- Keep FixedMathSharp values in their real types; do not create raw-value Unity
  mirrors.
- Use SwiftCollections-Unity `SerializedSwift*` adapters for persisted
  collection fields, then consume their live collections through `.Runtime`.
- Use GridForge runtime identity types only for their documented runtime
  lifetime. World, grid, blocker, and occupant tokens are not content IDs.
- Arrays and `IEnumerable<T>` remain appropriate at explicit API and conversion
  boundaries.

## Next steps

- [Create the scene-owned world](getting-started.md)
- [Add blockers and diagnostic tools](scene-tools.md)
- [Read core topology and sparse-storage guidance](https://github.com/mrdav30/GridForge/wiki/Sparse-Grid-Storage)
