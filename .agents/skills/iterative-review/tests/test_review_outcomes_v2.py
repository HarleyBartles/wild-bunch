#!/usr/bin/env python3
"""Task-5 kernel derivation tests: report binding and obligation outcomes.

These exercise the fail-closed checks in ``policy._install_attestations``,
``policy._apply_obligation_outcomes`` and ``policy._check_obligation_install``:
attestations bind their dispatch, report-derived verdicts replace declared
verdicts, structured output binds the exact installed record, and typed
obligation outcomes drive status transitions.
"""

from __future__ import annotations

import sys
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR.parent / "scripts"))
sys.path.insert(0, str(TESTS_DIR))

from review_core import model  # noqa: E402
from review_core import policy  # noqa: E402
from review_v2_helpers import (  # noqa: E402
    _Walk,
    _attach_report_claims,
    _attestation_id,
    _bind,
    _map_payload,
    _obligation_payload,
    _put,
)


def _strong_pending(w):
    """Current pending obligations scheduled at the strong gate."""
    return sorted(
        o["obligation_id"]
        for o in w.state["obligations"].values()
        if o["status"] == "pending"
        and policy._current(w.state, o, o["obligation_id"])
        and policy._scheduled_gate_tier(o) == "strong"
    )


def _to_strong(w):
    """preflight + fast + focused reviews, leaving the strong gate pending."""
    if not policy.preflight_current_and_green(w.state, w.policies)[0]:
        w.local_check("run-preflight", kind="preflight", item=w.items[0])
    pending = [
        o
        for o in w.state["obligations"].values()
        if o["status"] == "pending" and policy._current(w.state, o, o["obligation_id"])
    ]
    fast = sorted(o["obligation_id"] for o in pending if policy._scheduled_gate_tier(o) == "fast")
    focused = sorted(o["obligation_id"] for o in pending if policy._scheduled_gate_tier(o) == "focused")
    if fast:
        w.review(
            "run-fast-review",
            role="obligation-reviewer",
            profile="reviewer-fast",
            tier="fast",
            reasoning="low",
            assignments=fast,
        )
    if focused:
        w.review(
            "run-focused-review",
            role="obligation-reviewer",
            profile="reviewer-focused",
            tier="focused",
            reasoning="standard",
            assignments=focused,
        )


def _built_review(w, *, role, profile, tier, reasoning, assignments, outcomes=None):
    """Register -> launch -> attestation, with report claims attached; the
    payload is returned unsubmitted so a test can tamper before completing."""
    serial, did = w._register(
        role=role,
        profile=profile,
        tier=tier,
        reasoning=reasoning,
        assignments=assignments,
    )
    w._launch(did, serial=serial)
    att = w._attestation(did, serial=serial)
    att = _attach_report_claims(w.state, att, role=role, outcomes=outcomes)
    return {"attestations": [att], "findings": []}


