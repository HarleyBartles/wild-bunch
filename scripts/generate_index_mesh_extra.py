#!/usr/bin/env python3
"""Repo-specific INDEX.md post-processing for the Wild Bunch ADR freshness table."""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


ADR_DIR_REL = Path("docs/adr")
ADR_STATUS_RE = re.compile(r"^## Status\s*\n\s*(.+?)\s*$", re.MULTILINE)
ADR_DATED_HISTORY_RE = re.compile(
    r"^## Dated Status History\s*\n(.*?)(?=^## |\Z)",
    re.MULTILINE | re.DOTALL,
)
ADR_DATE_RE = re.compile(r"(\d{4}-\d{2}-\d{2})")
FRESHNESS_HEADER = "## ADR Freshness Table"


def _adr_files(adr_dir: Path) -> list[Path]:
    return sorted(
        (
            f
            for f in adr_dir.iterdir()
            if f.is_file() and f.name.startswith("ADR-") and f.suffix == ".md"
        ),
        key=lambda f: f.name,
    )


def _render_freshness_table(adr_dir: Path) -> list[str]:
    adr_files = _adr_files(adr_dir)
    if not adr_files:
        return []

    lines: list[str] = [FRESHNESS_HEADER, ""]
    lines.append("| ADR | Status | Last checked |")
    lines.append("| --- | --- | --- |")
    for adr_file in adr_files:
        text = adr_file.read_text(encoding="utf-8")
        status_match = ADR_STATUS_RE.search(text)
        status = status_match.group(1).strip() if status_match else "unknown"
        last_checked = "unknown"
        history_match = ADR_DATED_HISTORY_RE.search(text)
        if history_match:
            dates = ADR_DATE_RE.findall(history_match.group(1))
            if dates:
                last_checked = max(dates)
        lines.append(
            f"| [{adr_file.name}]({adr_file.name}) | {status} | {last_checked} |"
        )
    lines.append("")
    return lines


def _strip_existing_freshness(content: str) -> str:
    """Remove an existing ADR freshness table section, if present."""
    lines = content.splitlines()
    cutoff = None
    for i, line in enumerate(lines):
        if line.strip() == FRESHNESS_HEADER:
            cutoff = i
            break
    if cutoff is None:
        return content
    return "\n".join(lines[:cutoff]).rstrip() + "\n"


def _update_adr_index(adr_index: Path) -> None:
    table_lines = _render_freshness_table(adr_index.parent)
    if not table_lines:
        return

    original = adr_index.read_text(encoding="utf-8")
    body = _strip_existing_freshness(original)
    updated = body.rstrip() + "\n\n" + "\n".join(table_lines)
    adr_index.write_text(updated, encoding="utf-8", newline="\n")


def _check_adr_index(adr_index: Path) -> list[str]:
    errors: list[str] = []
    table_lines = _render_freshness_table(adr_index.parent)
    if not table_lines:
        if FRESHNESS_HEADER in adr_index.read_text(encoding="utf-8"):
            errors.append("expected no ADR freshness table, but one is present")
        return errors

    expected = "\n".join(table_lines).strip()
    current = adr_index.read_text(encoding="utf-8")
    if FRESHNESS_HEADER not in current:
        errors.append("ADR freshness table is missing")
        return errors

    current_section_match = re.search(
        re.escape(FRESHNESS_HEADER) + r".*?(?=\n## |\Z)",
        current,
        re.DOTALL,
    )
    if current_section_match is None:
        errors.append("could not isolate current ADR freshness table")
        return errors

    current_section = current_section_match.group(0).strip()
    if current_section != expected:
        errors.append("ADR freshness table is stale")
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Post-process docs/adr/INDEX.md with an ADR freshness table."
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Validate without writing.",
    )
    parser.add_argument(
        "repo_root",
        type=Path,
        help="Repository root path.",
    )
    args = parser.parse_args(argv)

    adr_index = args.repo_root / ADR_DIR_REL / "INDEX.md"
    if not adr_index.is_file():
        print(f"ERROR: ADR index not found: {adr_index}", file=sys.stderr)
        return 1

    if args.check:
        errors = _check_adr_index(adr_index)
        if errors:
            for error in errors:
                print(f"ERROR: {error}", file=sys.stderr)
            return 1
        return 0

    _update_adr_index(adr_index)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
