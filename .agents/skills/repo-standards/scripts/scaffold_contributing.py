#!/usr/bin/env python3
"""Scaffold the repo's root CONTRIBUTING.md entry point."""

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
    return Path(__file__).resolve().parent.parent / "templates" / "contributing-template.md"


def _has_required_boilerplate(content: str) -> bool:
    lines = content.splitlines()
    has_heading = any(line.strip() == "# Contributing" for line in lines)
    has_repo_standards = any(
        "/repo-standards" in line and line.lstrip().startswith("-") for line in lines
    )
    has_repo_worker_base = any(
        "/repo-worker-base" in line and line.lstrip().startswith("-") for line in lines
    )
    return has_heading and has_repo_standards and has_repo_worker_base


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check               verify CONTRIBUTING.md exists and contains boilerplate
  %(prog)s                       write CONTRIBUTING.md if it is missing
  %(prog)s --force               overwrite CONTRIBUTING.md with the template

The template expects the file to keep the `# Contributing` heading and the
required skill invocations (`/repo-standards`, `/repo-worker-base`). Use --force
to restore the scaffold after heavy customization.

exit codes:
  0  CONTRIBUTING.md is present/valid or was written
  1  drift detected, template missing, or write failed"""
    parser = argparse.ArgumentParser(
        description="Scaffold the repo's root CONTRIBUTING.md entry point. (mixed)",
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
        help="Overwrite an existing CONTRIBUTING.md",
    )
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    contributing_path = repo_root / "CONTRIBUTING.md"
    template = _template_path()
    if not template.is_file():
        print(f"ERROR: template not found: {template}", file=sys.stderr)
        return 1

    if contributing_path.is_file():
        if args.check:
            content = contributing_path.read_text(encoding="utf-8")
            if not _has_required_boilerplate(content):
                print("DRIFT: CONTRIBUTING.md exists but is missing required boilerplate")
                return 1
            print("OK CONTRIBUTING.md: contributor entry point present")
            return 0
        if not args.force:
            print("CONTRIBUTING.md already exists; use --force to overwrite")
            return 0

    if args.check:
        print("DRIFT: CONTRIBUTING.md missing")
        return 1

    with contributing_path.open("w", encoding="utf-8", newline="\n") as f:
        f.write(template.read_text(encoding="utf-8"))
    print(f"wrote {contributing_path.relative_to(repo_root).as_posix()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
