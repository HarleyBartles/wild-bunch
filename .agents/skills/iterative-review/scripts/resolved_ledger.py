#!/usr/bin/env python3
"""resolved_ledger.py — write review-log-resolved-ledger.md only when the fix queue is clean.

Contract:
- --help   prints usage and exits 0
- --check  reports whether the script is in a runnable state and exits 0
- --apply  writes the ledger if and only if the metrics file shows no unresolved
           important/blocking findings and no regressions.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def _load_metrics(path: Path) -> dict:
    if not path.exists():
        return {}
    return json.loads(path.read_text(encoding="utf-8-sig"))


def _is_ledger_clean(metrics: dict) -> tuple[bool, str]:
    if not metrics:
        return False, "review-metrics.json not found"
    rounds = metrics.get("rounds_per_finding", [])
    for f in rounds:
        severity = f.get("severity", "")
        resolved = f.get("resolved_at_node")
        if severity in ("blocking", "important") and not resolved:
            return False, f"unresolved {severity} finding: {f.get('finding_id')}"
    regressions = metrics.get("regressions", [])
    if regressions:
        return False, f"{len(regressions)} unresolved regression(s)"
    return True, "ledger clean"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Produce the resolved-ledger evidence file. (mixed)")
    parser.add_argument("--check", action="store_true", help="self-check; exits 0 if ready")
    parser.add_argument("--apply", action="store_true", help="write the ledger if allowed")
    parser.add_argument("--metrics", help="path to review-metrics.json")
    parser.add_argument("--ledger", help="path to review-log-resolved-ledger.md")
    args = parser.parse_args(argv)

    if args.check:
        print("resolved_ledger.py is ready")
        return 0

    if not args.metrics:
        print("--metrics is required when not using --check", file=sys.stderr)
        return 2

    metrics_path = Path(args.metrics)
    if not args.ledger:
        ledger_path = metrics_path.parent / "review-log-resolved-ledger.md"
    else:
        ledger_path = Path(args.ledger)

    metrics = _load_metrics(metrics_path)
    clean, reason = _is_ledger_clean(metrics)

    if not clean:
        print(f"BLOCKED: {reason}", file=sys.stderr)
        return 1

    if not args.apply:
        print("resolved-ledger allowed")
        return 0

    pr = metrics.get("pr", {})
    lines = [
        "# Resolved ledger",
        "",
        f"- branch: {pr.get('branch', '<unknown>')}",
        f"- base: {pr.get('base', '<unknown>')}",
        f"- head_sha: {pr.get('head_sha', '<unknown>')}",
        "",
        "All `blocking` and `important` findings recorded in `review-metrics.json` have a `resolved_at_node`.",
        f"Total findings: {len(metrics.get('rounds_per_finding', []))}",
        "Unresolved important/blocking: 0",
        f"Regressions: {len(metrics.get('regressions', []))}",
        "",
        "resolved-ledger: ready for final-strong",
    ]
    ledger_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Wrote {ledger_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
