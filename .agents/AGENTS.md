# AGENTS.md

## Repo Scope

- The actual Git repo root is the `Assets/Packages` directory, not the outer Unity project root.
- This repo ships two Unity Package Manager variants:
  `com.mrdav30.gridforge` and `com.mrdav30.gridforge.lean`.
- Both package variants are currently using GridForge `v7.1.4`.
- The old `.variants/` workflow is retired and should not be reintroduced.

## Source Of Truth

- Shared managed code now lives under `Build/Base/`.
- Package-specific content stays in the visible package folders:
  `com.mrdav30.gridforge/` and `com.mrdav30.gridforge.lean/`.
- The build tooling lives under `Build/Editor/`.
- `Build/GridForge.Build.asmdef` exists only to support the build/editor tooling and is not part of either shipped package.
- GridForge-Unity is an adapter package. Core grid behavior belongs in the
  upstream GridForge repo; fixed-point math belongs in FixedMathSharp; Unity
  math serialization/editor support belongs in FixedMathSharp-Unity; collection
  behavior and Unity collection adapters belong in SwiftCollections and
  SwiftCollections-Unity.

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
- Unity `.meta` files are Unity-owned. Do not manually create or edit them.
- Serialized sample prefabs and scenes are package-specific assets. Prefer
  regenerating them through `GridForgeSampleAssetGenerator` instead of hand
  editing YAML.

## Unity Sample Authoring And Export Layout

- Package samples use two folder shapes: `Samples/` for local authoring and
  `Samples~/` for distributable Git/package-source installs.
- Package manifests should point sample entries at `Samples~/...`. Unity hides
  tilde folders in the Project pane, so edit sample scenes, prefabs, and scripts
  through the local `Samples/` mirror.
- Package-local `.gitignore` files ignore `Samples/`, `Samples.meta`, and
  `Samples~.meta`. Do not commit package-variant `Samples/` folders or
  top-level `Samples~.meta` files.
- Keep nested `.meta` files inside `Samples~/GridforgeDemo` and local
  `Samples/GridforgeDemo`. They preserve scene, prefab, asmdef, and script
  references when Unity imports samples into `Assets/Samples/...`.
- Shared sample scripts live in `Build/Base/Samples/GridforgeDemo/Scripts`.
  The sync step hydrates missing package-local `Samples/` mirrors from tracked
  `Samples~/`, then copies shared scripts into the visible authoring mirror.
- The exporter overwrites each package `Samples~/` folder from its local
  `Samples/` mirror and excludes `Samples/` from `.unitypackage` exports.

## Editing Rules

- Do not hand-edit the same shared managed file in both package folders.
- Edit shared managed files in `Build/Base/`, then propagate them through the sync tool.
- Edit package-specific files directly in the owning package folder.
- If you add a new shared managed path, update the `ManagedEntries` list in `Build/Editor/GridForgePackageSync.cs`.
- If dependency behavior changes, update both package-specific `Editor/Utility/GitDependencyInstaller.cs` files and the repo `README.md` together.
- Preserve the Unity package structure when moving or adding files.
- When Unity serialization behaves badly, check the lower-stack Unity packages
  before adding GridForge-owned compatibility mirrors. Do not add raw Fixed64
  fields, custom FixedMathSharp drawers, or GridForge-specific serialized
  collection variants when FixedMathSharp-Unity or SwiftCollections-Unity
  already owns that problem.
- Do not preserve compatibility state unless explicitly requested.

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
- The exporter regenerates distributable `Samples~/` folders from local
  authoring `Samples/` folders before packaging.
- Use `.assets/scripts/sync-gridforge-unity-packages.ps1` for batch package sync.
- Use `.assets/scripts/run-gridforge-unity-editmode-tests.ps1` for EditMode
  tests. Do not add `-quit` to Unity `-runTests`; the Unity Test Framework exits
  after writing the result XML.
- Use `.assets/scripts/update-unity-package-versions.ps1 -ValidateOnly` to
  check package version and dependency installer drift.
- Use `.assets/scripts/test-gridforge-package-sync.ps1` to verify `Build/Base`
  managed files match both package variants.

## Package Layout Notes

- `Plugins/GridForge.dll` is a precompiled upstream GridForge dependency that
  lives inside each package variant.
- `Plugins/GridForge.xml` is useful for API discovery when the core GridForge source is not locally available.
- The runtime code in this repo is intentionally thin. If a change belongs in the core data-structure library rather than Unity integration, it likely belongs in the upstream GridForge repo instead.
- Each package folder keeps its own install README, manifest, asmdefs, dependencies, and serialized sample assets.
- Package manifests intentionally keep `dependencies` empty. Dependency repair
  is handled by the package-specific installer scripts and version config.

## Coding Expectations

- Prefer reusing the lowest owning layer before writing new GridForge-Unity
  code. Do not reimplement FixedMathSharp, FixedMathSharp-Unity,
  SwiftCollections, SwiftCollections-Unity, Chronicler, or GridForge core
  behavior in this adapter package.
- Prefer SwiftCollections types and helpers over .NET/BCL collections whenever a suitable SwiftCollections type exists.
- Do not introduce `List<>`, `Dictionary<>`, `HashSet<>`, `Stack<>`, or similar .NET collections in package code unless there is no SwiftCollections equivalent and the reason is explicit.
- Arrays and `IEnumerable<T>` are still acceptable for Unity-backed serialized
  data, interop boundaries, and APIs where they are the natural representation.
- Direct `SwiftList<T>`, `SwiftDictionary<TKey,TValue>`, and related core
  collection types are runtime collections, not Unity-persisted fields. For
  Unity serialized fields, use SwiftCollections-Unity `SerializedSwift*`
  adapters and consume the real collection through `.Runtime`.
- Store FixedMathSharp types directly in GridForge authoring data. Do not add
  GridForge-owned raw-value mirrors for `Fixed64`, `Vector2d`, or `Vector3d`;
  fix or consume FixedMathSharp-Unity support instead.
- Keep deterministic math in FixedMathSharp and GridForge types. Convert Unity
  `float`, `Vector2`, `Vector3`, `Bounds`, `Transform`, collider, and renderer
  data at the adapter boundary only.
- Prefer GridForge APIs for topology, storage, diagnostics, tracing, and
  logging. For debugging visuals, consume `GridForge.Diagnostics` geometry and
  descriptors instead of recreating rectangular-only loops in Unity code.
- Keep this repo focused on Unity integration instead of re-implementing GridForge core behavior here.

## Verification

- Automated EditMode tests live under `Tests/EditMode`.
- Primary CI is `.github/workflows/build-and-test.yml`. It runs EditMode tests
  for both standard and lean packages on Unity `6000.5.0f1`.
- A main-only sample import smoke job installs both package variants via local
  Git URL, imports the Demo Scene sample, and scans Unity logs for known sample
  packaging failures.
- Standard verification after managed code changes:
  - `.assets/scripts/test-gridforge-package-sync.ps1`
  - `.assets/scripts/update-unity-package-versions.ps1 -ValidateOnly`
  - `.assets/scripts/sync-gridforge-unity-packages.ps1`
  - `.assets/scripts/run-gridforge-unity-editmode-tests.ps1`
  - `git diff --check`
- Generated Unity `.csproj` builds can be useful after Unity import has produced
  the project files. For example:
  `dotnet build F:\gamedevrepos\GridForge-Unity\GridForge.Unity.Tests.EditMode.csproj`
- If Unity tests cannot be run, state that clearly and do not claim the package
  is verified.
