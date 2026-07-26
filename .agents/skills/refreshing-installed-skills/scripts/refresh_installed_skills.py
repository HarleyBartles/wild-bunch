#!/usr/bin/env python3
"""Install/refresh skills in .agents/skills from installed marketplace plugins."""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path
from typing import Any

import yaml


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


ROOT = _repo_root()


def _marketplace_source_path(repo_root: Path) -> Path:
    """Return the path to the marketplace-source submodule root."""
    return repo_root / ".agents" / "plugins" / "marketplace-source"


def _load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


MARKETPLACE_PATH = ROOT / ".agents" / "plugins" / "marketplace.json"
AGENTS_SKILLS_PATH = ROOT / ".agents" / "skills"
PROVENANCE_PATH = AGENTS_SKILLS_PATH / ".provenance.json"


def _local_skill_prefixes(config: dict[str, Any]) -> list[str]:
    repo = config.get("repo") or {}
    prefixes = repo.get("local_skill_prefixes") or ["mark-"]
    return [str(p) for p in prefixes]


def _is_local_skill_dir(skill_dir: Path, prefixes: list[str]) -> bool:
    return skill_dir.is_dir() and any(skill_dir.name.startswith(p) for p in prefixes)


def _frontmatter_name(skill_dir: Path) -> object:
    lines = (skill_dir / "SKILL.md").read_text(encoding="utf-8").splitlines()
    if not lines or lines[0] != "---":
        raise ValueError("SKILL.md must start with a YAML frontmatter delimiter")
    end_index = None
    for index, line in enumerate(lines[1:], start=1):
        if line == "---":
            end_index = index
            break
    if end_index is None:
        raise ValueError("SKILL.md is missing a closing YAML frontmatter delimiter")
    return yaml.safe_load("\n".join(lines[1:end_index])).get("name")


def _validate_local_skill_dirs(prefixes: list[str]) -> list[Path]:
    if not AGENTS_SKILLS_PATH.is_dir():
        return []

    invalid: list[Path] = []
    for skill_dir in sorted(AGENTS_SKILLS_PATH.iterdir()):
        if not _is_local_skill_dir(skill_dir, prefixes):
            continue
        try:
            if _frontmatter_name(skill_dir) != skill_dir.name:
                raise ValueError("local skill directory name must match frontmatter name")
        except (FileNotFoundError, UnicodeDecodeError, ValueError, AttributeError, TypeError, yaml.YAMLError) as exc:
            try:
                display_path = skill_dir.relative_to(ROOT)
            except ValueError:
                display_path = skill_dir
            print(f"ERROR: local skill {display_path} is invalid: {exc}")
            invalid.append(skill_dir)
    return invalid


def _powershell_cmd() -> list[str]:
    for name in ("pwsh", "powershell"):
        if shutil.which(name):
            return [name, "-NoProfile", "-File"]
    return ["powershell", "-NoProfile", "-File"]


def _run_validate_local_skills_extra(check_mode: bool, prefixes: list[str]) -> bool:
    """Run the repo-supplied local-skill validation hook if one exists.

    The hook receives the skills root and any local skill prefixes:
        scripts/validate_local_skills_extra.sh [--check] <skills-root> <prefix> ...
    """
    hook_sh = ROOT / "scripts" / "validate_local_skills_extra.sh"
    hook_ps1 = ROOT / "scripts" / "validate_local_skills_extra.ps1"

    # Prefer .ps1 on Windows and .sh elsewhere, but allow fallback.
    if sys.platform == "win32" and hook_ps1.is_file():
        cmd = _powershell_cmd() + [str(hook_ps1)]
        if check_mode:
            cmd.append("-Check")
    elif hook_sh.is_file():
        cmd = ["bash", str(hook_sh)]
        if check_mode:
            cmd.append("--check")
    elif hook_ps1.is_file():
        cmd = _powershell_cmd() + [str(hook_ps1)]
        if check_mode:
            cmd.append("-Check")
    else:
        return True

    cmd.append(AGENTS_SKILLS_PATH.relative_to(ROOT).as_posix())
    cmd.extend(prefixes)

    result = subprocess.run(
        cmd,
        cwd=ROOT,
        capture_output=True,
        text=True,
        env=_stripped_env(),
    )
    if result.returncode != 0:
        for line in (result.stdout + result.stderr).splitlines():
            line = line.strip()
            if line:
                print(f"ERROR: local skill validation hook: {line}")
        return False
    return True


