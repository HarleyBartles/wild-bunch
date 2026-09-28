#!/usr/bin/env python3
"""Tests for the sealed review-assignment policy (Plan 3 Task 3)."""

from __future__ import annotations

import json
import sys
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR.parent / "scripts"))

from review_core import assignment_policy, engine, model  # noqa: E402

SKILL_DIR = TESTS_DIR.parent
DOC_PATH = SKILL_DIR / "references" / "review-assignment-policy.v1.json"


def _doc(**over):
    doc = json.loads(DOC_PATH.read_bytes())
    doc.update(over)
    return model.canonical_json(doc)


def _obligation(oid, *, scope="surface", risk="low", consequences=("none",)):
    return {
        "obligation_id": oid,
        "scope_level": scope,
        "risk": risk,
        "consequences": list(consequences),
    }


def _state(**over):
    state = {
        "obligations": {},
        "hypothesis_assignments": {},
        "findings": {},
        "reviews": {},
        "dispatches": {},
        "route_selections": {},
        "review_repairs": {},
        "checks": {},
    }
    state.update(over)
    return state


def _dispatch(state, did, *, role, assignment_ids=()):
    rsid = f"route:{did}"
    state["route_selections"][rsid] = {
        "route_selection_id": rsid,
        "required_role": role,
        "snapshot_epoch": 1,
        "snapshot_fingerprint": "f" * 64,
    }
    state["dispatches"][did] = {
        "dispatch_id": did,
        "route_selection_id": rsid,
        "assignment_ids": list(assignment_ids),
        "snapshot_epoch": 1,
        "snapshot_fingerprint": "f" * 64,
    }
    return state["dispatches"][did]


class TestSealedDocument:
    def test_loads_shipped_default(self):
        pol = assignment_policy.SealedReviewAssignmentPolicy()
        assert pol.source_id == "review-core-review-assignment"
        assert pol.source_version == "1"
        assert len(pol.sha256) == 64
        int(pol.sha256, 16)

    def test_rejects_malformed_document(self):
        with pytest.raises(assignment_policy.AssignmentPolicyError):
            assignment_policy.SealedReviewAssignmentPolicy(document_bytes=b"not json {")
        with pytest.raises(assignment_policy.AssignmentPolicyError):
            assignment_policy.SealedReviewAssignmentPolicy(document_bytes=b'{"policy_id": "wrong"}')
        with pytest.raises(assignment_policy.AssignmentPolicyError):
            assignment_policy.SealedReviewAssignmentPolicy(document_bytes=b"[1, 2, 3]")
        bad = json.loads(DOC_PATH.read_bytes())
        del bad["independence"]
        with pytest.raises(assignment_policy.AssignmentPolicyError):
            assignment_policy.SealedReviewAssignmentPolicy(document_bytes=model.canonical_json(bad))

    def test_rejects_unknown_role_in_floors(self):
        bad = json.loads(DOC_PATH.read_bytes())
        bad["role_floors"]["not-a-role"] = {"tier": "fast", "reasoning": "low"}
        with pytest.raises(assignment_policy.AssignmentPolicyError):
            assignment_policy.SealedReviewAssignmentPolicy(document_bytes=model.canonical_json(bad))

    def test_sha256_stable_over_canonical_bytes(self):
        doc = json.loads(DOC_PATH.read_bytes())
        a = assignment_policy.SealedReviewAssignmentPolicy()
        b = assignment_policy.SealedReviewAssignmentPolicy(document_bytes=DOC_PATH.read_bytes())
        assert a.sha256 == b.sha256
        assert a.sha256 == model.sha256_hex(model.canonical_json(doc))


