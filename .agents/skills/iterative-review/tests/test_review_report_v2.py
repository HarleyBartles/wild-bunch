#!/usr/bin/env python3
"""Tests for the section-4 structured reviewer report contract (Plan 3 Task 2)."""

from __future__ import annotations

import sys
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR.parent / "scripts"))

from review_core import model  # noqa: E402
from review_core import report  # noqa: E402


def _report(**over):
    base = {
        "schema_version": 1,
        "review_session_id": "sess-1",
        "attestation_id": "",
        "snapshot_epoch": 1,
        "snapshot_fingerprint": "f" * 64,
        "dispatch_id": "dispatch:abc",
        "reviewer": {
            "profile": "reviewer-a",
            "profile_sha256": "p" * 64,
            "capability_tier": "strong",
            "reasoning_floor": "high",
            "model": "inherit",
            "reasoning": "inherit",
            "context_mode": "fresh",
        },
        "assignments": [
            {
                "assignment_id": "obligation:1",
                "result": {"kind": "obligation", "outcome": "covered", "evidence_ids": ["evidence:e1"]},
                "notes": "ok",
            }
        ],
        "structured_output": {"kind": "assignment-results"},
        "findings": [],
        "uncertainties": [],
        "verdict": "clean",
    }
    base.update(over)
    return base


def _raw(obj) -> bytes:
    return model.canonical_json(obj)


def _adjudication_report(outcome, **result_over):
    result = {
        "kind": "finding-adjudication",
        "outcome": outcome,
        "remediation_class": None,
        "repair_target_kind": None,
        "repair_target_ids": [],
        "evidence_ids": ["evidence:e1"],
    }
    result.update(result_over)
    return _report(
        assignments=[{"assignment_id": "finding:f1", "result": result, "notes": ""}],
    )


class TestParseReport:
    def test_accepts_minimal_obligation_report(self):
        parsed = report.parse_report(_raw(_report()), role="obligation-reviewer")
        assert parsed["assignments"][0]["result"]["outcome"] == "covered"

    def test_rejects_unknown_top_level_key(self):
        r = _report()
        r["surprise"] = True
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(r), role="obligation-reviewer")

    def test_rejects_missing_top_level_key(self):
        r = _report()
        del r["uncertainties"]
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(r), role="obligation-reviewer")

    def test_rejects_wrong_schema_version(self):
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(_report(schema_version=2)), role="obligation-reviewer")

    def test_rejects_result_kind_wrong_for_role(self):
        # An obligation result is not lawful on a blind-final report.
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(_report()), role="blind-final")

    def test_rejects_result_with_extra_keys(self):
        r = _report()
        r["assignments"][0]["result"]["extra"] = 1
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(r), role="obligation-reviewer")

    def test_rejects_missing_outcome(self):
        r = _report()
        del r["assignments"][0]["result"]["outcome"]
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(r), role="obligation-reviewer")

    def test_rejects_empty_evidence_ids(self):
        r = _report()
        r["assignments"][0]["result"]["evidence_ids"] = []
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(r), role="obligation-reviewer")

    def test_rejects_malformed_finding_entry(self):
        r = _report()
        r["findings"] = [{"title": "x"}]
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(r), role="obligation-reviewer")

    def test_rejects_non_dict(self):
        with pytest.raises(report.ReportError):
            report.parse_report(_raw([1, 2]), role="obligation-reviewer")
        with pytest.raises(report.ReportError):
            report.parse_report(b"not json {", role="obligation-reviewer")

    def test_rejects_unknown_role(self):
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(_report()), role="not-a-role")

    def test_rejects_bad_reviewer_block(self):
        r = _report()
        r["reviewer"]["extra"] = 1
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(r), role="obligation-reviewer")
        r = _report()
        r["reviewer"]["capability_tier"] = "godlike"
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(r), role="obligation-reviewer")

    def test_adjudication_confirmed_requires_remediation_class(self):
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(_adjudication_report("confirmed")), role="finding-adjudicator")
        ok = _adjudication_report("confirmed", remediation_class="candidate-change")
        report.parse_report(_raw(ok), role="finding-adjudicator")

    def test_adjudication_review_process_requires_targets(self):
        bad = _adjudication_report("confirmed", remediation_class="review-process")
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(bad), role="finding-adjudicator")
        ok = _adjudication_report(
            "confirmed",
            remediation_class="review-process",
            repair_target_kind="obligation-review",
            repair_target_ids=["attestation:a1"],
        )
        report.parse_report(_raw(ok), role="finding-adjudicator")

    def test_adjudication_false_positive_requires_null_remediation(self):
        bad = _adjudication_report("false-positive", remediation_class="candidate-change")
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(bad), role="finding-adjudicator")
        report.parse_report(_raw(_adjudication_report("false-positive")), role="finding-adjudicator")

    def test_mapper_structured_output_shape(self):
        r = _report(
            assignments=[
                {
                    "assignment_id": "surfaces",
                    "result": {"kind": "impact-map", "outcome": "produced", "evidence_ids": ["evidence:e1"]},
                    "notes": "",
                }
            ],
            structured_output={
                "kind": "impact-map",
                "subject_sha256": "a" * 64,
                "record": {"role": "impact-mapper-semantic", "entries": []},
            },
        )
        parsed = report.parse_report(_raw(r), role="impact-mapper-semantic")
        assert parsed["structured_output"]["kind"] == "impact-map"
        # A reviewer-kind structured_output is rejected on a mapper report.
        r["structured_output"] = {"kind": "assignment-results"}
        with pytest.raises(report.ReportError):
            report.parse_report(_raw(r), role="impact-mapper-semantic")


