#!/usr/bin/env python3
"""Scaffold the repo's .agents/doctrine/repo-runbook-policy.md mapping file."""

from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
from pathlib import Path

import document_contracts


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


def _template_path() -> Path:
    return Path(__file__).resolve().parent.parent / "templates" / "repo-runbook-policy.md"


def _has_required_boilerplate(content: str) -> bool:
    lines = [line.strip() for line in content.splitlines()]
    return (
        "# Repository Runbook and Playbook Policy" in lines
        and "## Standard runbooks" in lines
        and "## Standard playbooks" in lines
        and "## Exceptions" in lines
    )


def _section_lines(content: str, heading: str) -> list[str]:
    lines: list[str] = []
    in_section = False
    for line in content.splitlines():
        stripped = line.strip()
        if stripped.startswith("## "):
            in_section = stripped == heading
            continue
        if in_section:
            lines.append(stripped)
    return lines


def _duplicate_playbook_classifications(content: str) -> set[str]:
    standard: set[str] = set()
    for line in _section_lines(content, "## Standard playbooks"):
        if not line.startswith("|"):
            continue
        cells = [cell.strip().strip("`") for cell in line.strip("|").split("|")]
        if cells and cells[0].endswith(".md"):
            standard.add(cells[0])

    additional: set[str] = set()
    for line in _section_lines(content, "## Additional repository-specific playbooks"):
        if not line.startswith("-") or line.startswith("- <!--"):
            continue
        match = re.search(r"`([^`]+\.md)`", line) or re.search(r"\[[^\]]+\]\(([^)]+\.md)\)", line)
        if match:
            additional.add(Path(match.group(1)).name)

    return standard & additional


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check               verify repo-runbook-policy.md exists and contains boilerplate
  %(prog)s                       write repo-runbook-policy.md if it is missing
  %(prog)s --force               legacy option; use coordinator force deployment

This file maps the cross-repo runbook standard to the repo's local paths and
records any surface exceptions under ## Exceptions. The boilerplate check
ensures the heading, standard mapping section, and exceptions section are
present.

exit codes:
  0  repo-runbook-policy.md is present/valid or was written
  1  drift detected, template missing, or write failed"""
    parser = argparse.ArgumentParser(
        description="Scaffold the repo's .agents/doctrine/repo-runbook-policy.md mapping file. (mixed)",
        epilog=epilog,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Report drift without writing",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Overwrite an existing repo-runbook-policy.md",
    )
    args = parser.parse_args(argv)
    if args.force:
        print("ERROR: direct scaffold force is disabled; use confirmed repo-standards --force <surface-id>")
        return 1

    repo_root = _repo_root()
    policy_path = repo_root / ".agents" / "doctrine" / "repo-runbook-policy.md"
    template = _template_path()
    if not template.is_file():
        print(f"ERROR: template not found: {template}", file=sys.stderr)
        return 1

    if policy_path.is_file():
        if args.check:
            content = policy_path.read_text(encoding="utf-8")
            findings = document_contracts.check_policy(policy_path, repo_root)
            if findings:
                for finding in findings:
                    print(f"DRIFT: [{finding.code}] {finding.message}")
                return 1
            duplicates = _duplicate_playbook_classifications(content)
            if duplicates:
                joined = ", ".join(sorted(duplicates))
                print(f"DRIFT: playbook classified as both standard and repository-specific: {joined}")
                return 1
            print("OK repo-runbook-policy.md: mapping file present")
            return 0
        if not args.force:
            print("Policy exists; normal apply preserves it; use confirmed coordinator force for restore")
            return 0

    if args.check:
        print("DRIFT: repo-runbook-policy.md missing")
        return 1

    policy_path.parent.mkdir(parents=True, exist_ok=True)
    with policy_path.open("w", encoding="utf-8", newline="\n") as f:
        f.write(template.read_text(encoding="utf-8"))
    print(f"wrote {policy_path.relative_to(repo_root).as_posix()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