def _built_challenge(w, *, categories=None, surfaces=None, hazards=None):
    """Mirror ``_Walk.challenge`` but return the payload unsubmitted."""
    surface = "src/foo.py"
    surf_list = surfaces or [surface]
    inv_hazards = hazards or ["h-con", "h-sem"]
    categories = categories or list(model.OBLIGATION_CATEGORIES)
    serial, did = w._register(
        role="scope-challenger",
        profile="challenger",
        tier="final-strong",
        reasoning="final-strong",
        context_evidence=tuple(m["evidence_id"] for m in w.state["impact_maps"].values()),
    )
    w._launch(did, serial=serial)
    chall_att = w._attestation(did, serial=serial)

    revised = []
    for cat in categories:
        if cat == "security-privacy":
            risk, scope, cons = "high", "surface", ["security"]
        elif cat == "documentation-contract":
            risk, scope, cons = "low", "hunk", ["none"]
        elif cat == "performance-resources":
            risk, scope, cons = "low", "surface", ["none"]
        else:
            risk, scope, cons = "low", "cross-surface", ["security"]
        revised.append(
            _obligation_payload(
                w.state,
                category=cat,
                risk=risk,
                consequences=cons,
                status="pending",
                scope_level=scope,
                surfaces=surf_list,
            )
        )
    revised_categories = {o["category"] for o in revised}
    surviving = [
        o["obligation_id"]
        for o in w.state["obligations"].values()
        if o["category"] not in revised_categories and policy._current(w.state, o, o["obligation_id"])
    ]
    inv_entries = [
        {
            "surface": surface,
            "categories": sorted(model.OBLIGATION_CATEGORIES),
            "hazards": sorted(set(inv_hazards)),
            "consequences": ["security"],
            "obligation_ids": sorted(
                surviving
                + [
                    model.derived_id(
                        "obligation",
                        w.state["snapshot"]["epoch"],
                        model.obligation_subject(o),
                    )
                    for o in revised
                ]
            ),
        }
    ]
    inv_cid = _put(w.state, {"entries": inv_entries})
    inv_ev = _bind(w.state, inv_cid, "scope-challenge")
    inv = {
        "coverage_inventory_id": "",
        "semantic_impact_map_id": next(
            m["impact_map_id"]
            for m in w.state["impact_maps"].values()
            if m["role"] == "impact-mapper-semantic" and policy._current(w.state, m, m["impact_map_id"])
        ),
        "contract_impact_map_id": next(
            m["impact_map_id"]
            for m in w.state["impact_maps"].values()
            if m["role"] == "impact-mapper-contract" and policy._current(w.state, m, m["impact_map_id"])
        ),
        "challenger_attestation_id": _attestation_id(w.state, chall_att),
        "entries": inv_entries,
        "evidence_id": inv_ev,
        "snapshot_epoch": w.state["snapshot"]["epoch"],
        "snapshot_fingerprint": w.state["snapshot"]["fingerprint"],
    }
    chall_att = _attach_report_claims(
        w.state,
        chall_att,
        role="scope-challenger",
        structured_output={
            "kind": "coverage-inventory",
            "subject_sha256": model.sha256_json(model.coverage_inventory_subject(inv)),
            "revised_obligations_sha256": model.sha256_json(revised),
            "record": inv,
            "revised_obligations": revised,
        },
    )
    inv["challenger_attestation_id"] = _attestation_id(w.state, chall_att)
    return {
        "coverage_inventory": inv,
        "attestation": chall_att,
        "findings": [],
        "revised_obligations": revised,
    }


def _complete(w, action, data):
    return policy.complete_action(
        w.state,
        action=action,
        raw_data=model.canonical_json(data),
        policies=w.policies,
    )


