#!/usr/bin/env python3
"""Deploy vendor subagent profiles from installed plugin packs.

This script follows the skill-bundled CLI contract:
- `--help` prints usage and classifies each flag.
- `--check` (the default) reports what the script would do and exits 0 when
  `.agents/agents/` is already aligned with the installed plugin packs.
- `--apply` copies missing or changed profiles and removes orphan profiles.

`repo-standards` owns the one-shot deployment of `codex-marketplace/plugins/*/assets/profiles/*.md`
into `.agents/agents/`.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any


NON_PROFILE_MD_NAMES = {"INDEX.md"}


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


def _is_submodule(repo_root: Path) -> bool:
    result = subprocess.run(
        ["git", "rev-parse", "--show-superproject-working-tree"],
        capture_output=True,
        text=True,
        cwd=repo_root,
        env=_stripped_env(),
    )
    return result.returncode == 0 and bool(result.stdout.strip())


def _is_vendor_profile_file(path: Path) -> bool:
    return path.is_file() and path.suffix.lower() == ".md" and path.name not in NON_PROFILE_MD_NAMES


def _load_marketplace_plugins(marketplace_path: Path) -> list[dict[str, Any]]:
    if not marketplace_path.is_file():
        return []
    data = json.loads(marketplace_path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        return []
    plugins = data.get("plugins", [])
    if not isinstance(plugins, list):
        return []
    return [
        p for p in plugins if isinstance(p, dict) and p.get("policy", {}).get("installation") == "INSTALLED_BY_DEFAULT"
    ]


def _expected_profiles(repo_root: Path, installed_plugins: list[dict[str, Any]]) -> dict[str, Path]:
    """Return the expected profile name to source path mapping.

    The first plugin to contribute a profile name wins, matching the historical
    installer behavior.
    """
    expected: dict[str, Path] = {}
    for plugin in installed_plugins:
        source = plugin.get("source", {}) if isinstance(plugin.get("source"), dict) else {}
        source_path = source.get("path")
        if not isinstance(source_path, str) or not source_path:
            continue
        plugin_path = Path(source_path)
        if not plugin_path.is_absolute():
            plugin_path = (repo_root / plugin_path).resolve()
        if not plugin_path.is_dir():
            plugin_path = (repo_root / ".agents" / "plugins" / "marketplace-source" / source_path).resolve()
        profiles_dir = plugin_path / "assets" / "profiles"
        if not profiles_dir.is_dir():
            continue
        for child in sorted(profiles_dir.iterdir()):
            if _is_vendor_profile_file(child):
                expected.setdefault(child.name, child)
    return expected


def _installed_profile_names(agents_agents_path: Path) -> set[str]:
    if not agents_agents_path.is_dir():
        return set()
    return {child.name for child in agents_agents_path.iterdir() if _is_vendor_profile_file(child)}


def _is_git_tracked(repo_root: Path, path: Path) -> bool:
    """Return True if a file is tracked by git (a repo-local profile)."""
    try:
        rel = path.relative_to(repo_root)
    except ValueError:
        return False
    result = subprocess.run(
        ["git", "ls-files", "--error-unmatch", str(rel)],
        capture_output=True,
        text=True,
        cwd=repo_root,
        env=_stripped_env(),
    )
    return result.returncode == 0


def _deploy(
    repo_root: Path,
    installed_plugins: list[dict[str, Any]],
    apply: bool,
) -> int:
    expected = _expected_profiles(repo_root, installed_plugins)
    agents_agents_path = repo_root / ".agents" / "agents"
    existing = _installed_profile_names(agents_agents_path)

    changes = False

    for name, src in sorted(expected.items()):
        dest = agents_agents_path / name
        is_new = not dest.exists()
        needs_copy = is_new
        if not needs_copy and dest.read_text(encoding="utf-8") != src.read_text(encoding="utf-8"):
            needs_copy = True
        if needs_copy:
            if apply:
                agents_agents_path.mkdir(parents=True, exist_ok=True)
                action = "Installed" if is_new else "Updated"
                shutil.copy2(src, dest)
                print(f"{action} vendor profile: {dest.relative_to(repo_root)}")
            else:
                if is_new:
                    print(f"CHECK: Would install vendor profile: {dest.relative_to(repo_root)}")
                else:
                    print(f"CHECK: Would update vendor profile: {dest.relative_to(repo_root)}")
            changes = True

    for name in sorted(existing):
        if name in expected:
            continue
        orphan = agents_agents_path / name
        if _is_git_tracked(repo_root, orphan):
            # Repo-local override; do not treat as orphan.
            continue
        if apply:
            orphan.unlink()
            print(f"Removed orphan vendor profile: {orphan.relative_to(repo_root)}")
        else:
            print(f"CHECK: Would remove orphan vendor profile: {orphan.relative_to(repo_root)}")
        changes = True

    if apply:
        return 0
    return 1 if changes else 0


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description=(
            "Deploy vendor subagent profiles from installed plugin packs. (mixed: supports --check and --apply)"
        ),
        epilog="Default mode is --check. Use --apply to copy or remove changed or missing profiles.",
    )
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument(
        "--check",
        action="store_true",
        help="report what the script would do and exit 0 if no deployment is needed (default, read-only)",
    )
    mode.add_argument(
        "--apply",
        action="store_true",
        help="copy missing or changed and remove orphan vendor profiles (mutating)",
    )
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = _build_parser()
    args = parser.parse_args(argv)
    args.check = not args.apply

    repo_root = _repo_root()
    if _is_submodule(repo_root):
        print("error: this script must not run inside a git submodule", file=sys.stderr)
        return 1

    marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
    installed_plugins = _load_marketplace_plugins(marketplace_path)

    if not installed_plugins:
        print("No plugins with INSTALLED_BY_DEFAULT policy found")
        return 0

    return _deploy(repo_root, installed_plugins, apply=args.apply)


if __name__ == "__main__":
    raise SystemExit(main())
