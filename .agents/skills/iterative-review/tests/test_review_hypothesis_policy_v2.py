#!/usr/bin/env python3
"""Tests for the sealed hypothesis-derivation policy (Plan 3 Task 4)."""

from __future__ import annotations

import json
import sys
from pathlib import Path
from types import SimpleNamespace

import pytest

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR.parent / "scripts"))

from review_core import engine, hypothesis_policy, model, policy  # noqa: E402

SKILL_DIR = TESTS_DIR.parent
DOC_PATH = SKILL_DIR / "references" / "hypothesis-derivation-policy.v1.json"


def _obligation(**over):
    rec = {
        "obligation_id": "obligation:1",
        "category": "behavioral-correctness",
        "surfaces": ["src/foo.py"],
        "risk": "low",
        "consequences": ["none"],
        "scope_level": "surface",
        "minimum_capability_tier": "focused",
        "minimum_reasoning_floor": "standard",
    }
    rec.update(over)
    return rec


class TestSealedDocument:
    def test_loads_shipped_default(self):
        pol = hypothesis_policy.SealedHypothesisDerivationPolicy()
        assert pol.source_id == "review-core-hypothesis-derivation"
        assert pol.source_version == "1"
        assert len(pol.sha256) == 64
        int(pol.sha256, 16)

    def test_rejects_malformed_document(self):
        with pytest.raises(hypothesis_policy.HypothesisPolicyError):
            hypothesis_policy.SealedHypothesisDerivationPolicy(document_bytes=b"not json {")
        with pytest.raises(hypothesis_policy.HypothesisPolicyError):
            hypothesis_policy.SealedHypothesisDerivationPolicy(document_bytes=b"[1]")
        bad = json.loads(DOC_PATH.read_bytes())
        del bad["families"]
        with pytest.raises(hypothesis_policy.HypothesisPolicyError):
            hypothesis_policy.SealedHypothesisDerivationPolicy(document_bytes=model.canonical_json(bad))
        bad = json.loads(DOC_PATH.read_bytes())
        bad["families"]["not-a-category"] = []
        with pytest.raises(hypothesis_policy.HypothesisPolicyError):
            hypothesis_policy.SealedHypothesisDerivationPolicy(document_bytes=model.canonical_json(bad))

    def test_rejects_unknown_template_placeholder(self):
        bad = json.loads(DOC_PATH.read_bytes())
        bad["families"]["behavioral-correctness"][0]["claim"] = "uses {bogus}"
        with pytest.raises(hypothesis_policy.HypothesisPolicyError):
            hypothesis_policy.SealedHypothesisDerivationPolicy(document_bytes=model.canonical_json(bad))

    def test_sha256_stable_over_canonical_bytes(self):
        doc = json.loads(DOC_PATH.read_bytes())
        a = hypothesis_policy.SealedHypothesisDerivationPolicy()
        b = hypothesis_policy.SealedHypothesisDerivationPolicy(document_bytes=DOC_PATH.read_bytes())
        assert a.sha256 == b.sha256
        assert a.sha256 == model.sha256_hex(model.canonical_json(doc))


class TestDerive:
    def test_every_category_produces_at_least_one_pair(self):
        pol = hypothesis_policy.SealedHypothesisDerivationPolicy()
        for category in model.OBLIGATION_CATEGORIES:
            out = pol.derive(obligation=_obligation(category=category))
            polarities = {h["polarity"] for h in out}
            assert {"claim", "counterexample"} <= polarities, category
            assert len(out) >= 2

    def test_claim_and_counterexample_share_family(self):
        pol = hypothesis_policy.SealedHypothesisDerivationPolicy()
        out = pol.derive(obligation=_obligation())
        families = {}
        for h in out:
            families.setdefault(h["family"], set()).add(h["polarity"])
        assert all(ps == {"claim", "counterexample"} for ps in families.values())

    def test_high_risk_gains_extra_families(self):
        pol = hypothesis_policy.SealedHypothesisDerivationPolicy()
        low = pol.derive(obligation=_obligation(risk="low"))
        high = pol.derive(obligation=_obligation(risk="high"))
        assert len(high) > len(low)
        assert {h["family"] for h in low} < {h["family"] for h in high}

    def test_statements_deterministic_for_same_obligation(self):
        pol = hypothesis_policy.SealedHypothesisDerivationPolicy()
        a = pol.derive(obligation=_obligation())
        b = pol.derive(obligation=_obligation())
        assert a == b

    def test_derivation_policy_digest_bound(self):
        pol = hypothesis_policy.SealedHypothesisDerivationPolicy()
        for h in pol.derive(obligation=_obligation()):
            assert h["derivation_policy_sha256"] == pol.sha256

    def test_hypothesis_floors_track_obligation_floor(self):
        pol = hypothesis_policy.SealedHypothesisDerivationPolicy()
        high = _obligation(
            risk="high",
            scope_level="cross-surface",
            consequences=["security"],
            minimum_capability_tier="strong",
            minimum_reasoning_floor="high",
        )
        for h in pol.derive(obligation=high):
            assert h["minimum_capability_tier"] == "strong"
            assert h["minimum_reasoning_floor"] == "high"

    def test_ids_derived_from_subject_not_raw_prompt(self):
        pol = hypothesis_policy.SealedHypothesisDerivationPolicy()
        state = {
            "snapshot": {"epoch": 3, "fingerprint": "a" * 64},
            "obligations": {},
            "hypothesis_assignments": {},
        }
        rec = _obligation()
        rec["assignees"] = []
        rec["status"] = "pending"
        rec["evidence_ids"] = []
        rec["not_applicable_attestation_ids"] = []
        policies = SimpleNamespace(hypotheses=pol)
        policy._install_obligations(state, [rec], policies)
        for h in state["hypothesis_assignments"].values():
            expected = model.derived_id("hypothesis", 3, model.hypothesis_assignment_subject(h))
            assert h["hypothesis_assignment_id"] == expected
            assert h["obligation_id"] in state["obligations"]
            assert h["snapshot_epoch"] == 3
            assert h["snapshot_fingerprint"] == "a" * 64


class TestEngineWiring:
    def test_fail_closed_branch_uses_sealed_policy(self):
        sources = engine.load_witness_sources(runtime="unknown")
        assert isinstance(sources.policies.hypotheses, hypothesis_policy.SealedHypothesisDerivationPolicy)

    def test_builtin_stub_deleted(self):
        assert not hasattr(engine, "_BuiltinHypotheses")
