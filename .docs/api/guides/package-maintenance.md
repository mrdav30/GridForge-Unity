# Package maintenance

This contributor guide records the release and validation workflow for the
Unity package host. Public user guidance lives beside it in the other
`.docs/api/guides` pages.

## Source ownership

- `Build/Base` is the shared managed-code source of truth.
- `com.mrdav30.gridforge` and `com.mrdav30.gridforge.lean` own their package
  metadata, plugins, asmdefs, install READMEs, dependencies, and serialized
  sample assets.
- `Build/Base/CHANGELOG.md` is the canonical changelog and syncs into both
  variants.
- Do not manually create or edit Unity `.meta` files.
- Prefer lower-stack APIs from FixedMathSharp, FixedMathSharp-Unity,
  SwiftCollections, SwiftCollections-Unity, GridForge, and Chronicler before
  adding adapter-local replacements.

## Package variants

| Variant | Repair menu | Dependency family |
| --- | --- | --- |
| `com.mrdav30.gridforge` | `Tools > GridForge > Repair Dependencies` | Standard |
| `com.mrdav30.gridforge.lean` | `Tools > GridForge.Lean > Repair Dependencies` | Lean, without the MemoryPack runtime chain |

The variants define overlapping assemblies and must not be installed together.

Each package has a dependency bootstrap assembly under
`Editor/Utility/DependencyInstaller`. It compiles before the main GridForge
assemblies, writes pinned dependency URLs into `Packages/manifest.json`, and
triggers package resolution. Fresh-import validation checks the settled compile
after that bootstrap completes.

## Core DLL intake

The package `Plugins/GridForge.dll`, `.pdb`, and `.xml` files come from matching
GridForge release builds:

```powershell
$GridForgeRoot = "C:\path\to\GridForge"
$GridForgeProject = Join-Path $GridForgeRoot "src\GridForge\GridForge.csproj"

dotnet build $GridForgeProject -c Release
dotnet build $GridForgeProject -c ReleaseLean
```

Keep the Standard DLL in the Standard package and the Lean DLL in the Lean
package.

## Required validation

From the `Assets/Packages` Git root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets/scripts/test-update-unity-package-versions.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets/scripts/update-unity-package-versions.ps1 -ValidateOnly
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets/scripts/test-gridforge-package-sync.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets/scripts/sync-gridforge-unity-packages.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets/scripts/run-gridforge-unity-editmode-tests.ps1
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet tool run docfx .docs/api/docfx.json --warningsAsErrors
git diff --check
```

Run the package sync a second time. A clean second pass must report `Copied 0
files`.

Use `-WhatIf` with the sync or export scripts when you only need to inspect the
planned action. The exporter syncs shared code and regenerates distributable
`Samples~/` content from the package-local authoring mirrors before creating
archives.

## Unity menus

- Sync shared managed source: **Tools → GridForge → Sync Managed Package Files**
- Export both package archives: **Tools → GridForge → Export Unity Packages**

Produce release archives only after version validation, source sync, EditMode
tests, sample import smoke coverage, and DocFX validation are clean.
