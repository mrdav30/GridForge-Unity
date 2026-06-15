# GridForge v7 Unity Package Migration Battle Plan

> **For agentic workers:** Use this as the strategic migration brief. Before implementing any phase, create a focused task plan for that phase and work through `Build/Base` first, then sync both package variants.

**Goal:** Bring `GridForge-Unity` up to the GridForge v7 public model and harden the Unity package experience around topology-aware grids, sparse storage, diagnostics, samples, docs, and release validation.

**Architecture:** Keep GridForge core engine-agnostic and treat Unity as an adapter layer. Unity authoring components should produce explicit `GridConfiguration` data, Unity debugging should consume `GridForge.Diagnostics` descriptors, and package variants should remain generated from shared managed source wherever practical.

**Tech Stack:** Unity 2022.3+ package source, Unity 6000.3.9f1 CI smoke project, GridForge v7 DLL/XML, FixedMathSharp v5 Unity packages, SwiftCollections v5 Unity packages, NUnit EditMode tests, PowerShell package scripts.

---

## Status

- Date: 2026-06-15.
- Current Unity package branch: `develop`.
- Current Unity package migration baseline reviewed: `e10602aad40e98a6a9602833f7086a0ca833a941`.
- Current `HEAD` reviewed: `420a93e` (`task: migrate to GridForge v7`).
- Core GridForge source reviewed: `F:\gamedevrepos\GridForge` on `develop` at `866c91f`.
- Core feature docs reviewed from `F:\gamedevrepos\GridForge\docs\feature-work\done`, excluding `gridWorldRefactorPlan.md` per request.
- Core wiki docs reviewed from `F:\gamedevrepos\GridForge\docs\wiki`.
- This is a planning document only. It intentionally does not change runtime code.

## Source Material

- Core v7 docs:
  - `2026-06-11-feature-roadmap-overview.md`
  - `2026-06-11-vector2d-query-api-plan.md`
  - `2026-06-11-sparse-voxel-grid-plan.md`
  - `2026-06-11-hex-prism-grid-plan.md`
  - `2026-06-13-hex-prism-follow-up-plan.md`
  - `2026-06-14-grid-diagnostics-geometry-plan.md`
- Core wiki pages most relevant to Unity:
  - `Getting-Started.md`
  - `Common-Workflows.md`
  - `Architecture-Overview.md`
  - `VoxelGrid-and-Voxel-Model.md`
  - `Sparse-Grid-Storage.md`
  - `Grid-Diagnostics-and-Geometry.md`
  - `Diagnostics-and-Logging.md`
  - `GridTracer-and-Coverage.md`
  - `Testing-and-Benchmarking.md`
- Unity package areas reviewed:
  - `Build/Base`
  - `com.mrdav30.gridforge`
  - `com.mrdav30.gridforge.lean`
  - `.assets/scripts`
  - `.github/workflows/build-and-test.yml`
  - `Tests/EditMode`

## Core v7 Facts The Unity Package Must Align With

- `GridWorld` remains the explicit runtime owner. Unity should continue to expose a scene-owned `GridWorldComponent`.
- `GridConfiguration` now carries topology and storage intent:
  - `GridTopologyKind.RectangularPrism`
  - `GridTopologyKind.HexPrism`
  - `GridTopologyMetrics.Rectangular(...)`
  - `GridTopologyMetrics.Hex(radius, layerHeight, orientation)`
  - `GridStorageKind.Dense`
  - `GridStorageKind.Sparse`
