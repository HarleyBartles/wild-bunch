#!/usr/bin/env python3
"""Sealed review-assignment policy (Plan 3 Task 3).

``SealedReviewAssignmentPolicy`` implements ``policy.ReviewAssignmentPolicy``
from the digest-bound reference document
``references/review-assignment-policy.v1.json``. The document is the policy:
role floors, composition rules, and independence tuples all derive from its
canonical bytes, and acquisition binds its ``policy_id``/``version``/``sha256``
into every new snapshot. Loading fails closed on any malformed or incomplete
document; ``requirement`` composes floors upward only - a repository or
assignment may raise a floor, never lower it.
"""

from __future__ import annotations

from pathlib import Path

from . import model, policy

_POLICY_DOC = Path(__file__).resolve().parent.parent.parent / "references" / "review-assignment-policy.v1.json"

_POLICY_ID = "review-core-review-assignment"

# Independence-list tokens the module resolves dynamically rather than
# treating as literal role names.
_SELF = "self"
_SOURCE_ROLE = "source-role"
_DYNAMIC_TOKENS = frozenset({_SELF, _SOURCE_ROLE})
_SOURCE_MARKED_ROLES = frozenset({"finding-adjudicator", "review-repair-verifier"})

_TOP_KEYS = frozenset(
    {
        "schema_version",
        "policy_id",
        "version",
        "role_floors",
        "obligation_reviewer_composition",
        "independence",
        "finding_adjudicator_composition",
        "repair_verifier_composition",
        "deep_cut_target_kinds",
        "deep_cut_roles",
    }
)

_INDEPENDENCE_KEYS = frozenset({"context_mode", "distinct_execution_from", "distinct_role_contract_from"})


class AssignmentPolicyError(Exception):
    """Raised when the sealed document or a requirement query fails closed."""


def _fail(message: str) -> None:
    raise AssignmentPolicyError(message)


def _tier_idx(tier: str) -> int:
    return model.CAPABILITY_TIERS.index(tier)


def _reasoning_idx(floor: str) -> int:
    return model.REASONING_FLOORS.index(floor)


def _validate_document(doc: object, *, source: str) -> dict:
    if not isinstance(doc, dict):
        _fail(f"{source}: policy document is not an object")
    unknown = set(doc) - _TOP_KEYS
    if unknown:
        _fail(f"{source}: unknown keys {sorted(unknown)}")
    if doc.get("schema_version") != 1:
        _fail(f"{source}: unsupported schema_version")
    if doc.get("policy_id") != _POLICY_ID:
        _fail(f"{source}: wrong policy_id")
    if not isinstance(doc.get("version"), str) or not doc["version"]:
        _fail(f"{source}: version must be a non-empty string")

    floors = doc.get("role_floors")
    if not isinstance(floors, dict):
        _fail(f"{source}: role_floors must be an object")
    if set(floors) != set(model.REVIEW_ROLES):
        _fail(f"{source}: role_floors must cover exactly the known review roles")
    for role, entry in floors.items():
        if not isinstance(entry, dict) or set(entry) != {"tier", "reasoning"}:
            _fail(f"{source}: role_floors[{role!r}] must be {{tier, reasoning}}")
        if entry["tier"] not in model.CAPABILITY_TIERS:
            _fail(f"{source}: role_floors[{role!r}].tier unknown")
        if entry["reasoning"] not in model.REASONING_FLOORS:
            _fail(f"{source}: role_floors[{role!r}].reasoning unknown")

    if doc.get("obligation_reviewer_composition") != "max-over-assignments":
        _fail(f"{source}: unsupported obligation_reviewer_composition")
    if doc.get("finding_adjudicator_composition") != "max-source-and-linked":
        _fail(f"{source}: unsupported finding_adjudicator_composition")
    if doc.get("repair_verifier_composition") != "max-source-target-or-final-strong":
        _fail(f"{source}: unsupported repair_verifier_composition")

    for key, universe in (
        ("deep_cut_target_kinds", model.REVIEW_REPAIR_TARGET_KINDS),
        ("deep_cut_roles", model.REVIEW_ROLES),
    ):
        values = doc.get(key)
        if not isinstance(values, list) or not all(isinstance(v, str) for v in values):
            _fail(f"{source}: {key} must be a list of strings")
        unknown_values = set(values) - set(universe)
        if unknown_values:
            _fail(f"{source}: {key} has unknown entries {sorted(unknown_values)}")

    independence = doc.get("independence")
    if not isinstance(independence, dict):
        _fail(f"{source}: independence must be an object")
    if set(independence) != set(model.REVIEW_ROLES):
        _fail(f"{source}: independence must cover exactly the known review roles")
    for role, entry in independence.items():
        if not isinstance(entry, dict):
            _fail(f"{source}: independence[{role!r}] must be an object")
        extra = set(entry) - _INDEPENDENCE_KEYS
        if extra:
            _fail(f"{source}: independence[{role!r}] unknown keys {sorted(extra)}")
        if entry.get("context_mode") not in model.CONTEXT_MODES:
            _fail(f"{source}: independence[{role!r}].context_mode unknown")
        for key in ("distinct_execution_from", "distinct_role_contract_from"):
            values = entry.get(key)
            if not isinstance(values, list) or not all(isinstance(v, str) for v in values):
                _fail(f"{source}: independence[{role!r}].{key} must be a list of strings")
            tokens = set(values) - set(model.REVIEW_ROLES) - _DYNAMIC_TOKENS
            if tokens:
                _fail(f"{source}: independence[{role!r}].{key} unknown tokens {sorted(tokens)}")
            if _SOURCE_ROLE in values and role not in _SOURCE_MARKED_ROLES:
                _fail(f"{source}: independence[{role!r}].{key} source marker on a non-source role")
    return doc


