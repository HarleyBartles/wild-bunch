#!/usr/bin/env python3
"""record_finding.py - append one or more finding events to the review log. (mixed)"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


REQUIRED = {"finding_id", "lens", "discovered_at_node", "discovered_at_round", "severity"}

_EPILOG = """\nREQUIRED KEYS:
  finding_id, lens, discovered_at_node, discovered_at_round, severity\n\nEXAMPLE JSON FILE:
  {
    "finding_id": "example-001",
    "lens": "reviewer-scripts",
    "discovered_at_node": "lens-triage",
    "discovered_at_round": 1,
    "severity": "important"
  }\n"""


def _load_state(path: Path) -> dict:
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except FileNotFoundError:
        raise SystemExit(f"ERROR: state file not found: {path}")
    except json.JSONDecodeError as e:
        raise SystemExit(f"ERROR: invalid state JSON in {path}: {e}")


def _main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Record a new iterative-review finding. (mixed)",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=_EPILOG,
    )
    parser.add_argument("--check", action="store_true", help="self-check; exits 0 if ready")
    parser.add_argument("--state", help="path to review-state.json")
    parser.add_argument("--data", help="JSON finding object or array of objects")
    parser.add_argument("--data-file", help="path to JSON file containing a single object or array of objects")
    args = parser.parse_args(argv)

    if args.check:
        print("record_finding.py is ready")
        return 0

    if not args.state or (not args.data and not args.data_file):
        parser.error("the following arguments are required: --state, and one of --data or --data-file")
    if args.data and args.data_file:
        parser.error("--data and --data-file are mutually exclusive")

    state_path = Path(args.state)
    state = _load_state(state_path)
    if args.data_file:
        data_file_path = Path(args.data_file)
        try:
            raw_data = data_file_path.read_text(encoding="utf-8-sig")
        except FileNotFoundError:
            print(f"ERROR: data file not found: {data_file_path}", file=sys.stderr)
            return 1
    else:
        raw_data = args.data
    try:
        parsed = json.loads(raw_data)
    except json.JSONDecodeError as e:
        print(f"ERROR: invalid finding JSON: {e}", file=sys.stderr)
        return 1

    if not isinstance(parsed, (dict, list)):
        print("ERROR: --data must be a JSON object or array", file=sys.stderr)
        return 1
    data_items = parsed if isinstance(parsed, list) else [parsed]
    if not all(isinstance(item, dict) for item in data_items):
        print("ERROR: every --data item must be a JSON object", file=sys.stderr)
        return 1

    try:
        scratch = Path(state["scratch_dir"])
    except KeyError as e:
        print(f"ERROR: missing state key {e}", file=sys.stderr)
        return 1
    log = scratch / "findings.jsonl"
    log.parent.mkdir(parents=True, exist_ok=True)

    existing = set()
    if log.exists():
        for line in log.read_text(encoding="utf-8-sig").splitlines():
            if line.strip():
                existing.add(json.loads(line).get("finding_id"))

    errors = []
    for item in data_items:
        if not isinstance(item, dict):
            continue  # guarded above
        missing = REQUIRED - item.keys()
        if missing:
            errors.append(f"missing keys {missing} in {item}")
    if errors:
        for e in errors:
            print(f"ERROR: {e}", file=sys.stderr)
        return 1

    recorded = []
    for item in data_items:
        if item["finding_id"] in existing:
            continue
        with log.open("a", encoding="utf-8", newline="\n") as f:
            f.write(json.dumps(item, ensure_ascii=False) + "\n")
        existing.add(item["finding_id"])
        recorded.append(item["finding_id"])

    if recorded:
        print(f"record_finding.py: recorded {', '.join(recorded)}")
    else:
        print("record_finding.py: all findings already recorded; no change")
    return 0


if __name__ == "__main__":
    raise SystemExit(_main())
