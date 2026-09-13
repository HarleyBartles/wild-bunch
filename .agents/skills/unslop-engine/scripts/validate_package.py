#!/usr/bin/env python3
"""Validate the unslop-engine source skill files."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path


REQUIRED = [
    "SKILL.md",
    "agents/openai.yaml",
    "scripts/unslop.py",
    "scripts/validate_unslop_output.py",
    "scripts/validate_package.py",
    "references/upstream-provenance.md",
    "assets/authority/authority.yaml",
    "assets/authority/CITATIONS.md",
    "assets/authority/source-map.yaml",
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

    checked_files = [
        file
        for file in skill_root.rglob("*")
        if file.is_file() and file.suffix.lower() in {".md", ".json", ".yaml", ".py", ".txt"}
    ]
    for file in checked_files:
        if file.name == "upstream-provenance.md":
            continue
        content = file.read_text(encoding="utf-8", errors="ignore").lower()
        for forbidden in forbidden_fragments():
            if forbidden.lower() in content:
                issues.append(f"forbidden runtime instruction in {file.relative_to(skill_root)}")

    return issues


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=f"{__doc__} (read-only)")
    parser.add_argument("--check", action="store_true", help="Validate the source package (read-only).")
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