- World-level voxel-size ownership is gone. Per-grid cell geometry lives in `GridConfiguration.TopologyMetrics`.
- Sparse grids use bounds as an address space but only configured voxels are physical. Missing sparse cells are not empty voxels.
- `VoxelGrid.EnumerateVoxels()` and `ConfiguredVoxelCount` are the storage-neutral physical traversal/counting surfaces.
- Hex-prism grids use axial coordinates in the XZ plane: `VoxelIndex.x = q`, `VoxelIndex.y = layer`, `VoxelIndex.z = r`.
- `GridDiagnostics` is the adapter-facing layer for tools and overlays:
  - `GridDiagnostics.VisitCells(...)`
  - `GridDiagnostics.GetCellsInto(...)`
  - `GridDiagnosticQuery`
  - `GridDiagnosticAddressMode`
  - `GridDiagnosticCellState`
  - `GridDiagnosticGeometry`
  - `GridDiagnosticSession`
- `GridDiagnosticGeometry` emits fixed-point vertices and edge topology for rectangular and hex cells. Unity owns conversion to `Vector3`, gizmos, meshes, colors, and editor UI.
- Logging should route through `GridForgeLogger`, with Unity deciding how to forward diagnostics into `Debug.Log`, `Debug.LogWarning`, or `Debug.LogError`.

## Current Unity Package Findings

### Already Started

- Both package manifests now report `7.0.0`.
- Both package folders contain updated `GridForge.dll`, `.pdb`, and `.xml` files exposing v7 topology, storage, diagnostics, and logging APIs.
- Dependency version config targets FixedMathSharp Unity v5.0.0 and SwiftCollections Unity v5.0.0.
- PowerShell wrappers exist for version sync, package sync, and `.unitypackage` export.
- Unity CI scaffolding exists for standard and lean package EditMode test matrices.

### High-Priority Gaps

- `README.md`, `com.mrdav30.gridforge/README.md`, and `com.mrdav30.gridforge.lean/README.md` still describe GridForge v6 and contain v6-era examples such as `new GridWorld(Fixed64.One, 50)`.
- The package readmes do not cover hex-prism grids, sparse storage, `GridDiagnostics`, or `GridForgeLogger`.
- `SerializableGridConfiguration` only stores bounds and scan-cell size. It cannot author topology kind, topology metrics, storage kind, or sparse configured cells.
- `GridConfigurationSaver` still exposes `_voxelSize` and draws editor cubes by looping bounds directly. v7 cell geometry is per-grid topology metrics, and dense loops are the wrong debug/tooling path for sparse and hex.
- `GridWorldComponent` still serializes `_voxelSize`, but v7 `GridWorld` no longer owns voxel size.
- `GridDebugger` loops `Width`, `Height`, and `Length`, calls `TryGetVoxel(x, y, z)`, and draws unit cubes. It cannot correctly visualize sparse holes, sparse physical-only grids, hex geometry, or topology-specific cell metrics.
- `GridTracerTests` is still a rectangular/cube trail tool and should become a topology-aware trace visualizer.
- `Build/Base` and both package copies have drifted in `GridDebugger.cs` and `GridTracerTests.cs`. Package copies of `GridTracerTests.cs` introduce `FillSize` without assigning it, which makes the drawn cubes zero-sized.
- `Tests/EditMode` contains only an asmdef. The CI workflow currently copies `Tests/EditMode/*.cs`; without test files this will fail before Unity runs tests.
- `.assets/scripts/test-update-unity-package-versions.ps1` currently fails because its test config does not define a `packages` array.
- `GitDependencyInstaller.cs` logs a mojibake arrow (`â†’`) and should be checked against Unity profile support for `System.Text.Json`.
- Package manifests declare no UPM dependencies and rely on editor-time manifest mutation. That path needs explicit validation and friendlier failure behavior.

### Design Constraint

`Build/Base` is the shared managed-code source of truth. Most implementation should happen there first, then be copied to both package variants through the package sync path. Package-specific source should remain limited to package metadata, plugin assets, asmdefs, installers, samples, and serialized Unity assets that need separate GUIDs.

## Non-Goals

