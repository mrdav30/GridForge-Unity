# GridForge for Unity

The Standard Unity adapter package: scene-owned worlds, serialized grid
authoring, blockers, diagnostics, logging, and the Demo Scene. This variant uses
the Standard MemoryPack-enabled GridForge dependency family.

Requires Unity 2022.3 or newer.

[Documentation](https://mrdav30.github.io/GridForge-Unity/) ·
[Getting started](https://mrdav30.github.io/GridForge-Unity/guides/getting-started.html) ·
[Changelog](CHANGELOG.md)

## Install

In **Window → Package Manager**, choose **+ → Install package from git URL…**:

```text
https://github.com/mrdav30/GridForge-Unity.git?path=/com.mrdav30.gridforge#v9.0.0
```

Do not install the Lean variant in the same project. In a custom asmdef,
reference `GridForge.Runtime`.

## First scene

1. Add `GridWorldComponent` to the GameObject that owns the runtime world.
2. Add `GridConfigurationSaver` and author a rectangular or hex, dense or sparse
   configuration.
3. From a bootstrap `MonoBehaviour`, apply the saved configurations after the
   world component initializes:

   ```csharp
   _configurationSaver.EarlyApply(
       _gridWorldComponent.RebuildWorld(_configurationSaver.SpatialGridCellSize));
   ```

   The
   [getting-started guide](https://mrdav30.github.io/GridForge-Unity/guides/getting-started.html)
   includes the complete component.
4. Import **Demo Scene** from this package's Samples tab for working examples of
   configuration, blockers, diagnostics, tracing, and logging.

The dependency bootstrapper installs the matching FixedMathSharp-Unity and
SwiftCollections-Unity packages on first editor load. If resolution was
interrupted, choose **Tools → GridForge → Repair Dependencies**.

Manual dependency URLs, when needed:

```text
https://github.com/mrdav30/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp#v7.0.0
https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections#v7.0.0
https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.fixedmathsharp#v7.0.0
```
