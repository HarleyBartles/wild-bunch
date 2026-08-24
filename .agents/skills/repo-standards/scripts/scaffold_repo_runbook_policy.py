#!/usr/bin/env python3
"""Scaffold the repo's .agents/doctrine/repo-runbook-policy.md mapping file."""

from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path


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
    return "# Repo Runbook Policy" in lines and "## Standard-to-local mapping" in lines and "## Exceptions" in lines


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check               verify repo-runbook-policy.md exists and contains boilerplate
  %(prog)s                       write repo-runbook-policy.md if it is missing
  %(prog)s --force               overwrite repo-runbook-policy.md with the template

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

    repo_root = _repo_root()
    policy_path = repo_root / ".agents" / "doctrine" / "repo-runbook-policy.md"
    template = _template_path()
    if not template.is_file():
        print(f"ERROR: template not found: {template}", file=sys.stderr)
        return 1

    if policy_path.is_file():
        if args.check:
            content = policy_path.read_text(encoding="utf-8")
            if not _has_required_boilerplate(content):
                print("DRIFT: repo-runbook-policy.md exists but is missing required boilerplate")
                return 1
            print("OK repo-runbook-policy.md: mapping file present")
            return 0
        if not args.force:
            print("repo-runbook-policy.md already exists; use --force to overwrite")
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
