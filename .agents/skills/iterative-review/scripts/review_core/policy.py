#!/usr/bin/env python3
"""Pure fail-closed policy and green predicate for the version-2 kernel.

This module never writes files and never reaches for a witness source itself.
Witness authenticity is delegated to the composition-root ``WitnessVerifier``
protocol; this module computes the exact expected subjects and fails closed on
any ``WitnessVerificationError``. Local green is a calibrated prediction of
frontier confirmation, not a cryptographic guarantee.
"""

from __future__ import annotations

import copy
import re
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from typing import Protocol

from . import model
from . import store

# ---------------------------------------------------------------------------
# Witness policy / verifier protocols


class WitnessVerificationError(RuntimeError):
    def __init__(self, code: str, message: str):
        super().__init__(f"{code}: {message}")
        self.code = code


@dataclass(frozen=True)
class WitnessSource:
    kind: str
    source: str  # "hook-transcript" | "github-remote"
    locator_prefix: str


@dataclass(frozen=True)
class VerifiedWitness:
    witness_id: str
    kind: str
    source: str
    locator: str
    review_id: str
    dispatch_id: str | None
    snapshot_epoch: int
    snapshot_fingerprint: str
    subject_sha256: str
    tool_use_id: str | None
    agent_id: str | None
    observed_at: datetime
    record_sha256: str


class WitnessPolicy(Protocol):
    @property
    def sha256(self) -> str: ...

    @property
    def sources(self) -> tuple[WitnessSource, ...]: ...

    def permits(self, *, kind: str, source: str, locator: str) -> bool: ...


class WitnessVerifier(Protocol):
    @property
    def policy(self) -> WitnessPolicy: ...

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
    ) -> VerifiedWitness: ...


@dataclass(frozen=True)
class LocalCheckItem:
    policy_item_id: str
    name: str
    command: tuple[str, ...]
    working_directory: str
    required: bool


class LocalCheckPolicy(Protocol):
    @property
    def source_id(self) -> str: ...

    @property
    def source_version(self) -> str: ...

    @property
    def sha256(self) -> str: ...

    @property
    def items(self) -> tuple[LocalCheckItem, ...]: ...


@dataclass(frozen=True)
class RoleRequirement:
    capability_tier: str
    reasoning_floor: str
    context_mode: str
    distinct_execution_from: tuple[str, ...]
    distinct_role_contract_from: tuple[str, ...]


class ReviewAssignmentPolicy(Protocol):
    @property
    def source_id(self) -> str: ...

    @property
    def source_version(self) -> str: ...

    @property
    def sha256(self) -> str: ...

    def requirement(self, *, state: dict, role: str, assignment_ids: tuple[str, ...]) -> RoleRequirement: ...


class CommandExecutionPolicy(Protocol):
    @property
    def source_id(self) -> str: ...

    @property
    def source_version(self) -> str: ...

    @property
    def sha256(self) -> str: ...

    def command_intent(self, *, snapshot: dict, item: LocalCheckItem) -> dict: ...


class HypothesisDerivationPolicy(Protocol):
    @property
    def source_id(self) -> str: ...

    @property
    def source_version(self) -> str: ...

    @property
    def sha256(self) -> str: ...

    def derive(self, *, obligation: dict) -> tuple[dict, ...]: ...


@dataclass(frozen=True)
class PolicyBundle:
    witness_verifier: WitnessVerifier
    local_checks: LocalCheckPolicy
    review_assignments: ReviewAssignmentPolicy
    command_execution: CommandExecutionPolicy
    hypotheses: HypothesisDerivationPolicy
    # Where the authority-discovery policy was resolved from. The default is
    # fail-closed: only a composition root that resolves policy against the
    # reviewed base revision may claim otherwise.
    discovery_policy_origin: str = "reviewed-head"


@dataclass(frozen=True)
class ActionRecipe:
    action: str
    dispatch_required: bool
    required_role: str | None
    minimum_capability_tier: str | None
    minimum_reasoning_floor: str | None
    preferred_profile: str | None
    data_keys: tuple[str, ...]
    evidence_kinds: tuple[str, ...]
    record_command: str


@dataclass(frozen=True)
class Decision:
    allowed: bool
    action: str
    reason: str
    missing: tuple[str, ...] = ()
    recipe: ActionRecipe | None = None
    status: str | None = None


# ---------------------------------------------------------------------------
# Action contract


ACTION_ORDER = (
    "freeze-review-input",
    "map-impact-semantic",
    "map-impact-contract",
    "plan-coverage",
    "challenge-coverage",
    "run-preflight",
    "run-fast-review",
    "run-focused-review",
    "run-strong-review",
    "run-final-review",
    "run-closure-audit",
    "mark-ready-for-ci",
    "run-remote-ci",
    "seal-green",
)

ACTION_PAYLOAD_KEYS = {
    "freeze-review-input": frozenset({"snapshot", "authority_manifest", "authorities", "findings", "witnesses"}),
    "refresh-review-input": frozenset(
        {"snapshot", "authority_manifest", "authorities", "drift_reasons", "findings", "witnesses"}
    ),
    "map-impact-semantic": frozenset({"impact_map", "attestation", "findings"}),
    "map-impact-contract": frozenset({"impact_map", "attestation", "findings"}),
    "plan-coverage": frozenset({"obligations"}),
    "challenge-coverage": frozenset({"coverage_inventory", "attestation", "findings", "revised_obligations"}),
    "run-preflight": frozenset({"checks"}),
    "run-fast-review": frozenset({"attestations", "findings"}),
    "run-focused-review": frozenset({"attestations", "findings"}),
    "run-strong-review": frozenset({"attestations", "findings"}),
    "run-exemption-challenge": frozenset({"attestations", "findings"}),
    "adjudicate-findings": frozenset({"attestations", "findings"}),
    "close-false-positive": frozenset({"resolutions"}),
    "enter-fixing": frozenset(
        {
            "resolutions",
            "replacement_snapshot",
            "replacement_authority_manifest",
            "replacement_authorities",
        }
    ),
    "run-fix-verification": frozenset({"checks"}),
    "review-fix": frozenset({"attestations", "findings"}),
    "close-fixed": frozenset({"resolutions"}),
    "enter-review-repair": frozenset({"resolutions"}),
    "verify-review-repair": frozenset({"attestations", "findings"}),
    "close-review-repaired": frozenset({"resolutions"}),
    "accept-risk": frozenset({"resolutions"}),
    "resume-review": frozenset({"blocker_id", "resolution_evidence_ids"}),
    "run-final-review": frozenset({"attestation", "findings"}),
    "run-closure-audit": frozenset({"attestation", "findings"}),
    "mark-ready-for-ci": frozenset({"prior_lifecycle_state"}),
    "run-remote-ci": frozenset({"remote_observation", "checks"}),
    "seal-green": frozenset(),
}

# role floors: (capability tier, reasoning floor)
_ROLE_FLOORS = {
    "impact-mapper-semantic": ("strong", "high"),
    "impact-mapper-contract": ("strong", "high"),
    "scope-challenger": ("final-strong", "final-strong"),
    "exemption-challenger": ("final-strong", "final-strong"),
    "blind-final": ("final-strong", "final-strong"),
    "closure-auditor": ("final-strong", "final-strong"),
    "finding-adjudicator": ("strong", "high"),
    "review-repair-verifier": ("strong", "high"),
    "fix-reviewer": ("focused", "standard"),
}

_ACTION_ROLE = {
    "map-impact-semantic": "impact-mapper-semantic",
    "map-impact-contract": "impact-mapper-contract",
    "challenge-coverage": "scope-challenger",
    "run-fast-review": "obligation-reviewer",
    "run-focused-review": "obligation-reviewer",
    "run-strong-review": "obligation-reviewer",
    "run-exemption-challenge": "exemption-challenger",
    "adjudicate-findings": "finding-adjudicator",
    "review-fix": "fix-reviewer",
    "verify-review-repair": "review-repair-verifier",
    "run-final-review": "blind-final",
    "run-closure-audit": "closure-auditor",
}

_TIER_ORDER = model.CAPABILITY_TIERS  # fast < focused < strong < final-strong
_TIER_REASONING = {
    "fast": "low",
    "focused": "standard",
    "strong": "high",
    "final-strong": "final-strong",
}
_SCOPE_TIER = {
    "hunk": "fast",
    "file": "fast",
    "surface": "focused",
    "cross-surface": "strong",
    "whole-pr": "final-strong",
}
_RISK_TIER = {"low": "fast", "medium": "focused", "high": "strong"}


def _tier_max(*tiers: str) -> str:
    return max(tiers, key=lambda t: _TIER_ORDER.index(t))


def obligation_floor(scope_level: str, risk: str, consequences) -> tuple[str, str]:
    """Portable scope/risk/consequence floor table (pure function)."""
    tier = _tier_max(_SCOPE_TIER[scope_level], _RISK_TIER[risk])
    if any(c in model.SUBSTANTIVE_CONSEQUENCES for c in consequences):
        tier = _tier_max(tier, "strong")
    return tier, _TIER_REASONING[tier]


def _role_floor(role: str) -> tuple[str, str]:
    return _ROLE_FLOORS.get(role, ("fast", "low"))


# ---------------------------------------------------------------------------
# Small helpers


def _fail(code: str, path: str, message: str) -> None:
    raise model.StateValidationError(code, path, message)


def _invalidated_ids(state: dict) -> frozenset:
    """Ids inside any current review-repair's invalidation cut."""
    out = set()
    for r in state["review_repairs"].values():
        if _current_epoch_only(state, r):
            out.update(r["invalidated_record_ids"])
    return frozenset(out)


def _current_epoch_only(state: dict, record: dict) -> bool:
    snap = state["snapshot"]
    if snap is None:
        return False
    return record["snapshot_epoch"] == snap["epoch"] and record["snapshot_fingerprint"] == snap["fingerprint"]


def _current(state: dict, record: dict, rid: str | None = None) -> bool:
    if not _current_epoch_only(state, record):
        return False
    if rid is not None and rid in _invalidated_ids(state):
        return False
    return True


def _current_records(state: dict, key: str) -> list[dict]:
    return [r for k, r in state[key].items() if _current(state, r, k)]


def _content_bytes(state: dict, content_id: str) -> bytes:
    obj = state["content_objects"].get(content_id)
    if obj is None:
        _fail("content-missing", "content_objects", f"unknown content {content_id!r}")
    from pathlib import Path

    return Path(obj["path"]).read_bytes()


def _evidence_bytes(state: dict, evidence_id: str) -> bytes:
    rec = state["evidence"].get(evidence_id)
    if rec is None:
        _fail("dangling-ref", "evidence", f"unknown evidence {evidence_id!r}")
    return _content_bytes(state, rec["content_id"])


def _evidence_kind(state: dict, evidence_id: str, kind: str) -> bytes:
    rec = state["evidence"].get(evidence_id)
    if rec is None:
        _fail("dangling-ref", "evidence", f"unknown evidence {evidence_id!r}")
    if rec["kind"] != kind:
        _fail("wrong-kind", "evidence", f"evidence {evidence_id!r} is not {kind!r}")
    return _evidence_bytes(state, evidence_id)


def _decode(data: bytes, source: str) -> object:
    return model.strict_json_loads(data, source=source)


def _dispatch_for_witness(state: dict, witness_id: str, field: str) -> dict | None:
    for d in state["dispatches"].values():
        if d.get(field) == witness_id:
            return d
    return None


def _role_of_dispatch(state: dict, dispatch: dict) -> str:
    rs = state["route_selections"].get(dispatch["route_selection_id"])
    return rs["required_role"] if rs else ""


def _reviews_of_role(state: dict, role: str) -> list[dict]:
    out = []
    for r in state["reviews"].values():
        d = state["dispatches"].get(r["dispatch_id"])
        if d is not None and _role_of_dispatch(state, d) == role:
            out.append(r)
    return out


def dispatch_context_manifest(dispatch: dict) -> dict:
    """Canonical context manifest the launch subject binds."""
    return {
        "assignment_ids": sorted(dispatch["assignment_ids"]),
        "context_package_sha256": dispatch["context_package_sha256"],
        "data_manifest_sha256": dispatch["data_manifest_sha256"],
        "hazard_framing_sha256": dispatch["hazard_framing_sha256"],
        "instruction_manifest_sha256": dispatch["instruction_manifest_sha256"],
        "route_selection_id": dispatch["route_selection_id"],
        "tool_confinement_policy_sha256": dispatch["tool_confinement_policy_sha256"],
    }


def dispatch_task_bytes(dispatch: dict, route: dict) -> bytes:
    """Canonical verbatim task bytes the launch witness binds."""
    return model.canonical_json(
        {
            "dispatch": model.pending_dispatch_intent_subject(dispatch),
            "profile": route["profile"],
            "role": route["required_role"],
        }
    )


def command_result_subject(check: dict) -> dict:
    """Canonical command-execution result projection for a local check."""
    return {
        "check": model.local_check_subject(check),
        "exit_code": 0 if check["conclusion"] == "success" else 1,
    }