class TestAttestationBinding:
    def test_attestation_assignment_ids_must_equal_dispatch(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent_to_exemption()
        _to_strong(w)
        oid = _strong_pending(w)[0]
        data = _built_review(
            w,
            role="obligation-reviewer",
            profile="reviewer-a",
            tier="strong",
            reasoning="high",
            assignments=[oid],
        )
        data["attestations"][0]["assignment_ids"] = []
        with pytest.raises(model.StateValidationError) as exc:
            _complete(w, "run-strong-review", data)
        assert exc.value.code == "assignment-mismatch"

    def test_verdict_must_match_derived(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent_to_exemption()
        _to_strong(w)
        oid = _strong_pending(w)[0]
        data = _built_review(
            w,
            role="obligation-reviewer",
            profile="reviewer-a",
            tier="strong",
            reasoning="high",
            assignments=[oid],
        )
        # The claims derive "clean"; declaring "findings" is a lie.
        data["attestations"][0]["verdict"] = "findings"
        with pytest.raises(model.StateValidationError) as exc:
            _complete(w, "run-strong-review", data)
        assert exc.value.code == "verdict-mismatch"

    def test_unknown_dispatch_id_rejected(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent_to_exemption()
        _to_strong(w)
        oid = _strong_pending(w)[0]
        data = _built_review(
            w,
            role="obligation-reviewer",
            profile="reviewer-a",
            tier="strong",
            reasoning="high",
            assignments=[oid],
        )
        data["attestations"][0]["dispatch_id"] = "dispatch:nonexistent"
        with pytest.raises(model.StateValidationError) as exc:
            _complete(w, "run-strong-review", data)
        assert exc.value.code == "dangling-ref"

    def test_result_kind_must_be_lawful_for_role(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent_to_exemption()
        _to_strong(w)
        oid = _strong_pending(w)[0]
        data = _built_review(
            w,
            role="obligation-reviewer",
            profile="reviewer-a",
            tier="strong",
            reasoning="high",
            assignments=[oid],
        )
        entry = data["attestations"][0]["assignment_results"][0]
        entry["kind"] = "fix-review"
        entry["verified_obligation_ids"] = [oid]
        # Fix the derived verdict so only the kind lawfulness is at issue.
        att = _attach_report_claims(
            w.state,
            data["attestations"][0],
            role="obligation-reviewer",
            outcomes={oid: "covered"},
        )
        att["assignment_results"] = [entry]
        data["attestations"][0] = att
        with pytest.raises(model.StateValidationError) as exc:
            _complete(w, "run-strong-review", data)
        assert exc.value.code == "bad-kind"

    def test_result_outcome_must_be_lawful_for_kind(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent_to_exemption()
        _to_strong(w)
        oid = _strong_pending(w)[0]
        data = _built_review(
            w,
            role="obligation-reviewer",
            profile="reviewer-a",
            tier="strong",
            reasoning="high",
            assignments=[oid],
        )
        data["attestations"][0]["assignment_results"][0]["outcome"] = "verified"
        with pytest.raises(model.StateValidationError) as exc:
            _complete(w, "run-strong-review", data)
        assert exc.value.code == "bad-value"

    def test_result_assignment_must_be_in_dispatch(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent_to_exemption()
        _to_strong(w)
        oid, other = _strong_pending(w)[0], _strong_pending(w)[1]
        data = _built_review(
            w,
            role="obligation-reviewer",
            profile="reviewer-a",
            tier="strong",
            reasoning="high",
            assignments=[oid],
        )
        # Claim a result for an obligation the dispatch never assigned.
        entry = data["attestations"][0]["assignment_results"][0]
        entry["assignment_id"] = other
        with pytest.raises(model.StateValidationError) as exc:
            _complete(w, "run-strong-review", data)
        assert exc.value.code == "assignment-mismatch"


class TestStructuredOutputBinding:
    def test_map_subject_digest_mismatch_rejected(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        entries = [
            {
                "surface": "src/foo.py",
                "category": "behavioral-correctness",
                "hazards": ["h-sem"],
                "consequences": ["security"],
            }
        ]
        serial, did = w._register(
            role="impact-mapper-semantic",
            profile="mapper-semantic",
            tier="strong",
            reasoning="high",
        )
        w._launch(did, serial=serial)
        att = w._attestation(did, serial=serial)
        impact_map = _map_payload(w.state, role="impact-mapper-semantic", entries=entries)
        att = _attach_report_claims(
            w.state,
            att,
            role="impact-mapper-semantic",
            structured_output={
                "kind": "impact-map",
                # A digest over different entries never matches the install.
                "subject_sha256": model.sha256_json({"role": "impact-mapper-semantic", "entries": []}),
                "record": impact_map,
            },
        )
        with pytest.raises(model.StateValidationError) as exc:
            _complete(
                w,
                "map-impact-semantic",
                {"impact_map": impact_map, "attestation": att, "findings": []},
            )
        assert exc.value.code == "structured-output-mismatch"

    def test_map_subject_digest_match_accepted(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.map_impact(
            "map-impact-semantic",
            role="impact-mapper-semantic",
            profile="mapper-semantic",
            entries=[
                {
                    "surface": "src/foo.py",
                    "category": "behavioral-correctness",
                    "hazards": ["h-sem"],
                    "consequences": ["security"],
                }
            ],
        )
        assert any(m["role"] == "impact-mapper-semantic" for m in w.state["impact_maps"].values())

    def test_inventory_subject_and_revised_digest_checked(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.maps()
        w.plan()
        data = _built_challenge(w)
        data["attestation"]["structured_output"]["revised_obligations_sha256"] = "0" * 64
        with pytest.raises(model.StateValidationError) as exc:
            _complete(w, "challenge-coverage", data)
        assert exc.value.code == "structured-output-mismatch"

    def test_challenger_attestation_id_must_resolve(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.maps()
        w.plan()
        data = _built_challenge(w)
        data["coverage_inventory"]["challenger_attestation_id"] = "attestation:1:" + "0" * 64
        with pytest.raises(model.StateValidationError) as exc:
            _complete(w, "challenge-coverage", data)
        assert exc.value.code == "structured-output-mismatch"


class TestObligationOutcomes:
    def test_covered_outcome_covers_obligation(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent_to_exemption()
        _to_strong(w)
        oid = _strong_pending(w)[0]
        assert w.state["obligations"][oid]["status"] == "pending"
        w.review(
            "run-strong-review",
            role="obligation-reviewer",
            profile="reviewer-a",
            tier="strong",
            reasoning="high",
            assignments=[oid],
            outcomes={oid: "covered"},
        )
        assert w.state["obligations"][oid]["status"] == "covered"

    def test_not_applicable_records_attestation(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent_to_exemption()
        _to_strong(w)
        oid = next(
            o["obligation_id"]
            for o in w.state["obligations"].values()
            if o["risk"] == "high" and o["status"] == "pending"
        )
        att = w.review(
            "run-strong-review",
            role="obligation-reviewer",
            profile="reviewer-a",
            tier="strong",
            reasoning="high",
            assignments=[oid],
            outcomes={oid: "not-applicable"},
        )
        o = w.state["obligations"][oid]
        assert o["status"] == "not-applicable"
        assert att["attestation_id"] in o["not_applicable_attestation_ids"]

    def test_findings_outcome_reopens_covered(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent_to_exemption()
        _to_strong(w)
        pending = _strong_pending(w)
        oid = pending[0]
        w.review(
            "run-strong-review",
            role="obligation-reviewer",
            profile="reviewer-a",
            tier="strong",
            reasoning="high",
            assignments=[oid],
            outcomes={oid: "covered"},
        )
        assert w.state["obligations"][oid]["status"] == "covered"
        # Other strong obligations are still pending, so re-reviewing oid with
        # a findings outcome is lawful and returns it to the pending path.
        w.review(
            "run-strong-review",
            role="obligation-reviewer",
            profile="reviewer-b",
            tier="strong",
            reasoning="high",
            assignments=[oid],
            outcomes={oid: "findings"},
        )
        assert w.state["obligations"][oid]["status"] == "pending"

    def test_incomplete_opens_blocker(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent_to_exemption()
        _to_strong(w)
        oid = _strong_pending(w)[0]
        w.review(
            "run-strong-review",
            role="obligation-reviewer",
            profile="reviewer-a",
            tier="strong",
            reasoning="high",
            assignments=[oid],
            outcomes={oid: "incomplete"},
        )
        blockers = [b for b in w.state["blockers"].values() if b["active"]]
        assert len(blockers) == 1
        assert blockers[0]["class"] == "incomplete-review"
        assert w.state["obligations"][oid]["status"] == "pending"
        decision = policy.next_action(w.state, policies=w.policies)
        assert decision.action == "resume-review"

    def test_fix_review_verified_covers_obligations(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent_to_exemption()
        _to_strong(w)
        oid = _strong_pending(w)[0]
        serial, did = w._register(
            role="fix-reviewer",
            profile="fix-reviewer",
            tier="focused",
            reasoning="standard",
            assignments=[oid],
        )
        att = {"dispatch_id": did, "attestation_id": "att:x", "evidence_id": "ev:x"}
        claims = {
            "assignment_results": [
                {
                    "assignment_id": oid,
                    "kind": "fix-review",
                    "outcome": "verified",
                    "verified_obligation_ids": [oid],
                    "evidence_ids": ["ev:x"],
                }
            ]
        }
        policy._apply_obligation_outcomes(w.state, att, claims)
        assert w.state["obligations"][oid]["status"] == "covered"

    def test_high_risk_not_applicable_still_needs_exemption(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent_to_exemption()
        w.review_ascent()
        oid = next(
            o["obligation_id"]
            for o in w.state["obligations"].values()
            if o["risk"] == "high"
            and o["status"] == "not-applicable"
            and policy._current(w.state, o, o["obligation_id"])
        )
        assert oid
        decision = policy.next_action(w.state, policies=w.policies)
        assert decision.action == "run-exemption-challenge"


class TestInstallFloors:
    def test_under_floored_obligation_rejected(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.maps()
        rec = _obligation_payload(
            w.state,
            category="security-privacy",
            risk="high",
            consequences=["security"],
            status="pending",
            scope_level="surface",
        )
        # High-risk surface obligations floor at strong/high; a fast/low
        # declared minimum is below policy.
        rec["minimum_capability_tier"] = "fast"
        rec["minimum_reasoning_floor"] = "low"
        with pytest.raises(model.StateValidationError) as exc:
            _complete(w, "plan-coverage", {"obligations": [rec]})
        assert exc.value.code == "floor-below-policy"

    def test_repo_raised_floor_accepted(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.maps()
        rec = _obligation_payload(
            w.state,
            category="security-privacy",
            risk="high",
            consequences=["security"],
            status="pending",
            scope_level="surface",
        )
        # Raising the declared minimum above the computed floor is lawful.
        rec["minimum_capability_tier"] = "final-strong"
        rec["minimum_reasoning_floor"] = "final-strong"
        w.run("plan-coverage", {"obligations": [rec]})
        installed = next(
            o
            for o in w.state["obligations"].values()
            if o["category"] == "security-privacy" and policy._current(w.state, o, o["obligation_id"])
        )
        assert installed["minimum_capability_tier"] == "final-strong"
        assert installed["status"] == "pending"

    def test_pre_covered_obligation_rejected(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.maps()
        rec = _obligation_payload(
            w.state,
            category="security-privacy",
            risk="high",
            consequences=["security"],
            status="covered",
            scope_level="surface",
        )
        with pytest.raises(model.StateValidationError) as exc:
            _complete(w, "plan-coverage", {"obligations": [rec]})
        assert exc.value.code == "bad-status"
