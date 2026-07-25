#!/usr/bin/env python3
"""Find and run the repo's generate_index_mesh.py command."""

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


def _reject_submodule() -> None:
    result = subprocess.run(
        ["git", "rev-parse", "--show-superproject-working-tree"],
        capture_output=True,
        text=True,
        env=_stripped_env(),
    )
    if result.returncode == 0 and result.stdout.strip():
        raise RuntimeError("This script must not run inside a git submodule")


def _find_override(repo_root: Path) -> list[str] | None:
    for rel in [
        "scripts/generate-index-mesh.py",
        "scripts/generate-index-mesh.ps1",
        "scripts/generate-index-mesh.sh",
        "tools/generate-index-mesh.py",
        "tools/generate-index-mesh.ps1",
        "tools/generate-index-mesh.sh",
    ]:
        candidate = repo_root / rel
        if candidate.is_file():
            if rel.endswith(".py"):
                return [sys.executable, str(candidate)]
            if sys.platform == "win32" and rel.endswith(".ps1"):
                return ["pwsh", "-File", str(candidate)]
            elif rel.endswith(".sh") and shutil.which("bash"):
                return ["bash", str(candidate)]
            raise RuntimeError(f"Found {candidate} but no interpreter available")
    return None


def _find_command(repo_root: Path) -> list[str] | None:
    for rel in ["tools/generate_index_mesh.py", "scripts/generate_index_mesh.py"]:
        candidate = repo_root / rel
        if candidate.is_file():
            return [sys.executable, str(candidate)]
    return None


def find_mesh_command(repo_root: Path) -> list[str] | None:
    """Return the command list for the repo's index mesh generator, if any."""
    return _find_override(repo_root) or _find_command(repo_root)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Run the repo's index mesh generator")
    parser.add_argument("--check", action="store_true", help="validate without writing")
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    _reject_submodule()

    command = find_mesh_command(repo_root)
    if command is None:
        print("error: no generate_index_mesh command found", file=sys.stderr)
        return 1

    if args.check:
        command = [*command, "--check"]

    result = subprocess.run(command, cwd=repo_root, env=_stripped_env())
    return result.returncode


if __name__ == "__main__":
    raise SystemExit(main())
