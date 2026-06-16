# GridForge Unity Package Maintenance

This page records the release and validation workflow for the Unity package
host. It complements the public user guide and the phase plan in
`.docs/feature-work`.

## Source Ownership

- `Build/Base` is the shared managed-code source of truth.
- `com.mrdav30.gridforge` and `com.mrdav30.gridforge.lean` keep package
  metadata, plugins, asmdefs, samples, and Unity-generated `.meta` files.
- Do not manually create or edit `.meta` files.
- New bundled source files should carry the project license header unless the
  file is part of an intentionally excluded sample project.
- Prefer lower-stack APIs from FixedMathSharp, FixedMathSharp-Unity,
  SwiftCollections, SwiftCollections-Unity, GridForge, and Chronicler before
  adding Unity-side replacements.

## Package Variants

Standard package:

- `com.mrdav30.gridforge`
- dependency repair menu: `Tools > GridForge > Repair Dependencies`
- depends on standard FixedMathSharp-Unity and SwiftCollections-Unity variants

Lean package:

- `com.mrdav30.gridforge.lean`
- dependency repair menu: `Tools > GridForge.Lean > Repair Dependencies`
- depends on lean FixedMathSharp-Unity and SwiftCollections-Unity variants
- omits the `MemoryPack` dependency chain

The two package variants overlap and should not be installed together.

Each package has a dependency-installer bootstrap assembly under
`Editor/Utility/DependencyInstaller`. The bootstrapper has no FixedMathSharp or
SwiftCollections assembly references, so it can compile during a fresh import,
write the required git dependencies into `Packages/manifest.json`, and trigger a
package resolve before the main GridForge assemblies compile.

Fresh-import validation should check the settled compile after the bootstrap
updates the manifest. A first pass can log transient unresolved plugin
references before dependency resolution completes; the second pass must be clean.

## Core DLL Intake

Until GridForge v7 is released, the package `Plugins/GridForge.dll` and
`Plugins/GridForge.xml` files are sourced from local core builds:

```powershell
dotnet build F:\gamedevrepos\GridForge -c Release
dotnet build F:\gamedevrepos\GridForge -c ReleaseLean
```

After updating embedded DLL/XML files, record the core commit and hashes in
`.assets/gridforge-core-source.json`.

## Required Validation

Run the lightweight package metadata checks:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\test-update-unity-package-versions.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\update-unity-package-versions.ps1 -ValidateOnly
```

Run shared-source drift checks:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\test-gridforge-package-sync.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\sync-gridforge-unity-packages.ps1 -WhatIf
```

Run Unity EditMode tests for both package variants:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\run-gridforge-unity-editmode-tests.ps1
```

Check export wiring before producing release archives:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets\scripts\export-gridforge-unity-packages.ps1 -WhatIf
```

Finish with whitespace validation:

```powershell
git diff --check
```

## Unity Menus

- Sync shared managed source from Unity:
  `Tools > GridForge > Sync Managed Package Files`
- Export package archives from Unity:
  `Tools > GridForge > Export Unity Packages`

Use Unity-generated package exports only after version validation, package sync
validation, and EditMode tests pass.