- Do not move GridForge core behavior into Unity.
- Do not add Unity types to GridForge core APIs.
- Do not create a Unity-only grid model that competes with `GridWorld`, `VoxelGrid`, `Voxel`, `GridConfiguration`, or `GridDiagnostics`.
- Do not make missing sparse cells behave like empty dense cells.
- Do not use floating-point math before the Unity adapter boundary.
- Do not require users to install both package variants together.
- Do not preserve stale v6 docs or serialized fields as public concepts after migration; preserve serialized data only long enough to migrate safely.

## Phase 0: Stabilize The Migration Baseline

**Goal:** Make the current v7 migration state trustworthy before changing APIs.

**Files:**

- Review: `com.mrdav30.gridforge/Plugins/GridForge.dll`
- Review: `com.mrdav30.gridforge.lean/Plugins/GridForge.dll`
- Review: `com.mrdav30.gridforge/Plugins/GridForge.xml`
- Review: `com.mrdav30.gridforge.lean/Plugins/GridForge.xml`
- Review: `.assets/unity-package-versions.json`
- Modify: `.assets/scripts/test-update-unity-package-versions.ps1`
- Modify: `.github/workflows/build-and-test.yml`
- Create: `Tests/EditMode/GridWorldComponentEditModeTests.cs`

**Work:**

