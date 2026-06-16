# GridForge For Unity Lean

Lean Unity adapter package for GridForge.

Install through Unity Package Manager:

```text
https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge.lean
```

Install this package when you want the same Unity integration as the standard
package without the `MemoryPack` dependency chain. Prefer it for Burst AOT
projects or projects that provide their own serialization stack. Do not install
it with `com.mrdav30.gridforge`.

## Dependencies

On first editor load, the included dependency bootstrapper attempts to add the
required lean git dependencies to `Packages/manifest.json`. If Unity does not
resolve them automatically, use `Tools > GridForge.Lean > Repair Dependencies`
or install these URLs manually:

```text
https://github.com/mrdav30/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp.lean
https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.lean
https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.fixedmathsharp.lean
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

The package sample named `Demo Scene` shows the workflows in Unity:
rectangular grids, hex grids, sparse grids, diagnostics, blockers, tracing, and
optional Unity logging.

## Docs

- Unity guide:
  [../.docs/wiki/GridForge-Unity-User-Guide.md](../.docs/wiki/GridForge-Unity-User-Guide.md)
- Package selection and maintenance:
  [../README.md](../README.md)
- Core GridForge wiki:
  [Getting Started](https://github.com/mrdav30/GridForge/wiki/Getting-Started),
  [Common Workflows](https://github.com/mrdav30/GridForge/wiki/Common-Workflows),
  [Sparse Grid Storage](https://github.com/mrdav30/GridForge/wiki/Sparse-Grid-Storage),
  [Grid Diagnostics and Geometry](https://github.com/mrdav30/GridForge/wiki/Grid-Diagnostics-and-Geometry),
  [Diagnostics and Logging](https://github.com/mrdav30/GridForge/wiki/Diagnostics-and-Logging)

Unity compatibility: `2022.3+`
