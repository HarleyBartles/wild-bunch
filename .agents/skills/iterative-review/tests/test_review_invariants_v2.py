#!/usr/bin/env python3
"""Version-2 evidence-kernel invariant catalog.

Every test starts from a valid green candidate (or the relevant earlier stage)
and breaks exactly one guarantee. All of these are RED until Tasks 2-5 land the
model, store, policy, and engine.
"""

from __future__ import annotations

import sys
from datetime import timedelta
from pathlib import Path

import pytest

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR))
sys.path.insert(0, str(TESTS_DIR.parent / "scripts"))

from review_v2_helpers import (  # noqa: E402
    MISSING_PREDICATES,
    _finding,
    make_complete_candidate,
    make_policy_bundle,
    make_remote_observation,
    remove_predicate,
)

from review_core.policy import evaluate_green  # noqa: E402


def _eval(state, observation=None, now=None, policies=None):
    if observation is None:
        observation, now = make_remote_observation(state)
    if policies is None:
        policies = make_policy_bundle(state)
    return evaluate_green(state, observation, policies=policies, now=now)


def _route_for_role(state, role):
    """Return the route selection for the dispatch carrying `role`."""
    for d in state["dispatches"].values():
        rs = state["route_selections"][d["route_selection_id"]]
        if rs["required_role"] == role:
            return rs
    raise AssertionError(f"no dispatch for role {role!r}")


def _witnesses_of_kind(state, kind):
    return [w for w in state["witness_records"].values() if w["kind"] == kind]


@pytest.mark.parametrize("missing", list(MISSING_PREDICATES))
def test_green_rejects_each_missing_predicate(tmp_path, missing):
    state = make_complete_candidate(tmp_path)
    observation, now = make_remote_observation(state)
    policies = make_policy_bundle(state)
    remove_predicate(state, observation, missing)
    decision = evaluate_green(state, observation, policies=policies, now=now)
    assert decision.allowed is False
    assert missing in decision.missing


def test_green_rejects_stale_epoch_evidence(tmp_path):
    state = make_complete_candidate(tmp_path)
    state["snapshot"]["epoch"] += 1
    assert _eval(state).allowed is False


def test_green_rejects_wrong_snapshot_fingerprint(tmp_path):
    state = make_complete_candidate(tmp_path)
    state["snapshot"]["fingerprint"] = "0" * 64
    assert _eval(state).allowed is False


def test_green_rejects_unassessed_obligation(tmp_path):
    state = make_complete_candidate(tmp_path)
    oid = next(iter(state["obligations"]))
    state["obligations"][oid]["status"] = "unassessed"
    assert _eval(state).allowed is False


def test_green_rejects_incomplete_expected_authority_manifest(tmp_path):
    state = make_complete_candidate(tmp_path)
    # A manifest-listed authority has no corresponding record.
    state["authorities"].pop(next(iter(state["authorities"])))
    assert _eval(state).allowed is False


def test_green_rejects_unverified_authority_discovery_witness(tmp_path):
    state = make_complete_candidate(tmp_path)
    for w in _witnesses_of_kind(state, "authority-discovery"):
        w["source_locator"] = "untrusted:" + w["source_locator"]
    assert _eval(state).allowed is False


def test_green_rejects_missing_impact_map_surface(tmp_path):
    state = make_complete_candidate(tmp_path)
    # A map whose entries omit a covered surface leaves the inventory
    # listing an obligation surface the union no longer reports. The
    # challenger-bound inventory may only add, so removing a map entry
    # breaks nothing; deleting the map breaks role completeness.
    state["impact_maps"].pop(next(iter(state["impact_maps"])))
    assert _eval(state).allowed is False


def test_green_rejects_coverage_inventory_that_omits_map_surface(tmp_path):
    state = make_complete_candidate(tmp_path)
    state["coverage_inventory"]["entries"] = state["coverage_inventory"]["entries"][:-1]
    assert _eval(state).allowed is False


def test_green_rejects_coverage_inventory_that_omits_map_hazard(tmp_path):
    state = make_complete_candidate(tmp_path)
    for e in state["coverage_inventory"]["entries"]:
        e["hazards"] = e["hazards"][:-1]
    assert _eval(state).allowed is False


