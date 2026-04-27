# AGENTS.md

## Repo Scope

- The actual Git repo root is the `Assets/Packages` directory, not the outer Unity project root.
- This repo ships the Unity packages `com.mrdav30.gridforge` and `com.mrdav30.gridforge.lean`, both currently at version `6.0.0`.
- The authored source of truth lives under `.variants/gridforge/`.
- `.variants/gridforge/build-packages.py` stages variants through Unity batchmode, emits package outputs under `UpmPackages~/`, and mirrors them back into the visible package folders unless `--skip-visible-sync` is used.

## Package Layout

- `.variants/gridforge/base/` contains the shared authored package content.
- `.variants/gridforge/standard/` and `.variants/gridforge/lean/` contain variant-only overrides such as package metadata, asmdefs, dependency installers, and plugin payload.
- `python3 .variants/gridforge/sync_variants.py bootstrap --force` refreshes the hidden source tree from the legacy visible package folders when needed.
- `python3 .variants/gridforge/build-packages.py` stages variants through Unity, writes package outputs to `UpmPackages~/`, and refreshes the visible `com.mrdav30.gridforge*` folders from those generated results.
- `Plugins/GridForge.dll` is the precompiled core library. Most collection behavior lives there, not in this repo.
- `Plugins/GridForge.xml` is useful for API discovery when the core source is not locally available.
- `Runtime/` contains the package runtime assembly.
- `Editor/Utility/GitDependencyInstaller.cs` manages required Unity package dependencies.
- `README.md`, `package.json`, `LICENSE`, `NOTICE`, and `COPYRIGHT` are part of the shipped package surface.

## Coding Expectations

- Prefer SwiftCollections types and helpers over .NET/BCL collections whenever a suitable SwiftCollections type exists.
- Do not introduce `List<>`, `Dictionary<>`, `HashSet<>`, `Stack<>`, or similar .NET collections in package code unless there is no SwiftCollections equivalent and the reason is explicit.
- Keep this package as a thin Unity wrapper around GridForge rather than re-implementing core collection behavior here.
- Preserve Unity package structure when moving or adding assets.
- Edit shared files in `.variants/gridforge/base/` and variant-specific files in the matching override folder, then rebuild through `.variants/gridforge/build-packages.py` instead of hand-editing both variants.
- Agents do not need to generate associated Unity `*.meta` files for newly created assets. Unity Editor will regenerate them on load.

## Dependencies

- The standard editor installer ensures `com.mrdav30.fixedmathsharp` and `com.mrdav30.swiftcollections` are present via Git URL.
- The lean editor installer ensures `com.mrdav30.fixedmathsharp.lean` and `com.mrdav30.swiftcollections.lean` are present via Git URL.
- If dependency behavior changes, update the variant override installers, the root README, and the hidden workflow docs together.

## Verification

- There are currently no automated tests set up for this package.
- Command-line `dotnet build` may fail outside a proper Unity environment because Unity-generated `.csproj` files can reference local Unity analyzers and source generators that are not available on every machine.
- Prefer verification in the Unity Editor when possible, and call out environment limitations clearly when CLI validation is incomplete.

## Known Project Context

- The runtime code in this repo is intentionally small. If a change seems to belong in the core data-structure library rather than Unity integration, it probably belongs in the upstream GridForge project instead.
