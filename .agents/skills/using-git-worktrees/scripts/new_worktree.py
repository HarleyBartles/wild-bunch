#!/usr/bin/env python3
"""Create a git worktree at the canonical sibling location and refresh skills.

This script follows the skill-bundled CLI contract:
- `--help` prints usage and classifies each flag.
- `--check` (the default) reports what the script would do and exits 0 when
  the requested worktree already exists, otherwise 1.
- `--apply` performs the creation and skill refresh.
"""

from __future__ import annotations

import argparse
import json
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


def _sanitize_branch_name(branch: str) -> str:
    """Replace filesystem/URL-unsafe characters with a dash.

    This must match the canonical set in the repo-standards scratch-workspace policy.
    """
    return re.sub(r'[:\\?*"<>|/\\\\]', "-", branch)


def _canonical_scratch_root(main_repo_root: Path, branch: str) -> Path:
    repo_name = main_repo_root.name
    return main_repo_root.parent / "_agent-scratch" / repo_name / _sanitize_branch_name(branch)


def _normalize_branch_name(branch: str) -> str:
    """Strip a leading refs/heads/ prefix so full refs can be used as branch names."""
    prefix = "refs/heads/"
    if branch.startswith(prefix):
        branch = branch[len(prefix) :]
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
        raise ValueError(f"branch {branch!r} would place worktree outside the canonical root {canonical_root}") from exc
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


def _find_mesh_script(worktree_root: Path) -> Path | None:
    """Return the path to the new worktree's generate-index-mesh script."""
    return _find_skill_core(worktree_root, "generating-agent-mesh", "generate_index_mesh.py")


def _remove_worktree(worktree_root: Path, main_repo_root: Path, branch: str) -> None:
    """Remove a newly created worktree and its branch so failed runs can be retried."""
    remove = subprocess.run(
        ["git", "worktree", "remove", "--force", str(worktree_root)],
        cwd=main_repo_root,
        env=_stripped_env(),
        capture_output=True,
    )
    if remove.returncode != 0 and worktree_root.exists():
        shutil.rmtree(worktree_root, ignore_errors=True)
    # The branch was created by `git worktree add -b` in this run; delete it so
    # the caller can retry with the same branch name.
    subprocess.run(
        ["git", "branch", "-D", branch],
        cwd=main_repo_root,
        env=_stripped_env(),
        capture_output=True,
    )


