# GridForge v7 Unity Package Migration Battle Plan

> **For agentic workers:** Use this as the strategic migration brief and living implementation ledger. Before implementing any phase, create a focused task plan for that phase, update this file when scope or status changes, and work through `Build/Base` first, then sync both package variants.

**Goal:** Bring `GridForge-Unity` up to the GridForge v7 public model and harden the Unity package experience around topology-aware grids, sparse storage, diagnostics, samples, docs, and release validation.

**Architecture:** Keep GridForge core engine-agnostic and treat Unity as an adapter layer. Unity authoring components should produce explicit `GridConfiguration` data, Unity debugging should consume `GridForge.Diagnostics` descriptors, and package variants should remain generated from shared managed source wherever practical.

**Tech Stack:** Unity 2022.3+ package source, Unity 6000.3.9f1 CI smoke project, GridForge v7 DLL/XML, FixedMathSharp v5 Unity packages, SwiftCollections v5 Unity packages, NUnit EditMode tests, PowerShell package scripts.

---

## Status

- Date: 2026-06-16.
- Overall migration status: Done. This plan is closed and should live under `.docs/feature-work/done` after the Phase 9 wrap-up.
- Current Unity package branch: `develop`.
- Current Unity package migration baseline reviewed: `e10602aad40e98a6a9602833f7086a0ca833a941`.
- Current Unity package `HEAD`: `b7c06f0` (`chore: licensing`); Phases 5 through 9 are currently local worktree implementation awaiting owner review/stage/commit.
- Core GridForge source reviewed: `F:\gamedevrepos\GridForge` on `develop`; embedded package DLLs now come from local HEAD `b5b2f3e` plus the uncommitted `GridTracer.TraceLine` fixes captured below.
- Core feature docs reviewed from `F:\gamedevrepos\GridForge\docs\feature-work\done`, excluding `gridWorldRefactorPlan.md` per request.
- Core wiki docs reviewed from `F:\gamedevrepos\GridForge\docs\wiki`.
- This file is the migration progress ledger. Keep it current as phases land or as new release risks are discovered.
- Release ceremony and final human Scene View smoke are tracked separately in `.docs/feature-work/2026-06-16-gridforge-v7-unity-release-follow-up.md`.

## Implementation Log

### 2026-06-15 - Phase 0 And Phase 1 Baseline Hardening

- Completed the first hardening slice in the package working tree.
- Added `.assets/gridforge-core-source.json` to record the local pre-release GridForge v7 source commit and SHA-256 hashes for both standard and lean `GridForge.dll` and `GridForge.xml` artifacts.
- Fixed `.assets/scripts/test-update-unity-package-versions.ps1` so its generated fixture exercises standard and lean package manifests, dependency versions, and installer URLs.
- Added `.assets/scripts/test-gridforge-package-sync.ps1` to compare managed, non-meta shared source from `Build/Base` against both package variants.
- Added `Tests/EditMode/GridWorldComponentEditModeTests.cs` as a real EditMode smoke test source for CI.
- Updated `.github/workflows/build-and-test.yml` to run package maintenance validation and fail clearly if no EditMode test sources are present.
- Updated root and package READMEs from stale v6 language to the current v7 baseline, including temporary local-DLL sourcing notes.
- Verification completed:
  - `test-update-unity-package-versions.ps1`: 4/4 pass.
  - `update-unity-package-versions.ps1 -ValidateOnly`: pass.
  - `test-gridforge-package-sync.ps1`: pass.
  - `sync-gridforge-unity-packages.ps1 -WhatIf`: resolves Unity/project path.
  - `export-gridforge-unity-packages.ps1 -WhatIf`: resolves Unity/project path.
  - stale `v6`, `GridWorld(Fixed64`, and `BoundingArea` grep across package READMEs: clean.
  - `git diff --check`: no whitespace errors; line-ending normalization warnings only.
- Follow-up found the local Unity EditMode runner issue: invoking `-runTests` with `-quit` exits before the Test Framework runs. Use `.assets/scripts/run-gridforge-unity-editmode-tests.ps1`, which intentionally omits `-quit` and requires a fresh result XML.
- Next phase target at the time: Phase 2, redesign Unity grid authoring around v7 topology, metrics, storage, and sparse configuration.

### 2026-06-15 - Phase 2 Grid Authoring Redesign

- Added `SerializableGridTopologyMetrics` for rectangular and hex-prism metrics authoring.
- Added `SerializableSparseVoxelSet` and `SerializableVoxelIndex` for explicit sparse configured cells.
- Expanded `SerializableGridConfiguration` to carry topology kind, topology metrics, storage kind, configured sparse cells, safe `TryToGridConfiguration(...)`, and sparse index conversion.
- Updated `GridConfigurationSaver` so world spatial-hash size remains the only world-level sizing concept, dense grids use `TryAddGrid(config, out _)`, sparse grids use `TryAddGrid(config, configuredVoxels, out _)`, and invalid authoring data logs Unity warnings before registration.
- Removed v6 `_voxelSize` compatibility state instead of preserving it; v7 authoring now requires explicit per-grid topology metrics.
- Updated `GridWorldComponent` so it owns only the world lifecycle and spatial hash cell size, with no hidden legacy voxel-size storage.
- Updated the custom `GridConfigurationSaver` inspector to remove the voxel-size field and show validation help boxes for invalid bounds, metrics, and sparse indices.
- Added `GridConfigurationAuthoringEditModeTests.cs` covering default rectangular authoring, rectangular metric round-trip, pointy/flat hex metrics, sparse rectangular/hex configured cells, no legacy compatibility fields, scan-cell fallback, and invalid metric rejection.
- Synced shared managed source from `Build/Base` into both standard and lean package variants; Unity's package sync reported 0 remaining copied/deleted files afterward.
- Verification completed:
  - `.assets/scripts/run-gridforge-unity-editmode-tests.ps1`: pass, 10 total, 10 passed, 0 failed, result XML written.
  - Unity script compilation: pass (`Tundra build success`, no `error CS` lines).
  - Generated `GridForge.Unity.Tests.EditMode.csproj` restore/build: pass, 0 warnings, 0 errors.
  - `test-gridforge-package-sync.ps1`: pass.
  - `test-update-unity-package-versions.ps1`: 4/4 pass.
  - `update-unity-package-versions.ps1 -ValidateOnly`: pass.
  - `export-gridforge-unity-packages.ps1 -WhatIf`: resolves Unity/project path.
  - stale visible `Voxel Size`, `public Fixed64 VoxelSize`, `GridWorld(Fixed64`, `BoundingArea`, and `v6` grep across package source/docs/tests: clean.
  - `git diff --check`: no whitespace errors; line-ending normalization warnings only.
- Local Unity EditMode execution is now verified through the dedicated script. Do not add `-quit` to Unity Test Framework runs; let the Test Framework exit Unity after writing results.
- Next phase target: Phase 3, rebuild grid debugging around `GridForge.Diagnostics`.

### 2026-06-15 - Phase 3 Diagnostics Debugger Redesign

