#!/usr/bin/env python3
"""Scaffold or validate .agents/plugins/marketplace.json."""

from __future__ import annotations

import argparse
import json
import os
import subprocess
from pathlib import Path


DEFAULT_PREFIXES = []
MINIMAL = {"repo": {"local_skills": DEFAULT_PREFIXES}, "plugins": []}


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


def _normalize_prefixes(value: object) -> list[str]:
    prefixes: list[str] = []
    if isinstance(value, str):
        prefixes.append(value)
    elif isinstance(value, list):
        for item in value:
            if isinstance(item, str):
                prefixes.append(item)
    elif isinstance(value, dict):
        for item in value.values():
            prefixes.extend(_normalize_prefixes(item))
    return [p for p in prefixes if p]


def _migrate(data: dict[str, object]) -> dict[str, object]:
    """Return a normalized marketplace.json dict with repo.local_skills."""
    prefixes: list[str] = []
    if "repo" in data and isinstance(data["repo"], dict):
        repo_block = dict(data["repo"])
    else:
        repo_block = {}

    if "local_skill_prefixes" in repo_block:
        prefixes.extend(_normalize_prefixes(repo_block["local_skill_prefixes"]))
        del repo_block["local_skill_prefixes"]
    if "local_skills" in repo_block:
        prefixes.extend(_normalize_prefixes(repo_block["local_skills"]))
        del repo_block["local_skills"]

    # Legacy top-level keys
    if "local_skill_prefixes" in data:
        prefixes.extend(_normalize_prefixes(data["local_skill_prefixes"]))
    if "local_skills" in data:
        prefixes.extend(_normalize_prefixes(data["local_skills"]))

    prefixes = sorted(set(prefixes))
    if not prefixes:
        prefixes = list(DEFAULT_PREFIXES)

    repo_block["local_skills"] = prefixes

    result: dict[str, object] = {}
    for key, value in data.items():
        if key in ("local_skill_prefixes", "local_skills"):
            continue
        if key == "repo":
            continue
        result[key] = value
    result["repo"] = repo_block
    return result


def _check(path: Path) -> list[str]:
    findings: list[str] = []
    if not path.is_file():
        findings.append(".agents/plugins/marketplace.json missing")
        return findings
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError) as exc:
        findings.append(f"invalid JSON in marketplace.json: {exc}")
        return findings
    if not isinstance(data, dict):
        findings.append("marketplace.json must be a JSON object")
        return findings
    normalized = _migrate(data)
    repo_block = normalized.get("repo")
    if not isinstance(repo_block, dict) or repo_block.get("local_skills") is None:
        findings.append("marketplace.json missing repo.local_skills")
    return findings


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check               validate .agents/plugins/marketplace.json
  %(prog)s                       write or migrate marketplace.json
  %(prog)s --force               rewrite marketplace.json with normalized content

The marketplace.json file is read from .agents/plugins/marketplace.json under
the repo root. Legacy top-level or repo-level keys named local_skill_prefixes
are merged into repo.local_skills. Other blocks (plugins, interface, name, etc.)
are preserved.

exit codes:
  0  marketplace.json is valid or was written/migrated successfully
  1  drift detected or the file could not be written"""
    parser = argparse.ArgumentParser(
        description="Scaffold, migrate, or validate .agents/plugins/marketplace.json. (mixed)",
        epilog=epilog,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument("--check", action="store_true", help="Report drift without writing")
    parser.add_argument("--force", action="store_true", help="Overwrite an existing marketplace.json")
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    marketplace = repo_root / ".agents" / "plugins" / "marketplace.json"

    if args.check:
        findings = _check(marketplace)
        if findings:
            for finding in findings:
                print(f"DRIFT: {finding}")
            return 1
        print("OK marketplace.json")
        return 0

    if marketplace.is_file() and not args.force:
        try:
            data = json.loads(marketplace.read_text(encoding="utf-8"))
            if not isinstance(data, dict):
                data = {}
        except (json.JSONDecodeError, OSError):
            data = {}
        normalized = _migrate(data)
        with marketplace.open("w", encoding="utf-8", newline="\n") as f:
            f.write(json.dumps(normalized, indent=2) + "\n")
        print("migrated marketplace.json")
        return 0

    marketplace.parent.mkdir(parents=True, exist_ok=True)
    with marketplace.open("w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(MINIMAL, indent=2) + "\n")
    print("wrote marketplace.json")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
