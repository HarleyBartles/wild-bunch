#!/usr/bin/env python3
"""Policy tests for the version-2 fail-closed policy layer."""

from __future__ import annotations

import copy
import json
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR.parent / "scripts"))
sys.path.insert(0, str(TESTS_DIR))

from review_core import model  # noqa: E402
from review_core import policy  # noqa: E402
from review_v2_helpers import (  # noqa: E402
    _Walk,
    _bind,
    _enter_repair,
    _finding_payload,
    _put,
    _run_adjudicated,
    _verify_close_repair,
    _witness_bytes,
    make_complete_candidate,
    make_empty_v2_state,
    make_policy_bundle,
    make_remote_observation,
    walk_accept_risk_path,
    walk_false_positive_path,
    walk_fix_path,
    walk_happy_path,
    walk_review_repair_path,
)


def _bundle(state):
    return make_policy_bundle(state)


def _eval(state, policies=None):
    observation, now = make_remote_observation(state)
    return policy.evaluate_green(
        state,
        observation,
        policies=policies if policies is not None else _bundle(state),
        now=now,
    )


def _complete_state(tmp_path):
    return make_complete_candidate(tmp_path)


# ---------------------------------------------------------------------------
# Transition table: the clean happy path, derived from actual records


def test_next_action_intake_requires_freeze(tmp_path):
    state = make_empty_v2_state(tmp_path)
    decision = policy.next_action(state, policies=_try_bundle(state))
    assert decision.action == "freeze-review-input"
    assert decision.allowed


def _try_bundle(state):
    try:
        return make_policy_bundle(state)
    except KeyError:
        return None


def test_transition_table_complete_happy_path(tmp_path):
    """The complete candidate must derive green-candidate and offer seal-green."""
    state = _complete_state(tmp_path)
    assert policy._derived_stage(state, _bundle(state)) == "green-candidate"
    decision = policy.next_action(state, policies=_bundle(state))
    assert decision.action == "seal-green"


def test_next_action_after_freeze_is_semantic_map(tmp_path):
    state = _complete_state(tmp_path)
    # rewind: remove everything downstream of freeze
    fresh = make_empty_v2_state(tmp_path)
    fresh["snapshot"] = state["snapshot"]
    fresh["authority_manifest"] = state["authority_manifest"]
    fresh["authorities"] = state["authorities"]
    fresh["content_objects"] = state["content_objects"]
    fresh["evidence"] = state["evidence"]
    fresh["witness_records"] = {k: w for k, w in state["witness_records"].items() if w["kind"] == "authority-discovery"}
    d = policy.next_action(fresh, policies=_bundle(state))
    assert d.action == "map-impact-semantic"


def test_authorities_complete_detects_swapped_evidence_content(tmp_path):
    # A file swapped between _load_dir verification and store registration
    # leaves the recorded sha256 honest but the registered content tampered;
    # the cross-check must fail closed.
    state = _complete_state(tmp_path)
    rec = state["authorities"]["auth-agents"]
    bad_cid = _put(state, {"path": "AGENTS.md", "note": "tampered"})
    rec["evidence_id"] = _bind(state, bad_cid, "authority")
    ok, missing = policy.authorities_complete(state, _bundle(state))
    assert not ok


def test_authorities_complete_detects_dangling_evidence(tmp_path):
    # An evidence_id that resolves to nothing must fail, not silently pass
    # on record-vs-manifest agreement alone.
    state = _complete_state(tmp_path)
    rec = state["authorities"]["auth-agents"]
    rec["evidence_id"] = "evidence:nonexistent"
    ok, missing = policy.authorities_complete(state, _bundle(state))
    assert not ok


def _add_blocker(state, blocker_id="b-1", active=True):
    snap = state["snapshot"]
    state["blockers"][blocker_id] = {
        "blocker_id": blocker_id,
        "class": "missing-capability",
        "reason": "test blocker",
        "evidence_ids": [],
        "active": active,
        "opened_sequence": len(state["history"]) + 1,
        "closed_sequence": None,
        "resolution_evidence_ids": [],
        "resolution_snapshot_epoch": None,
        "resolution_snapshot_fingerprint": None,
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }


def test_derived_stage_blocked_when_blockers_active(tmp_path):
    state = _complete_state(tmp_path)
    _add_blocker(state)
    state["status"] = "blocked"
    assert policy._derived_stage(state, _bundle(state)) == "blocked"


def test_derived_stage_review_repair_when_open_repair(tmp_path):
    state = _complete_state(tmp_path)
    rep = next(iter(state["review_repairs"].values()))
    rep["status"] = "repairing"
    assert policy._derived_stage(state, _bundle(state)) == "review-repair"


def test_derived_stage_fixing_when_fix_pending(tmp_path):
    state = _complete_state(tmp_path)
    f = next(f for f in state["findings"].values() if f["disposition"] == "fixed")
    del state["checks"][f["resolution"]["check_id"]]
    assert policy._derived_stage(state, _bundle(state)) == "resolution"


# ---------------------------------------------------------------------------
# Green predicate


def test_evaluate_green_seals_complete_candidate(tmp_path):
    state = _complete_state(tmp_path)
    d = _eval(state)
    assert d.allowed, d
    assert d.action == "seal-green"
    sealed = policy.complete_action(state, action="seal-green", raw_data=b"{}", policies=_bundle(state))
    seal = sealed["green_seal"]
    assert seal is not None
    assert seal["snapshot_fingerprint"] == state["snapshot"]["fingerprint"]
    assert seal["snapshot_epoch"] == state["snapshot"]["epoch"]


def test_evaluate_green_missing_is_explicit(tmp_path):
    state = _complete_state(tmp_path)
    obs, now = make_remote_observation(state)
    state["impact_maps"] = {}
    d = policy.evaluate_green(state, obs, policies=_bundle(state), now=now)
    assert not d.allowed
    assert "impact-map" in d.missing


def test_evaluate_green_blocks_during_blockers(tmp_path):
    state = _complete_state(tmp_path)
    _add_blocker(state)
    d = _eval(state)
    assert not d.allowed


def test_evaluate_green_rejects_stale_observation(tmp_path):
    state = _complete_state(tmp_path)
    obs, now = make_remote_observation(state)
    obs["observed_at"] = (now - timedelta(seconds=61)).isoformat()
    d = policy.evaluate_green(state, obs, policies=_bundle(state), now=now)
    assert not d.allowed
    assert "remote-head-identity" in d.missing or "presentation-recheck" in d.missing or "remote-ci" in d.missing


def test_evaluate_green_rejects_future_observation(tmp_path):
    state = _complete_state(tmp_path)
    obs, now = make_remote_observation(state)
    obs["observed_at"] = (now + timedelta(seconds=6)).isoformat()
    d = policy.evaluate_green(state, obs, policies=_bundle(state), now=now)
    assert not d.allowed


