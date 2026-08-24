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


if __name__ == "__main__":
    unittest.main()