class TestRequirement:
    def test_mapper_floors_and_independence(self):
        pol = assignment_policy.SealedReviewAssignmentPolicy()
        req = pol.requirement(state=_state(), role="impact-mapper-semantic", assignment_ids=())
        assert req.capability_tier == "strong"
        assert req.reasoning_floor == "high"
        assert req.context_mode == "fresh"
        assert "impact-mapper-contract" in req.distinct_role_contract_from
        assert "impact-mapper-semantic" in req.distinct_execution_from

    def test_scope_challenger_distinct_from_both_mappers(self):
        pol = assignment_policy.SealedReviewAssignmentPolicy()
        req = pol.requirement(state=_state(), role="scope-challenger", assignment_ids=())
        assert req.capability_tier == "final-strong"
        assert req.reasoning_floor == "final-strong"
        assert set(req.distinct_role_contract_from) >= {
            "impact-mapper-semantic",
            "impact-mapper-contract",
        }

    def test_obligation_reviewer_composes_max_floor(self):
        state = _state(
            obligations={
                "obligation:low": _obligation("obligation:low", scope="surface", risk="low"),
                "obligation:high": _obligation(
                    "obligation:high", scope="cross-surface", risk="high", consequences=["security"]
                ),
            }
        )
        pol = assignment_policy.SealedReviewAssignmentPolicy()
        req = pol.requirement(
            state=state,
            role="obligation-reviewer",
            assignment_ids=("obligation:low", "obligation:high"),
        )
        assert req.capability_tier == "strong"
        assert req.reasoning_floor == "high"
        low_only = pol.requirement(state=state, role="obligation-reviewer", assignment_ids=("obligation:low",))
        assert low_only.capability_tier == "focused"
        assert low_only.reasoning_floor == "standard"

    def test_blind_final_floor(self):
        pol = assignment_policy.SealedReviewAssignmentPolicy()
        req = pol.requirement(state=_state(), role="blind-final", assignment_ids=())
        assert req.capability_tier == "final-strong"
        assert req.reasoning_floor == "final-strong"
        assert req.context_mode == "fresh"

    def test_policy_may_raise_never_lower(self):
        doc = json.loads(DOC_PATH.read_bytes())
        doc["role_floors"]["obligation-reviewer"] = {"tier": "focused", "reasoning": "standard"}
        raised = assignment_policy.SealedReviewAssignmentPolicy(document_bytes=model.canonical_json(doc))
        req = raised.requirement(state=_state(), role="obligation-reviewer", assignment_ids=())
        assert req.capability_tier == "focused"
        state = _state(
            obligations={
                "obligation:high": _obligation(
                    "obligation:high", scope="cross-surface", risk="high", consequences=["security"]
                )
            }
        )
        req = raised.requirement(state=state, role="obligation-reviewer", assignment_ids=("obligation:high",))
        assert req.capability_tier == "strong"
        assert req.reasoning_floor == "high"

    def test_unknown_role_rejected(self):
        pol = assignment_policy.SealedReviewAssignmentPolicy()
        with pytest.raises(assignment_policy.AssignmentPolicyError):
            pol.requirement(state=_state(), role="not-a-role", assignment_ids=())

    def test_adjudicator_distinct_from_source_role(self):
        state = _state()
        _dispatch(state, "dispatch:src", role="obligation-reviewer", assignment_ids=("obligation:1",))
        state["reviews"]["review:src"] = {
            "attestation_id": "review:src",
            "dispatch_id": "dispatch:src",
        }
        state["findings"]["finding:1"] = {
            "finding_id": "finding:1",
            "source_kind": "review",
            "source_id": "review:src",
            "source_assignment_id": "obligation:1",
            "obligation_id": "obligation:1",
        }
        state["obligations"]["obligation:1"] = _obligation(
            "obligation:1", scope="cross-surface", risk="high", consequences=["security"]
        )
        pol = assignment_policy.SealedReviewAssignmentPolicy()
        req = pol.requirement(state=state, role="finding-adjudicator", assignment_ids=("finding:1",))
        assert "obligation-reviewer" in req.distinct_role_contract_from
        assert "obligation-reviewer" in req.distinct_execution_from
        assert req.capability_tier == "strong"

    def test_repair_verifier_final_strong_when_cut_reaches_closure(self):
        state = _state()
        _dispatch(state, "dispatch:adj", role="finding-adjudicator")
        state["reviews"]["review:adj"] = {
            "attestation_id": "review:adj",
            "dispatch_id": "dispatch:adj",
        }
        state["review_repairs"]["repair:1"] = {
            "repair_id": "repair:1",
            "target_kind": "blind-final",
            "target_ids": ["review:final"],
            "invalidated_record_ids": ["review:final"],
            "entry_adjudicator_attestation_id": "review:adj",
        }
        pol = assignment_policy.SealedReviewAssignmentPolicy()
        req = pol.requirement(state=state, role="review-repair-verifier", assignment_ids=("repair:1",))
        assert req.capability_tier == "final-strong"
        assert req.reasoning_floor == "final-strong"
        assert "finding-adjudicator" in req.distinct_role_contract_from


class TestEngineWiring:
    def test_fail_closed_branch_uses_sealed_policy(self):
        sources = engine.load_witness_sources(runtime="unknown")
        assert isinstance(sources.policies.review_assignments, assignment_policy.SealedReviewAssignmentPolicy)

    def test_builtin_stub_deleted(self):
        assert not hasattr(engine, "_BuiltinReviewAssignments")
