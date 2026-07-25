#!/usr/bin/env python3
"""Ensure the repo's root .gitignore contains the repo-standards sdd rule."""

from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path


SDD_RULE = """# Superpowers sdd/ is a local-only session workspace.
# Track only the directory scaffold (.gitignore); ignore all session contents at any depth.
# plans/ and specs/ are fully repo resident and not governed by this block.
.agents/superpowers/sdd/**
!.agents/superpowers/sdd/.gitignore"""


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


def _has_rule(text: str) -> bool:
    return ".agents/superpowers/sdd/**" in text and "!.agents/superpowers/sdd/.gitignore" in text


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check               verify .gitignore contains the sdd rule
  %(prog)s                       append the sdd rule to .gitignore if missing
  %(prog)s --force               same as without --force (accepted for uniform CLI)

The sdd rule is:

  .agents/superpowers/sdd/**
  !.agents/superpowers/sdd/.gitignore

If the rule is already present, the script makes no changes. Other rules are
preserved.

exit codes:
  0  rule is present or was appended
  1  drift detected or .gitignore could not be written"""
    parser = argparse.ArgumentParser(
        description="Ensure the repo's root .gitignore contains the repo-standards sdd rule.",
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
        help="Accepted for a uniform scaffold interface; has no destructive effect",
    )
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    gitignore_path = repo_root / ".gitignore"

    if gitignore_path.is_file():
        content = gitignore_path.read_text(encoding="utf-8")
        if _has_rule(content):
            print("OK .gitignore: sdd rule present")
            return 0
        if args.check:
            print("DRIFT: .gitignore missing sdd rule")
            return 1
        new_content = content.rstrip() + "\n\n" + SDD_RULE + "\n"
    else:
        if args.check:
            print("DRIFT: .gitignore missing")
            return 1
        new_content = SDD_RULE + "\n"

    gitignore_path.write_text(new_content, encoding="utf-8", newline="\n")
    print(f"wrote {gitignore_path.relative_to(repo_root).as_posix()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