def test_evaluate_green_rejects_head_mismatch(tmp_path):
    state = _complete_state(tmp_path)
    obs, now = make_remote_observation(state)
    obs["head_sha"] = "0" * 40
    d = policy.evaluate_green(state, obs, policies=_bundle(state), now=now)
    assert not d.allowed
    assert "remote-head-identity" in d.missing


def test_evaluate_green_rejects_missing_check_in_observation(tmp_path):
    state = _complete_state(tmp_path)
    obs, now = make_remote_observation(state)
    obs["check_runs"] = []
    obs["workflow_runs"] = []
    d = policy.evaluate_green(state, obs, policies=_bundle(state), now=now)
    assert not d.allowed


def test_evaluate_green_rejects_failed_remote_check(tmp_path):
    state = _complete_state(tmp_path)
    for c in state["checks"].values():
        if c["kind"] == "remote-ci":
            c["conclusion"] = "failure"
    d = _eval(state)
    assert not d.allowed
    assert "remote-ci" in d.missing


def test_evaluate_green_rejects_no_available_profile(tmp_path):
    state = _complete_state(tmp_path)
    policies = _bundle(state)
    policies.available_profiles = ()
    d = _eval(state, policies)
    assert not d.allowed
    assert "reasoning-floor" in d.missing


def test_evaluate_green_rejects_reviewed_head_discovery_policy(tmp_path):
    state = _complete_state(tmp_path)
    policies = _bundle(state)
    policies.discovery_policy_origin = "reviewed-head"
    d = _eval(state, policies)
    assert not d.allowed


def test_evaluate_green_rejects_tampered_witness(tmp_path):
    state = _complete_state(tmp_path)
    for w in state["witness_records"].values():
        if w["kind"] == "review-launch":
            w["tool_use_id"] = "toolu-forged"
            break
    d = _eval(state)
    assert not d.allowed
    assert "witnesses" in d.missing


def test_evaluate_green_rejects_wrong_source_locator(tmp_path):
    state = _complete_state(tmp_path)
    for w in state["witness_records"].values():
        w["source_locator"] = "local-file:" + w["source_locator"]
    d = _eval(state)
    assert not d.allowed
    assert "witnesses" in d.missing


# ---------------------------------------------------------------------------
# Obligation floors


def test_obligation_floor_table():
    assert policy.obligation_floor("hunk", "low", ("none",)) == ("fast", "low")
    assert policy.obligation_floor("file", "low", ("none",)) == ("fast", "low")
    assert policy.obligation_floor("surface", "low", ("none",)) == ("focused", "standard")
    assert policy.obligation_floor("cross-surface", "low", ("none",)) == ("strong", "high")
    assert policy.obligation_floor("whole-pr", "low", ("none",)) == ("final-strong", "final-strong")
    assert policy.obligation_floor("hunk", "medium", ("none",)) == ("focused", "standard")
    assert policy.obligation_floor("hunk", "high", ("none",)) == ("strong", "high")
    for consequence in (
        "security",
        "authorization",
        "privacy",
        "secrets",
        "irreversible-data-loss",
        "concurrency-recovery",
        "migration-rollback",
        "public-compatibility",
        "source-custody",
    ):
        tier, reasoning = policy.obligation_floor("hunk", "low", (consequence,))
        assert tier == "strong"
        assert reasoning == "high"


def test_role_floors():
    assert policy._role_floor("scope-challenger") == ("final-strong", "final-strong")
    assert policy._role_floor("exemption-challenger") == ("final-strong", "final-strong")
    assert policy._role_floor("blind-final") == ("final-strong", "final-strong")
    assert policy._role_floor("closure-auditor") == ("final-strong", "final-strong")
    assert policy._role_floor("impact-mapper-semantic") == ("strong", "high")
    assert policy._role_floor("finding-adjudicator") == ("strong", "high")
    # obligation-reviewer has no portable role floor; the obligation's own
    # floor dominates, so the identity element keeps the tier honest.
    assert policy._role_floor("obligation-reviewer") == ("fast", "low")
    assert policy._role_floor("fix-reviewer") == ("focused", "standard")


def test_dispatch_and_completion_subject_binding(tmp_path):
    state = _complete_state(tmp_path)
    # tamper with the recorded task digest of a dispatch: witness verification must fail
    d = next(iter(state["dispatches"].values()))
    d["instruction_manifest_sha256"] = "0" * 64
    decision = _eval(state)
    assert not decision.allowed
    assert "witnesses" in decision.missing


# ---------------------------------------------------------------------------
# Payload allowlists


def test_register_dispatch_rejects_unknown_keys(tmp_path):
    state = _complete_state(tmp_path)
    dispatch = next(iter(state["dispatches"].values()))
    bogus = dict(dispatch)
    bogus["dispatch_id"] = ""
    bogus["bogus_key"] = 1
    with pytest.raises(model.StateValidationError):
        policy.register_dispatch(
            state,
            route_selection=state["route_selections"][dispatch["route_selection_id"]],
            dispatch=bogus,
            policies=_bundle(state),
        )


def test_action_payload_keys_documented():
    """Every action name in the transition table has a payload key allowlist."""
    for action in (
        "freeze-review-input",
        "refresh-review-input",
        "map-impact-semantic",
        "map-impact-contract",
        "plan-coverage",
        "challenge-coverage",
        "run-preflight",
        "run-fast-review",
        "run-focused-review",
        "run-strong-review",
        "run-exemption-challenge",
        "adjudicate-findings",
        "close-false-positive",
        "enter-fixing",
        "run-fix-verification",
        "review-fix",
        "close-fixed",
        "enter-review-repair",
        "verify-review-repair",
        "close-review-repaired",
        "accept-risk",
        "resume-review",
        "run-final-review",
        "run-closure-audit",
        "mark-ready-for-ci",
        "run-remote-ci",
        "seal-green",
    ):
        assert action in policy.ACTION_PAYLOAD_KEYS, action


EXPECTED_HAPPY_PATH = [
    "freeze-review-input",
    "map-impact-semantic",
    "map-impact-contract",
    "plan-coverage",
    "challenge-coverage",
    "run-exemption-challenge",
    "run-preflight",
    "run-fast-review",
    "run-focused-review",
    "run-strong-review",
    "run-final-review",
    "run-closure-audit",
    "mark-ready-for-ci",
    "run-remote-ci",
    "seal-green",
]


def test_complete_action_happy_path_walk(tmp_path):
    """Drive the full clean action order through complete_action.

    Every step's action must be the one next_action proposes, the resulting
    state must validate, and the sealed candidate must evaluate green.
    """
    state, bundle, registry, steps = walk_happy_path(tmp_path)
    assert [s[0] for s in steps] == EXPECTED_HAPPY_PATH
    assert [s[1] for s in steps] == EXPECTED_HAPPY_PATH
    assert [s[2] for s in steps][-1] == "green-candidate"
    assert state["green_seal"] is not None
    obs, now = make_remote_observation(state)
    decision = policy.evaluate_green(state, obs, policies=bundle, now=now)
    assert decision.allowed, decision.missing
    assert decision.action == "seal-green"


