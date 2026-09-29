#!/usr/bin/env python3
"""Maintain the decision status and review-date table in its README."""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


STATUS_RE = re.compile(r"^## Status\s*\n\s*(.+?)\s*$", re.MULTILINE)
HISTORY_RE = re.compile(r"^## Dated Status History\s*\n(.*?)(?=^## |\Z)", re.MULTILINE | re.DOTALL)
DATE_RE = re.compile(r"(\d{4}-\d{2}-\d{2})")
SECTION = "## Decision Status and Review Dates"


def render_table(decisions_dir: Path) -> str:
    rows: list[str] = []
    for adr in sorted(decisions_dir.glob("ADR-*.md"), key=lambda path: path.name):
        text = adr.read_text(encoding="utf-8")
        status_match = STATUS_RE.search(text)
        status = status_match.group(1).strip().strip("`") if status_match else "unknown"
        history_match = HISTORY_RE.search(text)
        dates = DATE_RE.findall(history_match.group(1)) if history_match else []
        reviewed = max(dates) if dates else "unknown"
        rows.append(f"| [{adr.name}]({adr.name}) | {status} | {reviewed} |")
    if not rows:
        return ""
    return "\n".join(
        [SECTION, "", "| Decision | Status | Last reviewed |", "| --- | --- | --- |", *rows]
    )


def _replace_section(readme: str, table: str) -> str:
    section_pattern = re.compile(re.escape(SECTION) + r"\s*\n.*?(?=\n## |\Z)", re.DOTALL)
    if section_pattern.search(readme):
        updated = section_pattern.sub(table, readme, count=1)
        return updated.rstrip() + "\n"
    if not table:
        return readme.rstrip() + "\n"
    return readme.rstrip() + "\n\n" + table + "\n"


def update_readme(readme: Path) -> None:
    readme.write_text(_replace_section(readme.read_text(encoding="utf-8"), render_table(readme.parent)), encoding="utf-8", newline="\n")


def check_readme(readme: Path) -> list[str]:
    expected = _replace_section(readme.read_text(encoding="utf-8"), render_table(readme.parent))
    current = readme.read_text(encoding="utf-8")
    return [] if expected == current else ["decision status and review-date table is stale"]


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Update or check the docs/decisions README table. (mixed)")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--apply", action="store_true", help="update the README table")
    mode.add_argument("--check", action="store_true", help="check the README table without writing")
    parser.add_argument("--repo-root", type=Path, default=Path.cwd(), help="Wild Bunch repository root")
    args = parser.parse_args(argv)
    readme = args.repo_root / "docs" / "decisions" / "README.md"
    if not readme.is_file():
        print(f"ERROR: decisions README not found: {readme}", file=sys.stderr)
        return 1
    if args.apply:
        update_readme(readme)
        return 0
    errors = check_readme(readme)
    for error in errors:
        print(f"DRIFT: {error}", file=sys.stderr)
    if not errors:
        print("OK decision status and review-date table")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
