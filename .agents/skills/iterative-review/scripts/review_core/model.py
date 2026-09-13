#!/usr/bin/env python3
"""Version-2 strict state model for the iterative-review evidence kernel.

Canonical JSON, strict decoding, closed enums, constructor-owned subject
projections, derived IDs, and recursive fail-closed validation. Every rule in
this module is structural: it proves shape and internal cross-reference
consistency and never labels a witness record authentic.
"""

from __future__ import annotations

import hashlib
import json
import re
from pathlib import Path

SCHEMA_VERSION = 2

STATUSES = ("active", "blocked", "reviewed-with-exceptions")
STAGES = (
    "intake",
    "authority",
    "impact-mapping",
    "coverage",
    "coverage-challenge",
    "preflight",
    "fast-review",
    "focused-review",
    "strong-review",
    "resolution",
    "review-repair",
    "final-review",
    "closure-audit",
    "remote-ci-candidate",
    "remote-ci",
    "green-candidate",
    "reviewed-with-exceptions",
    "blocked",
)
SEVERITIES = ("blocking", "important", "minor")
DISPOSITIONS = (
    "open",
    "fixing",
    "fixed",
    "review-repairing",
    "review-repaired",
    "false-positive",
    "accepted-risk",
    "deferred",
    "contested",
    "unassessed",
)
OBLIGATION_STATUSES = ("pending", "covered", "not-applicable", "invalidated", "unassessed")
REVIEW_VERDICTS = ("clean", "findings", "incomplete", "blocked")
OBLIGATION_OUTCOMES = ("covered", "not-applicable", "findings", "incomplete")
BLIND_FINAL_OUTCOMES = ("covered", "findings", "incomplete")
CLOSURE_AUDIT_OUTCOMES = ("covered", "findings", "incomplete")
FINDING_ADJUDICATION_OUTCOMES = ("confirmed", "false-positive", "contested")
ADJUDICATION_REMEDIATION_CLASSES = ("candidate-change", "review-process")
FIX_REVIEW_OUTCOMES = ("verified", "findings", "incomplete")
REVIEW_REPAIR_OUTCOMES = ("verified", "findings", "incomplete")
EXEMPTION_CHALLENGE_OUTCOMES = ("not-applicable-confirmed", "applicable", "incomplete")
AUDIT_RESULTS = ("clean", "contaminated", "incomplete")
DISPATCH_STATUSES = ("pending", "reported", "incomplete", "invalidated")
READY_TRANSITION_STATUSES = ("pending", "authorized", "completed")
ROUTE_SELECTION_MODES = ("profile",)
ROUTE_QUALIFICATION_SOURCES = ("profile-file",)
REQUIRED_TOOL_CLASSES = (
    "repo-read",
    "git-read",
    "github-read",
    "issue-read",
    "document-read",
    "command-exec",
    "browser-read",
    "domain-read",
)
CHECK_CONCLUSIONS = ("success", "failure", "cancelled", "skipped")
GIT_OBJECT_FORMATS = ("sha1", "sha256")
EVIDENCE_KINDS = (
    "snapshot",
    "authority",
    "authority-manifest-payload",
    "impact-map",
    "scope-challenge",
    "route-selection",
    "witness-record",
    "check-output",
    "review-attestation",
    "tool-transcript",
    "finding-proof",
    "fix-proof",
    "review-repair-proof",
    "human-decision",
    "remote-observation",
)
AUTHORITY_KINDS = (
    "repo-law",
    "pr-description",
    "issue",
    "document",
    "plan",
    "spec",
    "non-goal",
    "review-feedback",
)
AUTHORITY_AVAILABILITY = ("loaded", "unavailable")
OBLIGATION_CATEGORIES = (
    "authority-scope",
    "behavioral-correctness",
    "test-adequacy",
    "security-privacy",
    "reliability-concurrency",
    "compatibility-migration",
    "performance-resources",
    "operability-configuration",
    "documentation-contract",
    "source-custody",
)
OBLIGATION_RISKS = ("high", "medium", "low")
CONSEQUENCES = (
    "none",
    "security",
    "authorization",
    "privacy",
    "secrets",
    "irreversible-data-loss",
    "concurrency-recovery",
    "migration-rollback",
    "public-compatibility",
    "source-custody",
)
SUBSTANTIVE_CONSEQUENCES = tuple(c for c in CONSEQUENCES if c != "none")
HYPOTHESIS_POLARITIES = ("claim", "counterexample")
SCOPE_LEVELS = ("hunk", "file", "surface", "cross-surface", "whole-pr")
CAPABILITY_TIERS = ("fast", "focused", "strong", "final-strong")
REASONING_FLOORS = ("low", "standard", "high", "final-strong")
CONTEXT_MODES = ("fresh", "forked")
REVIEW_ROLES = (
    "impact-mapper-semantic",
    "impact-mapper-contract",
    "scope-challenger",
    "obligation-reviewer",
    "exemption-challenger",
    "finding-adjudicator",
    "fix-reviewer",
    "review-repair-verifier",
    "blind-final",
    "closure-auditor",
)
MAPPER_ROLES = ("impact-mapper-semantic", "impact-mapper-contract")
REVIEW_REPAIR_TARGET_KINDS = (
    "semantic-impact",
    "contract-impact",
    "coverage-plan",
    "coverage-challenge",
    "obligation-review",
    "exemption-review",
    "finding-adjudication",
    "fix-review",
    "blind-final",
    "closure-audit",
    "local-check",
    "hosted-check",
)
REVIEW_REPAIR_STATUSES = ("repairing", "verified", "closed")
CHECK_KINDS = ("preflight", "targeted", "remote-ci")
CHECK_LOCI = ("local", "hosted")
FINDING_SOURCE_KINDS = ("review", "check", "feedback")
WITNESS_KINDS = (
    "authority-discovery",
    "profile-resolution",
    "review-launch",
    "review-completion",
    "command-execution",
    "remote-transition",
    "remote-observation",
    "human-decision",
)
LOCAL_WITNESS_KINDS = (
    "authority-discovery",
    "profile-resolution",
    "review-launch",
    "review-completion",
    "command-execution",
    "human-decision",
)
REMOTE_WITNESS_KINDS = ("remote-transition", "remote-observation")
BLOCKER_CLASSES = (
    "authority-missing",
    "feedback-unresolved",
    "intake-incomplete",
    "coverage-gap",
    "incomplete-review",
    "malformed-evidence",
    "snapshot-drift",
    "tool-blocked",
    "contested",
    "state-invalid",
)
CALIBRATION_MISS_CLASSES = (
    "uncovered-surface",
    "weak-lens",
    "premature-closure",
    "stale-epoch",
    "frontier-false-positive",
    "new-information",
)

STATE_KEYS = frozenset(
    {
        "schema_version",
        "review_id",
        "generation",
        "status",
        "stage",
        "scratch_dir",
        "snapshot",
        "content_objects",
        "evidence",
        "authorities",
        "authority_manifest",
        "impact_maps",
        "coverage_inventory",
        "obligations",
        "hypothesis_assignments",
        "dispatches",
        "reviews",
        "route_selections",
        "witness_records",
        "findings",
        "review_repairs",
        "checks",
        "ready_transition",
        "ci_candidate",
        "calibration",
        "blockers",
        "green_seal",
        "history",
    }
)

_SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
_GITSHA_RE = {"sha1": re.compile(r"^[0-9a-f]{40}$"), "sha256": _SHA256_RE}
_ID_RE = re.compile(r"^[A-Za-z0-9_.:-]+$")


class StateValidationError(ValueError):
    """Stable, path-prefixed structural validation failure."""

    def __init__(self, code: str, path: str, message: str):
        super().__init__(f"{path}: {message}")
        self.code = code
        self.path = path


def _fail(code: str, path: str, message: str) -> None:
    raise StateValidationError(code, path, message)


# ---------------------------------------------------------------------------
# Canonical JSON and strict decoding


def _reject_constant(value: str) -> None:
    raise ValueError(f"non-finite JSON number {value!r}")


def _no_dupes(pairs: list) -> dict:
    out: dict = {}
    for key, value in pairs:
        if key in out:
            raise ValueError(f"duplicate object key {key!r}")
        out[key] = value
    return out


def strict_json_loads(raw: bytes, *, source: str = "input") -> object:
    """The only JSON decoder used by kernel ingress boundaries.

    Rejects BOMs, non-UTF-8, non-finite numbers, and duplicate object keys.
    """
    if not isinstance(raw, (bytes, bytearray)):
        _fail("not-bytes", source, "strict JSON input must be bytes")
    data = bytes(raw)
    if data.startswith(b"\xef\xbb\xbf"):
        _fail("bom", source, "UTF-8 BOM is not allowed")
    try:
        text = data.decode("utf-8")
    except UnicodeDecodeError as exc:
        _fail("not-utf8", source, f"invalid UTF-8: {exc}")
    try:
        return json.loads(
            text,
            object_pairs_hook=_no_dupes,
            parse_constant=_reject_constant,
        )
    except ValueError as exc:
        _fail("invalid-json", source, str(exc))


def canonical_json(value: object) -> bytes:
    return json.dumps(
        value,
        sort_keys=True,
        separators=(",", ":"),
        ensure_ascii=False,
        allow_nan=False,
    ).encode("utf-8", errors="surrogateescape")


canonical_bytes = canonical_json


def sha256_json(value: object) -> str:
    return hashlib.sha256(canonical_json(value)).hexdigest()