def test_complete_action_false_positive_path(tmp_path):
    """A finding adjudicated false-positive closes through
    close-false-positive with counter-evidence, then the review seals."""
    state, bundle, registry, steps = walk_false_positive_path(tmp_path)
    actions = [s[0] for s in steps]
    assert (
        actions
        == EXPECTED_HAPPY_PATH[:10]
        + [
            "adjudicate-findings",
            "close-false-positive",
        ]
        + EXPECTED_HAPPY_PATH[10:]
    )
    assert state["green_seal"] is not None
    obs, now = make_remote_observation(state)
    assert policy.evaluate_green(state, obs, policies=bundle, now=now).allowed


def test_complete_action_accept_risk_path(tmp_path):
    """accept-risk closes the finding but leaves the review
    reviewed-with-exceptions; seal-green is never lawful."""
    state, bundle, registry, steps = walk_accept_risk_path(tmp_path)
    assert state["status"] == "reviewed-with-exceptions"
    assert state["green_seal"] is None
    decision = policy.next_action(state, policies=bundle)
    assert decision.action != "seal-green"
    lawful = policy._lawful_actions(state, bundle)
    assert "seal-green" not in lawful
    verdict = policy.evaluate_green(
        state,
        None,
        policies=bundle,
        now=datetime.now(timezone.utc),
    )
    assert not verdict.allowed


def test_complete_action_accept_risk_partial_keeps_status_active(tmp_path):
    """accept-risk on one finding while another stays open must not claim
    reviewed-with-exceptions: the status only advances once every finding
    sits in a closed disposition."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f = w.reported_finding
    _run_adjudicated(w, f["finding_id"], outcome="confirmed", remediation_class="candidate-change")
    second = _finding_payload(
        w.state,
        source_kind="review",
        source_id=f["source_id"],
        source_assignment_id=f["source_assignment_id"],
        obligation_id=f["obligation_id"],
        severity="minor",
        title="walk-finding-2",
    )
    w.state["findings"][second["finding_id"]] = second
    _run_adjudicated(w, second["finding_id"], outcome="confirmed", remediation_class="candidate-change")
    hd = _witness_bytes(
        w.state,
        w.registry,
        kind="human-decision",
        subject={"transcript_range": {"stream": "human", "start": 1, "end": 2}},
        tool_use_id=f"toolu-human-{w._next_serial()}",
        transcript_range={"stream": "human", "start": 1, "end": 2},
    )
    w.state = policy.record_witness(w.state, witness_bytes=hd, kind="human-decision", policies=w.policies)
    wid = model.strict_json_loads(hd, source="witness")["witness_id"]
    w.run(
        "accept-risk",
        {"resolutions": [{"finding_id": f["finding_id"], "human_decision_witness_id": wid}]},
        sole=False,
    )
    assert w.state["findings"][f["finding_id"]]["disposition"] == "accepted-risk"
    assert w.state["findings"][second["finding_id"]]["disposition"] == "open"
    assert w.state["status"] == "active"


def test_complete_action_review_repair_path(tmp_path):
    """A review-process finding enters repair; the invalidation cut retires
    the targeted review and downstream proof, the re-ascent re-proves the
    review gate, and an independent verifier closes the repair before the
    remote gates."""
    state, bundle, registry, steps = walk_review_repair_path(tmp_path)
    actions = [s[0] for s in steps]
    repair_at = actions.index("enter-review-repair")
    assert actions[repair_at + 1] == "run-focused-review"
    assert "verify-review-repair" in actions
    assert actions.index("verify-review-repair") > actions.index("run-focused-review", repair_at)
    assert "close-review-repaired" in actions
    assert state["green_seal"] is not None
    rep = state["review_repairs"]["repair-1"]
    assert rep["status"] == "closed"
    assert rep["verification_attestation_id"]
    obs, now = make_remote_observation(state)
    assert policy.evaluate_green(state, obs, policies=bundle, now=now).allowed


def test_complete_action_fix_path(tmp_path):
    """A candidate-change finding advances the epoch; the whole ascent
    re-proves at epoch 2 before the fix lifecycle and remote gates."""
    state, bundle, registry, steps = walk_fix_path(tmp_path)
    actions = [s[0] for s in steps]
    assert state["snapshot"]["epoch"] == 2
    fix_at = actions.index("enter-fixing")
    # epoch-2 ascent re-runs in order after the fix begins
    tail = actions[fix_at + 1 :]
    for re_action in (
        "map-impact-semantic",
        "map-impact-contract",
        "plan-coverage",
        "challenge-coverage",
        "run-exemption-challenge",
        "run-preflight",
        "run-fast-review",
        "run-focused-review",
        "run-strong-review",
    ):
        assert re_action in tail, re_action
    assert tail.index("run-fix-verification") > tail.index("run-strong-review")
    assert tail.index("review-fix") > tail.index("run-fix-verification")
    assert tail.index("close-fixed") > tail.index("review-fix")
    assert state["green_seal"] is not None
    obs, now = make_remote_observation(state)
    assert policy.evaluate_green(state, obs, policies=bundle, now=now).allowed


def test_adjudication_branches_are_exclusive(tmp_path):
    """candidate-change adjudication does not authorize the review-process
    branch, and contested authorizes none."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f = w.reported_finding
    _run_adjudicated(w, f["finding_id"], outcome="confirmed", remediation_class="candidate-change")
    # enter-review-repair is not a lawful branch for candidate-change
    with pytest.raises(model.StateValidationError):
        policy.complete_action(
            w.state,
            action="enter-review-repair",
            raw_data=model.canonical_json(
                {
                    "resolutions": [
                        {
                            "finding_id": f["finding_id"],
                            "repair_id": "repair-1",
                            "target_kind": "blind-final",
                            "target_ids": ["x"],
                        }
                    ]
                }
            ),
            policies=w.policies,
        )
    # a second adjudication on the same finding is also rejected
    with pytest.raises(model.StateValidationError):
        policy.complete_action(
            w.state,
            action="adjudicate-findings",
            raw_data=model.canonical_json({"attestations": [], "findings": []}),
            policies=w.policies,
        )


def test_contested_outcome_blocks_all_branches(tmp_path):
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f = w.reported_finding
    _run_adjudicated(w, f["finding_id"], outcome="contested")
    decision = policy.next_action(w.state, policies=w.policies)
    assert not decision.allowed
    lawful = policy._lawful_actions(w.state, w.policies)
    for action in (
        "close-false-positive",
        "enter-fixing",
        "enter-review-repair",
        "accept-risk",
    ):
        assert action not in lawful


