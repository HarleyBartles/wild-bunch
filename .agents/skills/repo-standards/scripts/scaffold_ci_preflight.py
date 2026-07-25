#!/usr/bin/env python3
"""Scaffold repo-resident CI preflight scripts from the skill template."""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path


def _stripped_env() -> dict[str, str]:
    env = os.environ.copy()
    env.pop("GIT_DIR", None)
    env.pop("GIT_WORK_TREE", None)
    env.pop("GIT_INDEX_FILE", None)
    return env


def _repo_root() -> Path:
    result = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True,
        text=True,
        check=True,
        env=_stripped_env(),
    )
    return Path(result.stdout.strip())


def _skill_root() -> Path:
    return Path(__file__).resolve().parent.parent


def _copy_template(name: str, target: Path, force: bool, check: bool) -> bool:
    template = _skill_root() / "templates" / name
    if not template.is_file():
        print(f"ERROR: template not found: {template}", file=sys.stderr)
        return False
    if target.is_file():
        if check:
            print(f"OK {target.name}: present")
            return True
        if not force:
            print(f"{target.name} already exists; use --force to overwrite")
            return True
    if check:
        print(f"DRIFT: {target.name} missing")
        return False
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(template, target)
    print(f"wrote {target.relative_to(_repo_root()).as_posix()}")
    return True


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check               verify that scripts/ci-preflight.sh and .ps1 exist
  %(prog)s                       create the two preflight scripts if they are missing
  %(prog)s --force               replace the preflight scripts with the template copies

These scripts are copied from the repo-standards skill template. They are
intended as a starting point and may be customized after creation, so --check
only verifies presence unless --force is used.

exit codes:
  0  preflight scripts are present or were written
  1  drift detected or a template could not be copied"""
    parser = argparse.ArgumentParser(
        description="Scaffold the repo's CI preflight scripts.",
        epilog=epilog,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Report drift without writing",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Overwrite existing preflight scripts",
    )
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    scripts_dir = repo_root / "scripts"

    ok = True
    ok &= _copy_template("ci-preflight.sh", scripts_dir / "ci-preflight.sh", args.force, args.check)
    ok &= _copy_template("ci-preflight.ps1", scripts_dir / "ci-preflight.ps1", args.force, args.check)

    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
