# GridForge for Unity Migration Guide

## Migrating from v9 to v10

Both packages now require Unity **6.3 LTS (6000.3) or newer**. Upgrade the
editor before adopting v10; older editors should keep v9 packages. CI validates
Unity 6000.3.9f1.

Update your selected Standard or Lean GridForge-Unity package to v10.0.0 and
its FixedMathSharp-Unity, SwiftCollections-Unity base, and SwiftCollections
FixedMathSharp companion dependencies to v8.0.0 together. Update existing pins
in `Packages/manifest.json` before opening Unity to avoid compiling the new core
against old dependencies. Do not mix Standard and Lean variants. The package's
Repair Dependencies menu can restore missing dependency entries.

Both variants bundle the released GridForge v10.0.0 .NET Standard 2.1 core.
The lower-stack Unity packages supply Chronicler v1.0.0 support, including
`Chronicler.Diagnostics`; do not copy another set of these DLLs into GridForge.
Lean continues to omit the MemoryPack runtime.

The scene authoring, blocker, diagnostics, and trace visualization APIs are
unchanged. Scripts that use logging or core collections need these updates:

| v9 dependency API | v10 dependency API |
| --- | --- |
| `SwiftCollections.Diagnostics.DiagnosticLevel` | `Chronicler.Diagnostics.DiagnosticLevel` |
| `SwiftBucket<T>` | `SwiftSlotMap<T>` |
| `SwiftGenerationalBucket<T>` | `SwiftTicketMap<T>` |
| `SwiftHandle` | `SwiftTicket` |

`GridForgeUnityLogger.MinimumLevel`, `GetUnityLogType`, and
`FormatUnityLogMessage` now use the new `DiagnosticLevel` type. Update imports
and rebuild dependent assemblies. If a custom asmdef uses `overrideReferences`
and consumes these APIs, add `Chronicler.Diagnostics.dll` to its precompiled
references. The logger keeps its serialized field names and diagnostic values.

`GridWorld.ActiveGrids` now exposes `SwiftSlotMap<VoxelGrid>`. Update explicit
collection, state, and nested enumerator types. Persisted CLR type names and
serializer registrations for renamed collections need explicit migration.
Re-run serialization and simulation tests: upstream fixed-point geometry and
hex lattice corrections can change query results.

See the [GridForge v10 migration guide](https://github.com/mrdav30/GridForge/blob/v10.0.0/docs/MIGRATION.md),
[FixedMathSharp-Unity v8 migration guide](https://github.com/mrdav30/FixedMathSharp-Unity/blob/v8.0.0/MIGRATION.md),
and [SwiftCollections-Unity v8 migration guide](https://github.com/mrdav30/SwiftCollections-Unity/blob/v8.0.0/MIGRATION.md)
for the complete upstream changes.
