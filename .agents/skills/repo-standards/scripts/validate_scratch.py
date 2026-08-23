#!/usr/bin/env python3
"""Validate and optionally clean the _agent-scratch directory layout."""

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


def _main_repo_root() -> Path:
    result = subprocess.run(
        ["git", "worktree", "list", "--porcelain"],
        capture_output=True,
        text=True,
        check=True,
        env=_stripped_env(),
    )
    for line in result.stdout.splitlines():
        if line.startswith("worktree "):
            return Path(line.split(" ", 1)[1]).resolve()
    raise RuntimeError("Could not determine the main repository root")


def _scratch_root() -> Path:
    main = _main_repo_root()
    return main.parent / "_agent-scratch"


_FORBIDDEN = set(r':\?*"<>|/\\\\')


def _valid_name(name: str) -> bool:
    """Return True if name is a non-empty, path-safe token without forbidden characters."""
    return bool(name) and name not in (".", "..") and not _FORBIDDEN.intersection(name)


def _remove_path(path: Path) -> bool:
    """Remove a file or directory and return True on success."""
    try:
        if path.is_dir():
            shutil.rmtree(path)
        else:
            path.unlink()
        return True
    except OSError as exc:
        print(f"  error removing {path}: {exc}", file=sys.stderr)
        return False


def _validate(apply: bool) -> int:
    main_repo_root = _main_repo_root()
    repo_name = main_repo_root.name
    scratch_root = _scratch_root()
    if not scratch_root.exists():
        print(f"OK: {scratch_root} does not exist")
        return 0

    repo_scratch = scratch_root / repo_name
    if not repo_scratch.exists():
        print(f"OK: {repo_scratch} does not exist")
        return 0

    if repo_scratch.is_file():
        print(f"FAIL: {repo_name} is a file, expected a repo folder")
        if apply:
            if not _remove_path(repo_scratch):
                return 1
            repo_scratch.mkdir(parents=True, exist_ok=True)
            print(f"  replaced {repo_scratch} with an empty repo folder")
        return 0 if apply else 1

    issues = 0
    failures = 0
    for entry in repo_scratch.iterdir():
        if entry.is_file():
            print(f"FAIL: {repo_name} contains a file {entry.name}, expected branch/task folders")
            if apply:
                if _remove_path(entry):
                    print(f"  removed {entry}")
                else:
                    failures += 1
            issues += 1
            continue
        if not _valid_name(entry.name):
            print(f"FAIL: {repo_name}/{entry.name} is not a valid branch/task folder")
            if apply:
                if _remove_path(entry):
                    print(f"  removed {entry}")
                else:
                    failures += 1
            issues += 1

    if failures:
        print(f"FAIL: {failures} removal(s) failed")
        return 1
    if issues:
        print(f"FAIL: {issues} issue(s) found")
        return 0 if apply else 1
    print(f"OK: {repo_scratch} is clean and namespaced")
    return 0


def _cli() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Validate the _agent-scratch directory is namespaced by repo. (mixed: supports --check and --apply)"
        ),
        epilog=(
            "--check is the default and exits 1 when layout drift is found; "
            "--apply removes invalid entries in the current repo's namespace and exits 1 if any removal fails."
        ),
    )
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument(
        "--check",
        action="store_true",
        default=True,
        help="report drift and exit 1 if found (default, read-only)",
    )
    mode.add_argument(
        "--apply",
        action="store_true",
        default=False,
        help="remove invalid top-level files and non-namespaced folders (mutating)",
    )
    args = parser.parse_args()
    if args.apply:
        return _validate(apply=True)
    return _validate(apply=False)


if __name__ == "__main__":
    sys.exit(_cli())
