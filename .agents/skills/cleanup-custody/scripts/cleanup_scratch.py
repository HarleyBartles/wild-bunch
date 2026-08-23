#!/usr/bin/env python3
"""Classify and optionally clean orphan _agent-scratch directories.

This script follows the skill-bundled CLI contract:
- `--help` prints usage and classifies each flag.
- `--check` (the default) reports what the script would do and exits 0
  regardless so it can be used in a read-only preflight.
- `--apply` removes delete_now entries.
"""

import argparse
import os
import re
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


def _sanitize_branch_name(branch: str) -> str:
    """Replace filesystem/URL-unsafe characters with a dash.

    This must match the canonical set in the repo-standards scratch-workspace policy.
    """
    return re.sub(r'[:\\?*"<>|/\\\\]', "-", branch)


def _active_branches(main_repo_root: Path) -> set[str]:
    result = subprocess.run(
        ["git", "branch", "--format=%(refname:short)"],
        cwd=main_repo_root,
        capture_output=True,
        text=True,
        check=True,
        env=_stripped_env(),
    )
    return {_sanitize_branch_name(line.strip()) for line in result.stdout.splitlines() if line.strip()}


def _worktree_branches(main_repo_root: Path) -> set[str]:
    """Return the sanitized branch names that currently have a checked-out worktree."""
    result = subprocess.run(
        ["git", "worktree", "list", "--porcelain"],
        cwd=main_repo_root,
        capture_output=True,
        text=True,
        check=True,
        env=_stripped_env(),
    )
    branches: set[str] = set()
    current_branch: str | None = None
    for line in result.stdout.splitlines():
        if line.startswith("branch "):
            current_branch = line.split(" ", 1)[1]
            if current_branch.startswith("refs/heads/"):
                current_branch = current_branch[len("refs/heads/") :]
            branches.add(_sanitize_branch_name(current_branch))
            current_branch = None
    return branches


_SEP_CLASS = "[" + "".join(re.escape(c) for c in r':\?*"<>|/') + re.escape("-") + "]"


def _branch_plan_pattern(sanitized_branch: str) -> re.Pattern:
    """Build a pattern that matches the sanitized branch in plan text, including raw separators."""
    if not sanitized_branch:
        return re.compile(r"(?<!\w)(?!\w)")  # never matches
    pieces = [_SEP_CLASS if c == "-" else re.escape(c) for c in sanitized_branch]
    return re.compile(rf"(?<!\w){''.join(pieces)}(?!\w)")


def _plans_referencing(plans_root: Path, branch: str) -> bool:
    """Return True if any plan file explicitly references the given branch/scratch folder."""
    if not plans_root.exists():
        return False
    pattern = _branch_plan_pattern(branch)
    for plan in plans_root.glob("*.md"):
        try:
            if pattern.search(plan.read_text(encoding="utf-8")):
                return True
        except OSError:
            pass
    return False


def _classify(
    scratch_root: Path,
    repo_name: str,
    active_branches: set[str],
    worktree_branches: set[str],
    plans_root: Path,
) -> list[tuple[str, Path]]:
    repo_scratch = scratch_root / repo_name
    if not repo_scratch.exists():
        return []
    decisions = []
    for entry in repo_scratch.iterdir():
        if entry.is_dir() and entry.name in active_branches:
            decisions.append(("keep_live", entry))
        elif entry.is_dir() and entry.name in worktree_branches:
            decisions.append(("keep_live (worktree present)", entry))
        elif entry.is_dir() and _plans_referencing(plans_root, entry.name):
            decisions.append(("keep_live (plan references)", entry))
        elif entry.is_dir():
            decisions.append(("delete_now", entry))
        else:
            decisions.append(("delete_now", entry))
    return decisions


def _main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Classify or remove orphan _agent-scratch directories for the current repo. "
            "(mixed: supports --check and --apply)"
        ),
        epilog="--check is the default and always exits 0 (read-only preflight); --apply removes delete_now entries.",
    )
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument(
        "--check",
        action="store_true",
        default=True,
        help="report classification and exit 0 (default, read-only)",
    )
    mode.add_argument(
        "--apply",
        action="store_true",
        default=False,
        help="remove delete_now entries (mutating)",
    )
    args = parser.parse_args()

    main_repo_root = _main_repo_root()
    repo_name = main_repo_root.name
    scratch_root = main_repo_root.parent / "_agent-scratch"
    active = _active_branches(main_repo_root)
    worktrees = _worktree_branches(main_repo_root)
    plans_root = main_repo_root / ".agents" / "plans"

    decisions = _classify(scratch_root, repo_name, active, worktrees, plans_root)
    if not decisions:
        print(f"No scratch entries for {repo_name}")
        return 0

    for decision, path in decisions:
        print(f"{decision}: {path}")
        if decision == "delete_now" and args.apply:
            if path.is_dir():
                shutil.rmtree(path, ignore_errors=True)
            else:
                path.unlink(missing_ok=True)
            print(f"  removed {path}")

    return 0


if __name__ == "__main__":
    sys.exit(_main())