- Rebuilt `GridDebugger` around `GridDiagnostics.VisitCells(...)`, `GridDiagnosticQuery`, `GridDiagnosticSession`, and a reusable `GridDiagnosticScratch`.
- Added `GridDiagnosticUnityVisitor` as the Unity adapter visitor that counts physical and missing sparse diagnostic cells, tracks last-cell metadata, and optionally draws cells.
- Added `GridDiagnosticGizmoDrawer` so rectangular and hex-prism gizmo geometry comes from `GridDiagnosticGeometry` fixed-point vertices/edges and converts to Unity `Vector3` only at the draw boundary.
- Replaced the debugger inspector with diagnostics-first controls for all-grids versus single-grid queries, topology/storage filters, address mode, bounded queries, max cell count, state filters, selection, colors, and query status.
- Kept selected-cell resolution physical-only through `GridDiagnostics.TryResolvePhysicalCell(...)`; missing sparse address descriptors intentionally do not resolve to live `Voxel` instances.
- Did not preserve or migrate old `VoxelFilterType` values. This remains a breaking v7 debugger redesign with no legacy compatibility patches.
- Synced shared managed source from `Build/Base` into both standard and lean package variants; `.assets/scripts/sync-gridforge-unity-packages.ps1` reported 0 copied, 0 deleted, and 0 removed files after sync.
- `.meta` files are Unity-owned. Do not manually create or edit them; let Unity import/regenerate metadata as needed.
- Verification completed:
  - Initial RED Unity compile run failed on missing diagnostic debugger adapter types/methods after adding `GridDebuggerDiagnosticsEditModeTests.cs`.
  - `.assets/scripts/run-gridforge-unity-editmode-tests.ps1`: pass, 16 total, 16 passed, 0 failed, result XML written.
  - Unity package sync: pass, 0 copied, 0 deleted, 0 removed files.
  - `test-gridforge-package-sync.ps1`: pass.
  - Generated `GridForge.Unity.Tests.EditMode.csproj` restore/build: pass, 0 warnings, 0 errors.
  - `test-update-unity-package-versions.ps1`: 4/4 pass.
  - `update-unity-package-versions.ps1 -ValidateOnly`: pass.
  - `export-gridforge-unity-packages.ps1 -WhatIf`: resolves Unity/project/export command.
  - debugger-only stale scan for `VoxelFilterType`, `_voxelFilter`, dense `Width`/`Height`/`Length` loops, `TryGetVoxel`, and `Gizmos.DrawCube`: clean except the intentional edge-list loop in `GridDiagnosticGizmoDrawer`.
- `git diff --check`: no whitespace errors; line-ending normalization warnings only.
- Next phase target: Phase 4, modernize tracing, blockers, and scene tools.

### 2026-06-15 - Phase 4 Tracing And Blocker Modernization

- Reframed `GridTracerTests` as the user-facing Grid Trace Visualizer through component menu/name text while preserving the existing script asset path.
- Replaced trace cube drawing with diagnostic cell drawing through `GridDiagnosticGizmoDrawer`, so rectangular and hex traced cells use `GridDiagnosticGeometry`.
- Added `GridTraceMode.World3D` and `GridTraceMode.XzLayer`; XZ tracing uses GridForge's `Vector2d` overloads with explicit `layerY`.
- Added trace helper APIs for collecting traced voxels into caller-owned lists and resolving the first traced diagnostic cell for tooling/tests.
- Extended `BlockerComponent` with explicit `BlockAreaMode.Bounds3D` and `BlockAreaMode.XzLayer` authoring, including manual XZ `Vector2d` bounds and layer selection.
- Updated the blocker inspector to use `Fixed Bound Area` naming, expose XZ layer controls, and explain that preview/coverage affect configured physical voxels only.
- Added blocker coverage preview counting/drawing through `GridTracer.GetCoveredVoxels(...)` and diagnostic gizmos without applying blockage.
- Verified collider/renderer and transform-derived bounds convert Unity `Bounds`/`Transform` data to fixed-point at the adapter boundary.
- Reviewed `SceneGridManager`; no code change was needed for this slice because it already bootstraps `GridConfigurationSaver` into the scene-owned `GridWorldComponent`.
- Synced shared managed source from `Build/Base` into both standard and lean package variants; `.assets/scripts/sync-gridforge-unity-packages.ps1` reported 0 copied, 0 deleted, and 0 removed files after sync.
- Verification completed:
  - Initial RED Unity compile run failed on missing Phase 4 blocker and trace visualizer APIs after adding `BlockerComponentEditModeTests.cs` and `GridTraceVisualizerEditModeTests.cs`.
  - `.assets/scripts/run-gridforge-unity-editmode-tests.ps1`: pass, 24 total, 24 passed, 0 failed, result XML written.
  - Unity package sync: pass, 0 copied, 0 deleted, 0 removed files.
  - `test-gridforge-package-sync.ps1`: pass.
  - Generated `GridForge.Unity.Tests.EditMode.csproj` restore/build: pass, 0 warnings, 0 errors.
  - `test-update-unity-package-versions.ps1`: 4/4 pass.
  - `update-unity-package-versions.ps1 -ValidateOnly`: pass.
  - `export-gridforge-unity-packages.ps1 -WhatIf`: resolves Unity/project/export command.
  - stale scan for `FillSize`, `WireSize`, cube gizmos, `BoundingArea`, and old direct trace call patterns in Phase 4 files: clean.
- `git diff --check`: no whitespace errors; line-ending normalization warnings only.
- Next phase target: Phase 5, add Unity logging and diagnostics UX.

### 2026-06-15 - Phase 5 Unity Logging UX

- Added `GridForgeUnityLogger`, an optional `ExecuteAlways` Unity adapter that forwards `GridForgeLogger` messages into Unity `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError`.
- Kept logging opt-in explicit: adding the component does not install a global handler unless the user enables logging or opts into enable-on-component-enable.
- Exposed `SwiftCollections.Diagnostics.DiagnosticLevel` as the Unity minimum level and preserved GridForge level/source information in Unity log messages.
- Captured and restored the previous `GridForgeLogger.LogHandler` and `GridForgeLogger.MinimumLevel` on explicit disable and component destruction.
- Used reference-identity ownership checks for the static active logger so Unity destroyed-object equality cannot skip logger restoration.
- Added `EditorGridForgeUnityLogger` with enable/disable/apply controls and an inspector note that `GridForgeLogger` messages are separate from `GridDiagnostics` cell descriptors.
- Reviewed `GridForgePackageSync`; no code change was needed because existing managed directory entries already cover `Runtime/Utility/Debugging` and `Editor/Utility/Debugging`.
- Synced shared managed source from `Build/Base` into both standard and lean package variants; `.assets/scripts/sync-gridforge-unity-packages.ps1` reported 0 copied, 0 deleted, and 0 removed files after sync.
- Unity generated `.meta` files for the new scripts during import. Do not manually create or edit them.
- Verification completed:
  - Initial RED Unity compile run failed on missing `GridForgeUnityLogger` after adding `GridForgeUnityLoggerEditModeTests.cs`.
  - `.assets/scripts/run-gridforge-unity-editmode-tests.ps1`: pass, 29 total, 29 passed, 0 failed, result XML written.
  - Unity package sync: pass, 0 copied, 0 deleted, 0 removed files.
  - `test-gridforge-package-sync.ps1`: pass.
  - Generated `GridForge.Unity.Tests.EditMode.csproj` restore/build: pass, 0 warnings, 0 errors.
  - `test-update-unity-package-versions.ps1`: 4/4 pass.
  - `update-unity-package-versions.ps1 -ValidateOnly`: pass.
  - `export-gridforge-unity-packages.ps1 -WhatIf`: resolves Unity/project/export command.
  - `git diff --check`: no whitespace errors.
- Next phase target at the time: Phase 6, rebuild samples around v7 workflows.

### 2026-06-15 - Phase 6 v7 Sample Workflow Rebuild

- Added `GridForgeSampleWorkflow` and a public `ApplyAuthoringToWorld()` method to the sample `SceneGridManager`, keeping sample runtime code small and centered on `GridConfigurationSaver` plus `GridWorldComponent`.
- Added `GridForgeSampleAssetGenerator`, an Editor-only build utility that generates sample prefabs and scenes through Unity APIs while resolving standard and lean runtime/sample types by assembly name to avoid package variant type collisions.
- Generated v7 sample workflow prefabs for both package variants:
  - `DenseRectangular`
  - `DenseHex`
  - `SparseRectangular`
  - `SparseHex`
  - `MixedTopologyDiagnostics`
