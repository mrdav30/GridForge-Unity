# GridForge v7 Unity Release Follow-Up

**Status:** Open.

**Context:** The GridForge v7 Unity package migration plan is complete and archived under `.docs/feature-work/done`. This follow-up holds the intentionally deferred release ceremony and human visual smoke work that should happen after owner review, staging, and commit.

## Goal

Finish the public v7 Unity package release without reopening migration implementation work.

## Already Completed

- Fresh standard package import was validated in a temporary Unity project.
- Fresh lean package import was validated in a temporary Unity project.
- Unity EditMode tests passed: 42 total, 42 passed, 0 failed, 0 skipped.
- Package sync validation passed.
- Package version validation passed.
- Package export completed for:
  - `F:\gamedevrepos\GridForge-Unity\UnityPackageExports~\com.mrdav30.gridforge-7.0.0.unitypackage`
  - `F:\gamedevrepos\GridForge-Unity\UnityPackageExports~\com.mrdav30.gridforge.lean-7.0.0.unitypackage`

## Follow-Up Work

- [ ] After owner review, stage and commit the completed migration work.
- [ ] Re-run package export after the final commit if any reviewed changes affect package contents.
- [ ] Create the Unity package v7 git tag from the clean reviewed commit.
- [ ] Publish release notes that call out:
  - GridForge v7 core alignment
  - rectangular, hex-prism, dense, and sparse authoring workflows
  - diagnostics debugger rewrite on `GridForge.Diagnostics`
  - trace visualizer and blocker modernization
  - optional Unity logging adapter
  - FixedMathSharp-Unity and SwiftCollections-Unity serialization expectations
  - dependency bootstrap behavior and manual repair menu names
  - breaking serialized-field migration notes, especially the removal of legacy voxel-size compatibility
- [ ] Perform final human Scene View smoke in Unity:
  - Open `F:\gamedevrepos\GridForge-Unity`.
  - Open `Assets/Packages/com.mrdav30.gridforge/Samples/GridforgeDemo/Scenes/DemoScene.unity`.
  - Select the sample manager and cycle through `DenseRectangular`, `DenseHex`, `SparseRectangular`, `SparseHex`, and `MixedTopologyDiagnostics`.
  - Confirm the debugger draws rectangular cells, hex prisms, sparse physical cells, and bounded missing sparse address diagnostics.
  - Move trace start/end objects across rectangular-only, hex-only, and mixed rectangular-to-hex paths; confirm the trace continues at topology boundaries.
  - Toggle the blocker sample and confirm obstacle state applies/removes in the visualized grid.
  - Enable the optional Unity logger and confirm warnings route to Unity logs, then disable it and confirm defaults restore.
  - Repeat the same pass for `Assets/Packages/com.mrdav30.gridforge.lean/Samples/GridforgeDemo/Scenes/DemoScene.Lean.unity`.

## Close Criteria

- Reviewed commit exists.
- Final exports match the reviewed commit.
- Git tag exists on the reviewed commit.
- Release notes are published.
- Standard and lean demo scenes pass the final human visual smoke.
