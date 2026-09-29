#!/usr/bin/env python3
"""Scaffold and validate native repo-scoped plugin declarations."""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import tomllib
from pathlib import Path
from urllib.parse import urlsplit


MARKETPLACE = Path(".agents/plugins/marketplace.json")
CODEX_CONFIG = Path(".codex/config.toml")
DEVIN_CONFIG = Path(".devin/config.json")
TEMPLATE_ROOT = Path(__file__).resolve().parent.parent / "templates/repo-plugin-subscriptions"
_NAME = re.compile(r"^[a-zA-Z0-9][a-zA-Z0-9._-]*$")


def _load_json(path: Path, label: str) -> dict:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ValueError(f"{label} is not valid JSON: {exc}") from exc
    if not isinstance(value, dict):
        raise ValueError(f"{label} must be a JSON object")
    return value


def validate(repo_root: Path) -> list[str]:
    findings: list[str] = []
    marketplace_path = repo_root / MARKETPLACE
    codex_path = repo_root / CODEX_CONFIG
    devin_path = repo_root / DEVIN_CONFIG
    try:
        marketplace = _load_json(marketplace_path, MARKETPLACE.as_posix())
        plugins = marketplace.get("plugins")
        if not isinstance(plugins, list):
            raise ValueError(f"{MARKETPLACE.as_posix()}: plugins must be an array")
        marketplace_name = marketplace.get("name")
        if not isinstance(marketplace_name, str) or not _NAME.fullmatch(marketplace_name):
            raise ValueError(f"{MARKETPLACE.as_posix()}: name must be a valid marketplace identifier")
        names: set[str] = set()
        for index, plugin in enumerate(plugins):
            label = f"{MARKETPLACE.as_posix()}: plugins[{index}]"
            if not isinstance(plugin, dict):
                raise ValueError(f"{label} must be an object")
            name = plugin.get("name")
            source = plugin.get("source")
            if not isinstance(name, str) or not _NAME.fullmatch(name):
                raise ValueError(f"{label}.name must be a valid plugin identifier")
            if name in names:
                raise ValueError(f"{label}.name duplicates plugin identity {name!r}")
            names.add(name)
            if not isinstance(source, dict) or source.get("source") != "git-subdir":
                raise ValueError(f"{label}.source.source must be 'git-subdir'")
            url = source.get("url")
            if isinstance(url, str) and any(character.isspace() for character in url):
                raise ValueError(f"{label}.source.url must be a Git repository URL")
            parsed = urlsplit(url) if isinstance(url, str) else None
            scp_url = isinstance(url, str) and re.fullmatch(r"[^@/\s]+@[^:/\s]+:.+", url)
            if not scp_url and (
                not parsed
                or parsed.scheme not in {"https", "ssh", "git", "file"}
                or not parsed.path
                or (parsed.scheme != "file" and parsed.hostname is None)
                or parsed.path.lower().endswith((".zip", ".tar", ".tar.gz", ".tgz"))
            ):
                raise ValueError(f"{label}.source.url must be a Git repository URL")
            path = source.get("path")
            plugin_path = Path(path[2:]) if isinstance(path, str) else Path()
            if (
                not isinstance(path, str)
                or not path.startswith("./")
                or not plugin_path.parts
                or ".." in plugin_path.parts
            ):
                raise ValueError(f"{label}.source.path must be a plugin-relative path without '..'")
            selectors = [key for key in ("ref", "sha") if key in source]
            if len(selectors) != 1 or not isinstance(source.get(selectors[0]), str) or not source[selectors[0]].strip():
                raise ValueError(f"{label}.source must specify exactly one non-empty ref or sha")
            if selectors == ["sha"] and not re.fullmatch(r"(?:[0-9a-f]{40}|[0-9a-f]{64})", source["sha"]):
                raise ValueError(f"{label}.source.sha must be a full hexadecimal Git commit")
    except ValueError as exc:
        findings.append(str(exc))
        names = set()
        marketplace_name = ""

    if not codex_path.is_file():
        findings.append(f"missing: {CODEX_CONFIG.as_posix()}")
    else:
        try:
            codex = tomllib.loads(codex_path.read_text(encoding="utf-8"))
            activation = codex.get("plugins", {})
            if not isinstance(activation, dict):
                raise ValueError("[plugins] must be a table")
            for key in activation:
                if not isinstance(key, str) or "@" not in key:
                    raise ValueError(f"{CODEX_CONFIG.as_posix()}: invalid plugin activation key {key!r}")
                plugin_name, configured_marketplace = key.rsplit("@", 1)
                if plugin_name not in names or configured_marketplace != marketplace_name:
                    raise ValueError(
                        f"{CODEX_CONFIG.as_posix()}: activation {key!r} has no matching marketplace plugin"
                    )
                if not isinstance(activation[key], dict) or not isinstance(activation[key].get("enabled"), bool):
                    raise ValueError(f"{CODEX_CONFIG.as_posix()}: activation {key!r} must set enabled to a boolean")
        except (tomllib.TOMLDecodeError, ValueError) as exc:
            findings.append(f"{CODEX_CONFIG.as_posix()}: {exc}")

    if devin_path.is_file():
        try:
            devin = _load_json(devin_path, DEVIN_CONFIG.as_posix())
            for key in ("requiredPlugins", "optionalPlugins", "forbiddenPlugins"):
                if key in devin and not isinstance(devin[key], list):
                    raise ValueError(f"{DEVIN_CONFIG.as_posix()}: {key} must be an array")
        except ValueError as exc:
            findings.append(str(exc))
    return findings


def scaffold(repo_root: Path) -> list[str]:
    created: list[str] = []
    for relative, template_name in (
        (MARKETPLACE, "codex-marketplace.json"),
        (CODEX_CONFIG, "codex-config.toml"),
        (DEVIN_CONFIG, "devin-config.json"),
    ):
        destination = repo_root / relative
        if destination.exists():
            continue
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes((TEMPLATE_ROOT / template_name).read_bytes())
        created.append(relative.as_posix())
    return created


def _repo_root() -> Path:
    result = subprocess.run(["git", "rev-parse", "--show-toplevel"], check=True, capture_output=True, text=True)
    return Path(result.stdout.strip())


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Scaffold or check repo-scoped native plugin declarations. (mixed)")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="validate config without writing")
    mode.add_argument("--apply", action="store_true", help="create missing native config files")
    args = parser.parse_args(argv)
    try:
        root = _repo_root()
        if not args.check:
            for path in scaffold(root):
                print(f"wrote {path}")
        findings = validate(root)
    except (OSError, subprocess.CalledProcessError, ValueError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    for finding in findings:
        print(f"DRIFT: {finding}")
    if findings:
        return 1
    print("OK repo plugin subscriptions")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
