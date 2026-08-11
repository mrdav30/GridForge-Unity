---
uid: GridForge.Unity.GettingStarted
title: Getting started with GridForge for Unity
description: Install one GridForge Unity package, let its dependencies settle, create a scene-owned world, and import the Demo Scene.
---

# Getting started

GridForge core owns deterministic grid behavior. The Unity companion owns scene
lifetime, serialized authoring data, inspectors, gizmos, logging adapters, and
samples.

This guide takes you from a pinned package install to an active scene-owned
world. For engine-neutral behavior, use the
[GridForge core wiki](https://github.com/mrdav30/GridForge/wiki).

## Requirements

- Unity 2022.3 or newer
- exactly one GridForge package variant
- Git available to Unity Package Manager

The repository currently tests both variants with Unity 6000.5.0f1.

## Install one variant

In **Window → Package Manager**, choose **+ → Install package from git URL…**.

Standard package with the MemoryPack-enabled dependency family:

```text
https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge#v9.0.0
```

Lean package without the MemoryPack runtime dependency chain:

```text
https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge.lean#v9.0.0
```

Do not install both. They define overlapping GridForge assemblies.

## Let dependencies settle

On first editor load, a small bootstrap assembly adds the matching pinned
FixedMathSharp-Unity and SwiftCollections-Unity packages to
`Packages/manifest.json`, then asks Unity Package Manager to resolve again.

The first import may need one additional resolve and compile cycle. If the
manifest was edited outside Unity or resolution was interrupted, use the
matching menu:

- **Tools → GridForge → Repair Dependencies**
- **Tools → GridForge.Lean → Repair Dependencies**

The final compiler pass should be clean after the dependency entries appear in
the manifest.

## Create a scene-owned world

The simplest setup uses one GameObject for the world and its authored grid
configurations:

1. Add <xref:GridForge.Unity.GridWorldComponent>.
2. Add <xref:GridForge.Configuration.GridConfigurationSaver>.
3. Add one or more saved configurations in the Inspector.
4. Let the world component initialize on `Awake`, then apply the saved
   configurations.

For an explicit bootstrap component:

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
        GridWorld world = _worldComponent.RebuildWorld(
            _configurationSaver.SpatialGridCellSize);

        _configurationSaver.EarlyApply(world);
    }
}
```

`GridWorldComponent` owns disposal by default. If your host disables that
behavior, it also owns calling `DisposeWorld()` at the end of the world lifetime.

## Import the sample

Select the installed package in Package Manager, open its **Samples** tab, and
import **Demo Scene**. The sample demonstrates:

- rectangular and hex-prism grids
- dense and sparse storage
- scene-owned world setup
- blocker authoring
- physical-cell and sparse-hole diagnostics
- topology-aware line tracing
- optional Unity logging

The sample's `SceneGridManager` rebuilds the world and switches between the
included workflow demonstrations.

## Multiple worlds

GridForge intentionally supports more than one isolated world in a process or
scene. Assign the intended `GridWorldComponent` explicitly on blockers,
debuggers, and trace visualizers when automatic local-or-parent resolution would
be ambiguous.

## Next steps

- [Author rectangular, hex, dense, and sparse grids](grid-authoring.md)
- [Use blockers, diagnostics, tracing, and logging](scene-tools.md)
- [Learn the GridForge core model](https://github.com/mrdav30/GridForge/wiki/Core-Concepts)