class TestDeriveVerdict:
    def test_clean_requires_all_lawful_and_no_findings(self):
        assert report.derive_verdict(report.parse_report(_raw(_report()), role="obligation-reviewer")) == "clean"

    def test_findings_present_gives_findings(self):
        r = _report()
        r["findings"] = [
            {
                "title": "t",
                "description": "d",
                "locations": ["src/x.py"],
                "severity": "minor",
                "evidence_ids": [],
                "source_assignment_id": "obligation:1",
            }
        ]
        parsed = report.parse_report(_raw(r), role="obligation-reviewer")
        assert report.derive_verdict(parsed) == "findings"

    def test_incomplete_result_gives_incomplete(self):
        r = _report()
        r["assignments"][0]["result"]["outcome"] = "incomplete"
        parsed = report.parse_report(_raw(r), role="obligation-reviewer")
        assert report.derive_verdict(parsed) == "incomplete"

    def test_uncertainty_forces_not_clean(self):
        r = _report(uncertainties=["could not reach edge"])
        parsed = report.parse_report(_raw(r), role="obligation-reviewer")
        assert report.derive_verdict(parsed) != "clean"

    def test_findings_outcome_is_finding_bearing(self):
        r = _report()
        r["assignments"][0]["result"]["outcome"] = "findings"
        parsed = report.parse_report(_raw(r), role="obligation-reviewer")
        assert report.derive_verdict(parsed) == "findings"


class TestOutcomeExtraction:
    def test_obligation_outcomes_maps_ids(self):
        r = _report()
        r["assignments"].append(
            {
                "assignment_id": "obligation:2",
                "result": {"kind": "obligation", "outcome": "not-applicable", "evidence_ids": ["evidence:e2"]},
                "notes": "",
            }
        )
        parsed = report.parse_report(_raw(r), role="obligation-reviewer")
        assert report.obligation_outcomes(parsed) == {
            "obligation:1": "covered",
            "obligation:2": "not-applicable",
        }

    def test_verified_obligations_fix_review_only(self):
        r = _report(
            assignments=[
                {
                    "assignment_id": "finding:f1",
                    "result": {
                        "kind": "fix-review",
                        "outcome": "verified",
                        "verified_obligation_ids": ["obligation:1"],
                        "evidence_ids": ["evidence:e1"],
                    },
                    "notes": "",
                }
            ]
        )
        parsed = report.parse_report(_raw(r), role="fix-reviewer")
        assert report.verified_obligations(parsed) == ("obligation:1",)
        parsed_ob = report.parse_report(_raw(_report()), role="obligation-reviewer")
        assert report.verified_obligations(parsed_ob) == ()

    def test_structured_subject_mapper_shape(self):
        r = _report(
            assignments=[
                {
                    "assignment_id": "s",
                    "result": {"kind": "impact-map", "outcome": "produced", "evidence_ids": ["evidence:e1"]},
                    "notes": "",
                }
            ],
            structured_output={
                "kind": "impact-map",
                "subject_sha256": "a" * 64,
                "record": {"role": "impact-mapper-semantic", "entries": []},
            },
        )
        parsed = report.parse_report(_raw(r), role="impact-mapper-semantic")
        assert report.structured_subject(parsed)["subject_sha256"] == "a" * 64
        parsed_ob = report.parse_report(_raw(_report()), role="obligation-reviewer")
        assert report.structured_subject(parsed_ob) is None


