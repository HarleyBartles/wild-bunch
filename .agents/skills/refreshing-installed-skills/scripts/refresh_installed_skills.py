#!/usr/bin/env python3
"""Refresh installed skills from the plugin source, then regenerate the index mesh."""

from __future__ import annotations

import argparse
import json
import os
import shlex
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


def _init_marketplace_source(repo_root: Path) -> None:
    submodule = repo_root / ".agents" / "plugins" / "marketplace-source"
    if not (repo_root / ".gitmodules").is_file():
        return
    relative = submodule.relative_to(repo_root).as_posix()
    status = subprocess.run(
        ["git", "submodule", "status", relative],
        cwd=repo_root,
        capture_output=True,
        text=True,
        env=_stripped_env(),
    )
    if status.returncode != 0:
        return
    subprocess.run(
        ["git", "submodule", "update", "--init", "--recursive", relative],
        cwd=repo_root,
        env=_stripped_env(),
        check=True,
    )


def _find_override(repo_root: Path) -> list[str] | None:
    for rel in [
        "scripts/refresh-installed-skills.py",
        "scripts/refresh-installed-skills.ps1",
        "scripts/refresh-installed-skills.sh",
        "tools/refresh-installed-skills.py",
        "tools/refresh-installed-skills.ps1",
        "tools/refresh-installed-skills.sh",
    ]:
        candidate = repo_root / rel
        if candidate.is_file():
            if rel.endswith(".py"):
                return [sys.executable, str(candidate)]
            elif sys.platform == "win32" and rel.endswith(".ps1"):
                return ["pwsh", "-File", str(candidate)]
            elif rel.endswith(".sh") and shutil.which("bash"):
                return ["bash", str(candidate)]
            raise RuntimeError(f"Found {candidate} but no interpreter available")
    return None


def find_install_command(repo_root: Path) -> list[str] | None:
    """Return the command list for the repo's install_agent_skills.py, if any."""
    if (repo_root / "codex-marketplace" / "plugins").is_dir() and (repo_root / "tools" / "install_agent_skills.py").is_file():
        return [sys.executable, str(repo_root / "tools" / "install_agent_skills.py")]
    _init_marketplace_source(repo_root)
    if (repo_root / "scripts" / "install_agent_skills.py").is_file():
        return [sys.executable, str(repo_root / "scripts" / "install_agent_skills.py")]
    return None


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


def find_mesh_script(repo_root: Path) -> Path | None:
    """Return the path to the repo's generating-index-mesh script, if any."""
    return _find_skill_core(repo_root, "generating-index-mesh", "generate_index_mesh.py")


def _is_under(parent: Path, child: Path) -> bool:
    try:
        child.resolve().relative_to(parent.resolve())
    except ValueError:
        return False
    return True


def _is_refresh_path(repo_root: Path, path: Path) -> bool:
    """Return True for paths the refresh command owns (skills, index mesh, repo-index)."""
    try:
        rel = path.resolve().relative_to(repo_root.resolve())
    except ValueError:
        return False
    parts = rel.parts
    if len(parts) >= 2 and parts[0] == ".agents" and parts[1] == "skills":
        return True
    if parts and parts[0] == "repo-index":
        return True
    if rel.name == "INDEX.md":
        # INDEX.md is generated by the mesh generator, but not under third-party
        # snapshots or skill zip exports.
        if _is_under(repo_root / "sources" / "third_party", path):
            return False
        if _is_under(repo_root / "generated" / "skill-zips", path):
            return False
        return True
    return False


def _git_refresh_changes(repo_root: Path) -> list[str]:
    """Return repo-relative paths that belong to the refresh surfaces and are dirty."""
    result = subprocess.run(
        ["git", "status", "--porcelain", "--untracked-files=all"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        env=_stripped_env(),
        check=True,
    )
    changed: list[str] = []
    for line in result.stdout.splitlines():
        if not line:
            continue
        # status is two chars, then a space, then the path (quoted if needed).
        # Renamed entries include " -> " between old and new path.
        path_field = line[3:]
        tokens = shlex.split(path_field)
        if "->" in tokens:
            path_str = tokens[tokens.index("->") + 1]
        elif tokens:
            path_str = tokens[-1]
        else:
            continue
        path = repo_root / path_str
        if _is_refresh_path(repo_root, path):
            changed.append(path_str)
    return changed


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Refresh installed skills and regenerate the index mesh")
    parser.add_argument("--check", action="store_true", help="validate without writing")
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    _reject_submodule()

    override = _find_override(repo_root)
    if override:
        result = subprocess.run(
            override + (["--check"] if args.check else []),
            cwd=repo_root,
            env=_stripped_env(),
        )
        return result.returncode

    install_cmd = find_install_command(repo_root)
    if install_cmd is None:
        print("error: no install_agent_skills.py command found", file=sys.stderr)
        return 1

    install_run = install_cmd + (["--check"] if args.check else [])
    result = subprocess.run(install_run, cwd=repo_root, env=_stripped_env())
    if result.returncode != 0:
        return result.returncode

    mesh_script = find_mesh_script(repo_root)
    if mesh_script is None:
        print("error: generating-index-mesh skill not found", file=sys.stderr)
        return 1
    mesh_cmd = [sys.executable, str(mesh_script)] + (["--check"] if args.check else [])
    result = subprocess.run(mesh_cmd, cwd=repo_root, env=_stripped_env())
    if result.returncode != 0:
        return result.returncode

    if not args.check:
        refresh_paths = _git_refresh_changes(repo_root)
        if refresh_paths:
            subprocess.run(
                ["git", "add", "--", *refresh_paths],
                cwd=repo_root,
                env=_stripped_env(),
                check=True,
            )
            subprocess.run(
                [
                    "git",
                    "commit",
                    "--no-verify",
                    "-m",
                    "chore: refresh installed skills and regenerate index mesh",
                ],
                cwd=repo_root,
                env=_stripped_env(),
                check=True,
            )

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
