# GridForge-Unity

[![Build](https://github.com/mrdav30/GridForge-Unity/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/mrdav30/GridForge-Unity/actions/workflows/build-and-test.yml)

Unity Package Manager host for [GridForge](https://github.com/mrdav30/GridForge).

GridForge remains an engine-agnostic deterministic grid library. This repository
contains Unity adapters, samples, inspectors, package metadata, and embedded
GridForge runtime DLLs for Unity projects.

## Packages

Install one package variant only. The variants expose the same Unity-facing
GridForge API surface and should not be installed together.

| Package | Use it when | Install URL |
| --- | --- | --- |
| `com.mrdav30.gridforge` | You want the standard package and the default dependency chain. | `https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge` |
| `com.mrdav30.gridforge.lean` | You want the same Unity integration without the `MemoryPack` dependency chain. Prefer this for Burst AOT projects or custom serialization stacks. | `https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge.lean` |

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
5. Read the Unity guide:
   [.docs/wiki/GridForge-Unity-User-Guide.md](.docs/wiki/GridForge-Unity-User-Guide.md).

## What Ships

- `GridWorldComponent` owns an explicit scene `GridWorld`.
- `GridConfigurationSaver` authors rectangular, hex, dense, and sparse grid
  configurations.
- `BlockerComponent` creates scene-authored blockers from transforms,
  colliders, renderers, or manual fixed bounds.
- `GridDebugger` visualizes active grids through `GridForge.Diagnostics`.
- `Grid Trace Visualizer` displays topology-aware traced voxel coverage.
- `GridForgeUnityLogger` optionally forwards core GridForge logs into Unity.

Unity adapts the core library; it does not replace the core ownership model. In
multi-world scenes, assign the intended `GridWorldComponent` on blockers,
debuggers, and trace tools instead of relying on automatic scene lookup.

## Docs

- Unity guide:
  [.docs/wiki/GridForge-Unity-User-Guide.md](.docs/wiki/GridForge-Unity-User-Guide.md)
- Package maintenance:
  [.docs/wiki/GridForge-Unity-Package-Maintenance.md](.docs/wiki/GridForge-Unity-Package-Maintenance.md)
- Standard package README:
  [com.mrdav30.gridforge/README.md](com.mrdav30.gridforge/README.md)
- Lean package README:
  [com.mrdav30.gridforge.lean/README.md](com.mrdav30.gridforge.lean/README.md)
- Core wiki:
  [Getting Started](https://github.com/mrdav30/GridForge/wiki/Getting-Started),
  [Common Workflows](https://github.com/mrdav30/GridForge/wiki/Common-Workflows),
  [Sparse Grid Storage](https://github.com/mrdav30/GridForge/wiki/Sparse-Grid-Storage),
  [Grid Diagnostics and Geometry](https://github.com/mrdav30/GridForge/wiki/Grid-Diagnostics-and-Geometry),
  [Diagnostics and Logging](https://github.com/mrdav30/GridForge/wiki/Diagnostics-and-Logging)

## Maintainer Quick Path

Shared managed source belongs in `Build/Base`. The package folders keep their
own package metadata, plugins, asmdefs, samples, and Unity-generated `.meta`
files.

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\test-update-unity-package-versions.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\update-unity-package-versions.ps1 -ValidateOnly
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\test-gridforge-package-sync.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\run-gridforge-unity-editmode-tests.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\sync-gridforge-unity-packages.ps1 -WhatIf
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\export-gridforge-unity-packages.ps1 -WhatIf
git diff --check
```
