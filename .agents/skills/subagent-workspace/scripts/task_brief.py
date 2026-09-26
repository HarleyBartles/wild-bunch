#!/usr/bin/env python3
"""(mixed: extract task text; --check previews, --apply writes the brief)"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

from workspace import resolve_plan, workspace_path


TASK_HEADING = re.compile(r"^#+[ \t]+Task[ \t]+([0-9]+)\b")


def extract_task(plan: Path, task_number: str) -> list[str]:
    selected: list[str] = []
    in_fence = False
    in_task = False
    for line in plan.read_text(encoding="utf-8").splitlines():
        if line.startswith("```"):
            in_fence = not in_fence
        elif not in_fence and (match := TASK_HEADING.match(line)):
            if in_task and match.group(1) != task_number:
                break
            in_task = match.group(1) == task_number
        if in_task:
            selected.append(line)
    return selected


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="mixed: extract task text; --check previews and --apply writes the brief"
    )
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="validate and preview (default)")
    mode.add_argument("--apply", action="store_true", help="write the task brief")
    parser.add_argument("plan_file", nargs="?")
    parser.add_argument("task_number", nargs="?")
    parser.add_argument("out_file", nargs="?")
    return parser


def main() -> int:
    args = build_parser().parse_args()
    if not args.plan_file or not args.task_number:
        if args.apply:
            print("PLAN_FILE and TASK_NUMBER are required with --apply", file=sys.stderr)
            return 2
        print("No brief requested; --check requires PLAN_FILE and TASK_NUMBER for a preview.")
        return 0
    try:
        plan, repo_root = resolve_plan(args.plan_file, Path.cwd())
        assert plan is not None
        lines = extract_task(plan, args.task_number)
        if not lines:
            print(f"task {args.task_number} not found in {plan}", file=sys.stderr)
            return 3
        if args.out_file:
            output = Path(args.out_file)
            if not output.is_absolute():
                output = repo_root / output
        else:
            output = workspace_path(str(plan), Path.cwd(), args.apply) / f"task-{args.task_number}-brief.md"
        if not args.apply:
            print(f"would write {output}: {len(lines)} lines")
            return 0
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
        print(f"wrote {output}: {len(lines)} lines")
        return 0
    except (FileNotFoundError, OSError) as exc:
        print(str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
