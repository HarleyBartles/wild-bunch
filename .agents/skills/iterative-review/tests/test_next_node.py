#!/usr/bin/env python3
"""Focused tests for next_node.py --propose graph transitions."""

import json
import subprocess
import tempfile
import unittest
from pathlib import Path


SKILL_DIR = Path(__file__).resolve().parent.parent
NEXT_NODE = SKILL_DIR / "scripts" / "next_node.py"


def _write_state(scratch: Path, *, current: str = "setup", previous: str = "", non_trivial_fix: bool = False) -> Path:
    p = scratch / "review-state.json"
    p.write_text(
        json.dumps(
            {
                "current_node": current,
                "previous_node": previous,
                "round": 1,
                "max_fix_rounds": 4,
                "non_trivial_fix": non_trivial_fix,
                "pr": {
                    "pr_number": 999,
                    "base": "main",
                    "branch": "test",
                    "head_sha": "abc123",
                },
                "scratch_dir": str(scratch),
            },
            ensure_ascii=False,
        ),
        encoding="utf-8",
    )
    return p


def _propose(state: Path, node: str, extra: list[str] | None = None) -> subprocess.CompletedProcess:
    cmd = ["py", "-3", str(NEXT_NODE), "--state", str(state), "--propose", node]
    if extra:
        cmd.extend(extra)
    return subprocess.run(
        cmd,
        capture_output=True,
        text=True,
    )


class TestNextNodePropose(unittest.TestCase):
    def test_propose_setup_allows_normalize_inputs(self):
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            state = _write_state(scratch)
            result = _propose(state, "normalize-inputs")
            self.assertEqual(result.returncode, 0)
            self.assertIn("ALLOWED: normalize-inputs", result.stdout)

    def test_propose_blocked_for_missing_artifact(self):
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            # With no unresolved findings and no regressions, reviewer-fixes is
            # ready for the resolved-ledger node, which requires resolutions.jsonl.
            state = _write_state(scratch, current="reviewer-fixes", previous="regression-scan")
            (scratch / "findings.jsonl").write_text("", encoding="utf-8")
            (scratch / "regressions.jsonl").write_text("", encoding="utf-8")
            result = _propose(state, "resolved-ledger")
            self.assertEqual(result.returncode, 1)
            self.assertIn("BLOCKED", result.stderr)
            self.assertIn("resolutions.jsonl", result.stderr)

    def test_non_trivial_routes_reviewer_fixes_to_regression_scan(self):
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            state = _write_state(scratch, current="reviewer-fixes", previous="re-preflight")
            (scratch / "findings.jsonl").write_text("", encoding="utf-8")
            (scratch / "regressions.jsonl").write_text("", encoding="utf-8")
            (scratch / "review-log-reviewer-fixes.md").write_text("\nreviewer-fixes: clean\n", encoding="utf-8")
            result = _propose(state, "regression-scan", extra=["--non-trivial"])
            self.assertEqual(result.returncode, 0)
            self.assertIn("ALLOWED: regression-scan", result.stdout)
            fresh = json.loads(state.read_text(encoding="utf-8"))
            self.assertTrue(fresh.get("non_trivial_fix"))

    def test_non_trivial_cleared_on_resolved_ledger(self):
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            state = _write_state(scratch, current="regression-scan", previous="reviewer-fixes", non_trivial_fix=True)
            (scratch / "findings.jsonl").write_text(
                '{"finding_id": "f-1", "lens": "test", "severity": "trivial"}',
                encoding="utf-8",
            )
            (scratch / "regressions.jsonl").write_text("", encoding="utf-8")
            (scratch / "resolutions.jsonl").write_text('{"finding_id": "f-1"}', encoding="utf-8")
            result = _propose(state, "resolved-ledger")
            self.assertEqual(result.returncode, 0)
            fresh = json.loads(state.read_text(encoding="utf-8"))
            self.assertFalse(fresh.get("non_trivial_fix", True))

    def test_lens_triage_resolution_skips_fix(self):
        """An important finding resolved at lens-triage should route to final-strong."""
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            state = _write_state(scratch, current="lens-triage", previous="normalize-inputs")
            (scratch / "findings.jsonl").write_text(
                '{"finding_id": "f-1", "lens": "test", "severity": "important"}',
                encoding="utf-8",
            )
            (scratch / "resolutions.jsonl").write_text(
                '{"finding_id": "f-1", "resolved_at_node": "lens-triage", "resolved_at_round": 1}',
                encoding="utf-8",
            )
            result = subprocess.run(
                ["py", "-3", str(NEXT_NODE), "--state", str(state)],
                capture_output=True,
                text=True,
            )
            self.assertEqual(result.returncode, 0)
            self.assertIn("final-strong", result.stdout)