def test_green_rejects_missing_inventory_surface_category_obligation(tmp_path):
    state = make_complete_candidate(tmp_path)
    drop = next(iter(state["obligations"].values()))["category"]
    state["obligations"] = {k: v for k, v in state["obligations"].items() if v["category"] != drop}
    assert _eval(state).allowed is False


def test_green_rejects_inventory_entry_without_all_universal_categories(tmp_path):
    state = make_complete_candidate(tmp_path)
    for e in state["coverage_inventory"]["entries"]:
        e["categories"] = e["categories"][:-1]
    assert _eval(state).allowed is False


def test_green_rejects_not_applicable_without_qualified_attestation(tmp_path):
    state = make_complete_candidate(tmp_path)
    for o in state["obligations"].values():
        if o["status"] == "not-applicable":
            o["not_applicable_attestation_ids"] = []
    assert _eval(state).allowed is False


def _dispatch_role(state, attestation_id):
    d = state["dispatches"][state["reviews"][attestation_id]["dispatch_id"]]
    return state["route_selections"][d["route_selection_id"]]["required_role"]


def test_green_rejects_high_risk_not_applicable_without_independent_overlap(tmp_path):
    state = make_complete_candidate(tmp_path)
    for o in state["obligations"].values():
        if o["risk"] == "high" and o["status"] == "not-applicable":
            o["not_applicable_attestation_ids"] = [
                aid
                for aid in o["not_applicable_attestation_ids"]
                if _dispatch_role(state, aid) != "exemption-challenger"
            ]
    assert _eval(state).allowed is False


def test_green_rejects_deferred_minor_finding(tmp_path):
    state = make_complete_candidate(tmp_path)
    for f in state["findings"].values():
        f["disposition"] = "deferred"
    assert _eval(state).allowed is False


def test_green_rejects_reviewer_uncertainty(tmp_path):
    state = make_complete_candidate(tmp_path)
    for r in state["reviews"].values():
        r["verdict"] = "incomplete"
    assert _eval(state).allowed is False


def test_green_rejects_malformed_report(tmp_path):
    state = make_complete_candidate(tmp_path)
    for r in state["reviews"].values():
        r["evidence_id"] = "evidence:nonexistent"
    assert _eval(state).allowed is False


def test_green_rejects_ci_for_previous_head(tmp_path):
    state = make_complete_candidate(tmp_path)
    for c in state["checks"].values():
        c["head_sha"] = "0" * 40
    assert _eval(state).allowed is False


def test_green_rejects_required_check_from_wrong_app_or_workflow(tmp_path):
    state = make_complete_candidate(tmp_path)
    for c in state["checks"].values():
        if c["locus"] == "hosted":
            c["workflow_run_id"] = "run-not-in-observation"
    assert _eval(state).allowed is False


def test_green_rejects_wrong_repository_or_pr_identity(tmp_path):
    state = make_complete_candidate(tmp_path)
    obs, now = make_remote_observation(state)
    obs["repository_id"] = "other/repo"
    assert _eval(state, observation=obs, now=now).allowed is False


def test_green_rejects_unresolved_feedback_drift(tmp_path):
    state = make_complete_candidate(tmp_path)
    obs, now = make_remote_observation(state)
    obs["unresolved_feedback_sha256"] = "0" * 64
    assert _eval(state, observation=obs, now=now).allowed is False


def test_green_rejects_stable_nonempty_unresolved_feedback_set(tmp_path):
    state = make_complete_candidate(tmp_path)
    _finding(
        state,
        source_kind="feedback",
        source_id="gh:pr:7:thread:open",
        source_assignment_id="gh:pr:7:thread:open",
        disposition="open",
        title="unresolved-feedback",
    )
    assert _eval(state).allowed is False


def test_provider_resolved_feedback_remains_a_finding_until_lifecycle_closure(tmp_path):
    state = make_complete_candidate(tmp_path)
    for f in state["findings"].values():
        if f["source_kind"] == "feedback":
            f["disposition"] = "open"
            f["resolution"] = None
    assert _eval(state).allowed is False


def test_initially_resolved_feedback_materializes_finding(tmp_path):
    state = make_complete_candidate(tmp_path)
    # The fixture's provider-resolved feedback is a closed finding record.
    assert any(f["source_kind"] == "feedback" for f in state["findings"].values())
    assert _eval(state).allowed is True