- [ ] Confirm both standard and lean DLL/XML files were built from the intended GridForge v7 core commit.
- [ ] Add a small manifest file or doc note that records the core commit used for the embedded DLLs.
- [ ] Fix `.assets/scripts/test-update-unity-package-versions.ps1` so its generated fixture includes a `packages` array for standard and lean package manifests.
- [ ] Add at least one real EditMode test file so CI does not fail on `cp Tests/EditMode/*.cs`.
- [ ] Update CI setup to fail with a clear message if the package test source glob is empty.
- [ ] Run:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\test-update-unity-package-versions.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\update-unity-package-versions.ps1 -ValidateOnly
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\sync-gridforge-unity-packages.ps1 -WhatIf
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\export-gridforge-unity-packages.ps1 -WhatIf
git diff --check
```

**Exit Criteria:**

- Version self-tests pass.
- Version validation passes.
- Unity batch wrappers resolve the intended Unity project and editor.
- CI has at least one actual EditMode test source file per package matrix.
- No package docs claim v6 after this phase.

## Phase 1: Repair Shared Source Ownership And Package Sync

**Goal:** Make `Build/Base` and package copies deterministic before larger edits.

**Files:**

- Modify: `Build/Base/Runtime/Utility/Debugging/GridDebugger.cs`
- Modify: `Build/Base/Runtime/Utility/Debugging/GridTracerTests.cs`
- Modify: `com.mrdav30.gridforge/Runtime/Utility/Debugging/GridDebugger.cs`
- Modify: `com.mrdav30.gridforge/Runtime/Utility/Debugging/GridTracerTests.cs`
- Modify: `com.mrdav30.gridforge.lean/Runtime/Utility/Debugging/GridDebugger.cs`
- Modify: `com.mrdav30.gridforge.lean/Runtime/Utility/Debugging/GridTracerTests.cs`
- Modify: `Build/Editor/GridForgePackageSync.cs`

**Work:**

- [ ] Decide whether the package-copy drift in `GridDebugger.cs` and `GridTracerTests.cs` should be kept. Current evidence says it should not be kept.
- [ ] Re-sync package copies from `Build/Base` so the shared source is byte-equivalent except for `.meta` files.
- [ ] Add a package-sync validation command or test that compares non-meta managed files from `Build/Base` to both packages.
- [ ] Keep package-specific `.meta` GUIDs intact so existing samples and scenes do not lose script references.
- [ ] Add any new managed-code directories from later phases to `GridForgePackageSync.ManagedEntries`.

**Exit Criteria:**

- `Build/Base` is the only place future shared managed-code edits are made.
- Package copies are synchronized by tool rather than by manual edits.
- Sync validation catches source drift before release.

## Phase 2: Redesign Unity Grid Authoring For v7 Configuration

**Goal:** Let users author rectangular, hex, dense, and sparse grids through Unity without hiding GridForge's explicit configuration model.

**Files:**

- Modify: `Build/Base/Runtime/Configuration/SerializableGridConfiguration.cs`
- Modify: `Build/Base/Runtime/Configuration/GridConfigurationSaver.cs`
- Modify: `Build/Base/Editor/Configuration/Editor/EditorGridConfigurationSaver.cs`
- Modify: `Build/Base/Runtime/GridWorldComponent.cs`
- Create: `Build/Base/Runtime/Configuration/SerializableGridTopologyMetrics.cs`
- Create: `Build/Base/Runtime/Configuration/SerializableSparseVoxelSet.cs`
- Test: `Tests/EditMode/GridConfigurationAuthoringEditModeTests.cs`

**Public Authoring Shape:**

- `SerializableGridConfiguration`
  - bounds min
  - bounds max
  - scan cell size
  - topology kind
  - rectangular cell width
  - rectangular layer height
  - rectangular cell length
  - hex radius
  - hex layer height
  - hex orientation
  - storage kind
  - configured sparse indices
- `GridConfigurationSaver`
  - world spatial hash size remains world-level
  - per-grid topology metrics replace the old global voxel-size mental model
  - sparse configured cells are optional and only applied when storage is sparse

**Work:**

- [ ] Add topology and storage fields to `SerializableGridConfiguration`.
- [ ] Use `GridTopologyMetrics.Rectangular(...)` for rectangular configs.
- [ ] Use `GridTopologyMetrics.Hex(...)` for hex configs.
- [ ] Use `GridStorageKind.Dense` as the default storage kind.
- [ ] Use `GridStorageKind.Sparse` only when explicit configured sparse indices are supplied or the user intentionally creates an empty sparse grid.
- [ ] Remove `_voxelSize` from new authoring UI as a world-level concept.
- [ ] Preserve old serialized `_voxelSize` values by migrating them into rectangular topology metrics for existing scenes.
- [ ] Use `UnityEngine.Serialization.FormerlySerializedAs` for renamed serialized fields where Unity can migrate them safely.
- [ ] Add editor validation for positive rectangular width/layer/length and positive hex radius/layer height.
- [ ] In `EarlyApply`, call:
  - `world.TryAddGrid(config, out _)` for dense grids
  - `world.TryAddGrid(config, configuredVoxels, out _)` for sparse grids
- [ ] Log clear Unity warnings for invalid bounds, invalid metrics, invalid sparse indices, and failed grid registration.

**Tests:**

- [ ] Rectangular default authoring creates v7 default rectangular config.
- [ ] Old voxel-size authoring migrates to rectangular topology metrics.
- [ ] Rectangular independent X/Y/Z metrics round-trip through `ToGridConfiguration`.
- [ ] Pointy-top hex config round-trips.
- [ ] Flat-top hex config round-trips.
- [ ] Sparse rectangular configured indices are passed into `TryAddGrid`.
- [ ] Sparse hex configured axial indices are passed into `TryAddGrid`.
- [ ] Invalid scan cell size clamps or resolves to `GridConfiguration.DefaultScanCellSize`.
- [ ] Invalid metrics are rejected before world registration.

**Exit Criteria:**

- Users can create dense rectangular, dense hex, sparse rectangular, and sparse hex grids from the Inspector.
- Existing rectangular scenes migrate without losing bounds or intended cell size.
- The authoring UI uses v7 names: topology, topology metrics, storage, sparse configured cells.

## Phase 3: Rebuild Grid Debugging Around `GridForge.Diagnostics`

**Goal:** Replace dense rectangular cube loops with one topology-aware diagnostic adapter path.

**Files:**

- Modify: `Build/Base/Runtime/Utility/Debugging/GridDebugger.cs`
- Modify: `Build/Base/Editor/Utility/Debugging/Editor/EditorGridDebugger.cs`
- Create: `Build/Base/Runtime/Utility/Debugging/GridDiagnosticGizmoDrawer.cs`
- Create: `Build/Base/Runtime/Utility/Debugging/GridDiagnosticUnityVisitor.cs`
- Test: `Tests/EditMode/GridDebuggerDiagnosticsEditModeTests.cs`

**Debugger Capabilities:**

- Query all active grids or one grid index.
- Filter by topology kind.
- Filter by storage kind.
- Select diagnostic address mode:
  - physical cells only
  - physical plus missing sparse address cells
  - missing sparse address cells only
- Filter by cell states:
  - empty
  - occupied
  - blocked
  - boundary
  - partitioned
  - missing sparse address
- Limit query bounds and max cell count.
- Draw rectangular and hex cells through `GridDiagnosticGeometry`.
- Use `GridDiagnosticSession` to expose dirty changes and support future incremental rendering.

**Work:**

- [ ] Replace `Width`/`Height`/`Length` nested loops with `GridDiagnostics.VisitCells(...)`.
- [ ] Keep one reusable `GridDiagnosticScratch` per debugger component.
- [ ] Draw physical cells using `GridDiagnosticGeometry.WriteVertices(...)` and `GridDiagnosticGeometry.GetEdges(...)`.
- [ ] Convert `Vector3d` to Unity `Vector3` only in the drawing layer.
- [ ] Draw missing sparse address cells with a distinct color and alpha.
- [ ] Respect `GridDiagnosticQuery.MaxCells` and show query status in the inspector.
- [ ] Avoid per-cell managed allocations in the draw loop.
- [ ] Keep selected-cell resolution physical-only through `GridDiagnostics.TryResolvePhysicalCell(...)`.
- [ ] Add editor controls for topology, storage, address mode, bounds, max cells, and state filters.
- [ ] Keep old `VoxelFilterType` serialized values migrating into the new state filter model.

**Tests:**

- [ ] Dense rectangular diagnostic query draws eight-vertex cells.
- [ ] Dense hex diagnostic query draws twelve-vertex cells.
- [ ] Sparse physical-only mode skips missing address cells.
- [ ] Sparse missing-only mode emits missing descriptors without resolving to `Voxel`.
- [ ] Query max-cell overflow surfaces a non-completed status instead of freezing the editor.
- [ ] Selected physical cells resolve to live `Voxel` values.
- [ ] Missing sparse descriptors do not resolve to live `Voxel` values.

**Exit Criteria:**

- The debugger correctly visualizes rectangular and hex grids.
- The debugger can inspect dense and sparse grids without storage-specific loops.
- The debugger cannot accidentally materialize missing sparse cells.
- Large sparse address spaces require explicit bounded queries or max-cell opt-in.

## Phase 4: Modernize Tracing, Blockers, And Scene Tools

**Goal:** Make Unity helper components reflect v7 coverage, 2D XZ helpers, sparse behavior, and topology-aware visuals.

**Files:**

- Modify: `Build/Base/Runtime/Utility/Debugging/GridTracerTests.cs`
- Modify: `Build/Base/Runtime/Blockers/BlockerComponent.cs`
- Modify: `Build/Base/Editor/Blockers/Editor/EditorBlockerComponent.cs`
- Modify: `Build/Base/Samples/GridforgeDemo/Scripts/SceneGridManager.cs`
- Test: `Tests/EditMode/BlockerComponentEditModeTests.cs`
- Test: `Tests/EditMode/GridTraceVisualizerEditModeTests.cs`

**Work:**

- [ ] Rename or reframe `GridTracerTests` as a trace visualizer component in user-facing docs and inspector labels.
- [ ] Fix the current `FillSize`/`WireSize` drift by deriving draw geometry from diagnostics instead of local cube sizing.
- [ ] Add layer-locked XZ trace mode using v7 `Vector2d` overloads and explicit `layerY`.
- [ ] Keep 3D trace mode for full `Vector3d` workflows.
- [ ] Draw traced cells through diagnostic geometry so hex trails are shaped correctly.
- [ ] Extend `BlockerComponent` authoring with an explicit 3D bounds mode and XZ layer-locked mode.
- [ ] Use `FixedBoundArea` naming in code and docs.
- [ ] For collider/renderer bounds, keep conversion to fixed-point at the Unity adapter boundary.
- [ ] Document that blockers affect configured sparse voxels only.
- [ ] Add inspector preview of blocker coverage through diagnostics or tracer output without mutating runtime state.

**Tests:**

- [ ] Bounds blocker applies over dense rectangular grids.
- [ ] Bounds blocker applies over dense hex grids.
- [ ] Bounds blocker applies only configured cells for sparse rectangular grids.
- [ ] Bounds blocker applies only configured axial cells for sparse hex grids.
- [ ] XZ layer-locked blocker uses `Vector2d` semantics and does not affect other layers.
- [ ] Trace visualizer uses v7 2D overloads with explicit `layerY`.

**Exit Criteria:**

- Scene-authored blockers remain easy to use but no longer imply rectangular-only coverage.
- Trace visualization works for rectangular, hex, sparse, and mixed worlds.
- Existing blocker samples continue working after migration.

## Phase 5: Add Unity Logging And Diagnostics UX

**Goal:** Surface GridForge v7 diagnostics in a Unity-friendly way without adding Unity dependencies to core.

**Files:**

- Create: `Build/Base/Runtime/Utility/Debugging/GridForgeUnityLogger.cs`
- Create: `Build/Base/Editor/Utility/Debugging/Editor/EditorGridForgeUnityLogger.cs`
- Modify: `Build/Editor/GridForgePackageSync.cs`
- Test: `Tests/EditMode/GridForgeUnityLoggerEditModeTests.cs`

**Work:**

- [ ] Add an optional component or static adapter that routes `GridForgeLogger.LogHandler` to Unity logging.
- [ ] Provide an explicit enable/disable toggle rather than silently taking over global logging.
- [ ] Expose minimum level using SwiftCollections `DiagnosticLevel`.
- [ ] Restore previous logger settings on disable/destroy.
- [ ] Document the difference between `GridForgeLogger` messages and `GridDiagnostics` cell descriptors.
- [ ] Ensure tests reset logger settings after each test.

**Exit Criteria:**

- Users can turn on GridForge logging from a scene or editor utility.
- Unity logs preserve level and source information.
- Tests prove logger settings do not leak across scenarios.

## Phase 6: Rebuild Samples Around v7 Workflows

**Goal:** Make samples teach the package's real v7 value instead of only proving that a rectangular demo scene opens.

**Files:**

- Modify: `Build/Base/Samples/GridforgeDemo/Scripts/SceneGridManager.cs`
- Modify: `com.mrdav30.gridforge/Samples/GridforgeDemo`
- Modify: `com.mrdav30.gridforge.lean/Samples/GridforgeDemo`
- Create: sample scene or prefab for dense hex grids.
- Create: sample scene or prefab for sparse grids.
- Create: sample scene or prefab for diagnostics/debugger usage.

**Recommended Samples:**

- `Dense Rectangular`: current basic scene, updated for v7 terminology.
- `Dense Hex`: pointy-top or flat-top hex grid with debugger and trace visualizer.
- `Sparse Rectangular`: configured cells plus missing sparse address diagnostics.
- `Sparse Hex`: axial configured cells plus diagnostics.
- `Mixed Topology`: rectangular and hex grids in one `GridWorld`, showing ordinary lookup and debug overlay.

**Work:**

- [ ] Keep sample code small and explicit.
- [ ] Use `GridWorldComponent` plus authoring components rather than ad hoc runtime construction where possible.
- [ ] Show the debugger configured for physical cells and missing sparse address cells.
- [ ] Keep standard and lean sample assets in sync while preserving package-specific GUIDs.
- [ ] Run package sync before touching serialized sample assets so managed-code drift is gone.

**Exit Criteria:**

- Importing either package gives users an obvious v7 path: rectangular, hex, sparse, diagnostics.
- Samples do not install both variants together.
- Samples do not teach stale v6 constructors or world-level voxel-size concepts.

## Phase 7: Documentation And Public User Experience Pass

**Goal:** Make package docs accurate, discoverable, and aligned with core wiki language.

**Files:**

- Modify: `README.md`
- Modify: `com.mrdav30.gridforge/README.md`
- Modify: `com.mrdav30.gridforge.lean/README.md`
- Create: `docs/feature-work/2026-06-15-gridforge-v7-unity-package-migration-notes.md` if implementation uncovers migration details worth tracking separately.

**Work:**

- [ ] Replace all v6 references with v7 language.
- [ ] Remove or update `new GridWorld(Fixed64.One, 50)` examples.
- [ ] Replace stale `BoundingArea` references with `FixedBoundArea`.
- [ ] Add a short package selection section for standard versus lean variants.
- [ ] Add setup examples for:
  - scene-owned `GridWorldComponent`
  - rectangular grid authoring
  - hex grid authoring
  - sparse grid authoring
  - diagnostics debugger
  - optional Unity logging adapter
- [ ] Link to relevant core wiki pages:
  - Getting Started
  - Common Workflows
  - Sparse Grid Storage
  - Grid Diagnostics and Geometry
  - Diagnostics and Logging
- [ ] Document package maintenance:
  - edit shared managed source in `Build/Base`
  - run package sync
  - run version validation
  - run Unity EditMode tests for both variants
  - export `.unitypackage` archives only after validation

**Exit Criteria:**

- A new user can install one package variant and find a v7 example for their grid shape.
- Docs consistently explain that Unity is an adapter, not the owner of GridForge core state.
- No stale v6 terms remain except in historical notes.

## Phase 8: Dependency Installer And Package Release Hardening

**Goal:** Make package import reliable for users and release maintainers.

**Files:**

- Modify: `com.mrdav30.gridforge/Editor/Utility/GitDependencyInstaller.cs`
- Modify: `com.mrdav30.gridforge.lean/Editor/Utility/GitDependencyInstaller.cs`
- Modify: `.assets/unity-package-versions.json`
- Modify: `.assets/scripts/update-unity-package-versions.ps1`
- Modify: `.github/workflows/build-and-test.yml`

**Work:**

- [ ] Replace the mojibake log arrow with ASCII `->`.
- [ ] Verify `System.Text.Json` is available in the supported Unity editor profile. If not, replace it with a small manifest parser/writer or Unity-compatible JSON path.
- [ ] Make dependency installation idempotent and explicit in logs.
- [ ] Keep standard dependencies:
  - `com.mrdav30.fixedmathsharp`
  - `com.mrdav30.swiftcollections`
  - `com.mrdav30.swiftcollections.fixedmathsharp`
- [ ] Keep lean dependencies:
  - `com.mrdav30.fixedmathsharp.lean`
  - `com.mrdav30.swiftcollections.lean`
  - `com.mrdav30.swiftcollections.fixedmathsharp.lean`
- [ ] Decide whether automatic manifest mutation should remain on import or move behind an explicit menu prompt. If automatic remains, improve failure messages and document it prominently.
- [ ] Extend CI to run the version self-test script.
- [ ] Extend CI to validate package version config before Unity import.
- [ ] Confirm both package variants compile in a fresh Unity project.

**Exit Criteria:**

- Fresh import succeeds for standard and lean packages.
- Dependency repair menu items are clear and variant-specific.
- Version drift fails CI before release.

## Phase 9: Release Candidate Validation

**Goal:** Prove the migration works end to end before tagging v7.

**Required Validation:**

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\test-update-unity-package-versions.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\update-unity-package-versions.ps1 -ValidateOnly
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\sync-gridforge-unity-packages.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\export-gridforge-unity-packages.ps1
git diff --check
```