def test_forged_stage_cannot_skip_actions(tmp_path):
    """The stored stage is display-only: forging it cannot skip gates."""
    w = _Walk(tmp_path)
    w.freeze()
    w.state["stage"] = "green-candidate"
    decision = policy.next_action(w.state, policies=w.policies)
    assert decision.action == "map-impact-semantic"
    with pytest.raises(model.StateValidationError):
        policy.complete_action(
            w.state,
            action="seal-green",
            raw_data=model.canonical_json({}),
            policies=w.policies,
        )


def test_skipping_ahead_is_rejected(tmp_path):
    """An action whose gate is not yet satisfied is rejected mid-walk."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent()
    # Remote CI cannot run before the ready transition exists.
    with pytest.raises(model.StateValidationError):
        policy.complete_action(
            w.state,
            action="run-remote-ci",
            raw_data=model.canonical_json({"remote_observation": {}, "checks": []}),
            policies=w.policies,
        )
    with pytest.raises(model.StateValidationError):
        policy.complete_action(
            w.state,
            action="seal-green",
            raw_data=model.canonical_json({}),
            policies=w.policies,
        )


def test_refresh_review_input_advances_one_epoch(tmp_path):
    """Trusted drift advances exactly one epoch and clears downstream
    green-path records without touching finding state."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent()
    intake = w.intake(epoch=2, head_sha="9" * 40)
    w.run(
        "refresh-review-input",
        dict(intake, drift_reasons=["remote head moved"]),
        sole=False,
    )
    assert w.state["snapshot"]["epoch"] == 2
    assert w.state["snapshot"]["head_sha"] == "9" * 40
    assert w.state["coverage_inventory"] is None
    assert w.state["green_seal"] is None
    assert w.state["ready_transition"] is None


def test_refresh_rejects_identical_snapshot(tmp_path):
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent()
    intake = w.intake(epoch=2, head_sha="b" * 40)  # same head as epoch 1
    # epoch 2 with identical subject fields is not drift
    with pytest.raises(model.StateValidationError):
        policy.complete_action(
            w.state,
            action="refresh-review-input",
            raw_data=model.canonical_json(
                dict(
                    intake,
                    snapshot={**intake["snapshot"], "epoch": 1},
                    drift_reasons=["no real drift"],
                )
            ),
            policies=w.policies,
        )


def test_block_and_resume_review(tmp_path):
    """An active blocker makes resume-review the only lawful action; resume
    requires non-empty current resolution evidence."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent()
    ev0 = _bind(w.state, _put(w.state, {"why": "clarify"}), "finding-proof")
    w.state = policy.block_review(
        w.state,
        blocker_id="blocker-1",
        blocker_class="feedback-unresolved",
        reason="planner needs clarification",
        evidence_ids=[ev0],
    )
    decision = policy.next_action(w.state, policies=w.policies)
    assert decision.action == "resume-review"
    assert policy._lawful_actions(w.state, w.policies) == frozenset({"resume-review"})
    blocker_id = next(bid for bid, b in w.state["blockers"].items() if b["active"])
    ev = _bind(w.state, _put(w.state, {"resolution": blocker_id}), "finding-proof")
    w.state = policy.resume_review(
        w.state,
        blocker_id=blocker_id,
        resolution_evidence_ids=[ev],
        policies=w.policies,
    )
    assert w.state["status"] == "active"
    assert not any(b["active"] for b in w.state["blockers"].values())
    assert policy.next_action(w.state, policies=w.policies).action in (
        "run-final-review",
        "seal-green",
        "mark-ready-for-ci",
        "run-remote-ci",
    )


def test_epoch_mismatched_evidence_rejected(tmp_path):
    """Evidence bound to a different epoch does not satisfy the current
    snapshot: a freeze payload whose authority evidence is epoch-2-bound
    while the snapshot is epoch-1 must fail."""
    w = _Walk(tmp_path)
    data = w.intake(epoch=1, head_sha="b" * 40)
    # Tamper the wrapper's epoch so the manifest binds the wrong epoch.
    data["authority_manifest"] = {
        **data["authority_manifest"],
        "snapshot_epoch": 2,
    }
    with pytest.raises(model.StateValidationError):
        policy.complete_action(
            w.state,
            action="freeze-review-input",
            raw_data=model.canonical_json(data),
            policies=w.policies,
        )


def test_tier_gate_requires_witnessed_attestation(tmp_path):
    """A covered status alone must not satisfy a tier gate."""
    state = _complete_state(tmp_path)
    bundle = _bundle(state)
    for rid in list(state["reviews"]):
        d = state["dispatches"][state["reviews"][rid]["dispatch_id"]]
        if state["route_selections"][d["route_selection_id"]]["required_role"] == "obligation-reviewer":
            del state["reviews"][rid]
    assert not policy.fast_reviews_complete(state, bundle)[0]
    assert not policy.focused_reviews_complete(state, bundle)[0]
    assert not policy.strong_reviews_complete(state, bundle)[0]


def test_high_risk_covered_obligation_needs_two_distinct_reviews(tmp_path):
    """One reviewer attestation cannot satisfy a covered high-risk obligation."""
    state = _complete_state(tmp_path)
    bundle = _bundle(state)
    high = next(o for o in state["obligations"].values() if o["risk"] == "high")
    high["status"] = "covered"
    high["not_applicable_attestation_ids"] = []
    # Remove all but one obligation-reviewer attestation covering it.
    keep = None
    for rid in list(state["reviews"]):
        r = state["reviews"][rid]
        d = state["dispatches"][r["dispatch_id"]]
        if (
            state["route_selections"][d["route_selection_id"]]["required_role"] == "obligation-reviewer"
            and high["obligation_id"] in r["assignment_ids"]
        ):
            if keep is None:
                keep = rid
            else:
                del state["reviews"][rid]
    assert keep is not None
    assert not policy.strong_reviews_complete(state, bundle)[0]


def test_high_risk_obligation_requires_opposite_polarity_pair(tmp_path):
    state = _complete_state(tmp_path)
    bundle = _bundle(state)
    high = next(o for o in state["obligations"].values() if o["risk"] == "high")
    for aid in list(high["assignees"]):
        h = state["hypothesis_assignments"].get(aid)
        if h is not None and h["polarity"] == "counterexample":
            del state["hypothesis_assignments"][aid]
            high["assignees"] = [a for a in high["assignees"] if a != aid]
    ok, reasons = policy.coverage_complete(state, bundle)
    assert not ok
    assert "coverage" in reasons


def test_complete_action_rejects_unknown_payload_key(tmp_path):
    state, bundle, registry, steps = walk_happy_path(tmp_path)
    # seal-green declares no caller keys; an extra key must fail closed.
    with pytest.raises(model.StateValidationError):
        policy.complete_action(
            state,
            action="seal-green",
            raw_data=model.canonical_json({"bogus": 1}),
            policies=bundle,
        )


def test_complete_action_rejects_unlawful_order(tmp_path):
    """seal-green before the remote-CI gate must be an unlawful transition."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent()
    with pytest.raises(model.StateValidationError):
        policy.complete_action(
            w.state,
            action="seal-green",
            raw_data=model.canonical_json({}),
            policies=w.policies,
        )


