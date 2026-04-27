#!/usr/bin/env python3
from __future__ import annotations

import argparse
import shutil
from dataclasses import dataclass
from pathlib import Path
from typing import Dict

REPO_ROOT = Path(__file__).resolve().parents[2]
WORK_ROOT = Path(__file__).resolve().parent
BASE_ROOT = WORK_ROOT / "base"
LEGACY_GUID_MAP_ROOT = WORK_ROOT / ".guidmaps"


@dataclass(frozen=True)
class Variant:
    name: str
    package_dir: str
    overlay_dir: str
    overlay_exact_paths: tuple[str, ...]
    overlay_prefixes: tuple[str, ...] = ()

    @property
    def output_dir(self) -> Path:
        return REPO_ROOT / self.package_dir

    @property
    def overlay_root(self) -> Path:
        return WORK_ROOT / self.overlay_dir

    def matches_overlay_path(self, relative_path: str) -> bool:
        if relative_path in self.overlay_exact_paths:
            return True

        return any(
            relative_path == prefix or relative_path.startswith(f"{prefix}/")
            for prefix in self.overlay_prefixes
        )


STANDARD_VARIANT = Variant(
    name="standard",
    package_dir="com.mrdav30.gridforge",
    overlay_dir="standard",
    overlay_exact_paths=(
        "package.json",
        "package.json.meta",
        "README.md",
        "README.md.meta",
        "Plugins.meta",
        "Editor/GridForge.Editor.asmdef",
        "Editor/GridForge.Editor.asmdef.meta",
        "Editor/Utility/GitDependencyInstaller.cs",
        "Editor/Utility/GitDependencyInstaller.cs.meta",
        "Runtime/GridForge.Runtime.asmdef",
        "Runtime/GridForge.Runtime.asmdef.meta",
        "Samples/GridforgeDemo/GridForge.Samples.asmdef",
        "Samples/GridforgeDemo/GridForge.Samples.asmdef.meta",
    ),
    overlay_prefixes=("Plugins",),
)

LEAN_VARIANT = Variant(
    name="lean",
    package_dir="com.mrdav30.gridforge.lean",
    overlay_dir="lean",
    overlay_exact_paths=(
        "package.json",
        "package.json.meta",
        "README.md",
        "README.md.meta",
        "Plugins.meta",
        "Editor/GridForge.Lean.Editor.asmdef",
        "Editor/GridForge.Lean.Editor.asmdef.meta",
        "Editor/Utility/GitDependencyInstaller.cs",
        "Editor/Utility/GitDependencyInstaller.cs.meta",
        "Runtime/GridForge.Lean.Runtime.asmdef",
        "Runtime/GridForge.Lean.Runtime.asmdef.meta",
        "Samples/GridforgeDemo/GridForge.Lean.Samples.asmdef",
        "Samples/GridforgeDemo/GridForge.Lean.Samples.asmdef.meta",
    ),
    overlay_prefixes=("Plugins",),
)

VARIANTS = {
    STANDARD_VARIANT.name: STANDARD_VARIANT,
    LEAN_VARIANT.name: LEAN_VARIANT,
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Manage the hidden GridForge variant source tree.")
    subparsers = parser.add_subparsers(dest="command", required=True)

    bootstrap_parser = subparsers.add_parser(
        "bootstrap",
        help="Create the hidden source tree from the current visible package folders.",
    )
    bootstrap_parser.add_argument(
        "--force",
        action="store_true",
        help="Replace any existing hidden source tree.",
    )

    compose_parser = subparsers.add_parser(
        "compose",
        help="Compose a variant from base plus overrides into a destination folder.",
    )
    compose_parser.add_argument(
        "--variant",
        required=True,
        choices=tuple(VARIANTS.keys()),
        help="The variant to compose.",
    )
    compose_parser.add_argument(
        "--destination",
        required=True,
        help="Destination directory for the composed package tree.",
    )

    return parser.parse_args()


def main() -> None:
    args = parse_args()

    if args.command == "bootstrap":
        bootstrap_hidden_source(force=args.force)
        return

    variant = VARIANTS[args.variant]
    destination = Path(args.destination).resolve()
    compose_variant(variant, destination)


def bootstrap_hidden_source(force: bool) -> None:
    targets = (
        BASE_ROOT,
        WORK_ROOT / STANDARD_VARIANT.overlay_dir,
        WORK_ROOT / LEAN_VARIANT.overlay_dir,
    )

    if force:
        for path in targets:
            if path.exists():
                shutil.rmtree(path)

        if LEGACY_GUID_MAP_ROOT.exists():
            shutil.rmtree(LEGACY_GUID_MAP_ROOT)

    for path in targets:
        ensure_clean_bootstrap_target(path, force)

    copy_variant_tree_to_source(STANDARD_VARIANT, BASE_ROOT, include_overlay=False)
    copy_variant_tree_to_source(STANDARD_VARIANT, STANDARD_VARIANT.overlay_root, include_overlay=True)
    copy_variant_tree_to_source(LEAN_VARIANT, LEAN_VARIANT.overlay_root, include_overlay=True)


def ensure_clean_bootstrap_target(path: Path, force: bool) -> None:
    if path.exists() and any(path.iterdir()) and not force:
        raise SystemExit(
            f"{path} already exists. Re-run bootstrap with --force to replace it."
        )

    path.mkdir(parents=True, exist_ok=True)


def copy_variant_tree_to_source(variant: Variant, destination_root: Path, include_overlay: bool) -> None:
    source_root = variant.output_dir
    for source_path in iter_output_files(source_root):
        relative_path = source_path.relative_to(source_root).as_posix()
        is_overlay_path = variant.matches_overlay_path(relative_path)

        if include_overlay != is_overlay_path:
            continue

        destination_path = destination_root / relative_path
        destination_path.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source_path, destination_path)


def iter_output_files(output_root: Path):
    for path in sorted(output_root.rglob("*")):
        if path.is_file():
            yield path


def compose_variant(variant: Variant, destination_root: Path) -> None:
    merged_sources = collect_merged_sources(BASE_ROOT, variant.overlay_root)

    if destination_root.exists():
        shutil.rmtree(destination_root)

    destination_root.mkdir(parents=True, exist_ok=True)

    for relative_path, source_path in merged_sources.items():
        destination_path = destination_root / relative_path
        destination_path.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source_path, destination_path)


def collect_merged_sources(base_root: Path, overlay_root: Path) -> Dict[str, Path]:
    merged: Dict[str, Path] = {}
    merged.update(collect_files(base_root))
    merged.update(collect_files(overlay_root))
    return merged


def collect_files(root: Path) -> Dict[str, Path]:
    files: Dict[str, Path] = {}
    for path in sorted(root.rglob("*")):
        if path.is_file():
            files[path.relative_to(root).as_posix()] = path
    return files


if __name__ == "__main__":
    main()
