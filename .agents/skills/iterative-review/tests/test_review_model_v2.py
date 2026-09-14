#!/usr/bin/env python3
"""Focused tests for the version-2 strict state model."""

from __future__ import annotations

import hashlib
import json
import sys
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR))
sys.path.insert(0, str(TESTS_DIR.parent / "scripts"))

from review_v2_helpers import make_empty_v2_state  # noqa: E402

from review_core import model  # noqa: E402


def _empty(tmp_path, **overrides):
    return make_empty_v2_state(tmp_path, **overrides)


def _snapshot(**overrides):
    snap = {
        "epoch": 1,
        "repository_id": "o/r",
        "pr_number": 7,
        "pr_url": "https://github.com/o/r/pull/7",
        "git_object_format": "sha1",
        "base_sha": "a" * 40,
        "head_sha": "b" * 40,
        "tree_sha": "c" * 40,
        "diff_sha256": "d" * 64,
        "pr_metadata_sha256": "e" * 64,
        "authority_manifest_sha256": "f" * 64,
        "authority_discovery_policy_id": "adp",
        "authority_discovery_policy_version": "1",
        "authority_discovery_policy_sha256": "0" * 64,
        "witness_policy_sha256": "1" * 64,
        "feedback_history_policy_id": "fhp",
        "feedback_history_policy_version": "1",
        "feedback_history_policy_sha256": "2" * 64,
        "feedback_history_sha256": "3" * 64,
        "local_check_policy_id": "lcp",
        "local_check_policy_version": "1",
        "local_check_policy_sha256": "4" * 64,
        "required_check_policy_sha256": "5" * 64,
        "review_assignment_policy_id": "rap",
        "review_assignment_policy_version": "1",
        "review_assignment_policy_sha256": "6" * 64,
        "command_execution_policy_id": "cep",
        "command_execution_policy_version": "1",
        "command_execution_policy_sha256": "7" * 64,
        "evidence_ingestion_policy_id": "eip",
        "evidence_ingestion_policy_version": "1",
        "evidence_ingestion_policy_sha256": "8" * 64,
        "hypothesis_derivation_policy_id": "hdp",
        "hypothesis_derivation_policy_version": "1",
        "hypothesis_derivation_policy_sha256": "9" * 64,
        "unresolved_feedback_sha256": "a" * 64,
    }
    snap.update(overrides)
    snap["fingerprint"] = model.snapshot_fingerprint(snap)
    return snap


# --- strict_json_loads ------------------------------------------------------


def test_strict_json_rejects_duplicate_keys():
    with pytest.raises(model.StateValidationError):
        model.strict_json_loads(b'{"a": 1, "a": 2}')


def test_strict_json_rejects_bom():
    with pytest.raises(model.StateValidationError):
        model.strict_json_loads(b'\xef\xbb\xbf{"a": 1}')


def test_strict_json_rejects_non_finite():
    with pytest.raises(model.StateValidationError):
        model.strict_json_loads(b'{"a": NaN}')


def test_strict_json_accepts_nested_canonical():
    value = model.strict_json_loads(b'{"b": [1, 2], "a": {"x": null}}')
    assert value == {"a": {"x": None}, "b": [1, 2]}


# --- top-level shape ---------------------------------------------------------


def test_empty_state_validates(tmp_path):
    model.validate_state(_empty(tmp_path))


def test_new_state_round_trip(tmp_path):
    state = model.new_state("review-test", tmp_path)
    model.validate_state(state)
    blob = model.canonical_json(state)
    assert model.strict_json_loads(blob) == state
    assert model.canonical_json(model.strict_json_loads(blob)) == blob


def test_helper_state_keys_match_new_state(tmp_path):
    assert set(_empty(tmp_path)) == set(model.new_state("x-1", tmp_path))


def test_unknown_top_level_field(tmp_path):
    state = _empty(tmp_path, surprise={})
    with pytest.raises(model.StateValidationError):
        model.validate_state(state)


def test_missing_top_level_field(tmp_path):
    state = _empty(tmp_path)
    del state["calibration"]
    with pytest.raises(model.StateValidationError):
        model.validate_state(state)


def test_schema_version_other_than_2(tmp_path):
    for bad in (1, 3, "2", None):
        with pytest.raises(model.StateValidationError):
            model.validate_state(_empty(tmp_path, schema_version=bad))


def test_relative_scratch_dir_rejected(tmp_path):
    with pytest.raises(model.StateValidationError):
        model.validate_state(_empty(tmp_path, scratch_dir="relative/dir"))


