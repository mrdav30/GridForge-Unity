# GridForge-Unity

Unity package host for [GridForge](https://github.com/mrdav30/GridForge).

This repository contains two installable Unity Package Manager variants. Choose
one package only. The variants overlap and are not meant to be installed
together.

## Which Package Should I Use?

| Package | Use it when | Install |
| --- | --- | --- |
| `com.mrdav30.gridforge` | You want the default GridForge Unity package with the standard dependency chain. | `https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge` |
| `com.mrdav30.gridforge.lean` | You want the same Unity integration without the `MemoryPack` dependency chain. Prefer this for Burst AOT or your own serialization stack. | `https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge.lean` |

## How The Variants Differ

`Lean` variants:

- Omit the `MemoryPack` dependency chain.
- Prefer these when Burst AOT compatibility or a custom serialization layer is
  more important than the default serialization path.

Shared behavior:

- Both packages target GridForge v6 and use explicit `GridWorld` ownership.
- Both packages include the same Unity-facing helpers such as
  `GridWorldComponent`, `GridConfigurationSaver`, `BlockerComponent`,
  `GridDebugger`, and `GridTracerTests`.
- If you use multiple worlds in one scene, assign the intended
  `GridWorldComponent` explicitly on blockers and debugging helpers instead of
  relying on auto-resolution.

## Dependency Handling

Each package includes an editor-side dependency installer that attempts to add
the matching `FixedMathSharp-Unity` and `SwiftCollections-Unity` package
variants for you.

If Unity does not resolve those dependencies cleanly, use the matching install
URLs below or run the package repair menu item under `Tools > mrdav30`.

- Standard dependencies:
  `https://github.com/mrdav30/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp`
  and
  `https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections`
- Lean dependencies:
  `https://github.com/mrdav30/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp.lean`
  and
  `https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.lean`

## Notes

- All packages in this repo target Unity `2022.3+`.
- The underlying .NET library lives here:
  [GridForge](https://github.com/mrdav30/GridForge)
- Each package folder keeps a short, package-specific install README with
  `GridWorld` usage examples.
- Repo maintenance note:
  `Assets/Packages/Build/Base/` is the shared managed-code source of truth.
  The package-specific folders keep their own package metadata, plugins, asmdefs,
  and serialized sample assets.
- Sync shared managed code from Unity via
  `Tools > GridForge > Sync Managed Package Files`.
- Sync from the command line via Unity batchmode and
  `-executeMethod GridForge.Build.Editor.GridForgePackageSync.SyncPackagesBatchMode`.
