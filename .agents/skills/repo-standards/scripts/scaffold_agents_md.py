#!/usr/bin/env python3
"""Scaffold or validate the root router AGENTS.md."""

from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path

import _agents_md


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


def _skill_root() -> Path:
    return Path(__file__).resolve().parent.parent


def _template() -> str:
    template = _skill_root() / "templates" / "AGENTS.md"
    return template.read_text(encoding="utf-8")


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check               validate the current AGENTS.md router
  %(prog)s                       write AGENTS.md from the template if missing
  %(prog)s --force               overwrite AGENTS.md with the template

validation:
  The five core sections (Repository purpose, Source-of-truth split,
  Build and test commands, Routing pointers, Maintenance responsibility)
  must exist. The Routing pointers section must contain resolvable links,
  and the union of root headings and routed targets must cover the 12
  canonical topics.

exit codes:
  0  AGENTS.md is valid or was written successfully
  1  drift detected, the template is missing, or write failed"""
    parser = argparse.ArgumentParser(
        description="Scaffold or validate the root AGENTS.md router. (mixed)",
        epilog=epilog,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument("--check", action="store_true", help="Report drift without writing")
    parser.add_argument("--force", action="store_true", help="Overwrite an existing AGENTS.md")
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    agents_path = repo_root / "AGENTS.md"

    if args.check:
        findings = _agents_md.validate_agents_md(agents_path, repo_root)
        if findings:
            for finding in findings:
                print(f"DRIFT: {finding}")
            return 1
        print("OK AGENTS.md router is valid")
        return 0

    if agents_path.is_file() and not args.force:
        print("AGENTS.md already exists; use --force to overwrite")
        return 0

    with agents_path.open("w", encoding="utf-8", newline="\n") as f:
        f.write(_template())
    print("wrote AGENTS.md")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
