#!/usr/bin/env python3
"""next_node.py  -  mechanical next-node validator for the iterative-review graph.

Classification (read-only/mutating/mixed): mixed.
- --check                 read-only self-check; exits 0
- --state <path>          canonical router state (read-only or commit gate)
- --metrics <path>        backward-compatible read-only aggregate (compile_metrics output)
- --ledger <path>         path to review-log-resolved-ledger.md (default: state.ledger_path)
- --propose <node>        commit gate; if <node> is the allowed next node and its
                          required artifact logs are non-empty, exits 0 and advances state
- --status                print current status without mutating state
- --resync                compare state to logs and report drift; exits 0 in sync,
                          1 if drift is detected, 2 on usage error
- --resync --apply        correct current_node if the logs have run ahead
- --json                  machine-readable discovery; emits {"node": "...", "reason": "..."}
- no --propose            read-only discovery; prints the allowed next node

The orchestrator must call this before any node recipe (use --propose to advance
state) and must not proceed if it exits 1. The script is the mechanical source of
truth for the graph; it returns the single allowed next node given the state in
review-state.json and the append-only logs in the scratch directory.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


# Canonical graph transitions. Each key is a completed node; the value is a list
# of (condition, next_node) tuples. The first matching condition wins. If no
# condition matches, the default linear next node is used.
#
# Conditions are one of:
#   - "always": unconditional
#   - "green": the previous step has no remaining work (see _green)
#   - "red": the previous step has remaining work (see _red)
#   - "findings": unresolved blocking/important findings exist
#   - "no_findings": no unresolved blocking/important findings exist
#   - "regressions": unresolved regressions exist
#   - "clean": the previous lens pass reported nothing to fix
#   - "trivial": only trivial/deferred findings remain
#   - "blocked": any unresolved blocker (contested or tool-blocked) exists
#   - "contested": a contested finding exists
#   - "tool_blocked": a tool-blocked finding exists
#   - "ledger_missing": the resolved-ledger evidence file is missing
#   - "ready": the resolved-ledger evidence file is present and clean
#   - "more_findings": unresolved findings remain in the queue
#   - "all_resolved": all findings are resolved

GRAPH: dict[str, list[tuple[str, str]]] = {
    "setup": [("always", "normalize-inputs")],
    "preflight": [
        ("red", "fast-fix"),
        ("green", "scope-honesty"),
    ],
    "fast-fix": [("always", "preflight")],
    "scope-honesty": [("always", "reviewer-fast")],
    "reviewer-fast": [
        ("findings", "lens-triage"),
        ("clean", "lens-dispatch"),
    ],
    "lens-triage": [
        ("blocked", "blocked"),
        ("findings", "metrics-track"),
        ("after_reviewer_fast", "lens-dispatch"),
        ("after_lens_dispatch", "final-strong"),
    ],
    "metrics-track": [("always", "finding-fix")],
    "finding-fix": [
        ("round_cap", "blocked"),
        ("always", "re-preflight"),
    ],
    "re-preflight": [
        ("red", "fast-fix"),
        ("fast_origin", "lens-dispatch"),
        ("green", "reviewer-fixes"),
    ],
    "lens-dispatch": [("always", "normalize-inputs")],
    "normalize-inputs": [
        ("after_lens_dispatch", "lens-triage"),
        ("after_setup", "preflight"),
    ],
    "reviewer-fixes": [
        ("blocked", "blocked"),
        ("regressions", "metrics-track"),
        ("non_trivial", "regression-scan"),
        ("reviewer_fixes_clean", "resolved-ledger"),
        ("not_fixed", "finding-fix"),
    ],
    "regression-scan": [
        ("regression_scan_clean", "resolved-ledger"),
        ("new_issue", "metrics-track"),
    ],
    "resolved-ledger": [
        ("more_findings", "finding-fix"),
        ("all_resolved", "final-strong"),
    ],
    "final-strong": [
        ("blocked", "blocked"),
        ("findings", "metrics-track"),
        ("clean", "closeout"),
    ],
    "closeout": [("always", "ready")],
    "ready": [("always", "ready")],  # terminal
    "blocked": [("always", "blocked")],  # terminal
}


# Nodes that require a non-empty log file before the router will allow state to
# advance into them via --propose. Keys are target nodes; values are the required
# log files ("*" means the file just needs to be non-empty).
ARTIFACTS_FOR_NODE: dict[str, list[tuple[str, str]]] = {
    "metrics-track": [("findings.jsonl", "*")],
    "resolved-ledger": [("resolutions.jsonl", "*")],
}


def _load_state(path: Path) -> dict:
    if not path.exists():
        return {}
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (json.JSONDecodeError, OSError):
        return {}


def _load_jsonl(path: Path) -> list[dict]:
    if not path.exists():
        return []
    records: list[dict] = []
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        if not line.strip():
            continue
        try:
            records.append(json.loads(line))
        except json.JSONDecodeError as exc:
            print(f"Warning: malformed JSONL line in {path}: {exc}", file=sys.stderr)
    return records


def _load_metrics(path: Path) -> dict:
    if not path.exists():
        return {}
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (json.JSONDecodeError, OSError):
        return {}


def _lens_log_clean(scratch: Path, lens: str) -> bool:
    """Return True if the lens's log ends with '<lens>: clean'."""
    log = scratch / f"review-log-{lens}.md"
    if not log.exists():
        return False
    for line in reversed(log.read_text(encoding="utf-8").splitlines()):
        stripped = line.strip()
        if not stripped:
            continue
        return stripped == f"{lens}: clean"
    return False


