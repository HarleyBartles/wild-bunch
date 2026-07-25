#!/usr/bin/env python3
"""Create a git worktree at the canonical sibling location and refresh skills."""

from __future__ import annotations

import argparse
import json
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


def _reject_submodule() -> None:
    result = subprocess.run(
        ["git", "rev-parse", "--show-superproject-working-tree"],
        capture_output=True,
        text=True,
        env=_stripped_env(),
    )
    if result.returncode == 0 and result.stdout.strip():
        raise RuntimeError("This script must not run inside a git submodule")


def _main_repo_root() -> Path:
    """Return the main repository worktree root.

    The first worktree reported by `git worktree list` is the main worktree.
    Using this instead of `--git-common-dir` avoids misplacing worktrees when
    the repository uses `--separate-git-dir` or other non-standard git-dir
    layouts.
    """
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


def _canonical_worktree_root(main_repo_root: Path, branch: str) -> Path:
    repo_name = main_repo_root.name
    return main_repo_root.parent / "_agent-worktrees" / repo_name / branch


def _normalize_branch_name(branch: str) -> str:
    """Strip a leading refs/heads/ prefix so full refs can be used as branch names."""
    prefix = "refs/heads/"
    if branch.startswith(prefix):
        branch = branch[len(prefix):]
    return branch


def _validate_branch_name(branch: str) -> None:
    """Raise ValueError if branch is not a valid git branch name."""
    result = subprocess.run(
        ["git", "check-ref-format", "--branch", branch],
        capture_output=True,
        text=True,
        env=_stripped_env(),
    )
    if result.returncode != 0:
        raise ValueError(f"invalid branch name: {branch!r}")


def _validate_worktree_root(main_repo_root: Path, branch: str) -> Path:
    """Return the resolved worktree path, refusing paths that escape the canonical root."""
    _validate_branch_name(branch)
    canonical_root = _canonical_worktree_root(main_repo_root, "placeholder").parent
    worktree_root = _canonical_worktree_root(main_repo_root, branch).resolve()
    try:
        worktree_root.relative_to(canonical_root.resolve())
    except ValueError as exc:
        raise ValueError(
            f"branch {branch!r} would place worktree outside the canonical root {canonical_root}"
        ) from exc
    if worktree_root == canonical_root.resolve():
        raise ValueError(f"branch {branch!r} resolves to the canonical worktree root")
    return worktree_root


def _is_under_repo(repo_root: Path, candidate: Path) -> bool:
    """Return True if candidate resolves to a path inside repo_root."""
    try:
        candidate.resolve().relative_to(repo_root.resolve())
    except ValueError:
        return False
    return True


def _find_skill_core(repo_root: Path, skill_name: str, core_name: str) -> Path | None:
    """Return the path to a skill's core script, searching installed plugins first.

    This helper is intentionally self-contained in each skill script so the
    skills remain independent; do not share a module between installed skills.
    """
    fast_path = repo_root / ".agents" / "skills" / skill_name / "scripts" / core_name
    if fast_path.is_file() and _is_under_repo(repo_root, fast_path):
        return fast_path

    marketplace = repo_root / ".agents" / "plugins" / "marketplace.json"
    if marketplace.is_file():
        try:
            data = json.loads(marketplace.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            data = {}
        for plugin in data.get("plugins", []):
            if plugin.get("policy", {}).get("installation") != "INSTALLED_BY_DEFAULT":
                continue
            source_path = plugin.get("source", {}).get("path")
            if not source_path:
                continue
            plugin_path = Path(source_path)
            if not plugin_path.is_absolute():
                plugin_path = (repo_root / plugin_path).resolve()
            candidate = plugin_path / "skills" / skill_name / "scripts" / core_name
            if candidate.is_file() and _is_under_repo(repo_root, candidate):
                return candidate

    for pattern in [
        f"codex-marketplace/plugins/*/skills/{skill_name}/scripts/{core_name}",
        f".agents/plugins/marketplace-source/codex-marketplace/plugins/*/skills/{skill_name}/scripts/{core_name}",
    ]:
        for candidate in sorted(repo_root.glob(pattern)):
            if candidate.is_file() and _is_under_repo(repo_root, candidate):
                return candidate
    return None


def _find_refresh_script(worktree_root: Path) -> Path | None:
    """Return the path to the new worktree's refreshing-installed-skills script."""
    return _find_skill_core(worktree_root, "refreshing-installed-skills", "refresh_installed_skills.py")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Create a git worktree at the canonical sibling location")
    parser.add_argument("branch", help="branch name to create")
    parser.add_argument("--base-ref", default=None, help="base ref for the new branch (default: HEAD)")
    parser.add_argument("--no-skill-refresh", action="store_true", help="skip refreshing installed skills in the new worktree")
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    _reject_submodule()
    main_repo_root = _main_repo_root()
    branch = _normalize_branch_name(args.branch)

    try:
        worktree_root = _validate_worktree_root(main_repo_root, branch)
    except ValueError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1
    if worktree_root.is_file():
        print(f"error: worktree path is an existing file: {worktree_root}", file=sys.stderr)
        return 1
    if worktree_root.is_dir():
        print(f"error: worktree directory already exists: {worktree_root}", file=sys.stderr)
        return 1

    worktree_root.parent.mkdir(parents=True, exist_ok=True)

    cmd = ["git", "worktree", "add", "-b", branch, str(worktree_root)]
    if args.base_ref:
        cmd.append(args.base_ref)

    # Run from the main worktree so that the default base is main's HEAD, not the
    # HEAD of any linked worktree the user may be invoking this script from.
    result = subprocess.run(cmd, cwd=main_repo_root, env=_stripped_env())
    if result.returncode != 0:
        return result.returncode

    if not args.no_skill_refresh:
        refresh_script = _find_refresh_script(worktree_root)
        if refresh_script:
            result = subprocess.run(
                [sys.executable, str(refresh_script), "--allow-shared-checkout"],
                cwd=worktree_root,
                env=_stripped_env(),
            )
            if result.returncode != 0:
                print(f"error: refreshing installed skills failed in {worktree_root}", file=sys.stderr)
                return result.returncode
        else:
            print(
                "warning: refreshing-installed-skills not found; worktree created but skills were not refreshed",
                file=sys.stderr,
            )

    print(f"Worktree ready at {worktree_root}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