def _expected_witness_subject(state: dict, policies, record: dict) -> tuple[bytes, str | None, str | None]:
    """Return (expected_subject_bytes, expected_tool_use_id, expected_agent_id)."""
    kind = record["kind"]
    if kind == "authority-discovery":
        manifest = state["authority_manifest"]
        if manifest is None:
            raise WitnessVerificationError("witness-mismatch", "no authority manifest")
        payload = _decode(
            _evidence_kind(state, manifest["payload_evidence_id"], "authority-manifest-payload"),
            "authority-manifest-payload",
        )
        return (
            model.canonical_json(model.authority_discovery_subject(state["snapshot"], payload)),
            None,
            None,
        )
    if kind == "profile-resolution":
        d = _dispatch_for_witness(state, record["witness_id"], "profile_resolution_witness_id")
        if d is None:
            raise WitnessVerificationError("scope-mismatch", "no dispatch binds this resolution witness")
        rs = state["route_selections"].get(d["route_selection_id"])
        if rs is None:
            raise WitnessVerificationError("scope-mismatch", "route selection missing")
        return (
            model.canonical_json(model.profile_resolution_subject(rs)),
            None,
            None,
        )
    if kind == "review-launch":
        d = _dispatch_for_witness(state, record["witness_id"], "launch_witness_id")
        if d is None:
            raise WitnessVerificationError("scope-mismatch", "no dispatch binds this launch witness")
        rs = state["route_selections"][d["route_selection_id"]]
        subject = model.review_launch_subject(
            d,
            dispatch_context_manifest(d),
            tool_use_id=record["tool_use_id"],
            task_bytes_sha256=model.sha256_hex(dispatch_task_bytes(d, rs)),
            profile_name=rs["profile"],
        )
        return model.canonical_json(subject), d["tool_use_id"], d["agent_id"]
    if kind == "review-completion":
        d = _dispatch_for_witness(state, record["witness_id"], "completion_witness_id")
        if d is None:
            raise WitnessVerificationError("scope-mismatch", "no dispatch binds this completion witness")
        attestation = next(
            (r for r in state["reviews"].values() if r["completion_witness_id"] == record["witness_id"]),
            None,
        )
        if attestation is None:
            raise WitnessVerificationError("scope-mismatch", "no review binds this completion witness")
        raw = _evidence_kind(state, attestation["evidence_id"], "review-attestation")
        subject = model.review_completion_subject(
            raw,
            tool_transcript_sha256=d["transcript_sha256"],
            agent_id=record["agent_id"],
        )
        return model.canonical_json(subject), None, d["agent_id"]
    if kind == "command-execution":
        check = next(
            (c for c in state["checks"].values() if c.get("execution_witness_id") == record["witness_id"]),
            None,
        )
        if check is None:
            raise WitnessVerificationError("scope-mismatch", "no check binds this command witness")
        item = next(
            (i for i in policies.local_checks.items if i.policy_item_id == check["policy_item_id"]),
            None,
        )
        if item is None:
            raise WitnessVerificationError("missing-source", "check policy item unknown")
        intent = policies.command_execution.command_intent(snapshot=state["snapshot"], item=item)
        subject = model.command_execution_subject(intent, command_result_subject(check))
        return model.canonical_json(subject), record["tool_use_id"], None
    if kind == "remote-transition":
        ready = state["ready_transition"]
        if ready is None or ready["transition_witness_id"] != record["witness_id"]:
            raise WitnessVerificationError("scope-mismatch", "no ready transition binds this witness")
        subject = model.remote_transition_subject(
            ready,
            tool_use_id=record["tool_use_id"],
            prior_lifecycle_state=ready["prior_lifecycle_state"],
            result_lifecycle_state=ready["expected_lifecycle_state"],
        )
        return model.canonical_json(subject), record["tool_use_id"], None
    if kind == "remote-observation":
        check = next(
            (c for c in state["checks"].values() if c.get("remote_observation_witness_id") == record["witness_id"]),
            None,
        )
        if check is None:
            raise WitnessVerificationError("scope-mismatch", "no hosted check binds this observation")
        # The observation payload is an epoch-bound evidence record of kind
        # remote-observation containing this check's identities.
        for eid, ev in state["evidence"].items():
            if ev["kind"] != "remote-observation" or not _current(state, ev, eid):
                continue
            obs = _decode(_evidence_bytes(state, eid), "remote-observation")
            check_ids = {r.get("check_run_id") for r in obs.get("check_runs", [])}
            run_ids = {r.get("workflow_run_id") for r in obs.get("workflow_runs", [])}
            if check["check_run_id"] in check_ids and check["workflow_run_id"] in run_ids:
                return (
                    model.canonical_json(model.remote_observation_subject(obs)),
                    None,
                    None,
                )
        raise WitnessVerificationError("missing-source", "no remote-observation evidence binds this witness")
    if kind == "human-decision":
        payload = record.get("transcript_range")
        if payload is None:
            raise WitnessVerificationError("missing-source", "human-decision witness lacks transcript range")
        return model.canonical_json({"transcript_range": payload}), record["tool_use_id"], None
    raise WitnessVerificationError("missing-source", f"unknown witness kind {kind!r}")


def _verify_witness(state: dict, policies, record: dict, *, dispatch_id: str | None = None) -> VerifiedWitness:
    expected_subject, tool_use_id, agent_id = _expected_witness_subject(state, policies, record)
    verifier = policies.witness_verifier
    if verifier.policy.sha256 != state["snapshot"]["witness_policy_sha256"]:
        raise WitnessVerificationError("policy-drift", "verifier policy digest differs from snapshot")
    return verifier.verify(
        stored_record_bytes=model.canonical_json(record),
        expected_kind=record["kind"],
        expected_review_id=state["review_id"],
        expected_dispatch_id=dispatch_id,
        expected_snapshot_epoch=state["snapshot"]["epoch"],
        expected_snapshot_fingerprint=state["snapshot"]["fingerprint"],
        expected_subject=expected_subject,
        expected_tool_use_id=tool_use_id,
        expected_agent_id=agent_id,
    )


# ---------------------------------------------------------------------------
# Named predicates. Each returns (ok, missing-reasons).


