#!/usr/bin/env python3
"""Validate the shipped unslop skill package files."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


REQUIRED = [
    "SKILL.md",
    "agents/openai.yaml",
    "scripts/unslop.py",
    "scripts/validate_unslop_output.py",
    "references/output-contract.md",
    "references/upstream-provenance.md",
    "profiles/writing.md",
    "profiles/react-design.md",
]


def forbidden_fragments() -> list[str]:
    return [
        "git " + "clone https://github.com/mshumer/unslop",
        "claude" + " -p",
        "requires " + "Claude Code",
    ]


def validate(skill_root: Path) -> list[str]:
    issues: list[str] = []
    for rel in REQUIRED:
        if not (skill_root / rel).exists():
            issues.append(f"missing {rel}")

    package_root = skill_root.parents[1]
    plugin_manifest = package_root / ".codex-plugin" / "plugin.json"
    if not plugin_manifest.exists():
        issues.append("missing plugin manifest")
    else:
        data = json.loads(plugin_manifest.read_text(encoding="utf-8"))
        if data.get("name") != "unslop":
            issues.append("plugin manifest name mismatch")
        if data.get("skills") != "./skills/":
            issues.append("plugin manifest skills path mismatch")

    checked_files = [
        file
        for file in package_root.rglob("*")
        if file.is_file()
        and "sources/third_party" not in str(file)
        and file.suffix.lower() in {".md", ".json", ".yaml", ".py", ".txt"}
    ]
    for file in checked_files:
        content = file.read_text(encoding="utf-8", errors="ignore").lower()
        for forbidden in forbidden_fragments():
            if forbidden.lower() in content:
                issues.append(f"forbidden runtime instruction in {file.relative_to(package_root)}")

    return issues


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("skill_root", type=Path, nargs="?", default=Path(__file__).resolve().parents[1])
    args = parser.parse_args(argv)
    issues = validate(args.skill_root)
    if issues:
        for issue in issues:
            print(f"ERROR: {issue}", file=sys.stderr)
        return 1
    print(f"OK: {args.skill_root}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