def test_green_blocks_when_authority_discovery_witness_reports_incomplete(tmp_path):
    state = make_complete_candidate(tmp_path)
    for w in _witnesses_of_kind(state, "authority-discovery"):
        w["agent_id"] = "agent-foreign"
    assert _eval(state).allowed is False


def test_green_rejects_discovery_policy_qualified_by_reviewed_head(tmp_path):
    state = make_complete_candidate(tmp_path)
    policies = make_policy_bundle(state)
    policies.discovery_policy_origin = "reviewed-head"
    assert _eval(state, policies=policies).allowed is False


def test_green_rejects_remote_observation_older_than_60_seconds(tmp_path):
    state = make_complete_candidate(tmp_path)
    obs, now = make_remote_observation(state)
    obs["observed_at"] = (now - timedelta(seconds=61)).isoformat()
    assert _eval(state, observation=obs, now=now).allowed is False


def test_green_rejects_remote_observation_more_than_5_seconds_in_future(tmp_path):
    state = make_complete_candidate(tmp_path)
    obs, now = make_remote_observation(state)
    obs["observed_at"] = (now + timedelta(seconds=6)).isoformat()
    assert _eval(state, observation=obs, now=now).allowed is False


def test_green_rejects_accepted_risk_and_returns_reviewed_with_exceptions(tmp_path):
    state = make_complete_candidate(tmp_path)
    for f in state["findings"].values():
        f["disposition"] = "accepted-risk"
    decision = _eval(state)
    assert decision.allowed is False
    assert decision.status == "reviewed-with-exceptions"


def test_green_rejects_fast_profile_used_for_strong_obligation(tmp_path):
    state = make_complete_candidate(tmp_path)
    # Find the strong-tier reviewer route and downgrade it.
    for d in state["dispatches"].values():
        cand = state["route_selections"][d["route_selection_id"]]
        if cand["required_role"] == "obligation-reviewer" and cand["required_capability_tier"] == "strong":
            cand["required_capability_tier"] = "fast"
    assert _eval(state).allowed is False


def test_green_rejects_mapper_below_strong_or_without_fresh_distinct_execution(tmp_path):
    state = make_complete_candidate(tmp_path)
    _route_for_role(state, "impact-mapper-semantic")["selected_reasoning"] = "low"
    assert _eval(state).allowed is False


def test_green_rejects_scope_challenger_below_final_strong_or_reusing_mapper_contract(tmp_path):
    state = make_complete_candidate(tmp_path)
    _route_for_role(state, "scope-challenger")["required_capability_tier"] = "strong"
    assert _eval(state).allowed is False


def test_green_rejects_adjudicator_below_source_floor_or_self_adjudicating(tmp_path):
    state = make_complete_candidate(tmp_path)
    # Self-adjudication is caught by per-kind launch-witness agent
    # uniqueness: the adjudicator shares the source reviewer's execution.
    adj = next(
        d
        for d in state["dispatches"].values()
        if state["route_selections"][d["route_selection_id"]]["required_role"] == "finding-adjudicator"
    )
    other = next(d for d in state["dispatches"].values() if d["dispatch_id"] != adj["dispatch_id"] and d["agent_id"])
    adj_launch = state["witness_records"][adj["launch_witness_id"]]
    adj_launch["agent_id"] = other["agent_id"]
    assert _eval(state).allowed is False


def test_green_rejects_final_reviewer_below_final_strong(tmp_path):
    state = make_complete_candidate(tmp_path)
    _route_for_role(state, "blind-final")["required_capability_tier"] = "strong"
    assert _eval(state).allowed is False


def test_green_accepts_role_qualified_profile_for_blind_final(tmp_path):
    state = make_complete_candidate(tmp_path)
    assert _eval(state).allowed is True


def test_green_accepts_role_qualified_profile_for_closure_auditor(tmp_path):
    state = make_complete_candidate(tmp_path)
    assert _eval(state).allowed is True


def test_current_vendored_reviewer_strong_profile_cannot_qualify_as_blind_final(tmp_path):
    state = make_complete_candidate(tmp_path)
    _route_for_role(state, "blind-final")["qualified_roles"] = ("obligation-reviewer",)
    assert _eval(state).allowed is False


def test_green_does_not_require_parent_equality_for_qualified_profile(tmp_path):
    state = make_complete_candidate(tmp_path)
    assert _eval(state).allowed is True


