#!/usr/bin/env python3
"""Shared helpers and fixtures for the version-2 evidence-kernel tests.

The intake-state builder and malformed fixtures are final. The green-candidate
helpers compose only production constructors from ``review_core.model`` and
materialize real content-addressed evidence files under ``scratch_dir``.
"""

from __future__ import annotations

import base64
import copy
import json
import sys
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from pathlib import Path

TESTS_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(TESTS_DIR.parent / "scripts"))

from review_core import acquisition as acq  # noqa: E402
from review_core import model  # noqa: E402
from review_core import policy  # noqa: E402


def make_empty_v2_state(tmp_path: Path, **overrides: object) -> dict:
    state = {
        "schema_version": 2,
        "review_id": "review-test",
        "generation": 0,
        "status": "active",
        "stage": "intake",
        "scratch_dir": str(tmp_path),
        "snapshot": None,
        "content_objects": {},
        "evidence": {},
        "authorities": {},
        "authority_manifest": None,
        "impact_maps": {},
        "coverage_inventory": None,
        "obligations": {},
        "hypothesis_assignments": {},
        "dispatches": {},
        "reviews": {},
        "route_selections": {},
        "witness_records": {},
        "findings": {},
        "review_repairs": {},
        "checks": {},
        "ready_transition": None,
        "ci_candidate": None,
        "calibration": None,
        "blockers": {},
        "green_seal": None,
        "history": [],
    }
    state.update(copy.deepcopy(overrides))
    return state


def write_v2_state(tmp_path: Path, state: dict) -> Path:
    path = tmp_path / "review-state.json"
    path.write_text(json.dumps(state, indent=2) + "\n", encoding="utf-8")
    return path


MISSING_PREDICATES = (
    "snapshot",
    "authority-manifest",
    "authority",
    "impact-map",
    "coverage-inventory",
    "coverage",
    "preflight",
    "fast-review",
    "focused-review",
    "strong-review",
    "finding-resolution",
    "blind-final-review",
    "closure-audit",
    "remote-ci",
    "remote-head-identity",
    "presentation-recheck",
    "reasoning-floor",
    "witnesses",
)

MALFORMED_STATE_MISSING_VERSION = {
    "review_id": "review-test",
    "generation": 0,
    "status": "active",
}

MALFORMED_STATE_WRONG_VERSION = {
    "schema_version": 1,
    "review_id": "review-test",
    "generation": 0,
    "status": "active",
}

MALFORMED_STATE_UNKNOWN_TOP_LEVEL_KEY = {
    "schema_version": 2,
    "review_id": "review-test",
    "generation": 0,
    "status": "active",
    "stage": "intake",
    "surprise_field": {},
}

MALFORMED_STATE_NONZERO_GENERATION_INTAKE = {
    "schema_version": 2,
    "review_id": "review-test",
    "generation": 7,
    "status": "active",
    "stage": "intake",
}


# ---------------------------------------------------------------------------
# Test-double witness policy / verifier
#
# The registry simulates the composition-root witness source: it maps each
# witness record's source locator to the exact record bytes the source holds.
# Verification is real digest work - it compares the stored record bytes,
# recomputes subject digests, and enforces locator-prefix policy.


class _TestWitnessPolicy:
    PREFIXES = {
        "authority-discovery": "hook-transcript:",
        "profile-resolution": "hook-transcript:",
        "review-launch": "hook-transcript:",
        "review-completion": "hook-transcript:",
        "command-execution": "hook-transcript:",
        "human-decision": "hook-transcript:",
        "remote-transition": "gh:",
        "remote-observation": "gh:",
    }

    def __init__(self, sha256: str):
        self._sha256 = sha256

    @property
    def sha256(self) -> str:
        return self._sha256

    @property
    def sources(self) -> tuple:
        return tuple(
            policy.WitnessSource(
                kind=k,
                source="github-remote" if k.startswith("remote-") else "hook-transcript",
                locator_prefix=p,
            )
            for k, p in self.PREFIXES.items()
        )

    def permits(self, *, kind: str, source: str, locator: str) -> bool:
        prefix = self.PREFIXES.get(kind)
        if prefix is None:
            return False
        expected_source = "github-remote" if kind.startswith("remote-") else "hook-transcript"
        return source == expected_source and locator.startswith(prefix)


class _TestWitnessVerifier:
    """Verifies witness records against a locator-keyed byte registry."""

    def __init__(self, policy_obj, registry: dict, review_id: str):
        self._policy = policy_obj
        self._registry = registry
        self._review_id = review_id

    @property
    def policy(self):
        return self._policy

    def verify(
        self,
        *,
        stored_record_bytes: bytes,
        expected_kind: str,
        expected_review_id: str,
        expected_dispatch_id: str | None,
        expected_snapshot_epoch: int,
        expected_snapshot_fingerprint: str,
        expected_subject: bytes,
        expected_tool_use_id: str | None,
        expected_agent_id: str | None,
    ) -> policy.VerifiedWitness:
        record = model.strict_json_loads(stored_record_bytes, source="witness-record")
        if record.get("kind") != expected_kind:
            raise policy.WitnessVerificationError("witness-mismatch", "kind mismatch")
        if self._review_id != expected_review_id:
            raise policy.WitnessVerificationError("scope-mismatch", "review mismatch")
        if expected_dispatch_id is not None and expected_dispatch_id.encode() not in expected_subject:
            raise policy.WitnessVerificationError("scope-mismatch", "dispatch scope mismatch")
        if record["snapshot_epoch"] != expected_snapshot_epoch:
            raise policy.WitnessVerificationError("scope-mismatch", "epoch mismatch")
        if record["snapshot_fingerprint"] != expected_snapshot_fingerprint:
            raise policy.WitnessVerificationError("scope-mismatch", "fingerprint mismatch")
        locator = record["source_locator"]
        source = "github-remote" if expected_kind.startswith("remote-") else "hook-transcript"
        if not self._policy.permits(kind=expected_kind, source=source, locator=locator):
            raise policy.WitnessVerificationError("locator-untrusted", "locator not permitted")
        stored = self._registry.get(locator)
        if stored is None:
            raise policy.WitnessVerificationError("missing-source", "source has no record at locator")
        if stored != stored_record_bytes:
            raise policy.WitnessVerificationError("witness-mismatch", "source bytes differ from stored record")
        if model.sha256_hex(expected_subject) != record["subject_sha256"]:
            raise policy.WitnessVerificationError("subject-mismatch", "subject digest mismatch")
        if expected_tool_use_id is not None and record.get("tool_use_id") != expected_tool_use_id:
            raise policy.WitnessVerificationError("identity-replay", "tool_use_id mismatch")
        if expected_agent_id is not None and record.get("agent_id") != expected_agent_id:
            raise policy.WitnessVerificationError("identity-replay", "agent_id mismatch")
        return policy.VerifiedWitness(
            witness_id=record["witness_id"],
            kind=record["kind"],
            source=source,
            locator=locator,
            review_id=expected_review_id,
            dispatch_id=expected_dispatch_id,
            snapshot_epoch=record["snapshot_epoch"],
            snapshot_fingerprint=record["snapshot_fingerprint"],
            subject_sha256=record["subject_sha256"],
            tool_use_id=record.get("tool_use_id"),
            agent_id=record.get("agent_id"),
            observed_at=datetime.now(timezone.utc),
            record_sha256=model.sha256_hex(stored_record_bytes),
        )


@dataclass(frozen=True)
class _TestLocalChecks:
    _sha256: str
    _items: tuple

    @property
    def source_id(self) -> str:
        return "test-local-checks"

    @property
    def source_version(self) -> str:
        return "1"

    @property
    def sha256(self) -> str:
        return self._sha256

    @property
    def items(self) -> tuple:
        return self._items


@dataclass(frozen=True)
class _TestAssignments:
    _sha256: str

    @property
    def source_id(self) -> str:
        return "test-assignments"

    @property
    def source_version(self) -> str:
        return "1"

    @property
    def sha256(self) -> str:
        return self._sha256

    def requirement(self, *, state: dict, role: str, assignment_ids: tuple) -> policy.RoleRequirement:
        tier, reasoning = policy._role_floor(role)
        return policy.RoleRequirement(
            capability_tier=tier,
            reasoning_floor=reasoning,
            context_mode="fresh",
            distinct_execution_from=(),
            distinct_role_contract_from=(),
        )


@dataclass(frozen=True)
class _TestCommandExecution:
    _sha256: str

    @property
    def source_id(self) -> str:
        return "test-command-exec"

    @property
    def source_version(self) -> str:
        return "1"

    @property
    def sha256(self) -> str:
        return self._sha256

    def command_intent(self, *, snapshot: dict, item: policy.LocalCheckItem) -> dict:
        return {
            "argv": list(item.command),
            "head_sha": snapshot["head_sha"],
            "policy_item_id": item.policy_item_id,
            "working_directory": item.working_directory,
        }


@dataclass(frozen=True)
class _TestHypotheses:
    _sha256: str

    @property
    def source_id(self) -> str:
        return "test-hypotheses"

    @property
    def source_version(self) -> str:
        return "1"

    @property
    def sha256(self) -> str:
        return self._sha256

    def derive(self, *, obligation: dict) -> tuple:
        base = {
            "family": f"f-{obligation['category']}",
            "derivation_policy_sha256": self._sha256,
            "minimum_capability_tier": obligation["minimum_capability_tier"],
            "minimum_reasoning_floor": obligation["minimum_reasoning_floor"],
        }
        return (
            {**base, "polarity": "claim", "statement": f"{obligation['category']} holds"},
            {**base, "polarity": "counterexample", "statement": f"{obligation['category']} fails"},
        )


class _TestPolicyBundle:
    """Mutable test double satisfying the PolicyBundle surface."""

    def __init__(self, verifier, local_checks, assignments, command_execution, hypotheses):
        self.witness_verifier = verifier
        self.local_checks = local_checks
        self.review_assignments = assignments
        self.command_execution = command_execution
        self.hypotheses = hypotheses
        self.discovery_policy_origin = "external"
        self.available_profiles = ()


_BUNDLES: dict[str, tuple] = {}


# ---------------------------------------------------------------------------
# Complete-candidate construction


def _put(state: dict, payload) -> str:
    raw = model.canonical_json(payload)
    digest = model.sha256_hex(raw)
    store_dir = Path(state["scratch_dir"]) / "evidence-store" / "sha256"
    store_dir.mkdir(parents=True, exist_ok=True)
    (store_dir / digest).write_bytes(raw)
    cid = "sha256:" + digest
    state["content_objects"][cid] = {
        "content_id": cid,
        "path": str(store_dir / digest),
        "sha256": digest,
        "bytes": len(raw),
    }
    return cid


