#!/usr/bin/env python3
"""Validate that installed marketplace plugins match their canonical configuration."""

from __future__ import annotations

import json
import subprocess
from collections.abc import Mapping
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[1]
MARKETPLACE_PATH = REPO_ROOT / ".agents" / "plugins" / "marketplace.json"
PROVENANCE_PATH = REPO_ROOT / ".agents" / "skills" / ".provenance.json"
SUBMODULE_PATH = ".agents/plugins/marketplace-source"


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

    return configured_plugins


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