def has_current_snapshot(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    if state["snapshot"] is None:
        return False, ("snapshot",)
    return True, ()


def authority_manifest_complete(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    manifest = state["authority_manifest"]
    snap = state["snapshot"]
    if snap is None or manifest is None or not _current(state, manifest, manifest["authority_manifest_id"]):
        return False, ("authority-manifest",)
    if getattr(policies, "discovery_policy_origin", "reviewed-head") == "reviewed-head":
        return False, ("authority-manifest",)
    wrec = state["witness_records"].get(manifest["discovery_witness_id"])
    if wrec is None or not _current(state, wrec, manifest["discovery_witness_id"]):
        return False, ("authority-manifest",)
    try:
        payload_raw = _evidence_kind(state, manifest["payload_evidence_id"], "authority-manifest-payload")
        payload = _decode(payload_raw, "authority-manifest-payload")
    except (model.StateValidationError, OSError):
        return False, ("authority-manifest",)
    if model.sha256_json(payload) != snap["authority_manifest_sha256"]:
        return False, ("authority-manifest",)
    if model.sha256_json(payload) != manifest["authority_manifest_id"]:
        return False, ("authority-manifest",)
    try:
        _verify_witness(state, policies, wrec)
    except WitnessVerificationError:
        return False, ("authority-manifest",)
    return True, ()


def _manifest_payload(state: dict) -> dict | None:
    manifest = state["authority_manifest"]
    if manifest is None:
        return None
    try:
        raw = _evidence_kind(state, manifest["payload_evidence_id"], "authority-manifest-payload")
        return _decode(raw, "authority-manifest-payload")
    except (model.StateValidationError, OSError):
        return None


def authorities_complete(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    ok, _ = has_current_snapshot(state, policies)
    if not ok:
        return False, ("authority",)
    payload = _manifest_payload(state)
    if payload is None:
        return False, ("authority",)
    entries = payload["authorities"]
    for entry in entries:
        rec = state["authorities"].get(entry["authority_id"])
        if rec is None or not _current(state, rec, entry["authority_id"]):
            return False, ("authority",)
        if rec["availability"] != entry["availability"]:
            return False, ("authority",)
        if entry["availability"] == "loaded" and rec["sha256"] != entry["sha256"]:
            return False, ("authority",)
        if entry["availability"] == "unavailable" and (
            rec.get("failure_class") != entry.get("failure_class")
            or rec.get("failure_sha256") != entry.get("failure_sha256")
        ):
            return False, ("authority",)
        eid = rec.get("evidence_id" if entry["availability"] == "loaded" else "failure_evidence_id")
        want = rec["sha256"] if entry["availability"] == "loaded" else rec.get("failure_sha256")
        ev = state["evidence"].get(eid) if isinstance(eid, str) else None
        cid = ev.get("content_id") if isinstance(ev, dict) else None
        have = cid[len("sha256:") :] if isinstance(cid, str) and cid.startswith("sha256:") else None
        if want is None or have != want:
            return False, ("authority",)
    extra = set(state["authorities"]) - {e["authority_id"] for e in entries}
    current_extra = {aid for aid in extra if _current(state, state["authorities"][aid], aid)}
    if current_extra:
        return False, ("authority",)
    return True, ()


def impact_maps_complete(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    ok, _ = has_current_snapshot(state, policies)
    if not ok:
        return False, ("impact-map",)
    for role in model.MAPPER_ROLES:
        maps = [
            m for m in state["impact_maps"].values() if m["role"] == role and _current(state, m, m["impact_map_id"])
        ]
        if len(maps) != 1:
            return False, ("impact-map",)
    return True, ()


def _map_union(state: dict) -> dict[str, dict]:
    """Union over current impact maps: surface -> {categories, hazards, consequences}."""
    union: dict[str, dict] = {}
    for m in state["impact_maps"].values():
        if not _current(state, m, m["impact_map_id"]):
            continue
        for e in m["entries"]:
            slot = union.setdefault(e["surface"], {"categories": [], "hazards": [], "consequences": []})
            slot["categories"] = sorted(set(slot["categories"]) | {e["category"]})
            slot["hazards"] = sorted(set(slot["hazards"]) | set(e["hazards"]))
            slot["consequences"] = list(model.normalize_consequences(slot["consequences"], e["consequences"]))
    return union


def coverage_plan_covers_map_union(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    ok, _ = impact_maps_complete(state, policies)
    if not ok:
        return False, ("coverage",)
    union = _map_union(state)
    for surface, slot in union.items():
        for category in slot["categories"]:
            match = [
                o
                for o in state["obligations"].values()
                if _current(state, o, o["obligation_id"]) and o["category"] == category and surface in o["surfaces"]
            ]
            if not match:
                return False, ("coverage",)
    return True, ()


def scope_challenge_complete(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    inv = state["coverage_inventory"]
    if inv is None or not _current(state, inv, inv["coverage_inventory_id"]):
        return False, ("coverage-inventory",)
    att = state["reviews"].get(inv["challenger_attestation_id"])
    if att is None or not _current(state, att, inv["challenger_attestation_id"]):
        return False, ("coverage-inventory",)
    d = state["dispatches"].get(att["dispatch_id"])
    if d is None or _role_of_dispatch(state, d) != "scope-challenger":
        return False, ("coverage-inventory",)
    # Challenger attestation must be assigned both map evidence ids.
    map_eids = {m["evidence_id"] for m in state["impact_maps"].values() if _current(state, m, m["impact_map_id"])}
    if not map_eids.issubset(set(d["context_evidence_ids"]) | set(d["assignment_ids"])):
        # The challenger's assignment ids are its dispatch assignment set; map
        # evidence ids must appear in the recorded context.
        return False, ("coverage-inventory",)
    # The inventory covers the union and may only add.
    union = _map_union(state)
    inv_surfaces = {e["surface"]: e for e in inv["entries"]}
    for surface, slot in union.items():
        entry = inv_surfaces.get(surface)
        if entry is None:
            return False, ("coverage-inventory",)
        for c in slot["categories"]:
            if c not in entry["categories"]:
                return False, ("coverage-inventory",)
        for h in slot["hazards"]:
            if h not in entry["hazards"]:
                return False, ("coverage-inventory",)
        for cons in slot["consequences"]:
            if cons not in entry["consequences"]:
                return False, ("coverage-inventory",)
    return True, ()


def _obligation_tier(obligation: dict) -> str:
    tier, _ = obligation_floor(obligation["scope_level"], obligation["risk"], obligation["consequences"])
    return tier


def coverage_complete(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    ok, reasons = scope_challenge_complete(state, policies)
    if not ok:
        return False, ("coverage",)
    inv = state["coverage_inventory"]
    for entry in inv["entries"]:
        if set(entry["categories"]) != set(model.OBLIGATION_CATEGORIES):
            return False, ("coverage",)
        for oid in entry["obligation_ids"]:
            o = state["obligations"].get(oid)
            if o is None or not _current(state, o, oid):
                return False, ("coverage",)
    for entry in inv["entries"]:
        for category in entry["categories"]:
            match = [
                state["obligations"][oid]
                for oid in entry["obligation_ids"]
                if oid in state["obligations"]
                and state["obligations"][oid]["category"] == category
                and entry["surface"] in state["obligations"][oid]["surfaces"]
            ]
            if not match:
                return False, ("coverage",)
    for oid, o in state["obligations"].items():
        if not _current(state, o, oid):
            continue
        if not o["assignees"]:
            return False, ("coverage",)
        if o["status"] not in ("covered", "not-applicable"):
            return False, ("coverage",)
        tier, reasoning = obligation_floor(o["scope_level"], o["risk"], o["consequences"])
        if _TIER_ORDER.index(o["minimum_capability_tier"]) < _TIER_ORDER.index(tier):
            return False, ("coverage",)
        if o["risk"] == "high":
            # A high-risk obligation needs an opposite-polarity hypothesis
            # pair in one family, bound through assignees.
            families = {}
            for aid in o["assignees"]:
                h = state["hypothesis_assignments"].get(aid)
                if h is not None and _current(state, h, aid):
                    families.setdefault(h["family"], set()).add(h["polarity"])
            if not any({"claim", "counterexample"} <= ps for ps in families.values()):
                return False, ("coverage",)
        if o["status"] == "not-applicable":
            if not o["not_applicable_attestation_ids"]:
                return False, ("coverage",)
            atts = [state["reviews"].get(a) for a in o["not_applicable_attestation_ids"]]
            if any(a is None or not _current(state, a, a["attestation_id"]) for a in atts):
                return False, ("coverage",)
            if o["risk"] == "high":
                chall = [
                    a
                    for a in atts
                    if a is not None
                    and _role_of_dispatch(state, state["dispatches"].get(a["dispatch_id"], {}))
                    == "exemption-challenger"
                ]
                if not chall:
                    return False, ("coverage",)
    return True, ()


def _check_green(state: dict, check: dict) -> bool:
    return (
        _current(state, check, check["check_id"])
        and check["conclusion"] == "success"
        and check["head_sha"] == state["snapshot"]["head_sha"]
    )


def preflight_current_and_green(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    ok, _ = has_current_snapshot(state, policies)
    if not ok:
        return False, ("preflight",)
    required = {i.policy_item_id for i in policies.local_checks.items if i.required}
    seen = set()
    for c in state["checks"].values():
        if c["kind"] != "preflight" or c["locus"] != "local" or not _current(state, c, c["check_id"]):
            continue
        if c["local_check_policy_sha256"] != policies.local_checks.sha256:
            return False, ("preflight",)
        if c["command_execution_policy_sha256"] != policies.command_execution.sha256:
            return False, ("preflight",)
        seen.add(c["policy_item_id"])
        if not _check_green(state, c):
            return False, ("preflight",)
    if seen != required:
        return False, ("preflight",)
    return True, ()


def _scheduled_gate_tier(obligation: dict) -> str:
    """The reviewer gate an obligation is scheduled at: the max of its computed
    floor and any override raise, clamped to the strongest ordered reviewer
    stage so a final-strong obligation still gates."""
    sched = max(
        (_obligation_tier(obligation), obligation["minimum_capability_tier"]),
        key=lambda t: _TIER_ORDER.index(t),
    )
    return _TIER_ORDER[min(_TIER_ORDER.index(sched), _TIER_ORDER.index("strong"))]


def _tier_reviews_complete(state: dict, tier: str) -> bool:
    """Every current obligation scheduled at `tier` has a clean current
    attestation at or above its own floor. A covered status is a claim, not a
    substitute: coverage without a witnessed review is incomplete. High-risk
    obligations require reviews from two distinct profile contracts."""
    obligations = [o for oid, o in state["obligations"].items() if _current(state, o, oid)]
    targets = [o for o in obligations if _scheduled_gate_tier(o) == tier]
    if not targets:
        # Vacuously complete only when nothing is scheduled at this tier.
        scheduled = [
            ha
            for ha in state["hypothesis_assignments"].values()
            if _current(state, ha, ha["hypothesis_assignment_id"]) and ha["minimum_capability_tier"] == tier
        ]
        return not scheduled
    for o in targets:
        if o["status"] == "not-applicable":
            continue
        need = _TIER_ORDER.index(
            max(
                (_obligation_tier(o), o["minimum_capability_tier"]),
                key=lambda t: _TIER_ORDER.index(t),
            )
        )
        profiles = set()
        for r in _reviews_of_role(state, "obligation-reviewer"):
            if not _current(state, r, r["attestation_id"]):
                continue
            if o["obligation_id"] not in r["assignment_ids"]:
                continue
            d = state["dispatches"].get(r["dispatch_id"])
            if d is None:
                continue
            rs = state["route_selections"].get(d["route_selection_id"])
            if rs is None:
                continue
            if _TIER_ORDER.index(rs["required_capability_tier"]) < need:
                continue
            if r["verdict"] not in ("clean", "findings"):
                continue
            profiles.add(rs["profile_sha256"])
        if not profiles:
            return False
        if o["risk"] == "high" and len(profiles) < 2:
            return False
    return True


def fast_reviews_complete(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    ok, _ = has_current_snapshot(state, policies)
    if not ok or not _tier_reviews_complete(state, "fast"):
        return False, ("fast-review",)
    return True, ()


def focused_reviews_complete(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    ok, _ = has_current_snapshot(state, policies)
    if not ok or not _tier_reviews_complete(state, "focused"):
        return False, ("focused-review",)
    return True, ()


def strong_reviews_complete(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    ok, _ = has_current_snapshot(state, policies)
    if not ok or not _tier_reviews_complete(state, "strong"):
        return False, ("strong-review",)
    return True, ()


_OPEN_DISPOSITIONS = ("open", "fixing", "review-repairing", "deferred", "contested", "unassessed")


def all_findings_closed(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    for f in state["findings"].values():
        if f["disposition"] in _OPEN_DISPOSITIONS or f["disposition"] == "accepted-risk":
            return False, ("finding-resolution",)
        res = f.get("resolution") or {}
        if f["disposition"] == "fixed":
            if not res.get("review_id") or not res.get("check_id"):
                return False, ("finding-resolution",)
            rev = state["reviews"].get(res["review_id"])
            chk = state["checks"].get(res["check_id"])
            rev_ok = rev is not None and _current(state, rev, res["review_id"])
            chk_ok = chk is not None and _current(state, chk, res["check_id"])
            if not rev_ok or not chk_ok:
                return False, ("finding-resolution",)
            if chk["conclusion"] != "success":
                return False, ("finding-resolution",)
        elif f["disposition"] == "review-repaired":
            repair = next(
                (r for r in state["review_repairs"].values() if r["finding_id"] == f["finding_id"]),
                None,
            )
            if repair is None or repair["status"] != "closed" or not _current(state, repair, repair["repair_id"]):
                return False, ("finding-resolution",)
            if not repair["verification_attestation_id"]:
                return False, ("finding-resolution",)
        elif f["disposition"] == "false-positive":
            rid = res.get("review_id")
            rev = state["reviews"].get(rid) if rid else None
            if rev is None or not _current(state, rev, rid):
                return False, ("finding-resolution",)
        elif f["disposition"] == "accepted-risk":
            return False, ("finding-resolution",)
    return True, ()


def review_repairs_current_and_verified(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    for r in state["review_repairs"].values():
        if not _current(state, r, r["repair_id"]):
            return False, ("review-repair",)
        if r["status"] == "repairing":
            return False, ("review-repair",)
        if r["status"] in ("verified", "closed"):
            att_id = r["verification_attestation_id"]
            att = state["reviews"].get(att_id) if att_id else None
            if att is None or not _current(state, att, att_id):
                return False, ("review-repair",)
    return True, ()


def _role_attestation_clean(state: dict, role: str) -> bool:
    atts = _reviews_of_role(state, role)
    current = [a for a in atts if _current(state, a, a["attestation_id"])]
    if not current:
        return False
    return any(a["verdict"] == "clean" and a["audit_result"] == "clean" for a in current)


def blind_final_current_and_clean(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    if not _role_attestation_clean(state, "blind-final"):
        return False, ("blind-final-review",)
    return True, ()


def closure_audit_current_and_clean(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    if not _role_attestation_clean(state, "closure-auditor"):
        return False, ("closure-audit",)
    return True, ()


def remote_ci_candidate_current(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    ci = state["ci_candidate"]
    ready = state["ready_transition"]
    if ci is None or ready is None:
        return False, ("remote-ci",)
    if not _current(state, ci, ci["ci_candidate_id"]) or not _current(state, ready, ready["ready_transition_id"]):
        return False, ("remote-ci",)
    if ready["status"] != "completed" or ready["transition_witness_id"] is None:
        return False, ("remote-ci",)
    if ci["transition_witness_id"] != ready["transition_witness_id"]:
        return False, ("remote-ci",)
    if ci["head_sha"] != state["snapshot"]["head_sha"]:
        return False, ("remote-ci",)
    return True, ()


def remote_ci_current_and_green(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    ok, reasons = remote_ci_candidate_current(state, policies)
    if not ok:
        return False, ("remote-ci",)
    hosted = [
        c
        for c in state["checks"].values()
        if c["kind"] == "remote-ci" and c["locus"] == "hosted" and _current(state, c, c["check_id"])
    ]
    if not hosted:
        return False, ("remote-ci",)
    for c in hosted:
        if not _check_green(state, c):
            return False, ("remote-ci",)
    return True, ()


def remote_identity_matches(state: dict, policies, observation: dict | None) -> tuple[bool, tuple[str, ...]]:
    snap = state["snapshot"]
    if snap is None or observation is None:
        return False, ("remote-head-identity",)
    if (
        observation["repository_id"] != snap["repository_id"]
        or observation["pr_number"] != snap["pr_number"]
        or observation["head_sha"] != snap["head_sha"]
        or observation["authority_manifest_sha256"] != snap["authority_manifest_sha256"]
        or observation["unresolved_feedback_sha256"] != snap["unresolved_feedback_sha256"]
    ):
        return False, ("remote-head-identity",)
    return True, ()


def presentation_observation_matches_candidate(
    state: dict, policies, observation: dict | None
) -> tuple[bool, tuple[str, ...]]:
    if observation is None:
        return False, ("presentation-recheck",)
    # The fresh observation's subject must equal the stored remote-observation
    # witness subject for every current hosted check.
    obs_subject = model.sha256_json(model.remote_observation_subject(observation))
    hosted = [
        c
        for c in state["checks"].values()
        if c["kind"] == "remote-ci" and c["locus"] == "hosted" and _current(state, c, c["check_id"])
    ]
    if not hosted:
        return False, ("presentation-recheck",)
    for c in hosted:
        w = state["witness_records"].get(c["remote_observation_witness_id"])
        if w is None or w["subject_sha256"] != obs_subject:
            return False, ("presentation-recheck",)
    return True, ()


def witnesses_verified(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    if policies is None or policies.witness_verifier is None:
        return False, ("witnesses",)
    seen_tool_use = set()
    seen_agent = set()
    for wid, record in state["witness_records"].items():
        if not _current(state, record, wid):
            continue
        try:
            _verify_witness(state, policies, record)
        except (WitnessVerificationError, model.StateValidationError, OSError):
            return False, ("witnesses",)
        # Execution identity is unique per dispatch, not per record: a launch
        # and its completion legitimately share the agent they describe.
        # Uniqueness is enforced within each witness kind.
        if record["tool_use_id"] is not None:
            key = (record["kind"], record["tool_use_id"])
            if key in seen_tool_use:
                return False, ("witnesses",)
            seen_tool_use.add(key)
        if record["agent_id"] is not None and record["kind"] == "review-launch":
            if record["agent_id"] in seen_agent:
                return False, ("witnesses",)
            seen_agent.add(record["agent_id"])
    # Every current dispatch must bind a current profile-resolution witness;
    # a reported dispatch must additionally bind its current launch+completion
    # pair. Non-empty ids are not enough: the referenced records must be
    # present and in-epoch.
    for d in _current_records(state, "dispatches"):
        prw_id = d["profile_resolution_witness_id"]
        prw = state["witness_records"].get(prw_id)
        if prw is None or not _current(state, prw, prw_id):
            return False, ("witnesses",)
        if d["status"] == "reported":
            for wid in (d["launch_witness_id"], d["completion_witness_id"]):
                w = state["witness_records"].get(wid) if wid else None
                if w is None or not _current(state, w, wid):
                    return False, ("witnesses",)
    return True, ()


def verify_witness(state: dict, policies, record: dict, *, dispatch_id: str | None = None) -> VerifiedWitness:
    """Public wrapper: verify one stored witness record against its expected
    kind, review, epoch, subject, and execution identity."""
    return _verify_witness(state, policies, record, dispatch_id=dispatch_id)


def _floors_satisfied(state: dict, policies) -> tuple[bool, tuple[str, ...]]:
    """Every current dispatch meets its role and assignment floors."""
    profiles = getattr(policies, "available_profiles", None)
    for d in _current_records(state, "dispatches"):
        rs = state["route_selections"].get(d["route_selection_id"])
        if rs is None or not _current(state, rs, d["route_selection_id"]):
            return False, ("reasoning-floor",)
        role = rs["required_role"]
        if profiles is not None and rs["profile"] not in profiles:
            return False, ("reasoning-floor",)
        floor_t, floor_r = _role_floor(role)
        if role == "obligation-reviewer":
            floor_t = "fast"
            floor_r = "low"
            for aid in d["assignment_ids"]:
                o = state["obligations"].get(aid)
                if o is not None:
                    t, r = obligation_floor(o["scope_level"], o["risk"], o["consequences"])
                    floor_t = _tier_max(floor_t, t)
                    floor_r = max(floor_r, r, key=lambda x: model.REASONING_FLOORS.index(x))
        if _TIER_ORDER.index(rs["required_capability_tier"]) < _TIER_ORDER.index(floor_t):
            return False, ("reasoning-floor",)
        if model.REASONING_FLOORS.index(rs["selected_reasoning"]) < model.REASONING_FLOORS.index(floor_r):
            return False, ("reasoning-floor",)
        if rs["selected_context_mode"] != "fresh":
            return False, ("reasoning-floor",)
        if role in ("blind-final", "closure-auditor"):
            if role not in rs["qualified_roles"]:
                return False, ("reasoning-floor",)
    return True, ()


# ---------------------------------------------------------------------------
# evaluate_green


def evaluate_green(state: dict, remote_observation, *, policies, now) -> Decision:
    if isinstance(remote_observation, (bytes, bytearray)):
        observation = _decode(bytes(remote_observation), "remote-observation")
    else:
        observation = remote_observation
    if observation is not None:
        try:
            model.validate_remote_observation(observation)
        except model.StateValidationError:
            observation = None

    missing: list[str] = []

    def run(pred, name, *extra):
        ok, reasons = pred(state, policies, *extra) if extra else pred(state, policies)
        if not ok:
            for r in reasons or (name,):
                if r not in missing:
                    missing.append(r)
        return ok

    if state["status"] == "blocked" or _active_blocker(state):
        missing.append("blocker-active")
    run(has_current_snapshot, "snapshot")
    run(authority_manifest_complete, "authority-manifest")
    run(authorities_complete, "authority")
    run(impact_maps_complete, "impact-map")
    run(scope_challenge_complete, "coverage-inventory")
    run(coverage_complete, "coverage")
    run(preflight_current_and_green, "preflight")
    run(fast_reviews_complete, "fast-review")
    run(focused_reviews_complete, "focused-review")
    run(strong_reviews_complete, "strong-review")
    run(all_findings_closed, "finding-resolution")
    run(review_repairs_current_and_verified, "review-repair")
    run(blind_final_current_and_clean, "blind-final-review")
    run(closure_audit_current_and_clean, "closure-audit")
    run(remote_ci_candidate_current, "remote-ci")
    run(remote_ci_current_and_green, "remote-ci")
    run(remote_identity_matches, "remote-head-identity", observation)
    run(presentation_observation_matches_candidate, "presentation-recheck", observation)
    floors_ok, floors_reasons = _floors_satisfied(state, policies)
    if not floors_ok:
        for r in floors_reasons:
            if r not in missing:
                missing.append(r)
    run(witnesses_verified, "witnesses")

    if observation is not None:
        try:
            observed_at = datetime.fromisoformat(observation["observed_at"])
        except (ValueError, TypeError):
            observed_at = None
        now_dt = now if isinstance(now, datetime) else None
        if observed_at is None or now_dt is None:
            if "presentation-recheck" not in missing:
                missing.append("presentation-recheck")
        else:
            if observed_at.tzinfo is None:
                observed_at = observed_at.replace(tzinfo=timezone.utc)
            if now_dt.tzinfo is None:
                now_dt = now_dt.replace(tzinfo=timezone.utc)
            age = now_dt - observed_at
            if age > timedelta(seconds=60) or age < -timedelta(seconds=5):
                if "presentation-recheck" not in missing:
                    missing.append("presentation-recheck")
    else:
        if "presentation-recheck" not in missing:
            missing.append("presentation-recheck")

    if missing:
        status = None
        accepted = [f for f in state["findings"].values() if f["disposition"] == "accepted-risk"]
        if accepted and not [f for f in state["findings"].values() if f["disposition"] in _OPEN_DISPOSITIONS]:
            status = "reviewed-with-exceptions"
        return Decision(False, "blocked", "; ".join(missing), tuple(missing), status=status)
    return Decision(True, "seal-green", "all green predicates satisfied")


# ---------------------------------------------------------------------------
# next_action


def _findings_requiring_adjudication(state: dict) -> list[dict]:
    """Open/unassessed findings that lack a recorded adjudication outcome."""
    return [
        f
        for f in state["findings"].values()
        if f["disposition"] in ("open", "unassessed") and not (f.get("resolution") or {}).get("outcome")
    ]


def _findings_awaiting_branch(state: dict) -> list[dict]:
    """Open findings with a recorded adjudication outcome not yet branched."""
    return [
        f
        for f in state["findings"].values()
        if f["disposition"] in ("open", "unassessed") and (f.get("resolution") or {}).get("outcome")
    ]


def _branch_actions_for(finding: dict) -> tuple[str, ...]:
    """Lawful branch actions for one adjudicated open finding."""
    res = finding.get("resolution") or {}
    outcome = res.get("outcome")
    if outcome == "false-positive":
        return ("close-false-positive",)
    if outcome == "confirmed":
        rc = res.get("remediation_class")
        if rc == "candidate-change":
            return ("enter-fixing", "accept-risk")
        if rc == "review-process":
            return ("enter-review-repair", "accept-risk")
    return ()


def _findings_fixing(state: dict) -> list[dict]:
    return [f for f in state["findings"].values() if f["disposition"] == "fixing"]


def _findings_repairing(state: dict) -> list[dict]:
    return [f for f in state["findings"].values() if f["disposition"] == "review-repairing"]


def _finding_closure_broken(state: dict, finding: dict) -> bool:
    """A closed-claimed finding whose required proof is missing or not current."""
    res = finding.get("resolution") or {}
    if finding["disposition"] == "fixed":
        rev = state["reviews"].get(res.get("review_id"))
        chk = state["checks"].get(res.get("check_id"))
        return not (
            res.get("review_id")
            and res.get("check_id")
            and rev is not None
            and _current(state, rev, res["review_id"])
            and chk is not None
            and _current(state, chk, res["check_id"])
            and chk["conclusion"] == "success"
        )
    if finding["disposition"] == "review-repaired":
        # Broken only when no current repair record exists to close; a live
        # repair is review-repair work, not a re-adjudication.
        repair = next(
            (
                r
                for r in state["review_repairs"].values()
                if r["finding_id"] == finding["finding_id"] and _current(state, r, r["repair_id"])
            ),
            None,
        )
        return repair is None or (repair["status"] == "closed" and not repair["verification_attestation_id"])
    if finding["disposition"] == "false-positive":
        return not res.get("review_id")
    return False


def _findings_broken_closure(state: dict) -> list[dict]:
    return [f for f in state["findings"].values() if _finding_closure_broken(state, f)]


def _open_repairs(state: dict) -> list[dict]:
    """Current repairs whose lifecycle is not yet closed."""
    return [r for rid, r in state["review_repairs"].items() if _current(state, r, rid) and r["status"] != "closed"]


def _pending_exemptions(state: dict) -> list[dict]:
    out = []
    for o in state["obligations"].values():
        if not _current(state, o, o["obligation_id"]) or o["status"] != "not-applicable" or o["risk"] != "high":
            continue
        atts = [state["reviews"].get(a) for a in o["not_applicable_attestation_ids"]]
        has_challenger = any(
            a is not None
            and _current(state, a, a["attestation_id"])
            and _role_of_dispatch(state, state["dispatches"].get(a["dispatch_id"], {})) == "exemption-challenger"
            for a in atts
        )
        if not has_challenger:
            out.append(o)
    return out


def _active_blocker(state: dict) -> bool:
    return any(b["active"] for b in state["blockers"].values())


def _derived_stage(state: dict, policies) -> str:
    """The lawful stage recomputed from records; the stored stage is display only."""
    if state["status"] == "blocked" or _active_blocker(state):
        return "blocked"
    if state["snapshot"] is None:
        return "intake"
    ok, _ = authority_manifest_complete(state, policies)
    ok2, _ = authorities_complete(state, policies)
    if not (ok and ok2):
        return "intake"
    ok, _ = impact_maps_complete(state, policies)
    if not ok:
        return "authority"
    ok, _ = coverage_plan_covers_map_union(state, policies)
    if not ok:
        return "impact-mapping"
    ok, _ = scope_challenge_complete(state, policies)
    if not ok:
        return "coverage"
    ok, _ = coverage_complete(state, policies)
    if not ok:
        return "coverage-challenge"
    ok, _ = preflight_current_and_green(state, policies)
    if not ok:
        return "preflight"
    ok, _ = fast_reviews_complete(state, policies)
    if not ok:
        return "fast-review"
    ok, _ = focused_reviews_complete(state, policies)
    if not ok:
        return "focused-review"
    ok, _ = strong_reviews_complete(state, policies)
    if not ok:
        return "strong-review"
    if _findings_requiring_adjudication(state) or _findings_fixing(state) or _findings_broken_closure(state):
        return "resolution"
    if _findings_repairing(state) or _open_repairs(state):
        # A repair whose invalidation cut reaches final/closure or remote
        # proof must wait for those gates to be re-proven before the
        # repair stage permits verification.
        cut_ids = _invalidated_ids(state)
        if cut_ids:
            reaches_final = any(
                rid in cut_ids
                and _role_of_dispatch(
                    state,
                    state["dispatches"].get(r["dispatch_id"], {}),
                )
                in ("blind-final", "closure-auditor")
                for rid, r in state["reviews"].items()
            )
            reaches_remote = (
                any(cid in cut_ids and c["kind"] == "remote-ci" for cid, c in state["checks"].items())
                or (
                    state["ready_transition"] is not None
                    and state["ready_transition"]["ready_transition_id"] in cut_ids
                )
                or (state["ci_candidate"] is not None and state["ci_candidate"]["ci_candidate_id"] in cut_ids)
            )
            if reaches_final:
                if not blind_final_current_and_clean(state, policies)[0]:
                    return "final-review"
                if not closure_audit_current_and_clean(state, policies)[0]:
                    return "closure-audit"
            if reaches_remote:
                if not remote_ci_candidate_current(state, policies)[0]:
                    return "remote-ci-candidate"
                if not remote_ci_current_and_green(state, policies)[0]:
                    return "remote-ci"
        return "review-repair"
    ok, _ = blind_final_current_and_clean(state, policies)
    if not ok:
        return "final-review"
    ok, _ = closure_audit_current_and_clean(state, policies)
    if not ok:
        return "closure-audit"
    ok, _ = remote_ci_candidate_current(state, policies)
    if not ok:
        return "remote-ci-candidate"
    ok, _ = remote_ci_current_and_green(state, policies)
    if not ok:
        return "remote-ci"
    return "green-candidate"


def _stored_remote_observation(state: dict) -> dict | None:
    """Decode the current remote-observation evidence payload, if any.

    Multiple remote-observation records may be current (re-observation after
    a hosted-check repair); prefer the evidence a current hosted remote-ci
    check binds, since that is the observation the checks were proven
    against."""
    bound = {
        c["evidence_id"]
        for c in state["checks"].values()
        if c["kind"] == "remote-ci" and c["locus"] == "hosted" and _current(state, c, c["check_id"])
    }
    first = None
    for eid, ev in state["evidence"].items():
        if ev["kind"] != "remote-observation" or not _current(state, ev, eid):
            continue
        try:
            obs = _decode(_evidence_bytes(state, eid), "remote-observation")
        except (model.StateValidationError, OSError, ValueError):
            return None
        try:
            model.validate_remote_observation(obs)
        except model.StateValidationError:
            return None
        if eid in bound:
            return obs
        if first is None:
            first = obs
    return first


def _green_gate(state: dict, policies) -> tuple[bool, list[str]]:
    """The seal-green gate: every predicate the derived stage does not cover.

    Uses the stored remote-observation evidence as the observation; live
    freshness is a presentation-time property of evaluate_green.
    """
    missing: list[str] = []

    def run(pred, name, *extra):
        ok, reasons = pred(state, policies, *extra) if extra else pred(state, policies)
        if not ok:
            for r in reasons or (name,):
                if r not in missing:
                    missing.append(r)
        return ok

    run(all_findings_closed, "finding-resolution")
    run(review_repairs_current_and_verified, "review-repair")
    run(_floors_satisfied, "reasoning-floor")
    run(witnesses_verified, "witnesses")
    obs = _stored_remote_observation(state)
    run(remote_identity_matches, "remote-head-identity", obs)
    run(presentation_observation_matches_candidate, "presentation-recheck", obs)
    return (not missing), missing


# Advisory: evidence kinds an action payload is expected to reference. The
# kernel enforces reference resolution, not this list; it exists so `next`
# output can name the evidence the caller must supply.
_ACTION_EVIDENCE_KINDS = {
    "freeze-review-input": ("authority", "authority-manifest-payload"),
    "refresh-review-input": ("authority", "authority-manifest-payload"),
    "map-impact-semantic": ("impact-map", "review-attestation", "tool-transcript"),
    "map-impact-contract": ("impact-map", "review-attestation", "tool-transcript"),
    "plan-coverage": ("impact-map",),
    "challenge-coverage": ("review-attestation", "scope-challenge", "tool-transcript"),
    "run-preflight": ("check-output",),
    "run-fast-review": ("review-attestation", "tool-transcript"),
    "run-focused-review": ("review-attestation", "tool-transcript"),
    "run-strong-review": ("review-attestation", "tool-transcript"),
    "run-exemption-challenge": ("review-attestation", "tool-transcript"),
    "adjudicate-findings": ("finding-proof", "review-attestation", "tool-transcript"),
    "close-false-positive": ("finding-proof",),
    "enter-fixing": ("authority", "authority-manifest-payload", "fix-proof"),
    "run-fix-verification": ("check-output",),
    "review-fix": ("fix-proof", "review-attestation", "tool-transcript"),
    "close-fixed": ("fix-proof",),
    "enter-review-repair": ("review-repair-proof",),
    "verify-review-repair": ("review-attestation", "tool-transcript"),
    "close-review-repaired": ("review-repair-proof",),
    "accept-risk": ("human-decision",),
    "resume-review": ("check-output", "finding-proof", "human-decision", "review-repair-proof"),
    "run-final-review": ("review-attestation", "tool-transcript"),
    "run-closure-audit": ("review-attestation", "tool-transcript"),
    "mark-ready-for-ci": (),
    "run-remote-ci": ("remote-observation",),
    "seal-green": (),
}


def _recipe(action: str, state_path: str = "<state>") -> ActionRecipe:
    role = _ACTION_ROLE.get(action)
    if role:
        floor_t, floor_r = _role_floor(role)
        if role == "obligation-reviewer":
            floor_t, floor_r = None, None
        dispatch_required = True
    else:
        floor_t = floor_r = None
        dispatch_required = False
    verb = "dispatch" if dispatch_required else "complete"
    return ActionRecipe(
        action=action,
        dispatch_required=dispatch_required,
        required_role=role,
        minimum_capability_tier=floor_t,
        minimum_reasoning_floor=floor_r,
        preferred_profile=None,
        data_keys=tuple(sorted(ACTION_PAYLOAD_KEYS.get(action, ()))),
        evidence_kinds=_ACTION_EVIDENCE_KINDS.get(action, ()),
        record_command=(f"py -3 scripts/reviewctl.py {verb} --action {action} --state {state_path} --apply"),
    )


def action_recipe(action: str, *, state_path: str = "<state>") -> ActionRecipe:
    """Public recipe lookup for a known action name."""
    if action not in ACTION_PAYLOAD_KEYS:
        _fail("unknown-action", "action", f"unknown action {action!r}")
    return _recipe(action, state_path)


def lawful_actions(state: dict, *, policies) -> frozenset:
    """Public view of the action names lawful in the current state."""
    return _lawful_actions(state, policies)


def ready_idempotency_key(state: dict) -> str:
    """The idempotency key a pending ready-transition intent must carry for the
    current snapshot. Remote observers use this to recognize the exact intent
    they are asked to apply or confirm."""
    snap = state["snapshot"]
    return model.sha256_hex(
        model.canonical_json(
            {
                "action": "mark-ready-for-ci",
                "head_sha": snap["head_sha"],
                "pr_number": snap["pr_number"],
                "repository_id": snap["repository_id"],
                "review_id": state["review_id"],
                "snapshot_epoch": snap["epoch"],
            }
        )
    )


def next_action(state: dict, *, policies) -> Decision:
    if state["status"] == "blocked" or _active_blocker(state):
        return Decision(
            False,
            "resume-review",
            "review is blocked; resume requires blocker-resolution evidence",
            recipe=_recipe("resume-review"),
            status="blocked",
        )
    # Findings interrupt the linear ascent.
    if _findings_requiring_adjudication(state) or _findings_broken_closure(state):
        return Decision(
            True,
            "adjudicate-findings",
            "open findings require witnessed adjudication",
            recipe=_recipe("adjudicate-findings"),
        )
    for f in _findings_awaiting_branch(state):
        branches = _branch_actions_for(f)
        if branches:
            return Decision(
                True,
                branches[0],
                f"finding {f['finding_id']} adjudicated; a lawful branch must close it",
                recipe=_recipe(branches[0]),
            )
        return Decision(
            False,
            "blocked",
            f"finding {f['finding_id']} adjudication outcome authorizes no branch",
            status=None,
        )
    if _pending_exemptions(state) and scope_challenge_complete(state, policies)[0]:
        return Decision(
            True,
            "run-exemption-challenge",
            "high-risk not-applicable requires an exemption challenger",
            recipe=_recipe("run-exemption-challenge"),
        )
    fixing = _findings_fixing(state)
    if fixing and _derived_stage(state, policies) == "resolution":
        # Re-ascent complete; proceed through the fix lifecycle.
        f = fixing[0]
        if not _fix_check_current(state):
            return Decision(
                True,
                "run-fix-verification",
                "fix requires targeted checks",
                recipe=_recipe("run-fix-verification"),
            )
        if not _fix_review_current(state, f["finding_id"]):
            return Decision(True, "review-fix", "fix requires witnessed fix review", recipe=_recipe("review-fix"))
        return Decision(True, "close-fixed", "fix proof complete", recipe=_recipe("close-fixed"))
    repairing = _findings_repairing(state)
    if repairing and _derived_stage(state, policies) == "review-repair":
        repair = next(
            (r for r in state["review_repairs"].values() if r["finding_id"] == repairing[0]["finding_id"]),
            None,
        )
        if repair is not None and repair["status"] == "verified":
            return Decision(
                True,
                "close-review-repaired",
                "repair verified",
                recipe=_recipe("close-review-repaired"),
            )
        return Decision(
            True,
            "verify-review-repair",
            "repair requires independent verification",
            recipe=_recipe("verify-review-repair"),
        )

    stage = _derived_stage(state, policies)
    order = {
        "intake": "freeze-review-input",
        "authority": "map-impact-semantic",
        "impact-mapping": "plan-coverage",
        "coverage": "challenge-coverage",
        "coverage-challenge": "challenge-coverage",
        "preflight": "run-preflight",
        "fast-review": "run-fast-review",
        "focused-review": "run-focused-review",
        "strong-review": "run-strong-review",
        "final-review": "run-final-review",
        "closure-audit": "run-closure-audit",
        "remote-ci-candidate": "mark-ready-for-ci",
        "remote-ci": "run-remote-ci",
        "green-candidate": "seal-green",
    }
    # finer-grained: which of the two maps is missing
    if stage == "authority":
        if not [
            m
            for m in state["impact_maps"].values()
            if m["role"] == "impact-mapper-semantic" and _current(state, m, m["impact_map_id"])
        ]:
            action = "map-impact-semantic"
        else:
            action = "map-impact-contract"
    elif stage == "coverage":
        action = "challenge-coverage"
    else:
        action = order.get(stage)
    if stage == "green-candidate":
        ok, missing = _green_gate(state, policies)
        if ok:
            return Decision(True, "seal-green", "all gates complete", recipe=_recipe("seal-green"))
        return Decision(
            False,
            "blocked",
            "green-candidate stage but extended green gate fails: " + "; ".join(missing),
            missing=tuple(missing),
        )
    if action is None:
        return Decision(
            False,
            "blocked",
            f"no lawful action derives for stage {stage!r}",
        )
    return Decision(
        True,
        action,
        f"{action} is the earliest incomplete gate",
        recipe=_recipe(action),
    )


# ---------------------------------------------------------------------------
# Mutations (pure; the engine wraps them in locked transactions)


def _require_keys(data: dict, action: str) -> None:
    allowed = ACTION_PAYLOAD_KEYS.get(action)
    if allowed is None:
        _fail("bad-action", "action", f"unknown action {action!r}")
    keys = set(data)
    extra = keys - allowed
    missing = allowed - keys
    if extra:
        _fail("unknown-field", "data", f"unexpected payload key(s) {sorted(extra)}")
    if missing:
        _fail("missing-field", "data", f"missing payload key(s) {sorted(missing)}")


def _set_stage(state: dict, policies) -> None:
    state["stage"] = _derived_stage(state, policies)


def _bind_snapshot_defaults(out: dict, record: dict, path: str) -> None:
    """Fill snapshot binding from the installed snapshot, or require the
    record to carry its own (snapshot actions bind the candidate snapshot)."""
    snap = out["snapshot"]
    if snap is not None:
        record.setdefault("snapshot_epoch", snap["epoch"])
        record.setdefault("snapshot_fingerprint", snap["fingerprint"])
    if record.get("snapshot_epoch") is None or record.get("snapshot_fingerprint") is None:
        _fail("missing-field", path, "record lacks snapshot binding and no snapshot is installed")


def _validate_candidate(out: dict) -> None:
    """Validate a mutation result that may be mid-transaction.

    The store bumps ``candidate.generation`` before the transaction's single
    history record exists, so an intermediate state can legitimately hold
    ``generation == history[-1].generation + 1``. The tail-generation
    coupling is enforced again at ``commit()``; here we validate everything
    else by normalizing only that pending-record shape.
    """
    history = out["history"]
    if history and out["generation"] == history[-1]["generation"] + 1:
        prior = out["generation"]
        out["generation"] = history[-1]["generation"]
        try:
            model.validate_state(out, verify_content=False)
        finally:
            out["generation"] = prior
        return
    model.validate_state(out, verify_content=False)


def register_dispatch(state: dict, *, route_selection: dict, dispatch: dict, policies) -> dict:
    """Persist a pending dispatch and its resolved route identity."""
    out = copy.deepcopy(state)
    rs = dict(route_selection)
    d = dict(dispatch)
    _bind_snapshot_defaults(out, rs, "route_selection")
    rs["route_selection_id"] = model.derived_id("route", rs["snapshot_epoch"], model.route_selection_subject(rs))
    _bind_snapshot_defaults(out, d, "dispatch")
    d["route_selection_id"] = rs["route_selection_id"]
    d["status"] = "pending"
    d["launch_witness_id"] = None
    d["completion_witness_id"] = None
    d["agent_id"] = None
    d["tool_use_id"] = None
    d["transcript_sha256"] = None
    d["dispatch_id"] = model.derived_id("dispatch", d["snapshot_epoch"], model.pending_dispatch_intent_subject(d))
    out["route_selections"][rs["route_selection_id"]] = rs
    out["dispatches"][d["dispatch_id"]] = d
    _validate_candidate(out)
    return out


def record_launch(state: dict, *, dispatch_id: str, launch_witness_bytes: bytes, policies) -> dict:
    out = copy.deepcopy(state)
    d = out["dispatches"].get(dispatch_id)
    if d is None:
        _fail("dangling-ref", "dispatch_id", f"unknown dispatch {dispatch_id!r}")
    record = _decode(bytes(launch_witness_bytes), "launch-witness")
    if record.get("kind") != "review-launch":
        _fail("wrong-kind", "launch_witness", "witness kind must be review-launch")
    _bind_snapshot_defaults(out, record, "launch_witness")
    record["witness_id"] = model.derived_id("witness", record["snapshot_epoch"], model.witness_record_subject(record))
    d["launch_witness_id"] = record["witness_id"]
    d["tool_use_id"] = record["tool_use_id"]
    d["agent_id"] = record.get("agent_id")
    out["witness_records"][record["witness_id"]] = record
    _validate_candidate(out)
    return out


def record_completion(
    state: dict, *, dispatch_id: str, completion_witness_bytes: bytes, transcript_sha256: str | None = None, policies
) -> dict:
    """Bind the witnessed review completion to a launched dispatch."""
    out = copy.deepcopy(state)
    d = out["dispatches"].get(dispatch_id)
    if d is None:
        _fail("dangling-ref", "dispatch_id", f"unknown dispatch {dispatch_id!r}")
    if not d["launch_witness_id"]:
        _fail("unlawful-transition", "dispatch_id", "dispatch has no launch witness")
    if d["completion_witness_id"]:
        _fail("conflict", "dispatch_id", "dispatch already has a completion witness")
    record = _decode(bytes(completion_witness_bytes), "completion-witness")
    if record.get("kind") != "review-completion":
        _fail("wrong-kind", "completion_witness", "witness kind must be review-completion")
    _bind_snapshot_defaults(out, record, "completion_witness")
    record["witness_id"] = model.derived_id("witness", record["snapshot_epoch"], model.witness_record_subject(record))
    d["completion_witness_id"] = record["witness_id"]
    if record.get("agent_id") is not None:
        d["agent_id"] = record["agent_id"]
    if transcript_sha256 is not None:
        d["transcript_sha256"] = transcript_sha256
    d["status"] = "reported"
    out["witness_records"][record["witness_id"]] = record
    _validate_candidate(out)
    return out


def record_witness(state: dict, *, witness_bytes: bytes, kind: str, policies) -> dict:
    """Ingest a witness record that binds to state only through its subject
    (remote-observation, human-decision, authority-discovery)."""
    out = copy.deepcopy(state)
    record = _decode(bytes(witness_bytes), f"{kind}-witness")
    if record.get("kind") != kind:
        _fail("wrong-kind", "witness", f"witness kind must be {kind}")
    _bind_snapshot_defaults(out, record, "witness")
    record["witness_id"] = model.derived_id("witness", record["snapshot_epoch"], model.witness_record_subject(record))
    out["witness_records"][record["witness_id"]] = record
    _validate_candidate(out)
    return out


def record_ready_transition(
    state: dict, *, transition_witness_bytes: bytes, observed_prior_lifecycle: str, policies
) -> dict:
    """Finalize a pending ready-transition intent from its remote-transition
    witness and create the ci_candidate atomically."""
    out = copy.deepcopy(state)
    ready = out["ready_transition"]
    if ready is None or not _current(out, ready, ready["ready_transition_id"]):
        _fail("missing-intent", "ready_transition", "no current ready-transition intent")
    if ready["status"] == "completed":
        _fail("conflict", "ready_transition", "ready transition already completed")
    record = _decode(bytes(transition_witness_bytes), "remote-transition")
    if record.get("kind") != "remote-transition":
        _fail("wrong-kind", "transition_witness", "witness kind must be remote-transition")
    _bind_snapshot_defaults(out, record, "transition_witness")
    record["witness_id"] = model.derived_id("witness", record["snapshot_epoch"], model.witness_record_subject(record))
    ready["prior_lifecycle_state"] = observed_prior_lifecycle
    subject = model.remote_transition_subject(
        ready,
        tool_use_id=record["tool_use_id"],
        prior_lifecycle_state=observed_prior_lifecycle,
        result_lifecycle_state=ready["expected_lifecycle_state"],
    )
    if record["subject_sha256"] != model.sha256_json(subject):
        _fail(
            "subject-mismatch",
            "transition_witness",
            "remote-transition witness subject does not match the pending intent",
        )
    out["witness_records"][record["witness_id"]] = record
    ready["transition_witness_id"] = record["witness_id"]
    ready["status"] = "completed"
    ci = {
        "ci_candidate_id": "",
        "repository_id": ready["repository_id"],
        "pr_number": ready["pr_number"],
        "head_sha": ready["head_sha"],
        "lifecycle_state": ready["expected_lifecycle_state"],
        "transition_witness_id": record["witness_id"],
        "snapshot_epoch": ready["snapshot_epoch"],
        "snapshot_fingerprint": ready["snapshot_fingerprint"],
    }
    ci["ci_candidate_id"] = model.derived_id("ci-candidate", ready["snapshot_epoch"], model.ci_candidate_subject(ci))
    out["ci_candidate"] = ci
    _validate_candidate(out)
    return out


def _lawful_actions(state: dict, policies) -> frozenset:
    """The set of actions lawful in the current state.

    An open finding interrupts: until a witnessed adjudication outcome exists,
    only adjudicate-findings is lawful; afterwards only the branches its outcome
    authorizes. During fixing/review-repairing the ascent actions stay lawful
    but final/closure/ready/CI/seal are held back. refresh-review-input is
    lawful whenever the caller presents trusted drift (the handler proves it).
    """
    if state["status"] == "blocked" or _active_blocker(state):
        return frozenset({"resume-review"})
    lawful: set[str] = {"refresh-review-input"}
    open_findings = [f for f in state["findings"].values() if f["disposition"] in ("open", "unassessed")]
    if _findings_requiring_adjudication(state) or _findings_broken_closure(state):
        lawful.add("adjudicate-findings")
        return frozenset(lawful)
    if open_findings:
        for f in open_findings:
            lawful.update(_branch_actions_for(f))
        return frozenset(lawful)
    fixing = _findings_fixing(state)
    repairing = _findings_repairing(state)
    if fixing and _derived_stage(state, policies) == "resolution":
        for f in fixing:
            if not _fix_check_current(state):
                lawful.add("run-fix-verification")
            if not _fix_review_current(state, f["finding_id"]):
                lawful.add("review-fix")
            if _fix_check_current(state) and _fix_review_current(state, f["finding_id"]):
                lawful.add("close-fixed")
    if repairing and _derived_stage(state, policies) == "review-repair":
        # verify/close only once the invalidated gates have replacement records.
        for f in repairing:
            repair = next(
                (r for r in state["review_repairs"].values() if r["finding_id"] == f["finding_id"]),
                None,
            )
            if repair is not None and repair["status"] == "verified":
                lawful.add("close-review-repaired")
            else:
                lawful.add("verify-review-repair")
    if _pending_exemptions(state) and scope_challenge_complete(state, policies)[0]:
        lawful.add("run-exemption-challenge")
    stage = _derived_stage(state, policies)
    if stage == "authority":
        # Both independent mappers may complete in either order; only the
        # still-missing map action is lawful.
        for action, role in (
            ("map-impact-semantic", "impact-mapper-semantic"),
            ("map-impact-contract", "impact-mapper-contract"),
        ):
            if not [
                m for m in state["impact_maps"].values() if m["role"] == role and _current(state, m, m["impact_map_id"])
            ]:
                lawful.add(action)
        return frozenset(lawful)
    if stage == "green-candidate":
        ok, _ = _green_gate(state, policies)
        if ok:
            lawful.add("seal-green")
        return frozenset(lawful)
    action = _STAGE_ACTION.get(stage)
    if action is not None:
        if fixing:
            # A fixing finding holds final/closure/ready/CI/seal until closed;
            # only the re-ascent actions stay lawful.
            if action in _ASCENT_ACTIONS:
                lawful.add(action)
        else:
            # A review repair may lawfully re-run any gate its cut reached,
            # including final/closure/remote when the cut covers them.
            lawful.add(action)
    return frozenset(lawful)


_ASCENT_ACTIONS = frozenset(
    {
        "map-impact-semantic",
        "map-impact-contract",
        "plan-coverage",
        "challenge-coverage",
        "run-preflight",
        "run-fast-review",
        "run-focused-review",
        "run-strong-review",
        "run-exemption-challenge",
    }
)

_STAGE_ACTION = {
    "intake": "freeze-review-input",
    "authority": "map-impact-semantic",
    "impact-mapping": "plan-coverage",
    "coverage": "challenge-coverage",
    "coverage-challenge": "challenge-coverage",
    "preflight": "run-preflight",
    "fast-review": "run-fast-review",
    "focused-review": "run-focused-review",
    "strong-review": "run-strong-review",
    "final-review": "run-final-review",
    "closure-audit": "run-closure-audit",
    "remote-ci-candidate": "mark-ready-for-ci",
    "remote-ci": "run-remote-ci",
    "green-candidate": "seal-green",
}


def complete_action(state: dict, *, action: str, raw_data: bytes, policies) -> dict:
    data = _decode(bytes(raw_data), f"data:{action}")
    if not isinstance(data, dict):
        _fail("bad-type", "data", "action payload must be an object")
    _require_keys(data, action)
    out = copy.deepcopy(state)
    data_sha = model.sha256_hex(model.canonical_json(data))
    last = out["history"][-1] if out["history"] else None
    if last is not None and last["event"] == action and last["data_sha256"] == data_sha:
        # Identical replay of the step that just committed: a no-op.
        return out
    lawful = _lawful_actions(out, policies)
    if action not in lawful:
        if last is not None and last["event"] == action:
            _fail(
                "conflicting-replay",
                "data",
                f"action {action!r} already produced this generation with different content",
            )
        _fail(
            "unlawful-transition",
            "action",
            f"action {action!r} is not lawful here; lawful: {sorted(lawful)}",
        )
    handler = _COMPLETE.get(action)
    if handler is None:
        _fail("unimplemented", "action", f"action {action!r} has no Plan-1 handler")
    handler(out, data, policies)
    _set_stage(out, policies)
    store.append_history(out, event=action, data_sha256=data_sha)
    model.validate_state(out, verify_content=False)
    return out


def _install_snapshot(out: dict, snap: dict, manifest_payload_value: dict, manifest: dict, authorities: list) -> None:
    snap = dict(snap)
    snap["fingerprint"] = model.snapshot_fingerprint(snap)
    out["snapshot"] = snap
    manifest = dict(manifest)
    manifest.setdefault("snapshot_epoch", snap["epoch"])
    manifest.setdefault("snapshot_fingerprint", snap["fingerprint"])
    out["authority_manifest"] = manifest
    for rec in authorities:
        r = dict(rec)
        r.setdefault("snapshot_epoch", snap["epoch"])
        r.setdefault("snapshot_fingerprint", snap["fingerprint"])
        out["authorities"][r["authority_id"]] = r


def _bind_now(out: dict, rec: dict) -> dict:
    rec = dict(rec)
    rec.setdefault("snapshot_epoch", out["snapshot"]["epoch"])
    rec.setdefault("snapshot_fingerprint", out["snapshot"]["fingerprint"])
    return rec


def _install_attestations(out: dict, attestations: list) -> None:
    for a in attestations:
        rec = _bind_now(out, a)
        rec["attestation_id"] = ""
        rec["attestation_id"] = model.derived_id(
            "attestation", rec["snapshot_epoch"], model.review_wrapper_subject(rec)
        )
        out["reviews"][rec["attestation_id"]] = rec


def _install_findings(out: dict, findings: list) -> None:
    snap = out["snapshot"]
    for f in findings:
        rec = dict(f)
        rec.setdefault("discovered_snapshot_epoch", snap["epoch"])
        rec.setdefault("discovered_snapshot_fingerprint", snap["fingerprint"])
        rec["finding_id"] = "finding:" + model.sha256_json(model.finding_identity_subject(rec))
        out["findings"][rec["finding_id"]] = rec


def _install_discovery_witnesses(out: dict, witnesses: list) -> None:
    """Install the authority-discovery witness records carried inside a
    freeze/refresh payload. They run after ``_install_snapshot`` because the
    records bind the candidate snapshot's epoch and fingerprint, and they run
    inside ``complete_action`` so ``validate_state`` sees the manifest's
    ``discovery_witness_id`` resolve in the same atomic transition.
    """
    for i, rec in enumerate(witnesses):
        path = f"witnesses[{i}]"
        if not isinstance(rec, dict):
            _fail("bad-type", path, "witness record must be an object")
        if rec.get("kind") != "authority-discovery":
            _fail("wrong-kind", path, "freeze/refresh witnesses must be authority-discovery records")
        r = dict(rec)
        _bind_snapshot_defaults(out, r, path)
        r["witness_id"] = model.derived_id("witness", r["snapshot_epoch"], model.witness_record_subject(r))
        out["witness_records"][r["witness_id"]] = r


_FEEDBACK_SOURCE_ID_RE = re.compile(r"^[a-z-]+:[a-z-]+:.+$")


def _install_feedback_findings(out: dict, findings: list) -> None:
    """Install provider feedback from a freeze/refresh payload.

    Unlike _install_findings, a re-enumerated item never overwrites: finding
    identity is content-derived, so an existing record (open or closed) is
    durable and stays untouched - provider Resolve cannot reset lifecycle.
    """
    snap = out["snapshot"]
    for i, f in enumerate(findings):
        path = f"findings[{i}]"
        if not isinstance(f, dict):
            _fail("bad-finding", path, "feedback finding must be an object")
        if f.get("source_kind") != "feedback":
            _fail("bad-finding", path, "freeze/refresh findings must be provider feedback")
        sid = f.get("source_id")
        if not isinstance(sid, str) or not _FEEDBACK_SOURCE_ID_RE.match(sid):
            _fail("bad-finding", path, "source_id must be a provider canonical id")
        if f.get("source_assignment_id") != sid:
            _fail("bad-finding", path, "source_assignment_id must equal source_id")
        locations = f.get("locations")
        if not isinstance(locations, list) or not locations or sid not in locations:
            _fail("bad-finding", path, "locations must be a non-empty list containing source_id")
        if f.get("disposition") != "open" or f.get("resolution") is not None:
            _fail("bad-finding", path, "feedback findings must enter open and unresolved")
        rec = dict(f)
        rec.setdefault("discovered_snapshot_epoch", snap["epoch"])
        rec.setdefault("discovered_snapshot_fingerprint", snap["fingerprint"])
        rec["finding_id"] = "finding:" + model.sha256_json(model.finding_identity_subject(rec))
        if rec["finding_id"] in out["findings"]:
            continue
        out["findings"][rec["finding_id"]] = rec


def _install_checks(out: dict, checks: list) -> None:
    for c in checks:
        rec = _bind_now(out, c)
        out["checks"][rec["check_id"]] = rec


def _install_obligations(out: dict, records: list, policies, *, replace_categories=()) -> None:
    epoch = out["snapshot"]["epoch"]
    fp = out["snapshot"]["fingerprint"]
    if replace_categories:
        # Only current-epoch records are replaced; non-current records are
        # history and must persist so older dispatches/findings still resolve.
        doomed = {
            oid
            for oid, o in out["obligations"].items()
            if o["category"] in replace_categories and _current(out, o, oid)
        }
        for oid in doomed:
            del out["obligations"][oid]
        for hid, h in list(out["hypothesis_assignments"].items()):
            if h["obligation_id"] in doomed:
                del out["hypothesis_assignments"][hid]
    for o in records:
        rec = _bind_now(out, o)
        rec["obligation_id"] = model.derived_id("obligation", epoch, model.obligation_subject(rec))
        hids = []
        for h in policies.hypotheses.derive(obligation=rec):
            hrec = dict(h)
            hrec.setdefault("snapshot_epoch", epoch)
            hrec.setdefault("snapshot_fingerprint", fp)
            hrec["obligation_id"] = rec["obligation_id"]
            hrec["hypothesis_assignment_id"] = model.derived_id(
                "hypothesis", epoch, model.hypothesis_assignment_subject(hrec)
            )
            out["hypothesis_assignments"][hrec["hypothesis_assignment_id"]] = hrec
            hids.append(hrec["hypothesis_assignment_id"])
        rec["assignees"] = sorted(set(rec.get("assignees") or ()) | set(hids))
        out["obligations"][rec["obligation_id"]] = rec


def _finding_by_id(out: dict, finding_id: str) -> dict:
    f = out["findings"].get(finding_id)
    if f is None:
        _fail("dangling-ref", "resolutions", f"unknown finding {finding_id!r}")
    return f


def _fix_check_current(state: dict) -> bool:
    """A current successful targeted local check exists."""
    return any(
        c["kind"] == "targeted"
        and c["locus"] == "local"
        and c["conclusion"] == "success"
        and _current(state, c, c["check_id"])
        for c in state["checks"].values()
    )


def _fix_review_current(state: dict, finding_id: str) -> bool:
    """A current clean fix-reviewer attestation assigned to the finding."""
    for r in _reviews_of_role(state, "fix-reviewer"):
        d = state["dispatches"].get(r["dispatch_id"])
        if finding_id in d["assignment_ids"] and _current(state, r, r["attestation_id"]) and r["verdict"] == "clean":
            return True
    return False


def _repair_for_finding(out: dict, finding_id: str) -> dict | None:
    return next(
        (
            r
            for r in out["review_repairs"].values()
            if r["finding_id"] == finding_id and _current(out, r, r["repair_id"])
        ),
        None,
    )


def _downstream_cut(out: dict) -> set:
    """Records every later gate depends on: final/closure attestations,
    ready transition, CI candidate and hosted CI checks."""
    ids = set()
    for r in out["reviews"].values():
        d = out["dispatches"].get(r["dispatch_id"])
        if d is not None and _role_of_dispatch(out, d) in (
            "blind-final",
            "closure-auditor",
        ):
            ids.add(r["attestation_id"])
    for c in out["checks"].values():
        if c["kind"] == "remote-ci":
            ids.add(c["check_id"])
    if out["ready_transition"] is not None:
        ids.add(out["ready_transition"]["ready_transition_id"])
    if out["ci_candidate"] is not None:
        ids.add(out["ci_candidate"]["ci_candidate_id"])
    return ids


def _preflight_ids(out: dict) -> set:
    return {c["check_id"] for c in out["checks"].values() if c["kind"] == "preflight" and c["locus"] == "local"}


def _obligation_review_cut(out: dict, target_ids) -> set:
    """Named assignment dispatches/attestations plus same-or-higher review
    tiers for the affected obligations, per the closed table."""
    cut = set(target_ids)
    affected = set()
    for tid in target_ids:
        r = out["reviews"].get(tid)
        if r is not None:
            affected.update(r["assignment_ids"])
        d = out["dispatches"].get(tid)
        if d is not None:
            affected.update(d["assignment_ids"])
    affected = {a for a in affected if a in out["obligations"] or a in out["hypothesis_assignments"]}
    if not affected:
        return cut
    tiers = set()
    for oid in affected:
        o = out["obligations"].get(oid)
        if o is None:
            h = out["hypothesis_assignments"].get(oid)
            o = out["obligations"].get(h["obligation_id"]) if h else None
        if o is not None:
            tiers.add(_obligation_tier(o))
    lowest = min((_TIER_ORDER.index(t) for t in tiers), default=None)
    if lowest is None:
        return cut
    for r in out["reviews"].values():
        d = out["dispatches"].get(r["dispatch_id"])
        if d is None or _role_of_dispatch(out, d) != "obligation-reviewer":
            continue
        if not affected & set(r["assignment_ids"]):
            continue
        rs = out["route_selections"].get(d["route_selection_id"])
        if rs is None:
            continue
        if _TIER_ORDER.index(rs["required_capability_tier"]) >= lowest:
            cut.add(r["attestation_id"])
            cut.add(d["dispatch_id"])
    return cut


def _repair_cut(out: dict, target_kind: str, target_ids, finding_id: str) -> set:
    """The closed same-snapshot invalidation table from the design spec."""
    targets = set(target_ids)
    inv = out["coverage_inventory"]
    downstream = _downstream_cut(out)
    if target_kind in ("semantic-impact", "contract-impact"):
        cut = set(targets)
        if inv is not None:
            cut.add(inv["coverage_inventory_id"])
            cut.add(inv["challenger_attestation_id"])
        cut.update(out["obligations"])
        cut.update(out["hypothesis_assignments"])
        cut.update(_preflight_ids(out))
        return cut | downstream
    if target_kind in ("coverage-plan", "coverage-challenge"):
        cut = set()
        if inv is not None:
            cut.add(inv["coverage_inventory_id"])
            cut.add(inv["challenger_attestation_id"])
        cut.update(out["obligations"])
        cut.update(out["hypothesis_assignments"])
        cut.update(_preflight_ids(out))
        return cut | downstream
    if target_kind == "obligation-review":
        return _obligation_review_cut(out, target_ids) | downstream
    if target_kind == "exemption-review":
        cut = set(targets)
        for oid, o in out["obligations"].items():
            if targets & set(o["not_applicable_attestation_ids"]):
                cut.update(o["not_applicable_attestation_ids"])
                cut.add(oid)
        return cut | downstream
    if target_kind == "finding-adjudication":
        cut = set(targets)
        for f in out["findings"].values():
            res = f.get("resolution") or {}
            if res.get("adjudicator_attestation_id") in targets:
                for field in ("review_id", "check_id"):
                    if res.get(field):
                        cut.add(res[field])
                for rid, rep in out["review_repairs"].items():
                    if rep["finding_id"] == f["finding_id"]:
                        cut.add(rid)
        return cut | downstream
    if target_kind == "fix-review":
        cut = set(targets)
        for f in out["findings"].values():
            res = f.get("resolution") or {}
            if res.get("review_id") in targets:
                if res.get("check_id"):
                    cut.add(res["check_id"])
        return cut | downstream
    if target_kind == "blind-final":
        return set(targets) | downstream
    if target_kind == "closure-audit":
        return set(targets) | {
            cid
            for cid in downstream
            if not any(
                r["attestation_id"] == cid
                and _role_of_dispatch(out, out["dispatches"].get(r["dispatch_id"], {"route_selection_id": ""}))
                == "blind-final"
                for r in out["reviews"].values()
            )
        }
    if target_kind == "local-check":
        cut = set(targets)
        cut.update(out["reviews"])
        cut.update(out["review_repairs"])
        cut.update(c["check_id"] for c in out["checks"].values() if c["kind"] == "targeted")
        cut.update(downstream)
        return cut
    if target_kind == "hosted-check":
        # Named hosted check and seal only: the CI candidate is derived data
        # over the surviving ready transition, so it is not invalidated.
        return set(targets)
    _fail("bad-enum", "resolutions", f"unknown repair target kind {target_kind!r}")


def _h_freeze(out: dict, data: dict, policies) -> None:
    _install_snapshot(
        out,
        data["snapshot"],
        data["authority_manifest"],
        data["authority_manifest"],
        data["authorities"],
    )
    _install_discovery_witnesses(out, data["witnesses"])
    _install_feedback_findings(out, data["findings"])


def _h_refresh(out: dict, data: dict, policies) -> None:
    old = out["snapshot"]
    if old is None:
        _fail("unlawful-transition", "snapshot", "refresh requires an existing snapshot")
    new_snap = dict(data["snapshot"])
    if new_snap.get("epoch") != old["epoch"] + 1:
        _fail("bad-epoch", "snapshot", "refresh must advance exactly one epoch")
    content_fields = tuple(f for f in model.SNAPSHOT_SUBJECT_FIELDS if f != "epoch")
    old_subject = model._project(old, content_fields, path="snapshot")
    new_subject = model._project(new_snap, content_fields, path="snapshot")
    if old_subject == new_subject:
        _fail("no-drift", "snapshot", "byte-identical refresh is not lawful")
    if not data["drift_reasons"]:
        _fail("missing-field", "drift_reasons", "refresh requires drift reasons")
    _install_snapshot(
        out,
        new_snap,
        data["authority_manifest"],
        data["authority_manifest"],
        data["authorities"],
    )
    _install_discovery_witnesses(out, data["witnesses"])
    _install_feedback_findings(out, data["findings"])
    out["coverage_inventory"] = None
    out["ready_transition"] = None
    out["ci_candidate"] = None
    out["green_seal"] = None


def _h_map_impact(out: dict, data: dict, policies) -> None:
    rec = _bind_now(out, data["impact_map"])
    rec["impact_map_id"] = model.derived_id("impact-map", rec["snapshot_epoch"], model.impact_map_subject(rec))
    out["impact_maps"][rec["impact_map_id"]] = rec
    _install_attestations(out, [data["attestation"]])
    _install_findings(out, data["findings"])


def _h_plan_coverage(out: dict, data: dict, policies) -> None:
    for o in data["obligations"]:
        rec = _bind_now(out, o)
        rec["obligation_id"] = model.derived_id("obligation", rec["snapshot_epoch"], model.obligation_subject(rec))
        out["obligations"][rec["obligation_id"]] = rec


def _h_challenge_coverage(out: dict, data: dict, policies) -> None:
    _install_attestations(out, [data["attestation"]])
    _install_findings(out, data["findings"])
    revised = data["revised_obligations"]
    _install_obligations(
        out,
        revised,
        policies,
        replace_categories={o["category"] for o in revised},
    )
    rec = _bind_now(out, data["coverage_inventory"])
    rec["coverage_inventory_id"] = model.derived_id(
        "coverage-inventory", rec["snapshot_epoch"], model.coverage_inventory_subject(rec)
    )
    out["coverage_inventory"] = rec


def _h_checks(out: dict, data: dict, policies) -> None:
    _install_checks(out, data["checks"])


def _h_reviews(out: dict, data: dict, policies) -> None:
    _install_attestations(out, data["attestations"])
    _install_findings(out, data["findings"])


def _h_single_review(out: dict, data: dict, policies) -> None:
    _install_attestations(out, [data["attestation"]])
    _install_findings(out, data["findings"])


def _h_exemption_challenge(out: dict, data: dict, policies) -> None:
    # Each exemption-challenger payload carries an `exemption_outcome` derived
    # from its witnessed raw report; it is consumed here, never stored.
    outcomes = {}
    atts = []
    for a in data["attestations"]:
        a = dict(a)
        oc = a.pop("exemption_outcome", None)
        d = out["dispatches"].get(a.get("dispatch_id"))
        challenger = d is not None and _role_of_dispatch(out, d) == "exemption-challenger"
        if challenger and oc not in model.EXEMPTION_CHALLENGE_OUTCOMES:
            _fail(
                "missing-field",
                "attestations",
                "exemption-challenger attestation requires exemption_outcome",
            )
        if not challenger and oc is not None:
            _fail(
                "unknown-field",
                "attestations",
                "exemption_outcome is only valid on exemption-challenger attestations",
            )
        atts.append(a)
        if challenger:
            probe = _bind_now(out, a)
            probe["attestation_id"] = ""
            outcomes[
                model.derived_id(
                    "attestation",
                    probe["snapshot_epoch"],
                    model.review_wrapper_subject(probe),
                )
            ] = oc
    _install_attestations(out, atts)
    _install_findings(out, data["findings"])
    for aid, oc in outcomes.items():
        att = out["reviews"].get(aid)
        if att is None:
            continue
        d = out["dispatches"].get(att["dispatch_id"])
        for oid in d["assignment_ids"]:
            o = out["obligations"].get(oid)
            if o is None or o["status"] != "not-applicable":
                continue
            if oc == "not-applicable-confirmed":
                if aid not in o["not_applicable_attestation_ids"]:
                    o["not_applicable_attestation_ids"] = sorted(set(o["not_applicable_attestation_ids"]) | {aid})
            elif oc == "applicable":
                # The exemption is rejected: the obligation returns to the
                # earliest incomplete obligation predicate (coverage, then
                # its scheduled review tiers).
                o["status"] = "pending"
            elif oc == "incomplete":
                _open_blocker(
                    out,
                    blocker_id=f"exemption-incomplete:{aid}",
                    blocker_class="incomplete-review",
                    reason=f"exemption challenge for {oid} returned incomplete",
                    evidence_ids=[att["evidence_id"]],
                )


def _h_adjudicate(out: dict, data: dict, policies) -> None:
    _install_attestations(out, data["attestations"])
    for upd in data["findings"]:
        f = _finding_by_id(out, upd["finding_id"])
        if f["disposition"] not in ("open", "unassessed"):
            _fail(
                "unlawful-transition",
                "resolutions",
                f"finding {upd['finding_id']!r} is not awaiting adjudication",
            )
        outcome = upd["outcome"]
        if outcome == "confirmed" and upd.get("remediation_class") not in (
            "candidate-change",
            "review-process",
        ):
            _fail(
                "missing-field",
                "resolutions",
                "confirmed outcome requires exactly one remediation class",
            )
        f["resolution"] = {
            "outcome": outcome,
            "remediation_class": upd.get("remediation_class"),
            "adjudicator_attestation_id": upd.get("adjudicator_attestation_id"),
        }


def _h_close_false_positive(out: dict, data: dict, policies) -> None:
    snap = out["snapshot"]
    for res in data["resolutions"]:
        f = _finding_by_id(out, res["finding_id"])
        current = f.get("resolution") or {}
        if current.get("outcome") != "false-positive":
            _fail(
                "unlawful-transition",
                "resolutions",
                "close-false-positive requires an adjudicated false-positive outcome",
            )
        counter = res.get("counter_evidence_ids") or ()
        if not counter:
            _fail(
                "missing-field",
                "resolutions",
                "false-positive closure requires counter-evidence ids",
            )
        for eid in counter:
            ev = out["evidence"].get(eid)
            if ev is None or not _current(out, ev, eid):
                _fail("stale", "resolutions", f"counter-evidence {eid!r} is not current")
        current.update(
            {
                "kind": "false-positive",
                "counter_evidence_ids": sorted(counter),
                "review_id": res.get("review_id") or current.get("adjudicator_attestation_id"),
                "resolved_snapshot_epoch": snap["epoch"],
                "resolved_snapshot_fingerprint": snap["fingerprint"],
            }
        )
        f["resolution"] = current
        f["disposition"] = "false-positive"


def _h_enter_fixing(out: dict, data: dict, policies) -> None:
    old = out["snapshot"]
    new_snap = dict(data["replacement_snapshot"])
    if new_snap.get("epoch") != old["epoch"] + 1:
        _fail("bad-epoch", "replacement_snapshot", "enter-fixing advances exactly one epoch")
    for res in data["resolutions"]:
        f = _finding_by_id(out, res["finding_id"])
        current = f.get("resolution") or {}
        if current.get("outcome") != "confirmed" or current.get("remediation_class") != "candidate-change":
            _fail(
                "unlawful-transition",
                "resolutions",
                "enter-fixing requires a confirmed candidate-change adjudication",
            )
        pubs = res.get("publication_evidence_ids") or ()
        if not pubs:
            _fail(
                "missing-field",
                "resolutions",
                "enter-fixing requires publication evidence ids",
            )
    _install_snapshot(
        out,
        new_snap,
        data["replacement_authority_manifest"],
        data["replacement_authority_manifest"],
        data["replacement_authorities"],
    )
    out["coverage_inventory"] = None
    out["ready_transition"] = None
    out["ci_candidate"] = None
    out["green_seal"] = None
    for res in data["resolutions"]:
        f = _finding_by_id(out, res["finding_id"])
        for eid in res["publication_evidence_ids"]:
            ev = out["evidence"].get(eid)
            if ev is None or not _current(out, ev, eid):
                _fail("stale", "resolutions", f"publication evidence {eid!r} is not current")
        f["resolution"] = {
            **(f.get("resolution") or {}),
            "kind": "candidate-change",
            "fix_sha": new_snap["head_sha"],
            "publication_evidence_ids": sorted(res["publication_evidence_ids"]),
            "replacement_snapshot_epoch": new_snap["epoch"],
            "replacement_snapshot_fingerprint": new_snap.get("fingerprint") or out["snapshot"]["fingerprint"],
            "check_id": None,
            "review_id": None,
        }
        f["disposition"] = "fixing"


def _h_review_fix(out: dict, data: dict, policies) -> None:
    _install_attestations(out, data["attestations"])
    _install_findings(out, data["findings"])


def _h_close_fixed(out: dict, data: dict, policies) -> None:
    snap = out["snapshot"]
    for res in data["resolutions"]:
        f = _finding_by_id(out, res["finding_id"])
        if f["disposition"] != "fixing":
            _fail(
                "unlawful-transition",
                "resolutions",
                "close-fixed requires a fixing finding",
            )
        chk = out["checks"].get(res.get("check_id"))
        rev = out["reviews"].get(res.get("review_id"))
        if chk is None or not _current(out, chk, res.get("check_id")) or chk["conclusion"] != "success":
            _fail("stale", "resolutions", "fix verification check is missing or not green")
        if rev is None or not _current(out, rev, res.get("review_id")) or rev["verdict"] != "clean":
            _fail("stale", "resolutions", "fix review is missing or not clean")
        f["resolution"] = {
            **(f.get("resolution") or {}),
            "check_id": chk["check_id"],
            "review_id": rev["attestation_id"],
            "resolved_snapshot_epoch": snap["epoch"],
            "resolved_snapshot_fingerprint": snap["fingerprint"],
            "verified_obligation_ids": sorted(res.get("verified_obligation_ids") or ()),
        }
        f["disposition"] = "fixed"


def _h_enter_review_repair(out: dict, data: dict, policies) -> None:
    snap = out["snapshot"]
    for res in data["resolutions"]:
        f = _finding_by_id(out, res["finding_id"])
        current = f.get("resolution") or {}
        if current.get("outcome") != "confirmed" or current.get("remediation_class") != "review-process":
            _fail(
                "unlawful-transition",
                "resolutions",
                "enter-review-repair requires a confirmed review-process adjudication",
            )
        repair_id = res["repair_id"]
        if repair_id in out["review_repairs"]:
            _fail("conflict", "resolutions", f"repair {repair_id!r} already exists")
        cut = _repair_cut(out, res["target_kind"], res["target_ids"], res["finding_id"])
        if not cut:
            _fail("empty-cut", "resolutions", "repair produced an empty invalidation cut")
        repair = {
            "repair_id": repair_id,
            "finding_id": res["finding_id"],
            "target_kind": res["target_kind"],
            "target_ids": sorted(res["target_ids"]),
            "invalidated_record_ids": sorted(cut),
            "entry_adjudicator_attestation_id": current.get("adjudicator_attestation_id"),
            "status": "repairing",
            "verification_attestation_id": None,
            "snapshot_epoch": snap["epoch"],
            "snapshot_fingerprint": snap["fingerprint"],
        }
        out["review_repairs"][repair_id] = repair
        f["resolution"] = {
            **current,
            "kind": "review-process",
            "repair_id": repair_id,
            "target_kind": res["target_kind"],
            "target_ids": sorted(res["target_ids"]),
            "invalidated_record_ids": sorted(cut),
        }
        f["disposition"] = "review-repairing"
        # A repair invalidating final/closure/remote proof clears any sealed claim.
        if cut & _downstream_cut(out):
            out["green_seal"] = None
        # Findings whose own lifecycle proof the cut invalidates transition:
        # an invalidated adjudication reopens for re-adjudication; an
        # invalidated fix review returns the same-snapshot finding to fixing.
        targets = set(res["target_ids"])
        if res["target_kind"] == "finding-adjudication":
            for f2 in out["findings"].values():
                if f2["finding_id"] == res["finding_id"]:
                    continue
                res2 = f2.get("resolution") or {}
                if res2.get("adjudicator_attestation_id") in targets:
                    f2["disposition"] = "open"
                    f2["resolution"] = None
        elif res["target_kind"] == "fix-review":
            for f2 in out["findings"].values():
                if f2["finding_id"] == res["finding_id"]:
                    continue
                res2 = f2.get("resolution") or {}
                if f2["disposition"] == "fixed" and res2.get("review_id") in targets:
                    f2["disposition"] = "fixing"


def _h_verify_review_repair(out: dict, data: dict, policies) -> None:
    _install_attestations(out, data["attestations"])
    _install_findings(out, data["findings"])
    for a in out["reviews"].values():
        d = out["dispatches"].get(a["dispatch_id"])
        if d is None or _role_of_dispatch(out, d) != "review-repair-verifier":
            continue
        for aid in d["assignment_ids"]:
            rep = _repair_for_finding(out, aid)
            if rep is not None and rep["status"] == "repairing":
                rep["verification_attestation_id"] = a["attestation_id"]
                rep["status"] = "verified"


def _h_close_review_repaired(out: dict, data: dict, policies) -> None:
    for res in data["resolutions"]:
        f = _finding_by_id(out, res["finding_id"])
        rep = _repair_for_finding(out, res["finding_id"])
        if rep is None or rep["status"] != "verified":
            _fail(
                "unlawful-transition",
                "resolutions",
                "close-review-repaired requires an already-verified repair",
            )
        rep["status"] = "closed"
        f["resolution"] = {
            **(f.get("resolution") or {}),
            "replacement_record_ids": sorted(res.get("replacement_record_ids") or ()),
            "review_id": rep["verification_attestation_id"],
        }
        f["disposition"] = "review-repaired"


def _h_accept_risk(out: dict, data: dict, policies) -> None:
    snap = out["snapshot"]
    for res in data["resolutions"]:
        f = _finding_by_id(out, res["finding_id"])
        current = f.get("resolution") or {}
        if current.get("outcome") != "confirmed":
            _fail(
                "unlawful-transition",
                "resolutions",
                "accept-risk requires a confirmed adjudication",
            )
        wid = res.get("human_decision_witness_id")
        w = out["witness_records"].get(wid)
        if w is None or w["kind"] != "human-decision" or not _current(out, w, wid):
            _fail(
                "missing-witness",
                "resolutions",
                "accept-risk requires a current human-decision witness",
            )
        f["resolution"] = {
            **current,
            "kind": "accepted-risk",
            "human_decision_witness_id": wid,
            "resolved_snapshot_epoch": snap["epoch"],
            "resolved_snapshot_fingerprint": snap["fingerprint"],
        }
        f["disposition"] = "accepted-risk"
    if not [f for f in out["findings"].values() if f["disposition"] in _OPEN_DISPOSITIONS]:
        out["status"] = "reviewed-with-exceptions"


def _h_resume(out: dict, data: dict, policies) -> None:
    _apply_resume(out, data["blocker_id"], data["resolution_evidence_ids"])


def _h_mark_ready(out: dict, data: dict, policies) -> None:
    snap = out["snapshot"]
    ready = out["ready_transition"]
    if ready is not None and _current(out, ready, ready["ready_transition_id"]):
        ci = out["ci_candidate"]
        if ready["status"] == "completed" and (ci is None or not _current(out, ci, ci["ci_candidate_id"])):
            # Reconcile: the remote transition committed but the candidate
            # record was invalidated (e.g. a hosted-check repair cut it).
            # The candidate is derived data over the completed transition, so
            # re-materializing it is idempotent and needs no new witness.
            ci = {
                "ci_candidate_id": "",
                "repository_id": ready["repository_id"],
                "pr_number": ready["pr_number"],
                "head_sha": ready["head_sha"],
                "lifecycle_state": ready["expected_lifecycle_state"],
                "transition_witness_id": ready["transition_witness_id"],
                "snapshot_epoch": ready["snapshot_epoch"],
                "snapshot_fingerprint": ready["snapshot_fingerprint"],
            }
            ci["ci_candidate_id"] = model.derived_id(
                "ci-candidate", ready["snapshot_epoch"], model.ci_candidate_subject(ci)
            )
            out["ci_candidate"] = ci
            return
        _fail("conflict", "ready_transition", "a current ready transition already exists")
    idem = ready_idempotency_key(out)
    prior = data["prior_lifecycle_state"]
    out["ready_transition"] = {
        "ready_transition_id": f"ready:{snap['epoch']}:{idem}",
        "idempotency_key": idem,
        "repository_id": snap["repository_id"],
        "pr_number": snap["pr_number"],
        "head_sha": snap["head_sha"],
        "prior_lifecycle_state": prior,
        "expected_lifecycle_state": "ready",
        "transition_witness_id": None,
        "status": "pending",
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }


def _h_run_remote_ci(out: dict, data: dict, policies) -> None:
    snap = out["snapshot"]
    obs = data["remote_observation"]
    content_id = "sha256:" + model.sha256_hex(model.canonical_json(obs))
    ev_id = "evidence:" + model.sha256_json(
        {
            "content_id": content_id,
            "kind": "remote-observation",
            "snapshot_epoch": snap["epoch"],
            "snapshot_fingerprint": snap["fingerprint"],
        }
    )
    if out["evidence"].get(ev_id) is None:
        _fail(
            "missing-evidence",
            "remote_observation",
            "remote observation is not registered as remote-observation evidence",
        )
    _install_checks(out, data["checks"])


def _h_seal(out: dict, data: dict, policies) -> None:
    snap = out["snapshot"]
    witnesses = [w for wid, w in out["witness_records"].items() if _current(out, w, wid)]
    latest = max(
        witnesses,
        key=lambda w: max(w["record_positions"], default=0),
        default=None,
    )
    obs = _stored_remote_observation(out) or {}
    out["green_seal"] = {
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
        "coverage_sha256": model.sha256_json(out["coverage_inventory"]),
        "findings_sha256": model.sha256_json(out["findings"]),
        "repairs_sha256": model.sha256_json(out["review_repairs"]),
        "reviews_sha256": model.sha256_json(out["reviews"]),
        "checks_sha256": model.sha256_json(out["checks"]),
        "witness_chain_head_sha256": (latest["chain_head_at_record"] if latest else "0" * 64),
        "evidence_ids": sorted(out["evidence"].keys()),
        "created_at": obs.get("observed_at") or f"epoch:{snap['epoch']}",
    }


def _apply_resume(out: dict, blocker_id: str, resolution_evidence_ids) -> None:
    rec = out["blockers"].get(blocker_id)
    if rec is None:
        _fail("dangling-ref", "blocker_id", f"unknown blocker {blocker_id!r}")
    if not rec["active"]:
        _fail("not-active", "blocker_id", "blocker is not active")
    if not resolution_evidence_ids:
        _fail("missing-field", "resolution_evidence_ids", "resolution evidence required")
    for eid in resolution_evidence_ids:
        ev = out["evidence"].get(eid)
        if ev is None:
            _fail("dangling-ref", "resolution_evidence_ids", f"unknown evidence {eid!r}")
        if not _current(out, ev, eid):
            _fail("stale", "resolution_evidence_ids", "resolution evidence is not current")
    rec["active"] = False
    rec["resolution_evidence_ids"] = list(resolution_evidence_ids)
    rec["closed_sequence"] = len(out["history"]) + 1
    snap = out["snapshot"]
    rec["resolution_snapshot_epoch"] = snap["epoch"] if snap else None
    rec["resolution_snapshot_fingerprint"] = snap["fingerprint"] if snap else None
    out["status"] = "active"


_COMPLETE = {
    "freeze-review-input": _h_freeze,
    "refresh-review-input": _h_refresh,
    "map-impact-semantic": _h_map_impact,
    "map-impact-contract": _h_map_impact,
    "plan-coverage": _h_plan_coverage,
    "challenge-coverage": _h_challenge_coverage,
    "run-preflight": _h_checks,
    "run-fast-review": _h_reviews,
    "run-focused-review": _h_reviews,
    "run-strong-review": _h_reviews,
    "run-exemption-challenge": _h_exemption_challenge,
    "adjudicate-findings": _h_adjudicate,
    "close-false-positive": _h_close_false_positive,
    "enter-fixing": _h_enter_fixing,
    "run-fix-verification": _h_checks,
    "review-fix": _h_review_fix,
    "close-fixed": _h_close_fixed,
    "enter-review-repair": _h_enter_review_repair,
    "verify-review-repair": _h_verify_review_repair,
    "close-review-repaired": _h_close_review_repaired,
    "accept-risk": _h_accept_risk,
    "resume-review": _h_resume,
    "run-final-review": _h_single_review,
    "run-closure-audit": _h_single_review,
    "mark-ready-for-ci": _h_mark_ready,
    "run-remote-ci": _h_run_remote_ci,
    "seal-green": _h_seal,
}


def _open_blocker(out: dict, *, blocker_id: str, blocker_class: str, reason: str, evidence_ids) -> None:
    if any(b["active"] for b in out["blockers"].values()):
        _fail("multi-blocker", "blockers", "another blocker is already active")
    snap = out["snapshot"]
    if snap is None:
        _fail("missing-snapshot", "blockers", "blockers require an installed snapshot")
    out["blockers"][blocker_id] = {
        "blocker_id": blocker_id,
        "class": blocker_class,
        "reason": reason,
        "evidence_ids": list(evidence_ids),
        "active": True,
        "opened_sequence": len(out["history"]) + 1,
        "closed_sequence": None,
        "resolution_evidence_ids": [],
        "resolution_snapshot_epoch": None,
        "resolution_snapshot_fingerprint": None,
        "snapshot_epoch": snap["epoch"],
        "snapshot_fingerprint": snap["fingerprint"],
    }
    out["status"] = "blocked"
    out["stage"] = "blocked"


def block_review(state: dict, *, blocker_id: str, blocker_class: str, reason: str, evidence_ids) -> dict:
    out = copy.deepcopy(state)
    _open_blocker(
        out,
        blocker_id=blocker_id,
        blocker_class=blocker_class,
        reason=reason,
        evidence_ids=evidence_ids,
    )
    store.append_history(
        out,
        event="block-review",
        data_sha256=model.sha256_json(
            {
                "blocker_id": blocker_id,
                "class": blocker_class,
                "reason": reason,
                "evidence_ids": list(evidence_ids),
            }
        ),
    )
    model.validate_state(out, verify_content=False)
    return out


def resume_review(state: dict, *, blocker_id: str, resolution_evidence_ids, policies) -> dict:
    out = copy.deepcopy(state)
    _apply_resume(out, blocker_id, resolution_evidence_ids)
    _set_stage(out, policies)
    store.append_history(
        out,
        event="resume-review",
        data_sha256=model.sha256_json(
            {
                "blocker_id": blocker_id,
                "resolution_evidence_ids": list(resolution_evidence_ids),
            }
        ),
    )
    model.validate_state(out, verify_content=False)
    return out