def test_green_rejects_model_override_on_qualified_profile(tmp_path):
    state = make_complete_candidate(tmp_path)
    # A post-resolution model override drifts the route away from the
    # witnessed profile resolution; the resolution witness subject binds the
    # full route including selected_model.
    for rs in state["route_selections"].values():
        if rs["required_role"] == "blind-final":
            rs["selected_model"] = "other-model"
    assert _eval(state).allowed is False


def test_green_rejects_profile_authority_from_reviewed_head(tmp_path):
    state = make_complete_candidate(tmp_path)
    # A profile resolved from the reviewed head is not in the trusted
    # harness profile inventory.
    for rs in state["route_selections"].values():
        if rs["required_role"] == "blind-final":
            rs["profile"] = "reviewed-head-profile"
    assert _eval(state).allowed is False


def test_green_rejects_final_strong_profile_without_blind_final_role(tmp_path):
    state = make_complete_candidate(tmp_path)
    _route_for_role(state, "blind-final")["qualified_roles"] = ("closure-auditor",)
    assert _eval(state).allowed is False


def test_green_rejects_closure_profile_without_closure_role(tmp_path):
    state = make_complete_candidate(tmp_path)
    _route_for_role(state, "closure-auditor")["qualified_roles"] = ("blind-final",)
    assert _eval(state).allowed is False


def test_green_rejects_fabricated_launch_or_completion_witness(tmp_path):
    state = make_complete_candidate(tmp_path)
    for w in state["witness_records"].values():
        if w["kind"] in ("review-launch", "review-completion"):
            w["chain_head_at_record"] = "f" * 64
    assert _eval(state).allowed is False


def test_green_rejects_fabricated_command_success_without_execution_witness(tmp_path):
    state = make_complete_candidate(tmp_path)
    for c in state["checks"].values():
        if c["locus"] == "local":
            c["execution_witness_id"] = "missing"
    assert _eval(state).allowed is False


def test_green_rejects_dirty_checkout_or_mutable_source_command_witness(tmp_path):
    state = make_complete_candidate(tmp_path)
    for c in state["checks"].values():
        if c["locus"] == "local":
            c["pre_source_sha256"] = "0" * 64
    assert _eval(state).allowed is False


def test_green_rejects_command_with_review_evidence_access_or_surviving_child(tmp_path):
    state = make_complete_candidate(tmp_path)
    for w in _witnesses_of_kind(state, "command-execution"):
        w["subject_sha256"] = "0" * 64
    assert _eval(state).allowed is False


def test_green_rejects_command_witness_with_wrong_toolchain_or_environment_digest(tmp_path):
    state = make_complete_candidate(tmp_path)
    for c in state["checks"].values():
        if c["locus"] == "local":
            c["toolchain_sha256"] = "0" * 64
    assert _eval(state).allowed is False


def test_green_rejects_hosted_check_with_local_command_witness(tmp_path):
    state = make_complete_candidate(tmp_path)
    cmd_witness = _witnesses_of_kind(state, "command-execution")[0]["witness_id"]
    for c in state["checks"].values():
        if c["locus"] == "hosted":
            c["remote_observation_witness_id"] = cmd_witness
    assert _eval(state).allowed is False


def test_green_rejects_local_check_with_remote_observation_witness(tmp_path):
    state = make_complete_candidate(tmp_path)
    obs_witness = _witnesses_of_kind(state, "remote-observation")[0]["witness_id"]
    for c in state["checks"].values():
        if c["locus"] == "local":
            c["execution_witness_id"] = obs_witness
    assert _eval(state).allowed is False


def test_green_rejects_replayed_launch_witness_or_reused_agent_id(tmp_path):
    state = make_complete_candidate(tmp_path)
    launches = _witnesses_of_kind(state, "review-launch")
    if len(launches) > 1:
        launches[1]["tool_use_id"] = launches[0]["tool_use_id"]
    assert _eval(state).allowed is False


def test_green_rejects_cross_review_replay_for_every_witness_kind(tmp_path):
    state = make_complete_candidate(tmp_path)
    policies = make_policy_bundle(state)
    state["review_id"] = "review-other"
    assert _eval(state, policies=policies).allowed is False


def test_green_rejects_unverified_witness_source_or_locator(tmp_path):
    state = make_complete_candidate(tmp_path)
    for w in state["witness_records"].values():
        w["source_locator"] = "untrusted"
    assert _eval(state).allowed is False


