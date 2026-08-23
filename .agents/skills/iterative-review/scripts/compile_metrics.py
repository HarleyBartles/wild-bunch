#!/usr/bin/env python3
"""compile_metrics.py - generate review-metrics.json from state and logs. (mixed)"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def _load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def _load_jsonl(path: Path) -> list[dict]:
    if not path.exists():
        return []
    return [json.loads(line) for line in path.read_text(encoding="utf-8-sig").splitlines() if line.strip()]


def _compile(state: dict, logs: dict) -> dict:
    findings = logs["findings"]
    resolutions = {r["finding_id"]: r for r in logs["resolutions"]}
    regressions = logs["regressions"]
    blockers = {b["finding_id"]: b for b in logs["blockers"]}

    rounds_per_finding: list[dict] = []
    findings_by_node: dict[str, int] = {}

    for f in findings:
        finding_id = f["finding_id"]
        r = resolutions.get(finding_id)
        b = blockers.get(finding_id)
        contested = (b is not None and b["blocker_class"] == "contested") or (b is None and f.get("contested", False))
        tool_blocked = b is not None and b["blocker_class"] == "tool-blocked"
        entry = {
            "finding_id": finding_id,
            "lens": f["lens"],
            "discovered_at_node": f["discovered_at_node"],
            "discovered_at_round": f["discovered_at_round"],
            "severity": f["severity"],
            "contested": contested,
            "tool_blocked": tool_blocked,
        }
        if r:
            entry["resolved_at_node"] = r["resolved_at_node"]
            entry["resolved_at_round"] = r["resolved_at_round"]
        rounds_per_finding.append(entry)
        findings_by_node[f["discovered_at_node"]] = findings_by_node.get(f["discovered_at_node"], 0) + 1

    total_rounds = max(
        (f.get("discovered_at_round", 0) for f in findings),
        default=state.get("round", 1),
    )
    findings_discovered_at_fix_nodes = sum(
        1 for f in findings if f.get("discovered_at_node") in {"finding-fix", "fast-fix"}
    )
    regressions_introduced = len(regressions)

    return {
        "pr": state.get("pr", {}),
        "findings_by_node": findings_by_node,
        "rounds_per_finding": rounds_per_finding,
        "regressions": regressions,
        "current_node": state.get("current_node"),
        "previous_node": state.get("previous_node"),
        "non_trivial_fix": state.get("non_trivial_fix", False),
        "total_rounds": total_rounds,
        "findings_discovered_at_fix_nodes": findings_discovered_at_fix_nodes,
        "regressions_introduced": regressions_introduced,
    }


def _main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Compile review-metrics.json from review-state and logs. (mixed)")
    parser.add_argument("--check", action="store_true", help="self-check; exits 0 if ready")
    parser.add_argument("--state", help="path to review-state.json")
    parser.add_argument("--metrics", help="path to review-metrics.json")
    args = parser.parse_args(argv)

    if args.check:
        print("compile_metrics.py is ready")
        return 0

    if not args.state or not args.metrics:
        parser.error("the following arguments are required: --state, --metrics")

    state_path = Path(args.state)
    state = _load(state_path)
    try:
        scratch = Path(state["scratch_dir"])
    except KeyError as e:
        print(f"ERROR: missing state key {e}", file=sys.stderr)
        return 1

    logs = {
        "findings": _load_jsonl(scratch / "findings.jsonl"),
        "resolutions": _load_jsonl(scratch / "resolutions.jsonl"),
        "regressions": _load_jsonl(scratch / "regressions.jsonl"),
        "blockers": _load_jsonl(scratch / "blockers.jsonl"),
    }

    metrics = _compile(state, logs)
    metrics_path = Path(args.metrics)
    metrics_path.parent.mkdir(parents=True, exist_ok=True)
    metrics_path.write_text(json.dumps(metrics, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"compile_metrics.py: wrote {metrics_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(_main())
