# GridForge Variant Workflow

This folder is the authored source of truth for the Unity package variants.
Unity ignores dot-prefixed folders, so the files here stay out of the Asset
Database while still living inside the repo.

## Layout

- `base/`
  Shared package content for all GridForge variants.
- `standard/`
  Standard-only overrides such as package metadata, asmdefs, dependency
  installer, and plugin payload.
- `lean/`
  Lean-only overrides for the no-`MemoryPack` package.
- `sync_variants.py`
  Bootstraps the hidden source and composes a merged variant tree.
- `build-packages.py`
  Stages a variant into Unity, runs batchmode import/reserialization, and emits
  final package folders under `UpmPackages~/`, then mirrors them back into the
  visible `com.mrdav30.gridforge*` package folders by default.

## Commands

- Bootstrap the hidden source from the current package folders:

```bash
python3 .variants/gridforge/sync_variants.py bootstrap --force
```

- Compose a merged variant tree to an arbitrary folder:

```bash
python3 .variants/gridforge/sync_variants.py compose \
  --variant standard \
  --destination /tmp/gridforge-standard
```

- Build both final package outputs through Unity:

```bash
python3 .variants/gridforge/build-packages.py
```

  The script supports both PowerShell and WSL hosts. It will normalize the
  Unity executable, project path, and log path for the current environment.

- Build only the lean package:

```bash
python3 .variants/gridforge/build-packages.py --variant lean
```

- Build without updating the visible package folders:

```bash
python3 .variants/gridforge/build-packages.py --skip-visible-sync
```

## Editing Rules

- Edit shared runtime, editor, sample, and common docs in `base/`.
- Edit package identity, dependency installer, asmdefs, and plugin payload in
  the matching variant folder.
- Treat `base/`, `standard/`, and `lean/` as the authored source of truth.
- Treat `com.mrdav30.gridforge/` and `com.mrdav30.gridforge.lean/` as generated
  package mirrors refreshed by `python3 .variants/gridforge/build-packages.py`.
- Treat `UpmPackages~/com.mrdav30.gridforge/` and
  `UpmPackages~/com.mrdav30.gridforge.lean/` as optional build artifacts for
  packaging or release automation.
- Unity is part of the packaging workflow. Shared asset `*.meta` files remain in
  the hidden source tree so scenes and prefabs keep valid internal references
  when each variant is imported into Unity one at a time.