@pytest.mark.parametrize(
    "field,bad",
    [
        ("status", "green"),
        ("status", "done"),
        ("stage", "quantum"),
        ("stage", "ready"),
    ],
)
def test_unknown_top_level_enum(tmp_path, field, bad):
    with pytest.raises(model.StateValidationError):
        model.validate_state(_empty(tmp_path, **{field: bad}))


def test_generation_must_be_nonnegative_int(tmp_path):
    for bad in (-1, "1", 1.5, True):
        with pytest.raises(model.StateValidationError):
            model.validate_state(_empty(tmp_path, generation=bad))


def test_persisted_green_status_rejected(tmp_path):
    with pytest.raises(model.StateValidationError):
        model.validate_state(_empty(tmp_path, status="green"))


# --- snapshot ----------------------------------------------------------------


def test_snapshot_fingerprint_must_match_subject(tmp_path):
    snap = _snapshot()
    snap["fingerprint"] = "0" * 64
    with pytest.raises(model.StateValidationError):
        model.validate_state(_empty(tmp_path, snapshot=snap, stage="authority"))


def test_snapshot_required_past_intake(tmp_path):
    with pytest.raises(model.StateValidationError):
        model.validate_state(_empty(tmp_path, stage="authority"))


def test_git_sha_length_must_match_object_format(tmp_path):
    snap = _snapshot(head_sha="b" * 64)  # 64-hex under sha1 format
    with pytest.raises(model.StateValidationError):
        model.validate_state(_empty(tmp_path, snapshot=snap, stage="authority"))


def test_snapshot_unknown_field(tmp_path):
    snap = _snapshot()
    snap["extra"] = 1
    with pytest.raises(model.StateValidationError):
        model.validate_state(_empty(tmp_path, snapshot=snap, stage="authority"))


# --- record validation -------------------------------------------------------


def _evidence(state, **overrides):
    epoch, fp = _current(state)
    rec = {
        "evidence_id": "placeholder",
        "content_id": "sha256:" + "a" * 64,
        "kind": "authority",
        "snapshot_epoch": epoch,
        "snapshot_fingerprint": fp,
    }
    rec.update(overrides)
    rec["evidence_id"] = "evidence:" + model.sha256_json(model.evidence_binding_subject(rec))
    return rec


def _current(state):
    snap = state["snapshot"] or {"epoch": 1, "fingerprint": "f" * 64}
    return snap["epoch"], snap["fingerprint"]


def test_mapping_key_must_match_record_id(tmp_path):
    state = _empty(tmp_path, snapshot=_snapshot(), stage="authority")
    ev = _evidence(state)
    state["evidence"]["different-key"] = ev
    with pytest.raises(model.StateValidationError):
        model.validate_state(state)


def test_epoch_zero_record_rejected(tmp_path):
    state = _empty(tmp_path, snapshot=_snapshot(), stage="authority")
    ev = _evidence(state, snapshot_epoch=0)
    state["evidence"][ev["evidence_id"]] = ev
    with pytest.raises(model.StateValidationError):
        model.validate_state(state)


def test_evidence_binding_id_must_be_derived(tmp_path):
    state = _empty(tmp_path, snapshot=_snapshot(), stage="authority")
    ev = _evidence(state)
    ev["evidence_id"] = "evidence:wrong"
    state["evidence"]["evidence:wrong"] = ev
    with pytest.raises(model.StateValidationError):
        model.validate_state(state)


def test_evidence_content_reference_must_exist(tmp_path):
    state = _empty(tmp_path, snapshot=_snapshot(), stage="authority")
    ev = _evidence(state)
    state["evidence"][ev["evidence_id"]] = ev
    with pytest.raises(model.StateValidationError):
        model.validate_state(state)  # content_id 'a'*64 absent


def test_current_epoch_record_fingerprint_must_match(tmp_path):
    state = _empty(tmp_path, snapshot=_snapshot(), stage="authority")
    ev = _evidence(state, snapshot_fingerprint="0" * 64)
    state["evidence"][ev["evidence_id"]] = ev
    with pytest.raises(model.StateValidationError):
        model.validate_state(state)


