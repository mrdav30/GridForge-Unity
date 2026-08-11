<p align="center">
  <img src=".assets/icon.png" alt="GridForge icon" width="96" height="96">
</p>

# GridForge for Unity

[![Build](https://github.com/mrdav30/GridForge-Unity/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/mrdav30/GridForge-Unity/actions/workflows/build-and-test.yml)
[![Unity 2022.3+](https://img.shields.io/badge/Unity-2022.3%2B-222c37?logo=unity)](https://unity.com/releases/editor/whats-new/2022.3.0)
[![API](https://img.shields.io/badge/docs-Unity_API-00a9d6)](https://mrdav30.github.io/GridForge-Unity/)
[![Discord](https://img.shields.io/badge/discord-join%20community-5865F2?logo=discord&logoColor=white)](https://discord.gg/mhwK2QFNBA)

**Scene authoring, inspectors, diagnostics, and samples for the deterministic
[GridForge](https://github.com/mrdav30/GridForge) voxel-world library.**

Keep grid behavior in the engine-agnostic core. Use this package host where
Unity owns the boundary: serialized configurations, scene-owned worlds,
topology-aware gizmos, authored blockers, logging, and sample workflows.

Requires Unity **2022.3 or newer**. Repository CI currently validates Unity
**6000.5.0f1**.

## Choose one package

Install exactly one variant; they expose overlapping GridForge assemblies.

| Package | Use it when |
| --- | --- |
| `com.mrdav30.gridforge` | You want the Standard dependency chain with MemoryPack support. |
| `com.mrdav30.gridforge.lean` | You want the same Unity-facing API without the MemoryPack runtime dependency. |

In **Window → Package Manager**, choose **+ → Install package from git URL…**
and use one pinned URL:

```text
https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge#v9.0.0
```

```text
https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge.lean#v9.0.0
```

On first import, the package adds the matching FixedMathSharp-Unity and
SwiftCollections-Unity dependencies, then asks Unity Package Manager to resolve
again. If that process was interrupted, use **Tools → GridForge → Repair
Dependencies** or the matching **GridForge.Lean** menu.

## Build your first scene

1. Add `GridWorldComponent` to the GameObject that owns the runtime world.
2. Add `GridConfigurationSaver` and author a rectangular or hex, dense or sparse
   configuration.
3. From a small bootstrap `MonoBehaviour`, apply the saved configurations after
   the world component initializes:

   ```csharp
   _configurationSaver.EarlyApply(
       _gridWorldComponent.RebuildWorld(_configurationSaver.SpatialGridCellSize));
   ```

   The [getting-started guide](https://mrdav30.github.io/GridForge-Unity/guides/getting-started.html)
   includes the complete component.
4. Import the package sample named **Demo Scene** to explore the complete setup.
5. Add `GridDebugger`, `GridTraceVisualizer`, or blocker components when you
   need scene visualization and authoring.

`GridWorldComponent` preserves the explicit ownership model from GridForge
core. In a scene with multiple worlds, assign the intended component on each
debugger, blocker, and trace tool.

## What the adapter owns

- Inspector-friendly rectangular, hex, dense, and sparse grid configuration
- Direct FixedMathSharp field serialization through FixedMathSharp-Unity
- Scene-owned `GridWorld` lifecycle
- Transform-, collider-, renderer-, and manual-bounds blocker authoring
- Physical-cell and sparse-hole diagnostic gizmos
- Topology-aware trace visualization
- Optional forwarding from `GridForgeLogger` into Unity logs
- A Demo Scene covering the supported workflows

## Learn more

- [Getting started](https://mrdav30.github.io/GridForge-Unity/guides/getting-started.html)
- [Grid authoring](https://mrdav30.github.io/GridForge-Unity/guides/grid-authoring.html)
- [Scene tools and diagnostics](https://mrdav30.github.io/GridForge-Unity/guides/scene-tools.html)
- [Unity API reference](https://mrdav30.github.io/GridForge-Unity/api/GridForge.html)
- [GridForge core guide and API](https://mrdav30.github.io/GridForge/)
- [Contributing and package maintenance](CONTRIBUTING.md)

The Standard and Lean package folders each own their manifest, asmdefs, embedded
core DLL, install README, and serialized sample assets. Shared adapter source is
maintained once under `Build/Base` and synchronized into both packages.