# ---------------------------------------------------------------------------
# Idempotent replay: identical content is a no-op, different content conflicts


def test_complete_action_identical_replay_is_noop(tmp_path):
    w = _Walk(tmp_path)
    w.freeze()
    before = w.state
    again = policy.complete_action(before, action=w.last_action, raw_data=w.last_raw, policies=w.policies)
    assert again == before
    assert len(again["history"]) == len(before["history"])


def test_complete_action_replay_tolerates_key_order(tmp_path):
    """The replay digest is over canonical payload bytes, so a semantically
    identical payload with reordered keys is still a no-op."""
    w = _Walk(tmp_path)
    w.freeze()
    data = json.loads(w.last_raw)
    reordered = dict(reversed(list(data.items())))
    again = policy.complete_action(
        w.state,
        action=w.last_action,
        raw_data=json.dumps(reordered).encode(),
        policies=w.policies,
    )
    assert again == w.state


def test_complete_action_conflicting_replay_rejected(tmp_path):
    w = _Walk(tmp_path)
    w.freeze()
    data = json.loads(w.last_raw)
    data["snapshot"]["head_sha"] = "c" * 40
    with pytest.raises(model.StateValidationError) as exc:
        policy.complete_action(
            w.state,
            action=w.last_action,
            raw_data=model.canonical_json(data),
            policies=w.policies,
        )
    assert exc.value.code == "conflicting-replay"
    assert len(w.state["history"]) == 1


# ---------------------------------------------------------------------------
# Exemption challenge outcomes


def test_exemption_applicable_returns_to_coverage(tmp_path):
    """An applicable outcome rejects the exemption: the obligation goes back
    to pending, the earliest incomplete obligation predicate is the coverage
    challenge, and the re-covered high-risk obligation needs two distinct
    strong reviews before green."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent_to_exemption()
    oid = w.exemption("applicable")
    o = w.state["obligations"][oid]
    assert o["status"] == "pending"
    decision = policy.next_action(w.state, policies=w.policies)
    assert decision.action == "challenge-coverage"
    w.challenge(na=False)
    w.ascent_after_exemption()
    # high-risk covered obligation: second distinct profile contract
    w.review(
        "run-strong-review",
        role="obligation-reviewer",
        profile="reviewer-b",
        tier="strong",
        reasoning="high",
        assignments=[oid],
    )
    w.remote()
    obs, now = make_remote_observation(w.state)
    assert policy.evaluate_green(w.state, obs, policies=w.policies, now=now).allowed


def test_exemption_incomplete_blocks_then_resumes(tmp_path):
    """An incomplete outcome opens a typed blocker; resume-review runs
    through the complete_action funnel and the exemption re-challenges."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent_to_exemption()
    w.exemption("incomplete")
    assert w.state["status"] == "blocked"
    actives = [b for b in w.state["blockers"].values() if b["active"]]
    assert len(actives) == 1
    blocker = actives[0]
    assert blocker["class"] == "incomplete-review"
    assert policy._lawful_actions(w.state, w.policies) == frozenset({"resume-review"})
    w.run(
        "resume-review",
        {
            "blocker_id": blocker["blocker_id"],
            "resolution_evidence_ids": blocker["evidence_ids"],
        },
    )
    assert w.state["status"] == "active"
    assert not any(b["active"] for b in w.state["blockers"].values())
    # The obligation is still not-applicable; the exemption re-challenges.
    w.exemption("not-applicable-confirmed")
    w.ascent_after_exemption()
    w.remote()
    obs, now = make_remote_observation(w.state)
    assert policy.evaluate_green(w.state, obs, policies=w.policies, now=now).allowed


# ---------------------------------------------------------------------------
# Blockers


def test_resume_rejects_unknown_blocker(tmp_path):
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent()
    ev = _bind(w.state, _put(w.state, {"r": 1}), "finding-proof")
    with pytest.raises(model.StateValidationError) as exc:
        policy.resume_review(
            w.state,
            blocker_id="no-such-blocker",
            resolution_evidence_ids=[ev],
            policies=w.policies,
        )
    assert exc.value.code == "dangling-ref"


def test_block_review_rejects_second_active_blocker(tmp_path):
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent()
    ev = _bind(w.state, _put(w.state, {"x": 1}), "finding-proof")
    w.state = policy.block_review(
        w.state,
        blocker_id="blocker-1",
        blocker_class="feedback-unresolved",
        reason="first",
        evidence_ids=[ev],
    )
    with pytest.raises(model.StateValidationError) as exc:
        policy.block_review(
            w.state,
            blocker_id="blocker-2",
            blocker_class="coverage-gap",
            reason="second",
            evidence_ids=[ev],
        )
    assert exc.value.code == "multi-blocker"


def test_resume_review_rejects_bad_inputs(tmp_path):
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent()
    ev = _bind(w.state, _put(w.state, {"x": 1}), "finding-proof")
    w.state = policy.block_review(
        w.state,
        blocker_id="blocker-1",
        blocker_class="feedback-unresolved",
        reason="needs clarification",
        evidence_ids=[ev],
    )
    with pytest.raises(model.StateValidationError):
        policy.resume_review(
            w.state,
            blocker_id="wrong-id",
            resolution_evidence_ids=[ev],
            policies=w.policies,
        )
    with pytest.raises(model.StateValidationError):
        policy.resume_review(
            w.state,
            blocker_id="blocker-1",
            resolution_evidence_ids=[],
            policies=w.policies,
        )
    with pytest.raises(model.StateValidationError):
        policy.resume_review(
            w.state,
            blocker_id="blocker-1",
            resolution_evidence_ids=["evidence:bogus"],
            policies=w.policies,
        )
    # Stale (wrong-epoch) resolution evidence: probe on a throwaway copy so
    # the out-of-epoch record does not poison the live state.
    probe = copy.deepcopy(w.state)
    stale = _bind(
        probe,
        _put(probe, {"old": True}),
        "finding-proof",
        snap={"epoch": 99, "fingerprint": "f" * 64},
    )
    with pytest.raises(model.StateValidationError) as exc:
        policy.resume_review(
            probe,
            blocker_id="blocker-1",
            resolution_evidence_ids=[stale],
            policies=w.policies,
        )
    assert exc.value.code == "stale"
    w.state = policy.resume_review(
        w.state,
        blocker_id="blocker-1",
        resolution_evidence_ids=[ev],
        policies=w.policies,
    )
    assert w.state["status"] == "active"


