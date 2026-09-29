#!/usr/bin/env python3
"""Shared-checkout detection and explicit mutation-intent guard."""

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


def approve_mutation(repo_root: Path, script_name: str, flag_approved: bool) -> bool:
    """Return True if mutation is approved.

    - Linked worktree: always approved.
    - Main shared checkout on any branch: requires --allow-shared-checkout.
    - The flag prints a warning and records explicit intent to write there.
    """
    if not is_main_shared_checkout(repo_root):
        return True
    if flag_approved:
        print(
            f"warning: --allow-shared-checkout supplied; {script_name} will apply changes in the main shared checkout",
            file=sys.stderr,
        )
        return True
    print(
        f"error: refusing to apply {script_name} in the main shared checkout "
        "without --allow-shared-checkout. Pass --allow-shared-checkout only if writing to "
        "the shared checkout is intentional.",
        file=sys.stderr,
    )
    return False
