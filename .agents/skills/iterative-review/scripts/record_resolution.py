#!/usr/bin/env python3
"""record_resolution.py - append one or more resolution events to the review log. (mixed)"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


REQUIRED = {"finding_id", "resolved_at_node", "resolved_at_round"}
VALID_NODES = {"reviewer-fixes", "regression-scan", "lens-triage"}

_EPILOG = """\nREQUIRED KEYS:
  finding_id, resolved_at_node, resolved_at_round\n\nVALID resolved_at_node VALUES:
  reviewer-fixes, regression-scan, lens-triage\n\nEXAMPLE JSON FILE:
  {
    "finding_id": "example-001",
    "resolved_at_node": "<reviewer-fixes|regression-scan|lens-triage>",
    "resolved_at_round": 2
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
        description="Record a new iterative-review resolution. (mixed)",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=_EPILOG,
    )
    parser.add_argument("--check", action="store_true", help="self-check; exits 0 if ready")
    parser.add_argument("--state", help="path to review-state.json")
    parser.add_argument("--data", help="JSON resolution object or array of objects")
    parser.add_argument("--data-file", help="path to JSON file containing a single object or array of objects")
    args = parser.parse_args(argv)

    if args.check:
        print("record_resolution.py is ready")
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
        print(f"ERROR: invalid resolution JSON: {e}", file=sys.stderr)
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
    log = scratch / "resolutions.jsonl"
    log.parent.mkdir(parents=True, exist_ok=True)

    existing = set()
    if log.exists():
        for line in log.read_text(encoding="utf-8-sig").splitlines():
            if line.strip():
                existing.add(json.loads(line).get("finding_id"))

    errors = []
    for item in data_items:
        if not isinstance(item, dict):
            continue
        missing = REQUIRED - item.keys()
        if missing:
            errors.append(f"missing keys {missing} in {item}")
            continue
        node = item.get("resolved_at_node")
        if node not in VALID_NODES:
            errors.append(f"invalid resolved_at_node {node!r} in {item}")
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
        print(f"record_resolution.py: recorded resolutions for {', '.join(recorded)}")
    else:
        print("record_resolution.py: all resolutions already recorded; no change")
    return 0


if __name__ == "__main__":
    raise SystemExit(_main())