def _unresolved_findings(state: dict) -> list[str]:
    """Return finding_ids of unresolved blocking/important findings."""
    if "rounds_per_finding" in state:
        rounds = state.get("rounds_per_finding", [])
        return [
            f.get("finding_id", "?")
            for f in rounds
            if f.get("severity") in ("blocking", "important") and not f.get("resolved_at_node")
        ]
    scratch = Path(state.get("scratch_dir", "."))
    findings = _load_jsonl(scratch / "findings.jsonl")
    resolved = {r["finding_id"] for r in _load_jsonl(scratch / "resolutions.jsonl")}
    return [
        f["finding_id"]
        for f in findings
        if f["finding_id"] not in resolved and f.get("severity") in ("blocking", "important")
    ]


def _unresolved_regressions(state: dict) -> list[dict]:
    """Return regression events that are not yet resolved."""
    if "regressions" in state and "scratch_dir" not in state:
        return [r for r in state.get("regressions", []) if not r.get("resolved")]
    scratch = Path(state.get("scratch_dir", "."))
    regressions = _load_jsonl(scratch / "regressions.jsonl")
    resolved = {r["finding_id"] for r in _load_jsonl(scratch / "resolutions.jsonl")}
    return [r for r in regressions if r.get("new_finding") not in resolved]


def _findings_by_node(state: dict) -> dict[str, int]:
    """Return a count of findings discovered at each node."""
    if "findings_by_node" in state:
        return state.get("findings_by_node", {})
    scratch = Path(state.get("scratch_dir", "."))
    findings = _load_jsonl(scratch / "findings.jsonl")
    counts: dict[str, int] = {}
    for f in findings:
        node = f.get("discovered_at_node")
        if node:
            counts[node] = counts.get(node, 0) + 1
    return counts


def _contested(state: dict) -> bool:
    """Return True if any unresolved finding is marked as contested."""
    if "rounds_per_finding" in state:
        return any(f.get("contested") for f in state.get("rounds_per_finding", []) if not f.get("resolved_at_node"))
    scratch = Path(state.get("scratch_dir", "."))
    blockers = _load_jsonl(scratch / "blockers.jsonl")
    unresolved = set(_unresolved_findings(state))
    return any(b.get("blocker_class") == "contested" and b.get("finding_id") in unresolved for b in blockers)


def _tool_blocked(state: dict) -> bool:
    """Return True if any unresolved finding is marked as tool-blocked."""
    if "rounds_per_finding" in state:
        return any(f.get("tool_blocked") for f in state.get("rounds_per_finding", []) if not f.get("resolved_at_node"))
    scratch = Path(state.get("scratch_dir", "."))
    blockers = _load_jsonl(scratch / "blockers.jsonl")
    unresolved = set(_unresolved_findings(state))
    return any(b.get("blocker_class") == "tool-blocked" and b.get("finding_id") in unresolved for b in blockers)


