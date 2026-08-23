#!/usr/bin/env python3
"""Tests for compile_metrics.py."""

import importlib.util
import json
import tempfile
from pathlib import Path

SKILL_DIR = Path(__file__).resolve().parent.parent
COMPILE_METRICS = SKILL_DIR / "scripts" / "compile_metrics.py"


def _load_module():
    spec = importlib.util.spec_from_file_location("compile_metrics", COMPILE_METRICS)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def test_compile_metrics_includes_churn_fields():
    module = _load_module()
    state = {
        "pr": {"branch": "test", "base": "main", "head_sha": "abc"},
        "current_node": "resolved-ledger",
        "previous_node": "regression-scan",
        "round": 3,
        "non_trivial_fix": True,
    }
    with tempfile.TemporaryDirectory() as td:
        scratch = Path(td)
        (scratch / "findings.jsonl").write_text(
            json.dumps(
                {
                    "finding_id": "f-1",
                    "lens": "security",
                    "discovered_at_node": "finding-fix",
                    "discovered_at_round": 2,
                    "severity": "important",
                }
            )
            + "\n",
            encoding="utf-8",
        )
        (scratch / "resolutions.jsonl").write_text(
            json.dumps(
                {
                    "finding_id": "f-1",
                    "resolved_at_node": "reviewer-fixes",
                    "resolved_at_round": 3,
                }
            )
            + "\n",
            encoding="utf-8",
        )
        (scratch / "regressions.jsonl").write_text(
            json.dumps(
                {
                    "fix_for": "f-1",
                    "new_finding": "f-2",
                    "discovered_at_node": "reviewer-fixes",
                    "discovered_at_round": 3,
                    "regression_class": "same-lens-blast-radius",
                    "severity": "important",
                }
            )
            + "\n",
            encoding="utf-8",
        )
        (scratch / "blockers.jsonl").write_text("", encoding="utf-8")
        state["scratch_dir"] = str(scratch)

        logs = {
            "findings": module._load_jsonl(scratch / "findings.jsonl"),
            "resolutions": module._load_jsonl(scratch / "resolutions.jsonl"),
            "regressions": module._load_jsonl(scratch / "regressions.jsonl"),
            "blockers": module._load_jsonl(scratch / "blockers.jsonl"),
        }
        metrics = module._compile(state, logs)

        assert metrics["findings_discovered_at_fix_nodes"] == 1
        assert metrics["regressions_introduced"] == 1
        assert metrics["non_trivial_fix"] is True


def test_compile_metrics_cli_writes_metrics_with_churn_fields():
    module = _load_module()
    with tempfile.TemporaryDirectory() as td:
        scratch = Path(td)
        state_path = scratch / "review-state.json"
        metrics_path = scratch / "review-metrics.json"
        state = {
            "pr": {"branch": "test", "base": "main", "head_sha": "abc"},
            "current_node": "resolved-ledger",
            "previous_node": "regression-scan",
            "round": 1,
            "non_trivial_fix": False,
            "scratch_dir": str(scratch),
        }
        state_path.write_text(json.dumps(state), encoding="utf-8")
        (scratch / "findings.jsonl").write_text("", encoding="utf-8")
        (scratch / "resolutions.jsonl").write_text("", encoding="utf-8")
        (scratch / "regressions.jsonl").write_text("", encoding="utf-8")
        (scratch / "blockers.jsonl").write_text("", encoding="utf-8")
        rc = module._main(["--state", str(state_path), "--metrics", str(metrics_path)])
        assert rc == 0
        written = json.loads(metrics_path.read_text(encoding="utf-8"))
        assert "findings_discovered_at_fix_nodes" in written
        assert "regressions_introduced" in written
        assert written["findings_discovered_at_fix_nodes"] == 0
        assert written["regressions_introduced"] == 0