# ---------------------------------------------------------------------------
# Repair cuts: the closed target-kind table


def _repair_finding(w):
    """Report a finding at the strong gate and adjudicate it
    confirmed/review-process; returns the finding record."""
    _run_adjudicated(
        w,
        w.reported_finding["finding_id"],
        outcome="confirmed",
        remediation_class="review-process",
    )
    return w.reported_finding


def _role_attestation(state, role):
    return next(
        r["attestation_id"]
        for r in state["reviews"].values()
        if policy._role_of_dispatch(state, state["dispatches"][r["dispatch_id"]]) == role
    )


@pytest.mark.parametrize(
    "target_kind,action,role,profile,category,kept_hazard",
    [
        (
            "semantic-impact",
            "map-impact-semantic",
            "impact-mapper-semantic",
            "mapper-semantic",
            "behavioral-correctness",
            "h-con",
        ),
        (
            "contract-impact",
            "map-impact-contract",
            "impact-mapper-contract",
            "mapper-contract",
            "documentation-contract",
            "h-sem",
        ),
    ],
)
def test_repair_impact_map_row(tmp_path, target_kind, action, role, profile, category, kept_hazard):
    """An impact-map repair cuts the named map, inventory, obligations,
    hypotheses, preflight and downstream: the ascent re-proves from that map
    with fresh replacement ids."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f = _repair_finding(w)
    map_id = next(m["impact_map_id"] for m in w.state["impact_maps"].values() if m["role"] == role)
    _enter_repair(w, f, target_kind=target_kind, target_ids=[map_id])
    # Only the named map was cut; re-run that gate then re-plan coverage.
    # The replacement map differs in hazards; the replacement obligations
    # carry an extra surface so their subjects (and derived ids) are fresh.
    w.map_impact(
        action,
        role=role,
        profile=profile,
        entries=[
            {
                "surface": "src/foo.py",
                "category": category,
                "hazards": ["h-repaired"],
                "consequences": ["security"],
            }
        ],
    )
    w.plan(surfaces=["src/foo.py", "src/foo2.py"])
    w.challenge(
        na=True,
        surface="src/foo.py",
        surfaces=["src/foo.py", "src/foo2.py"],
        hazards=["h-repaired", kept_hazard],
    )
    w.exemption()
    w.ascent_after_exemption()
    _verify_close_repair(w, f)
    w.remote()
    obs, now = make_remote_observation(w.state)
    assert policy.evaluate_green(w.state, obs, policies=w.policies, now=now).allowed


@pytest.mark.parametrize("target_kind", ["coverage-plan", "coverage-challenge"])
def test_repair_coverage_row(tmp_path, target_kind):
    """A coverage repair cuts the inventory, challenger, obligations,
    hypotheses and preflight; the maps survive, so the re-ascent restarts at
    plan-coverage."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f = _repair_finding(w)
    inv_id = w.state["coverage_inventory"]["coverage_inventory_id"]
    _enter_repair(w, f, target_kind=target_kind, target_ids=[inv_id])
    # Replacement obligations need fresh subjects: an extra surface keeps
    # coverage over the map union while deriving new ids.
    w.plan(surfaces=["src/foo.py", "src/foo2.py"])
    w.challenge(na=True, surfaces=["src/foo.py", "src/foo2.py"])
    w.exemption()
    w.ascent_after_exemption()
    _verify_close_repair(w, f)
    w.remote()
    obs, now = make_remote_observation(w.state)
    assert policy.evaluate_green(w.state, obs, policies=w.policies, now=now).allowed


def test_repair_exemption_review_row(tmp_path):
    """An exemption repair cuts the challenger attestation, the affected
    not-applicable backing and the obligation; coverage re-proves only the
    affected category and the exemption re-challenges."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f = _repair_finding(w)
    ex_att = _role_attestation(w.state, "exemption-challenger")
    _enter_repair(w, f, target_kind="exemption-review", target_ids=[ex_att])
    # Only the affected category lost its obligation, and it sits outside the
    # map union, so coverage re-challenges that category alone; unrelated
    # current obligations (and the tier reviews bound to them) stay intact.
    w.challenge(
        na=True,
        categories=["security-privacy"],
        surfaces=["src/foo.py", "src/foo2.py"],
    )
    w.exemption()
    _verify_close_repair(w, f)
    w.remote()
    obs, now = make_remote_observation(w.state)
    assert policy.evaluate_green(w.state, obs, policies=w.policies, now=now).allowed


def test_repair_finding_adjudication_row(tmp_path):
    """Cutting an adjudication attestation reopens the finding it resolved:
    it must be re-adjudicated and re-closed before the repair verifies."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f1 = w.reported_finding
    adj1 = _run_adjudicated(w, f1["finding_id"], outcome="false-positive")
    counter = _bind(w.state, _put(w.state, {"counter": f1["finding_id"]}), "finding-proof")
    w.run(
        "close-false-positive",
        {
            "resolutions": [
                {
                    "finding_id": f1["finding_id"],
                    "counter_evidence_ids": [counter],
                    "review_id": adj1,
                }
            ]
        },
    )
    # A second finding reported at blind-final drives the repair.
    w.final(report=True)
    f2 = w.reported_finding
    _run_adjudicated(w, f2["finding_id"], outcome="confirmed", remediation_class="review-process")
    _enter_repair(w, f2, target_kind="finding-adjudication", target_ids=[adj1])
    assert w.state["findings"][f1["finding_id"]]["disposition"] == "open"
    # The reopened finding blocks everything until re-adjudicated + re-closed.
    adj2 = _run_adjudicated(w, f1["finding_id"], outcome="false-positive")
    counter2 = _bind(w.state, _put(w.state, {"counter2": f1["finding_id"]}), "finding-proof")
    w.run(
        "close-false-positive",
        {
            "resolutions": [
                {
                    "finding_id": f1["finding_id"],
                    "counter_evidence_ids": [counter2],
                    "review_id": adj2,
                }
            ]
        },
    )
    # The cut also reached the blind-final attestation (downstream): final
    # and closure must be re-proven before the repair can verify.
    w.final()
    w.closure()
    _verify_close_repair(w, f2)
    w.ready()
    w.transition()
    w.remote_ci()
    w.seal()
    obs, now = make_remote_observation(w.state)
    assert policy.evaluate_green(w.state, obs, policies=w.policies, now=now).allowed