def _condition_holds(condition: str, state: dict, ledger: Path, current_node: str) -> bool:
    unresolved = _unresolved_findings(state)
    regressions = _unresolved_regressions(state)
    findings_by_node = _findings_by_node(state)
    ledger_missing = not ledger.exists()
    previous_node = state.get("previous_node", "")
    scratch = Path(state.get("scratch_dir", "."))

    if condition == "always":
        return True
    if condition == "after_lens_dispatch":
        return previous_node == "lens-dispatch"
    if condition == "after_setup":
        return previous_node == "setup"
    if condition == "ready":
        return not ledger_missing
    if condition == "ledger_missing":
        return ledger_missing
    if condition == "findings":
        return bool(unresolved)
    if condition == "no_findings":
        return not unresolved
    if condition == "regressions":
        return bool(regressions)
    if condition == "contested":
        return _contested(state)
    if condition == "tool_blocked":
        return _tool_blocked(state)
    if condition == "blocked":
        return _contested(state) or _tool_blocked(state)
    if condition == "more_findings":
        return bool(unresolved)
    if condition == "all_resolved":
        return not unresolved and not ledger_missing
    if condition == "clean":
        return not unresolved
    if condition == "non_trivial":
        return _lens_log_clean(scratch, "reviewer-fixes") and state.get("non_trivial_fix", False)
    if condition == "trivial":
        if "rounds_per_finding" in state:
            rounds = state.get("rounds_per_finding", [])
            has_trivial = any(f.get("severity") in ("trivial", "deferred") for f in rounds)
        else:
            scratch = Path(state.get("scratch_dir", "."))
            findings = _load_jsonl(scratch / "findings.jsonl")
            has_trivial = any(f.get("severity") in ("trivial", "deferred") for f in findings)
        return not unresolved and has_trivial
    if condition == "red":
        return findings_by_node.get(current_node, 0) > 0
    if condition == "green":
        return findings_by_node.get(current_node, 0) == 0
    if condition == "round_cap":
        if "rounds_per_finding" in state:
            rounds = state.get("rounds_per_finding", [])
            return any(
                (f.get("fix_round", 0) or 0) >= state.get("max_fix_rounds", 4)
                for f in rounds
                if not f.get("resolved_at_node")
            )
        scratch = Path(state.get("scratch_dir", "."))
        findings = _load_jsonl(scratch / "findings.jsonl")
        resolved = {r["finding_id"] for r in _load_jsonl(scratch / "resolutions.jsonl")}
        round_ = state.get("round", 1)
        default_max = state.get("max_fix_rounds", 4)
        by_severity = state.get("max_rounds_by_severity", {})
        by_finding = state.get("max_rounds_by_finding", {})
        return any(
            f["finding_id"] not in resolved
            and f.get("severity") in ("blocking", "important")
            and (round_ - f.get("discovered_at_round", round_) + 1)
            >= by_finding.get(
                f["finding_id"],
                by_severity.get(f.get("severity", ""), default_max),
            )
            for f in findings
        )
    if condition == "fixed":
        return not unresolved and not regressions
    if condition == "not_fixed":
        return bool(unresolved)
    if condition == "new_issue":
        return bool(unresolved) or bool(regressions)
    if condition == "reviewer_fixes_clean":
        return _lens_log_clean(scratch, "reviewer-fixes") and not regressions
    if condition == "regression_scan_clean":
        return not unresolved and not regressions
    if condition == "after_reviewer_fast":
        return previous_node == "reviewer-fast" and not unresolved
    if condition == "after_lens_dispatch":
        return previous_node == "lens-dispatch" and not unresolved
    if condition == "fast_origin":
        findings = _load_jsonl(scratch / "findings.jsonl")
        resolved_ids = {r["finding_id"] for r in _load_jsonl(scratch / "resolutions.jsonl")}
        first_unresolved = next(
            (
                f
                for f in findings
                if f["finding_id"] not in resolved_ids and f.get("severity") in ("blocking", "important")
            ),
            None,
        )
        return not unresolved or (first_unresolved is not None and first_unresolved.get("lens") == "reviewer-fast")

    return False