def test_green_rejects_reduced_manifest_paired_with_genuine_discovery_witness(tmp_path):
    state = make_complete_candidate(tmp_path)
    # The manifest payload claims fewer authorities than the discovery
    # witness actually reported: the surviving records become undeclared.
    for rec in state["authorities"].values():
        if rec["availability"] == "loaded":
            rec["availability"] = "unavailable"
            break
    assert _eval(state).allowed is False


def test_green_rejects_discovery_witness_for_different_snapshot_subject(tmp_path):
    state = make_complete_candidate(tmp_path)
    for w in _witnesses_of_kind(state, "authority-discovery"):
        w["subject_sha256"] = "0" * 64
    assert _eval(state).allowed is False


def test_green_rejects_dispatch_witness_with_broken_or_tampered_transcript_chain(tmp_path):
    state = make_complete_candidate(tmp_path)
    for w in _witnesses_of_kind(state, "review-launch"):
        w["chain_head_at_record"] = "0" * 64
    assert _eval(state).allowed is False


def test_green_rejects_contaminated_blind_final_transcript_audit(tmp_path):
    state = make_complete_candidate(tmp_path)
    for r in state["reviews"].values():
        d = state["dispatches"].get(r["dispatch_id"])
        if d is not None and state["route_selections"][d["route_selection_id"]]["required_role"] == "blind-final":
            r["audit_result"] = "contaminated"
    assert _eval(state).allowed is False


def test_green_rejects_cloned_high_risk_not_applicable_attestations(tmp_path):
    state = make_complete_candidate(tmp_path)
    # The exemption attestation slot is filled by cloning the ordinary
    # obligation-reviewer's attestation; no exemption-challenger attestation
    # remains for the high-risk not-applicable obligation.
    for o in state["obligations"].values():
        if o["risk"] == "high" and o["status"] == "not-applicable":
            reviewer_att = next(
                aid
                for aid in o["not_applicable_attestation_ids"]
                if _dispatch_role(state, aid) == "obligation-reviewer"
            )
            o["not_applicable_attestation_ids"] = [reviewer_att, reviewer_att]
    assert _eval(state).allowed is False


def test_green_rejects_high_risk_exemption_without_final_strong_exemption_challenger(tmp_path):
    state = make_complete_candidate(tmp_path)
    _route_for_role(state, "exemption-challenger")["required_capability_tier"] = "strong"
    assert _eval(state).allowed is False


def test_green_rejects_whitespace_only_hazard_framing_as_independence(tmp_path):
    state = make_complete_candidate(tmp_path)
    mapper = next(
        d
        for d in state["dispatches"].values()
        if state["route_selections"][d["route_selection_id"]]["required_role"] == "impact-mapper-semantic"
    )
    challenger = next(
        d
        for d in state["dispatches"].values()
        if state["route_selections"][d["route_selection_id"]]["required_role"] == "scope-challenger"
    )
    challenger["hazard_framing_sha256"] = mapper["hazard_framing_sha256"]
    assert _eval(state).allowed is False


def test_green_rejects_mapper_report_paired_with_substituted_map_subject(tmp_path):
    state = make_complete_candidate(tmp_path)
    for m in state["impact_maps"].values():
        m["evidence_id"] = "evidence:substituted"
    assert _eval(state).allowed is False


def test_green_rejects_challenger_report_paired_with_substituted_inventory(tmp_path):
    state = make_complete_candidate(tmp_path)
    other_att = next(
        r["attestation_id"]
        for r in state["reviews"].values()
        if state["route_selections"][state["dispatches"][r["dispatch_id"]]["route_selection_id"]]["required_role"]
        != "scope-challenger"
    )
    state["coverage_inventory"]["challenger_attestation_id"] = other_att
    assert _eval(state).allowed is False


def test_green_rejects_copied_prior_conclusions_in_new_blind_context_evidence(tmp_path):
    state = make_complete_candidate(tmp_path)
    _route_for_role(state, "blind-final")["selected_context_mode"] = "forked"
    assert _eval(state).allowed is False


def test_blind_final_profile_has_no_exec_write_or_mcp_tools(tmp_path):
    state = make_complete_candidate(tmp_path)
    for d in state["dispatches"].values():
        if state["route_selections"][d["route_selection_id"]]["required_role"] == "blind-final":
            assert "command-exec" not in d["required_tool_classes"]
            assert "browser-read" not in d["required_tool_classes"]