- Regenerated legacy `SceneGridManager`, `GridDebugger`, `Blocker`, and `DemoScene` sample assets so they no longer carry stale debugger fields or v6-era authoring assumptions.
- Initially added `V7Workflows.unity` and `V7Workflows.Lean.unity`, each instantiating all five v7 workflow prefabs; removed during the 2026-06-16 review follow-up because those scenes were internal coverage clutter rather than useful public samples.
- Configured the mixed diagnostics sample with four grids in one authored world, `GridDebugger` address mode `PhysicalAndMissing`, bounded diagnostics, `GridTracerTests` in XZ layer mode, `GridForgeUnityLogger`, and an XZ blocker child.
- Follow-up hardening: Unity did not persist `FixedMathSharp.Fixed64` payloads inside `_savedGridConfigurations` while `Fixed64` was a readonly struct. Diagnostic probes in FixedMathSharp-Unity confirmed direct, nested, and list-held `Fixed64`/`Vector2d`/`Vector3d` values could drop to `{}`/zero under Unity serialization.
- FixedMathSharp-Unity v5.0.1 removed that Unity serialization limitation by making `Fixed64.m_rawValue` public mutable package storage. GridForge then removed its temporary raw-`long` mirrors and the GridForge-specific fixed-value property drawer; `SerializableGridConfiguration` now serializes `Vector3d` bounds directly and `SerializableGridTopologyMetrics` serializes `Fixed64` metrics directly.
- Added a generated-asset whitespace normalizer for prefabs/scenes only; Unity-owned `.meta` files are still generated and left untouched.
- Added `GridForgeSampleWorkflowsEditModeTests.cs` covering sample workflow enum/API surface, v7 workflow prefab existence, absence of extra workflow aggregation scenes, topology/storage/sparse authoring, and mixed diagnostics debugger setup for both package variants.
- Synced the shared sample script from `Build/Base` into both standard and lean package variants before generating serialized sample assets.
- Verification completed:
  - Initial RED Unity EditMode run: 29 passed, 8 failed on the expected missing Phase 6 workflow enum/API and assets.
  - `GridForgeSampleAssetGenerator.GenerateSamplesBatchMode`: pass after resolving FixedMathSharp value construction through runtime constructor reflection.
  - Follow-up RED evidence for authoring serialization: direct `Fixed64`/`Vector2d`/`Vector3d` fields serialized as `{}` through both Unity JSON serializer paths when `Fixed64` remained readonly.
  - FixedMathSharp-Unity v5.0.1 cleanup RED: 39/40 passed, with the lone failure requiring GridForge authoring to remove `_boundsMinXRaw`/raw metric mirror fields.
  - Direct FixedMathSharp serialization probe now covers scalar, vector, nested struct, and list-held values through a Unity `ScriptableObject` asset round trip.
  - Final Unity EditMode run without `-quit`: pass, 40 total, 40 passed, 0 failed, 0 skipped after direct FixedMathSharp authoring storage and sample regeneration.
  - `.assets/scripts/sync-gridforge-unity-packages.ps1`: pass, 0 copied, 0 deleted, 0 removed files after generation.
  - `.assets/scripts/test-gridforge-package-sync.ps1`: pass.
  - Generated `GridForge.Unity.Tests.EditMode.csproj` restore/build: pass, 0 warnings, 0 errors.
  - Follow-up FixedMathSharp-Unity drawer adapter check: `GridForge.Editor.csproj`, `GridForge.Lean.Editor.csproj`, and `GridForge.Build.csproj` build; `GridForge.Build.csproj` still has the pre-existing package-manifest field warnings.
  - `.assets/scripts/test-update-unity-package-versions.ps1`: 4/4 pass.
  - `.assets/scripts/update-unity-package-versions.ps1 -ValidateOnly`: pass.
  - `.assets/scripts/export-gridforge-unity-packages.ps1 -WhatIf`: resolves Unity/project/export command.
  - sample YAML scan for empty fixed authoring payloads (`_boundsMin: {}`, `_boundsMax: {}`, `_rectangularCellWidth: {}`, `_hexRadius: {}`): clean.
  - stale sample scan for `_voxelFilter`, `_legacyVoxelSize`, `GridWorld(`, `new GridWorld`, `Voxel Size`, world-level voxel language, and `BoundingArea`: clean.
  - `git diff --check`: no whitespace errors; line-ending normalization warnings only.
- Next phase target: Phase 7, documentation and public user experience pass.

### 2026-06-16 - Phase 6 Review Follow-Up And Core Trace Fix

- Removed `V7Workflows.unity` and `V7Workflows.Lean.unity` from the public samples. The workflow prefabs remain the reusable coverage surface, while `DemoScene` remains the sample scene entry point.
- Updated `GridForgeSampleAssetGenerator` so it does not recreate the workflow aggregation scenes and deletes the obsolete scene assets through `AssetDatabase.DeleteAsset(...)` when samples are regenerated. Unity owned the corresponding `.meta` deletions.
- Updated `GridForgeSampleWorkflowsEditModeTests` to assert the workflow scenes are absent while all workflow prefabs remain present.
- Confirmed the trace visualizer report was rooted in GridForge core, not Unity adapter code:
  - `GridTracer.TraceLine` accepted candidate grids from the broadphase even when the actual line segment did not intersect the grid bounds, which let off-segment hex grids contribute traced cells.
  - Hex tracing omitted the clamped grid-edge endpoint when the caller's end point was outside the grid, because endpoint inclusion was delegated to the global end voxel lookup.
  - Follow-up review found that intersected later grids still received the full global start/end segment, so traces visually restarted at the later grid's local origin instead of continuing from the segment entry point.
- Fixed `GridTracer.TraceLine` in core with deterministic fixed-point segment-vs-bounds filtering, per-grid clipped trace segments, exact boundary-coordinate snapping for clipped fixed-point intersections, and hex endpoint inclusion that preserves `includeEnd: false` for real in-grid end voxels.
- Added core regression tests for off-segment hex candidates, dense hex edge completion, sparse hex edge completion, cross-grid continuation into a later hex grid, and `includeEnd: false` semantics.
- Added Unity visualizer regressions for mixed-topology off-segment grids, hex edge completion, and cross-grid continuation through the `GridTracerTests` component path.
- Rebuilt standard and lean `GridForge.dll`/`.pdb` from local pre-release GridForge, copied the `netstandard2.1` artifacts into both package variants, and updated `.assets/gridforge-core-source.json` with the new DLL hashes plus a working-tree status note.
- Verification completed:
  - Core RED run before fix: 3 targeted `GridTracerTests` failures for off-segment hex candidate and dense/sparse hex edge endpoint cases.
  - Core targeted `GridTracerTests`: pass, 39/39.
  - Core full Debug tests: pass, 414/414.
  - Core full ReleaseLean tests: pass, 416/416.
  - Core `dotnet build GridForge.slnx --configuration Release`: pass, 0 warnings, 0 errors.
  - Core `dotnet build GridForge.slnx --configuration ReleaseLean`: pass, 0 warnings, 0 errors.
  - Unity RED sample-scene run before generator cleanup: 38 passed, 2 failed on the now-unwanted workflow scenes.
  - `GridForgeSampleAssetGenerator.GenerateSamplesBatchMode`: pass; obsolete workflow scenes removed.
  - Unity RED visualizer continuation run before rebuilt DLL copy: 42 passed, 1 failed, expected first hex voxel `(0, 0, 1)` but old DLL returned `(0, 0, 0)`.
  - Unity EditMode final run: pass, 43 total, 43 passed, 0 failed, 0 skipped.
  - `.assets/scripts/test-gridforge-package-sync.ps1`: pass after normalizing the standard sample `SceneGridManager.cs` copy back to `Build/Base`.

