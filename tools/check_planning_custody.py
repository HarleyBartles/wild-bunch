#!/usr/bin/env python3
"""Check the complete Git index for planning in a main-bound candidate."""

from __future__ import annotations

import argparse
import subprocess
import sys

PLANNING_HOMES = (
    ".agents/plans/",
    ".agents/specs/",
    ".agents/roadmaps/",
    ".agents/investigations/",
)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", required=True)
    parser.add_argument("--target", required=True, help="actual PR base or pushed branch")
    args = parser.parse_args(argv)
    if not args.target.strip():
        parser.error("--target must name the actual destination branch")
    try:
        result = subprocess.run(
            ["git", "ls-files", "--cached", "--full-name", "-z", "--", ":/"],
            capture_output=True,
            check=True,
        )
    except (OSError, subprocess.CalledProcessError) as exc:
        print(f"Cannot read Git index: {exc}", file=sys.stderr)
        return 1
    paths = result.stdout.decode("utf-8", errors="surrogateescape").split("\0")
    offending = sorted({path for path in paths if path.startswith(PLANNING_HOMES)})
    if args.target == "main" and offending:
        print("Planning artifacts are not permitted in the main candidate index:", file=sys.stderr)
        for path in offending:
            print(f"  {path}", file=sys.stderr)
        print(
            "Retire completed work on develop after semantic assessment; "
            "preserve live work there and omit it from the main candidate.",
            file=sys.stderr,
        )
        return 1
    print(f"OK planning custody for target {args.target} ({len(paths) - 1} indexed paths)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