def _reserved_marketplace_skill_collisions(installed_plugins: list[dict[str, Any]], prefixes: list[str]) -> list[tuple[str, str]]:
    collisions: list[tuple[str, str]] = []
    for plugin in installed_plugins:
        skills_path = _get_plugin_skills_path(plugin)
        if skills_path is None:
            continue
        plugin_name = plugin.get("name", "unknown")
        if not isinstance(plugin_name, str):
            plugin_name = "unknown"
        for skill_dir in sorted(skills_path.iterdir()):
            if skill_dir.is_dir() and any(skill_dir.name.startswith(p) for p in prefixes):
                collisions.append((plugin_name, skill_dir.name))
    return collisions


def _expected_marketplace_skill_inventory(installed_plugins: list[dict[str, Any]], prefixes: list[str]) -> dict[str, Path]:
    expected: dict[str, Path] = {}
    for plugin in installed_plugins:
        skills_path = _get_plugin_skills_path(plugin)
        if skills_path is None:
            continue
        for skill_dir in sorted(skills_path.iterdir()):
            if skill_dir.is_dir() and not any(skill_dir.name.startswith(p) for p in prefixes):
                expected.setdefault(skill_dir.name, skill_dir)
    return expected


def _marketplace_skill_inventory_is_current(installed_plugins: list[dict[str, Any]], prefixes: list[str]) -> bool:
    expected = _expected_marketplace_skill_inventory(installed_plugins, prefixes)
    if not expected or not AGENTS_SKILLS_PATH.is_dir():
        return False
    installed_marketplace_names = {
        skill_dir.name
        for skill_dir in AGENTS_SKILLS_PATH.iterdir()
        if skill_dir.is_dir() and not any(skill_dir.name.startswith(p) for p in prefixes)
    }
    return installed_marketplace_names == set(expected) and all(
        not _skill_needs_update(source_skill, AGENTS_SKILLS_PATH / name)
        for name, source_skill in expected.items()
    )


def _load_marketplace_config() -> dict[str, Any]:
    """Load the marketplace configuration."""
    if not MARKETPLACE_PATH.is_file():
        raise FileNotFoundError(
            f"{MARKETPLACE_PATH} not found; create it with at least a 'plugins' list"
        )
    config = _load_json(MARKETPLACE_PATH)
    if not isinstance(config, dict):
        raise ValueError(f"{MARKETPLACE_PATH}: must contain a JSON object")
    return config


def _get_marketplace_manifest_sha() -> str:
    """Get the current marketplace manifest SHA for provenance tracking.

    When the marketplace-source submodule is present, track its HEAD so the
    provenance reflects the version of the marketplace that was installed.
    Otherwise fall back to the consumer repo's HEAD.
    """
    submodule = _marketplace_source_path(ROOT)
    if submodule.is_dir() and (submodule / ".git").exists():
        try:
            result = subprocess.run(
                ["git", "rev-parse", "HEAD"],
                cwd=submodule,
                capture_output=True,
                text=True,
                check=True,
                env=_stripped_env(),
            )
            return result.stdout.strip()
        except subprocess.CalledProcessError:
            pass

    try:
        result = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            cwd=ROOT,
            capture_output=True,
            text=True,
            check=True,
            env=_stripped_env(),
        )
        return result.stdout.strip()
    except subprocess.CalledProcessError:
        # Fallback: use marketplace.json modification time
        return datetime.fromtimestamp(MARKETPLACE_PATH.stat().st_mtime).isoformat()


def _load_provenance() -> dict[str, Any] | None:
    """Load existing provenance data."""
    if not PROVENANCE_PATH.exists():
        return None
    try:
        return _load_json(PROVENANCE_PATH)
    except (json.JSONDecodeError, ValueError):
        return None


def _get_installed_plugins(config: dict[str, Any]) -> list[dict[str, Any]]:
    """Get plugins that should be installed (INSTALLED_BY_DEFAULT)."""
    plugins = config.get("plugins", [])
    if not isinstance(plugins, list):
        raise ValueError(f"{MARKETPLACE_PATH}: plugins must be a list")

    installed = []
    for plugin in plugins:
        if not isinstance(plugin, dict):
            continue
        policy = plugin.get("policy", {})
        if not isinstance(policy, dict):
            continue
        installation = policy.get("installation")
        if installation == "INSTALLED_BY_DEFAULT":
            installed.append(plugin)
    return installed


