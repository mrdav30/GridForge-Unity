---
title: GridForge for Unity
description: Unity authoring, scene, diagnostics, blocker, logging, sample, and API guidance for GridForge.
---

<div class="gf-hero">
  <p class="gf-kicker">GRIDFORGE AT THE UNITY BOUNDARY</p>
  <h1>Author worlds. See what the grid sees.</h1>
  <p>Bring deterministic GridForge worlds into Unity with serialized
  configurations, explicit scene ownership, authored blockers, topology-aware
  gizmos, trace visualization, logging, and samples.</p>
  <div class="gf-actions">
    <a href="guides/getting-started.md">Get started</a>
    <a href="xref:GridForge">Browse the Unity API</a>
  </div>
</div>

## Choose one variant

<div class="gf-card-grid">
  <div class="gf-card">
    <h3><a href="https://github.com/mrdav30/GridForge-Unity/tree/v10.0.0/com.mrdav30.gridforge">Standard</a></h3>
    <p>Unity authoring, scene tools, diagnostics, samples, and the Standard
    MemoryPack-enabled dependency family.</p>
  </div>
  <div class="gf-card">
    <h3><a href="https://github.com/mrdav30/GridForge-Unity/tree/v10.0.0/com.mrdav30.gridforge.lean">Lean</a></h3>
    <p>The same Unity-facing API without the MemoryPack runtime dependency
    chain.</p>
  </div>
  <div class="gf-card">
    <h3><a href="https://mrdav30.github.io/GridForge/">Core library</a></h3>
    <p>Explore the engine-agnostic world, topology, storage, query, blocker,
    occupant, and diagnostic contracts beneath the adapters.</p>
  </div>
</div>

Install exactly one variant. Both require Unity 6.3 LTS (6000.3) or newer; CI currently
tests Unity 6000.3.9f1.

## Add Unity where Unity owns the work

<div class="gf-card-grid">
  <div class="gf-card">
    <h3><a href="xref:GridForge.Unity.GridWorldComponent">Scene-owned worlds</a></h3>
    <p>Give each runtime world an explicit MonoBehaviour owner with deliberate
    create, reset, and dispose behavior.</p>
  </div>
  <div class="gf-card">
    <h3><a href="xref:GridForge.Configuration.SerializableGridConfiguration">Grid authoring</a></h3>
    <p>Persist rectangular, hex, dense, and sparse configuration intent in
    scenes and prefabs.</p>
  </div>
  <div class="gf-card">
    <h3><a href="xref:GridForge.Utility.GridDebugger">Diagnostics</a></h3>
    <p>Draw physical cells, sparse holes, topology geometry, and trace coverage
    without duplicating grid math in Unity.</p>
  </div>
  <div class="gf-card">
    <h3><a href="xref:GridForge.Blockers.BlockerComponent">Scene blockers</a></h3>
    <p>Resolve deterministic blocked regions from manual bounds, transforms,
    colliders, or renderers.</p>
  </div>
</div>

## Resources

- [Getting started](guides/getting-started.md)
- [Grid authoring](guides/grid-authoring.md)
- [Scene tools and diagnostics](guides/scene-tools.md)
- [GridForge core guide and API](https://mrdav30.github.io/GridForge/)
- [FixedMathSharp core guide and API](https://mrdav30.github.io/FixedMathSharp/)
- [SwiftCollections core guide and API](https://mrdav30.github.io/SwiftCollections/)
- [Package source, issues, and releases](https://github.com/mrdav30/GridForge-Unity)

The API reference is generated only from canonical adapter source under
`Build/Base/Runtime`. Core grid behavior remains owned by GridForge, fixed-point
math by FixedMathSharp, and collection behavior by SwiftCollections.