def test_repair_fix_review_row(tmp_path):
    """Cutting a fix review returns the fixed finding to fixing: its check and
    review re-prove, it re-closes, then the repair verifies."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f1 = w.reported_finding
    _run_adjudicated(w, f1["finding_id"], outcome="confirmed", remediation_class="candidate-change")
    intake = w.intake(epoch=2, head_sha="f" * 40)
    pub = _bind(
        w.state,
        _put(w.state, {"publication": f1["finding_id"]}),
        "fix-proof",
        snap=intake["snapshot"],
    )
    w.run(
        "enter-fixing",
        {
            "resolutions": [{"finding_id": f1["finding_id"], "publication_evidence_ids": [pub]}],
            "replacement_snapshot": intake["snapshot"],
            "replacement_authority_manifest": intake["authority_manifest"],
            "replacement_authorities": intake["authorities"],
        },
    )
    w.ascent()
    w.local_check("run-fix-verification", kind="targeted", item=w.items[1])
    fix_att = w.review(
        "review-fix",
        role="fix-reviewer",
        profile="fix-reviewer",
        tier="focused",
        reasoning="standard",
        assignments=[f1["finding_id"]],
    )
    targeted = next(
        c["check_id"] for c in w.state["checks"].values() if c["kind"] == "targeted" and c["locus"] == "local"
    )
    w.run(
        "close-fixed",
        {
            "resolutions": [
                {
                    "finding_id": f1["finding_id"],
                    "check_id": targeted,
                    "review_id": fix_att["attestation_id"],
                    "verified_obligation_ids": [f1["obligation_id"]],
                }
            ]
        },
    )
    assert w.state["findings"][f1["finding_id"]]["disposition"] == "fixed"
    # f2 reported at blind-final repairs f1's fix review.
    w.final(report=True)
    f2 = w.reported_finding
    _run_adjudicated(w, f2["finding_id"], outcome="confirmed", remediation_class="review-process")
    _enter_repair(w, f2, target_kind="fix-review", target_ids=[fix_att["attestation_id"]])
    assert w.state["findings"][f1["finding_id"]]["disposition"] == "fixing"
    # f1's fix check/review were cut; the fix lifecycle re-proves.
    w.local_check("run-fix-verification", kind="targeted", item=w.items[1])
    fix_att2 = w.review(
        "review-fix",
        role="fix-reviewer",
        profile="fix-reviewer-2",
        tier="focused",
        reasoning="standard",
        assignments=[f1["finding_id"]],
    )
    targeted2 = next(
        c["check_id"]
        for c in w.state["checks"].values()
        if c["kind"] == "targeted" and c["locus"] == "local" and policy._current(w.state, c, c["check_id"])
    )
    w.run(
        "close-fixed",
        {
            "resolutions": [
                {
                    "finding_id": f1["finding_id"],
                    "check_id": targeted2,
                    "review_id": fix_att2["attestation_id"],
                    "verified_obligation_ids": [f1["obligation_id"]],
                }
            ]
        },
    )
    # The blind-final attestation was downstream of the cut; final and
    # closure must be re-proven before the repair verifies.
    w.final()
    w.closure()
    _verify_close_repair(w, f2)
    w.ready()
    w.transition()
    w.remote_ci()
    w.seal()
    obs, now = make_remote_observation(w.state)
    assert policy.evaluate_green(w.state, obs, policies=w.policies, now=now).allowed


def test_repair_blind_final_row(tmp_path):
    """A blind-final repair cuts the final attestation: final re-runs fresh
    before the repair may verify."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent()
    att = w.final(report=True)
    f = _repair_finding(w)
    _enter_repair(w, f, target_kind="blind-final", target_ids=[att["attestation_id"]])
    # The cut reaches final and closure proof: both re-run before verify.
    w.final()
    w.closure()
    _verify_close_repair(w, f)
    w.ready()
    w.transition()
    w.remote_ci()
    w.seal()
    obs, now = make_remote_observation(w.state)
    assert policy.evaluate_green(w.state, obs, policies=w.policies, now=now).allowed


def test_repair_closure_audit_row(tmp_path):
    """A closure repair cuts the closure attestation only; blind-final stays
    current and closure re-runs before verify."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent()
    w.final()
    att = w.closure(report=True)
    f = _repair_finding(w)
    _enter_repair(w, f, target_kind="closure-audit", target_ids=[att["attestation_id"]])
    w.closure()
    _verify_close_repair(w, f)
    w.ready()
    w.transition()
    w.remote_ci()
    w.seal()
    obs, now = make_remote_observation(w.state)
    assert policy.evaluate_green(w.state, obs, policies=w.policies, now=now).allowed


def test_repair_local_check_row(tmp_path):
    """A local-check repair cuts the check plus every later reviewer/fix/
    repair/final/closure/ready/CI predicate: challenge, exemption, preflight
    and all review tiers re-prove."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f = _repair_finding(w)
    pre = next(c["check_id"] for c in w.state["checks"].values() if c["kind"] == "preflight")
    _enter_repair(w, f, target_kind="local-check", target_ids=[pre])
    # The challenger attestation was cut; coverage re-proves first.
    w.challenge(na=True)
    w.exemption()
    w.ascent_after_exemption()
    _verify_close_repair(w, f)
    w.remote()
    obs, now = make_remote_observation(w.state)
    assert policy.evaluate_green(w.state, obs, policies=w.policies, now=now).allowed


def test_repair_hosted_check_row(tmp_path):
    """A hosted-check repair cuts the remote-CI check; the ready transition
    and its derived CI candidate survive, so remote CI simply re-observes
    before the repair verifies."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent()
    w.final()
    w.closure()
    w.ready()
    w.transition()
    w.remote_ci()
    # A check finding against the remote-CI check surfaces after the fact.
    rci = next(c["check_id"] for c in w.state["checks"].values() if c["kind"] == "remote-ci")
    f = _finding_payload(
        w.state,
        source_kind="check",
        source_id=rci,
        source_assignment_id=rci,
        severity="important",
        title="hosted-check-regression",
    )
    w.state["findings"][f["finding_id"]] = f
    _run_adjudicated(w, f["finding_id"], outcome="confirmed", remediation_class="review-process")
    _enter_repair(w, f, target_kind="hosted-check", target_ids=[rci])
    assert policy.next_action(w.state, policies=w.policies).action == ("run-remote-ci")
    w.remote_ci(run=2)
    _verify_close_repair(w, f)
    w.seal()
    obs, now = make_remote_observation(w.state)
    assert policy.evaluate_green(w.state, obs, policies=w.policies, now=now).allowed


@pytest.mark.parametrize(
    "bad_kind",
    [
        "authority",
        "feedback-history",
        "policy",
        "snapshot",
        "repository-input",
        "bogus",
    ],
)
def test_repair_rejects_unsupported_target_kind(tmp_path, bad_kind):
    """Authority/feedback/policy/snapshot omissions are not same-snapshot
    repair targets; they require trusted refresh-review-input."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f = _repair_finding(w)
    with pytest.raises(model.StateValidationError):
        policy.complete_action(
            w.state,
            action="enter-review-repair",
            raw_data=model.canonical_json(
                {
                    "resolutions": [
                        {
                            "finding_id": f["finding_id"],
                            "repair_id": "repair-bad",
                            "target_kind": bad_kind,
                            "target_ids": ["x"],
                        }
                    ]
                }
            ),
            policies=w.policies,
        )