def _get_plugin_skills_path(plugin: dict[str, Any]) -> Path | None:
    """Get the skills directory path for a plugin."""
    source = plugin.get("source", {})
    if not isinstance(source, dict):
        return None

    source_type = source.get("source")
    if source_type == "local":
        base = ROOT
    elif source_type == "github":
        owner = source.get("owner")
        repo_name = source.get("repo")
        if not isinstance(owner, str) or not isinstance(repo_name, str) or not owner or not repo_name:
            return None
        base = _marketplace_source_path(ROOT)
    else:
        return None

    path = source.get("path")
    if not isinstance(path, str) or not path:
        return None

    plugin_path = (base / path).resolve()
    try:
        plugin_path.relative_to(base.resolve())
    except ValueError:
        return None

    skills_path = plugin_path / "skills"
    return skills_path if skills_path.is_dir() else None


SKIP_DIR_NAMES = {"__pycache__", ".pytest_cache", ".mypy_cache", ".ruff_cache"}
SKIP_FILE_SUFFIXES = {".pyc", ".pyo", ".log"}


def _should_skip_file(path: Path) -> bool:
    """Return True for cache/build artifacts that should not be synced."""
    return (
        path.name in SKIP_DIR_NAMES
        or path.suffix.lower() in SKIP_FILE_SUFFIXES
        or any(part in SKIP_DIR_NAMES for part in path.parts)
    )


def _copy_skill_directory(source_skill: Path, dest_skill: Path) -> None:
    """Copy a skill directory from plugin to .agents/skills."""
    if dest_skill.exists():
        shutil.rmtree(dest_skill)

    shutil.copytree(
        source_skill,
        dest_skill,
        ignore=lambda src, names: [
            name
            for name in names
            if _should_skip_file(Path(src) / name)
        ],
    )
    print(f"Installed skill: {dest_skill.relative_to(ROOT)}")


def _files_are_identical(source: Path, dest: Path) -> bool:
    """Check if two files have identical content."""
    if not source.exists() or not dest.exists():
        return False
    return source.read_bytes() == dest.read_bytes()


def _skill_needs_update(source_skill: Path, dest_skill: Path) -> bool:
    """Check if a skill needs to be updated."""
    if not dest_skill.exists():
        return True

    # Check if all files exist and have identical content
    for source_file in source_skill.rglob("*"):
        if not source_file.is_file() or _should_skip_file(source_file):
            continue
        relative_path = source_file.relative_to(source_skill)
        dest_file = dest_skill / relative_path

        if not dest_file.exists():
            return True

        if not _files_are_identical(source_file, dest_file):
            return True

    # Check if there are any extra files in dest
    for dest_file in dest_skill.rglob("*"):
        if not dest_file.is_file() or _should_skip_file(dest_file):
            continue
        relative_path = dest_file.relative_to(dest_skill)
        source_file = source_skill / relative_path

        if not source_file.exists():
            return True

    return False


def _install_plugin_skills(plugin: dict[str, Any], check_mode: bool = False, synced_skill_names: set[str] | None = None, prefixes: list[str] | None = None) -> bool:
    """Install skills from a single plugin."""
    skills_path = _get_plugin_skills_path(plugin)
    if skills_path is None:
        return False

    plugin_name = plugin.get("name", "unknown")
    if not isinstance(plugin_name, str):
        return False

    if synced_skill_names is None:
        synced_skill_names = set()

    if prefixes is None:
        prefixes = ["mark-"]

    installed_any = False
    for skill_dir in sorted(skills_path.iterdir()):
        if not skill_dir.is_dir():
            continue

        dest_skill = AGENTS_SKILLS_PATH / skill_dir.name

        if any(skill_dir.name.startswith(p) for p in prefixes):
            raise ValueError(
                f"Marketplace skill '{skill_dir.name}' uses the reserved local skill prefix"
            )

        # Collision guard: if two plugins project a skill with the same name,
        # the first one wins and a warning is emitted.
        if skill_dir.name in synced_skill_names:
            print(f"WARNING: Skill '{skill_dir.name}' (from plugin '{plugin_name}') collides with an already-synced skill of the same name; keeping the first copy.")
            continue

        if check_mode:
            # In check mode, verify if skills would need installation
            if not dest_skill.exists():
                print(f"CHECK: Would install skill: {dest_skill.relative_to(ROOT)}")
                installed_any = True
            elif _skill_needs_update(skill_dir, dest_skill):
                print(f"CHECK: Skill {dest_skill.relative_to(ROOT)} would be updated")
                installed_any = True
            synced_skill_names.add(skill_dir.name)
        else:
            if _skill_needs_update(skill_dir, dest_skill):
                _copy_skill_directory(skill_dir, dest_skill)
                installed_any = True
            synced_skill_names.add(skill_dir.name)

    return installed_any


