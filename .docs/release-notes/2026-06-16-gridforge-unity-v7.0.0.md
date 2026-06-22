# GridForge Unity v7.0.0 Release Notes

GridForge Unity v7.0.0 aligns the Unity packages with the GridForge v7 core
release and refreshes the public Unity authoring, diagnostics, samples, docs,
and package validation workflow.

## Highlights

- Updated both Unity package variants to GridForge v7.0.0 release artifacts.
- Added topology-aware authoring for rectangular-prism and hex-prism grids.
- Added explicit dense and sparse grid authoring, including configured sparse
  cells backed by SwiftCollections-Unity serialization adapters.
- Rebuilt the Unity debugger around `GridForge.Diagnostics` so rectangular,
  hex, physical sparse, and missing sparse address diagnostics all use core
  diagnostic geometry.
- Modernized blockers and the trace visualizer around v7 coverage APIs,
  layer-locked XZ tracing, mixed-topology tracing, and diagnostic gizmos.
- Added an optional Unity logging adapter for GridForge diagnostics.
- Rebuilt the demo samples around dense rectangular, dense hex, sparse
  rectangular, sparse hex, and mixed-topology diagnostics workflows.
- Hardened dependency bootstrapping for FixedMathSharp-Unity and
  SwiftCollections-Unity, including variant-specific repair menus and CI
  validation.
- Added a generated CI smoke-project path so each package variant is tested in a
  Package Manager-style Unity project with the same dependency versions used by
  the bootstrap installers.

## Breaking Changes

- Legacy world-level voxel-size authoring is removed. Cell geometry now lives in
  explicit per-grid topology metrics.
- Old debugger filter compatibility is removed. The debugger now exposes v7
  diagnostic query concepts directly.
- Sparse grids treat bounds as address space. Only configured sparse cells are
  physical voxels; missing sparse addresses are diagnostic descriptors and do
  not resolve to live `Voxel` instances.

## Validation

- Package version self-test passed.
- Package version validation passed.
- Shared source sync validation passed.
- Generated standard CI smoke project EditMode tests passed: 39 total, 39
  passed.
- Generated lean CI smoke project EditMode tests passed: 39 total, 39 passed.
- Full repository Unity EditMode tests passed: 42 total, 42 passed.
- Package sync completed with 0 copied, 0 deleted, and 0 removed files.
- Package export completed for:
  - `com.mrdav30.gridforge-7.0.0.unitypackage`
  - `com.mrdav30.gridforge.lean-7.0.0.unitypackage`