def _status_report(state: dict, ledger: Path, node: str, reason: str) -> str:
    scratch = Path(state.get("scratch_dir", "."))
    unresolved = _unresolved_findings(state)
    regressions = _unresolved_regressions(state)
    log_preview = []
    for log_name in ("findings.jsonl", "resolutions.jsonl", "regressions.jsonl", "blockers.jsonl"):
        p = scratch / log_name
        if p.exists():
            lines = [line for line in p.read_text(encoding="utf-8-sig").splitlines() if line.strip()]
            log_preview.append(f"{log_name}: {len(lines)} lines")
    return (
        f"current_node: {state.get('current_node', 'unknown')}\n"
        f"previous_node: {state.get('previous_node', '')}\n"
        f"next_allowed: {node}\n"
        f"reason: {reason}\n"
        f"round: {state.get('round', 1)} / max: {state.get('max_fix_rounds', 4)}\n"
        f"unresolved_important_blocking: {len(unresolved)}\n"
        f"unresolved_regressions: {len(regressions)}\n"
        f"ledger_present: {ledger.exists()}\n"
        f"logs:\n" + "\n".join(f"  {entry}" for entry in log_preview)
    )


def _artifacts_present(node: str, state: dict) -> tuple[bool, str]:
    scratch = Path(state.get("scratch_dir", "."))
    for log_name, _ in ARTIFACTS_FOR_NODE.get(node, []):
        p = scratch / log_name
        if not p.exists() or not p.read_text(encoding="utf-8-sig").strip():
            return False, f"missing or empty artifact: {log_name}"
    return True, ""