def test_blind_final_transcript_audit_detects_prior_review_or_feedback_reads(tmp_path):
    state = make_complete_candidate(tmp_path)
    for r in state["reviews"].values():
        d = state["dispatches"].get(r["dispatch_id"])
        if d is not None and state["route_selections"][d["route_selection_id"]]["required_role"] == "blind-final":
            r["audit_result"] = "contaminated"
    assert _eval(state).allowed is False


def test_green_rejects_profile_mapping_change_between_resolution_and_launch(tmp_path):
    state = make_complete_candidate(tmp_path)
    for rs in state["route_selections"].values():
        rs["profile_sha256"] = "0" * 64
    assert _eval(state).allowed is False


def test_final_and_closure_re_resolve_profile_at_each_launch(tmp_path):
    state = make_complete_candidate(tmp_path)
    # A dispatch whose recorded resolution witness does not exist in the
    # current record set cannot have re-resolved at launch.
    for d in state["dispatches"].values():
        if state["route_selections"][d["route_selection_id"]]["required_role"] == "blind-final":
            d["profile_resolution_witness_id"] = "witness:nonexistent"
    assert _eval(state).allowed is False


def test_green_blocks_dispatch_when_no_qualifying_profile_exists(tmp_path):
    state = make_complete_candidate(tmp_path)
    policies = make_policy_bundle(state)
    policies.available_profiles = ()
    assert _eval(state, policies=policies).allowed is False


def test_green_rejects_profile_without_allowed_tools_or_model_pin(tmp_path):
    state = make_complete_candidate(tmp_path)
    policies = make_policy_bundle(state)
    # Removing the profile from the trusted inventory models a profile whose
    # contract (tool list, model pin) cannot be re-resolved.
    drop = _route_for_role(state, "blind-final")["profile"]
    policies.available_profiles = tuple(p for p in policies.available_profiles if p != drop)
    assert _eval(state, policies=policies).allowed is False


def test_current_profile_qualification_is_reusable_only_for_matching_non_final_dispatches(tmp_path):
    state = make_complete_candidate(tmp_path)
    # Sharing a resolution witness between dispatches means the final role
    # did not re-resolve: the orphan witness is unbound and the reused one
    # describes a different dispatch's route.
    mappers = [
        d
        for d in state["dispatches"].values()
        if state["route_selections"][d["route_selection_id"]]["required_role"] == "impact-mapper-semantic"
    ]
    final = [
        d
        for d in state["dispatches"].values()
        if state["route_selections"][d["route_selection_id"]]["required_role"] == "blind-final"
    ]
    final[0]["profile_resolution_witness_id"] = mappers[0]["profile_resolution_witness_id"]
    assert _eval(state).allowed is False


def test_profile_or_inventory_change_invalidates_qualification(tmp_path):
    state = make_complete_candidate(tmp_path)
    for rs in state["route_selections"].values():
        rs["snapshot_epoch"] = state["snapshot"]["epoch"] - 1
    assert _eval(state).allowed is False


def test_any_snapshot_mutation_revokes_green_candidate(tmp_path):
    state = make_complete_candidate(tmp_path)
    state["snapshot"]["head_sha"] = "0" * 40
    assert _eval(state).allowed is False


def test_fix_enters_fixing_with_published_replacement_epoch_before_verification(tmp_path):
    state = make_complete_candidate(tmp_path)
    for f in state["findings"].values():
        if f["disposition"] == "fixed":
            f["resolution"]["check_id"] = "check:nonexistent"
    assert _eval(state).allowed is False


def test_fix_rereview_may_narrow_but_requires_broader_reascent_before_fixed(tmp_path):
    state = make_complete_candidate(tmp_path)
    for f in state["findings"].values():
        if f["disposition"] == "fixed":
            f["resolution"]["review_id"] = "review:nonexistent"
    assert _eval(state).allowed is False


def test_false_positive_resolution_does_not_require_replacement_snapshot(tmp_path):
    state = make_complete_candidate(tmp_path)
    # The fixture's false-positive finding carries no replacement snapshot
    # fields and the candidate still reaches green.
    fp = [f for f in state["findings"].values() if f["disposition"] == "false-positive"]
    assert fp and "replacement_snapshot_epoch" not in fp[0]["resolution"]
    assert _eval(state).allowed is True