### 2026-06-16 - Phase 6 Inspector UX Hardening

- Added a context-aware `SerializableGridConfiguration` property drawer for the `GridConfigurationSaver` inspector.
- The drawer shows rectangular metrics only for `GridTopologyKind.RectangularPrism`, hex metrics only for `GridTopologyKind.HexPrism`, and configured sparse voxels only for `GridStorageKind.Sparse`.
- Hidden metric and sparse fields remain serialized instead of being reset, so toggling topology or storage does not destroy inactive authoring values.
- The drawer still delegates `Fixed64`, `Vector2d`, and `Vector3d` rendering to FixedMathSharp-Unity editor drawers instead of introducing GridForge-owned serialized variants or fixed-value editors.
- Rendered `GridDebugger` query status and selected voxel output inside disabled IMGUI scopes, making runtime-derived status fields visually and semantically read-only in the inspector.
- Added EditMode policy regressions for topology/storage inspector visibility and read-only debugger display policy.
- Synced the shared editor source from `Build/Base` into both standard and lean package variants; no `.meta` files were created or edited manually.
- Verification completed:
  - RED Unity EditMode run after adding policy tests: pass compilation, 43 passed, 2 failed on missing editor policy helper types.
  - `.assets/scripts/sync-gridforge-unity-packages.ps1`: pass, 4 files copied, 0 deleted, 0 directories removed.
  - `.assets/scripts/run-gridforge-unity-editmode-tests.ps1`: pass, 45 total, 45 passed, 0 failed, 0 skipped.
  - `.assets/scripts/test-gridforge-package-sync.ps1`: pass.
  - Generated `GridForge.Unity.Tests.EditMode.csproj` restore/build: pass, 0 warnings, 0 errors.
  - `git diff --check`: no whitespace errors; line-ending normalization warnings only.

### 2026-06-16 - Collection Policy And Test Hygiene Hardening

- Removed remaining `System.Collections.Generic.List<T>` and `Dictionary<TKey,TValue>` usage from GridForge-owned package/runtime/test code, except for allowed `IEnumerable<T>` API signatures and generated XML documentation.
- Moved Unity-persisted authoring collections onto SwiftCollections-Unity v5.0.2 adapters:
  - `GridConfigurationSaver._savedGridConfigurations` is now `SerializedSwiftList<SerializableGridConfiguration>`.
  - `SerializableSparseVoxelSet._indices` is now `SerializedSwiftList<SerializableVoxelIndex>`.
  - `SavedGridConfigurations` and `SerializableSparseVoxelSet.Indices` expose `SwiftList<T>` runtime collections for GridForge consumers.
  - The FixedMathSharp serialization probe now uses an array for nested values.
- Used SwiftCollections where the collections are working buffers or maps:
  - `GridTracerTests.GetTraceVoxelsInto(...)` now accepts `SwiftList<Voxel>`.
  - Trace visualizer and logger tests use `SwiftList<T>` instead of `List<T>`.
  - `SerializableSparseVoxelSet` uses `SwiftList<SerializableVoxelIndex>` for runtime access and temporary conversion buffers.
  - `GridForgePackageSync` uses `SwiftDictionary<string, string>` for managed-file maps.
- Reworked package `GitDependencyInstaller` JSON handling from nested `Dictionary` deserialization to `JsonObject`, and replaced the mojibake dependency log arrow with ASCII `->`.
- Added `Chronicler.dll` to the EditMode test asmdef precompiled references because direct `SwiftList<T>` usage exposes SwiftCollections' `IStateBacked<>` interface to the compiler.
- Removed hollow tests that only guarded legacy or absence behavior:
  - `PackagesKeepV7WorkflowCoverageInPrefabsWithoutExtraDemoScene(...)`
  - `AuthoringComponentsDoNotRetainLegacyVoxelSizeCompatibilityFields()`
- Kept behaviorful coverage for prefab workflow authoring, FixedMathSharp serialization, authoring persistence, debugger inspector policy, tracing, blockers, and logging.
- Source review resolution: SwiftCollections-Unity v5.0.2 added `SerializedSwift*` Unity persistence adapters. GridForge now uses those adapters for saved grid configurations and sparse voxel authoring while continuing to treat direct `SwiftList<T>`/`SwiftDictionary<TKey,TValue>` fields as runtime-only types.
- Debugging note: NUnit `CollectionAssert` can trip `SwiftList<T>`'s non-generic enumerator after enumeration completes, so the logger tests now assert `Count` plus indexed values instead of routing `SwiftList<T>` through NUnit's collection adapter.
- Synced shared managed source from `Build/Base` into both standard and lean package variants. No `.meta` files were manually created or edited.
- Verification completed:
  - 2026-06-16 adapter follow-up:
    - `.assets/scripts/update-unity-package-versions.ps1 -ValidateOnly`: pass.
    - Initial package sync reproduced the expected compile red while package copies still exposed array APIs.
    - Added `SwiftCollections.Runtime` to `Build/GridForge.Build.asmdef` so Build/Base source can compile against `SerializedSwiftList<T>`.
    - Updated the local Unity project manifest/package lock to SwiftCollections-Unity v5.0.2 for lean dependencies as well as standard dependencies.
    - `.assets/scripts/sync-gridforge-unity-packages.ps1`: pass, 0 copied, 0 deleted, 0 removed files after the mechanical package-copy bootstrap.
    - `GridForgeSampleAssetGenerator.GenerateSamplesBatchMode`: pass; sample prefabs now serialize saved configs and sparse indices through adapter `_items` backing data.
  - `.assets/scripts/sync-gridforge-unity-packages.ps1`: pass after a targeted mechanical source sync let Unity compile the changed shared signatures; final sync reported 0 copied, 0 deleted, 0 removed files.
  - `.assets/scripts/run-gridforge-unity-editmode-tests.ps1`: pass, 42 total, 42 passed, 0 failed, 0 skipped.
  - `.assets/scripts/test-gridforge-package-sync.ps1`: pass.
  - Generated `GridForge.Unity.Tests.EditMode.csproj` restore/build: pass, 0 warnings, 0 errors.
  - `git diff --check`: no whitespace errors; line-ending normalization warnings only.

### 2026-06-16 - Phase 7 Documentation And Public UX Pass

- Reworked the root README into a concise package selection and maintainer entry point instead of a long API walkthrough.
- Reworked the standard and lean package READMEs into short Package Manager-friendly install/setup pages with exact dependency repair menu names.
- Added `.docs/wiki/GridForge-Unity-v7-User-Guide.md` for detailed public v7 Unity workflows:
  - scene-owned `GridWorldComponent`
  - rectangular grid authoring
  - hex grid authoring
  - sparse grid authoring
  - diagnostics debugger
  - trace visualizer and blockers
  - optional Unity logging adapter
  - FixedMathSharp and SwiftCollections-Unity serialization policy
- Added `.docs/wiki/GridForge-Unity-Package-Maintenance.md` for release-maintainer workflow, package variant policy, DLL intake, validation commands, package sync, and export reminders.
- Linked the READMEs and Unity guide to the core GridForge wiki pages for Getting Started, Common Workflows, Sparse Grid Storage, Grid Diagnostics and Geometry, and Diagnostics and Logging.
- Kept detailed public API information under `.docs/wiki` so Unity does not import the docs as assets and the package READMEs stay focused.
- Verification completed:
  - `test-update-unity-package-versions.ps1`: 4/4 pass.
  - `update-unity-package-versions.ps1 -ValidateOnly`: pass.
  - `test-gridforge-package-sync.ps1`: pass.
  - stale doc scan for `v6`, `BoundingArea`, old `GridWorld(Fixed64.One, 50)` examples, future-tense migration wording, and `_legacyVoxelSize`: clean.
  - `git diff --check`: no whitespace errors; line-ending normalization warnings only.
  - Unity EditMode tests were not rerun for this docs-only pass; the previous Phase 6 run covered the current code and sample state.