def _next_node(state: dict, ledger: Path) -> tuple[str, str]:
    current = state.get("current_node")
    if not current or current not in GRAPH:
        return "setup", "no current_node in review-state.json yet"

    transitions = GRAPH[current]
    for condition, next_node in transitions:
        if _condition_holds(condition, state, ledger, current):
            return next_node, f"from {current}: {condition} -> {next_node}"

    # Fall back to the old guard for unresolved / regressions / ledger if the
    # state machine has not yet covered the current node.
    unresolved = _unresolved_findings(state)
    if unresolved:
        return "finding-fix", f"unresolved important/blocking: {', '.join(unresolved)}"
    if _unresolved_regressions(state):
        return "regression-scan", f"{_unresolved_regressions(state)} unresolved regression(s)"
    if not ledger.exists():
        return "resolved-ledger", "resolved-ledger evidence file is missing"
    return "final-strong", "all important findings resolved and ledger evidence present"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Return or validate the allowed next node. (mixed)")
    parser.add_argument("--check", action="store_true", help="self-check; exits 0 if ready")
    parser.add_argument("--state", help="path to review-state.json")
    parser.add_argument("--metrics", help="path to review-metrics.json (read-only, legacy)")
    parser.add_argument("--ledger", help="path to review-log-resolved-ledger.md")
    parser.add_argument("--propose", help="proposed next node to validate")
    parser.add_argument(
        "--non-trivial",
        action="store_true",
        help="set non_trivial_fix when using --propose reviewer-fixes or regression-scan",
    )
    parser.add_argument("--json", action="store_true", help="emit machine-readable discovery JSON")
    parser.add_argument("--status", action="store_true", help="print status without mutating state")
    parser.add_argument("--resync", action="store_true", help="compare state to logs and report drift")
    parser.add_argument("--apply", action="store_true", help="apply the correction during --resync")
    args = parser.parse_args(argv)

    if args.apply and not args.resync:
        print("--apply is only valid with --resync", file=sys.stderr)
        return 2
    if args.propose and args.apply:
        print("--apply is only valid with --resync", file=sys.stderr)
        return 2
    if args.propose and (args.status or args.resync):
        print("--propose cannot be combined with --status or --resync", file=sys.stderr)
        return 2
    if args.non_trivial and not args.propose:
        print("--non-trivial is only valid with --propose", file=sys.stderr)
        return 2
    if args.non_trivial and args.propose not in {"reviewer-fixes", "regression-scan"}:
        print("--non-trivial is only valid with --propose reviewer-fixes or regression-scan", file=sys.stderr)
        return 2
    if args.check and (args.status or args.resync):
        print("--check cannot be combined with --status or --resync", file=sys.stderr)
        return 2

    if not args.check and not args.state and not args.metrics:
        print("--state or --metrics is required when not using --check", file=sys.stderr)
        return 2

    if args.check:
        print("next_node.py is ready")
        return 0

    state_path: Path | None = None
    if args.state:
        state_path = Path(args.state)
        state = _load_state(state_path)
        if args.non_trivial:
            state["non_trivial_fix"] = True
        ledger_path = (
            Path(args.ledger)
            if args.ledger
            else Path(state.get("ledger_path", state_path.parent / "review-log-resolved-ledger.md"))
        )
        node, reason = _next_node(state, ledger_path)
    elif args.metrics:
        if args.propose:
            # Existing recipes call --propose with --metrics; derive the canonical
            # review-state.json path from the metrics file.
            state_path = Path(args.metrics).with_name("review-state.json")
            state = _load_state(state_path)
            if args.non_trivial:
                state["non_trivial_fix"] = True
            ledger_path = (
                Path(args.ledger)
                if args.ledger
                else Path(state.get("ledger_path", state_path.parent / "review-log-resolved-ledger.md"))
            )
            node, reason = _next_node(state, ledger_path)
        else:
            # Backward-compatible read-only discovery from compiled metrics.
            metrics_path = Path(args.metrics)
            ledger_path = Path(args.ledger) if args.ledger else metrics_path.parent / "review-log-resolved-ledger.md"
            metrics = _load_metrics(metrics_path)
            node, reason = _next_node(metrics, ledger_path)
    else:
        print("--state or --metrics is required when not using --check", file=sys.stderr)
        return 2

    if args.status:
        if args.json:
            payload = {
                "current_node": state.get("current_node", "unknown"),
                "previous_node": state.get("previous_node", ""),
                "next_allowed": node,
                "reason": reason,
                "round": state.get("round", 1),
                "max_fix_rounds": state.get("max_fix_rounds", 4),
                "unresolved_important_blocking": len(_unresolved_findings(state)),
                "unresolved_regressions": len(_unresolved_regressions(state)),
                "ledger_present": ledger_path.exists(),
            }
            print(json.dumps(payload, ensure_ascii=False))
        else:
            print(_status_report(state, ledger_path, node, reason))
        return 0

    if args.resync:
        if args.apply and state_path is None:
            print("--state is required for --resync --apply", file=sys.stderr)
            return 2
        saved = state.get("current_node", "unknown")
        if saved == node:
            print(f"SYNC: current_node {saved} matches log-implied next node")
            return 0
        print(f"DRIFT: current_node is {saved}; logs imply {node}  -  {reason}")
        if not args.apply:
            print("Use --resync --apply to correct the state pointer", file=sys.stderr)
            return 1
        fresh = _load_state(state_path)
        fresh["previous_node"] = fresh.get("current_node", "")
        fresh["current_node"] = node
        state_path.write_text(json.dumps(fresh, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        print(f"SYNC: corrected current_node to {node}")
        return 0

    if not args.propose:
        # Discovery is read-only: it reports the allowed next node from the
        # current state without advancing state.
        if args.json:
            print(json.dumps({"node": node, "reason": reason}, ensure_ascii=False))
        else:
            print(f"{node}\n# {reason}")
            if node not in ("ready", "blocked"):
                node_path = f".agents/skills/iterative-review/references/node-{node}.md"
                state_path_for_hint = state_path or Path(args.metrics).with_name("review-state.json")
                print(f"# recipe: {node_path}")
                print(
                    "# authorize: py -3 .agents/skills/iterative-review/scripts/next_node.py "
                    f"--propose {node} --state {state_path_for_hint}"
                )
    elif state_path is not None and args.propose == node:
        ok, missing = _artifacts_present(args.propose, state)
        if not ok:
            print(f"BLOCKED: proposed {args.propose} is allowed, but {missing}", file=sys.stderr)
            return 1
        print(f"ALLOWED: {args.propose}  -  {reason}")
        # Re-read the state from disk immediately before writing to avoid
        # overwriting concurrent changes with a stale in-memory dict.
        fresh = _load_state(state_path)
        fresh["previous_node"] = fresh.get("current_node", "")
        fresh["current_node"] = node
        if args.propose == "reviewer-fixes":
            fresh["non_trivial_fix"] = args.non_trivial
        elif args.propose == "regression-scan":
            fresh["non_trivial_fix"] = state.get("non_trivial_fix", False)
        else:
            fresh["non_trivial_fix"] = False
        state_path.write_text(json.dumps(fresh, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    else:
        print(f"BLOCKED: proposed {args.propose}; allowed next node is {node}  -  {reason}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
