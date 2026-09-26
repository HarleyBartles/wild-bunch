#!/usr/bin/env python3
"""(mixed: validate a git range; --check previews, --apply writes a package)"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

from workspace import resolve_plan, workspace_path


def _git(cwd: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", *args],
        cwd=cwd,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        check=check,
    )


def _verify_revision(repo: Path, label: str, revision: str) -> bool:
    result = _git(repo, "rev-parse", "--verify", "--quiet", revision, check=False)
    if result.returncode:
        print(f"bad {label}: {revision}", file=sys.stderr)
        return False
    return True


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="mixed: validate a git range; --check previews and --apply writes a package"
    )
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="validate and preview (default)")
    mode.add_argument("--apply", action="store_true", help="write the review package")
    parser.add_argument("plan_file", nargs="?", help="plan path, or - for a plan-less package")
    parser.add_argument("base", nargs="?")
    parser.add_argument("head", nargs="?")
    parser.add_argument("out_file", nargs="?")
    return parser


def main() -> int:
    args = build_parser().parse_args()
    if not args.plan_file or not args.base or not args.head:
        if args.apply:
            print("PLAN_FILE, BASE, and HEAD are required with --apply", file=sys.stderr)
            return 2
        print("No package requested; --check requires PLAN_FILE, BASE, and HEAD for a preview.")
        return 0
    try:
        if args.plan_file == "-":
            plan = None
            repo = Path(_git(Path.cwd(), "rev-parse", "--show-toplevel").stdout.strip())
        else:
            plan, repo = resolve_plan(args.plan_file, Path.cwd())
        if not _verify_revision(repo, "BASE", args.base) or not _verify_revision(repo, "HEAD", args.head):
            return 2
        ancestor = _git(repo, "merge-base", "--is-ancestor", args.base, args.head, check=False)
        if ancestor.returncode:
            print(f"HEAD {args.head} is not a descendant of BASE {args.base}", file=sys.stderr)
            return 3
        count = int(_git(repo, "rev-list", "--count", f"{args.base}..{args.head}").stdout.strip())
        if count == 0:
            print(f"empty review range: {args.base}..{args.head}", file=sys.stderr)
            return 3
        if args.out_file:
            output = Path(args.out_file)
            if not output.is_absolute():
                output = repo / output
        else:
            workspace = workspace_path(str(plan) if plan else None, repo, args.apply)
            base_short = _git(repo, "rev-parse", "--short", args.base).stdout.strip()
            head_short = _git(repo, "rev-parse", "--short", args.head).stdout.strip()
            output = workspace / f"review-{base_short}..{head_short}.diff"
        if not args.apply:
            print(f"would write {output}: {count} commit(s)")
            return 0
        commits = _git(repo, "log", "--oneline", f"{args.base}..{args.head}").stdout.rstrip()
        stat = _git(repo, "diff", "--stat", f"{args.base}..{args.head}").stdout.rstrip()
        diff = _git(repo, "diff", "-U10", f"{args.base}..{args.head}").stdout.rstrip()
        content = (
            f"# Review package: {args.base}..{args.head}\n\n"
            f"## Commits\n{commits}\n\n"
            f"## Files changed\n{stat}\n\n"
            f"## Diff\n{diff}\n"
        )
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(content, encoding="utf-8", newline="\n")
        print(f"wrote {output}: {count} commit(s), {output.stat().st_size} bytes")
        return 0
    except (FileNotFoundError, OSError, subprocess.CalledProcessError) as exc:
        print(str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
