#!/usr/bin/env python3
"""Validate that installed marketplace plugins match their canonical configuration."""

from __future__ import annotations

import json
import hashlib
import subprocess
from collections.abc import Mapping
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[1]
MARKETPLACE_PATH = REPO_ROOT / ".agents" / "plugins" / "marketplace.json"
PROVENANCE_PATH = REPO_ROOT / ".agents" / "skills" / ".provenance.json"
SKILLS_ROOT = PROVENANCE_PATH.parent
SUBMODULE_PATH = ".agents/plugins/marketplace-source"
REPO_LOCAL_SKILL_PREFIX = "wild-bunch-"


def _is_ignored_artifact(path: Path) -> bool:
    """Return True for runtime artifacts that must never affect sync or hashes."""
    if "__pycache__" in path.parts:
        return True
    if path.suffix == ".pyc":
        return True
    return False


def _load_json(path: Path) -> dict[str, Any]:
    """Load a JSON object from disk."""
    with path.open(encoding="utf-8") as source:
        value = json.load(source)

    if not isinstance(value, dict):
        raise ValueError(f"{path} must contain a JSON object")

    return value


def _default_plugin_names(marketplace: Mapping[str, object]) -> list[str]:
    """Return the ordered default-installed plugin names from configuration."""
    plugins = marketplace.get("plugins")
    if not isinstance(plugins, list):
        raise ValueError("marketplace.json plugins must be a list")

    names: list[str] = []
    for plugin in plugins:
        if not isinstance(plugin, Mapping):
            raise ValueError("marketplace.json plugins must be objects")

        policy = plugin.get("policy")
        if not isinstance(policy, Mapping):
            raise ValueError("marketplace.json plugin policy must be an object")

        if policy.get("installation") != "INSTALLED_BY_DEFAULT":
            continue

        name = plugin.get("name")
        if not isinstance(name, str) or not name:
            raise ValueError("default-installed plugins must have a non-empty name")
        names.append(name)

    if not names:
        raise ValueError("marketplace.json has no default-installed plugins")
    if len(names) != len(set(names)):
        raise ValueError("default-installed plugin names must be unique")

    return names


def validate_marketplace_plugin_sync(
    marketplace: Mapping[str, object],
    provenance: Mapping[str, object],
    committed_submodule_sha: str,
    installed_skill_hashes: Mapping[str, str],
) -> list[str]:
    """Validate the generated installed-plugin projection against configuration."""
    if marketplace.get("name") != "wild-bunch":
        raise ValueError("marketplace.json must identify the wild-bunch repository")

    configured_plugins = _default_plugin_names(marketplace)
    synced_plugins = provenance.get("syncedPlugins")
    if synced_plugins != configured_plugins:
        raise ValueError(
            "installed-skill provenance syncedPlugins does not match the ordered "
            "default-installed plugins in marketplace.json"
        )

    if provenance.get("sha") != committed_submodule_sha:
        raise ValueError(
            "installed-skill provenance SHA does not match the committed marketplace "
            "submodule gitlink"
        )

    synced_skill_names = provenance.get("syncedSkillNames")
    if (
        not isinstance(synced_skill_names, list)
        or any(
            not isinstance(skill_name, str) or not skill_name
            for skill_name in synced_skill_names
        )
        or len(synced_skill_names) != len(set(synced_skill_names))
    ):
        raise ValueError(
            "installed-skill provenance syncedSkillNames must be a unique list of "
            "non-empty skill names"
        )

    if provenance.get("syncedSkills") != len(synced_skill_names):
        raise ValueError(
            "installed-skill provenance syncedSkills does not match syncedSkillNames"
        )

    repo_local_skill_names = sorted(
        skill_name
        for skill_name in synced_skill_names
        if skill_name.startswith(REPO_LOCAL_SKILL_PREFIX)
    )
    if repo_local_skill_names:
        raise ValueError(
            "installed-skill provenance must not record repo-local skill names: "
            f"{', '.join(repo_local_skill_names)}"
        )

    missing_skill_names = sorted(set(synced_skill_names) - set(installed_skill_hashes))
    if missing_skill_names:
        raise ValueError(
            "installed-skill projection is missing recorded skill director"
            f"ies: {', '.join(missing_skill_names)}"
        )

    synced_skill_hashes = provenance.get("syncedSkillHashes")
    if (
        not isinstance(synced_skill_hashes, Mapping)
        or set(synced_skill_hashes) != set(synced_skill_names)
        or any(
            not isinstance(skill_hash, str) or not skill_hash
            for skill_hash in synced_skill_hashes.values()
        )
    ):
        raise ValueError(
            "installed-skill provenance syncedSkillHashes must map every recorded "
            "skill name to a non-empty hash"
        )

    changed_skill_names = sorted(
        skill_name
        for skill_name in synced_skill_names
        if synced_skill_hashes[skill_name] != installed_skill_hashes[skill_name]
    )
    if changed_skill_names:
        raise ValueError(
            "installed-skill projection content does not match recorded hashes: "
            f"{', '.join(changed_skill_names)}"
        )

    return configured_plugins


def _skill_directory_hash(skill_dir: Path) -> str:
    """Return a deterministic content hash for one generated skill directory."""
    digest = hashlib.sha256()
    for file_path in sorted(
        (path for path in skill_dir.rglob("*") if path.is_file() and not _is_ignored_artifact(path)),
        key=lambda path: path.relative_to(skill_dir).as_posix(),
    ):
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


def _installed_skill_hashes() -> dict[str, str]:
    """Return hashes for current invocable generated and custody skill directories."""
    return {
        path.name: _skill_directory_hash(path)
        for path in SKILLS_ROOT.iterdir()
        if path.is_dir() and (path / "SKILL.md").is_file()
    }


def _committed_submodule_sha() -> str:
    """Read the marketplace submodule gitlink from the checked-out commit tree."""
    result = subprocess.run(
        ["git", "rev-parse", f"HEAD:{SUBMODULE_PATH}"],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=True,
    )
    return result.stdout.strip()


def main() -> int:
    """Run the repository-level marketplace sync validation."""
    try:
        configured_plugins = validate_marketplace_plugin_sync(
            _load_json(MARKETPLACE_PATH),
            _load_json(PROVENANCE_PATH),
            _committed_submodule_sha(),
            _installed_skill_hashes(),
        )
    except (FileNotFoundError, json.JSONDecodeError, subprocess.CalledProcessError, ValueError) as error:
        print(f"ERROR: marketplace plugin sync validation failed: {error}")
        return 1

    print(
        "OK: installed-skill provenance matches "
        f"{len(configured_plugins)} default-installed plugin(s) in marketplace.json"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