# ---------------------------------------------------------------------------
# Freeze/refresh findings: provider feedback enters through the payload and is
# durable identity - re-enumeration can never reset or resurrect a lifecycle.


def _feedback_finding(source_id="github:thread:PRRT_1", *, title="feedback finding"):
    return {
        "source_kind": "feedback",
        "source_id": source_id,
        "source_assignment_id": source_id,
        "obligation_id": None,
        "severity": "minor",
        "title": title,
        "description": "desc",
        "locations": [source_id],
        "evidence_ids": [],
        "regression_of": None,
        "disposition": "open",
        "resolution": None,
    }


class TestFreezeFindings:
    def test_freeze_installs_feedback_finding(self, tmp_path):
        w = _Walk(tmp_path)
        data = w.intake(epoch=1, head_sha="b" * 40)
        data["findings"] = [_feedback_finding()]
        w.run("freeze-review-input", data)
        f = next(iter(w.state["findings"].values()))
        assert f["source_kind"] == "feedback"
        assert f["source_id"] == "github:thread:PRRT_1"
        assert f["disposition"] == "open"
        assert f["discovered_snapshot_epoch"] == w.state["snapshot"]["epoch"]
        assert f["discovered_snapshot_fingerprint"] == w.state["snapshot"]["fingerprint"]

    def test_freeze_rejects_non_feedback_finding(self, tmp_path):
        w = _Walk(tmp_path)
        data = w.intake(epoch=1, head_sha="b" * 40)
        bad = _feedback_finding()
        bad["source_kind"] = "review"
        data["findings"] = [bad]
        with pytest.raises(model.StateValidationError) as ei:
            policy.complete_action(
                w.state,
                action="freeze-review-input",
                raw_data=model.canonical_json(data),
                policies=w.policies,
            )
        assert ei.value.code == "bad-finding"

    def test_freeze_rejects_finding_with_resolution(self, tmp_path):
        w = _Walk(tmp_path)
        data = w.intake(epoch=1, head_sha="b" * 40)
        bad = _feedback_finding()
        bad["resolution"] = {"note": "pre-resolved"}
        data["findings"] = [bad]
        with pytest.raises(model.StateValidationError) as ei:
            policy.complete_action(
                w.state,
                action="freeze-review-input",
                raw_data=model.canonical_json(data),
                policies=w.policies,
            )
        assert ei.value.code == "bad-finding"

    def test_freeze_rejects_mismatched_source_ids(self, tmp_path):
        w = _Walk(tmp_path)
        data = w.intake(epoch=1, head_sha="b" * 40)
        bad = _feedback_finding()
        bad["source_assignment_id"] = "github:review:OTHER"
        data["findings"] = [bad]
        with pytest.raises(model.StateValidationError) as ei:
            policy.complete_action(
                w.state,
                action="freeze-review-input",
                raw_data=model.canonical_json(data),
                policies=w.policies,
            )
        assert ei.value.code == "bad-finding"

    def test_freeze_rejects_locations_without_source_id(self, tmp_path):
        w = _Walk(tmp_path)
        data = w.intake(epoch=1, head_sha="b" * 40)
        bad = _feedback_finding()
        bad["locations"] = ["src/foo.py"]
        data["findings"] = [bad]
        with pytest.raises(model.StateValidationError) as ei:
            policy.complete_action(
                w.state,
                action="freeze-review-input",
                raw_data=model.canonical_json(data),
                policies=w.policies,
            )
        assert ei.value.code == "bad-finding"

    def test_refresh_keeps_prior_findings_and_adds_new(self, tmp_path):
        w = _Walk(tmp_path)
        data = w.intake(epoch=1, head_sha="b" * 40)
        data["findings"] = [_feedback_finding()]
        w.run("freeze-review-input", data)
        first_id = next(iter(w.state["findings"]))
        intake = w.intake(epoch=2, head_sha="9" * 40)
        w.run(
            "refresh-review-input",
            dict(
                intake,
                drift_reasons=["head_sha"],
                findings=[
                    _feedback_finding(),
                    _feedback_finding("github:thread:PRRT_2", title="new thread"),
                ],
            ),
            sole=False,
        )
        assert w.state["findings"][first_id]["disposition"] == "open"
        new = [f for f in w.state["findings"].values() if f["source_id"] == "github:thread:PRRT_2"]
        assert len(new) == 1
        assert new[0]["discovered_snapshot_epoch"] == 2
        assert new[0]["discovered_snapshot_fingerprint"] == w.state["snapshot"]["fingerprint"]

    def test_refresh_does_not_reopen_closed_finding(self, tmp_path):
        w = _Walk(tmp_path)
        data = w.intake(epoch=1, head_sha="b" * 40)
        data["findings"] = [_feedback_finding()]
        w.run("freeze-review-input", data)
        fid = next(iter(w.state["findings"]))
        adj_att_id = _run_adjudicated(w, fid, outcome="false-positive")
        counter = _bind(
            w.state,
            _put(w.state, {"counter-evidence": fid}),
            "finding-proof",
        )
        w.run(
            "close-false-positive",
            {
                "resolutions": [
                    {
                        "finding_id": fid,
                        "counter_evidence_ids": [counter],
                        "review_id": adj_att_id,
                    }
                ]
            },
            sole=False,
        )
        closed_disposition = w.state["findings"][fid]["disposition"]
        assert closed_disposition != "open"
        intake = w.intake(epoch=2, head_sha="9" * 40)
        # The provider still reports the same thread; refresh re-presents it.
        w.run(
            "refresh-review-input",
            dict(
                intake,
                drift_reasons=["head_sha"],
                findings=[_feedback_finding()],
            ),
            sole=False,
        )
        f = w.state["findings"][fid]
        assert f["disposition"] == closed_disposition
        assert f["resolution"] is not None
        # discovered_* still records epoch 1: identity is durable.
        assert f["discovered_snapshot_epoch"] == 1

    def test_refresh_payload_missing_findings_fails(self, tmp_path):
        w = _Walk(tmp_path)
        w.freeze()
        w.ascent()
        intake = w.intake(epoch=2, head_sha="9" * 40)
        with pytest.raises(model.StateValidationError) as ei:
            policy.complete_action(
                w.state,
                action="refresh-review-input",
                raw_data=model.canonical_json(
                    {k: v for k, v in dict(intake, drift_reasons=["head_sha"]).items() if k != "findings"}
                ),
                policies=w.policies,
            )
        assert ei.value.code == "missing-field"