def sha256_hex(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


# ---------------------------------------------------------------------------
# Field-spec validation machinery


def _is_sha256(value: object) -> bool:
    return isinstance(value, str) and bool(_SHA256_RE.match(value))


def _is_id(value: object) -> bool:
    return isinstance(value, str) and bool(_ID_RE.match(value))


def _check_field(spec: object, value: object, path: str) -> None:
    if isinstance(spec, tuple):
        tag = spec[0]
        if tag == "enum":
            if value not in spec[1]:
                _fail("bad-enum", path, f"unknown value {value!r}")
            return
        if tag == "enum?":
            if value is None:
                return
            if value not in spec[1]:
                _fail("bad-enum", path, f"unknown value {value!r}")
            return
        if tag == "list":
            if not isinstance(value, list):
                _fail("bad-type", path, "expected list")
            for i, item in enumerate(value):
                _check_field(spec[1], item, f"{path}[{i}]")
            return
        if tag == "set":
            if not isinstance(value, list):
                _fail("bad-type", path, "expected list")
            for i, item in enumerate(value):
                _check_field(spec[1], item, f"{path}[{i}]")
            if len(set(map(json.dumps, value))) != len(value):
                _fail("duplicate", path, "set field contains duplicates")
            if value != sorted(value, key=lambda v: canonical_json(v)):
                _fail("not-canonical", path, "set field is not canonically sorted")
            return
        if tag == "dict":
            if not isinstance(value, dict):
                _fail("bad-type", path, "expected object")
            for k, v in value.items():
                _check_field(spec[1], k, f"{path}.<key>")
                _check_field(spec[2], v, f"{path}.{k}")
            return
        _fail("internal", path, f"unknown spec tag {tag!r}")
    if spec == "str":
        if not isinstance(value, str):
            _fail("bad-type", path, "expected string")
    elif spec == "str?":
        if value is not None and not isinstance(value, str):
            _fail("bad-type", path, "expected nullable string")
    elif spec == "nonempty-str":
        if not isinstance(value, str) or not value.strip():
            _fail("bad-type", path, "expected non-empty string")
    elif spec == "int":
        if not isinstance(value, int) or isinstance(value, bool):
            _fail("bad-type", path, "expected integer")
    elif spec == "int?":
        if value is not None and (not isinstance(value, int) or isinstance(value, bool)):
            _fail("bad-type", path, "expected nullable integer")
    elif spec == "bool":
        if not isinstance(value, bool):
            _fail("bad-type", path, "expected boolean")
    elif spec == "bool?":
        if value is not None and not isinstance(value, bool):
            _fail("bad-type", path, "expected nullable boolean")
    elif spec == "sha256":
        if not _is_sha256(value):
            _fail("bad-sha256", path, "expected 64 lowercase hex sha256")
    elif spec == "content-id":
        if not (isinstance(value, str) and _CONTENT_ID_RE.match(value)):
            _fail("bad-content-id", path, "expected 'sha256:' + 64 lowercase hex")
    elif spec == "sha256?":
        if value is not None and not _is_sha256(value):
            _fail("bad-sha256", path, "expected nullable 64 hex sha256")
    elif spec == "id":
        if not _is_id(value):
            _fail("bad-id", path, "expected identifier")
    elif spec == "epoch":
        if not isinstance(value, int) or isinstance(value, bool) or value < 1:
            _fail("bad-epoch", path, "epoch must be a positive integer")
    elif spec == "epoch?":
        if value is not None and (not isinstance(value, int) or isinstance(value, bool) or value < 1):
            _fail("bad-epoch", path, "epoch must be a positive integer or null")
    elif spec == "abs-path":
        if not isinstance(value, str) or not Path(value).is_absolute():
            _fail("relative-path", path, "expected absolute path")
    elif spec == "obj":
        if not isinstance(value, dict):
            _fail("bad-type", path, "expected object")
    elif spec == "obj?":
        if value is not None and not isinstance(value, dict):
            _fail("bad-type", path, "expected nullable object")
    elif spec == "any":
        pass
    else:
        _fail("internal", path, f"unknown field spec {spec!r}")


def _check_fields(record: object, spec: dict, path: str) -> None:
    if not isinstance(record, dict):
        _fail("bad-type", path, "expected object")
    required = set(spec)
    present = set(record)
    missing = required - present
    extra = present - required
    if missing:
        _fail("missing-field", path, f"missing field(s) {sorted(missing)}")
    if extra:
        _fail("unknown-field", path, f"unknown field(s) {sorted(extra)}")
    for name, fspec in spec.items():
        _check_field(fspec, record[name], f"{path}.{name}")


# ---------------------------------------------------------------------------
# Record field contracts


SNAPSHOT_FIELDS = {
    "epoch": "epoch",
    "repository_id": "nonempty-str",
    "pr_number": "int",
    "pr_url": "nonempty-str",
    "git_object_format": ("enum", GIT_OBJECT_FORMATS),
    "base_sha": "nonempty-str",
    "head_sha": "nonempty-str",
    "tree_sha": "nonempty-str",
    "diff_sha256": "sha256",
    "pr_metadata_sha256": "sha256",
    "authority_manifest_sha256": "sha256",
    "authority_discovery_policy_id": "nonempty-str",
    "authority_discovery_policy_version": "nonempty-str",
    "authority_discovery_policy_sha256": "sha256",
    "witness_policy_sha256": "sha256",
    "feedback_history_policy_id": "nonempty-str",
    "feedback_history_policy_version": "nonempty-str",
    "feedback_history_policy_sha256": "sha256",
    "feedback_history_sha256": "sha256",
    "local_check_policy_id": "nonempty-str",
    "local_check_policy_version": "nonempty-str",
    "local_check_policy_sha256": "sha256",
    "required_check_policy_sha256": "sha256",
    "review_assignment_policy_id": "nonempty-str",
    "review_assignment_policy_version": "nonempty-str",
    "review_assignment_policy_sha256": "sha256",
    "command_execution_policy_id": "nonempty-str",
    "command_execution_policy_version": "nonempty-str",
    "command_execution_policy_sha256": "sha256",
    "evidence_ingestion_policy_id": "nonempty-str",
    "evidence_ingestion_policy_version": "nonempty-str",
    "evidence_ingestion_policy_sha256": "sha256",
    "hypothesis_derivation_policy_id": "nonempty-str",
    "hypothesis_derivation_policy_version": "nonempty-str",
    "hypothesis_derivation_policy_sha256": "sha256",
    "unresolved_feedback_sha256": "sha256",
    "fingerprint": "sha256",
}

MANIFEST_PAYLOAD_FIELDS = {
    "repository_id": "nonempty-str",
    "pr_number": "int",
    "pr_url": "nonempty-str",
    "authority_discovery_policy_id": "nonempty-str",
    "authority_discovery_policy_version": "nonempty-str",
    "authority_discovery_policy_sha256": "sha256",
    "authorities": ("list", "obj"),
    "feedback_history_policy_id": "nonempty-str",
    "feedback_history_policy_version": "nonempty-str",
    "feedback_history_policy_sha256": "sha256",
    "feedback_history_sha256": "sha256",
    "local_check_policy_id": "nonempty-str",
    "local_check_policy_version": "nonempty-str",
    "local_check_policy_sha256": "sha256",
    "required_check_policy_sha256": "sha256",
    "review_assignment_policy_id": "nonempty-str",
    "review_assignment_policy_version": "nonempty-str",
    "review_assignment_policy_sha256": "sha256",
    "command_execution_policy_id": "nonempty-str",
    "command_execution_policy_version": "nonempty-str",
    "command_execution_policy_sha256": "sha256",
    "evidence_ingestion_policy_id": "nonempty-str",
    "evidence_ingestion_policy_version": "nonempty-str",
    "evidence_ingestion_policy_sha256": "sha256",
    "hypothesis_derivation_policy_id": "nonempty-str",
    "hypothesis_derivation_policy_version": "nonempty-str",
    "hypothesis_derivation_policy_sha256": "sha256",
    "unresolved_feedback_sha256": "sha256",
}

MANIFEST_AUTHORITY_ENTRY_FIELDS = {
    "authority_id": "id",
    "kind": ("enum", AUTHORITY_KINDS),
    "locator": "nonempty-str",
    "availability": ("enum", AUTHORITY_AVAILABILITY),
    "sha256": "sha256?",
    "failure_class": "str?",
    "failure_sha256": "sha256?",
}

AUTHORITY_MANIFEST_FIELDS = {
    "authority_manifest_id": "sha256",
    "payload_evidence_id": "id",
    "discovery_witness_id": "id",
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

CONTENT_OBJECT_FIELDS = {
    "content_id": "content-id",
    "path": "abs-path",
    "sha256": "sha256",
    "bytes": "int",
}

EVIDENCE_FIELDS = {
    "evidence_id": "id",
    "content_id": "content-id",
    "kind": ("enum", EVIDENCE_KINDS),
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

LOADED_AUTHORITY_FIELDS = {
    "authority_id": "id",
    "kind": ("enum", AUTHORITY_KINDS),
    "locator": "nonempty-str",
    "availability": "str",
    "sha256": "sha256",
    "evidence_id": "id",
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

UNAVAILABLE_AUTHORITY_FIELDS = {
    "authority_id": "id",
    "kind": ("enum", AUTHORITY_KINDS),
    "locator": "nonempty-str",
    "availability": "str",
    "failure_class": "nonempty-str",
    "failure_sha256": "sha256",
    "failure_evidence_id": "id",
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

IMPACT_ENTRY_FIELDS = {
    "surface": "nonempty-str",
    "category": ("enum", OBLIGATION_CATEGORIES),
    "hazards": ("set", "nonempty-str"),
    "consequences": ("set", ("enum", CONSEQUENCES)),
}

IMPACT_MAP_FIELDS = {
    "impact_map_id": "id",
    "role": ("enum", MAPPER_ROLES),
    "entries": ("list", "obj"),
    "evidence_id": "id",
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

COVERAGE_ENTRY_FIELDS = {
    "surface": "nonempty-str",
    "categories": ("set", ("enum", OBLIGATION_CATEGORIES)),
    "hazards": ("set", "nonempty-str"),
    "consequences": ("set", ("enum", CONSEQUENCES)),
    "obligation_ids": ("set", "id"),
}

COVERAGE_INVENTORY_FIELDS = {
    "coverage_inventory_id": "id",
    "semantic_impact_map_id": "id",
    "contract_impact_map_id": "id",
    "challenger_attestation_id": "id",
    "entries": ("list", "obj"),
    "evidence_id": "id",
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

OBLIGATION_FIELDS = {
    "obligation_id": "id",
    "category": ("enum", OBLIGATION_CATEGORIES),
    "surfaces": ("set", "nonempty-str"),
    "risk": ("enum", OBLIGATION_RISKS),
    "consequences": ("set", ("enum", CONSEQUENCES)),
    "scope_level": ("enum", SCOPE_LEVELS),
    "minimum_capability_tier": ("enum", CAPABILITY_TIERS),
    "minimum_reasoning_floor": ("enum", REASONING_FLOORS),
    "assignees": ("set", "id"),
    "status": ("enum", OBLIGATION_STATUSES),
    "evidence_ids": ("list", "id"),
    "not_applicable_attestation_ids": ("list", "id"),
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

HYPOTHESIS_ASSIGNMENT_FIELDS = {
    "hypothesis_assignment_id": "id",
    "obligation_id": "id",
    "family": "nonempty-str",
    "polarity": ("enum", HYPOTHESIS_POLARITIES),
    "statement": "nonempty-str",
    "derivation_policy_sha256": "sha256",
    "minimum_capability_tier": ("enum", CAPABILITY_TIERS),
    "minimum_reasoning_floor": ("enum", REASONING_FLOORS),
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

ROUTE_SELECTION_FIELDS = {
    "route_selection_id": "id",
    "observed_at": "nonempty-str",
    "inventory_evidence_sha256": "sha256",
    "budget_contract_sha256": "sha256",
    "profile_authority_sha256": "sha256",
    "resolved_route_token_sha256": "sha256",
    "required_capability_tier": ("enum", CAPABILITY_TIERS),
    "required_role": ("enum", REVIEW_ROLES),
    "qualified_roles": ("set", ("enum", REVIEW_ROLES)),
    "profile": "nonempty-str",
    "profile_sha256": "sha256",
    "selection_mode": ("enum", ROUTE_SELECTION_MODES),
    "selected_model": "nonempty-str",
    "selected_reasoning": ("enum", REASONING_FLOORS),
    "selected_context_mode": ("enum", CONTEXT_MODES),
    "parent_model": "str?",
    "parent_reasoning": ("enum?", REASONING_FLOORS),
    "qualification_source": ("enum", ROUTE_QUALIFICATION_SOURCES),
    "rationale": "str",
    "evidence_id": "id",
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

DISPATCH_FIELDS = {
    "dispatch_id": "id",
    "route_selection_id": "id",
    "profile_resolution_witness_id": "id",
    "launch_witness_id": "str?",
    "completion_witness_id": "str?",
    "agent_id": "str?",
    "tool_use_id": "str?",
    "transcript_sha256": "sha256?",
    "assignment_ids": ("set", "id"),
    "context_evidence_ids": ("set", "id"),
    "instruction_manifest_sha256": "sha256",
    "data_manifest_sha256": "sha256",
    "tool_confinement_policy_sha256": "sha256",
    "context_package_sha256": "sha256",
    "hazard_framing_sha256": "sha256?",
    "required_tool_classes": ("set", ("enum", REQUIRED_TOOL_CLASSES)),
    "status": ("enum", DISPATCH_STATUSES),
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

REVIEW_FIELDS = {
    "attestation_id": "id",
    "dispatch_id": "id",
    "assignment_ids": ("set", "id"),
    "verdict": ("enum", REVIEW_VERDICTS),
    "finding_ids": ("set", "id"),
    "uncertainties": ("list", "str"),
    "tool_transcript_sha256": "sha256",
    "evidence_id": "id",
    "completion_witness_id": "id",
    "audit_result": ("enum", AUDIT_RESULTS),
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

WITNESS_RECORD_FIELDS = {
    "witness_id": "id",
    "kind": ("enum", WITNESS_KINDS),
    "subject_sha256": "sha256",
    "tool_use_id": "str?",
    "agent_id": "str?",
    "transcript_range": "obj?",
    "record_positions": ("list", "int"),
    "chain_head_at_record": "sha256",
    "source_locator": "nonempty-str",
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

FINDING_FIELDS = {
    "finding_id": "id",
    "source_kind": ("enum", FINDING_SOURCE_KINDS),
    "source_id": "id",
    "source_assignment_id": "id",
    "obligation_id": "str?",
    "severity": ("enum", SEVERITIES),
    "title": "nonempty-str",
    "description": "str",
    "locations": ("set", "nonempty-str"),
    "evidence_ids": ("list", "id"),
    "regression_of": "str?",
    "disposition": ("enum", DISPOSITIONS),
    "resolution": "obj?",
    "discovered_snapshot_epoch": "epoch",
    "discovered_snapshot_fingerprint": "sha256",
}

REVIEW_REPAIR_FIELDS = {
    "repair_id": "id",
    "finding_id": "id",
    "target_kind": ("enum", REVIEW_REPAIR_TARGET_KINDS),
    "target_ids": ("set", "id"),
    "invalidated_record_ids": ("set", "id"),
    "entry_adjudicator_attestation_id": "id",
    "status": ("enum", REVIEW_REPAIR_STATUSES),
    "verification_attestation_id": "str?",
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

LOCAL_CHECK_FIELDS = {
    "check_id": "id",
    "kind": ("enum", CHECK_KINDS),
    "locus": "str",
    "policy_item_id": "nonempty-str",
    "name": "nonempty-str",
    "command": ("list", "nonempty-str"),
    "working_directory": "nonempty-str",
    "local_check_policy_sha256": "sha256",
    "command_execution_policy_sha256": "sha256",
    "required": "bool",
    "conclusion": ("enum", CHECK_CONCLUSIONS),
    "head_sha": "nonempty-str",
    "source_materialization_sha256": "sha256",
    "toolchain_sha256": "sha256",
    "environment_sha256": "sha256",
    "sandbox_id": "nonempty-str",
    "pre_source_sha256": "sha256",
    "post_source_sha256": "sha256",
    "process_tree_terminated": "bool",
    "evidence_id": "id",
    "execution_witness_id": "id",
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

HOSTED_CHECK_FIELDS = {
    "check_id": "id",
    "kind": ("enum", CHECK_KINDS),
    "locus": "str",
    "policy_item_id": "nonempty-str",
    "name": "nonempty-str",
    "required": "bool",
    "conclusion": ("enum", CHECK_CONCLUSIONS),
    "head_sha": "nonempty-str",
    "app_id": "nonempty-str",
    "workflow_id": "nonempty-str",
    "workflow_path": "nonempty-str",
    "workflow_definition_ref": "nonempty-str",
    "workflow_definition_sha": "nonempty-str",
    "event": "nonempty-str",
    "trigger_subject": "nonempty-str",
    "policy_inputs_sha256": "sha256",
    "configuration_sha256": "sha256",
    "check_run_id": "nonempty-str",
    "workflow_run_id": "nonempty-str",
    "run_attempt": "int",
    "evidence_id": "id",
    "remote_observation_witness_id": "id",
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

READY_TRANSITION_FIELDS = {
    "ready_transition_id": "id",
    "idempotency_key": "nonempty-str",
    "repository_id": "nonempty-str",
    "pr_number": "int",
    "head_sha": "nonempty-str",
    "prior_lifecycle_state": "nonempty-str",
    "expected_lifecycle_state": "nonempty-str",
    "transition_witness_id": "str?",
    "status": ("enum", READY_TRANSITION_STATUSES),
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

CI_CANDIDATE_FIELDS = {
    "ci_candidate_id": "id",
    "repository_id": "nonempty-str",
    "pr_number": "int",
    "head_sha": "nonempty-str",
    "lifecycle_state": "nonempty-str",
    "transition_witness_id": "id",
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

CALIBRATION_FIELDS = {
    "frontier_runs": "int",
    "misses": ("dict", ("enum", CALIBRATION_MISS_CLASSES), "int"),
    "last_sample_sha": "str?",
    "last_sample_at": "str?",
    "sample_ids": ("list", "str"),
}

BLOCKER_FIELDS = {
    "blocker_id": "id",
    "class": ("enum", BLOCKER_CLASSES),
    "reason": "nonempty-str",
    "evidence_ids": ("list", "id"),
    "active": "bool",
    "opened_sequence": "int",
    "closed_sequence": "int?",
    "resolution_evidence_ids": ("list", "id"),
    "resolution_snapshot_epoch": "epoch?",
    "resolution_snapshot_fingerprint": "sha256?",
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
}

GREEN_SEAL_FIELDS = {
    "snapshot_epoch": "epoch",
    "snapshot_fingerprint": "sha256",
    "coverage_sha256": "sha256",
    "findings_sha256": "sha256",
    "repairs_sha256": "sha256",
    "reviews_sha256": "sha256",
    "checks_sha256": "sha256",
    "witness_chain_head_sha256": "sha256",
    "evidence_ids": ("set", "id"),
    "created_at": "nonempty-str",
}

HISTORY_FIELDS = {
    "sequence": "int",
    "generation": "int",
    "event": "nonempty-str",
    "snapshot_epoch": "epoch?",
    "snapshot_fingerprint": "sha256?",
    "data_sha256": "sha256",
    "previous_record_sha256": "sha256",
    "record_sha256": "sha256",
}

REMOTE_OBSERVATION_FIELDS = {
    "repository_id": "nonempty-str",
    "pr_number": "int",
    "pr_url": "nonempty-str",
    "head_sha": "nonempty-str",
    "lifecycle_state": "nonempty-str",
    "authority_manifest_sha256": "sha256",
    "unresolved_feedback_sha256": "sha256",
    "check_runs": ("list", "obj"),
    "workflow_runs": ("list", "obj"),
    "observed_at": "nonempty-str",
}

REMOTE_CHECK_RUN_FIELDS = {
    "check_run_id": "nonempty-str",
    "name": "nonempty-str",
    "app_id": "nonempty-str",
    "status": "nonempty-str",
    "conclusion": ("enum", CHECK_CONCLUSIONS),
    "head_sha": "nonempty-str",
}

REMOTE_WORKFLOW_RUN_FIELDS = {
    "workflow_run_id": "nonempty-str",
    "name": "nonempty-str",
    "status": "nonempty-str",
    "conclusion": ("enum", CHECK_CONCLUSIONS),
    "head_sha": "nonempty-str",
    "run_attempt": "int",
    "run_number": "int",
    "event": "nonempty-str",
}


# ---------------------------------------------------------------------------
# Constructor-owned subject projections

MANIFEST_AUTHORITY_ENTRY_SUBJECT = tuple(MANIFEST_AUTHORITY_ENTRY_FIELDS)


def _project(record: dict, fields, *, path: str) -> dict:
    missing = [f for f in fields if f not in record]
    if missing:
        _fail("missing-field", path, f"subject input missing field(s) {missing}")
    return {f: record[f] for f in fields}


def pr_metadata_subject(
    *,
    title: str,
    body: str,
    base_ref: str,
    scope_labels: tuple,
    declared_links: tuple,
) -> dict:
    labels = sorted(set(scope_labels))
    links = sorted(set(declared_links))
    for i, v in enumerate(labels):
        _check_field("nonempty-str", v, f"scope_labels[{i}]")
    for i, v in enumerate(links):
        _check_field("nonempty-str", v, f"declared_links[{i}]")
    subject = {
        "title": title,
        "body": body,
        "base_ref": base_ref,
        "scope_labels": labels,
        "declared_links": links,
    }
    _check_fields(
        subject,
        {
            "title": "str",
            "body": "str",
            "base_ref": "nonempty-str",
            "scope_labels": ("set", "nonempty-str"),
            "declared_links": ("set", "nonempty-str"),
        },
        "pr_metadata",
    )
    return subject


def manifest_payload(
    *,
    repository_id: str,
    pr_number: int,
    pr_url: str,
    authority_discovery_policy_id: str,
    authority_discovery_policy_version: str,
    authority_discovery_policy_sha256: str,
    authorities: tuple,
    feedback_history_policy_id: str,
    feedback_history_policy_version: str,
    feedback_history_policy_sha256: str,
    feedback_history_sha256: str,
    local_check_policy_id: str,
    local_check_policy_version: str,
    local_check_policy_sha256: str,
    required_check_policy_sha256: str,
    review_assignment_policy_id: str,
    review_assignment_policy_version: str,
    review_assignment_policy_sha256: str,
    command_execution_policy_id: str,
    command_execution_policy_version: str,
    command_execution_policy_sha256: str,
    evidence_ingestion_policy_id: str,
    evidence_ingestion_policy_version: str,
    evidence_ingestion_policy_sha256: str,
    hypothesis_derivation_policy_id: str,
    hypothesis_derivation_policy_version: str,
    hypothesis_derivation_policy_sha256: str,
    unresolved_feedback_sha256: str,
) -> dict:
    payload = {
        "repository_id": repository_id,
        "pr_number": pr_number,
        "pr_url": pr_url,
        "authority_discovery_policy_id": authority_discovery_policy_id,
        "authority_discovery_policy_version": authority_discovery_policy_version,
        "authority_discovery_policy_sha256": authority_discovery_policy_sha256,
        "authorities": [
            _project(a, MANIFEST_AUTHORITY_ENTRY_SUBJECT, path=f"authorities[{i}]") for i, a in enumerate(authorities)
        ],
        "feedback_history_policy_id": feedback_history_policy_id,
        "feedback_history_policy_version": feedback_history_policy_version,
        "feedback_history_policy_sha256": feedback_history_policy_sha256,
        "feedback_history_sha256": feedback_history_sha256,
        "local_check_policy_id": local_check_policy_id,
        "local_check_policy_version": local_check_policy_version,
        "local_check_policy_sha256": local_check_policy_sha256,
        "required_check_policy_sha256": required_check_policy_sha256,
        "review_assignment_policy_id": review_assignment_policy_id,
        "review_assignment_policy_version": review_assignment_policy_version,
        "review_assignment_policy_sha256": review_assignment_policy_sha256,
        "command_execution_policy_id": command_execution_policy_id,
        "command_execution_policy_version": command_execution_policy_version,
        "command_execution_policy_sha256": command_execution_policy_sha256,
        "evidence_ingestion_policy_id": evidence_ingestion_policy_id,
        "evidence_ingestion_policy_version": evidence_ingestion_policy_version,
        "evidence_ingestion_policy_sha256": evidence_ingestion_policy_sha256,
        "hypothesis_derivation_policy_id": hypothesis_derivation_policy_id,
        "hypothesis_derivation_policy_version": hypothesis_derivation_policy_version,
        "hypothesis_derivation_policy_sha256": hypothesis_derivation_policy_sha256,
        "unresolved_feedback_sha256": unresolved_feedback_sha256,
    }
    _check_fields(payload, MANIFEST_PAYLOAD_FIELDS, "authority_manifest_payload")
    for i, entry in enumerate(payload["authorities"]):
        _check_fields(entry, MANIFEST_AUTHORITY_ENTRY_FIELDS, f"authority_manifest_payload.authorities[{i}]")
        if entry["availability"] == "loaded" and entry["sha256"] is None:
            _fail(
                "missing-field",
                f"authority_manifest_payload.authorities[{i}].sha256",
                "loaded authority entry requires source sha256",
            )
        if entry["availability"] == "unavailable" and (
            entry["failure_class"] is None or entry["failure_sha256"] is None
        ):
            _fail(
                "missing-field",
                f"authority_manifest_payload.authorities[{i}]",
                "unavailable authority entry requires failure_class and failure_sha256",
            )
    return payload


def authority_manifest_id(payload: dict) -> str:
    _check_fields(payload, MANIFEST_PAYLOAD_FIELDS, "authority_manifest_payload")
    return sha256_json(payload)


SNAPSHOT_SUBJECT_FIELDS = tuple(f for f in SNAPSHOT_FIELDS if f != "fingerprint")


def snapshot_subject(*, fingerprint: object = None, **kwargs) -> dict:
    subject = _project(kwargs, SNAPSHOT_SUBJECT_FIELDS, path="snapshot")
    extra = set(kwargs) - set(SNAPSHOT_SUBJECT_FIELDS)
    if extra:
        _fail("unknown-field", "snapshot", f"unknown snapshot subject field(s) {sorted(extra)}")
    _check_fields(subject, {k: SNAPSHOT_FIELDS[k] for k in SNAPSHOT_SUBJECT_FIELDS}, "snapshot")
    fmt = subject["git_object_format"]
    for field in ("base_sha", "head_sha", "tree_sha"):
        if not _GITSHA_RE[fmt].match(subject[field]):
            _fail(
                "bad-git-sha",
                f"snapshot.{field}",
                f"length does not match git_object_format {fmt!r}",
            )
    return subject


def snapshot_fingerprint(snapshot: dict) -> str:
    subject = _project(snapshot, SNAPSHOT_SUBJECT_FIELDS, path="snapshot")
    return sha256_json(subject)


def content_id(raw: bytes) -> str:
    if not isinstance(raw, (bytes, bytearray)):
        _fail("not-bytes", "content", "content_id requires raw bytes")
    return "sha256:" + sha256_hex(bytes(raw))


_CONTENT_ID_RE = re.compile(r"^sha256:[0-9a-f]{64}$")


EVIDENCE_BINDING_SUBJECT = ("content_id", "kind", "snapshot_epoch", "snapshot_fingerprint")
LOADED_AUTHORITY_SUBJECT = ("authority_id", "kind", "locator", "availability", "sha256")
UNAVAILABLE_AUTHORITY_SUBJECT = (
    "authority_id",
    "kind",
    "locator",
    "availability",
    "failure_class",
    "failure_sha256",
)
IMPACT_MAP_SUBJECT = ("role", "entries")
COVERAGE_INVENTORY_SUBJECT = (
    "semantic_impact_map_id",
    "contract_impact_map_id",
    "challenger_attestation_id",
    "entries",
)
# assignees is deliberately excluded: hypothesis assignments bind obligation_id
# and obligations list hypothesis/dispatch assignees, so including it would make
# the derived id an unresolvable fixed point. assignees remains a required,
# referential-integrity-checked record field.
OBLIGATION_SUBJECT = (
    "category",
    "surfaces",
    "risk",
    "consequences",
    "scope_level",
    "minimum_capability_tier",
    "minimum_reasoning_floor",
)
HYPOTHESIS_ASSIGNMENT_SUBJECT = (
    "obligation_id",
    "family",
    "polarity",
    "statement",
    "derivation_policy_sha256",
    "minimum_capability_tier",
    "minimum_reasoning_floor",
)
ROUTE_SELECTION_SUBJECT = tuple(
    f for f in ROUTE_SELECTION_FIELDS if f not in ("evidence_id", "snapshot_epoch", "snapshot_fingerprint")
)
PENDING_DISPATCH_INTENT_SUBJECT = (
    "dispatch_id",
    "route_selection_id",
    "profile_resolution_witness_id",
    "assignment_ids",
    "context_evidence_ids",
    "instruction_manifest_sha256",
    "data_manifest_sha256",
    "tool_confinement_policy_sha256",
    "context_package_sha256",
    "hazard_framing_sha256",
    "required_tool_classes",
)
REVIEW_WRAPPER_SUBJECT = (
    "attestation_id",
    "dispatch_id",
    "assignment_ids",
    "verdict",
    "finding_ids",
    "uncertainties",
    "tool_transcript_sha256",
)
WITNESS_RECORD_SUBJECT = (
    "kind",
    "subject_sha256",
    "tool_use_id",
    "agent_id",
    "transcript_range",
    "record_positions",
    "chain_head_at_record",
    "source_locator",
)
FINDING_IDENTITY_SUBJECT = (
    "source_kind",
    "source_id",
    "source_assignment_id",
    "obligation_id",
    "severity",
    "title",
    "description",
    "locations",
    "regression_of",
)
REVIEW_REPAIR_INTENT_SUBJECT = (
    "repair_id",
    "finding_id",
    "target_kind",
    "target_ids",
    "invalidated_record_ids",
    "entry_adjudicator_attestation_id",
)
LOCAL_CHECK_SUBJECT = tuple(
    f
    for f in LOCAL_CHECK_FIELDS
    if f not in ("evidence_id", "execution_witness_id", "snapshot_epoch", "snapshot_fingerprint")
)
HOSTED_CHECK_SUBJECT = tuple(
    f
    for f in HOSTED_CHECK_FIELDS
    if f not in ("evidence_id", "remote_observation_witness_id", "snapshot_epoch", "snapshot_fingerprint")
)
READY_INTENT_SUBJECT = (
    "idempotency_key",
    "repository_id",
    "pr_number",
    "head_sha",
    "prior_lifecycle_state",
    "expected_lifecycle_state",
)
CI_CANDIDATE_SUBJECT = (
    "repository_id",
    "pr_number",
    "head_sha",
    "lifecycle_state",
    "transition_witness_id",
)
BLOCKER_IDENTITY_SUBJECT = ("blocker_id", "class", "reason", "evidence_ids", "opened_sequence")
GREEN_SEAL_SUBJECT = tuple(GREEN_SEAL_FIELDS)
HISTORY_RECORD_SUBJECT = (
    "sequence",
    "generation",
    "event",
    "snapshot_epoch",
    "snapshot_fingerprint",
    "data_sha256",
    "previous_record_sha256",
)


def evidence_binding_subject(record: dict) -> dict:
    return _project(record, EVIDENCE_BINDING_SUBJECT, path="evidence")


def loaded_authority_subject(record: dict) -> dict:
    return _project(record, LOADED_AUTHORITY_SUBJECT, path="authority")


def unavailable_authority_subject(record: dict) -> dict:
    return _project(record, UNAVAILABLE_AUTHORITY_SUBJECT, path="authority")


def impact_map_subject(record: dict) -> dict:
    return _project(record, IMPACT_MAP_SUBJECT, path="impact_map")


def coverage_inventory_subject(record: dict) -> dict:
    return _project(record, COVERAGE_INVENTORY_SUBJECT, path="coverage_inventory")


def obligation_subject(record: dict) -> dict:
    return _project(record, OBLIGATION_SUBJECT, path="obligation")


def hypothesis_assignment_subject(record: dict) -> dict:
    return _project(record, HYPOTHESIS_ASSIGNMENT_SUBJECT, path="hypothesis_assignment")


def route_selection_subject(record: dict) -> dict:
    return _project(record, ROUTE_SELECTION_SUBJECT, path="route_selection")


def pending_dispatch_intent_subject(record: dict) -> dict:
    return _project(record, PENDING_DISPATCH_INTENT_SUBJECT, path="dispatch")


def review_wrapper_subject(record: dict) -> dict:
    return _project(record, REVIEW_WRAPPER_SUBJECT, path="review")


def witness_record_subject(record: dict) -> dict:
    return _project(record, WITNESS_RECORD_SUBJECT, path="witness_record")


def finding_identity_subject(record: dict) -> dict:
    return _project(record, FINDING_IDENTITY_SUBJECT, path="finding")


def review_repair_intent_subject(record: dict) -> dict:
    return _project(record, REVIEW_REPAIR_INTENT_SUBJECT, path="review_repair")


def local_check_subject(record: dict) -> dict:
    return _project(record, LOCAL_CHECK_SUBJECT, path="check")


def hosted_check_subject(record: dict) -> dict:
    return _project(record, HOSTED_CHECK_SUBJECT, path="check")


def ready_intent_subject(record: dict) -> dict:
    return _project(record, READY_INTENT_SUBJECT, path="ready_transition")


def ci_candidate_subject(record: dict) -> dict:
    return _project(record, CI_CANDIDATE_SUBJECT, path="ci_candidate")


def blocker_identity_subject(record: dict) -> dict:
    return _project(record, BLOCKER_IDENTITY_SUBJECT, path="blocker")


def green_seal_subject(record: dict) -> dict:
    return _project(record, GREEN_SEAL_SUBJECT, path="green_seal")


def history_record_subject(record: dict) -> dict:
    return _project(record, HISTORY_RECORD_SUBJECT, path="history")


def authority_discovery_subject(snapshot: dict, manifest_payload_value: dict) -> dict:
    return {
        "snapshot_subject": _project(snapshot, SNAPSHOT_SUBJECT_FIELDS, path="snapshot"),
        "snapshot_fingerprint": snapshot.get("fingerprint"),
        "authority_manifest_sha256": sha256_json(manifest_payload_value),
    }


def profile_resolution_subject(route_selection: dict) -> dict:
    return {"route_selection_subject": route_selection_subject(route_selection)}


def review_launch_subject(
    pending_dispatch: dict,
    context_manifest: dict,
    *,
    tool_use_id: str,
    task_bytes_sha256: str,
    profile_name: str,
) -> dict:
    return {
        "pending_dispatch_intent": pending_dispatch_intent_subject(pending_dispatch),
        "context_manifest_sha256": sha256_json(context_manifest),
        "tool_use_id": tool_use_id,
        "task_bytes_sha256": task_bytes_sha256,
        "profile_name": profile_name,
    }


def review_completion_subject(
    raw_attestation_bytes: bytes,
    *,
    tool_transcript_sha256: str,
    agent_id: str,
) -> dict:
    return {
        "raw_attestation_sha256": sha256_hex(raw_attestation_bytes),
        "tool_transcript_sha256": tool_transcript_sha256,
        "agent_id": agent_id,
    }


def command_execution_subject(command_intent: dict, result: dict) -> dict:
    return {
        "command_intent": command_intent,
        "result": result,
    }


def remote_transition_subject(
    intent: dict,
    *,
    tool_use_id: str,
    prior_lifecycle_state: str,
    result_lifecycle_state: str,
) -> dict:
    return {
        "ready_intent": ready_intent_subject(intent),
        "tool_use_id": tool_use_id,
        "prior_lifecycle_state": prior_lifecycle_state,
        "result_lifecycle_state": result_lifecycle_state,
    }


def remote_observation_subject(observation_without_witness: dict) -> dict:
    return _project(
        observation_without_witness,
        tuple(REMOTE_OBSERVATION_FIELDS),
        path="remote_observation",
    )


def derived_id(kind: str, epoch: int, subject: dict) -> str:
    return f"{kind}:{epoch}:{sha256_json(subject)}"


# ---------------------------------------------------------------------------
# normalize_consequences


def normalize_consequences(*groups) -> tuple:
    merged = []
    for group in groups:
        for value in group:
            if value not in CONSEQUENCES:
                _fail("bad-enum", "consequences", f"unknown consequence {value!r}")
            merged.append(value)
    substantive = [c for c in merged if c != "none"]
    if substantive:
        result = [c for c in SUBSTANTIVE_CONSEQUENCES if c in set(substantive)]
    else:
        result = ["none"] if merged else []
    return tuple(result)


# ---------------------------------------------------------------------------
# new_state


def new_state(review_id: str, scratch_dir: Path) -> dict:
    if not _is_id(review_id):
        _fail("bad-id", "review_id", "expected identifier")
    scratch = str(scratch_dir)
    if not Path(scratch).is_absolute():
        _fail("relative-path", "scratch_dir", "expected absolute path")
    return {
        "schema_version": SCHEMA_VERSION,
        "review_id": review_id,
        "generation": 0,
        "status": "active",
        "stage": "intake",
        "scratch_dir": scratch,
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


# ---------------------------------------------------------------------------
# validate_remote_observation


def validate_remote_observation(observation: dict) -> None:
    _check_fields(observation, REMOTE_OBSERVATION_FIELDS, "remote_observation")
    for i, run in enumerate(observation["check_runs"]):
        _check_fields(run, REMOTE_CHECK_RUN_FIELDS, f"remote_observation.check_runs[{i}]")
    for i, run in enumerate(observation["workflow_runs"]):
        _check_fields(run, REMOTE_WORKFLOW_RUN_FIELDS, f"remote_observation.workflow_runs[{i}]")
        if run["run_attempt"] < 1:
            _fail(
                "bad-attempt",
                f"remote_observation.workflow_runs[{i}].run_attempt",
                "run_attempt must be >= 1",
            )


# ---------------------------------------------------------------------------
# validate_state


def _mapping(state: dict, key: str, fields: dict | None, errors) -> None:
    mapping = state[key]
    if not isinstance(mapping, dict):
        _fail("bad-type", key, "expected object mapping")
    for map_key, record in mapping.items():
        if fields is not None:
            _check_fields(record, fields, f"{key}.{map_key}")
        if isinstance(record, dict):
            id_field = {
                "content_objects": "content_id",
                "evidence": "evidence_id",
                "authorities": "authority_id",
                "impact_maps": "impact_map_id",
                "obligations": "obligation_id",
                "hypothesis_assignments": "hypothesis_assignment_id",
                "dispatches": "dispatch_id",
                "reviews": "attestation_id",
                "route_selections": "route_selection_id",
                "witness_records": "witness_id",
                "findings": "finding_id",
                "review_repairs": "repair_id",
                "checks": "check_id",
                "blockers": "blocker_id",
            }.get(key)
            if id_field and record.get(id_field) != map_key:
                _fail(
                    "key-mismatch",
                    f"{key}.{map_key}.{id_field}",
                    f"mapping key {map_key!r} differs from record id {record.get(id_field)!r}",
                )


def _current_epoch_fingerprint(state: dict) -> tuple:
    snapshot = state["snapshot"]
    if snapshot is None:
        return (None, None)
    return (snapshot["epoch"], snapshot["fingerprint"])


def _check_epoch_binding(record: dict, path: str, epoch: int, fingerprint: str) -> None:
    rec_epoch = record["snapshot_epoch"]
    if epoch is not None:
        if rec_epoch > epoch:
            _fail("bad-epoch", f"{path}.snapshot_epoch", "record epoch is ahead of snapshot")
        if rec_epoch == epoch and record["snapshot_fingerprint"] != fingerprint:
            _fail(
                "fingerprint-mismatch",
                f"{path}.snapshot_fingerprint",
                "current-epoch record fingerprint does not match snapshot",
            )


def _check_derived_id(record: dict, kind: str, subject_fn, epoch: int, id_field: str, path: str) -> None:
    subject = subject_fn(record)
    expected = derived_id(kind, epoch, subject)
    if record[id_field] != expected:
        _fail(
            "id-mismatch",
            f"{path}.{id_field}",
            f"expected derived id {expected!r}, found {record[id_field]!r}",
        )


def validate_state(state: dict, *, verify_content: bool = True) -> None:
    if not isinstance(state, dict):
        _fail("bad-type", "state", "expected object")
    if set(state) != STATE_KEYS:
        missing = sorted(STATE_KEYS - set(state))
        extra = sorted(set(state) - STATE_KEYS)
        parts = []
        if missing:
            parts.append(f"missing {missing}")
        if extra:
            parts.append(f"unknown {extra}")
        _fail("top-level-keys", "state", "; ".join(parts))
    if state["schema_version"] != SCHEMA_VERSION:
        _fail("bad-version", "schema_version", "schema_version must be 2")
    if not _is_id(state["review_id"]):
        _fail("bad-id", "review_id", "expected identifier")
    if not isinstance(state["generation"], int) or isinstance(state["generation"], bool) or state["generation"] < 0:
        _fail("bad-generation", "generation", "generation must be a non-negative integer")
    if state["status"] not in STATUSES:
        _fail("bad-enum", "status", f"unknown value {state['status']!r}")
    if state["stage"] not in STAGES:
        _fail("bad-enum", "stage", f"unknown value {state['stage']!r}")
    if not Path(state["scratch_dir"]).is_absolute():
        _fail("relative-path", "scratch_dir", "expected absolute path")

    snapshot = state["snapshot"]
    if snapshot is not None:
        _check_fields(snapshot, SNAPSHOT_FIELDS, "snapshot")
        fmt = snapshot["git_object_format"]
        for field in ("base_sha", "head_sha", "tree_sha"):
            if not _GITSHA_RE[fmt].match(snapshot[field]):
                _fail(
                    "bad-git-sha",
                    f"snapshot.{field}",
                    f"length does not match git_object_format {fmt!r}",
                )
        expected_fp = snapshot_fingerprint(snapshot)
        if snapshot["fingerprint"] != expected_fp:
            _fail(
                "fingerprint-mismatch",
                "snapshot.fingerprint",
                "does not equal sha256 of canonical snapshot subject",
            )
    epoch, fingerprint = _current_epoch_fingerprint(state)
    if snapshot is None and state["stage"] not in ("intake", "blocked"):
        _fail("missing-snapshot", "snapshot", "snapshot required once past intake")

    _mapping(state, "content_objects", CONTENT_OBJECT_FIELDS, None)
    _mapping(state, "evidence", EVIDENCE_FIELDS, None)
    _mapping(state, "impact_maps", IMPACT_MAP_FIELDS, None)
    _mapping(state, "obligations", OBLIGATION_FIELDS, None)
    _mapping(state, "hypothesis_assignments", HYPOTHESIS_ASSIGNMENT_FIELDS, None)
    _mapping(state, "dispatches", DISPATCH_FIELDS, None)
    _mapping(state, "reviews", REVIEW_FIELDS, None)
    _mapping(state, "route_selections", ROUTE_SELECTION_FIELDS, None)
    _mapping(state, "witness_records", WITNESS_RECORD_FIELDS, None)
    _mapping(state, "findings", FINDING_FIELDS, None)
    _mapping(state, "review_repairs", REVIEW_REPAIR_FIELDS, None)
    _mapping(state, "blockers", BLOCKER_FIELDS, None)

    # Per-entry strict shapes ------------------------------------------------
    for map_id, record in state["impact_maps"].items():
        if not record["entries"]:
            _fail("empty", f"impact_maps.{map_id}.entries", "entries must be non-empty")
        seen_surfaces = set()
        seen_categories = set()
        for i, entry in enumerate(record["entries"]):
            _check_fields(entry, IMPACT_ENTRY_FIELDS, f"impact_maps.{map_id}.entries[{i}]")
            if entry["surface"] in seen_surfaces:
                _fail("duplicate", f"impact_maps.{map_id}.entries[{i}].surface", "duplicate surface")
            seen_surfaces.add(entry["surface"])
            if entry["category"] in seen_categories:
                _fail(
                    "duplicate",
                    f"impact_maps.{map_id}.entries[{i}].category",
                    "duplicate category",
                )
            seen_categories.add(entry["category"])
            if "none" in entry["consequences"] and len(entry["consequences"]) > 1:
                _fail(
                    "bad-consequence",
                    f"impact_maps.{map_id}.entries[{i}].consequences",
                    "'none' cannot coexist with a substantive consequence",
                )

    inventory = state["coverage_inventory"]
    if inventory is not None:
        _check_fields(inventory, COVERAGE_INVENTORY_FIELDS, "coverage_inventory")
        if not inventory["entries"]:
            _fail("empty", "coverage_inventory.entries", "entries must be non-empty")
        for i, entry in enumerate(inventory["entries"]):
            _check_fields(entry, COVERAGE_ENTRY_FIELDS, f"coverage_inventory.entries[{i}]")
            if "none" in entry["consequences"] and len(entry["consequences"]) > 1:
                _fail(
                    "bad-consequence",
                    f"coverage_inventory.entries[{i}].consequences",
                    "'none' cannot coexist with a substantive consequence",
                )

    manifest = state["authority_manifest"]
    if manifest is not None:
        _check_fields(manifest, AUTHORITY_MANIFEST_FIELDS, "authority_manifest")

    for auth_id, record in state["authorities"].items():
        avail = record.get("availability")
        if avail == "loaded":
            _check_fields(record, LOADED_AUTHORITY_FIELDS, f"authorities.{auth_id}")
        elif avail == "unavailable":
            _check_fields(record, UNAVAILABLE_AUTHORITY_FIELDS, f"authorities.{auth_id}")
        else:
            _fail(
                "bad-enum",
                f"authorities.{auth_id}.availability",
                f"unknown value {avail!r}",
            )

    for check_id, record in state["checks"].items():
        locus = record.get("locus")
        if locus == "local":
            _check_fields(record, LOCAL_CHECK_FIELDS, f"checks.{check_id}")
        elif locus == "hosted":
            _check_fields(record, HOSTED_CHECK_FIELDS, f"checks.{check_id}")
        else:
            _fail("bad-enum", f"checks.{check_id}.locus", f"unknown value {locus!r}")

    ready = state["ready_transition"]
    if ready is not None:
        _check_fields(ready, READY_TRANSITION_FIELDS, "ready_transition")
        expected_id = f"ready:{ready['snapshot_epoch']}:{ready['idempotency_key']}"
        if ready["ready_transition_id"] != expected_id:
            _fail(
                "id-mismatch",
                "ready_transition.ready_transition_id",
                f"expected {expected_id!r}",
            )

    ci = state["ci_candidate"]
    if ci is not None:
        _check_fields(ci, CI_CANDIDATE_FIELDS, "ci_candidate")
        expected_ci = derived_id("ci-candidate", ci["snapshot_epoch"], ci_candidate_subject(ci))
        if ci["ci_candidate_id"] != expected_ci:
            _fail("id-mismatch", "ci_candidate.ci_candidate_id", f"expected {expected_ci!r}")

    calibration = state["calibration"]
    if calibration is not None:
        _check_fields(calibration, CALIBRATION_FIELDS, "calibration")

    seal = state["green_seal"]
    if seal is not None:
        if state["stage"] != "green-candidate":
            _fail(
                "premature-seal",
                "green_seal",
                "green_seal is only permitted at the green-candidate stage",
            )
        _check_fields(seal, GREEN_SEAL_FIELDS, "green_seal")
    if state["status"] not in STATUSES:
        _fail("bad-enum", "status", "unreachable")
    if state["stage"] == "green-candidate" and state["status"] == "blocked":
        pass

    # Epoch bindings ---------------------------------------------------------
    bound = []
    for key in (
        "authorities",
        "impact_maps",
        "obligations",
        "hypothesis_assignments",
        "dispatches",
        "reviews",
        "route_selections",
        "witness_records",
        "review_repairs",
        "checks",
        "blockers",
    ):
        bound.extend((f"{key}.{k}", v) for k, v in state[key].items())
    bound.extend(("evidence." + k, v) for k, v in state["evidence"].items())
    if manifest is not None:
        bound.append(("authority_manifest", manifest))
    if inventory is not None:
        bound.append(("coverage_inventory", inventory))
    if ready is not None:
        bound.append(("ready_transition", ready))
    if ci is not None:
        bound.append(("ci_candidate", ci))
    for path, record in bound:
        _check_epoch_binding(record, path, epoch, fingerprint)

    # Derived IDs ------------------------------------------------------------
    for map_id, record in state["impact_maps"].items():
        _check_derived_id(
            record,
            "impact-map",
            impact_map_subject,
            record["snapshot_epoch"],
            "impact_map_id",
            f"impact_maps.{map_id}",
        )
    if inventory is not None:
        _check_derived_id(
            inventory,
            "coverage-inventory",
            coverage_inventory_subject,
            inventory["snapshot_epoch"],
            "coverage_inventory_id",
            "coverage_inventory",
        )
    for oid, record in state["obligations"].items():
        _check_derived_id(
            record,
            "obligation",
            obligation_subject,
            record["snapshot_epoch"],
            "obligation_id",
            f"obligations.{oid}",
        )
    for hid, record in state["hypothesis_assignments"].items():
        _check_derived_id(
            record,
            "hypothesis",
            hypothesis_assignment_subject,
            record["snapshot_epoch"],
            "hypothesis_assignment_id",
            f"hypothesis_assignments.{hid}",
        )
    for fid, record in state["findings"].items():
        subject = finding_identity_subject(record)
        expected = f"finding:{sha256_json(subject)}"
        if record["finding_id"] != expected:
            _fail("id-mismatch", f"findings.{fid}.finding_id", f"expected {expected!r}")
    for eid, record in state["evidence"].items():
        subject = evidence_binding_subject(record)
        expected = f"evidence:{sha256_json(subject)}"
        if record["evidence_id"] != expected:
            _fail("id-mismatch", f"evidence.{eid}.evidence_id", f"expected {expected!r}")

    # Cross-references -------------------------------------------------------
    evidence_ids = set(state["evidence"])
    content_ids = set(state["content_objects"])
    witness_ids = set(state["witness_records"])
    dispatch_ids = set(state["dispatches"])
    review_ids = set(state["reviews"])
    route_ids = set(state["route_selections"])
    obligation_ids = set(state["obligations"])
    hypothesis_ids = set(state["hypothesis_assignments"])
    finding_ids = set(state["findings"])
    check_ids = set(state["checks"])
    map_ids = set(state["impact_maps"])

    def need(container: set, value: str, path: str) -> None:
        if value not in container:
            _fail("dangling-ref", path, f"references unknown id {value!r}")

    for eid, record in state["evidence"].items():
        need(content_ids, record["content_id"], f"evidence.{eid}.content_id")

    if manifest is not None:
        need(evidence_ids, manifest["payload_evidence_id"], "authority_manifest.payload_evidence_id")
        need(witness_ids, manifest["discovery_witness_id"], "authority_manifest.discovery_witness_id")
        wrec = state["witness_records"].get(manifest["discovery_witness_id"])
        if wrec and wrec["kind"] != "authority-discovery":
            _fail("wrong-kind", "authority_manifest.discovery_witness_id", "witness kind must be authority-discovery")
        if snapshot is not None:
            payload_evidence = state["evidence"].get(manifest["payload_evidence_id"])
            if payload_evidence and payload_evidence["kind"] != "authority-manifest-payload":
                _fail(
                    "wrong-kind",
                    "authority_manifest.payload_evidence_id",
                    "evidence kind must be authority-manifest-payload",
                )

    for aid, record in state["authorities"].items():
        if record["availability"] == "loaded":
            need(evidence_ids, record["evidence_id"], f"authorities.{aid}.evidence_id")
        else:
            need(evidence_ids, record["failure_evidence_id"], f"authorities.{aid}.failure_evidence_id")

    for mid, record in state["impact_maps"].items():
        need(evidence_ids, record["evidence_id"], f"impact_maps.{mid}.evidence_id")

    if inventory is not None:
        need(map_ids, inventory["semantic_impact_map_id"], "coverage_inventory.semantic_impact_map_id")
        need(map_ids, inventory["contract_impact_map_id"], "coverage_inventory.contract_impact_map_id")
        need(review_ids, inventory["challenger_attestation_id"], "coverage_inventory.challenger_attestation_id")
        need(evidence_ids, inventory["evidence_id"], "coverage_inventory.evidence_id")
        if map_ids:
            sem = state["impact_maps"].get(inventory["semantic_impact_map_id"])
            con = state["impact_maps"].get(inventory["contract_impact_map_id"])
            if sem and sem["role"] != "impact-mapper-semantic":
                _fail(
                    "wrong-role",
                    "coverage_inventory.semantic_impact_map_id",
                    "map role must be impact-mapper-semantic",
                )
            if con and con["role"] != "impact-mapper-contract":
                _fail(
                    "wrong-role",
                    "coverage_inventory.contract_impact_map_id",
                    "map role must be impact-mapper-contract",
                )
            if sem and con:
                # The inventory must cover the union of both current maps:
                # every surface, category, and hazard.
                inv_surfaces = {}
                for entry in inventory["entries"]:
                    inv_surfaces.setdefault(entry["surface"], entry)
                for role_map in (sem, con):
                    for entry in role_map["entries"]:
                        inv = inv_surfaces.get(entry["surface"])
                        if inv is None:
                            _fail(
                                "coverage-gap",
                                "coverage_inventory.entries",
                                f"surface {entry['surface']!r} from {role_map['impact_map_id']!r} is absent",
                            )
                        if entry["category"] not in inv["categories"]:
                            _fail(
                                "coverage-gap",
                                f"coverage_inventory.entries[{entry['surface']}].categories",
                                f"category {entry['category']!r} is absent",
                            )
                        for hazard in entry["hazards"]:
                            if hazard not in inv["hazards"]:
                                _fail(
                                    "coverage-gap",
                                    f"coverage_inventory.entries[{entry['surface']}].hazards",
                                    f"hazard {hazard!r} is absent",
                                )

    for oid, record in state["obligations"].items():
        for eid in record["evidence_ids"]:
            need(evidence_ids, eid, f"obligations.{oid}.evidence_ids")
        for aid in record["not_applicable_attestation_ids"]:
            need(review_ids, aid, f"obligations.{oid}.not_applicable_attestation_ids")
        for aid in record["assignees"]:
            if aid not in hypothesis_ids and aid not in dispatch_ids:
                _fail(
                    "dangling-ref",
                    f"obligations.{oid}.assignees",
                    f"assignee {aid!r} is neither a hypothesis assignment nor a dispatch",
                )
        if record["status"] == "not-applicable" and not record["not_applicable_attestation_ids"]:
            _fail(
                "missing-attestation",
                f"obligations.{oid}.not_applicable_attestation_ids",
                "not-applicable status requires attestation ids",
            )

    for hid, record in state["hypothesis_assignments"].items():
        need(obligation_ids, record["obligation_id"], f"hypothesis_assignments.{hid}.obligation_id")

    for rsid, record in state["route_selections"].items():
        need(evidence_ids, record["evidence_id"], f"route_selections.{rsid}.evidence_id")
        if record["required_role"] not in record["qualified_roles"]:
            _fail(
                "role-mismatch",
                f"route_selections.{rsid}.qualified_roles",
                "required role must be among qualified roles",
            )

    for did, record in state["dispatches"].items():
        need(route_ids, record["route_selection_id"], f"dispatches.{did}.route_selection_id")
        need(witness_ids, record["profile_resolution_witness_id"], f"dispatches.{did}.profile_resolution_witness_id")
        wrec = state["witness_records"].get(record["profile_resolution_witness_id"])
        if wrec and wrec["kind"] != "profile-resolution":
            _fail(
                "wrong-kind",
                f"dispatches.{did}.profile_resolution_witness_id",
                "witness kind must be profile-resolution",
            )
        for field, kind in (
            ("launch_witness_id", "review-launch"),
            ("completion_witness_id", "review-completion"),
        ):
            wid = record[field]
            if wid is not None:
                need(witness_ids, wid, f"dispatches.{did}.{field}")
                w = state["witness_records"].get(wid)
                if w and w["kind"] != kind:
                    _fail("wrong-kind", f"dispatches.{did}.{field}", f"witness kind must be {kind}")
        for eid in record["context_evidence_ids"]:
            need(evidence_ids, eid, f"dispatches.{did}.context_evidence_ids")
        for aid in record["assignment_ids"]:
            if aid not in hypothesis_ids and aid not in obligation_ids and aid not in finding_ids:
                _fail(
                    "dangling-ref",
                    f"dispatches.{did}.assignment_ids",
                    f"assignment {aid!r} is not a known assignment target",
                )

    for rid, record in state["reviews"].items():
        need(dispatch_ids, record["dispatch_id"], f"reviews.{rid}.dispatch_id")
        need(evidence_ids, record["evidence_id"], f"reviews.{rid}.evidence_id")
        need(witness_ids, record["completion_witness_id"], f"reviews.{rid}.completion_witness_id")
        w = state["witness_records"].get(record["completion_witness_id"])
        if w and w["kind"] != "review-completion":
            _fail("wrong-kind", f"reviews.{rid}.completion_witness_id", "witness kind must be review-completion")
        dispatch = state["dispatches"].get(record["dispatch_id"])
        if dispatch is not None:
            if record["assignment_ids"] != dispatch["assignment_ids"]:
                _fail(
                    "assignment-mismatch",
                    f"reviews.{rid}.assignment_ids",
                    "review assignments must equal its dispatch's assignments",
                )
            if dispatch["completion_witness_id"] != record["completion_witness_id"]:
                _fail(
                    "witness-mismatch",
                    f"reviews.{rid}.completion_witness_id",
                    "review completion witness must equal its dispatch's",
                )
        for fid in record["finding_ids"]:
            need(finding_ids, fid, f"reviews.{rid}.finding_ids")

    for wid, record in state["witness_records"].items():
        expected_wid = derived_id("witness", record["snapshot_epoch"], witness_record_subject(record))
        if record["witness_id"] != expected_wid:
            _fail(
                "id-mismatch",
                f"witness_records.{wid}.witness_id",
                f"expected derived id {expected_wid!r}",
            )

    for fid, record in state["findings"].items():
        for eid in record["evidence_ids"]:
            need(evidence_ids, eid, f"findings.{fid}.evidence_ids")
        if record["obligation_id"] is not None:
            need(obligation_ids, record["obligation_id"], f"findings.{fid}.obligation_id")
        if record["regression_of"] is not None:
            need(finding_ids, record["regression_of"], f"findings.{fid}.regression_of")
        if record["source_kind"] == "review":
            need(review_ids, record["source_id"], f"findings.{fid}.source_id")
        elif record["source_kind"] == "check":
            need(check_ids, record["source_id"], f"findings.{fid}.source_id")
        if record["resolution"] is not None:
            res = record["resolution"]
            if not isinstance(res, dict):
                _fail("bad-type", f"findings.{fid}.resolution", "expected object")
            else:
                for ref_field, container in (
                    ("review_id", review_ids),
                    ("check_id", check_ids),
                    ("human_decision_witness_id", witness_ids),
                ):
                    if ref_field in res and res[ref_field] is not None:
                        need(container, res[ref_field], f"findings.{fid}.resolution.{ref_field}")

    for rid, record in state["review_repairs"].items():
        need(finding_ids, record["finding_id"], f"review_repairs.{rid}.finding_id")
        need(
            review_ids,
            record["entry_adjudicator_attestation_id"],
            f"review_repairs.{rid}.entry_adjudicator_attestation_id",
        )
        if record["verification_attestation_id"] is not None:
            need(review_ids, record["verification_attestation_id"], f"review_repairs.{rid}.verification_attestation_id")

    for cid, record in state["checks"].items():
        need(evidence_ids, record["evidence_id"], f"checks.{cid}.evidence_id")
        if record["locus"] == "local":
            need(witness_ids, record["execution_witness_id"], f"checks.{cid}.execution_witness_id")
            w = state["witness_records"].get(record["execution_witness_id"])
            if w and w["kind"] != "command-execution":
                _fail("wrong-kind", f"checks.{cid}.execution_witness_id", "witness kind must be command-execution")
        else:
            need(witness_ids, record["remote_observation_witness_id"], f"checks.{cid}.remote_observation_witness_id")
            w = state["witness_records"].get(record["remote_observation_witness_id"])
            if w and w["kind"] != "remote-observation":
                _fail(
                    "wrong-kind",
                    f"checks.{cid}.remote_observation_witness_id",
                    "witness kind must be remote-observation",
                )

    if ready is not None:
        if ready["transition_witness_id"] is not None:
            need(witness_ids, ready["transition_witness_id"], "ready_transition.transition_witness_id")
            w = state["witness_records"].get(ready["transition_witness_id"])
            if w and w["kind"] != "remote-transition":
                _fail("wrong-kind", "ready_transition.transition_witness_id", "witness kind must be remote-transition")
        if ready["status"] == "completed" and ready["transition_witness_id"] is None:
            _fail(
                "missing-witness",
                "ready_transition.transition_witness_id",
                "completed transition requires a witness",
            )

    if ci is not None:
        need(witness_ids, ci["transition_witness_id"], "ci_candidate.transition_witness_id")

    # Seal projection digests -------------------------------------------------
    if seal is not None:
        if seal["snapshot_epoch"] != epoch or seal["snapshot_fingerprint"] != fingerprint:
            _fail("epoch-mismatch", "green_seal", "seal must bind the current snapshot")
        if seal["coverage_sha256"] != sha256_json(state["coverage_inventory"]):
            _fail("digest-mismatch", "green_seal.coverage_sha256", "coverage digest mismatch")
        if seal["findings_sha256"] != sha256_json(state["findings"]):
            _fail("digest-mismatch", "green_seal.findings_sha256", "findings digest mismatch")
        if seal["repairs_sha256"] != sha256_json(state["review_repairs"]):
            _fail("digest-mismatch", "green_seal.repairs_sha256", "repairs digest mismatch")
        if seal["reviews_sha256"] != sha256_json(state["reviews"]):
            _fail("digest-mismatch", "green_seal.reviews_sha256", "reviews digest mismatch")
        if seal["checks_sha256"] != sha256_json(state["checks"]):
            _fail("digest-mismatch", "green_seal.checks_sha256", "checks digest mismatch")
        for eid in seal["evidence_ids"]:
            need(evidence_ids, eid, "green_seal.evidence_ids")

    # Blockers / status consistency ------------------------------------------
    active_blockers = [b for b in state["blockers"].values() if b["active"]]
    if len(active_blockers) > 1:
        _fail("multi-blocker", "blockers", "at most one active blocker is permitted")
    if state["status"] == "blocked" and len(active_blockers) != 1:
        _fail("blocker-missing", "status", "blocked status requires exactly one active blocker")
    if state["status"] == "active" and active_blockers:
        _fail("blocker-active", "status", "active status forbids an active blocker")
    for bid, record in state["blockers"].items():
        if not record["active"]:
            if record["closed_sequence"] is None:
                _fail(
                    "blocker-unclosed",
                    f"blockers.{bid}.closed_sequence",
                    "inactive blocker requires closed_sequence",
                )
            if not record["resolution_evidence_ids"]:
                _fail(
                    "blocker-unresolved",
                    f"blockers.{bid}.resolution_evidence_ids",
                    "closed blocker requires resolution evidence",
                )
            if record["resolution_snapshot_epoch"] is None or record["resolution_snapshot_fingerprint"] is None:
                _fail(
                    "blocker-unresolved",
                    f"blockers.{bid}",
                    "closed blocker requires resolution epoch and fingerprint",
                )
        for eid in record["evidence_ids"] + record["resolution_evidence_ids"]:
            need(evidence_ids, eid, f"blockers.{bid}.evidence_ids")

    if state["status"] == "reviewed-with-exceptions":
        accepted = [f for f in state["findings"].values() if f["disposition"] == "accepted-risk"]
        if not accepted:
            _fail(
                "no-accepted-risk",
                "status",
                "reviewed-with-exceptions requires at least one accepted-risk finding",
            )
        open_findings = [
            f
            for f in state["findings"].values()
            if f["disposition"] in ("open", "fixing", "review-repairing", "contested", "deferred", "unassessed")
        ]
        if open_findings:
            _fail(
                "open-findings",
                "status",
                "reviewed-with-exceptions forbids unresolved findings",
            )
        for f in accepted:
            res = f.get("resolution") or {}
            if not res.get("human_decision_witness_id"):
                _fail(
                    "missing-witness",
                    f"findings.{f['finding_id']}.resolution",
                    "accepted-risk requires a human-decision witness",
                )

    # History chain -----------------------------------------------------------
    history = state["history"]
    if not isinstance(history, list):
        _fail("bad-type", "history", "expected list")
    prev = "0" * 64
    for i, record in enumerate(history):
        _check_fields(record, HISTORY_FIELDS, f"history[{i}]")
        if record["sequence"] != i + 1:
            _fail("bad-sequence", f"history[{i}].sequence", "sequence must be 1-based and contiguous")
        if record["generation"] > state["generation"]:
            _fail("bad-generation", f"history[{i}].generation", "generation exceeds state generation")
        if record["previous_record_sha256"] != prev:
            _fail("chain-broken", f"history[{i}].previous_record_sha256", "previous record hash mismatch")
        expected = sha256_json(history_record_subject(record))
        if record["record_sha256"] != expected:
            _fail("chain-broken", f"history[{i}].record_sha256", "record hash mismatch")
        prev = record["record_sha256"]
    if history and history[-1]["generation"] != state["generation"]:
        _fail(
            "generation-mismatch",
            "history",
            "final history generation must equal top-level generation",
        )

    # Epoch-zero prohibition ---------------------------------------------------
    for key in (
        "authorities",
        "impact_maps",
        "obligations",
        "hypothesis_assignments",
        "dispatches",
        "reviews",
        "route_selections",
        "witness_records",
        "review_repairs",
        "checks",
        "blockers",
        "evidence",
    ):
        for map_key, record in state[key].items():
            if record.get("snapshot_epoch", 1) < 1:
                _fail("epoch-zero", f"{key}.{map_key}.snapshot_epoch", "epoch-zero record")

    # Content verification -----------------------------------------------------
    if verify_content:
        for cid, record in state["content_objects"].items():
            path = Path(record["path"])
            store_root = Path(state["scratch_dir"]) / "evidence-store" / "sha256"
            try:
                path.relative_to(store_root)
            except ValueError:
                _fail(
                    "unsafe-path",
                    f"content_objects.{cid}.path",
                    "content object path must live under the review-owned content store",
                )
            if path.name != record["sha256"]:
                _fail(
                    "unsafe-path",
                    f"content_objects.{cid}.path",
                    "content object path must be the canonical store path for its digest",
                )
            if not path.is_file():
                _fail("content-missing", f"content_objects.{cid}.path", "content file is missing")
            raw = path.read_bytes()
            if len(raw) != record["bytes"]:
                _fail("content-resized", f"content_objects.{cid}.bytes", "content length mismatch")
            if sha256_hex(raw) != record["sha256"]:
                _fail("content-mismatch", f"content_objects.{cid}.sha256", "content digest mismatch")
            if cid != "sha256:" + record["sha256"]:
                _fail(
                    "content-mismatch",
                    f"content_objects.{cid}.content_id",
                    "content_id must equal 'sha256:' + sha256(bytes)",
                )
    else:
        for cid, record in state["content_objects"].items():
            if cid != "sha256:" + record["sha256"]:
                _fail(
                    "content-mismatch",
                    f"content_objects.{cid}.content_id",
                    "content_id must equal 'sha256:' + sha256(bytes)",
                )

    return None
