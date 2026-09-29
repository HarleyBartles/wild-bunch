#!/usr/bin/env python3
"""Run the pinned Marketplace skill refresher with its vendor deployer available."""

from __future__ import annotations

import argparse
import filecmp
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Callable


ROOT = Path(__file__).resolve().parent.parent
SOURCE_ROOT = ROOT / ".agents" / "plugins" / "marketplace-source"


def run_refresh(
    root: Path,
    source_root: Path,
    arguments: list[str],
    *,
    runner: Callable[..., subprocess.CompletedProcess] = subprocess.run,
) -> int:
    """Invoke the pinned refresher and provision its missing legacy lookup briefly."""
    refresher = (
        source_root
        / "dist"
        / "plugins"
        / "repo-worker-pack"
        / "skills"
        / "refreshing-installed-skills"
        / "scripts"
        / "refresh_installed_skills.py"
    )
    deployer = source_root / "skills" / "repo-shape" / "scripts" / "deploy_vendor_profiles.py"
    compatibility_deployer = root / "skills" / "repo-shape" / "scripts" / "deploy_vendor_profiles.py"
    for required in (refresher, deployer):
        if not required.is_file():
            raise RuntimeError(f"pinned Marketplace refresh resource is missing: {required}")

    created_directories: list[Path] = []
    created_deployer = False
    try:
        if compatibility_deployer.exists():
            if not filecmp.cmp(deployer, compatibility_deployer, shallow=False):
                raise RuntimeError(
                    "Marketplace compatibility deployer already exists with different content: "
                    f"{compatibility_deployer}"
                )
        else:
            parent = compatibility_deployer.parent
            missing: list[Path] = []
            cursor = parent
            while not cursor.exists():
                missing.append(cursor)
                cursor = cursor.parent
            parent.mkdir(parents=True, exist_ok=True)
            created_directories.extend(reversed(missing))
            shutil.copyfile(deployer, compatibility_deployer)
            created_deployer = True

        result = runner(
            [sys.executable, str(refresher), *arguments],
            cwd=root,
            check=False,
        )
        return result.returncode
    finally:
        if created_deployer:
            compatibility_deployer.unlink(missing_ok=True)
        for directory in reversed(created_directories):
            try:
                directory.rmdir()
            except OSError:
                break


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Refresh skills from the pinned Marketplace source (mutating)."
    )
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--check", action="store_true", help="preview skill projection drift")
    mode.add_argument("--apply", action="store_true", help="apply the skill projection")
    parser.add_argument("--no-roll-marketplace-source", action="store_true")
    parser.add_argument("--allow-shared-checkout", action="store_true")
    args = parser.parse_args(argv)

    upstream_arguments = ["--apply" if args.apply else "--check", "--no-roll-marketplace-source"]
    if args.allow_shared_checkout:
        if not args.apply:
            print("error: --allow-shared-checkout requires --apply", file=sys.stderr)
            return 1
        upstream_arguments.append("--allow-shared-checkout")
    try:
        return run_refresh(ROOT, SOURCE_ROOT, upstream_arguments)
    except (OSError, RuntimeError, subprocess.SubprocessError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
