# GridForge For Unity

Default Unity adapter package for GridForge v7.

Install through Unity Package Manager:

```text
https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge
```

Install this package when you want the standard GridForge Unity integration and
the default dependency chain. Do not install it with
`com.mrdav30.gridforge.lean`.

## Dependencies

The included editor repair tool attempts to add the required packages on import.
If Unity does not resolve them automatically, use
`Tools > GridForge > Repair Dependencies` or install these URLs manually:

```text
https://github.com/mrdav30/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp
https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections
https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.fixedmathsharp
```

## Quick Setup

1. Add `GridWorldComponent` to the scene object that owns the runtime world.
2. Add `GridConfigurationSaver` to author saved grid configurations.
3. Add rectangular, hex, dense, or sparse configurations in the inspector.
4. Apply the saved configurations at startup with
   `GridConfigurationSaver.EarlyApply(world)`, or use the sample
   `SceneGridManager`.
5. Assign the intended `GridWorldComponent` on `BlockerComponent`,
   `GridDebugger`, and `Grid Trace Visualizer` when a scene has multiple worlds.

The package sample named `Demo Scene` shows the v7 workflows in Unity:
rectangular grids, hex grids, sparse grids, diagnostics, blockers, tracing, and
optional Unity logging.

## Docs

- Unity v7 guide:
  [../.docs/wiki/GridForge-Unity-v7-User-Guide.md](../.docs/wiki/GridForge-Unity-v7-User-Guide.md)
- Package selection and maintenance:
  [../README.md](../README.md)
- Core GridForge wiki:
  [Getting Started](https://github.com/mrdav30/GridForge/wiki/Getting-Started),
  [Common Workflows](https://github.com/mrdav30/GridForge/wiki/Common-Workflows),
  [Sparse Grid Storage](https://github.com/mrdav30/GridForge/wiki/Sparse-Grid-Storage),
  [Grid Diagnostics and Geometry](https://github.com/mrdav30/GridForge/wiki/Grid-Diagnostics-and-Geometry),
  [Diagnostics and Logging](https://github.com/mrdav30/GridForge/wiki/Diagnostics-and-Logging)

Unity compatibility: `2022.3+`