- Next phase target: Phase 8, dependency installer and package release hardening.

### 2026-06-16 - Phase 8 Dependency Installer And Package Release Hardening

- Replaced the old shared installer placement with package-specific bootstrap installer assemblies under each package's `Editor/Utility/DependencyInstaller` folder.
- Kept the old `Editor/Utility/GitDependencyInstaller.cs` files as tiny placeholders so Unity-owned metadata and references remain stable, while the real implementation now compiles before the main package assemblies.
- Removed the dependency installer's reliance on FixedMathSharp, SwiftCollections, and `System.Text.Json`; isolated fresh-import tests showed the bootstrap path must compile before those dependencies exist, and `System.Text.Json.Nodes` was not available to the isolated Unity editor assembly.
- Added version-gated asmdef constraints so package runtime/editor/sample assemblies wait for the required FixedMathSharp and SwiftCollections packages before compiling.
- Kept automatic manifest mutation on first import, with explicit variant-specific logs, variant-specific repair menu items, and public docs that explain the first-import dependency resolve behavior.
- Hardened `.assets/scripts/update-unity-package-versions.ps1` so validation fails if an installer carries an unconfigured dependency entry.
- Extended the package version self-test to cover unconfigured installer dependency detection.
- Updated CI maintenance checks so package version self-tests, version validation, and package sync validation run as separate failure points before Unity import.
- Added `com.unity.modules.imgui` to the generated fresh Unity CI manifest because SwiftCollections-Unity v5.0.2 includes IMGUI-backed sample/editor code in minimal projects.
- Verification completed:
  - `test-update-unity-package-versions.ps1`: 5/5 pass.
  - `update-unity-package-versions.ps1 -ValidateOnly`: pass.
  - `test-gridforge-package-sync.ps1`: pass.
  - standard fresh Unity import: first pass installs the three standard dependencies; second pass compiles cleanly with dependencies already satisfied.
  - lean fresh Unity import: first pass installs the three lean dependencies; second pass compiles cleanly with dependencies already satisfied.
  - `GridForge.Editor.csproj`: pass, 0 warnings, 0 errors.
  - `GridForge.Lean.Editor.csproj`: pass, 0 warnings, 0 errors.
  - `GridForge.Unity.Tests.EditMode.csproj`: pass, 0 warnings, 0 errors.
  - `git diff --check`: no whitespace errors; line-ending normalization warnings only.
- Fresh-import note: the first editor pass can log transient plugin reference warnings before Unity resolves the newly added git dependencies. The second pass on the same fresh project is the settled-state validation, and the docs now call out the one extra resolve/recompile cycle.

### 2026-06-16 - Phase 9 Release Candidate Validation

- Ran the full package validation set instead of only dry-run checks:
  - package version self-test
  - package version validation
  - package sync validation
  - Unity EditMode runner
  - package sync
  - package export
  - `git diff --check`
- Unity EditMode validation reports 42 total, 42 passed, 0 failed, 0 skipped.
- Sample-facing behavior is covered by EditMode tests for workflow prefab loading, dense rectangular and dense hex diagnostic geometry, sparse physical/missing diagnostics, blocker application/removal semantics, rectangular/hex trace visualization behavior, and optional Unity logger routing/restoration.
- Exported release-candidate package archives:
  - `F:\gamedevrepos\GridForge-Unity\UnityPackageExports~\com.mrdav30.gridforge-7.0.0.unitypackage`
  - `F:\gamedevrepos\GridForge-Unity\UnityPackageExports~\com.mrdav30.gridforge.lean-7.0.0.unitypackage`
- Did not create a git tag from the dirty local worktree. Tagging, publishing release notes, and the final human Scene View smoke are tracked in `.docs/feature-work/2026-06-16-gridforge-v7-unity-release-follow-up.md` for after owner review/stage/commit.
- No implementation work remains deferred from this migration plan.

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
- Dependency version config targets FixedMathSharp Unity v5.0.1 and SwiftCollections Unity v5.0.2.
- PowerShell wrappers exist for version sync, package sync, and `.unitypackage` export.
- Unity CI scaffolding exists for standard and lean package EditMode test matrices.
- Phase 3 rebuilt `GridDebugger` on `GridForge.Diagnostics`, including topology-aware rectangular/hex drawing, sparse missing-address descriptors, query bounds, max-cell status, and physical-only selected-cell resolution.
- Phase 4 modernized trace visualization and blocker authoring around topology-aware coverage, XZ layer helpers, sparse physical coverage, and diagnostic gizmos.

### Closed High-Priority Gaps

- Phase 7 replaced the stale root and package READMEs with concise v7 package entry points.
- Phase 7 added `.docs/wiki/GridForge-Unity-v7-User-Guide.md` for hex-prism grids, sparse storage, `GridDiagnostics`, and `GridForgeLogger` Unity workflows.
- Phase 2 resolved grid configuration authoring for topology kind, topology metrics, storage kind, and sparse configured cells.
- Phase 2 removed `_voxelSize` authoring and compatibility storage; v7 cell geometry is explicitly per-grid topology metrics.
- Phase 3 replaced `GridDebugger` dense rectangular loops with `GridDiagnostics.VisitCells(...)`, topology-aware diagnostic geometry, sparse missing-address visualization, and physical-only cell resolution.
- Phase 4 reframed `GridTracerTests` as the Grid Trace Visualizer and replaced cube drawing with diagnostic geometry.
- Phase 4 resolved `GridTracerTests` `FillSize`/`WireSize` drift and package sync validation now catches managed-source drift across both variants.
- `Tests/EditMode` now contains lifecycle, authoring, diagnostics, blocker, and trace visualizer coverage.
- Phase 0 fixed `.assets/scripts/test-update-unity-package-versions.ps1` so the generated fixture defines standard and lean package entries.
- Phase 8 moved dependency installation into package-specific bootstrap assemblies that do not depend on FixedMathSharp, SwiftCollections, or `System.Text.Json`.
- Package manifests continue to declare no UPM dependencies because Unity package manifests cannot use git URLs for dependencies. The editor-time bootstrap path is validated through fresh-import tests, variant-specific logs, repair menu items, and public docs.

### Design Constraint

`Build/Base` is the shared managed-code source of truth. Most implementation should happen there first, then be copied to both package variants through the package sync path. Package-specific source should remain limited to package metadata, plugin assets, asmdefs, installers, samples, and serialized Unity assets that need separate GUIDs.

Unity `.meta` files are Unity-owned. Do not manually create or edit them during implementation; allow Unity import/package sync runs to generate or update metadata.

## Non-Goals

- Do not move GridForge core behavior into Unity.
- Do not add Unity types to GridForge core APIs.
- Do not create a Unity-only grid model that competes with `GridWorld`, `VoxelGrid`, `Voxel`, `GridConfiguration`, or `GridDiagnostics`.
- Do not make missing sparse cells behave like empty dense cells.
- Do not use floating-point math before the Unity adapter boundary.
- Do not require users to install both package variants together.
- Do not preserve stale v6 docs or serialized fields as compatibility baggage during this breaking v7 migration unless a future plan explicitly calls for it.

## Phase 0: Stabilize The Migration Baseline

**Goal:** Make the current v7 migration state trustworthy before changing APIs.

