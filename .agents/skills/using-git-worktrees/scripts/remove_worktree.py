#!/usr/bin/env python3
"""Remove a git worktree by branch name or path.

This script follows the skill-bundled CLI contract:
- `--help` prints usage and classifies each flag.
- `--check` (the default) reports what the script would do and exits 0 when
  the target worktree does not exist, otherwise 1.
- `--apply` removes the worktree.
"""

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
        raise RuntimeError(f"branch {target!r} is ambiguous; use a full ref such as {', '.join(sorted(by_leaf))}")

    raise RuntimeError(f"Could not resolve worktree: {target}")


def _is_worktree_registered(repo_root: Path, worktree: Path) -> bool:
    """Return True if git still lists the given worktree as registered."""
    try:
        worktrees = _list_worktrees(repo_root)
    except subprocess.CalledProcessError:
        return False
    registered_paths = {Path(path).resolve() for path in worktrees.values()}
    return worktree.resolve() in registered_paths


def _check_worktree(repo_root: Path, target: str) -> tuple[int, Path, str]:
    """Return (exit_code, worktree_path, summary).

    - 0 if the worktree does not exist and no removal is needed.
    - 1 if the worktree would be removed.
    """
    try:
        worktree = _resolve_worktree(repo_root, target)
    except RuntimeError as exc:
        return 0, Path(""), f"OK no worktree to remove for {target!r}: {exc}"

    if worktree == repo_root.resolve():
        return 1, worktree, f"Would fail: refusing to remove the main repository checkout ({worktree})"

    return 1, worktree, f"Would remove worktree {worktree} (matched by {target!r})"


def _apply_remove(repo_root: Path, worktree: Path, force: bool) -> int:
    if worktree == repo_root.resolve():
        print("error: refusing to remove the main repository checkout", file=sys.stderr)
        return 1

    if force:
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
    if force:
        cmd.append("--force")

    result = subprocess.run(
        cmd,
        cwd=repo_root,
        env=_stripped_env(),
        capture_output=True,
        text=True,
    )

    if result.returncode != 0:
        # Distinguish a locked directory (git deregistered the worktree but
        # could not delete the folder) from a genuine git failure. Never fall
        # back to force-deleting the directory.
        if not _is_worktree_registered(repo_root, worktree) and worktree.exists():
            print(
                "file is locked for editing; stop. Don't continue trying to delete the locked directory.\n"
                "Report to your human partner that the on disk folder can't be deleted "
                "but the worktree is deregistered.\n"
                f"Worktree path: {worktree}",
                file=sys.stderr,
            )
            return 1

        if result.stderr:
            print(result.stderr, file=sys.stderr)
        return result.returncode

    print(f"Removed worktree {worktree}")

    return 0


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Remove a git worktree. (mixed: supports --check and --apply)",
        epilog="Default mode is --check. Use --apply to remove the worktree.",
    )
    parser.add_argument(
        "target",
        nargs="?",
        default=None,
        help="branch name or absolute path of the worktree to remove (read-only during --check)",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="force remove the worktree, including submodule deinit (mutating, used with --apply)",
    )
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument(
        "--check",
        action="store_true",
        default=True,
        help="report what the script would do and exit 0 if no removal is needed (default, read-only)",
    )
    mode.add_argument(
        "--apply",
        action="store_true",
        help="remove the worktree (mutating)",
    )
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = _build_parser()
    args = parser.parse_args(argv)

    repo_root = _repo_root()

    if args.apply:
        if args.target is None:
            print("error: target is required for --apply", file=sys.stderr)
            return 2
        try:
            worktree = _resolve_worktree(repo_root, args.target)
        except RuntimeError as exc:
            print(f"error: {exc}", file=sys.stderr)
            return 1
        return _apply_remove(repo_root, worktree, args.force)

    if args.target is None:
        print("OK pass a target to check a specific worktree")
        return 0
    exit_code, _, summary = _check_worktree(repo_root, args.target)
    print(summary)
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