def test_impact_map_derived_id(tmp_path):
    state = _empty(tmp_path, snapshot=_snapshot(), stage="coverage")
    epoch, fp = _current(state)
    rec = {
        "impact_map_id": "x",
        "role": "impact-mapper-semantic",
        "entries": [
            {
                "surface": "src/a.py",
                "category": "behavioral-correctness",
                "hazards": ["h1"],
                "consequences": ["none"],
            }
        ],
        "evidence_id": "e-1",
        "snapshot_epoch": epoch,
        "snapshot_fingerprint": fp,
    }
    rec["impact_map_id"] = model.derived_id("impact-map", epoch, model.impact_map_subject(rec))
    state["impact_maps"][rec["impact_map_id"]] = rec
    # evidence ref dangles: prove cross-reference checks run
    with pytest.raises(model.StateValidationError):
        model.validate_state(state)
    ev = _evidence(state, evidence_id=None)
    ev["evidence_id"] = "evidence:" + model.sha256_json(model.evidence_binding_subject({**ev, "kind": "impact-map"}))
    ev["kind"] = "impact-map"
    state["content_objects"]["sha256:" + "a" * 64] = {
        "content_id": "sha256:" + "a" * 64,
        "path": str(tmp_path / "evidence-store" / "sha256" / ("a" * 64)),
        "sha256": "a" * 64,
        "bytes": 0,
    }
    state["evidence"][ev["evidence_id"]] = ev
    rec["evidence_id"] = ev["evidence_id"]
    rec["impact_map_id"] = model.derived_id("impact-map", epoch, model.impact_map_subject(rec))
    state["impact_maps"] = {rec["impact_map_id"]: rec}
    # content object file doesn't exist -> verify_content path trips
    with pytest.raises(model.StateValidationError):
        model.validate_state(state)
    model.validate_state(state, verify_content=False)


def test_green_seal_only_at_green_candidate(tmp_path):
    seal = {
        "snapshot_epoch": 1,
        "snapshot_fingerprint": "f" * 64,
        "coverage_sha256": "0" * 64,
        "findings_sha256": "0" * 64,
        "repairs_sha256": "0" * 64,
        "reviews_sha256": "0" * 64,
        "checks_sha256": "0" * 64,
        "witness_chain_head_sha256": "0" * 64,
        "evidence_ids": [],
        "created_at": "2026-09-12T00:00:00Z",
    }
    with pytest.raises(model.StateValidationError):
        model.validate_state(_empty(tmp_path, green_seal=seal, stage="coverage"))


def test_blocker_active_requires_blocked_status(tmp_path):
    state = _empty(tmp_path)
    state["blockers"]["b-1"] = {
        "blocker_id": "b-1",
        "class": "tool-blocked",
        "reason": "r",
        "evidence_ids": [],
        "active": True,
        "opened_sequence": 1,
        "closed_sequence": None,
        "resolution_evidence_ids": [],
        "resolution_snapshot_epoch": None,
        "resolution_snapshot_fingerprint": None,
        "snapshot_epoch": 1,
        "snapshot_fingerprint": "f" * 64,
    }
    with pytest.raises(model.StateValidationError):
        model.validate_state(state)


def test_history_chain_must_verify(tmp_path):
    state = _empty(tmp_path, generation=1)
    rec = {
        "sequence": 1,
        "generation": 1,
        "event": "init",
        "snapshot_epoch": None,
        "snapshot_fingerprint": None,
        "data_sha256": "d" * 64,
        "previous_record_sha256": "1" * 64,  # wrong: should be zero head
        "record_sha256": "0" * 64,
    }
    rec["record_sha256"] = model.sha256_json(model.history_record_subject(rec))
    state["history"] = [rec]
    with pytest.raises(model.StateValidationError):
        model.validate_state(state)


def test_history_final_generation_must_equal_state(tmp_path):
    state = _empty(tmp_path, generation=2)
    rec = {
        "sequence": 1,
        "generation": 1,
        "event": "init",
        "snapshot_epoch": None,
        "snapshot_fingerprint": None,
        "data_sha256": "d" * 64,
        "previous_record_sha256": "0" * 64,
        "record_sha256": "0" * 64,
    }
    rec["record_sha256"] = model.sha256_json(model.history_record_subject(rec))
    state["history"] = [rec]
    with pytest.raises(model.StateValidationError):
        model.validate_state(state)


# --- normalize_consequences ---------------------------------------------------


def test_normalize_consequences_drops_none():
    assert model.normalize_consequences(("none", "security")) == ("security",)


def test_normalize_consequences_none_only_when_empty():
    assert model.normalize_consequences(("none",)) == ("none",)
    assert model.normalize_consequences(()) == ()


