---
uid: GridForge.Unity.SceneTools
title: GridForge scene tools and diagnostics
description: Use Unity blockers, diagnostic gizmos, trace visualization, and logging without duplicating GridForge core behavior.
---

# Scene tools and diagnostics

GridForge-Unity turns engine-neutral core descriptors into Unity authoring and
visualization. The adapters convert at the boundary; topology, lookup,
coverage, blocker state, and deterministic math remain in GridForge core.

## Diagnostic grid gizmos

<xref:GridForge.Utility.GridDebugger> draws cells through
`GridForge.Diagnostics` instead of recreating rectangular-only loops in Unity.

Useful settings include:

- **Show Grid** — draw cells in Play mode.
- **Debug All Grids** — inspect every active grid in the resolved world.
- **Filter Topology Kind** — restrict the view to rectangular or hex grids.
- **Filter Storage Kind** — restrict the view to dense or sparse grids.
- **Address Mode** — draw physical cells, missing sparse addresses, or both.
- **Limit Query Bounds** — inspect only a bounded world-space region.
- **Max Cells** — cap diagnostic work and protect the editor.
- **Allow Full Sparse Address Scan** — explicitly opt in to an unbounded sparse
  address-space pass.

The read-only query status fields report whether the pass completed, hit its
cell budget, found an inactive world, or required explicit sparse query bounds.

## Trace visualization

<xref:GridForge.Utility.GridTraceVisualizer> displays topology-aware line
coverage in the Scene view.

Assign start and end transforms, then choose:

- `World3D` for a full three-dimensional trace
- `XzLayer` for a flat XZ trace locked to one deterministic world-Y layer

Use it to inspect rectangular, hex, dense, sparse, and mixed-grid coverage.
Because it consumes `GridTracer`, the visualizer follows the same core snapping,
candidate selection, duplicate suppression, and physical-voxel rules as runtime
queries.

## Scene-authored blockers

<xref:GridForge.Blockers.BlockerComponent> creates 3D world-space blockers.
<xref:GridForge.Blockers.BlockerComponent2d> creates XZ-area blockers on one
world-Y layer.

Depending on the component and mode, bounds can come from:

- manual fixed bounds
- transform position and scale
- collider bounds
- renderer bounds

GridForge core owns coverage, stacking, obstacle tokens, and response to grid
changes. The Unity component owns only scene references and conversion into the
fixed-point blocker contract.

In multi-world scenes, assign the intended `GridWorldComponent` explicitly.

## Unity logging

<xref:GridForge.Utility.GridForgeUnityLogger> optionally forwards the core
`GridForgeLogger` channel into Unity logs.

```csharp
using GridForge.Utility;
using Chronicler.Diagnostics;
using UnityEngine;

public sealed class GridLogging : MonoBehaviour
{
    private GridForgeUnityLogger _logger;

    private void Awake()
    {
        _logger = gameObject.AddComponent<GridForgeUnityLogger>();
        _logger.MinimumLevel = DiagnosticLevel.Warning;
        _logger.EnableLogging();
    }
}
```

Only one `GridForgeUnityLogger` can own forwarding at a time. Disabling it
restores the previous core handler and minimum level. Logging is separate from
`GridForge.Diagnostics`, which supplies the cell and geometry descriptors used
by visual tools.

## Ownership checklist

- Put engine-neutral behavior in GridForge core.
- Put fixed-point primitives and conversions in FixedMathSharp or
  FixedMathSharp-Unity.
- Put collection behavior and serialized collection adapters in
  SwiftCollections or SwiftCollections-Unity.
- Keep one explicit `GridWorldComponent` per intended scene world.
- Convert Unity `float`, `Vector2`, `Vector3`, `Bounds`, `Transform`, collider,
  and renderer data only at the adapter boundary.
- Do not retain pooled core query results beyond their documented lifetime.

## Next steps

- [Install and create a scene-owned world](getting-started.md)
- [Author rectangular, hex, dense, and sparse grids](grid-authoring.md)
- [Read the core diagnostics guide](https://github.com/mrdav30/GridForge/wiki/Grid-Diagnostics-and-Geometry)
