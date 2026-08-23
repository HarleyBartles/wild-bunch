#!/usr/bin/env python3
"""Shared-checkout detection and human-approval helpers."""

from __future__ import annotations

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


def _is_main_worktree(repo_root: Path) -> bool:
    """Return True if repo_root is the main git worktree (not a linked worktree).

    Older Git versions do not support ``rev-parse --is-main-worktree``, so we
    compare the resolved git directory to the resolved git common directory. In
    the main worktree they are the same; in a linked worktree the git directory
    is a ``worktrees/<name>`` subdirectory of the common directory.
    """
    git_dir = subprocess.run(
        ["git", "rev-parse", "--absolute-git-dir"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        check=True,
        env=_stripped_env(),
    ).stdout.strip()
    common_dir = subprocess.run(
        ["git", "rev-parse", "--git-common-dir"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        check=True,
        env=_stripped_env(),
    ).stdout.strip()
    return (repo_root / Path(git_dir)).resolve() == (repo_root / Path(common_dir)).resolve()


def is_main_shared_checkout(repo_root: Path) -> bool:
    """Return True if repo_root is the main (shared) checkout that should be gated.

    Linked worktrees are the intended mutation surface and are not treated as
    shared for gating purposes.
    """
    return _is_main_worktree(repo_root)


def _current_branch(repo_root: Path) -> str:
    """Return the current git branch name; returns 'HEAD' when detached, or an empty string if the git command fails."""
    result = subprocess.run(
        ["git", "rev-parse", "--abbrev-ref", "HEAD"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        check=False,
        env=_stripped_env(),
    )
    return result.stdout.strip()


def prompt_for_approval(script_name: str) -> bool:
    """Prompt an interactive user for main-worktree approval on main."""
    if not sys.stdin.isatty():
        return False
    try:
        response = input(
            f"warning: this is the main shared checkout on the main branch. "
            f"Allow {script_name} to apply changes? (y/N) "
        )
    except (EOFError, KeyboardInterrupt):
        return False
    return response.strip().lower() == "y"


def approve_mutation(repo_root: Path, script_name: str, flag_approved: bool) -> bool:
    """Return True if mutation is approved.

    - Linked worktree: always approved.
    - Main shared checkout on the main branch: requires explicit approval;
      --allow-shared-checkout prints a warning and approves.
    - Main shared checkout on any other branch: always approved.
    - Main shared checkout on main with interactive terminal: prompt the user.
    - Otherwise: print an actionable error and return False.
    """
    if not is_main_shared_checkout(repo_root):
        return True
    branch = _current_branch(repo_root)
    if branch != "main":
        return True
    if flag_approved:
        print(
            f"warning: --allow-shared-checkout supplied; {script_name} "
            f"will apply changes in the main shared checkout on the main branch",
            file=sys.stderr,
        )
        return True
    if prompt_for_approval(script_name):
        return True
    print(
        f"error: refusing to apply {script_name} in the main shared checkout "
        f"on the main branch. "
        f"Pass --allow-shared-checkout if this is intentional, or run interactively to confirm.",
        file=sys.stderr,
    )
    return False