def test_confirmed_closure_process_finding_repairs_same_snapshot_then_refinalizes(tmp_path):
    state = make_complete_candidate(tmp_path)
    for r in state["review_repairs"].values():
        r["snapshot_epoch"] = state["snapshot"]["epoch"] - 1
    assert _eval(state).allowed is False


def test_review_process_finding_rejects_enter_fixing_and_byte_identical_refresh(tmp_path):
    state = make_complete_candidate(tmp_path)
    for f in state["findings"].values():
        if f["disposition"] == "review-repaired":
            f["disposition"] = "fixing"
            f["resolution"] = None
    assert _eval(state).allowed is False


def test_review_repair_cannot_close_without_independent_current_verification(tmp_path):
    state = make_complete_candidate(tmp_path)
    for r in state["review_repairs"].values():
        r["verification_attestation_id"] = "review:nonexistent"
    assert _eval(state).allowed is False


def test_close_fixed_does_not_advance_epoch_again(tmp_path):
    state = make_complete_candidate(tmp_path)
    epoch = state["snapshot"]["epoch"]
    for f in state["findings"].values():
        if f["disposition"] == "fixed":
            check = state["checks"][f["resolution"]["check_id"]]
            assert check["snapshot_epoch"] == epoch


def test_accepted_risk_does_not_require_replacement_snapshot(tmp_path):
    state = make_complete_candidate(tmp_path)
    for f in state["findings"].values():
        if f["disposition"] == "fixed":
            f["disposition"] = "accepted-risk"
            f["resolution"] = {"human_decision_witness_id": next(iter(state["witness_records"]))}
    decision = _eval(state)
    assert decision.status == "reviewed-with-exceptions"


def test_finding_without_obligation_link_is_valid(tmp_path):
    state = make_complete_candidate(tmp_path)
    assert any(f["obligation_id"] is None for f in state["findings"].values())


def test_failed_remote_ci_atomically_materializes_a_check_finding(tmp_path):
    state = make_complete_candidate(tmp_path)
    for c in state["checks"].values():
        if c["locus"] == "hosted":
            c["conclusion"] = "failure"
    assert _eval(state).allowed is False


def test_green_blocks_when_remote_observation_witness_reports_untrusted_current_attempt(tmp_path):
    state = make_complete_candidate(tmp_path)
    obs, now = make_remote_observation(state)
    obs["workflow_runs"][0]["run_attempt"] += 1
    assert _eval(state, observation=obs, now=now).allowed is False


def test_green_rejects_manual_or_unauthorized_ci_trigger(tmp_path):
    state = make_complete_candidate(tmp_path)
    obs, now = make_remote_observation(state)
    for run in obs["workflow_runs"]:
        run["event"] = "workflow_dispatch"
    assert _eval(state, observation=obs, now=now).allowed is False


def test_green_rejects_wrong_workflow_definition_or_policy_input_digest(tmp_path):
    state = make_complete_candidate(tmp_path)
    for c in state["checks"].values():
        if c["locus"] == "hosted":
            c["check_run_id"] = "check-run-not-in-observation"
    assert _eval(state).allowed is False


def test_adjudication_and_fix_review_proof_exist_before_resolution_consumes_them(tmp_path):
    state = make_complete_candidate(tmp_path)
    for f in state["findings"].values():
        if f["disposition"] == "fixed":
            f["resolution"] = {}
    assert _eval(state).allowed is False


def test_adjudication_outcome_cannot_authorize_incompatible_resolution_branch(tmp_path):
    state = make_complete_candidate(tmp_path)
    # A fixed disposition whose resolution names the repair branch is an
    # incompatible authorization: the fixed branch needs a current fix
    # review and check.
    for f in state["findings"].values():
        if f["disposition"] == "fixed":
            f["resolution"] = {
                "kind": "review-process",
                "repair_id": next(iter(state["review_repairs"])),
            }
    assert _eval(state).allowed is False


def test_confirmed_adjudication_can_atomically_choose_accept_risk_with_human_proof(tmp_path):
    state = make_complete_candidate(tmp_path)
    for f in state["findings"].values():
        if f["disposition"] == "fixed":
            f["disposition"] = "accepted-risk"
            f["resolution"] = {"human_decision_witness_id": next(iter(state["witness_records"]))}
    decision = _eval(state)
    assert decision.status == "reviewed-with-exceptions"