**Unity Validation:**

- [ ] Standard package imports into a fresh Unity project.
- [ ] Lean package imports into a fresh Unity project.
- [ ] Standard package EditMode tests pass.
- [ ] Lean package EditMode tests pass.
- [ ] Dense rectangular sample opens and debugger draws cells.
- [ ] Dense hex sample opens and debugger draws hex prisms.
- [ ] Sparse rectangular sample shows configured cells and bounded missing address diagnostics.
- [ ] Sparse hex sample shows configured axial cells and bounded missing address diagnostics.
- [ ] Blocker sample applies and removes obstacle state.
- [ ] Trace visualizer draws rectangular and hex traces.
- [ ] Optional logger routes GridForge warnings to Unity logs and restores defaults when disabled.

**Release Artifacts:**

- [ ] `com.mrdav30.gridforge-7.0.0.unitypackage`
- [ ] `com.mrdav30.gridforge.lean-7.0.0.unitypackage`
- [ ] Git tag for Unity package v7.
- [ ] Release notes call out:
  - GridForge v7 core
  - hex-prism authoring/debugging
  - sparse grid authoring/debugging
  - diagnostics debugger rewrite
  - docs and dependency installer changes
  - any serialized-field migration notes

## Risk Register

| Risk | Why It Matters | Mitigation |
| --- | --- | --- |
| Serialized scene data breaks when `_voxelSize` disappears | Existing user scenes may lose intended rectangular cell size | Migrate old value into rectangular topology metrics using serialized-field migration and explicit tests |
| Debugger freezes large sparse worlds | Sparse address spaces can be huge | Use bounded diagnostics, max-cell budgets, and clear query status |
| Hex visualization looks correct but uses float math too early | Unity rendering can hide deterministic projection mistakes | Query geometry from core diagnostics in fixed-point, convert to `Vector3` only for drawing |
| Package variants drift | Standard and lean bugs become inconsistent | Enforce `Build/Base` sync validation and CI matrix |
| Dependency installer fails on user machines | Import becomes hostile | Validate `System.Text.Json` support, improve logs, keep repair menu, document manual URLs |
| CI exists but does not test behavior | False confidence before release | Add real EditMode tests for lifecycle, authoring, diagnostics, blockers, and logging |
| Sparse cells are presented as empty cells | Users misunderstand runtime behavior | Label missing sparse address diagnostics distinctly and never resolve them as `Voxel` |
| Mixed topology public UX becomes confusing | Users may expect direction mappings across rectangular and hex grids | Keep Unity docs aligned with core: contact queries for touching neighbors, directed lookup only within topology |
| Sample GUID churn breaks scenes | Unity references scripts by GUID | Preserve package `.meta` files and avoid replacing package script assets by delete/recreate |

## Recommended Execution Order

1. Phase 0: Stabilize baseline and fix immediate test/script hazards.
2. Phase 1: Repair shared source ownership and package sync.
3. Phase 2: Redesign grid authoring around topology/storage metrics.
4. Phase 3: Rebuild the debugger on `GridDiagnostics`.
5. Phase 4: Modernize tracing and blockers.
6. Phase 5: Add optional Unity logging adapter.
7. Phase 6: Rebuild samples.
8. Phase 7: Rewrite docs.
9. Phase 8: Harden dependency and release tooling.
10. Phase 9: Run release candidate validation and package export.

## First Implementation Slice Recommendation

Start with a narrow hardening PR before the bigger feature work:

- Fix version self-test fixture.
- Add one or two real EditMode tests.
- Fix or remove debug source drift between `Build/Base` and package copies.
- Update README/package README v6 references that are plainly incorrect.
- Add sync validation so future phases cannot drift silently.

That slice gives the rest of the migration a cleaner floor without forcing the diagnostics and authoring redesign to land all at once.