def _obligation_for(state: dict, assignment_id: str) -> dict | None:
    o = state.get("obligations", {}).get(assignment_id)
    if o is not None:
        return o
    h = state.get("hypothesis_assignments", {}).get(assignment_id)
    if h is not None:
        return state.get("obligations", {}).get(h["obligation_id"])
    return None


def _role_of_dispatch(state: dict, dispatch: dict) -> str:
    rs = state.get("route_selections", {}).get(dispatch.get("route_selection_id"))
    return rs["required_role"] if rs else ""


def _dispatch_for(state: dict, record_id: str) -> dict | None:
    """Resolve a record id to its dispatch: a dispatch id directly, or a
    review attestation id via its ``dispatch_id``."""
    d = state.get("dispatches", {}).get(record_id)
    if d is not None:
        return d
    r = state.get("reviews", {}).get(record_id)
    if r is not None:
        return state.get("dispatches", {}).get(r["dispatch_id"])
    return None


class SealedReviewAssignmentPolicy:
    """``policy.ReviewAssignmentPolicy`` backed by the sealed reference document."""

    def __init__(self, *, document_bytes: bytes | None = None) -> None:
        raw = document_bytes if document_bytes is not None else _POLICY_DOC.read_bytes()
        try:
            doc = model.strict_json_loads(raw, source=_POLICY_DOC.name)
        except ValueError as exc:
            raise AssignmentPolicyError(f"{_POLICY_DOC.name}: malformed JSON: {exc}") from exc
        self._doc = _validate_document(doc, source=_POLICY_DOC.name)
        self._sha256 = model.sha256_hex(model.canonical_json(self._doc))

    @property
    def source_id(self) -> str:
        return self._doc["policy_id"]

    @property
    def source_version(self) -> str:
        return self._doc["version"]

    @property
    def sha256(self) -> str:
        return self._sha256

    def _floor(self, role: str) -> tuple[str, str]:
        entry = self._doc["role_floors"][role]
        return entry["tier"], entry["reasoning"]

    def _raise(self, floor: list, tier: str, reasoning: str) -> None:
        if _tier_idx(tier) > _tier_idx(floor[0]):
            floor[0] = tier
        if _reasoning_idx(reasoning) > _reasoning_idx(floor[1]):
            floor[1] = reasoning

    def _raise_to_role(self, state: dict, floor: list, record_id: str) -> str:
        """Raise ``floor`` to the doc floor of the role owning ``record_id``
        and return that role (``""`` when unresolvable)."""
        d = _dispatch_for(state, record_id)
        if d is None:
            return ""
        role = _role_of_dispatch(state, d)
        if role in self._doc["role_floors"]:
            t, r = self._floor(role)
            self._raise(floor, t, r)
        return role

    def _raise_for_record(self, state: dict, floor: list, record_id: str) -> None:
        """Raise ``floor`` to whatever floor ``record_id`` demands: obligation
        floors for obligations/hypotheses, role floors for reviews/dispatches."""
        o = _obligation_for(state, record_id)
        if o is not None:
            t, r = policy.obligation_floor(o["scope_level"], o["risk"], o["consequences"])
            self._raise(floor, t, r)
            return
        h = state.get("hypothesis_assignments", {}).get(record_id)
        if h is not None:
            self._raise(floor, h["minimum_capability_tier"], h["minimum_reasoning_floor"])
            return
        f = state.get("findings", {}).get(record_id)
        if f is not None:
            if f.get("source_kind") == "review":
                self._raise_to_role(state, floor, f["source_id"])
            for linked in (f.get("obligation_id"), f.get("source_assignment_id")):
                if linked:
                    self._raise_for_record(state, floor, linked)
            return
        self._raise_to_role(state, floor, record_id)

    def _source_roles(self, state: dict, role: str, assignment_ids: tuple[str, ...]) -> list[str]:
        """Roles this dispatch must stay independent of, resolved through
        ``state``: for adjudicators, the roles of the dispatches whose reviews
        produced the assigned findings; for repair verifiers, the entry
        adjudicator's role."""
        roles: list[str] = []
        if role == "finding-adjudicator":
            for fid in assignment_ids:
                f = state.get("findings", {}).get(fid)
                if f is None or f.get("source_kind") != "review":
                    continue
                source_role = self._source_role_of(state, f["source_id"])
                if source_role:
                    roles.append(source_role)
        elif role == "review-repair-verifier":
            for rid in assignment_ids:
                r = state.get("review_repairs", {}).get(rid)
                if r is None:
                    continue
                source_role = self._source_role_of(state, r["entry_adjudicator_attestation_id"])
                if source_role:
                    roles.append(source_role)
        return roles

    def _source_role_of(self, state: dict, record_id: str) -> str:
        d = _dispatch_for(state, record_id)
        return _role_of_dispatch(state, d) if d is not None else ""

    def requirement(self, *, state: dict, role: str, assignment_ids: tuple[str, ...]) -> policy.RoleRequirement:
        if role not in self._doc["role_floors"]:
            raise AssignmentPolicyError(f"unknown role {role!r}")
        floor = list(self._floor(role))

        if role == "obligation-reviewer":
            for aid in assignment_ids:
                o = _obligation_for(state, aid)
                if o is None:
                    continue
                t, r = policy.obligation_floor(o["scope_level"], o["risk"], o["consequences"])
                self._raise(floor, t, r)
        elif role == "finding-adjudicator":
            for fid in assignment_ids:
                f = state.get("findings", {}).get(fid)
                if f is None:
                    continue
                if f.get("source_kind") == "review":
                    self._raise_to_role(state, floor, f["source_id"])
                for linked in (f.get("obligation_id"), f.get("source_assignment_id")):
                    if linked:
                        self._raise_for_record(state, floor, linked)
        elif role == "review-repair-verifier":
            for rid in assignment_ids:
                r = state.get("review_repairs", {}).get(rid)
                if r is None:
                    continue
                self._raise_to_role(state, floor, r["entry_adjudicator_attestation_id"])
                deep = r["target_kind"] in self._doc["deep_cut_target_kinds"]
                for tid in tuple(r["target_ids"]) + tuple(r["invalidated_record_ids"]):
                    self._raise_for_record(state, floor, tid)
                    deep = deep or self._is_deep_record(state, tid)
                if deep:
                    floor[0], floor[1] = "final-strong", "final-strong"

        entry = self._doc["independence"][role]
        source_roles = self._source_roles(state, role, assignment_ids)
        execution = []
        for token in entry["distinct_execution_from"]:
            if token == _SELF:
                execution.append(role)
            elif token == _SOURCE_ROLE:
                execution.extend(source_roles)
            else:
                execution.append(token)
        contracts = []
        for token in entry["distinct_role_contract_from"]:
            contracts.extend(source_roles if token == _SOURCE_ROLE else [token])
        return policy.RoleRequirement(
            capability_tier=floor[0],
            reasoning_floor=floor[1],
            context_mode=entry["context_mode"],
            distinct_execution_from=tuple(dict.fromkeys(execution)),
            distinct_role_contract_from=tuple(dict.fromkeys(contracts)),
        )

    def _is_deep_record(self, state: dict, record_id: str) -> bool:
        """True when an invalidated/target record is exemption, blind-final,
        closure, hosted-CI, or whole-PR proof (spec: deep cut => final-strong
        verifier)."""
        d = _dispatch_for(state, record_id)
        if d is not None and _role_of_dispatch(state, d) in self._doc["deep_cut_roles"]:
            return True
        c = state.get("checks", {}).get(record_id)
        if c is not None and c.get("kind") == "remote-ci":
            return True
        o = _obligation_for(state, record_id)
        return o is not None and o.get("scope_level") == "whole-pr"
