#!/usr/bin/env python3
"""Install/refresh skills in .agents/skills from the agent-asset-marketplace submodule.

This script provides a Python implementation with content comparison to reduce
file churn and supports --check mode for CI validation. It replaces the previous
sync-skills.ps1 PowerShell-only implementation.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import subprocess
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

# Paths relative to repo root
REPO_ROOT = Path(__file__).resolve().parents[1]
MARKETPLACE_JSON_PATH = REPO_ROOT / ".agents" / "plugins" / "marketplace.json"
SUBMODULE_ROOT = REPO_ROOT / ".agents" / "plugins" / "marketplace-source"
PLUGINS_ROOT = SUBMODULE_ROOT / "codex-marketplace" / "plugins"
SKILLS_ROOT = REPO_ROOT / ".agents" / "skills"
PROVENANCE_PATH = SKILLS_ROOT / ".provenance.json"


def _load_json(path: Path) -> dict[str, Any]:
    """Load and parse a JSON file."""
    if not path.exists():
        raise FileNotFoundError(f"{path} not found")
    
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def _get_submodule_sha() -> str:
    """Get the current HEAD SHA of the marketplace submodule."""
    result = subprocess.run(
        ["git", "-C", str(SUBMODULE_ROOT), "rev-parse", "HEAD"],
        capture_output=True,
        text=True,
        check=True
    )
    return result.stdout.strip()


def _load_provenance() -> dict[str, Any] | None:
    """Load existing provenance if it exists."""
    if not PROVENANCE_PATH.exists():
        return None
    
    try:
        return _load_json(PROVENANCE_PATH)
    except (json.JSONDecodeError, FileNotFoundError):
        return None


def _has_skill_dirs(expected_skill_names: set[str] | None = None) -> bool:
    """Check whether any or all expected skill directories exist."""
    if not SKILLS_ROOT.exists():
        return False

    if expected_skill_names is not None:
        return all(
            (SKILLS_ROOT / skill_name / "SKILL.md").is_file()
            for skill_name in expected_skill_names
        )

    return any(path.is_dir() for path in SKILLS_ROOT.iterdir())


def _expected_skill_names(default_plugins: list[dict[str, Any]]) -> set[str]:
    """Return the skill directories the configured marketplace plugins provide."""
    skill_names: set[str] = set()
    for plugin in default_plugins:
        plugin_name = plugin.get("name", "unknown")
        plugin_dir = PLUGINS_ROOT / plugin_name
        if not plugin_dir.is_dir():
            raise ValueError(
                f"Configured default plugin '{plugin_name}' is missing from {PLUGINS_ROOT}"
            )

        plugin_skills_dir = plugin_dir / "skills"
        if plugin_skills_dir.is_dir():
            for skill_dir in plugin_skills_dir.iterdir():
                if not skill_dir.is_dir():
                    continue
                if not (skill_dir / "SKILL.md").is_file():
                    raise ValueError(
                        f"Source skill '{skill_dir.name}' from plugin '{plugin_name}' "
                        "is missing SKILL.md"
                    )
                skill_names.add(skill_dir.name)

    return skill_names


def _expected_skill_hashes(default_plugins: list[dict[str, Any]]) -> dict[str, str]:
    """Return source hashes for generated skills, preserving collision precedence."""
    skill_hashes: dict[str, str] = {}
    for plugin in default_plugins:
        plugin_name = plugin.get("name", "unknown")
        plugin_skills_dir = PLUGINS_ROOT / plugin_name / "skills"
        if not plugin_skills_dir.is_dir():
            continue
        for skill_dir in plugin_skills_dir.iterdir():
            if skill_dir.is_dir() and skill_dir.name not in skill_hashes:
                skill_hashes[skill_dir.name] = _skill_directory_hash(skill_dir)
    return skill_hashes


def _can_skip_sync(
    provenance: dict[str, Any],
    submodule_sha: str,
    default_plugin_names: list[str],
    expected_skill_names: set[str],
    expected_skill_hashes: dict[str, str],
) -> bool:
    """Return whether provenance already matches the source and configuration."""
    synced_skill_names = provenance.get("syncedSkillNames")
    synced_skill_hashes = provenance.get("syncedSkillHashes")
    return (
        provenance.get("sha") == submodule_sha
        and provenance.get("syncedPlugins") == default_plugin_names
        and isinstance(synced_skill_names, list)
        and synced_skill_names == sorted(expected_skill_names)
        and provenance.get("syncedSkills") == len(expected_skill_names)
        and synced_skill_hashes == expected_skill_hashes
    )


def _projection_matches_source(default_plugins: list[dict[str, Any]]) -> bool:
    """Return whether each generated skill is byte-for-byte current from source."""
    seen_skill_names: set[str] = set()
    for plugin in default_plugins:
        plugin_name = plugin.get("name", "unknown")
        plugin_skills_dir = PLUGINS_ROOT / plugin_name / "skills"
        if not plugin_skills_dir.is_dir():
            continue

        for source_skill_dir in plugin_skills_dir.iterdir():
            if not source_skill_dir.is_dir() or source_skill_dir.name in seen_skill_names:
                continue

            seen_skill_names.add(source_skill_dir.name)
            if not _files_identical(
                source_skill_dir,
                SKILLS_ROOT / source_skill_dir.name,
            ):
                return False

    return True


def _files_identical(dir1: Path, dir2: Path) -> bool:
    """Compare two directories byte-by-byte for content equality.
    
    Uses byte-by-byte comparison rather than hash comparison because:
    - Deterministic: no hash collision risk (however unlikely)
    - Fast for small files: skill files are typically small text/markdown
    - Simple: no need to manage hash caching or invalidation
    - Optimized: file size check avoids reading contents when sizes differ
    
    This is appropriate for skill files which are small, numerous, and
    change infrequently. For very large files, hash comparison would be
    more efficient, but that's not the expected use case here.
    
    Optimizes by comparing file sizes before reading full contents.
    """
    if not dir1.exists() or not dir2.exists():
        return False
    
    # Get all files in both directories
    files1 = {f.relative_to(dir1) for f in dir1.rglob("*") if f.is_file()}
    files2 = {f.relative_to(dir2) for f in dir2.rglob("*") if f.is_file()}
    
    if files1 != files2:
        return False
    
    # Compare file contents byte-by-byte, with size check optimization
    for rel_path in files1:
        file1 = dir1 / rel_path
        file2 = dir2 / rel_path
        
        # Quick size check before reading contents
        if file1.stat().st_size != file2.stat().st_size:
            return False
        
        # Only read contents if sizes match
        if file1.read_bytes() != file2.read_bytes():
            return False
    
    return True


def _skill_directory_hash(skill_dir: Path) -> str:
    """Return a deterministic content hash for one generated skill directory."""
    digest = hashlib.sha256()
    for file_path in sorted(path for path in skill_dir.rglob("*") if path.is_file()):
        digest.update(file_path.relative_to(skill_dir).as_posix().encode("utf-8"))
        digest.update(b"\0")
        file_bytes = file_path.read_bytes()
        try:
            file_bytes = file_bytes.decode("utf-8").replace("\r\n", "\n").encode("utf-8")
        except UnicodeDecodeError:
            pass
        digest.update(file_bytes)
        digest.update(b"\0")
    return digest.hexdigest()


def _copy_skill_directory(source: Path, dest: Path) -> None:
    """Copy a skill directory from source to destination."""
    if dest.exists():
        shutil.rmtree(dest)
    
    shutil.copytree(source, dest)


def _sync_skills(
    force: bool = False,
    check_mode: bool = False
) -> tuple[int, int, bool]:
    """Sync skills from marketplace submodule.
    
    Returns:
        Tuple of (skills_synced, plugins_synced, changes_made)
    """
    # Validate environment
    if not MARKETPLACE_JSON_PATH.exists():
        raise FileNotFoundError(f"marketplace.json not found at {MARKETPLACE_JSON_PATH}")
    
    if not SUBMODULE_ROOT.exists():
        raise FileNotFoundError(
            f"Submodule not initialized at {SUBMODULE_ROOT}. "
            "Run: git submodule update --init .agents/plugins/marketplace-source"
        )
    
    if not PLUGINS_ROOT.exists():
        raise FileNotFoundError(
            f"Plugin source root not found at {PLUGINS_ROOT}. "
            "Submodule may be on an unexpected branch."
        )
    
    # Get submodule SHA
    submodule_sha = _get_submodule_sha()

    # Load the canonical plugin configuration before deciding whether sync can skip.
    marketplace = _load_json(MARKETPLACE_JSON_PATH)
    default_plugins = [
        p for p in marketplace.get("plugins", [])
        if p.get("policy", {}).get("installation") == "INSTALLED_BY_DEFAULT"
    ]

    if not default_plugins:
        raise ValueError("No INSTALLED_BY_DEFAULT plugins found in marketplace.json")

    default_plugin_names = [p.get("name", "unknown") for p in default_plugins]
    expected_skill_names = _expected_skill_names(default_plugins)
    expected_skill_hashes = _expected_skill_hashes(default_plugins)
    
    # Check provenance for skip
    existing_provenance = _load_provenance()
    has_skill_dirs = _has_skill_dirs(expected_skill_names)
    provenance_needs_refresh = (
        existing_provenance is None
        or not _can_skip_sync(
            existing_provenance,
            submodule_sha,
            default_plugin_names,
            expected_skill_names,
            expected_skill_hashes,
        )
    )
    
    if not force and existing_provenance:
        if (
            not provenance_needs_refresh
            and has_skill_dirs
            and _projection_matches_source(default_plugins)
        ):
            synced_plugins = existing_provenance.get("syncedPlugins", [])
            synced_skills = existing_provenance.get("syncedSkills", 0)
            print(f"Skills already synced at submodule SHA {submodule_sha}. Use --force to re-copy.")
            print(f"Synced skills: {synced_skills} from {len(synced_plugins)} plugins.")
            return synced_skills, len(synced_plugins), False
    
    print(f"Syncing skills from {len(default_plugins)} default-installed plugins (submodule SHA {submodule_sha})...")
    
    # Ensure skills root exists
    if not check_mode:
        SKILLS_ROOT.mkdir(parents=True, exist_ok=True)
    
    # Track synced skills
    synced_skill_names = set()
    synced_plugin_names = default_plugin_names.copy()
    changes_made = provenance_needs_refresh
    total_skills = 0
    skills_processed = 0

    if check_mode and provenance_needs_refresh:
        print("  CHECK: Would update marketplace skill provenance")
    
    # Count total skills for progress reporting
    for plugin in default_plugins:
        plugin_name = plugin.get("name", "unknown")
        plugin_skills_dir = PLUGINS_ROOT / plugin_name / "skills"
        if plugin_skills_dir.exists():
            total_skills += len([d for d in plugin_skills_dir.iterdir() if d.is_dir()])
    
    # Copy each plugin's skill directories
    for plugin in default_plugins:
        plugin_name = plugin.get("name", "unknown")
        plugin_skills_dir = PLUGINS_ROOT / plugin_name / "skills"
        
        if not plugin_skills_dir.exists():
            print(f"Plugin '{plugin_name}' has no skills/ directory; skipping.")
            continue
        
        skill_dirs = [d for d in plugin_skills_dir.iterdir() if d.is_dir()]
        if not skill_dirs:
            print(f"Plugin '{plugin_name}' skills/ is empty; skipping.")
            continue
        
        plugin_skill_count = 0
        
        for skill_dir in skill_dirs:
            skill_name = skill_dir.name
            dest_skill_dir = SKILLS_ROOT / skill_name
            
            # Collision handling
            if skill_name in synced_skill_names:
                print(f"Warning: Skill '{skill_name}' (from plugin '{plugin_name}') collides with already-synced skill; keeping first copy.")
                continue
            
            # Check if copy is needed
            needs_copy = force
            if not needs_copy and dest_skill_dir.exists():
                if not _files_identical(skill_dir, dest_skill_dir):
                    needs_copy = True
            
            # Always track the skill as synced (prevents pruning unchanged skills)
            synced_skill_names.add(skill_name)
            
            if check_mode:
                if needs_copy or not dest_skill_dir.exists():
                    print(f"  CHECK: Would copy skill: {skill_name}")
                    changes_made = True
                    plugin_skill_count += 1
            else:
                if needs_copy or not dest_skill_dir.exists():
                    _copy_skill_directory(skill_dir, dest_skill_dir)
                    plugin_skill_count += 1
                    changes_made = True
            
            # Progress indicator
            skills_processed += 1
            if total_skills > 0 and (skills_processed % 10 == 0 or skills_processed == total_skills):
                print(f"  Progress: {skills_processed}/{total_skills} skills processed")
        
        if plugin_skill_count > 0 or check_mode:
            print(f"  {plugin_name} : {plugin_skill_count} skill(s)")
    
    # Prune stale skill directories
    previous_synced_skill_names: set[str] = set()
    if existing_provenance:
        recorded_skill_names = existing_provenance.get("syncedSkillNames", [])
        if isinstance(recorded_skill_names, list):
            previous_synced_skill_names = {
                skill_name
                for skill_name in recorded_skill_names
                if isinstance(skill_name, str)
            }

    stale_dirs = [
        d for d in SKILLS_ROOT.iterdir()
        if (
            d.is_dir()
            and d.name in previous_synced_skill_names
            and d.name not in synced_skill_names
        )
    ]
    
    for stale in stale_dirs:
        if check_mode:
            print(f"  CHECK: Would remove stale skill: {stale.name}")
            changes_made = True
        else:
            print(f"  Removing stale skill: {stale.name}")
            shutil.rmtree(stale)
            changes_made = True
    
    # Write provenance
    if not check_mode:
        provenance = {
            "sha": submodule_sha,
            "syncedAt": datetime.now(timezone.utc).isoformat(),
            "syncedPlugins": synced_plugin_names,
            "syncedSkills": len(synced_skill_names),
            "syncedSkillNames": sorted(synced_skill_names),
            "syncedSkillHashes": {
                skill_name: _skill_directory_hash(SKILLS_ROOT / skill_name)
                for skill_name in sorted(synced_skill_names)
            },
            "source": "HarleyBartles/agent-asset-marketplace",
            "sourcePath": ".agents/plugins/marketplace-source/codex-marketplace/plugins",
            "marketplaceFile": ".agents/plugins/marketplace.json"
        }
        
        with open(PROVENANCE_PATH, "w", encoding="utf-8", newline="\n") as f:
            json.dump(provenance, f, indent=2)
    
    print(f"\nSynced {len(synced_skill_names)} skill(s) from {len(synced_plugin_names)} plugin(s) into .agents/skills/.")
    print(f"Provenance: {submodule_sha} -> {PROVENANCE_PATH}")
    print("Next: regenerate the index mesh with 'python scripts/generate_index_mesh.py'.")
    
    return len(synced_skill_names), len(synced_plugin_names), changes_made


def _parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Install/refresh skills in .agents/skills from the agent-asset-marketplace submodule"
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Check mode: report what would change without making changes"
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Force re-copy all skill directories even when provenance matches"
    )
    return parser.parse_args()


def main() -> int:
    args = _parse_args()
    
    try:
        skills_synced, plugins_synced, changes_made = _sync_skills(
            force=args.force,
            check_mode=args.check
        )
        
        if args.check:
            if changes_made:
                print("\nCHECK: Changes would be made")
                return 1
            else:
                print("\nCHECK: No changes needed")
                return 0
        else:
            return 0
            
    except Exception as e:
        print(f"Error: {e}")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
