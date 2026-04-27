#!/usr/bin/env python3
from __future__ import annotations

import argparse
import contextlib
import importlib.util
import os
import shutil
import subprocess
import sys
from pathlib import Path

TOOL_ROOT = Path(__file__).resolve().parent
REPO_ROOT = Path(__file__).resolve().parents[2]
PROJECT_ROOT = REPO_ROOT.parent.parent
ASSETS_ROOT = PROJECT_ROOT / "Assets"
STAGING_ROOT = ASSETS_ROOT / "__GridForgePackageBuild"
OUTPUT_ROOT = REPO_ROOT / "UpmPackages~"
LEGACY_BACKUP_ROOT = REPO_ROOT / "LegacyVisibleBackup~"
LOG_ROOT = PROJECT_ROOT / "Temp" / "GridForgePackageBuildLogs"
UNITY_EXECUTE_METHOD = "GridForge.Build.Editor.GridForgePackageBuild.ImportAndPrepare"
UNITY_VERSION = "6000.3.9f1"


def load_sync_module():
    sync_path = TOOL_ROOT / "sync_variants.py"
    spec = importlib.util.spec_from_file_location("gridforge_sync_variants", sync_path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load {sync_path}")

    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Stage and build GridForge Unity package variants through Unity batchmode.")
    parser.add_argument(
        "--unity-path",
        default=str(default_unity_path()),
        help="Path to Unity.exe.",
    )
    parser.add_argument(
        "--variant",
        action="append",
        choices=("standard", "lean"),
        help="Only build the selected variant. Repeat to build multiple variants.",
    )
    parser.add_argument(
        "--keep-staging",
        action="store_true",
        help="Keep the temporary imported staging folder after the build completes.",
    )
    parser.add_argument(
        "--skip-visible-sync",
        action="store_true",
        help="Do not mirror the generated outputs back into the visible package folders.",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    sync_module = load_sync_module()

    all_variants = list(sync_module.VARIANTS.values())
    selected_names = args.variant or list(sync_module.VARIANTS.keys())
    variants = [sync_module.VARIANTS[name] for name in selected_names]

    unity_path = resolve_host_path(args.unity_path)
    if not unity_path.exists():
        raise SystemExit(f"Unity executable not found: {unity_path}")

    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)

    with temporarily_hide_legacy_visible_packages(all_variants):
        try:
            for variant in variants:
                stage_root = STAGING_ROOT / variant.package_dir
                output_root = OUTPUT_ROOT / variant.package_dir
                stage_variant(sync_module, variant, stage_root)
                run_unity_import(unity_path, stage_root, variant.name)
                copy_stage_to_output(stage_root, output_root)
        finally:
            if not args.keep_staging:
                shutil.rmtree(STAGING_ROOT, ignore_errors=True)

    if not args.skip_visible_sync:
        for variant in variants:
            sync_visible_package(OUTPUT_ROOT / variant.package_dir, REPO_ROOT / variant.package_dir)


def stage_variant(sync_module, variant, stage_root: Path) -> None:
    shutil.rmtree(STAGING_ROOT, ignore_errors=True)
    STAGING_ROOT.mkdir(parents=True, exist_ok=True)
    sync_module.compose_variant(variant, stage_root)


def run_unity_import(unity_path: Path, stage_root: Path, variant_name: str) -> None:
    LOG_ROOT.mkdir(parents=True, exist_ok=True)
    log_path = LOG_ROOT / f"{variant_name}.log"

    stage_asset_path = to_unity_asset_path(stage_root)
    args = [
        str(unity_path),
        "-batchmode",
        "-projectPath",
        to_windows_path(PROJECT_ROOT),
        "-executeMethod",
        UNITY_EXECUTE_METHOD,
        "-gridforgeStagePackagePath",
        stage_asset_path,
        "-logFile",
        to_windows_path(log_path),
    ]

    result = subprocess.run(args, check=False)
    if result.returncode != 0:
        fallback_log = default_editor_log_path()
        log_hint = log_path if log_path.exists() else fallback_log
        raise SystemExit(
            f"Unity batch build failed for {variant_name}. Check {log_hint}."
        )


def copy_stage_to_output(stage_root: Path, output_root: Path) -> None:
    if output_root.exists():
        shutil.rmtree(output_root)

    output_root.parent.mkdir(parents=True, exist_ok=True)
    shutil.copytree(stage_root, output_root)


def sync_visible_package(source_root: Path, destination_root: Path) -> None:
    if destination_root.exists():
        shutil.rmtree(destination_root)

    destination_root.parent.mkdir(parents=True, exist_ok=True)
    shutil.copytree(source_root, destination_root)


@contextlib.contextmanager
def temporarily_hide_legacy_visible_packages(variants):
    LEGACY_BACKUP_ROOT.mkdir(parents=True, exist_ok=True)
    moved_entries: list[tuple[Path, Path]] = []

    try:
        for variant in variants:
            package_root = REPO_ROOT / variant.package_dir
            package_root_meta = REPO_ROOT / f"{variant.package_dir}.meta"
            backup_root = LEGACY_BACKUP_ROOT / variant.package_dir
            backup_meta = LEGACY_BACKUP_ROOT / f"{variant.package_dir}.meta"

            if package_root.exists():
                if backup_root.exists():
                    shutil.rmtree(backup_root)
                shutil.move(str(package_root), str(backup_root))
                moved_entries.append((backup_root, package_root))

            if package_root_meta.exists():
                if backup_meta.exists():
                    backup_meta.unlink()
                shutil.move(str(package_root_meta), str(backup_meta))
                moved_entries.append((backup_meta, package_root_meta))

        yield
    finally:
        for source, destination in reversed(moved_entries):
            destination.parent.mkdir(parents=True, exist_ok=True)
            if destination.exists():
                if destination.is_dir():
                    shutil.rmtree(destination)
                else:
                    destination.unlink()
            shutil.move(str(source), str(destination))

        if LEGACY_BACKUP_ROOT.exists() and not any(LEGACY_BACKUP_ROOT.iterdir()):
            LEGACY_BACKUP_ROOT.rmdir()


def to_unity_asset_path(path: Path) -> str:
    relative_path = path.relative_to(PROJECT_ROOT).as_posix()
    if not relative_path.startswith("Assets/"):
        raise ValueError(f"Expected an Assets-relative path, got {path}")

    return relative_path


def default_unity_path() -> Path:
    if os.name == "nt":
        return Path(fr"C:\Program Files\Unity\Hub\Editor\{UNITY_VERSION}\Editor\Unity.exe")

    return Path(f"/mnt/c/Program Files/Unity/Hub/Editor/{UNITY_VERSION}/Editor/Unity.exe")


def default_editor_log_path() -> Path:
    if os.name == "nt":
        local_app_data = Path(os.environ.get("LOCALAPPDATA", Path.home() / "AppData" / "Local"))
        return local_app_data / "Unity" / "Editor" / "Editor.log"

    return Path("/mnt/c/Users/david/AppData/Local/Unity/Editor/Editor.log")


def resolve_host_path(path_value: str) -> Path:
    if os.name == "nt":
        if is_wsl_path(path_value):
            return Path(wsl_to_windows_path(path_value))

        return Path(path_value)

    if is_windows_path(path_value):
        return Path(windows_to_wsl_path(path_value))

    return Path(path_value)


def to_windows_path(path: Path) -> str:
    if os.name == "nt":
        return str(path.resolve())

    result = subprocess.run(
        ["wslpath", "-w", str(path)],
        check=True,
        capture_output=True,
        text=True,
    )
    return result.stdout.strip()


def is_windows_path(path_value: str) -> bool:
    return len(path_value) >= 3 and path_value[1] == ":" and path_value[2] in ("\\", "/")


def is_wsl_path(path_value: str) -> bool:
    return path_value.startswith("/mnt/") and len(path_value) >= 7 and path_value[5].isalpha() and path_value[6] == "/"


def windows_to_wsl_path(path_value: str) -> str:
    normalized = path_value.replace("\\", "/")
    drive = normalized[0].lower()
    suffix = normalized[2:]
    if not suffix.startswith("/"):
        suffix = f"/{suffix}"

    return f"/mnt/{drive}{suffix}"


def wsl_to_windows_path(path_value: str) -> str:
    normalized = path_value.replace("\\", "/")
    drive = normalized[5].upper()
    suffix = normalized[6:]
    if suffix.startswith("/"):
        suffix = suffix[1:]

    return f"{drive}:\\{suffix.replace('/', '\\')}"


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        sys.exit(130)