def _bind(state: dict, content_id: str, kind: str, snap=None) -> str:
    snap = state["snapshot"] if snap is None else snap
    rec = {
        "evidence_id": "",
        "content_id": content_id,
        "kind": kind,
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    rec["evidence_id"] = "evidence:" + model.sha256_json(model.evidence_binding_subject(rec))
    state["evidence"][rec["evidence_id"]] = rec
    return rec["evidence_id"]


def _witness(
    state,
    registry,
    *,
    kind,
    subject,
    tool_use_id=None,
    agent_id=None,
    locator=None,
    transcript_range=None,
    snap=None,
):
    snap = state["snapshot"] if snap is None else snap
    if locator is None:
        prefix = "gh:" if kind.startswith("remote-") else "hook-transcript:"
        locator = f"{prefix}{state['review_id']}/{kind}/{len(state['witness_records'])}"
    rec = {
        "witness_id": "",
        "kind": kind,
        "subject_sha256": model.sha256_json(subject),
        "tool_use_id": tool_use_id,
        "agent_id": agent_id,
        "transcript_range": transcript_range,
        "record_positions": [len(state["witness_records"])],
        "chain_head_at_record": model.sha256_hex(f"chain:{locator}".encode()),
        "source_locator": locator,
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    rec["witness_id"] = model.derived_id("witness", snap["epoch"], model.witness_record_subject(rec))
    state["witness_records"][rec["witness_id"]] = rec
    registry[locator] = model.canonical_json(rec)
    return rec["witness_id"]


def _route(state, *, role, profile, profile_sha, tier, reasoning, qualified=None, serial=0):
    snap = state["snapshot"]
    rec = {
        "route_selection_id": "",
        "observed_at": "2026-09-12T00:00:00Z",
        "inventory_evidence_sha256": model.sha256_hex(f"inv:{serial}".encode()),
        "budget_contract_sha256": model.sha256_hex(f"budget:{serial}".encode()),
        "profile_authority_sha256": model.sha256_hex(f"authority:{profile}".encode()),
        "resolved_route_token_sha256": model.sha256_hex(f"token:{profile}:{serial}".encode()),
        "required_capability_tier": tier,
        "required_role": role,
        "qualified_roles": sorted(qualified if qualified is not None else [role]),
        "profile": profile,
        "profile_sha256": profile_sha,
        "selection_mode": "profile",
        "selected_model": "profile-defined",
        "selected_reasoning": reasoning,
        "selected_context_mode": "fresh",
        "parent_model": None,
        "parent_reasoning": None,
        "qualification_source": "profile-file",
        "rationale": "test route",
        "evidence_id": "",
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    cid = _put(state, {"route": profile, "role": role, "serial": serial})
    rec["evidence_id"] = _bind(state, cid, "route-selection")
    rec["route_selection_id"] = model.derived_id("route", snap["epoch"], model.route_selection_subject(rec))
    state["route_selections"][rec["route_selection_id"]] = rec
    return rec


def _dispatch(
    state,
    registry,
    *,
    role,
    profile,
    tier,
    reasoning,
    assignments=(),
    context_evidence=(),
    serial=0,
    qualified=None,
):
    snap = state["snapshot"]
    profile_sha = model.sha256_hex(f"profile:{profile}".encode())
    rs = _route(
        state,
        role=role,
        profile=profile,
        profile_sha=profile_sha,
        tier=tier,
        reasoning=reasoning,
        qualified=qualified,
        serial=serial,
    )
    rsid = rs["route_selection_id"]
    res_witness = _witness(
        state,
        registry,
        kind="profile-resolution",
        subject=model.profile_resolution_subject(rs),
        locator=f"hook-transcript:{state['review_id']}/profile-resolution/{rsid}",
    )
    d = {
        "dispatch_id": "",
        "route_selection_id": rsid,
        "profile_resolution_witness_id": res_witness,
        "launch_witness_id": None,
        "completion_witness_id": None,
        "agent_id": None,
        "tool_use_id": None,
        "transcript_sha256": None,
        "assignment_ids": sorted(assignments),
        "context_evidence_ids": sorted(context_evidence),
        "instruction_manifest_sha256": model.sha256_hex(f"instr:{serial}".encode()),
        "data_manifest_sha256": model.sha256_hex(f"data:{serial}".encode()),
        "tool_confinement_policy_sha256": model.sha256_hex(f"tools:{serial}".encode()),
        "context_package_sha256": model.sha256_hex(f"ctx:{serial}".encode()),
        "hazard_framing_sha256": model.sha256_hex(f"hazard:{serial}".encode()),
        "required_tool_classes": ["git-read", "github-read", "repo-read"],
        "status": "pending",
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    d["dispatch_id"] = model.derived_id("dispatch", snap["epoch"], model.pending_dispatch_intent_subject(d))
    tool_use = f"toolu-launch-{serial}"
    agent = f"agent-{serial}"
    launch_subject = model.review_launch_subject(
        d,
        policy.dispatch_context_manifest(d),
        tool_use_id=tool_use,
        task_bytes_sha256=model.sha256_hex(policy.dispatch_task_bytes(d, rs)),
        profile_name=profile,
    )
    launch_w = _witness(
        state,
        registry,
        kind="review-launch",
        subject=launch_subject,
        tool_use_id=tool_use,
        agent_id=agent,
    )
    transcript_sha = model.sha256_hex(f"transcript:{serial}".encode())
    d["launch_witness_id"] = launch_w
    d["tool_use_id"] = tool_use
    d["agent_id"] = agent
    d["transcript_sha256"] = transcript_sha
    d["status"] = "reported"
    state["dispatches"][d["dispatch_id"]] = d
    return d, rs, transcript_sha, agent


def _attestation_payload(state, registry, d, *, verdict="clean", finding_ids=(), audit="clean", serial=0):
    """Register attestation evidence + completion witness and return the
    attestation record. The record is not installed: callers either install it
    directly (_complete) or pass it to complete_action as a payload."""
    snap = state["snapshot"]
    att_bytes = model.canonical_json({"dispatch_id": d["dispatch_id"], "verdict": verdict, "serial": serial})
    cid = _put(state, json.loads(att_bytes))
    ev = _bind(state, cid, "review-attestation")
    _bind(state, _put(state, {"transcript": serial}), "tool-transcript")
    completion_subject = model.review_completion_subject(
        model.canonical_json(json.loads(att_bytes)),
        tool_transcript_sha256=d["transcript_sha256"],
        agent_id=d["agent_id"],
    )
    comp_w = _witness(
        state,
        registry,
        kind="review-completion",
        subject=completion_subject,
        agent_id=d["agent_id"],
    )
    d["completion_witness_id"] = comp_w
    rec = {
        "attestation_id": "",
        "dispatch_id": d["dispatch_id"],
        "assignment_ids": sorted(d["assignment_ids"]),
        "verdict": verdict,
        "finding_ids": sorted(finding_ids),
        "uncertainties": [],
        "tool_transcript_sha256": d["transcript_sha256"],
        "evidence_id": ev,
        "completion_witness_id": comp_w,
        "audit_result": audit,
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    rec["attestation_id"] = model.derived_id("attestation", snap["epoch"], model.review_wrapper_subject(rec))
    return rec


def _complete(state, registry, d, *, verdict="clean", finding_ids=(), audit="clean", serial=0):
    rec = _attestation_payload(state, registry, d, verdict=verdict, finding_ids=finding_ids, audit=audit, serial=serial)
    state["reviews"][rec["attestation_id"]] = rec
    return rec


def _map_payload(state, *, role, entries):
    snap = state["snapshot"]
    cid = _put(state, {"role": role, "entries": entries})
    ev = _bind(state, cid, "impact-map")
    rec = {
        "impact_map_id": "",
        "role": role,
        "entries": entries,
        "evidence_id": ev,
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    rec["impact_map_id"] = model.derived_id("impact-map", snap["epoch"], model.impact_map_subject(rec))
    return rec


def _map_record(state, *, role, entries):
    rec = _map_payload(state, role=role, entries=entries)
    state["impact_maps"][rec["impact_map_id"]] = rec
    return rec


def _obligation(
    state,
    *,
    category,
    risk,
    consequences,
    status,
    assignees=(),
    evidence_ids=(),
    na_att=(),
    scope_level="surface",
):
    snap = state["snapshot"]
    tier, reasoning = policy.obligation_floor(scope_level, risk, consequences)
    rec = {
        "obligation_id": "",
        "category": category,
        "surfaces": ["src/foo.py"],
        "risk": risk,
        "consequences": list(consequences),
        "scope_level": scope_level,
        "minimum_capability_tier": tier,
        "minimum_reasoning_floor": reasoning,
        "assignees": sorted(assignees),
        "status": status,
        "evidence_ids": list(evidence_ids),
        "not_applicable_attestation_ids": sorted(na_att),
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    rec["obligation_id"] = model.derived_id("obligation", snap["epoch"], model.obligation_subject(rec))
    state["obligations"][rec["obligation_id"]] = rec
    return rec


def _hypothesis(state, *, obligation_id, family, polarity, statement, policy_sha, tier="strong", reasoning="high"):
    snap = state["snapshot"]
    rec = {
        "hypothesis_assignment_id": "",
        "obligation_id": obligation_id,
        "family": family,
        "polarity": polarity,
        "statement": statement,
        "derivation_policy_sha256": policy_sha,
        "minimum_capability_tier": tier,
        "minimum_reasoning_floor": reasoning,
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    rec["hypothesis_assignment_id"] = model.derived_id(
        "hypothesis", snap["epoch"], model.hypothesis_assignment_subject(rec)
    )
    state["hypothesis_assignments"][rec["hypothesis_assignment_id"]] = rec
    return rec


def _finding(
    state,
    *,
    source_kind,
    source_id,
    source_assignment_id,
    obligation_id=None,
    severity="minor",
    disposition="open",
    resolution=None,
    title="finding",
):
    snap = state["snapshot"]
    rec = {
        "finding_id": "",
        "source_kind": source_kind,
        "source_id": source_id,
        "source_assignment_id": source_assignment_id,
        "obligation_id": obligation_id,
        "severity": severity,
        "title": title,
        "description": "desc",
        "locations": ["src/foo.py"],
        "evidence_ids": [],
        "regression_of": None,
        "disposition": disposition,
        "resolution": resolution,
        "discovered_snapshot_epoch": snap["epoch"],
        "discovered_snapshot_fingerprint": snap["fingerprint"],
    }
    rec["finding_id"] = "finding:" + model.sha256_json(model.finding_identity_subject(rec))
    state["findings"][rec["finding_id"]] = rec
    return rec


def _local_check_payload(state, registry, policies, *, kind, item, serial=0):
    """Build a local check record and its command-execution witness; the check
    itself is not installed (complete_action installs it from the payload)."""
    snap = state["snapshot"]
    cid = _put(state, {"check-output": kind, "serial": serial})
    ev = _bind(state, cid, "check-output")
    rec = {
        "check_id": f"check-{kind}-{serial}",
        "kind": kind,
        "locus": "local",
        "policy_item_id": item.policy_item_id,
        "name": item.name,
        "command": list(item.command),
        "working_directory": item.working_directory,
        "local_check_policy_sha256": policies.local_checks.sha256,
        "command_execution_policy_sha256": policies.command_execution.sha256,
        "required": item.required,
        "conclusion": "success",
        "head_sha": snap["head_sha"],
        "source_materialization_sha256": model.sha256_hex(f"mat:{serial}".encode()),
        "toolchain_sha256": model.sha256_hex(f"toolchain:{serial}".encode()),
        "environment_sha256": model.sha256_hex(f"env:{serial}".encode()),
        "sandbox_id": f"sandbox-{serial}",
        "pre_source_sha256": model.sha256_hex(snap["tree_sha"].encode()),
        "post_source_sha256": model.sha256_hex(snap["tree_sha"].encode()),
        "process_tree_terminated": True,
        "evidence_id": ev,
        "execution_witness_id": "",
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    intent = policies.command_execution.command_intent(snapshot=snap, item=item)
    subject = model.command_execution_subject(intent, policy.command_result_subject(rec))
    rec["execution_witness_id"] = _witness(
        state,
        registry,
        kind="command-execution",
        subject=subject,
        tool_use_id=f"toolu-exec-{serial}",
    )
    return rec


def _local_check(state, registry, policies, *, kind, item, serial=0):
    rec = _local_check_payload(state, registry, policies, kind=kind, item=item, serial=serial)
    state["checks"][rec["check_id"]] = rec
    return rec


def make_complete_candidate(tmp_path: Path) -> dict:
    state = make_empty_v2_state(tmp_path)
    registry: dict[str, bytes] = {}
    review_id = state["review_id"]

    # --- sealed policies (test doubles; digests are bound into the snapshot) --
    witness_policy = _TestWitnessPolicy(model.sha256_hex(b"witness-policy"))
    verifier = _TestWitnessVerifier(witness_policy, registry, review_id)
    items = (
        policy.LocalCheckItem("item-preflight", "preflight", ("py", "-3", "tools/run.py", "ci", "--check"), ".", True),
        policy.LocalCheckItem("item-targeted", "targeted", ("py", "-3", "-m", "pytest", "-q"), ".", False),
    )
    local_checks = _TestLocalChecks(model.sha256_hex(b"local-checks"), items)
    assignments_policy = _TestAssignments(model.sha256_hex(b"assignments"))
    command_exec = _TestCommandExecution(model.sha256_hex(b"command-exec"))
    hypotheses = _TestHypotheses(model.sha256_hex(b"hypotheses"))
    bundle = _TestPolicyBundle(verifier, local_checks, assignments_policy, command_exec, hypotheses)

    # --- authorities / manifest ----------------------------------------------
    auth_bytes = model.canonical_json({"path": "AGENTS.md", "note": "repo law"})
    auth_sha = model.sha256_hex(auth_bytes)
    auth_entry = {
        "authority_id": "auth-agents",
        "kind": "repo-law",
        "locator": "AGENTS.md",
        "availability": "loaded",
        "sha256": auth_sha,
        "failure_class": None,
        "failure_sha256": None,
    }
    empty_feedback_sha = model.sha256_hex(model.canonical_json([]))
    manifest_payload = model.manifest_payload(
        repository_id="o/r",
        pr_number=7,
        pr_url="https://github.com/o/r/pull/7",
        authority_discovery_policy_id="adp",
        authority_discovery_policy_version="1",
        authority_discovery_policy_sha256=model.sha256_hex(b"adp"),
        authorities=(auth_entry,),
        feedback_history_policy_id="fhp",
        feedback_history_policy_version="1",
        feedback_history_policy_sha256=model.sha256_hex(b"fhp"),
        feedback_history_sha256=empty_feedback_sha,
        local_check_policy_id=local_checks.source_id,
        local_check_policy_version=local_checks.source_version,
        local_check_policy_sha256=local_checks.sha256,
        required_check_policy_sha256=model.sha256_hex(b"required-checks"),
        review_assignment_policy_id=assignments_policy.source_id,
        review_assignment_policy_version=assignments_policy.source_version,
        review_assignment_policy_sha256=assignments_policy.sha256,
        command_execution_policy_id=command_exec.source_id,
        command_execution_policy_version=command_exec.source_version,
        command_execution_policy_sha256=command_exec.sha256,
        evidence_ingestion_policy_id="eip",
        evidence_ingestion_policy_version="1",
        evidence_ingestion_policy_sha256=model.sha256_hex(b"eip"),
        hypothesis_derivation_policy_id=hypotheses.source_id,
        hypothesis_derivation_policy_version=hypotheses.source_version,
        hypothesis_derivation_policy_sha256=hypotheses.sha256,
        unresolved_feedback_sha256=empty_feedback_sha,
    )
    manifest_id = model.authority_manifest_id(manifest_payload)

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
        "authority_manifest_sha256": manifest_id,
        "authority_discovery_policy_id": "adp",
        "authority_discovery_policy_version": "1",
        "authority_discovery_policy_sha256": model.sha256_hex(b"adp"),
        "witness_policy_sha256": witness_policy.sha256,
        "feedback_history_policy_id": "fhp",
        "feedback_history_policy_version": "1",
        "feedback_history_policy_sha256": model.sha256_hex(b"fhp"),
        "feedback_history_sha256": empty_feedback_sha,
        "local_check_policy_id": local_checks.source_id,
        "local_check_policy_version": local_checks.source_version,
        "local_check_policy_sha256": local_checks.sha256,
        "required_check_policy_sha256": model.sha256_hex(b"required-checks"),
        "review_assignment_policy_id": assignments_policy.source_id,
        "review_assignment_policy_version": assignments_policy.source_version,
        "review_assignment_policy_sha256": assignments_policy.sha256,
        "command_execution_policy_id": command_exec.source_id,
        "command_execution_policy_version": command_exec.source_version,
        "command_execution_policy_sha256": command_exec.sha256,
        "evidence_ingestion_policy_id": "eip",
        "evidence_ingestion_policy_version": "1",
        "evidence_ingestion_policy_sha256": model.sha256_hex(b"eip"),
        "hypothesis_derivation_policy_id": hypotheses.source_id,
        "hypothesis_derivation_policy_version": hypotheses.source_version,
        "hypothesis_derivation_policy_sha256": hypotheses.sha256,
        "unresolved_feedback_sha256": empty_feedback_sha,
    }
    snap["fingerprint"] = model.snapshot_fingerprint(snap)
    state["snapshot"] = snap
    epoch, fp = snap["epoch"], snap["fingerprint"]

    # manifest evidence + wrapper (discovery witness first, then payload)
    manifest_cid = _put(state, manifest_payload)
    manifest_ev = _bind(state, manifest_cid, "authority-manifest-payload")
    discovery_w = _witness(
        state,
        registry,
        kind="authority-discovery",
        subject=model.authority_discovery_subject(snap, manifest_payload),
    )
    state["authority_manifest"] = {
        "authority_manifest_id": manifest_id,
        "payload_evidence_id": manifest_ev,
        "discovery_witness_id": discovery_w,
        "snapshot_epoch": epoch,
        "snapshot_fingerprint": fp,
    }
    auth_cid = _put(state, json.loads(auth_bytes))
    auth_ev = _bind(state, auth_cid, "authority")
    state["authorities"]["auth-agents"] = {
        "authority_id": "auth-agents",
        "kind": "repo-law",
        "locator": "AGENTS.md",
        "availability": "loaded",
        "sha256": auth_sha,
        "evidence_id": auth_ev,
        "snapshot_epoch": epoch,
        "snapshot_fingerprint": fp,
    }

    # --- impact maps ----------------------------------------------------------
    surface = "src/foo.py"
    sem_map = _map_record(
        state,
        role="impact-mapper-semantic",
        entries=[
            {
                "surface": surface,
                "category": "behavioral-correctness",
                "hazards": ["h-sem"],
                "consequences": ["security"],
            }
        ],
    )
    con_map = _map_record(
        state,
        role="impact-mapper-contract",
        entries=[
            {
                "surface": surface,
                "category": "documentation-contract",
                "hazards": ["h-con"],
                "consequences": ["security"],
            }
        ],
    )
    # mapper dispatches/reviews
    d_sem, _, _, _ = _dispatch(
        state,
        registry,
        role="impact-mapper-semantic",
        profile="mapper-semantic",
        tier="strong",
        reasoning="high",
        serial=1,
    )
    _complete(state, registry, d_sem, serial=1)
    d_con, _, _, _ = _dispatch(
        state,
        registry,
        role="impact-mapper-contract",
        profile="mapper-contract",
        tier="strong",
        reasoning="high",
        serial=2,
    )
    _complete(state, registry, d_con, serial=2)

    # --- challenger dispatch + review (needed before inventory) --------------
    d_chall, _, _, _ = _dispatch(
        state,
        registry,
        role="scope-challenger",
        profile="challenger",
        tier="final-strong",
        reasoning="final-strong",
        context_evidence=(sem_map["evidence_id"], con_map["evidence_id"]),
        serial=3,
    )
    chall_att = _complete(state, registry, d_chall, serial=3)

    # --- hypothesis assignments + obligations ---------------------------------
    categories = list(model.OBLIGATION_CATEGORIES)
    obligations = {}
    hyps = {}
    for cat in categories:
        if cat == "security-privacy":
            risk, status, scope, cons = "high", "not-applicable", "surface", ["security"]
        elif cat == "documentation-contract":
            risk, status, scope, cons = "low", "covered", "hunk", ["none"]
        elif cat == "performance-resources":
            risk, status, scope, cons = "low", "covered", "surface", ["none"]
        else:
            risk, status, scope, cons = "low", "covered", "cross-surface", ["security"]
        o = _obligation(
            state,
            category=cat,
            risk=risk,
            consequences=cons,
            status=status,
            scope_level=scope,
        )
        obligations[cat] = o
        h = _hypothesis(
            state,
            obligation_id=o["obligation_id"],
            family=f"f-{cat}",
            polarity="claim",
            statement=f"{cat} holds",
            policy_sha=hypotheses.sha256,
            tier=o["minimum_capability_tier"],
            reasoning=o["minimum_reasoning_floor"],
        )
        hyps[cat] = h
        o["assignees"] = sorted([h["hypothesis_assignment_id"]])
        # re-derive id since assignees is part of the subject
        del state["obligations"][o["obligation_id"]]
        o["obligation_id"] = model.derived_id("obligation", epoch, model.obligation_subject(o))
        state["obligations"][o["obligation_id"]] = o
        h["obligation_id"] = o["obligation_id"]
        del state["hypothesis_assignments"][h["hypothesis_assignment_id"]]
        h["hypothesis_assignment_id"] = model.derived_id("hypothesis", epoch, model.hypothesis_assignment_subject(h))
        state["hypothesis_assignments"][h["hypothesis_assignment_id"]] = h

    high = obligations["security-privacy"]
    h2 = _hypothesis(
        state,
        obligation_id=high["obligation_id"],
        family="f-security-privacy",
        polarity="counterexample",
        statement="security-privacy fails",
        policy_sha=hypotheses.sha256,
        tier=high["minimum_capability_tier"],
        reasoning=high["minimum_reasoning_floor"],
    )
    del state["obligations"][high["obligation_id"]]
    high["assignees"] = sorted([hyps["security-privacy"]["hypothesis_assignment_id"], h2["hypothesis_assignment_id"]])
    high["obligation_id"] = model.derived_id("obligation", epoch, model.obligation_subject(high))
    state["obligations"][high["obligation_id"]] = high
    for h in (hyps["security-privacy"], h2):
        del state["hypothesis_assignments"][h["hypothesis_assignment_id"]]
        h["obligation_id"] = high["obligation_id"]
        h["hypothesis_assignment_id"] = model.derived_id("hypothesis", epoch, model.hypothesis_assignment_subject(h))
        state["hypothesis_assignments"][h["hypothesis_assignment_id"]] = h

    # --- coverage inventory ---------------------------------------------------
    inv_entries = [
        {
            "surface": surface,
            "categories": sorted(categories),
            "hazards": ["h-con", "h-sem"],
            "consequences": ["security"],
            "obligation_ids": sorted(o["obligation_id"] for o in obligations.values()),
        }
    ]
    inv_cid = _put(state, {"entries": inv_entries})
    inv_ev = _bind(state, inv_cid, "scope-challenge")
    inv = {
        "coverage_inventory_id": "",
        "semantic_impact_map_id": sem_map["impact_map_id"],
        "contract_impact_map_id": con_map["impact_map_id"],
        "challenger_attestation_id": chall_att["attestation_id"],
        "entries": inv_entries,
        "evidence_id": inv_ev,
        "snapshot_epoch": epoch,
        "snapshot_fingerprint": fp,
    }
    inv["coverage_inventory_id"] = model.derived_id("coverage-inventory", epoch, model.coverage_inventory_subject(inv))
    state["coverage_inventory"] = inv

    # --- obligation reviews ----------------------------------------------------
    # Tier gates are separate ordered dispatches: fast, focused, strong.
    covered_ids = sorted(o["obligation_id"] for c, o in obligations.items() if c != "security-privacy")
    fast_ids = sorted(
        o["obligation_id"]
        for o in obligations.values()
        if o["minimum_capability_tier"] == "fast" and o["status"] == "covered"
    )
    focused_ids = sorted(
        o["obligation_id"]
        for o in obligations.values()
        if o["minimum_capability_tier"] == "focused" and o["status"] == "covered"
    )
    strong_ids = sorted(oid for oid in covered_ids if oid not in fast_ids + focused_ids)
    d_fast, _, _, _ = _dispatch(
        state,
        registry,
        role="obligation-reviewer",
        profile="reviewer-fast",
        tier="fast",
        reasoning="low",
        assignments=fast_ids,
        serial=9,
    )
    _complete(state, registry, d_fast, serial=9)
    d_focused, _, _, _ = _dispatch(
        state,
        registry,
        role="obligation-reviewer",
        profile="reviewer-focused",
        tier="focused",
        reasoning="standard",
        assignments=focused_ids,
        serial=19,
    )
    _complete(state, registry, d_focused, serial=19)
    d_rev_a, _, _, _ = _dispatch(
        state,
        registry,
        role="obligation-reviewer",
        profile="reviewer-a",
        tier="strong",
        reasoning="high",
        assignments=strong_ids + [high["obligation_id"]],
        serial=10,
    )
    att_a = _complete(state, registry, d_rev_a, serial=10)
    d_rev_b, _, _, _ = _dispatch(
        state,
        registry,
        role="obligation-reviewer",
        profile="reviewer-b",
        tier="strong",
        reasoning="high",
        assignments=[high["obligation_id"]] + [h["hypothesis_assignment_id"] for h in (hyps["security-privacy"], h2)],
        serial=11,
    )
    att_b = _complete(state, registry, d_rev_b, serial=11)
    d_ex, _, _, _ = _dispatch(
        state,
        registry,
        role="exemption-challenger",
        profile="exemption-challenger",
        tier="final-strong",
        reasoning="final-strong",
        assignments=[high["obligation_id"]],
        serial=12,
    )
    att_ex = _complete(state, registry, d_ex, serial=12)

    # attach N/A attestations to the high-risk obligation
    del state["obligations"][high["obligation_id"]]
    high["not_applicable_attestation_ids"] = sorted(
        [att_a["attestation_id"], att_b["attestation_id"], att_ex["attestation_id"]]
    )
    high["obligation_id"] = model.derived_id("obligation", epoch, model.obligation_subject(high))
    state["obligations"][high["obligation_id"]] = high
    for h in (hyps["security-privacy"], h2):
        del state["hypothesis_assignments"][h["hypothesis_assignment_id"]]
        h["obligation_id"] = high["obligation_id"]
        h["hypothesis_assignment_id"] = model.derived_id("hypothesis", epoch, model.hypothesis_assignment_subject(h))
        state["hypothesis_assignments"][h["hypothesis_assignment_id"]] = h
    # inventory obligation_ids must reference the final obligation ids
    inv["entries"][0]["obligation_ids"] = sorted(o["obligation_id"] for o in obligations.values())
    inv["coverage_inventory_id"] = model.derived_id("coverage-inventory", epoch, model.coverage_inventory_subject(inv))
    state["coverage_inventory"] = inv

    # --- checks ----------------------------------------------------------------
    _local_check(state, registry, bundle, kind="preflight", item=items[0], serial=20)
    targeted = _local_check(state, registry, bundle, kind="targeted", item=items[1], serial=21)

    # --- adjudicator / fix / repair --------------------------------------------
    d_adj, _, _, _ = _dispatch(
        state,
        registry,
        role="finding-adjudicator",
        profile="adjudicator",
        tier="strong",
        reasoning="high",
        serial=13,
    )
    att_adj = _complete(state, registry, d_adj, serial=13)

    f_fixed = _finding(
        state,
        source_kind="review",
        source_id=att_a["attestation_id"],
        source_assignment_id=high["obligation_id"],
        obligation_id=high["obligation_id"],
        disposition="fixed",
        resolution={"review_id": "", "check_id": targeted["check_id"]},
        title="fixed-finding",
    )
    f_repaired = _finding(
        state,
        source_kind="review",
        source_id=att_a["attestation_id"],
        source_assignment_id=high["obligation_id"],
        obligation_id=high["obligation_id"],
        disposition="review-repaired",
        resolution={"review_id": att_adj["attestation_id"]},
        title="repaired-finding",
    )
    # fix-reviewer dispatch assigned the fixed finding
    d_fix, _, _, _ = _dispatch(
        state,
        registry,
        role="fix-reviewer",
        profile="fix-reviewer",
        tier="focused",
        reasoning="standard",
        assignments=[f_fixed["finding_id"]],
        serial=14,
    )
    att_fix = _complete(state, registry, d_fix, serial=14)
    f_fixed["resolution"]["review_id"] = att_fix["attestation_id"]

    # Provider-resolved feedback still materializes as a finding and closes
    # through the ordinary lifecycle (false-positive via adjudication).
    _finding(
        state,
        source_kind="feedback",
        source_id="gh:pr:7:thread:1",
        source_assignment_id="gh:pr:7:thread:1",
        obligation_id=None,
        disposition="false-positive",
        resolution={"review_id": att_adj["attestation_id"]},
        title="provider-feedback",
    )

    # the repaired review produced a superseded attestation that the closed
    # repair's cut invalidated; the obligation now relies on the replacements.
    d_old, _, _, _ = _dispatch(
        state,
        registry,
        role="obligation-reviewer",
        profile="reviewer-old",
        tier="strong",
        reasoning="high",
        assignments=[high["obligation_id"]],
        serial=18,
    )
    att_old = _complete(state, registry, d_old, serial=18)

    d_rv, _, _, _ = _dispatch(
        state,
        registry,
        role="review-repair-verifier",
        profile="repair-verifier",
        tier="strong",
        reasoning="high",
        assignments=[f_repaired["finding_id"]],
        serial=15,
    )
    att_rv = _complete(state, registry, d_rv, serial=15)
    state["review_repairs"]["repair-1"] = {
        "repair_id": "repair-1",
        "finding_id": f_repaired["finding_id"],
        "target_kind": "obligation-review",
        "target_ids": [att_old["attestation_id"]],
        "invalidated_record_ids": [att_old["attestation_id"]],
        "entry_adjudicator_attestation_id": att_adj["attestation_id"],
        "status": "closed",
        "verification_attestation_id": att_rv["attestation_id"],
        "snapshot_epoch": epoch,
        "snapshot_fingerprint": fp,
    }

    # --- blind final + closure -------------------------------------------------
    d_fin, _, _, _ = _dispatch(
        state,
        registry,
        role="blind-final",
        profile="blind-final-profile",
        tier="final-strong",
        reasoning="final-strong",
        qualified=("blind-final",),
        serial=16,
    )
    _complete(state, registry, d_fin, serial=16)
    d_clo, _, _, _ = _dispatch(
        state,
        registry,
        role="closure-auditor",
        profile="closure-auditor-profile",
        tier="final-strong",
        reasoning="final-strong",
        qualified=("closure-auditor",),
        serial=17,
    )
    _complete(state, registry, d_clo, serial=17)

    # --- remote transition + hosted check + observation -------------------------
    observation = {
        "repository_id": "o/r",
        "pr_number": 7,
        "pr_url": "https://github.com/o/r/pull/7",
        "head_sha": snap["head_sha"],
        "lifecycle_state": "ready",
        "authority_manifest_sha256": manifest_id,
        "unresolved_feedback_sha256": empty_feedback_sha,
        "check_runs": [
            {
                "check_run_id": "cr-1",
                "name": "ci",
                "app_id": "github-actions",
                "status": "completed",
                "conclusion": "success",
                "head_sha": snap["head_sha"],
            }
        ],
        "workflow_runs": [
            {
                "workflow_run_id": "wr-1",
                "name": "ci",
                "status": "completed",
                "conclusion": "success",
                "head_sha": snap["head_sha"],
                "run_attempt": 1,
                "run_number": 1,
                "event": "pull_request",
            }
        ],
        "observed_at": "2026-09-12T00:00:00+00:00",
    }
    obs_cid = _put(state, observation)
    obs_ev = _bind(state, obs_cid, "remote-observation")

    idem = model.sha256_hex(b"idem")
    ready = {
        "ready_transition_id": f"ready:{epoch}:{idem}",
        "idempotency_key": idem,
        "repository_id": "o/r",
        "pr_number": 7,
        "head_sha": snap["head_sha"],
        "prior_lifecycle_state": "draft",
        "expected_lifecycle_state": "ready",
        "transition_witness_id": None,
        "status": "completed",
        "snapshot_epoch": epoch,
        "snapshot_fingerprint": fp,
    }
    trans_subject = model.remote_transition_subject(
        ready,
        tool_use_id="toolu-ready-30",
        prior_lifecycle_state="draft",
        result_lifecycle_state="ready",
    )
    trans_w = _witness(
        state,
        registry,
        kind="remote-transition",
        subject=trans_subject,
        tool_use_id="toolu-ready-30",
        locator=f"gh:{review_id}/remote-transition/30",
    )
    ready["transition_witness_id"] = trans_w
    state["ready_transition"] = ready

    obs_w = _witness(
        state,
        registry,
        kind="remote-observation",
        subject=model.remote_observation_subject(observation),
        locator=f"gh:{review_id}/remote-observation/31",
    )
    hosted = {
        "check_id": "check-remote-ci-0",
        "kind": "remote-ci",
        "locus": "hosted",
        "policy_item_id": "ci",
        "name": "ci",
        "required": True,
        "conclusion": "success",
        "head_sha": snap["head_sha"],
        "app_id": "github-actions",
        "workflow_id": "wf-1",
        "workflow_path": ".github/workflows/ci.yml",
        "workflow_definition_ref": "refs/heads/x",
        "workflow_definition_sha": "d" * 40,
        "event": "pull_request",
        "trigger_subject": "pr",
        "policy_inputs_sha256": "0" * 64,
        "configuration_sha256": "0" * 64,
        "check_run_id": "cr-1",
        "workflow_run_id": "wr-1",
        "run_attempt": 1,
        "evidence_id": obs_ev,
        "remote_observation_witness_id": obs_w,
        "snapshot_epoch": epoch,
        "snapshot_fingerprint": fp,
    }
    state["checks"][hosted["check_id"]] = hosted

    ci = {
        "ci_candidate_id": "",
        "repository_id": "o/r",
        "pr_number": 7,
        "head_sha": snap["head_sha"],
        "lifecycle_state": "ready",
        "transition_witness_id": trans_w,
        "snapshot_epoch": epoch,
        "snapshot_fingerprint": fp,
    }
    ci["ci_candidate_id"] = model.derived_id("ci-candidate", epoch, model.ci_candidate_subject(ci))
    state["ci_candidate"] = ci

    # --- bookkeeping -------------------------------------------------------------
    state["generation"] = 1
    from review_core import store as _store

    _store.append_history(state, event="build", data_sha256=model.sha256_hex(b"build"))
    state["stage"] = policy._derived_stage(state, bundle)

    profile_names = tuple(rs["profile"] for rs in state["route_selections"].values())
    bundle.available_profiles = profile_names
    _BUNDLES[review_id] = (bundle, registry)

    model.validate_state(state)
    return state


def make_remote_observation(state: dict) -> tuple[dict, datetime]:
    """Return the stored remote observation plus a fresh `now` (+5s).

    When several remote-observation records exist (a re-observation after a
    hosted-check repair), prefer the evidence a current hosted remote-ci
    check binds, matching what `_stored_remote_observation` selects."""
    bound = {
        c["evidence_id"]
        for c in state["checks"].values()
        if c["kind"] == "remote-ci" and c["locus"] == "hosted" and policy._current(state, c, c["check_id"])
    }
    first = None
    for eid, ev in state["evidence"].items():
        if ev["kind"] != "remote-observation":
            continue
        obs = model.strict_json_loads(
            Path(state["content_objects"][ev["content_id"]]["path"]).read_bytes(),
            source="remote-observation",
        )
        if eid in bound:
            observed_at = datetime.fromisoformat(obs["observed_at"])
            return obs, observed_at + timedelta(seconds=5)
        if first is None:
            first = obs
    if first is not None:
        return first, datetime.fromisoformat(first["observed_at"]) + timedelta(seconds=5)
    raise AssertionError("candidate has no remote-observation evidence")


def make_policy_bundle(state: dict):
    return _BUNDLES[state["review_id"]][0]


def remove_predicate(state: dict, observation: dict, missing: str) -> None:
    """Remove exactly one named green proof while keeping the state coherent."""
    if missing == "snapshot":
        state["snapshot"] = None
    elif missing == "authority-manifest":
        state["authority_manifest"] = None
    elif missing == "authority":
        state["authorities"] = {}
    elif missing == "impact-map":
        state["impact_maps"] = {}
    elif missing == "coverage-inventory":
        state["coverage_inventory"] = None
    elif missing == "coverage":
        state["obligations"] = {}
    elif missing == "preflight":
        state["checks"] = {k: c for k, c in state["checks"].items() if c["kind"] != "preflight"}
    elif missing in ("fast-review", "focused-review", "strong-review"):
        tier = missing.split("-")[0]
        for o in state["obligations"].values():
            if o["minimum_capability_tier"] == tier and o["status"] == "covered":
                o["status"] = "pending"
        for rid in list(state["reviews"]):
            d = state["dispatches"].get(state["reviews"][rid]["dispatch_id"])
            if d is None:
                continue
            rs = state["route_selections"][d["route_selection_id"]]
            if rs["required_role"] == "obligation-reviewer" and rs["required_capability_tier"] == tier:
                del state["reviews"][rid]
    elif missing == "finding-resolution":
        for f in state["findings"].values():
            f["disposition"] = "open"
            f["resolution"] = None
    elif missing == "blind-final-review":
        for r in list(state["reviews"]):
            d = state["dispatches"].get(state["reviews"][r]["dispatch_id"])
            if d is not None and state["route_selections"][d["route_selection_id"]]["required_role"] == "blind-final":
                del state["reviews"][r]
    elif missing == "closure-audit":
        for r in list(state["reviews"]):
            d = state["dispatches"].get(state["reviews"][r]["dispatch_id"])
            if (
                d is not None
                and state["route_selections"][d["route_selection_id"]]["required_role"] == "closure-auditor"
            ):
                del state["reviews"][r]
    elif missing == "remote-ci":
        state["checks"] = {k: c for k, c in state["checks"].items() if c["kind"] != "remote-ci"}
    elif missing == "remote-head-identity":
        observation["head_sha"] = "0" * 40
    elif missing == "presentation-recheck":
        observation["check_runs"] = []
        observation["workflow_runs"] = []
    elif missing == "reasoning-floor":
        for d in state["dispatches"].values():
            rs = state["route_selections"][d["route_selection_id"]]
            rs["selected_reasoning"] = "low"
    elif missing == "witnesses":
        for w in state["witness_records"].values():
            w["source_locator"] = "untrusted:" + w["source_locator"]
    else:
        raise AssertionError(f"unknown predicate {missing!r}")


def _witness_bytes(
    state,
    registry,
    *,
    kind,
    subject,
    tool_use_id=None,
    agent_id=None,
    locator=None,
    transcript_range=None,
    snap=None,
):
    """Build a witness record and return its canonical bytes without
    installing it; the bytes are both the registry entry and the input to the
    record_* ingestion APIs."""
    snap = state["snapshot"] if snap is None else snap
    if locator is None:
        prefix = "gh:" if kind.startswith("remote-") else "hook-transcript:"
        locator = f"{prefix}{state['review_id']}/{kind}/{len(state['witness_records'])}"
    rec = {
        "witness_id": "",
        "kind": kind,
        "subject_sha256": model.sha256_json(subject),
        "tool_use_id": tool_use_id,
        "agent_id": agent_id,
        "transcript_range": transcript_range,
        "record_positions": [len(state["witness_records"])],
        "chain_head_at_record": model.sha256_hex(f"chain:{locator}".encode()),
        "source_locator": locator,
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    rec["witness_id"] = model.derived_id("witness", snap["epoch"], model.witness_record_subject(rec))
    raw = model.canonical_json(rec)
    registry[locator] = raw
    return raw


# ---------------------------------------------------------------------------
# complete_action walk drivers
#
# The fixture builders above install records directly for green-path tamper
# tests. The walk below instead drives the real ingestion surface:
# register_dispatch -> record_launch -> record_completion -> complete_action,
# asserting each action is the derived next action and appends exactly one
# history event with one generation increment.


def _route_payload(state, *, role, profile, profile_sha, tier, reasoning, qualified=None, serial=0):
    """Route-selection payload for register_dispatch (ids derived by policy)."""
    snap = state["snapshot"]
    rec = {
        "route_selection_id": "",
        "observed_at": "2026-09-12T00:00:00Z",
        "inventory_evidence_sha256": model.sha256_hex(f"inv:{serial}".encode()),
        "budget_contract_sha256": model.sha256_hex(f"budget:{serial}".encode()),
        "profile_authority_sha256": model.sha256_hex(f"authority:{profile}".encode()),
        "resolved_route_token_sha256": model.sha256_hex(f"token:{profile}:{serial}".encode()),
        "required_capability_tier": tier,
        "required_role": role,
        "qualified_roles": sorted(qualified if qualified is not None else [role]),
        "profile": profile,
        "profile_sha256": profile_sha,
        "selection_mode": "profile",
        "selected_model": "profile-defined",
        "selected_reasoning": reasoning,
        "selected_context_mode": "fresh",
        "parent_model": None,
        "parent_reasoning": None,
        "qualification_source": "profile-file",
        "rationale": "test route",
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    cid = _put(state, {"route": profile, "role": role, "serial": serial})
    rec["evidence_id"] = _bind(state, cid, "route-selection")
    return rec


def _finding_payload(
    state,
    *,
    source_kind,
    source_id,
    source_assignment_id,
    obligation_id=None,
    severity="minor",
    disposition="open",
    resolution=None,
    title="finding",
):
    """Finding payload for review/adjudication actions; the install path
    re-derives finding_id, so the returned record carries the final id."""
    snap = state["snapshot"]
    rec = {
        "finding_id": "",
        "source_kind": source_kind,
        "source_id": source_id,
        "source_assignment_id": source_assignment_id,
        "obligation_id": obligation_id,
        "severity": severity,
        "title": title,
        "description": "desc",
        "locations": ["src/foo.py"],
        "evidence_ids": [],
        "regression_of": None,
        "disposition": disposition,
        "resolution": resolution,
        "discovered_snapshot_epoch": snap["epoch"],
        "discovered_snapshot_fingerprint": snap["fingerprint"],
    }
    rec["finding_id"] = "finding:" + model.sha256_json(model.finding_identity_subject(rec))
    return rec


def _obligation_payload(
    state,
    *,
    category,
    risk,
    consequences,
    status,
    scope_level="surface",
    assignees=(),
    evidence_ids=(),
    na_att=(),
    surfaces=None,
):
    snap = state["snapshot"]
    tier, reasoning = policy.obligation_floor(scope_level, risk, consequences)
    return {
        "obligation_id": "",
        "category": category,
        "surfaces": sorted(surfaces or ["src/foo.py"]),
        "risk": risk,
        "consequences": list(consequences),
        "scope_level": scope_level,
        "minimum_capability_tier": tier,
        "minimum_reasoning_floor": reasoning,
        "assignees": sorted(assignees),
        "status": status,
        "evidence_ids": list(evidence_ids),
        "not_applicable_attestation_ids": sorted(na_att),
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }


def _attestation_id(state, att: dict) -> str:
    return model.derived_id(
        "attestation",
        state["snapshot"]["epoch"],
        model.review_wrapper_subject({**att, "attestation_id": ""}),
    )


class _Walk:
    """Drive a live v2 state through the real ingestion + complete_action
    surface, asserting lawful ordering and one-history-event semantics."""

    def __init__(self, tmp_path):
        self.state = make_empty_v2_state(tmp_path)
        self.registry: dict[str, bytes] = {}
        self.witness_policy = _TestWitnessPolicy(model.sha256_hex(b"witness-policy"))
        self.verifier = _TestWitnessVerifier(self.witness_policy, self.registry, self.state["review_id"])
        self.items = (
            policy.LocalCheckItem(
                "item-preflight",
                "preflight",
                ("py", "-3", "tools/run.py", "ci", "--check"),
                ".",
                True,
            ),
            policy.LocalCheckItem(
                "item-targeted",
                "targeted",
                ("py", "-3", "-m", "pytest", "-q"),
                ".",
                False,
            ),
        )
        self.local_checks = _TestLocalChecks(model.sha256_hex(b"local-checks"), self.items)
        self.assignments_policy = _TestAssignments(model.sha256_hex(b"assignments"))
        self.command_exec = _TestCommandExecution(model.sha256_hex(b"command-exec"))
        self.hypotheses = _TestHypotheses(model.sha256_hex(b"hypotheses"))
        self.policies = _TestPolicyBundle(
            self.verifier,
            self.local_checks,
            self.assignments_policy,
            self.command_exec,
            self.hypotheses,
        )
        self.serial = 0
        self.steps: list[str] = []

    # -- primitives ----------------------------------------------------------

    def _next_serial(self) -> int:
        self.serial += 1
        return self.serial

    def run(self, action: str, data: dict, *, sole: bool = True):
        decision = policy.next_action(self.state, policies=self.policies)
        lawful = policy._lawful_actions(self.state, self.policies)
        assert action in lawful, f"{action!r} not lawful here; lawful={sorted(lawful)} (next={decision.action!r})"
        if sole:
            assert decision.action == action, (
                f"expected {action!r}, derived {decision.action!r} "
                f"(missing={decision.missing}, reason={decision.reason})"
            )
        hist = len(self.state["history"])
        self.last_raw = model.canonical_json(data)
        self.last_action = action
        self.state = policy.complete_action(
            self.state,
            action=action,
            raw_data=self.last_raw,
            policies=self.policies,
        )
        assert len(self.state["history"]) == hist + 1, action
        assert self.state["history"][-1]["event"] == action
        self.steps.append((action, decision.action, self.state["stage"]))

    def intake(self, *, epoch: int, head_sha: str):
        """Build and register the intake evidence/witnesses for `epoch`;
        returns the freeze/refresh/enter-fixing payload pieces."""
        state, registry = self.state, self.registry
        auth_bytes = model.canonical_json({"path": "AGENTS.md", "note": "repo law", "epoch": epoch})
        auth_sha = model.sha256_hex(auth_bytes)
        empty_feedback_sha = model.sha256_hex(model.canonical_json([]))
        auth_entry = {
            "authority_id": "auth-agents",
            "kind": "repo-law",
            "locator": "AGENTS.md",
            "availability": "loaded",
            "sha256": auth_sha,
            "failure_class": None,
            "failure_sha256": None,
        }
        manifest_payload = model.manifest_payload(
            repository_id="o/r",
            pr_number=7,
            pr_url="https://github.com/o/r/pull/7",
            authority_discovery_policy_id="adp",
            authority_discovery_policy_version="1",
            authority_discovery_policy_sha256=model.sha256_hex(b"adp"),
            authorities=(auth_entry,),
            feedback_history_policy_id="fhp",
            feedback_history_policy_version="1",
            feedback_history_policy_sha256=model.sha256_hex(b"fhp"),
            feedback_history_sha256=empty_feedback_sha,
            local_check_policy_id=self.local_checks.source_id,
            local_check_policy_version=self.local_checks.source_version,
            local_check_policy_sha256=self.local_checks.sha256,
            required_check_policy_sha256=model.sha256_hex(b"required-checks"),
            review_assignment_policy_id=self.assignments_policy.source_id,
            review_assignment_policy_version=self.assignments_policy.source_version,
            review_assignment_policy_sha256=self.assignments_policy.sha256,
            command_execution_policy_id=self.command_exec.source_id,
            command_execution_policy_version=self.command_exec.source_version,
            command_execution_policy_sha256=self.command_exec.sha256,
            evidence_ingestion_policy_id="eip",
            evidence_ingestion_policy_version="1",
            evidence_ingestion_policy_sha256=model.sha256_hex(b"eip"),
            hypothesis_derivation_policy_id=self.hypotheses.source_id,
            hypothesis_derivation_policy_version=self.hypotheses.source_version,
            hypothesis_derivation_policy_sha256=self.hypotheses.sha256,
            unresolved_feedback_sha256=empty_feedback_sha,
        )
        manifest_id = model.authority_manifest_id(manifest_payload)
        snap = {
            "epoch": epoch,
            "repository_id": "o/r",
            "pr_number": 7,
            "pr_url": "https://github.com/o/r/pull/7",
            "git_object_format": "sha1",
            "base_sha": "a" * 40,
            "head_sha": head_sha,
            "tree_sha": "c" * 40,
            "diff_sha256": "d" * 64,
            "pr_metadata_sha256": "e" * 64,
            "authority_manifest_sha256": manifest_id,
            "authority_discovery_policy_id": "adp",
            "authority_discovery_policy_version": "1",
            "authority_discovery_policy_sha256": model.sha256_hex(b"adp"),
            "witness_policy_sha256": self.witness_policy.sha256,
            "feedback_history_policy_id": "fhp",
            "feedback_history_policy_version": "1",
            "feedback_history_policy_sha256": model.sha256_hex(b"fhp"),
            "feedback_history_sha256": empty_feedback_sha,
            "local_check_policy_id": self.local_checks.source_id,
            "local_check_policy_version": self.local_checks.source_version,
            "local_check_policy_sha256": self.local_checks.sha256,
            "required_check_policy_sha256": model.sha256_hex(b"required-checks"),
            "review_assignment_policy_id": self.assignments_policy.source_id,
            "review_assignment_policy_version": self.assignments_policy.source_version,
            "review_assignment_policy_sha256": self.assignments_policy.sha256,
            "command_execution_policy_id": self.command_exec.source_id,
            "command_execution_policy_version": self.command_exec.source_version,
            "command_execution_policy_sha256": self.command_exec.sha256,
            "evidence_ingestion_policy_id": "eip",
            "evidence_ingestion_policy_version": "1",
            "evidence_ingestion_policy_sha256": model.sha256_hex(b"eip"),
            "hypothesis_derivation_policy_id": self.hypotheses.source_id,
            "hypothesis_derivation_policy_version": self.hypotheses.source_version,
            "hypothesis_derivation_policy_sha256": self.hypotheses.sha256,
            "unresolved_feedback_sha256": empty_feedback_sha,
        }
        snap["fingerprint"] = model.snapshot_fingerprint(snap)
        # Intake records bind the candidate snapshot, which is not installed
        # until the action runs.
        manifest_cid = _put(state, manifest_payload)
        manifest_ev = _bind(state, manifest_cid, "authority-manifest-payload", snap=snap)
        discovery_w = _witness(
            state,
            registry,
            kind="authority-discovery",
            subject=model.authority_discovery_subject(snap, manifest_payload),
            snap=snap,
        )
        manifest_wrapper = {
            "authority_manifest_id": manifest_id,
            "payload_evidence_id": manifest_ev,
            "discovery_witness_id": discovery_w,
            "snapshot_epoch": snap["epoch"],
            "snapshot_fingerprint": snap["fingerprint"],
        }
        auth_cid = _put(state, json.loads(auth_bytes))
        auth_ev = _bind(state, auth_cid, "authority", snap=snap)
        auth_rec = {
            "authority_id": "auth-agents",
            "kind": "repo-law",
            "locator": "AGENTS.md",
            "availability": "loaded",
            "sha256": auth_sha,
            "evidence_id": auth_ev,
            "snapshot_epoch": snap["epoch"],
            "snapshot_fingerprint": snap["fingerprint"],
        }
        return {
            "snapshot": snap,
            "authority_manifest": manifest_wrapper,
            "authorities": [auth_rec],
            "findings": [],
            "witnesses": [],
        }

    # -- dispatch plumbing ---------------------------------------------------

    def _register(self, *, role, profile, tier, reasoning, assignments=(), context_evidence=(), qualified=None):
        serial = self._next_serial()
        state = self.state
        snap = state["snapshot"]
        rs = _route_payload(
            state,
            role=role,
            profile=profile,
            profile_sha=model.sha256_hex(f"profile:{profile}".encode()),
            tier=tier,
            reasoning=reasoning,
            qualified=qualified,
            serial=serial,
        )
        rsid = model.derived_id("route", snap["epoch"], model.route_selection_subject(rs))
        pwid = _witness(
            state,
            self.registry,
            kind="profile-resolution",
            subject=model.profile_resolution_subject({**rs, "route_selection_id": rsid}),
            locator=f"hook-transcript:{state['review_id']}/profile-resolution/{rsid}",
        )
        d = {
            "dispatch_id": "",
            "route_selection_id": rsid,
            "profile_resolution_witness_id": pwid,
            "assignment_ids": sorted(assignments),
            "context_evidence_ids": sorted(context_evidence),
            "instruction_manifest_sha256": model.sha256_hex(f"instr:{serial}".encode()),
            "data_manifest_sha256": model.sha256_hex(f"data:{serial}".encode()),
            "tool_confinement_policy_sha256": model.sha256_hex(f"tools:{serial}".encode()),
            "context_package_sha256": model.sha256_hex(f"ctx:{serial}".encode()),
            "hazard_framing_sha256": model.sha256_hex(f"hazard:{serial}".encode()),
            "required_tool_classes": ["git-read", "github-read", "repo-read"],
        }
        self.policies.available_profiles = tuple(sorted(set(self.policies.available_profiles) | {profile}))
        before = set(state["dispatches"])
        self.state = policy.register_dispatch(state, route_selection=rs, dispatch=d, policies=self.policies)
        did = (set(self.state["dispatches"]) - before).pop()
        return serial, did

    def _launch(self, did: str, *, serial: int):
        d = self.state["dispatches"][did]
        rs = self.state["route_selections"][d["route_selection_id"]]
        launch_bytes = _witness_bytes(
            self.state,
            self.registry,
            kind="review-launch",
            subject=model.review_launch_subject(
                d,
                policy.dispatch_context_manifest(d),
                tool_use_id=f"toolu-launch-{serial}",
                task_bytes_sha256=model.sha256_hex(policy.dispatch_task_bytes(d, rs)),
                profile_name=rs["profile"],
            ),
            tool_use_id=f"toolu-launch-{serial}",
            agent_id=f"agent-{serial}",
        )
        self.state = policy.record_launch(
            self.state,
            dispatch_id=did,
            launch_witness_bytes=launch_bytes,
            policies=self.policies,
        )

    def _attestation(self, did: str, *, serial: int, verdict="clean", finding_ids=(), audit="clean") -> dict:
        """Register attestation evidence + completion witness, record the
        completion on the dispatch, and return the attestation payload."""
        state = self.state
        d = state["dispatches"][did]
        att_bytes = model.canonical_json({"dispatch_id": did, "verdict": verdict, "serial": serial})
        ev = _bind(state, _put(state, json.loads(att_bytes)), "review-attestation")
        _bind(state, _put(state, {"transcript": serial}), "tool-transcript")
        ts = model.sha256_hex(f"transcript:{serial}".encode())
        comp_bytes = _witness_bytes(
            state,
            self.registry,
            kind="review-completion",
            subject=model.review_completion_subject(
                att_bytes,
                tool_transcript_sha256=ts,
                agent_id=d["agent_id"],
            ),
            agent_id=d["agent_id"],
        )
        comp_wid = model.strict_json_loads(comp_bytes, source="witness")["witness_id"]
        self.state = policy.record_completion(
            self.state,
            dispatch_id=did,
            completion_witness_bytes=comp_bytes,
            transcript_sha256=ts,
            policies=self.policies,
        )
        return {
            "attestation_id": "",
            "dispatch_id": did,
            "assignment_ids": sorted(d["assignment_ids"]),
            "verdict": verdict,
            "finding_ids": sorted(finding_ids),
            "uncertainties": [],
            "tool_transcript_sha256": ts,
            "evidence_id": ev,
            "completion_witness_id": comp_wid,
            "audit_result": audit,
            "snapshot_epoch": self.state["snapshot"]["epoch"],
            "snapshot_fingerprint": self.state["snapshot"]["fingerprint"],
        }

    def review(
        self,
        action: str,
        *,
        role,
        profile,
        tier,
        reasoning,
        assignments=(),
        context_evidence=(),
        qualified=None,
        verdict="clean",
        findings=(),
        finding_ids=(),
        audit="clean",
        plural=True,
        exemption_outcome=None,
        report=False,
    ) -> dict:
        """Dispatch -> launch -> complete -> run the review action.

        With report=True the review reports a fresh finding sourced to its own
        attestation (verdict findings); the finding is stashed on
        self.reported_finding."""
        serial, did = self._register(
            role=role,
            profile=profile,
            tier=tier,
            reasoning=reasoning,
            assignments=assignments,
            context_evidence=context_evidence,
            qualified=qualified,
        )
        self._launch(did, serial=serial)
        att = self._attestation(
            did,
            serial=serial,
            verdict="findings" if report else verdict,
            finding_ids=finding_ids,
            audit=audit,
        )
        findings = list(findings)
        if report:
            att_id = _attestation_id(self.state, att)
            finding = _finding_at_strong(self, source_id=att_id)
            findings.append(finding)
            self.reported_finding = finding
        if exemption_outcome is not None:
            att = dict(att)
            att["exemption_outcome"] = exemption_outcome
        if plural:
            self.run(action, {"attestations": [att], "findings": findings})
        else:
            self.run(action, {"attestation": att, "findings": findings})
        att = dict(att)
        att["attestation_id"] = _attestation_id(self.state, att)
        return att

    def map_impact(self, action: str, *, role, profile, entries) -> dict:
        serial, did = self._register(role=role, profile=profile, tier="strong", reasoning="high")
        self._launch(did, serial=serial)
        att = self._attestation(did, serial=serial)
        self.run(
            action,
            {
                "impact_map": _map_payload(self.state, role=role, entries=entries),
                "attestation": att,
                "findings": [],
            },
        )
        return att

    def local_check(self, action: str, *, kind: str, item) -> dict:
        serial = self._next_serial()
        rec = _local_check_payload(self.state, self.registry, self.policies, kind=kind, item=item, serial=serial)
        self.run(action, {"checks": [rec]})
        return rec

    # -- composed phases -----------------------------------------------------

    def freeze(self, *, epoch=1, head_sha="b" * 40):
        self.run("freeze-review-input", self.intake(epoch=epoch, head_sha=head_sha))

    def ascent(
        self, *, report_finding=False, exemption_outcome="not-applicable-confirmed", surface="src/foo.py", tag=""
    ):
        """maps -> plan -> challenge -> exemption -> preflight -> tier reviews.
        When report_finding, the covering strong review reports a finding
        (verdict findings); it is stashed on self.reported_finding.
        exemption_outcome drives the challenger's exemption_outcome payload
        field for the high-risk not-applicable obligation. surface/tag vary
        subject bytes so a repair re-ascent derives fresh replacement ids."""
        self.ascent_to_exemption(surface=surface, tag=tag)
        self.exemption(exemption_outcome)
        return self.ascent_after_exemption(report_finding=report_finding)

    def ascent_to_exemption(self, *, surface="src/foo.py", tag=""):
        """maps -> plan -> challenge (the head of `ascent`)."""
        self.maps(surface=surface, tag=tag)
        self.plan(surface=surface)
        # challenge-coverage: scope-challenger attestation + revised obligations
        # (hypotheses auto-derive; the high-risk obligation goes not-applicable
        # with the challenger attestation as its seed backing).
        self.challenge(na=True, surface=surface)

    def maps(self, *, surface="src/foo.py", tag=""):
        self.map_impact(
            "map-impact-semantic",
            role="impact-mapper-semantic",
            profile="mapper-semantic",
            entries=[
                {
                    "surface": surface,
                    "category": "behavioral-correctness",
                    "hazards": [f"h-sem{tag}"],
                    "consequences": ["security"],
                }
            ],
        )
        self.map_impact(
            "map-impact-contract",
            role="impact-mapper-contract",
            profile="mapper-contract",
            entries=[
                {
                    "surface": surface,
                    "category": "documentation-contract",
                    "hazards": [f"h-con{tag}"],
                    "consequences": ["security"],
                }
            ],
        )

    def plan(self, *, surface="src/foo.py", surfaces=None, categories=None):
        """plan-coverage installs bare obligations (pending);
        challenge-coverage replaces them with hypothesis-derivation
        revisions. `surfaces` overrides the per-obligation surface list so a
        repair re-ascent derives fresh obligation ids; `categories` limits
        the plan to the named categories."""
        surf_list = surfaces or [surface]
        categories = categories or list(model.OBLIGATION_CATEGORIES)
        plan_recs = []
        for cat in categories:
            if cat == "security-privacy":
                risk, status, scope, cons = "high", "pending", "surface", ["security"]
            elif cat == "documentation-contract":
                risk, status, scope, cons = "low", "pending", "hunk", ["none"]
            elif cat == "performance-resources":
                risk, status, scope, cons = "low", "pending", "surface", ["none"]
            else:
                risk, status, scope, cons = "low", "pending", "cross-surface", ["security"]
            plan_recs.append(
                _obligation_payload(
                    self.state,
                    category=cat,
                    risk=risk,
                    consequences=cons,
                    status=status,
                    scope_level=scope,
                    surfaces=surf_list,
                )
            )
        self.run("plan-coverage", {"obligations": plan_recs})

    def challenge(self, *, na=True, surface="src/foo.py", surfaces=None, hazards=None, categories=None):
        """challenge-coverage: scope-challenger attestation + revised
        obligations. When na, the high-risk obligation goes not-applicable
        with the challenger attestation as its seed backing; otherwise it is
        claimed covered. `surfaces`/`hazards` override the obligation surface
        list and the inventory hazard set so repair re-ascents can bind fresh
        subjects while still covering the map union. `categories` limits the
        revision to the named categories; the inventory still binds every
        current obligation (revised plus surviving)."""
        surf_list = surfaces or [surface]
        inv_hazards = hazards or ["h-con", "h-sem"]
        categories = categories or list(model.OBLIGATION_CATEGORIES)
        serial, did = self._register(
            role="scope-challenger",
            profile="challenger",
            tier="final-strong",
            reasoning="final-strong",
            context_evidence=tuple(m["evidence_id"] for m in self.state["impact_maps"].values()),
        )
        self._launch(did, serial=serial)
        chall_att = self._attestation(did, serial=serial)
        chall_att_id = _attestation_id(self.state, chall_att)

        revised = []
        for cat in categories:
            if cat == "security-privacy":
                risk, status, scope, cons = (
                    "high",
                    "not-applicable" if na else "covered",
                    "surface",
                    ["security"],
                )
            elif cat == "documentation-contract":
                risk, status, scope, cons = "low", "covered", "hunk", ["none"]
            elif cat == "performance-resources":
                risk, status, scope, cons = "low", "covered", "surface", ["none"]
            else:
                risk, status, scope, cons = "low", "covered", "cross-surface", ["security"]
            revised.append(
                _obligation_payload(
                    self.state,
                    category=cat,
                    risk=risk,
                    consequences=cons,
                    status=status,
                    scope_level=scope,
                    surfaces=surf_list,
                    na_att=(chall_att_id,) if status == "not-applicable" else (),
                )
            )
        # The inventory entry lists every obligation category; its
        # obligation_ids bind the revised records plus the current
        # obligations outside the revised categories.
        revised_categories = {o["category"] for o in revised}
        surviving = [
            o["obligation_id"]
            for o in self.state["obligations"].values()
            if o["category"] not in revised_categories and policy._current(self.state, o, o["obligation_id"])
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
                            self.state["snapshot"]["epoch"],
                            model.obligation_subject(o),
                        )
                        for o in revised
                    ]
                ),
            }
        ]
        inv_cid = _put(self.state, {"entries": inv_entries})
        inv_ev = _bind(self.state, inv_cid, "scope-challenge")
        inv = {
            "coverage_inventory_id": "",
            "semantic_impact_map_id": next(
                m["impact_map_id"]
                for m in self.state["impact_maps"].values()
                if m["role"] == "impact-mapper-semantic" and policy._current(self.state, m, m["impact_map_id"])
            ),
            "contract_impact_map_id": next(
                m["impact_map_id"]
                for m in self.state["impact_maps"].values()
                if m["role"] == "impact-mapper-contract" and policy._current(self.state, m, m["impact_map_id"])
            ),
            "challenger_attestation_id": chall_att_id,
            "entries": inv_entries,
            "evidence_id": inv_ev,
            "snapshot_epoch": self.state["snapshot"]["epoch"],
            "snapshot_fingerprint": self.state["snapshot"]["fingerprint"],
        }
        self.run(
            "challenge-coverage",
            {
                "coverage_inventory": inv,
                "attestation": chall_att,
                "findings": [],
                "revised_obligations": revised,
            },
        )

    def exemption(self, outcome="not-applicable-confirmed"):
        """the high-risk not-applicable obligation needs its exemption
        challenger (filter to the current epoch: prior-epoch records persist
        as history)."""
        snap = self.state["snapshot"]
        high_oid = next(
            o["obligation_id"]
            for o in self.state["obligations"].values()
            if o["status"] == "not-applicable"
            and o["snapshot_epoch"] == snap["epoch"]
            and o["snapshot_fingerprint"] == snap["fingerprint"]
            and policy._current(self.state, o, o["obligation_id"])
        )
        self.review(
            "run-exemption-challenge",
            role="exemption-challenger",
            profile="exemption-challenger",
            tier="final-strong",
            reasoning="final-strong",
            assignments=[high_oid],
            exemption_outcome=outcome,
        )
        return high_oid

    def ascent_after_exemption(self, *, report_finding=False):
        """preflight -> tier reviews (the tail of `ascent`)."""
        snap = self.state["snapshot"]
        self.local_check("run-preflight", kind="preflight", item=self.items[0])

        obligations = [
            o
            for o in self.state["obligations"].values()
            if o["snapshot_epoch"] == snap["epoch"]
            and o["snapshot_fingerprint"] == snap["fingerprint"]
            and policy._current(self.state, o, o["obligation_id"])
        ]
        covered = [o for o in obligations if o["status"] == "covered"]
        fast_ids = sorted(o["obligation_id"] for o in covered if o["minimum_capability_tier"] == "fast")
        focused_ids = sorted(o["obligation_id"] for o in covered if o["minimum_capability_tier"] == "focused")
        strong_ids = sorted(o["obligation_id"] for o in covered if o["obligation_id"] not in fast_ids + focused_ids)
        if fast_ids:
            self.review(
                "run-fast-review",
                role="obligation-reviewer",
                profile="reviewer-fast",
                tier="fast",
                reasoning="low",
                assignments=fast_ids,
            )
        if focused_ids:
            self.review(
                "run-focused-review",
                role="obligation-reviewer",
                profile="reviewer-focused",
                tier="focused",
                reasoning="standard",
                assignments=focused_ids,
            )
        if report_finding:
            # The finding's source_id must resolve to the reporting
            # attestation, so its derived id is computed before the action.
            # (finding_ids stays empty: listing the finding would make the
            # attestation id and finding id a cyclic derivation.)
            serial, did = self._register(
                role="obligation-reviewer",
                profile="reviewer-a",
                tier="strong",
                reasoning="high",
                assignments=strong_ids,
            )
            self._launch(did, serial=serial)
            att = self._attestation(did, serial=serial, verdict="findings")
            att_id = _attestation_id(self.state, att)
            finding = _finding_at_strong(self, source_id=att_id)
            self.reported_finding = finding
            self.run(
                "run-strong-review",
                {"attestations": [att], "findings": [finding]},
            )
            att = dict(att)
            att["attestation_id"] = att_id
        else:
            att = self.review(
                "run-strong-review",
                role="obligation-reviewer",
                profile="reviewer-a",
                tier="strong",
                reasoning="high",
                assignments=strong_ids,
            )
        return att

    def final(self, *, report=False):
        return self.review(
            "run-final-review",
            role="blind-final",
            profile="blind-final-profile",
            tier="final-strong",
            reasoning="final-strong",
            qualified=("blind-final",),
            plural=False,
            report=report,
        )

    def closure(self, *, report=False):
        return self.review(
            "run-closure-audit",
            role="closure-auditor",
            profile="closure-auditor-profile",
            tier="final-strong",
            reasoning="final-strong",
            qualified=("closure-auditor",),
            plural=False,
            report=report,
        )

    def ready(self):
        self.run("mark-ready-for-ci", {"prior_lifecycle_state": "draft"})

    def transition(self):
        """Witnessed `gh pr ready`: draft -> ready, completing the pending
        ready transition and materializing the CI candidate."""
        state = self.state
        ready = state["ready_transition"]
        tool_use = f"toolu-ready-{self._next_serial()}"
        trans_bytes = _witness_bytes(
            state,
            self.registry,
            kind="remote-transition",
            subject=model.remote_transition_subject(
                ready,
                tool_use_id=tool_use,
                prior_lifecycle_state="draft",
                result_lifecycle_state="ready",
            ),
            tool_use_id=tool_use,
            locator=f"gh:{state['review_id']}/remote-transition/{self.serial}",
        )
        self.state = policy.record_ready_transition(
            state,
            transition_witness_bytes=trans_bytes,
            observed_prior_lifecycle="draft",
            policies=self.policies,
        )

    def remote_ci(self, *, run: int = 1):
        """A remote-observation witness plus the hosted remote-ci check bound
        to it. `run` varies run identity so a replacement check derives a
        fresh id."""
        state = self.state
        snap = state["snapshot"]
        manifest_id = state["authority_manifest"]["authority_manifest_id"]
        empty_feedback_sha = model.sha256_hex(model.canonical_json([]))
        observation = {
            "repository_id": "o/r",
            "pr_number": 7,
            "pr_url": "https://github.com/o/r/pull/7",
            "head_sha": snap["head_sha"],
            "lifecycle_state": "ready",
            "authority_manifest_sha256": manifest_id,
            "unresolved_feedback_sha256": empty_feedback_sha,
            "check_runs": [
                {
                    "check_run_id": f"cr-{run}",
                    "name": "ci",
                    "app_id": "github-actions",
                    "status": "completed",
                    "conclusion": "success",
                    "head_sha": snap["head_sha"],
                }
            ],
            "workflow_runs": [
                {
                    "workflow_run_id": f"wr-{run}",
                    "name": "ci",
                    "status": "completed",
                    "conclusion": "success",
                    "head_sha": snap["head_sha"],
                    "run_attempt": run,
                    "run_number": run,
                    "event": "pull_request",
                }
            ],
            "observed_at": f"2026-09-12T00:{run:02d}:00+00:00",
        }
        serial = self._next_serial()
        obs_cid = _put(state, observation)
        obs_ev = _bind(state, obs_cid, "remote-observation")
        obs_bytes = _witness_bytes(
            state,
            self.registry,
            kind="remote-observation",
            subject=model.remote_observation_subject(observation),
            locator=f"gh:{state['review_id']}/remote-observation/{serial}",
        )
        self.state = policy.record_witness(
            self.state,
            witness_bytes=obs_bytes,
            kind="remote-observation",
            policies=self.policies,
        )
        obs_wid = model.strict_json_loads(obs_bytes, source="witness")["witness_id"]
        hosted = {
            "check_id": f"check-remote-ci-{serial}",
            "kind": "remote-ci",
            "locus": "hosted",
            "policy_item_id": "ci",
            "name": "ci",
            "required": True,
            "conclusion": "success",
            "head_sha": snap["head_sha"],
            "app_id": "github-actions",
            "workflow_id": "wf-1",
            "workflow_path": ".github/workflows/ci.yml",
            "workflow_definition_ref": "refs/heads/x",
            "workflow_definition_sha": "d" * 40,
            "event": "pull_request",
            "trigger_subject": "pr",
            "policy_inputs_sha256": "0" * 64,
            "configuration_sha256": "0" * 64,
            "check_run_id": f"cr-{run}",
            "workflow_run_id": f"wr-{run}",
            "run_attempt": run,
            "evidence_id": obs_ev,
            "remote_observation_witness_id": obs_wid,
        }
        self.run(
            "run-remote-ci",
            {"remote_observation": observation, "checks": [hosted]},
        )

    def seal(self):
        self.run("seal-green", {})

    def remote(self):
        """final -> closure -> mark-ready -> transition -> remote-ci -> seal."""
        self.final()
        self.closure()
        self.ready()
        self.transition()
        self.remote_ci()
        self.seal()


def walk_happy_path(tmp_path):
    """Clean ascent through every complete_action ending in a sealed green."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent()
    w.remote()
    return w.state, w.policies, w.registry, w.steps


def _finding_at_strong(w, *, source_id):
    """A finding payload attributed to the strong reviewer's coverage."""
    state = w.state
    oid = next(
        o["obligation_id"]
        for o in state["obligations"].values()
        if o["status"] == "covered" and o["minimum_capability_tier"] == "strong"
    )
    return _finding_payload(
        state,
        source_kind="review",
        source_id=source_id,
        source_assignment_id=oid,
        obligation_id=oid,
        severity="important",
        title="walk-finding",
    )


def _run_adjudicated(w, fid, *, outcome, remediation_class=None):
    serial, did = w._register(
        role="finding-adjudicator",
        profile="adjudicator",
        tier="strong",
        reasoning="high",
    )
    w._launch(did, serial=serial)
    adj_att = w._attestation(did, serial=serial)
    adj_att_id = _attestation_id(w.state, adj_att)
    w.run(
        "adjudicate-findings",
        {
            "attestations": [adj_att],
            "findings": [
                {
                    "finding_id": fid,
                    "outcome": outcome,
                    "remediation_class": remediation_class,
                    "adjudicator_attestation_id": adj_att_id,
                }
            ],
        },
    )
    return adj_att_id


def walk_false_positive_path(tmp_path):
    """A strong review reports a finding; adjudication marks it a false
    positive and close-false-positive retires it before the remote gates."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f = w.reported_finding
    adj_att_id = _run_adjudicated(w, f["finding_id"], outcome="false-positive")
    counter = _bind(w.state, _put(w.state, {"counter-evidence": f["finding_id"]}), "finding-proof")
    w.run(
        "close-false-positive",
        {
            "resolutions": [
                {
                    "finding_id": f["finding_id"],
                    "counter_evidence_ids": [counter],
                    "review_id": adj_att_id,
                }
            ]
        },
    )
    w.remote()
    return w.state, w.policies, w.registry, w.steps


def walk_accept_risk_path(tmp_path):
    """Adjudicated confirmed finding accepted as risk by witnessed human
    decision; the review ends reviewed-with-exceptions, never green."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f = w.reported_finding
    _run_adjudicated(w, f["finding_id"], outcome="confirmed", remediation_class="candidate-change")
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
    return w.state, w.policies, w.registry, w.steps


def walk_review_repair_path(tmp_path):
    """A confirmed review-process finding repairs the focused-tier review:
    the invalidation cut retires that review's attestation, the re-ascent
    re-proves it, then an independent verifier closes the repair and the
    remote gates re-run."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f = w.reported_finding
    _run_adjudicated(w, f["finding_id"], outcome="confirmed", remediation_class="review-process")
    focused_att_id = next(
        rid
        for rid, r in w.state["reviews"].items()
        if w.state["route_selections"][w.state["dispatches"][r["dispatch_id"]]["route_selection_id"]][
            "required_capability_tier"
        ]
        == "focused"
    )
    w.run(
        "enter-review-repair",
        {
            "resolutions": [
                {
                    "finding_id": f["finding_id"],
                    "repair_id": "repair-1",
                    "target_kind": "obligation-review",
                    "target_ids": [focused_att_id],
                }
            ]
        },
    )
    # The cut invalidated the focused-tier review; re-ascend that gate.
    focused_ids = sorted(
        o["obligation_id"]
        for o in w.state["obligations"].values()
        if o["status"] == "covered" and o["minimum_capability_tier"] == "focused"
    )
    att = w.review(
        "run-focused-review",
        role="obligation-reviewer",
        profile="reviewer-focused-2",
        tier="focused",
        reasoning="standard",
        assignments=focused_ids,
    )
    # verify-review-repair: independent verifier dispatched on the finding.
    w.review(
        "verify-review-repair",
        role="review-repair-verifier",
        profile="repair-verifier",
        tier="strong",
        reasoning="high",
        assignments=[f["finding_id"]],
    )
    w.run(
        "close-review-repaired",
        {
            "resolutions": [
                {
                    "finding_id": f["finding_id"],
                    "replacement_record_ids": [att["attestation_id"]],
                }
            ]
        },
    )
    w.remote()
    return w.state, w.policies, w.registry, w.steps


def walk_fix_path(tmp_path):
    """A confirmed candidate-change finding drives enter-fixing: the epoch
    advances, the full ascent re-proves at epoch 2, then the fix lifecycle
    (targeted check -> fix review -> close) precedes the remote gates."""
    w = _Walk(tmp_path)
    w.freeze()
    w.ascent(report_finding=True)
    f = w.reported_finding
    _run_adjudicated(w, f["finding_id"], outcome="confirmed", remediation_class="candidate-change")
    intake = w.intake(epoch=2, head_sha="f" * 40)
    pub = _bind(
        w.state,
        _put(w.state, {"publication": f["finding_id"]}),
        "fix-proof",
        snap=intake["snapshot"],
    )
    w.run(
        "enter-fixing",
        {
            "resolutions": [{"finding_id": f["finding_id"], "publication_evidence_ids": [pub]}],
            "replacement_snapshot": intake["snapshot"],
            "replacement_authority_manifest": intake["authority_manifest"],
            "replacement_authorities": intake["authorities"],
        },
    )
    # Epoch 2 re-ascent: all epoch-1 records are non-current.
    w.ascent()
    w.local_check("run-fix-verification", kind="targeted", item=w.items[1])
    fix_att = w.review(
        "review-fix",
        role="fix-reviewer",
        profile="fix-reviewer",
        tier="focused",
        reasoning="standard",
        assignments=[f["finding_id"]],
    )
    targeted = next(
        c["check_id"] for c in w.state["checks"].values() if c["kind"] == "targeted" and c["locus"] == "local"
    )
    w.run(
        "close-fixed",
        {
            "resolutions": [
                {
                    "finding_id": f["finding_id"],
                    "check_id": targeted,
                    "review_id": fix_att["attestation_id"],
                    "verified_obligation_ids": [f["obligation_id"]],
                }
            ]
        },
    )
    w.remote()
    return w.state, w.policies, w.registry, w.steps


def _enter_repair(w, finding, *, target_kind, target_ids, repair_id="repair-1"):
    """Branch a confirmed review-process finding into a same-snapshot repair."""
    w.run(
        "enter-review-repair",
        {
            "resolutions": [
                {
                    "finding_id": finding["finding_id"],
                    "repair_id": repair_id,
                    "target_kind": target_kind,
                    "target_ids": sorted(target_ids),
                }
            ]
        },
    )


def _verify_close_repair(w, finding, *, replacement_ids=()):
    """Independent verifier attestation then close-review-repaired."""
    att = w.review(
        "verify-review-repair",
        role="review-repair-verifier",
        profile="repair-verifier",
        tier="strong",
        reasoning="high",
        assignments=[finding["finding_id"]],
    )
    w.run(
        "close-review-repaired",
        {
            "resolutions": [
                {
                    "finding_id": finding["finding_id"],
                    "replacement_record_ids": sorted({*replacement_ids, att["attestation_id"]}),
                }
            ]
        },
    )


# ---------------------------------------------------------------------------
# Live-acquisition doubles (Plan 2): fake git/gh runners plus scratch +
# transcript fixtures shared by the acquisition and reviewctl suites.

ACQ_BASE = "b" * 40
ACQ_HEAD = "c" * 40
ACQ_TREE = "d" * 40
ACQ_MB = "e" * 40
ACQ_REPO_ID = "o/r"
ACQ_PR_URL = "https://github.com/o/r/pull/7"


def acq_pr_meta(**over):
    meta = {
        "number": 7,
        "url": ACQ_PR_URL,
        "title": "T",
        "body": "B",
        "isDraft": True,
        "state": "OPEN",
        "baseRefOid": ACQ_BASE,
        "headRefOid": ACQ_HEAD,
        "baseRefName": "main",
        "closingIssuesReferences": [{"number": 12}],
        "labels": [{"name": "bug"}],
        "assignees": [{"login": "me"}],
        "milestone": None,
        "author": {"login": "a"},
    }
    meta.update(over)
    return meta


class FakeGit:
    def __init__(
        self,
        files,
        *,
        head=ACQ_HEAD,
        base=ACQ_BASE,
        merge_base=ACQ_MB,
        tree=ACQ_TREE,
        obj_format="sha1",
        porcelain="",
        merge_bases=None,
        diff="diff-bytes",
        override=None,
    ):
        self.files = dict(files)
        self.head = head
        self.base = base
        self.merge_bases = [merge_base] if merge_bases is None else merge_bases
        self.tree = tree
        self.obj_format = obj_format
        self.porcelain = porcelain
        self.diff = diff
        self.override = override
        self.calls = []

    def __call__(self, args):
        self.calls.append(list(args))
        if args[:2] == ["rev-parse", "--is-shallow-repository"]:
            return 0, "false\n", ""
        if args[:2] == ["merge-base", "--all"]:
            return 0, "".join(f"{m}\n" for m in self.merge_bases), ""
        if args[:2] == ["status", "--porcelain"]:
            return 0, self.porcelain, ""
        if args[:2] == ["rev-parse", "HEAD"]:
            return 0, self.head + "\n", ""
        if args[:2] == ["rev-parse", "--show-object-format"]:
            return 0, self.obj_format + "\n", ""
        if args[:2] == ["rev-parse", f"{self.head}^{{tree}}"]:
            return 0, self.tree + "\n", ""
        if args[0] == "diff":
            return 0, self.diff, ""
        if args[:2] == ["ls-tree", "-r"]:
            return 0, "\n".join(sorted(self.files)) + "\n", ""
        if args[0] == "show" and ":" in args[1]:
            _sha, path = args[1].split(":", 1)
            if path == ".agents/iterative-review/authority-policy.json":
                return (0, self.override, "") if self.override else (1, "", "nf")
            if path in self.files:
                return 0, self.files[path], ""
            return 1, "", f"missing {path}"
        return 1, "", f"unsupported {args}"


class FakeGh:
    def __init__(
        self,
        *,
        authed=True,
        repo=ACQ_REPO_ID,
        pr=None,
        head_remote=True,
        threads=(),
        reviews=(),
        issues=None,
        contents=None,
        protection=None,
        graphql_error=False,
    ):
        self.authed = authed
        self.repo = repo
        self.pr = pr if pr is not None else acq_pr_meta()
        self.head_remote = head_remote
        self.threads = list(threads)
        self.reviews = list(reviews)
        self.issues = issues or {}
        self.contents = contents or {}
        self.protection = protection
        self.graphql_error = graphql_error
        self.calls = []

    def __call__(self, args):
        self.calls.append(list(args))
        if args[:2] == ["auth", "status"]:
            return (0, "ok", "") if self.authed else (1, "", "not logged in")
        if args[:2] == ["repo", "view"]:
            return 0, json.dumps({"nameWithOwner": self.repo}), ""
        if args[0] == "pr" and args[1] == "view":
            return 0, json.dumps(self.pr), ""
        if args[0] == "api" and args[1] == "graphql":
            if self.graphql_error:
                return 0, json.dumps({"errors": [{"message": "boom"}]}), ""
            pr = {
                "reviewThreads": {
                    "pageInfo": {"hasNextPage": False},
                    "nodes": list(self.threads),
                },
                "reviews": {
                    "pageInfo": {"hasNextPage": False},
                    "nodes": list(self.reviews),
                },
            }
            return 0, json.dumps({"data": {"repository": {"pullRequest": pr}}}), ""
        if args[0] == "api":
            path = args[1]
            if "/commits/" in path:
                return (0, "{}") + ("",) if self.head_remote else (1, "", "404")
            if "/issues/" in path:
                n = path.rsplit("/", 1)[-1]
                if n in self.issues:
                    return 0, json.dumps(self.issues[n]), ""
                return 1, "", "404"
            if "/contents/" in path:
                key = path.split("/contents/", 1)[1].split("?")[0]
                if key in self.contents:
                    body = base64.b64encode(self.contents[key].encode()).decode()
                    return 0, json.dumps({"content": body}), ""
                return 1, "", "404"
            if "/protection" in path:
                if self.protection is None:
                    return 1, "", "404"
                return 0, json.dumps(self.protection), ""
        return 1, "", f"unsupported {args}"


def acq_scratch(tmp_path):
    s = Path(tmp_path) / "scratch"
    (s / "transcripts").mkdir(parents=True, exist_ok=True)
    (s / "witness").mkdir(parents=True, exist_ok=True)
    return s


def acq_enumerate(tmp_path, git=None, gh=None, epoch=1):
    scratch = acq_scratch(tmp_path)
    out_dir = scratch / "acquire" / "latest"
    git = git or FakeGit({"AGENTS.md": "# law"})
    gh = gh or FakeGh()
    summary = acq.enumerate_acquisition(
        run_git=git, run_gh=gh, repo_root=Path(tmp_path), pr_number=7, out_dir=out_dir, scratch_dir=scratch, epoch=epoch
    )
    return summary, out_dir, scratch


def acq_transcript_with_marker(scratch, enumeration_id, *, session="s1", tool_use="exec_1", out_dir=None, extra_pre=()):
    lines = list(extra_pre)
    lines.append(
        {
            "hook_event_name": "PreToolUse",
            "tool_name": "exec",
            "tool_input": {
                "command": f"py -3 reviewctl.py enumerate --state {out_dir or 'X'}/state.json --repo . --pr 7"
            },
            "tool_use_id": tool_use,
            "session_id": session,
            "prompt_id": "p1",
        }
    )
    lines.append(
        {
            "hook_event_name": "PostToolUse",
            "tool_name": "exec",
            "tool_input": {
                "command": f"py -3 reviewctl.py enumerate --state {out_dir or 'X'}/state.json --repo . --pr 7"
            },
            "tool_use_id": tool_use,
            "session_id": session,
            "prompt_id": "p1",
            "tool_response": {"success": True, "output": f"enumeration-id: {enumeration_id}\n", "error": None},
        }
    )
    f = Path(scratch) / "transcripts" / f"{session}.jsonl"
    f.parent.mkdir(parents=True, exist_ok=True)
    f.write_text("".join(json.dumps(x) + "\n" for x in lines), encoding="utf-8")
    return f


def acq_source(out_dir, scratch):
    return acq.LiveAuthorityDiscovery(
        acquisition_dir=Path(out_dir),
        witness_log_path=Path(scratch) / "witness" / "witness-log.jsonl",
        transcript_root=Path(scratch) / "transcripts",
        review_id="rev-1",
    )