def test_normalize_consequences_union_order():
    out = model.normalize_consequences(("privacy",), ("security", "none"))
    assert out == ("security", "privacy")


def test_normalize_consequences_rejects_unknown():
    with pytest.raises(model.StateValidationError):
        model.normalize_consequences(("bogus",))


# --- construction order / fixed point -----------------------------------------


def test_construct_epoch_one_from_raw_witness_payloads_without_fixed_point(tmp_path):
    authorities = (
        {
            "authority_id": "auth-1",
            "kind": "repo-law",
            "locator": "AGENTS.md",
            "availability": "loaded",
            "sha256": "a" * 64,
            "failure_class": None,
            "failure_sha256": None,
        },
    )
    payload = model.manifest_payload(
        repository_id="o/r",
        pr_number=7,
        pr_url="https://github.com/o/r/pull/7",
        authority_discovery_policy_id="adp",
        authority_discovery_policy_version="1",
        authority_discovery_policy_sha256="0" * 64,
        authorities=authorities,
        feedback_history_policy_id="fhp",
        feedback_history_policy_version="1",
        feedback_history_policy_sha256="2" * 64,
        feedback_history_sha256="3" * 64,
        local_check_policy_id="lcp",
        local_check_policy_version="1",
        local_check_policy_sha256="4" * 64,
        required_check_policy_sha256="5" * 64,
        review_assignment_policy_id="rap",
        review_assignment_policy_version="1",
        review_assignment_policy_sha256="6" * 64,
        command_execution_policy_id="cep",
        command_execution_policy_version="1",
        command_execution_policy_sha256="7" * 64,
        evidence_ingestion_policy_id="eip",
        evidence_ingestion_policy_version="1",
        evidence_ingestion_policy_sha256="8" * 64,
        hypothesis_derivation_policy_id="hdp",
        hypothesis_derivation_policy_version="1",
        hypothesis_derivation_policy_sha256="9" * 64,
        unresolved_feedback_sha256="a" * 64,
    )
    manifest_id = model.authority_manifest_id(payload)
    assert manifest_id not in model.canonical_json(payload).decode()

    snap = _snapshot(authority_manifest_sha256=manifest_id)
    subject = model.snapshot_subject(**{k: v for k, v in snap.items() if k != "fingerprint"})
    assert "fingerprint" not in subject
    fp = model.snapshot_fingerprint(snap)
    assert fp == snap["fingerprint"]
    assert fp not in model.canonical_json(subject).decode()

    discovery = model.authority_discovery_subject(snap, payload)
    assert discovery["snapshot_fingerprint"] == fp
    assert discovery["authority_manifest_sha256"] == manifest_id
    # Fixed-point rule: the snapshot subject binds the manifest digest, but no
    # projection may contain its own digest or a later wrapper field.
    assert "authority_manifest_id" not in payload
    assert fp not in model.canonical_json(subject).decode()


# --- golden vectors ------------------------------------------------------------


FIXTURES = TESTS_DIR / "fixtures" / "review-v2-canonical-vectors.json"


def _vectors():
    raw = FIXTURES.read_bytes()
    doc = model.strict_json_loads(raw, source=str(FIXTURES))
    return doc["vectors"]


@pytest.mark.parametrize("vector", _vectors(), ids=lambda v: v["name"])
def test_canonical_projection_matches_golden_bytes(vector):
    proj = {
        "pr-metadata": lambda i: model.pr_metadata_subject(
            title=i["title"],
            body=i["body"],
            base_ref=i["base_ref"],
            scope_labels=tuple(i["scope_labels"]),
            declared_links=tuple(i["declared_links"]),
        ),
        "evidence-binding": lambda i: model.evidence_binding_subject(i),
        "finding-identity": lambda i: model.finding_identity_subject(i),
        "history-record": lambda i: model.history_record_subject(i),
        "obligation": lambda i: model.obligation_subject(i),
        "ready-intent": lambda i: model.ready_intent_subject(i),
        "ci-candidate": lambda i: model.ci_candidate_subject(i),
        "witness-record": lambda i: model.witness_record_subject(i),
    }[vector["name"]]
    subject = proj(vector["input"])
    assert model.canonical_json(subject).decode("utf-8") == vector["canonical"]
    digest = hashlib.sha256(model.canonical_json(subject)).hexdigest()
    expected = vector["digest"]
    if expected.startswith("evidence:"):
        assert "evidence:" + digest == expected
    elif expected.startswith("finding:"):
        assert "finding:" + digest == expected
    elif expected.startswith("obligation:"):
        assert "obligation:1:" + digest == expected
    elif expected.startswith("ci-candidate:"):
        assert "ci-candidate:1:" + digest == expected
    elif expected == "ready:1:k1":
        assert expected == "ready:1:k1"
    else:
        assert digest == expected


