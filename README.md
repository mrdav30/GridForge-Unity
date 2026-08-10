# GridForge-Unity

[![Build](https://github.com/mrdav30/GridForge-Unity/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/mrdav30/GridForge-Unity/actions/workflows/build-and-test.yml)
[![Discord](https://img.shields.io/badge/discord-join%20community-5865F2?logo=discord&logoColor=white)](https://discord.gg/mhwK2QFNBA)

Unity Package Manager host for the deterministic, engine-agnostic
[GridForge](https://github.com/mrdav30/GridForge) library. This repository owns
the Unity adapters, samples, inspectors, package metadata, and embedded runtime
DLLs.

## Packages

Install one package variant only. The variants expose the same Unity-facing
GridForge API surface and should not be installed together.

| Package                      | Use it when                                                                                                    | Install URL                                                                       |
| ---------------------------- | -------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| `com.mrdav30.gridforge`      | You want the standard package and its default dependency chain.                                                | `https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge`      |
| `com.mrdav30.gridforge.lean` | You want the same Unity integration without the `MemoryPack` dependency chain, such as for Burst AOT projects. | `https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge.lean` |

Both variants target Unity `2022.3+`.

## Start Here

1. Install exactly one package URL through Unity Package Manager.
2. On first editor load, let the package dependency bootstrapper add the
   matching FixedMathSharp-Unity and SwiftCollections-Unity git dependencies to
   `Packages/manifest.json`.
3. If dependency resolution needs a manual nudge, use:
   - `Tools > GridForge > Repair Dependencies` for the standard package.
   - `Tools > GridForge.Lean > Repair Dependencies` for the lean package.
4. Import the package sample named `Demo Scene`.
5. Use the
   [hosted Unity guide and API reference](https://mrdav30.github.io/GridForge-Unity/).

## Authoring

- `GridWorldComponent` owns an explicit scene `GridWorld`.
- `GridConfigurationSaver` authors rectangular, hex, dense, and sparse grid
  configurations.
- `BlockerComponent` creates scene-authored blockers from transforms, colliders,
  renderers, or manual fixed bounds.
- `GridDebugger` visualizes active grids through `GridForge.Diagnostics`.
- `Grid Trace Visualizer` displays topology-aware traced voxel coverage.
- `GridForgeUnityLogger` optionally forwards core GridForge logs into Unity.

Unity adapts the core library; it does not replace the core ownership model. In
multi-world scenes, assign the intended `GridWorldComponent` on blockers,
debuggers, and trace tools instead of relying on automatic scene lookup.

## Documentation

- [Hosted Unity guide and API reference](https://mrdav30.github.io/GridForge-Unity/)
- Standard package README:
  [com.mrdav30.gridforge/README.md](com.mrdav30.gridforge/README.md)
- Lean package README:
  [com.mrdav30.gridforge.lean/README.md](com.mrdav30.gridforge.lean/README.md)
