#!/usr/bin/env python3
"""Remove a git worktree by branch name or path."""

from __future__ import annotations

import argparse
import os
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


def _list_worktrees(repo_root: Path) -> dict[str, str]:
    result = subprocess.run(
        ["git", "worktree", "list", "--porcelain"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        env=_stripped_env(),
        check=True,
    )
    worktrees: dict[str, str] = {}
    current_path = ""
    current_branch = ""
    for line in result.stdout.splitlines():
        if line.startswith("worktree "):
            current_path = line.split(" ", 1)[1]
            current_branch = ""
        elif line.startswith("branch "):
            current_branch = line.split(" ", 1)[1]
        elif line == "":
            if current_path and current_branch:
                worktrees[current_branch] = current_path
            current_path = ""
            current_branch = ""
    if current_path and current_branch:
        worktrees[current_branch] = current_path
    return worktrees


def _resolve_worktree(repo_root: Path, target: str) -> Path:
    worktrees = _list_worktrees(repo_root)
    registered_paths = {Path(path).resolve() for path in worktrees.values()}

    candidate = Path(target)
    if candidate.is_absolute():
        resolved = candidate.resolve()
        if resolved in registered_paths:
            return resolved
        if candidate.is_dir():
            raise RuntimeError(f"directory {target!r} is not a registered worktree of this repository")
        raise RuntimeError(f"Could not resolve worktree: {target}")

    if target in worktrees:
        return Path(worktrees[target]).resolve()

    by_name = {Path(path).resolve() for path in worktrees.values() if Path(path).resolve().name == target}
    if len(by_name) == 1:
        return by_name.pop()
    if len(by_name) > 1:
        raise RuntimeError(f"worktree name {target!r} is ambiguous: {', '.join(str(p) for p in sorted(by_name))}")

    by_leaf = {branch: path for branch, path in worktrees.items() if branch.split("/")[-1] == target}
    if len(by_leaf) == 1:
        return Path(next(iter(by_leaf.values()))).resolve()
    if len(by_leaf) > 1:
        raise RuntimeError(
            f"branch {target!r} is ambiguous; use a full ref such as {', '.join(sorted(by_leaf))}"
        )

    raise RuntimeError(f"Could not resolve worktree: {target}")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Remove a git worktree")
    parser.add_argument("target", help="branch name or absolute path of the worktree to remove")
    parser.add_argument("--force", action="store_true", help="force remove the worktree")
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    worktree = _resolve_worktree(repo_root, args.target)

    if worktree == repo_root.resolve():
        print("error: refusing to remove the main repository checkout", file=sys.stderr)
        return 1

    if args.force:
        # Deinitialize submodules only when force-removing; this mutates the
        # shared git config and can affect other worktrees, so it is gated.
        try:
            subprocess.run(
                ["git", "-C", str(worktree), "submodule", "deinit", "--all", "-f"],
                check=False,
                capture_output=True,
                env=_stripped_env(),
            )
        except (OSError, subprocess.SubprocessError) as exc:
            print(f"warning: submodule deinit failed: {exc}", file=sys.stderr)

    cmd = ["git", "worktree", "remove", str(worktree)]
    if args.force:
        cmd.append("--force")

    result = subprocess.run(cmd, cwd=repo_root, env=_stripped_env())
    if result.returncode != 0:
        return result.returncode

    print(f"Removed worktree {worktree}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
