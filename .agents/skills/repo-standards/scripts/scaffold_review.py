#!/usr/bin/env python3
"""Scaffold or verify the repo's REVIEW.md entry point.

This is a mechanical helper: it creates a standard REVIEW.md scaffold when
one is missing. The agent remains responsible for any repo-specific additions.
"""

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
    return Path(__file__).resolve().parent.parent / "templates" / "REVIEW.md"


def _has_required_boilerplate(content: str) -> bool:
    return (
        "# Review entry point" in content
        and ".agents/doctrine/repo-runbook-policy.md" in content
        and "/requesting-code-review" in content
    )


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check               verify REVIEW.md exists and contains boilerplate
  %(prog)s                       write REVIEW.md if it is missing
  %(prog)s --force               overwrite REVIEW.md with the template

The template expects the file to keep the `# Review entry point` heading and
references to `.agents/doctrine/repo-runbook-policy.md` and `/requesting-code-review`.

exit codes:
  0  REVIEW.md is present/valid or was written
  1  drift detected, template missing, or write failed"""
    parser = argparse.ArgumentParser(
        description="Scaffold the repo's REVIEW.md review entry point. (mixed)",
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
        help="Overwrite an existing REVIEW.md",
    )
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    review_path = repo_root / "REVIEW.md"
    template = _template_path()
    if not template.is_file():
        print(f"ERROR: template not found: {template}", file=sys.stderr)
        return 1

    if review_path.is_file():
        if args.check:
            content = review_path.read_text(encoding="utf-8")
            if not _has_required_boilerplate(content):
                print("DRIFT: REVIEW.md exists but is missing required boilerplate")
                return 1
            print("OK REVIEW.md: review entry point present")
            return 0
        if not args.force:
            print("REVIEW.md already exists; use --force to overwrite")
            return 0

    if args.check:
        print("DRIFT: REVIEW.md missing")
        return 1

    with review_path.open("w", encoding="utf-8", newline="\n") as f:
        f.write(template.read_text(encoding="utf-8"))
    print(f"wrote {review_path.relative_to(repo_root).as_posix()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