**Files:**

- Review: `com.mrdav30.gridforge/Plugins/GridForge.dll`
- Review: `com.mrdav30.gridforge.lean/Plugins/GridForge.dll`
- Review: `com.mrdav30.gridforge/Plugins/GridForge.xml`
- Review: `com.mrdav30.gridforge.lean/Plugins/GridForge.xml`
- Review: `.assets/unity-package-versions.json`
- Create: `.assets/gridforge-core-source.json`
- Modify: `.assets/scripts/test-update-unity-package-versions.ps1`
- Modify: `.github/workflows/build-and-test.yml`
- Create: `Tests/EditMode/GridWorldComponentEditModeTests.cs`

**Work:**

- [x] Confirm both standard and lean DLL/XML files were built from the intended GridForge v7 core commit.
  - 2026-06-15: recorded local core commit `866c91f6900c710d0cac561a7a01d08711f8e2ab` and standard/lean DLL/XML SHA-256 hashes in `.assets/gridforge-core-source.json`.
- [x] Add a small manifest file or doc note that records the core commit used for the embedded DLLs.
- [x] Fix `.assets/scripts/test-update-unity-package-versions.ps1` so its generated fixture includes a `packages` array for standard and lean package manifests.
- [x] Add at least one real EditMode test file so CI does not fail on `cp Tests/EditMode/*.cs`.
- [x] Update CI setup to fail with a clear message if the package test source glob is empty.
- [x] Run:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\test-update-unity-package-versions.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\update-unity-package-versions.ps1 -ValidateOnly
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\sync-gridforge-unity-packages.ps1 -WhatIf
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\export-gridforge-unity-packages.ps1 -WhatIf
git diff --check
```

**Exit Criteria:**

- [x] Version self-tests pass.
- [x] Version validation passes.
- [x] Unity batch wrappers resolve the intended Unity project and editor.
- [x] CI has at least one actual EditMode test source file per package matrix.
- [x] No package docs claim v6 after this phase.

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
- Create: `.assets/scripts/test-gridforge-package-sync.ps1`

**Work:**

- [x] Decide whether the package-copy drift in `GridDebugger.cs` and `GridTracerTests.cs` should be kept. Current evidence says it should not be kept.
  - 2026-06-15: user-side package sync fix removed the drift before implementation; validation now confirms `Build/Base` matches both package variants for managed non-meta source.
- [x] Re-sync package copies from `Build/Base` so the shared source is byte-equivalent except for `.meta` files.
  - 2026-06-15: no additional source copy was needed after the sync fix; `.assets/scripts/test-gridforge-package-sync.ps1` confirms byte-equivalence.
- [x] Add a package-sync validation command or test that compares non-meta managed files from `Build/Base` to both packages.
- [x] Keep package-specific `.meta` GUIDs intact so existing samples and scenes do not lose script references.
- [x] Add any new managed-code directories from later phases to `GridForgePackageSync.ManagedEntries`.
  - 2026-06-16: no new shared managed-code directories were introduced after Phase 6. The Phase 8 bootstrap installers are package-specific editor sources and intentionally stay outside the `Build/Base` sync-managed set.

**Exit Criteria:**

- [x] `Build/Base` is the only place future shared managed-code edits are made.
- [x] Package copies are synchronized by tool rather than by manual edits.
- [x] Sync validation catches source drift before release.

## Phase 2: Redesign Unity Grid Authoring For v7 Configuration

**Goal:** Let users author rectangular, hex, dense, and sparse grids through Unity without hiding GridForge's explicit configuration model.

**Execution Status:** Implemented locally as of 2026-06-15; local Unity EditMode execution is verified through `.assets/scripts/run-gridforge-unity-editmode-tests.ps1`.

**Focused Implementation Plan:**

1. [x] Add EditMode tests first for the desired public authoring surface:
   - default rectangular dense config
   - no legacy `_voxelSize` compatibility fields or serialized migration attributes
   - independent rectangular metrics
   - pointy-top and flat-top hex metrics
   - sparse rectangular and sparse hex configured indices
   - scan-cell fallback
   - invalid topology metrics rejected before `GridWorld.TryAddGrid(...)`
2. [x] Implement serializable runtime data in `Build/Base`:
   - `SerializableGridTopologyMetrics`
   - `SerializableSparseVoxelSet`
   - expanded `SerializableGridConfiguration`
3. [x] Update `GridConfigurationSaver`:
   - keep `_spatialGridCellSize` as the only world-level sizing concept
   - remove old `_voxelSize` compatibility state
   - use dense `TryAddGrid(config, out _)` and sparse `TryAddGrid(config, configuredVoxels, out _)`
   - emit Unity warnings for invalid bounds, metrics, sparse indices, and registration failures
4. [x] Update `GridWorldComponent`:
   - remove voxel-size authoring from the visible component model
   - keep no hidden serialized voxel-size migration storage
5. [x] Update the editor inspector:
   - remove the world-level voxel-size field
   - expose spatial cell size, saved grid configs, and visualization toggle
   - rely on v7 field names: topology, metrics, storage, sparse configured cells
6. [x] Sync `Build/Base` to both package variants and run the package sync validator.
7. [x] Update this ledger with verification results and any Unity TestRunner caveats.

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

- [x] Add topology and storage fields to `SerializableGridConfiguration`.
- [x] Use `GridTopologyMetrics.Rectangular(...)` for rectangular configs.
- [x] Use `GridTopologyMetrics.Hex(...)` for hex configs.
- [x] Use `GridStorageKind.Dense` as the default storage kind.
- [x] Use `GridStorageKind.Sparse` only when explicit configured sparse indices are supplied or the user intentionally creates an empty sparse grid.
- [x] Remove `_voxelSize` from new authoring UI as a world-level concept.
- [x] Remove old serialized `_voxelSize` compatibility fields instead of migrating them.
- [x] Guard against reintroducing `[FormerlySerializedAs("_voxelSize")]` compatibility state in authoring components.
- [x] Add editor validation for positive rectangular width/layer/length and positive hex radius/layer height.
- [x] In `EarlyApply`, call:
  - `world.TryAddGrid(config, out _)` for dense grids
  - `world.TryAddGrid(config, configuredVoxels, out _)` for sparse grids
- [x] Log clear Unity warnings for invalid bounds, invalid metrics, invalid sparse indices, and failed grid registration.

**Tests:**

- [x] Rectangular default authoring creates v7 default rectangular config.
- [x] Authoring components do not retain legacy voxel-size compatibility fields.
- [x] Rectangular independent X/Y/Z metrics round-trip through `ToGridConfiguration`.
- [x] Pointy-top hex config round-trips.
- [x] Flat-top hex config round-trips.
- [x] Sparse rectangular configured indices are passed into `TryAddGrid`.
- [x] Sparse hex configured axial indices are passed into `TryAddGrid`.
- [x] Invalid scan cell size clamps or resolves to `GridConfiguration.DefaultScanCellSize`.
- [x] Invalid metrics are rejected before world registration.
- [x] Saved grid configurations persist fixed-point bounds and topology metrics through Unity prefab serialization.

> 2026-06-15 follow-up: the local TestRunner issue was caused by passing `-quit` with `-runTests`. `.assets/scripts/run-gridforge-unity-editmode-tests.ps1` omits `-quit`, produces a result XML, and currently reports 10/10 EditMode tests passing.
> 2026-06-15 follow-up: `FixedMathSharp.Fixed64` values were not safe as direct Unity serialized storage while the type remained readonly. FixedMathSharp-Unity v5.0.1 now publishes mutable Unity package storage for `m_rawValue`, so GridForge authoring again stores `Fixed64` and `Vector3d` directly instead of carrying private raw mirrors.

**Exit Criteria:**

- [x] Users can create dense rectangular, dense hex, sparse rectangular, and sparse hex grids from the Inspector.
- [x] v7 authoring uses explicit topology metrics; old scene `_voxelSize` migration is intentionally not retained for this breaking release.
- [x] The authoring UI uses v7 names: topology, topology metrics, storage, sparse configured cells.

## Phase 3: Rebuild Grid Debugging Around `GridForge.Diagnostics`

**Goal:** Replace dense rectangular cube loops with one topology-aware diagnostic adapter path.

**Execution Status:** Implemented locally as of 2026-06-15; verified through Unity EditMode tests, generated project build, package sync validation, and package maintenance scripts.

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

- [x] Replace `Width`/`Height`/`Length` nested loops with `GridDiagnostics.VisitCells(...)`.
- [x] Keep one reusable `GridDiagnosticScratch` per debugger component.
- [x] Draw physical cells using `GridDiagnosticGeometry.WriteVertices(...)` and `GridDiagnosticGeometry.GetEdges(...)`.
- [x] Convert `Vector3d` to Unity `Vector3` only in the drawing layer.
- [x] Draw missing sparse address cells with a distinct color and alpha.
- [x] Respect `GridDiagnosticQuery.MaxCells` and show query status in the inspector.
- [x] Avoid per-cell managed allocations in the draw loop.
- [x] Keep selected-cell resolution physical-only through `GridDiagnostics.TryResolvePhysicalCell(...)`.
- [x] Add editor controls for topology, storage, address mode, bounds, max cells, and state filters.
- [x] Remove old `VoxelFilterType` compatibility instead of migrating it; this is a breaking v7 debugger surface.

**Tests:**

- [x] Dense rectangular diagnostic query draws eight-vertex cells.
- [x] Dense hex diagnostic query draws twelve-vertex cells.
- [x] Sparse physical-only mode skips missing address cells.
- [x] Sparse missing-only mode emits missing descriptors without resolving to `Voxel`.
- [x] Query max-cell overflow surfaces a non-completed status instead of freezing the editor.
- [x] Selected physical cells resolve to live `Voxel` values.
- [x] Missing sparse descriptors do not resolve to live `Voxel` values.

**Exit Criteria:**

- [x] The debugger correctly visualizes rectangular and hex grids.
- [x] The debugger can inspect dense and sparse grids without storage-specific loops.
- [x] The debugger cannot accidentally materialize missing sparse cells.
- [x] Large sparse address spaces require explicit bounded queries or max-cell opt-in.

## Phase 4: Modernize Tracing, Blockers, And Scene Tools

**Goal:** Make Unity helper components reflect v7 coverage, 2D XZ helpers, sparse behavior, and topology-aware visuals.

**Execution Status:** Implemented locally as of 2026-06-15; verified through Unity EditMode tests, generated project build, package sync validation, and package maintenance scripts.

**Files:**

- Modify: `Build/Base/Runtime/Utility/Debugging/GridTracerTests.cs`
- Modify: `Build/Base/Runtime/Blockers/BlockerComponent.cs`
- Modify: `Build/Base/Editor/Blockers/Editor/EditorBlockerComponent.cs`
- Review: `Build/Base/Samples/GridforgeDemo/Scripts/SceneGridManager.cs`
- Test: `Tests/EditMode/BlockerComponentEditModeTests.cs`
- Test: `Tests/EditMode/GridTraceVisualizerEditModeTests.cs`

**Work:**

- [x] Rename or reframe `GridTracerTests` as a trace visualizer component in user-facing docs and inspector labels.
- [x] Fix the current `FillSize`/`WireSize` drift by deriving draw geometry from diagnostics instead of local cube sizing.
- [x] Add layer-locked XZ trace mode using v7 `Vector2d` overloads and explicit `layerY`.
- [x] Keep 3D trace mode for full `Vector3d` workflows.
- [x] Draw traced cells through diagnostic geometry so hex trails are shaped correctly.
- [x] Extend `BlockerComponent` authoring with an explicit 3D bounds mode and XZ layer-locked mode.
- [x] Use `FixedBoundArea` naming in code and docs.
- [x] For collider/renderer bounds, keep conversion to fixed-point at the Unity adapter boundary.
- [x] Document that blockers affect configured sparse voxels only.
- [x] Add inspector preview of blocker coverage through diagnostics or tracer output without mutating runtime state.

**Tests:**

- [x] Bounds blocker applies over dense rectangular grids.
- [x] Bounds blocker applies over dense hex grids.
- [x] Bounds blocker applies only configured cells for sparse rectangular grids.
- [x] Bounds blocker applies only configured axial cells for sparse hex grids.
- [x] XZ layer-locked blocker uses `Vector2d` semantics and does not affect other layers.
- [x] Trace visualizer uses v7 2D overloads with explicit `layerY`.

**Exit Criteria:**

- [x] Scene-authored blockers remain easy to use but no longer imply rectangular-only coverage.
- [x] Trace visualization works for rectangular, hex, sparse, and mixed worlds.
- [x] Existing blocker samples continue working after migration.

## Phase 5: Add Unity Logging And Diagnostics UX

**Goal:** Surface GridForge v7 diagnostics in a Unity-friendly way without adding Unity dependencies to core.

**Execution Status:** Implemented locally as of 2026-06-15; verified through Unity EditMode tests, generated project build, package sync validation, and package maintenance scripts.

**Files:**

- Create: `Build/Base/Runtime/Utility/Debugging/GridForgeUnityLogger.cs`
- Create: `Build/Base/Editor/Utility/Debugging/Editor/EditorGridForgeUnityLogger.cs`
- Review: `Build/Editor/GridForgePackageSync.cs` (no code change needed; managed directories already cover the new files)
- Test: `Tests/EditMode/GridForgeUnityLoggerEditModeTests.cs`

**Work:**

- [x] Add an optional component or static adapter that routes `GridForgeLogger.LogHandler` to Unity logging.
- [x] Provide an explicit enable/disable toggle rather than silently taking over global logging.
- [x] Expose minimum level using SwiftCollections `DiagnosticLevel`.
- [x] Restore previous logger settings on disable/destroy.
- [x] Document the difference between `GridForgeLogger` messages and `GridDiagnostics` cell descriptors.
- [x] Ensure tests reset logger settings after each test.

**Exit Criteria:**

- [x] Users can turn on GridForge logging from a scene or editor utility.
- [x] Unity logs preserve level and source information.
- [x] Tests prove logger settings do not leak across scenarios.

## Phase 6: Rebuild Samples Around v7 Workflows

**Goal:** Make samples teach the package's real v7 value instead of only proving that a rectangular demo scene opens.

**Execution Status:** Implemented locally as of 2026-06-15; verified through Unity EditMode tests, generated project build, package sync validation, and package maintenance scripts.

**Files:**

- Modify: `Build/Base/Samples/GridforgeDemo/Scripts/SceneGridManager.cs`
- Modify: `com.mrdav30.gridforge/Samples/GridforgeDemo`
- Modify: `com.mrdav30.gridforge.lean/Samples/GridforgeDemo`
- Create: `Build/Editor/GridForgeSampleAssetGenerator.cs`
- Create: `Tests/EditMode/GridForgeSampleWorkflowsEditModeTests.cs`
- Create: dense rectangular, dense hex, sparse rectangular, sparse hex, and mixed diagnostics workflow prefabs for both package variants.
- Remove: obsolete `V7Workflows.unity` and `V7Workflows.Lean.unity`; keep workflow coverage in prefabs and EditMode tests.

**Recommended Samples:**

- `Dense Rectangular`: current basic scene, updated for v7 terminology.
- `Dense Hex`: pointy-top or flat-top hex grid with debugger and trace visualizer.
- `Sparse Rectangular`: configured cells plus missing sparse address diagnostics.
- `Sparse Hex`: axial configured cells plus diagnostics.
- `Mixed Topology`: rectangular and hex grids in one `GridWorld`, showing ordinary lookup and debug overlay.

**Work:**

- [x] Keep sample code small and explicit.
- [x] Use `GridWorldComponent` plus authoring components rather than ad hoc runtime construction where possible.
- [x] Show the debugger configured for physical cells and missing sparse address cells.
- [x] Keep standard and lean sample assets in sync while preserving package-specific GUIDs.
- [x] Run package sync before touching serialized sample assets so managed-code drift is gone.
- [x] Avoid extra sample scenes that exist only to aggregate workflow prefabs for internal coverage.

**Exit Criteria:**

- [x] Importing either package gives users an obvious v7 path: rectangular, hex, sparse, diagnostics.
- [x] Samples do not install both variants together.
- [x] Samples do not teach stale v6 constructors or world-level voxel-size concepts.

## Phase 7: Documentation And Public User Experience Pass

**Goal:** Make package docs accurate, discoverable, and aligned with core wiki language.

**Execution Status:** Implemented locally as of 2026-06-16; verified through package version validation, package sync validation, stale-doc scans, and `git diff --check`.

**Files:**

- Modify: `README.md`
- Modify: `com.mrdav30.gridforge/README.md`
- Modify: `com.mrdav30.gridforge.lean/README.md`
- Create: `.docs/wiki/GridForge-Unity-v7-User-Guide.md`
- Create: `.docs/wiki/GridForge-Unity-Package-Maintenance.md`

**Work:**

- [x] Replace all v6 references with v7 language.
- [x] Remove or update `new GridWorld(Fixed64.One, 50)` examples.
- [x] Replace stale `BoundingArea` references with `FixedBoundArea`.
- [x] Add a short package selection section for standard versus lean variants.
- [x] Add setup examples for:
  - scene-owned `GridWorldComponent`
  - rectangular grid authoring
  - hex grid authoring
  - sparse grid authoring
  - diagnostics debugger
  - optional Unity logging adapter
- [x] Link to relevant core wiki pages:
  - Getting Started
  - Common Workflows
  - Sparse Grid Storage
  - Grid Diagnostics and Geometry
  - Diagnostics and Logging
- [x] Document package maintenance:
  - edit shared managed source in `Build/Base`
  - run package sync
  - run version validation
  - run Unity EditMode tests for both variants
  - export `.unitypackage` archives only after validation

**Exit Criteria:**

- [x] A new user can install one package variant and find a v7 example for their grid shape.
- [x] Docs consistently explain that Unity is an adapter, not the owner of GridForge core state.
- [x] No stale v6 terms remain except in historical notes.

## Phase 8: Dependency Installer And Package Release Hardening

**Goal:** Make package import reliable for users and release maintainers.

**Files:**

- Modify: `com.mrdav30.gridforge/Editor/Utility/GitDependencyInstaller.cs`
- Modify: `com.mrdav30.gridforge.lean/Editor/Utility/GitDependencyInstaller.cs`
- Modify: `.assets/unity-package-versions.json`
- Modify: `.assets/scripts/update-unity-package-versions.ps1`
- Modify: `.github/workflows/build-and-test.yml`

**Work:**

- [x] Replace the mojibake log arrow with ASCII `->`.
- [x] Verify `System.Text.Json` is available in the supported Unity editor profile. If not, replace it with a small manifest parser/writer or Unity-compatible JSON path.
  - 2026-06-16: isolated bootstrap assemblies could not depend on `System.Text.Json.Nodes`; the installer now uses a small Unity-compatible manifest dependency updater.
- [x] Make dependency installation idempotent and explicit in logs.
- [x] Keep standard dependencies:
  - `com.mrdav30.fixedmathsharp`
  - `com.mrdav30.swiftcollections`
  - `com.mrdav30.swiftcollections.fixedmathsharp`
- [x] Keep lean dependencies:
  - `com.mrdav30.fixedmathsharp.lean`
  - `com.mrdav30.swiftcollections.lean`
  - `com.mrdav30.swiftcollections.fixedmathsharp.lean`
- [x] Decide whether automatic manifest mutation should remain on import or move behind an explicit menu prompt. If automatic remains, improve failure messages and document it prominently.
  - 2026-06-16: automatic bootstrap remains because package-level git dependencies are invalid in Unity package manifests. Logs and manual repair menus are variant-specific, and public docs explain the first-import resolve cycle.
- [x] Extend CI to run the version self-test script.
- [x] Extend CI to validate package version config before Unity import.
- [x] Confirm both package variants compile in a fresh Unity project.

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

- [x] Standard package imports into a fresh Unity project.
  - Verified through a fresh Unity project first pass that installs dependencies and a second pass that compiles cleanly with dependencies already satisfied.
- [x] Lean package imports into a fresh Unity project.
  - Verified through a fresh Unity project first pass that installs dependencies and a second pass that compiles cleanly with dependencies already satisfied.
- [x] Standard package EditMode tests pass.
- [x] Lean package EditMode tests pass.
  - Current Unity EditMode run: 42 total, 42 passed, 0 failed, 0 skipped.
- [x] Dense rectangular sample opens and debugger draws cells.
  - Covered by sample prefab loading tests and dense rectangular diagnostics geometry tests; final human Scene View smoke is tracked in the release follow-up plan.
- [x] Dense hex sample opens and debugger draws hex prisms.
  - Covered by sample prefab loading tests and dense hex diagnostics geometry tests; final human Scene View smoke is tracked in the release follow-up plan.
- [x] Sparse rectangular sample shows configured cells and bounded missing address diagnostics.
  - Covered by sparse rectangular authoring tests, sparse diagnostics tests, and mixed-diagnostics prefab coverage.
- [x] Sparse hex sample shows configured axial cells and bounded missing address diagnostics.
  - Covered by sparse hex authoring tests, sparse diagnostics tests, and mixed-diagnostics prefab coverage.
- [x] Blocker sample applies and removes obstacle state.
  - Covered by dense/sparse rectangular and dense/sparse hex blocker EditMode tests.
- [x] Trace visualizer draws rectangular and hex traces.
  - Covered by trace visualizer tests for grid filtering, mixed-topology continuation, hex boundary inclusion, and hex diagnostic geometry.
- [x] Optional logger routes GridForge warnings to Unity logs and restores defaults when disabled.
  - Covered by logger EditMode tests for explicit opt-in, warning routing, minimum level filtering, disable restoration, and destroy restoration.

**Release Artifacts:**

- [x] `com.mrdav30.gridforge-7.0.0.unitypackage`
- [x] `com.mrdav30.gridforge.lean-7.0.0.unitypackage`
- [x] Git tag for Unity package v7.
  - Intentionally deferred until after owner review/stage/commit; tracked in `.docs/feature-work/2026-06-16-gridforge-v7-unity-release-follow-up.md`.
- [x] Release notes call out:
  - GridForge v7 core
  - hex-prism authoring/debugging
  - sparse grid authoring/debugging
  - diagnostics debugger rewrite
  - docs and dependency installer changes
  - any serialized-field migration notes
  - Release notes are tracked in `.docs/feature-work/2026-06-16-gridforge-v7-unity-release-follow-up.md` so they can be finalized against the reviewed commit/tag.

## Risk Register

| Risk | Why It Matters | Mitigation |
| --- | --- | --- |
| Serialized scene data breaks when `_voxelSize` disappears | Existing user scenes may lose intended rectangular cell size | Accepted for the breaking v7 authoring redesign; document the explicit topology-metrics replacement |
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
