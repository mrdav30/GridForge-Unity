# AGENTS.md

## Repo Scope

- The actual Git repo root is the `Assets/Packages` directory, not the outer Unity project root.
- This repo ships the Unity package `com.mrdav30.gridforge` and is currently at version `6.0.0`.

## Package Layout

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
- Agents do not need to generate associated Unity `*.meta` files for newly created assets. Unity Editor will regenerate them on load.

## Dependencies

- The editor installer currently ensures `com.mrdav30.fixedmathsharp` and `com.mrdav30.swiftcollections` are present via Git URL.
- If dependency behavior changes, update both the installer logic and any user-facing installation docs.

## Verification

- There are currently no automated tests set up for this package.
- Command-line `dotnet build` may fail outside a proper Unity environment because Unity-generated `.csproj` files can reference local Unity analyzers and source generators that are not available on every machine.
- Prefer verification in the Unity Editor when possible, and call out environment limitations clearly when CLI validation is incomplete.

## Known Project Context

- The runtime code in this repo is intentionally small. If a change seems to belong in the core data-structure library rather than Unity integration, it probably belongs in the upstream GridForge project instead.