def _init_submodules(worktree_root: Path) -> int:
    """Initialize and update submodules in the new worktree.

    A linked worktree does not automatically populate submodule checkouts,
    so any skill source that lives in a submodule (e.g. ``marketplace-source``)
    would be missing when ``refreshing-installed-skills`` runs and its skills
    would be deleted as orphans. Run ``git submodule update --init --recursive``
    before the refresh to avoid that.
    """
    gitmodules = worktree_root / ".gitmodules"
    if not gitmodules.is_file():
        return 0

    result = subprocess.run(
        ["git", "submodule", "update", "--init", "--recursive"],
        cwd=worktree_root,
        env=_stripped_env(),
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        print(f"error: failed to initialize submodules in {worktree_root}: {result.stderr.strip()}", file=sys.stderr)
        return result.returncode
    return 0


def _submodule_paths(worktree_root: Path) -> list[str]:
    """Return the list of submodule paths declared in .gitmodules."""
    result = subprocess.run(
        ["git", "config", "--file", ".gitmodules", "--get-regexp", r"^submodule\..*\.path$"],
        cwd=worktree_root,
        env=_stripped_env(),
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        return []
    paths: list[str] = []
    for line in result.stdout.splitlines():
        parts = line.strip().split(maxsplit=1)
        if len(parts) == 2:
            paths.append(parts[1])
    return paths


def _roll_submodules_to_origin_main(worktree_root: Path) -> int:
    """Roll each initialized submodule to origin/main.

    ``git submodule update --init`` populates the checkout, but it does not
    advance to the latest upstream commit. Fetch origin inside each submodule
    and hard-reset to ``origin/main`` so the new worktree starts from the
    latest marketplace source before refreshing skills.
    """
    for path in _submodule_paths(worktree_root):
        submodule = worktree_root / path
        if not (submodule / ".git").exists() and not (submodule / ".git").is_file():
            # not yet initialized; skip silently
            continue

        fetch = subprocess.run(
            ["git", "-C", str(submodule), "fetch", "origin"],
            cwd=worktree_root,
            env=_stripped_env(),
            capture_output=True,
            text=True,
        )
        if fetch.returncode != 0:
            print(
                f"error: failed to fetch origin in submodule {path}: {fetch.stderr.strip()}",
                file=sys.stderr,
            )
            return fetch.returncode

        verify = subprocess.run(
            ["git", "-C", str(submodule), "rev-parse", "--verify", "origin/main"],
            cwd=worktree_root,
            env=_stripped_env(),
            capture_output=True,
            text=True,
        )
        if verify.returncode != 0:
            print(
                f"error: submodule {path} does not have origin/main; cannot roll forward",
                file=sys.stderr,
            )
            return 1

        reset = subprocess.run(
            ["git", "-C", str(submodule), "reset", "--hard", "origin/main"],
            cwd=worktree_root,
            env=_stripped_env(),
            capture_output=True,
            text=True,
        )
        if reset.returncode != 0:
            print(
                f"error: failed to reset {path} to origin/main: {reset.stderr.strip()}",
                file=sys.stderr,
            )
            return reset.returncode

        print(f"Rolled submodule {path} to origin/main")
    return 0


def _configure_worktree(
    worktree_root: Path,
    main_repo_root: Path,
    no_skill_refresh: bool,
) -> int:
    """Refresh skills and regenerate the index mesh inside the new worktree.

    Returns an exit code; the caller is responsible for removing the worktree
    when this returns non-zero.
    """
    if not no_skill_refresh:
        exit_code = _init_submodules(worktree_root)
        if exit_code != 0:
            return exit_code

        exit_code = _roll_submodules_to_origin_main(worktree_root)
        if exit_code != 0:
            return exit_code

        refresh_script = _find_refresh_script(worktree_root)
        if refresh_script:
            refresh_args = [str(refresh_script), "--apply", "--allow-shared-checkout"]
            result = subprocess.run(
                [sys.executable, *refresh_args],
                cwd=worktree_root,
                env=_stripped_env(),
            )
            if result.returncode != 0:
                print(f"error: refreshing installed skills failed in {worktree_root}", file=sys.stderr)
                return result.returncode

            mesh_script = _find_mesh_script(worktree_root)
            if mesh_script:
                mesh_args = [str(mesh_script), "--apply", "--allow-shared-checkout"]
                result = subprocess.run(
                    [sys.executable, *mesh_args],
                    cwd=worktree_root,
                    env=_stripped_env(),
                )
                if result.returncode != 0:
                    print(f"error: generating index mesh failed in {worktree_root}", file=sys.stderr)
                    return result.returncode
            else:
                print(
                    "warning: generate-index-mesh not found; worktree created but index mesh was not regenerated",
                    file=sys.stderr,
                )
        else:
            print(
                "warning: refreshing-installed-skills not found; worktree created but skills were not refreshed",
                file=sys.stderr,
            )

    print(f"Worktree ready at {worktree_root}")
    return 0


def _default_base_ref(main_repo_root: Path) -> tuple[str, bool]:
    """Return the base ref to use and whether it was resolved from origin."""
    fetch = subprocess.run(
        ["git", "fetch", "origin"],
        cwd=main_repo_root,
        env=_stripped_env(),
        capture_output=True,
    )
    if fetch.returncode == 0:
        return "origin/main", True
    return "HEAD", False


def _check_worktree(
    main_repo_root: Path,
    branch: str,
    base_ref: str | None,
) -> tuple[int, str, str]:
    """Return (exit_code, human_summary, base_ref_to_use).

    - 0 if the worktree already exists and no changes are needed.
    - 1 if the worktree would be created (or the base ref is missing).
    """
    try:
        worktree_root = _validate_worktree_root(main_repo_root, branch)
    except ValueError as exc:
        return 1, f"error: {exc}", ""

    resolved = worktree_root.resolve()
    result = subprocess.run(
        ["git", "worktree", "list", "--porcelain"],
        cwd=main_repo_root,
        env=_stripped_env(),
        capture_output=True,
        text=True,
        check=False,
    )
    for line in result.stdout.splitlines():
        if line.startswith("worktree "):
            existing = Path(line.split(" ", 1)[1]).resolve()
            if existing == resolved:
                return 0, f"OK worktree already exists at {resolved}", ""

    if resolved.is_dir() or resolved.is_file():
        return 1, f"Would fail: path already exists on disk but is not a registered worktree ({resolved})", ""

    effective_base = base_ref
    if effective_base is None:
        effective_base, _ = _default_base_ref(main_repo_root)

    if effective_base == "origin/main":
        verify = subprocess.run(
            ["git", "rev-parse", "--verify", "origin/main"],
            cwd=main_repo_root,
            env=_stripped_env(),
            capture_output=True,
            text=True,
        )
        if verify.returncode != 0:
            return 1, "Would fail: origin/main is not available (fetch from origin failed or ref is missing)", ""

    return 1, f"Would create worktree {resolved} from {effective_base} (branch {branch})", effective_base


def _apply_worktree(
    main_repo_root: Path,
    branch: str,
    base_ref: str,
    no_skill_refresh: bool,
) -> int:
    worktree_root = _validate_worktree_root(main_repo_root, branch)

    if worktree_root.is_file():
        print(f"error: worktree path is an existing file: {worktree_root}", file=sys.stderr)
        return 1
    if worktree_root.is_dir():
        print(f"error: worktree directory already exists: {worktree_root}", file=sys.stderr)
        return 1

    worktree_root.parent.mkdir(parents=True, exist_ok=True)

    cmd = ["git", "worktree", "add", "--no-track", "-b", branch, str(worktree_root), base_ref]

    # Run from the main worktree so that the default base is origin/main, not the
    # HEAD of any linked worktree the user may be invoking this script from.
    result = subprocess.run(cmd, cwd=main_repo_root, env=_stripped_env())
    if result.returncode != 0:
        return result.returncode

    try:
        exit_code = _configure_worktree(worktree_root, main_repo_root, no_skill_refresh)
    except BaseException:
        _remove_worktree(worktree_root, main_repo_root, branch)
        raise
    if exit_code != 0:
        _remove_worktree(worktree_root, main_repo_root, branch)
        return exit_code

    scratch_root = _canonical_scratch_root(main_repo_root, branch)
    scratch_root.mkdir(parents=True, exist_ok=True)
    print(f"Scratch ready at {scratch_root}")

    return 0


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Create a git worktree at the canonical sibling location. (mixed: supports --check and --apply)",
        epilog="Default mode is --check. Use --apply to create the worktree.",
    )
    parser.add_argument("branch", nargs="?", default=None, help="branch name to create (read-only during --check)")
    parser.add_argument(
        "--base-ref",
        default=None,
        help=(
            "base ref for the new branch (default: origin/main, or HEAD if origin/main is unavailable; "
            "read-only during --check)"
        ),
    )
    parser.add_argument(
        "--no-skill-refresh",
        action="store_true",
        help="skip refreshing installed skills in the new worktree (mutating, used with --apply)",
    )
    parser.add_argument(
        "--allow-shared-checkout",
        action="store_true",
        help="deprecated; no effect (kept for compatibility)",
    )

    mode = parser.add_mutually_exclusive_group()
    mode.add_argument(
        "--check",
        action="store_true",
        default=True,
        help="report what the script would do and exit 0 if no changes are needed (default, read-only)",
    )
    mode.add_argument(
        "--apply",
        action="store_true",
        help="create the worktree and refresh skills (mutating)",
    )
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = _build_parser()
    args = parser.parse_args(argv)

    _reject_submodule()
    main_repo_root = _main_repo_root()
    branch = _normalize_branch_name(args.branch)

    if args.apply:
        if args.branch is None:
            print("error: branch is required for --apply", file=sys.stderr)
            return 2
        base_ref = args.base_ref
        if base_ref is None:
            base_ref, _ = _default_base_ref(main_repo_root)
        return _apply_worktree(
            main_repo_root,
            branch,
            base_ref,
            args.no_skill_refresh,
        )

    # Default / --check mode
    if args.branch is None:
        print("OK pass a branch to check a specific worktree")
        return 0
    exit_code, summary, _ = _check_worktree(main_repo_root, branch, args.base_ref)
    print(summary)
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
