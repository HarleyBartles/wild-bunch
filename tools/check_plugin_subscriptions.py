#!/usr/bin/env python3
"""Validate Wild Bunch's repository-owned Codex Git plugin declarations.

The scope follows the optional Codex checker published with Agent Operating
Model at commit b481f98ae90aa45e5271d10fe1f7aaeb6c7047aa. It checks local
configuration only; it does not fetch, install, or certify runtime access.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import tomllib
from pathlib import Path, PurePosixPath, PureWindowsPath
from urllib.parse import urlsplit

CATALOG = Path(".agents/plugins/marketplace.json")
CONFIG = Path(".codex/config.toml")
IDENTIFIER = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]*$")
SHA = re.compile(r"^(?:[0-9a-fA-F]{40}|[0-9a-fA-F]{64})$")


def _git_url(value: object) -> bool:
    if not isinstance(value, str) or not value.strip() or any(c.isspace() for c in value):
        return False
    if re.fullmatch(r"[^@/\s]+@[^:/\s]+:.+", value):
        return True
    parsed = urlsplit(value)
    return parsed.scheme in {"https", "ssh", "git"} and bool(parsed.hostname and parsed.path)


def _contained_codex_path(value: object) -> bool:
    if not isinstance(value, str) or not value.startswith("./"):
        return False
    path = value[2:]
    posix, windows = PurePosixPath(path), PureWindowsPath(path)
    return bool(
        path
        and posix.parts
        and not posix.is_absolute()
        and not windows.is_absolute()
        and not windows.drive
        and all(part not in {".", ".."} for part in posix.parts)
        and all(part not in {".", ".."} for part in windows.parts)
    )


def _selector_valid(source: dict[str, object]) -> bool:
    present = [key for key in ("ref", "sha") if key in source]
    if len(present) != 1:
        return False
    value = source[present[0]]
    if not isinstance(value, str) or not value.strip():
        return False
    return present[0] != "sha" or bool(SHA.fullmatch(value))


def _load_catalog(path: Path) -> tuple[str | None, set[str], list[str]]:
    errors: list[str] = []
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        return None, set(), [f"{CATALOG.as_posix()}: unreadable catalog JSON: {exc}"]
    if not isinstance(data, dict):
        return None, set(), [f"{CATALOG.as_posix()}: catalog root must be an object"]
    name, plugins = data.get("name"), data.get("plugins")
    if not isinstance(name, str) or not IDENTIFIER.fullmatch(name):
        errors.append(f"{CATALOG.as_posix()}: name must be a valid identifier")
    if not isinstance(plugins, list):
        return name if isinstance(name, str) else None, set(), [*errors, "plugins must be an array"]

    names: set[str] = set()
    for index, plugin in enumerate(plugins):
        label = f"{CATALOG.as_posix()}: plugins[{index}]"
        if not isinstance(plugin, dict):
            errors.append(f"{label} must be an object")
            continue
        plugin_name, source = plugin.get("name"), plugin.get("source")
        if not isinstance(plugin_name, str) or not IDENTIFIER.fullmatch(plugin_name):
            errors.append(f"{label}.name must be a valid identifier")
        elif plugin_name in names:
            errors.append(f"{label}.name duplicates {plugin_name!r}")
        else:
            names.add(plugin_name)
        if not isinstance(source, dict):
            errors.append(f"{label}.source must be an object")
            continue
        kind = source.get("source")
        if kind not in {"url", "git-subdir"}:
            errors.append(f"{label}.source.source must be 'url' or 'git-subdir'")
        if not _git_url(source.get("url")):
            errors.append(f"{label}.source.url must be a Git URL")
        if kind == "git-subdir" and not _contained_codex_path(source.get("path")):
            errors.append(
                f"{label}.source.path must be a contained './' repository-relative plugin path"
            )
        if kind == "url" and "path" in source:
            errors.append(f"{label}.source.path is only valid with 'git-subdir'")
        if not _selector_valid(source):
            errors.append(f"{label}.source must specify exactly one valid ref or full sha")
    return name if isinstance(name, str) else None, names, errors


def check_codex(root: Path) -> list[str]:
    catalog_path, config_path = root / CATALOG, root / CONFIG
    catalog_name, plugin_names, errors = _load_catalog(catalog_path)
    try:
        config = tomllib.loads(config_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, tomllib.TOMLDecodeError) as exc:
        return [*errors, f"{CONFIG.as_posix()}: unreadable project config: {exc}"]
    marketplaces, plugins = config.get("marketplaces", {}), config.get("plugins", {})
    if not isinstance(marketplaces, dict) or not isinstance(plugins, dict):
        return [*errors, f"{CONFIG.as_posix()}: marketplaces and plugins must be TOML tables"]
    registration = marketplaces.get(catalog_name) if catalog_name else None
    if not isinstance(registration, dict) or registration.get("source_type") != "git":
        errors.append(
            "Codex marketplace registration must match the catalog name and use source_type = 'git'"
        )
    elif not _git_url(registration.get("source")):
        errors.append("Codex marketplace registration source must be a Git URL")
    elif "ref" in registration and (
        not isinstance(registration["ref"], str) or not registration["ref"].strip()
    ):
        errors.append("Codex marketplace registration ref must be a non-empty string when provided")

    for key, settings in plugins.items():
        if not isinstance(key, str) or "@" not in key:
            continue
        plugin_name, marketplace = key.rsplit("@", 1)
        if marketplace != catalog_name:
            continue
        if plugin_name not in plugin_names:
            errors.append(f"{CONFIG.as_posix()}: activation {key!r} has no matching catalog entry")
        elif not isinstance(settings, dict) or not isinstance(settings.get("enabled"), bool):
            errors.append(f"{CONFIG.as_posix()}: activation {key!r} must set enabled to a boolean")
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Check Codex plugin declarations without fetching or installing. (read-only)"
    )
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--check", action="store_true", help="check declarations without writing")
    args = parser.parse_args(argv)
    findings = check_codex(args.repo_root)
    for finding in findings:
        print(f"ERROR: {finding}", file=sys.stderr)
    if findings:
        return 1
    print(
        "OK Codex declarations; fetching, access, installation, and runtime loading "
        "are not assessed"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