def _clean_orphan_skills(installed_plugins: list[dict[str, Any]], check_mode: bool = False, synced_skill_names: set[str] | None = None, prefixes: list[str] | None = None) -> bool:
    """Remove skills that don't belong to any installed plugin."""
    if not AGENTS_SKILLS_PATH.exists():
        return False

    if synced_skill_names is None:
        synced_skill_names = set()

    if prefixes is None:
        prefixes = ["mark-"]

    cleaned_any = False
    for skill_dir in sorted(AGENTS_SKILLS_PATH.iterdir()):
        if not skill_dir.is_dir():
            continue

        if _is_local_skill_dir(skill_dir, prefixes):
            continue

        if skill_dir.name not in synced_skill_names:
            if check_mode:
                print(f"CHECK: Would remove orphan skill: {skill_dir.relative_to(ROOT)}")
                cleaned_any = True
            else:
                shutil.rmtree(skill_dir)
                print(f"Removed orphan skill: {skill_dir.relative_to(ROOT)}")
                cleaned_any = True

    return cleaned_any


def _write_provenance(manifest_sha: str, installed_plugins: list[dict[str, Any]], synced_skill_count: int) -> None:
    """Write provenance data.

    Distinguishes marketplace-derived plugins from repo-local plugins so the
    provenance file does not falsely attribute local plugins to the marketplace.
    """
    synced_plugins = [
        plugin.get("name", "unknown") if isinstance(plugin.get("name"), str) else "unknown"
        for plugin in installed_plugins
    ]
    local_plugins: list[dict[str, Any]] = []
    for plugin in installed_plugins:
        source = plugin.get("source", {}) if isinstance(plugin.get("source"), dict) else {}
        if source.get("source") == "local":
            name = plugin.get("name", "unknown")
            if not isinstance(name, str):
                name = "unknown"
            local_plugins.append({
                "name": name,
                "path": source.get("path"),
                "source": "local",
            })

    provenance = {
        "manifestSha": manifest_sha,
        "syncedAt": datetime.now().isoformat(),
        "syncedPlugins": synced_plugins,
        "syncedSkills": synced_skill_count,
        "marketplace": {
            "source": "HarleyBartles/agent-asset-marketplace",
            "sourcePath": "codex-marketplace/plugins",
        },
        "localPlugins": local_plugins,
        "marketplaceFile": ".agents/plugins/marketplace.json"
    }
    with PROVENANCE_PATH.open("w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(provenance, indent=2) + "\n")


def _is_shared_checkout(repo_root: Path) -> bool:
    git_dir = subprocess.run(["git", "rev-parse", "--git-dir"], cwd=repo_root, capture_output=True, text=True, check=True, env=_stripped_env()).stdout.strip()
    git_common = subprocess.run(["git", "rev-parse", "--git-common-dir"], cwd=repo_root, capture_output=True, text=True, check=True, env=_stripped_env()).stdout.strip()
    # A linked worktree (shared checkout) has its git-dir under .git/worktrees/<name>
    # while the common dir is the main .git directory.
    return Path(git_dir).resolve() != Path(git_common).resolve()


def _is_submodule(repo_root: Path) -> bool:
    result = subprocess.run(["git", "rev-parse", "--show-superproject-working-tree"], cwd=repo_root, capture_output=True, text=True, env=_stripped_env())
    return result.returncode == 0 and result.stdout.strip()


def _roll_marketplace_source(repo_root: Path) -> None:
    """Roll the marketplace-source submodule to origin/main when present."""
    submodule = _marketplace_source_path(repo_root)
    if not submodule.is_dir() or not (submodule / ".git").exists():
        return
    print("Rolling marketplace-source to origin/main...")
    try:
        subprocess.run(["git", "-C", str(submodule), "fetch", "origin"], check=True, env=_stripped_env())
        subprocess.run(["git", "-C", str(submodule), "reset", "--hard", "origin/main"], check=True, env=_stripped_env())
    except subprocess.CalledProcessError as exc:
        print(
            f"ERROR: could not roll {submodule.relative_to(repo_root).as_posix()} to origin/main: {exc}",
            file=sys.stderr,
        )
        raise
    rel = submodule.relative_to(repo_root).as_posix()
    subprocess.run(["git", "add", "--", rel], cwd=repo_root, check=True, env=_stripped_env())


def _regenerate_index_mesh(repo_root: Path) -> None:
    """Regenerate the repo-wide INDEX.md mesh after skill installation."""
    candidates = [
        repo_root / ".agents" / "skills" / "generating-agent-mesh" / "scripts" / "generate_index_mesh.py",
        _marketplace_source_path(repo_root) / "codex-marketplace" / "plugins" / "repo-worker-pack" / "skills" / "generating-agent-mesh" / "scripts" / "generate_index_mesh.py",
    ]
    mesh_script = next((p for p in candidates if p.is_file()), None)
    if not mesh_script:
        print("warning: generate_index_mesh.py not found; skipping mesh regeneration", file=sys.stderr)
        return
    print("Regenerating index mesh...")
    subprocess.run([sys.executable, str(mesh_script)], cwd=repo_root, check=True, env=_stripped_env())


def _parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Install/refresh skills in .agents/skills from installed marketplace plugins"
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Check mode: report what would change without making changes"
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Force refresh even when provenance matches"
    )
    parser.add_argument(
        "--allow-shared-checkout",
        action="store_true",
        help="Allow running in the shared checkout with a warning",
    )
    parser.add_argument(
        "--roll-marketplace-source",
        action="store_true",
        help="Roll the marketplace-source submodule to origin/main before syncing",
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = _parse_args(argv)

    if _is_submodule(ROOT):
        print("error: this script must not run inside a git submodule", file=sys.stderr)
        return 1

    if not args.check and not args.allow_shared_checkout and _is_shared_checkout(ROOT):
        print("error: refusing to modify a shared checkout; use --allow-shared-checkout to override", file=sys.stderr)
        return 1

    if not args.check and args.roll_marketplace_source:
        _roll_marketplace_source(ROOT)

    config = _load_marketplace_config()
    prefixes = _local_skill_prefixes(config)

    invalid_local_skills = _validate_local_skill_dirs(prefixes)
    if invalid_local_skills:
        return 1

    if not _run_validate_local_skills_extra(check_mode=args.check, prefixes=prefixes):
        return 1

    installed_plugins = _get_installed_plugins(config)

    if not installed_plugins:
        print("No plugins with INSTALLED_BY_DEFAULT policy found")
        return 0

    collisions = _reserved_marketplace_skill_collisions(installed_plugins, prefixes)
    if collisions:
        for plugin_name, skill_name in collisions:
            print(
                f"ERROR: Marketplace plugin '{plugin_name}' exposes reserved local skill prefix "
                f"'{skill_name}'"
            )
        return 1

    # Get current provenance and manifest SHA
    existing_provenance = _load_provenance()
    current_manifest_sha = _get_marketplace_manifest_sha()

    # Check if refresh is needed based on provenance
    if not args.force and existing_provenance:
        if existing_provenance.get("manifestSha") == current_manifest_sha:
            if _marketplace_skill_inventory_is_current(installed_plugins, prefixes):
                print(f"Skills already synced at manifest SHA {current_manifest_sha}. Use --force to re-copy.")
                print(f"Synced skills: {existing_provenance.get('syncedSkills')} from {existing_provenance.get('syncedPlugins')} plugins.")
                return 0

    print(f"Found {len(installed_plugins)} installed plugin(s)")

    # Ensure .agents/skills directory exists
    if not args.check:
        AGENTS_SKILLS_PATH.mkdir(parents=True, exist_ok=True)

    # Record every configured INSTALLED_BY_DEFAULT plugin name in order,
    # regardless of whether its skills needed copying on this run.
    installed_plugin_names = [
        plugin.get("name", "unknown") if isinstance(plugin.get("name"), str) else "unknown"
        for plugin in installed_plugins
    ]

    # Install skills from each plugin
    changes_made = False
    synced_skill_names = set()

    for plugin in installed_plugins:
        plugin_name = plugin.get("name", "unknown")
        print(f"\nProcessing plugin: {plugin_name}")
        if _install_plugin_skills(plugin, check_mode=args.check, synced_skill_names=synced_skill_names, prefixes=prefixes):
            changes_made = True

    # Clean orphan skills
    print("\nChecking for orphan skills...")
    if _clean_orphan_skills(installed_plugins, check_mode=args.check, synced_skill_names=synced_skill_names, prefixes=prefixes):
        changes_made = True

    # Write provenance only when the installed skill tree changed. A forced
    # byte-identical refresh must remain a no-diff operation.
    if not args.check and changes_made:
        _write_provenance(current_manifest_sha, installed_plugins, len(synced_skill_names))
        print(f"\nProvenance: {current_manifest_sha} -> {PROVENANCE_PATH}")
        _regenerate_index_mesh(ROOT)

    if args.check:
        if changes_made:
            print("\nCHECK: Changes would be made")
            return 1
        else:
            print("\nCHECK: No changes needed")
            return 0
    else:
        if changes_made:
            print(f"\nSkills installed/refreshed successfully ({len(synced_skill_names)} skills from {len(installed_plugin_names)} plugins)")
        else:
            print("\nNo changes needed")
        return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