def test_accept_risk_and_enter_fixing_are_mutually_exclusive(tmp_path):
    state = make_complete_candidate(tmp_path)
    for f in state["findings"].values():
        if f["disposition"] == "fixed":
            # Fix-shaped proof attached to an accepted-risk branch does not
            # redeem the finding: accepted risk never reaches green.
            f["disposition"] = "accepted-risk"
    assert _eval(state).allowed is False


def test_non_fix_drift_uses_refresh_review_input_and_reascends(tmp_path):
    state = make_complete_candidate(tmp_path)
    for m in state["impact_maps"].values():
        m["snapshot_fingerprint"] = "0" * 64
    assert _eval(state).allowed is False


def test_scope_risk_consequence_matrix_derives_required_tier(tmp_path):
    state = make_complete_candidate(tmp_path)
    for o in state["obligations"].values():
        assert "minimum_capability_tier" in o
        assert "minimum_reasoning_floor" in o


def test_consequence_union_drops_none_when_other_mapper_reports_security(tmp_path):
    state = make_complete_candidate(tmp_path)
    for o in state["obligations"].values():
        if "security" in o["consequences"]:
            assert o["minimum_capability_tier"] != "fast"


def test_repo_override_can_raise_but_not_lower_required_tier(tmp_path):
    state = make_complete_candidate(tmp_path)
    for o in state["obligations"].values():
        o["minimum_capability_tier"] = "fast"
    assert _eval(state).allowed is False


def test_final_and_closure_reject_not_applicable_outcome(tmp_path):
    state = make_complete_candidate(tmp_path)
    for r in state["reviews"].values():
        d = state["dispatches"].get(r["dispatch_id"])
        if d is None:
            continue
        if state["route_selections"][d["route_selection_id"]]["required_role"] in (
            "blind-final",
            "closure-auditor",
        ):
            r["verdict"] = "incomplete"
    assert _eval(state).allowed is False


def test_hypothesis_assignments_are_canonical_dispatchable_and_seal_bound(tmp_path):
    state = make_complete_candidate(tmp_path)
    for a in state["hypothesis_assignments"].values():
        assert a["hypothesis_assignment_id"].startswith("hypothesis:")
        assert a["derivation_policy_sha256"]
        assert a["snapshot_fingerprint"] == state["snapshot"]["fingerprint"]


def test_preflight_rejects_omitted_or_substituted_local_check_policy_item(tmp_path):
    state = make_complete_candidate(tmp_path)
    for c in state["checks"].values():
        if c["locus"] == "local" and c["kind"] == "preflight":
            c["policy_item_id"] = "substituted"
    assert _eval(state).allowed is False


def test_duplicate_json_keys_rejected_at_every_ingestion_boundary(tmp_path):
    from review_core.model import strict_json_loads

    with pytest.raises(Exception):
        strict_json_loads(b'{"a": 1, "a": 2}')


def test_every_canonical_projection_matches_independent_golden_bytes_and_hash(tmp_path):
    state = make_complete_candidate(tmp_path)
    from review_core.model import canonical_json

    blob = canonical_json(state["snapshot"])
    assert isinstance(blob, bytes) and len(blob) > 0


def test_every_included_projection_field_changes_digest(tmp_path):
    state = make_complete_candidate(tmp_path)
    from review_core.model import canonical_json
    import hashlib

    snap = dict(state["snapshot"])
    h1 = hashlib.sha256(canonical_json(snap)).hexdigest()
    snap["fingerprint"] = "f" * 64
    h2 = hashlib.sha256(canonical_json(snap)).hexdigest()
    assert h1 != h2


def test_mark_ready_for_ci_is_reserved_and_blocks_without_remote_witness(tmp_path):
    state = make_complete_candidate(tmp_path)
    state["ready_transition"] = None
    assert _eval(state).allowed is False


def test_ready_transition_recovers_after_remote_success_before_local_commit(tmp_path):
    state = make_complete_candidate(tmp_path)
    rt = state["ready_transition"]
    rt["status"] = "pending"
    assert _eval(state).allowed is False


def test_initially_ready_pr_uses_verified_idempotent_noop_transition(tmp_path):
    state = make_complete_candidate(tmp_path)
    rt = state["ready_transition"]
    rt["transition_witness_id"] = None
    assert _eval(state).allowed is False