REVIEWCTL = SKILL_DIR / "scripts" / "reviewctl.py"


def _write_v1_state(scratch: Path, **overrides) -> Path:
    state = {
        "current_node": "setup",
        "previous_node": "",
        "round": 1,
        "max_fix_rounds": 4,
        "non_trivial_fix": False,
        "pr": {"pr_number": 999, "base": "main", "branch": "test", "head_sha": "abc123"},
        "scratch_dir": str(scratch),
    }
    state.update(overrides)
    p = scratch / "review-state.json"
    p.write_text(json.dumps(state, ensure_ascii=False), encoding="utf-8")
    return p


def _reviewctl_validate(state: Path) -> subprocess.CompletedProcess:
    return subprocess.run(
        ["py", "-3", str(REVIEWCTL), "validate", "--state", str(state)],
        capture_output=True,
        text=True,
    )


def _assert_v1_cannot_seal(testcase: unittest.TestCase, state: Path, scratch: Path) -> None:
    """Version-1 state must be refused by the v2 CLI and by v1 ready."""
    result = _reviewctl_validate(state)
    testcase.assertEqual(result.returncode, 1, result.stderr)
    testcase.assertIn("version-1", result.stderr + result.stdout)
    ready = _propose(state, "ready")
    testcase.assertEqual(ready.returncode, 1)
    testcase.assertIn("BLOCKED", ready.stderr)
    testcase.assertIn("version-1", ready.stderr)
    seal = scratch / "review-state.json"
    testcase.assertNotIn('"green_seal"', seal.read_text(encoding="utf-8"))


class TestV1DefectsCannotProduceV2Green(unittest.TestCase):
    def test_v1_final_strong_without_report_cannot_produce_v2_green(self):
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            # final-strong claims completion but its report artifact is absent.
            state = _write_v1_state(scratch, current_node="final-strong", previous_node="resolved-ledger")
            (scratch / "findings.jsonl").write_text("", encoding="utf-8")
            (scratch / "resolutions.jsonl").write_text("", encoding="utf-8")
            _assert_v1_cannot_seal(self, state, scratch)

    def test_v1_circular_resolution_state_cannot_produce_v2_green(self):
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            state = _write_v1_state(scratch, current_node="resolved-ledger")
            (scratch / "findings.jsonl").write_text('{"finding_id": "f-1"}\n{"finding_id": "f-2"}', encoding="utf-8")
            # f-1 resolved by f-2's fix and vice versa: no independent evidence.
            (scratch / "resolutions.jsonl").write_text(
                '{"finding_id": "f-1", "resolved_by": "f-2"}\n{"finding_id": "f-2", "resolved_by": "f-1"}',
                encoding="utf-8",
            )
            _assert_v1_cannot_seal(self, state, scratch)

    def test_v1_cumulative_preflight_state_cannot_produce_v2_green(self):
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            # A preflight that merged results across epochs must not seal.
            state = _write_v1_state(scratch, current_node="ready", preflight_mode="cumulative")
            _assert_v1_cannot_seal(self, state, scratch)

    def test_v1_lost_normalization_origin_cannot_produce_v2_green(self):
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            # normalize-inputs ran but its origin record was lost/overwritten.
            state = _write_v1_state(scratch, current_node="preflight", normalized_inputs_origin=None)
            _assert_v1_cannot_seal(self, state, scratch)

    def test_v1_blocked_state_cannot_produce_v2_green(self):
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            state = _write_v1_state(scratch, current_node="blocked", blocker="unresolved-finding")
            _assert_v1_cannot_seal(self, state, scratch)

    def test_v1_round_state_cannot_produce_v2_green(self):
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            state = _write_v1_state(scratch, current_node="ready", round=9, max_fix_rounds=4)
            _assert_v1_cannot_seal(self, state, scratch)

    def test_v1_unrepresentable_blocker_cannot_produce_v2_green(self):
        with tempfile.TemporaryDirectory() as td:
            scratch = Path(td)
            # A blocker with no legal class in v2 vocabulary cannot migrate.
            state = _write_v1_state(
                scratch,
                current_node="blocked",
                blocker={"kind": "unrepresentable", "note": "no v2 blocker class exists"},
            )
            _assert_v1_cannot_seal(self, state, scratch)


if __name__ == "__main__":
    unittest.main()
