# GridForge for Unity Lean

Lean Unity package host for GridForge v6.

Install:
`https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge.lean`

This package matches the default Unity integration but omits the `MemoryPack`
dependency chain.

The included editor repair tool will attempt to add the matching lean
dependencies for you. If Unity does not resolve them automatically, install:

- `https://github.com/mrdav30/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp.lean`
- `https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.lean`

## GridWorld Setup

GridForge v6 removed the process-wide `GlobalGridManager`. Unity scenes should
now own an explicit `GridWorld` through `GridWorldComponent`.

Typical scene setup:

1. Add `GridWorldComponent` to the scene object that should own the world.
2. Add `GridConfigurationSaver` to author grid bounds and voxel size data.
3. Call `GridConfigurationSaver.EarlyApply(world)` after creating the world, or
   use the sample `SceneGridManager` component which does that bootstrap for
   you.
4. Point `BlockerComponent`, `GridDebugger`, and `GridTracerTests` at the
   intended `GridWorldComponent` when using multiple worlds in one scene.

## Core Usage

```csharp
using FixedMathSharp;
using GridForge.Blockers;
using GridForge.Configuration;
using GridForge.Grids;

GridWorld world = new GridWorld(Fixed64.One, 50);

GridConfiguration config = new GridConfiguration(
    new Vector3d(-10, 0, -10),
    new Vector3d(10, 0, 10));

world.TryAddGrid(config, out ushort gridIndex);

Vector3d queryPosition = new Vector3d(5, 0, 5);
if (world.TryGetGridAndVoxel(queryPosition, out VoxelGrid grid, out Voxel voxel))
{
    UnityEngine.Debug.Log($"Voxel at {queryPosition} is {(voxel.IsOccupied ? "occupied" : "empty")}");
}

BoundingArea blockArea = new BoundingArea(new Vector3d(3, 0, 3), new Vector3d(5, 0, 5));
BoundsBlocker blocker = new BoundsBlocker(world, blockArea, true, false);
blocker.ApplyBlockage();
```

## Query Helpers

```csharp
using FixedMathSharp;
using GridForge.Grids;
using GridForge.Spatial;

if (world.TryGetGridAndVoxel(queryPosition, out _, out Voxel voxel))
{
    PathPartition partition = new PathPartition();
    partition.Setup(voxel.WorldIndex);
    voxel.AddPartition(partition);
}

Vector3d scanCenter = new Vector3d(0, 0, 0);
Fixed64 scanRadius = (Fixed64)5;
foreach (IVoxelOccupant occupant in GridScanManager.ScanRadius(world, scanCenter, scanRadius))
{
    UnityEngine.Debug.Log($"Found occupant at {occupant.WorldPosition}");
}
```

## Debugging Tools

- `GridDebugger` visualizes grids, voxels, and selected cells.
- `GridTracerTests` visualizes traced voxel coverage.
- `BlockerComponent` plus its custom inspector provides scene-authored blockers.

Unity compatibility: `2022.3+`