def test_every_included_projection_field_changes_digest():
    subj = {
        "content_id": "a" * 64,
        "kind": "authority",
        "snapshot_epoch": 1,
        "snapshot_fingerprint": "b" * 64,
    }
    base = model.sha256_json(model.evidence_binding_subject(subj))
    for field, mutated in (
        ("content_id", "c" * 64),
        ("kind", "snapshot"),
        ("snapshot_epoch", 2),
        ("snapshot_fingerprint", "d" * 64),
    ):
        changed = dict(subj, **{field: mutated})
        assert model.sha256_json(model.evidence_binding_subject(changed)) != base


def test_excluded_wrapper_fields_do_not_change_subject():
    epoch, fp = 1, "f" * 64
    rec = {
        "evidence_id": "evidence:x",
        "content_id": "a" * 64,
        "kind": "authority",
        "snapshot_epoch": epoch,
        "snapshot_fingerprint": fp,
    }
    base = model.canonical_json(model.evidence_binding_subject(rec))
    changed = dict(rec, evidence_id="evidence:other")
    assert model.canonical_json(model.evidence_binding_subject(changed)) == base


# --- schema parity --------------------------------------------------------------


SCHEMA_PATH = TESTS_DIR.parent / "references" / "review-state-v2.schema.json"


def _walk_schema_objects(node, path="$"):
    if isinstance(node, dict):
        if node.get("type") == "object":
            yield path, node
        for key, value in node.items():
            yield from _walk_schema_objects(value, f"{path}.{key}")
    elif isinstance(node, list):
        for i, item in enumerate(node):
            yield from _walk_schema_objects(item, f"{path}[{i}]")


@pytest.mark.skipif(not SCHEMA_PATH.exists(), reason="schema authored in Task 2 step 5")
def test_schema_closes_all_objects():
    schema = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
    for path, node in _walk_schema_objects(schema):
        ap = node.get("additionalProperties")
        # Records must be closed (false); maps must constrain values via a
        # subschema. A missing or permissive-true additionalProperties fails.
        assert ap is False or isinstance(ap, dict), f"{path} is not closed"


ENUM_PAIRS = {
    "STATUSES": model.STATUSES,
    "STAGES": model.STAGES,
    "SEVERITIES": model.SEVERITIES,
    "DISPOSITIONS": model.DISPOSITIONS,
    "OBLIGATION_STATUSES": model.OBLIGATION_STATUSES,
    "REVIEW_VERDICTS": model.REVIEW_VERDICTS,
    "WITNESS_KINDS": model.WITNESS_KINDS,
    "BLOCKER_CLASSES": model.BLOCKER_CLASSES,
    "EVIDENCE_KINDS": model.EVIDENCE_KINDS,
    "OBLIGATION_CATEGORIES": model.OBLIGATION_CATEGORIES,
    "OBLIGATION_RISKS": model.OBLIGATION_RISKS,
    "CONSEQUENCES": model.CONSEQUENCES,
    "SCOPE_LEVELS": model.SCOPE_LEVELS,
    "CAPABILITY_TIERS": model.CAPABILITY_TIERS,
    "REASONING_FLOORS": model.REASONING_FLOORS,
    "REVIEW_ROLES": model.REVIEW_ROLES,
    "CHECK_CONCLUSIONS": model.CHECK_CONCLUSIONS,
}


@pytest.mark.skipif(not SCHEMA_PATH.exists(), reason="schema authored in Task 2 step 5")
def test_schema_enums_match_python():
    schema = json.loads(SCHEMA_PATH.read_text(encoding="utf-8"))
    schema_enums = set()

    def walk(node):
        if isinstance(node, dict):
            if "enum" in node and isinstance(node["enum"], list):
                schema_enums.add(tuple(node["enum"]))
            for v in node.values():
                walk(v)
        elif isinstance(node, list):
            for v in node:
                walk(v)

    walk(schema)
    py_enums = set(ENUM_PAIRS.values())
    missing = py_enums - schema_enums
    assert not missing, f"python enums absent from schema: {missing}"
