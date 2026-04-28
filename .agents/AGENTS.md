# AGENTS.md

## Repo Scope

- The actual Git repo root is the `Assets/Packages` directory, not the outer Unity project root.
- This repo ships two Unity Package Manager variants:
  `com.mrdav30.gridforge` and `com.mrdav30.gridforge.lean`.
- Both package variants are currently aligned to GridForge `v6.0.0`.
- The old `.variants/` workflow is retired and should not be reintroduced.

## Source Of Truth

- Shared managed code now lives under `Build/Base/`.
- Package-specific content stays in the visible package folders:
  `com.mrdav30.gridforge/` and `com.mrdav30.gridforge.lean/`.
- The build tooling lives under `Build/Editor/`.
- `Build/GridForge.Build.asmdef` exists only to support the build/editor tooling and is not part of either shipped package.

## Managed Vs Unmanaged Content

- `Build/Base/` is for shared managed code only.
- The sync tool currently manages these shared paths into both package variants:
  - `Editor/Blockers`
  - `Editor/Configuration`
  - `Editor/Utility/Debugging`
  - `Runtime/Blockers`
  - `Runtime/Configuration`
  - `Runtime/GridWorldComponent.cs`
  - `Runtime/Utility/Debugging`
  - `Samples/GridforgeDemo/Scripts`
- The shared sample script path is intentional. Shared serialized sample assets are not.
- Package-specific asmdefs, package manifests, plugin payloads, installer scripts, README files, and serialized sample assets remain unmanaged and must be edited in the package-specific folder that owns them.
- If a file should differ between standard and lean, it does not belong in `Build/Base/`.

## Editing Rules

- Do not hand-edit the same shared managed file in both package folders.
- Edit shared managed files in `Build/Base/`, then propagate them through the sync tool.
- Edit package-specific files directly in the owning package folder.
- If you add a new shared managed path, update the `ManagedEntries` list in `Build/Editor/GridForgePackageSync.cs`.
- If dependency behavior changes, update both package-specific `Editor/Utility/GitDependencyInstaller.cs` files and the repo `README.md` together.
- Preserve the Unity package structure when moving or adding files.

## Tooling

- Sync shared managed code from the Unity Editor via:
  `Tools > GridForge > Sync Managed Package Files`
- Sync from Unity batchmode via:
  `-executeMethod GridForge.Build.Editor.GridForgePackageSync.SyncPackagesBatchMode`
- Export both `.unitypackage` archives from the Unity Editor via:
  `Tools > GridForge > Export Unity Packages`
- Export from Unity batchmode via:
  `-executeMethod GridForge.Build.Editor.GridForgeUnityPackageExporter.ExportUnityPackagesBatchMode`
- Batchmode export optionally accepts:
  `-gridforgeUnityPackageOutputPath <folder>`
- The exporter runs the shared-code sync before packaging.

## Package Layout Notes

- `Plugins/GridForge.dll` is a precompiled upstream dependency that lives inside each package variant.
- `Plugins/GridForge.xml` is useful for API discovery when the core GridForge source is not locally available.
- The runtime code in this repo is intentionally thin. If a change belongs in the core data-structure library rather than Unity integration, it likely belongs in the upstream GridForge repo instead.
- Each package folder keeps its own install README, manifest, asmdefs, dependencies, and serialized sample assets.

## Coding Expectations

- Prefer SwiftCollections types and helpers over .NET/BCL collections whenever a suitable SwiftCollections type exists.
- Do not introduce `List<>`, `Dictionary<>`, `HashSet<>`, `Stack<>`, or similar .NET collections in package code unless there is no SwiftCollections equivalent and the reason is explicit.
- Keep this repo focused on Unity integration instead of re-implementing GridForge core behavior here.

## Verification

- There are currently no automated tests set up for this package repo.
- Command-line `dotnet build` may fail outside a proper Unity environment because Unity-generated `.csproj` files can reference local Unity analyzers and source generators that are not available on every machine.
- Prefer verification in the Unity Editor when possible, and call out environment limitations clearly when CLI validation is incomplete.