class TestReportToActionData:
    def _attestation(self):
        return {
            "attestation_id": "",
            "dispatch_id": "dispatch:abc",
            "assignment_ids": ["obligation:1"],
            "verdict": "clean",
            "finding_ids": [],
            "uncertainties": [],
            "tool_transcript_sha256": "t" * 64,
            "evidence_id": "evidence:e0",
            "completion_witness_id": "witness:w1",
            "audit_result": "clean",
        }

    def test_map_impact_payload_shape(self):
        record = {"role": "impact-mapper-semantic", "entries": [], "evidence_id": "evidence:map"}
        r = _report(
            assignments=[
                {
                    "assignment_id": "s",
                    "result": {"kind": "impact-map", "outcome": "produced", "evidence_ids": ["evidence:e1"]},
                    "notes": "",
                }
            ],
            structured_output={
                "kind": "impact-map",
                "subject_sha256": model.sha256_json({"x": 1}),
                "record": record,
            },
        )
        parsed = report.parse_report(_raw(r), role="impact-mapper-semantic")
        data = report.report_to_action_data("map-impact-semantic", parsed, attestation=self._attestation(), findings=[])
        assert set(data) == {"impact_map", "attestation", "findings"}
        assert data["impact_map"] == record
        assert data["attestation"]["structured_output"]["kind"] == "impact-map"
        assert data["attestation"]["assignment_results"][0]["kind"] == "impact-map"

    def test_challenge_coverage_payload_shape(self):
        record = {"semantic_impact_map_id": "impact-map:s", "contract_impact_map_id": "impact-map:c", "entries": []}
        revised = [{"category": "security-privacy", "surfaces": ["s.py"]}]
        r = _report(
            assignments=[
                {
                    "assignment_id": "scope",
                    "result": {"kind": "coverage-inventory", "outcome": "produced", "evidence_ids": ["evidence:e1"]},
                    "notes": "",
                }
            ],
            structured_output={
                "kind": "coverage-inventory",
                "subject_sha256": "b" * 64,
                "revised_obligations_sha256": model.sha256_json(revised),
                "record": record,
                "revised_obligations": revised,
            },
        )
        parsed = report.parse_report(_raw(r), role="scope-challenger")
        data = report.report_to_action_data("challenge-coverage", parsed, attestation=self._attestation(), findings=[])
        assert set(data) == {"coverage_inventory", "revised_obligations", "attestation", "findings"}
        assert data["coverage_inventory"] == record
        assert data["revised_obligations"] == revised

    def test_review_payload_carries_typed_outcomes(self):
        parsed = report.parse_report(_raw(_report()), role="obligation-reviewer")
        data = report.report_to_action_data("run-strong-review", parsed, attestation=self._attestation(), findings=[])
        assert set(data) == {"attestations", "findings"}
        att = data["attestations"][0]
        assert att["structured_output"] == {"kind": "assignment-results"}
        assert att["assignment_results"] == [
            {
                "assignment_id": "obligation:1",
                "kind": "obligation",
                "outcome": "covered",
                "evidence_ids": ["evidence:e1"],
            }
        ]
        assert att["verdict"] == "clean"

    def test_final_review_payload_uses_singular_attestation(self):
        r = _report()
        r["assignments"][0]["result"] = {
            "kind": "blind-final",
            "outcome": "covered",
            "evidence_ids": ["evidence:e1"],
        }
        parsed = report.parse_report(_raw(r), role="blind-final")
        data = report.report_to_action_data("run-final-review", parsed, attestation=self._attestation(), findings=[])
        assert set(data) == {"attestation", "findings"}

    def test_exemption_outcome_attached(self):
        r = _report(
            assignments=[
                {
                    "assignment_id": "obligation:9",
                    "result": {
                        "kind": "exemption-challenge",
                        "outcome": "not-applicable-confirmed",
                        "hypothesis_assignment_ids": ["hypothesis:h1"],
                        "evidence_ids": ["evidence:e1"],
                    },
                    "notes": "",
                }
            ]
        )
        parsed = report.parse_report(_raw(r), role="exemption-challenger")
        data = report.report_to_action_data(
            "run-exemption-challenge", parsed, attestation=self._attestation(), findings=[]
        )
        assert data["attestations"][0]["exemption_outcome"] == "not-applicable-confirmed"
