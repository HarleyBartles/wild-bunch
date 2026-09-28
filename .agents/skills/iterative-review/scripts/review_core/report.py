#!/usr/bin/env python3
"""Section-4 structured reviewer report contract.

``parse_report`` strict-decodes and validates one reviewer report against the
spec's section-4 schema: exact top-level key set, closed reviewer block,
role-discriminated assignment-result key sets, and typed outcome enums.
``derive_verdict`` computes the lawful verdict from parsed content rather than
trusting the declared field; ``report_to_action_data`` folds the report into
the ``complete_action`` payload grammar, attaching the typed claims
(``structured_output``, ``assignment_results``, ``exemption_outcome``) the
kernel pops and verifies at install time.

This module is a deterministic contract surface: it never performs I/O, model
calls, or state access.
"""

from __future__ import annotations

from . import model


class ReportError(Exception):
    """Typed report-contract failure. ``.reason`` is a stable code."""

    def __init__(self, reason: str, detail: str):
        super().__init__(f"{reason}: {detail}")
        self.reason = reason
        self.detail = detail


# Role -> allowed assignment-result kinds, per spec section 4.
RESULT_KINDS = {
    "impact-mapper-semantic": frozenset({"impact-map"}),
    "impact-mapper-contract": frozenset({"impact-map"}),
    "scope-challenger": frozenset({"coverage-inventory"}),
    "obligation-reviewer": frozenset({"obligation"}),
    "exemption-challenger": frozenset({"exemption-challenge"}),
    "finding-adjudicator": frozenset({"finding-adjudication"}),
    "fix-reviewer": frozenset({"fix-review"}),
    "review-repair-verifier": frozenset({"review-repair"}),
    "blind-final": frozenset({"blind-final"}),
    "closure-auditor": frozenset({"closure-audit"}),
}

# Product-bearing results (mapper/challenger) mark whether the product was
# produced; a capped run reports "incomplete".
PRODUCT_OUTCOMES = ("produced", "incomplete")

_RESULT_KEYS = {
    "impact-map": frozenset({"kind", "outcome", "evidence_ids"}),
    "coverage-inventory": frozenset({"kind", "outcome", "evidence_ids"}),
    "obligation": frozenset({"kind", "outcome", "evidence_ids"}),
    "blind-final": frozenset({"kind", "outcome", "evidence_ids"}),
    "closure-audit": frozenset({"kind", "outcome", "evidence_ids"}),
    "exemption-challenge": frozenset({"kind", "outcome", "hypothesis_assignment_ids", "evidence_ids"}),
    "finding-adjudication": frozenset(
        {
            "kind",
            "outcome",
            "remediation_class",
            "repair_target_kind",
            "repair_target_ids",
            "evidence_ids",
        }
    ),
    "fix-review": frozenset({"kind", "outcome", "verified_obligation_ids", "evidence_ids"}),
    "review-repair": frozenset({"kind", "outcome", "repair_id", "replacement_record_ids", "evidence_ids"}),
}

_RESULT_OUTCOMES = {
    "impact-map": PRODUCT_OUTCOMES,
    "coverage-inventory": PRODUCT_OUTCOMES,
    "obligation": model.OBLIGATION_OUTCOMES,
    "blind-final": model.BLIND_FINAL_OUTCOMES,
    "closure-audit": model.CLOSURE_AUDIT_OUTCOMES,
    "exemption-challenge": model.EXEMPTION_CHALLENGE_OUTCOMES,
    "finding-adjudication": model.FINDING_ADJUDICATION_OUTCOMES,
    "fix-review": model.FIX_REVIEW_OUTCOMES,
    "review-repair": model.REVIEW_REPAIR_OUTCOMES,
}

_TOP_KEYS = frozenset(
    {
        "schema_version",
        "review_session_id",
        "attestation_id",
        "snapshot_epoch",
        "snapshot_fingerprint",
        "dispatch_id",
        "reviewer",
        "assignments",
        "structured_output",
        "findings",
        "uncertainties",
        "verdict",
    }
)
_REVIEWER_KEYS = frozenset(
    {
        "profile",
        "profile_sha256",
        "capability_tier",
        "reasoning_floor",
        "model",
        "reasoning",
        "context_mode",
    }
)
_ASSIGNMENT_KEYS = frozenset({"assignment_id", "result", "notes"})
_FINDING_KEYS = frozenset({"title", "description", "locations", "severity", "evidence_ids", "source_assignment_id"})

_MAPPER_ACTIONS = frozenset({"map-impact-semantic", "map-impact-contract"})
_PLURAL_ACTIONS = frozenset(
    {
        "run-fast-review",
        "run-focused-review",
        "run-strong-review",
        "run-exemption-challenge",
        "adjudicate-findings",
        "review-fix",
        "verify-review-repair",
    }
)
_SINGLE_ACTIONS = frozenset({"run-final-review", "run-closure-audit"})

# Outcomes that leave nothing to remediate: the report may still be clean.
_NON_FINDING_OUTCOMES = frozenset(
    {
        "covered",
        "not-applicable",
        "verified",
        "false-positive",
        "not-applicable-confirmed",
        "produced",
    }
)


def _is_sha256_str(value) -> bool:
    return isinstance(value, str) and len(value) == 64 and all(c in "0123456789abcdef" for c in value)


def _check_str_list(value, path: str, *, non_empty: bool) -> None:
    if not isinstance(value, list) or not all(isinstance(x, str) and x for x in value):
        raise ReportError("bad-type", f"{path} must be a list of non-empty strings")
    if non_empty and not value:
        raise ReportError("missing-field", f"{path} must not be empty")


def _check_reviewer(rev) -> None:
    if not isinstance(rev, dict) or set(rev) != _REVIEWER_KEYS:
        raise ReportError("bad-fields", f"reviewer block keys must be {sorted(_REVIEWER_KEYS)}")
    for f in ("profile", "profile_sha256", "model", "reasoning"):
        if not isinstance(rev[f], str):
            raise ReportError("bad-type", f"reviewer.{f} must be a string")
    if rev["capability_tier"] not in model.CAPABILITY_TIERS:
        raise ReportError("bad-value", "reviewer.capability_tier is not a lawful tier")
    if rev["reasoning_floor"] not in model.REASONING_FLOORS:
        raise ReportError("bad-value", "reviewer.reasoning_floor is not a lawful floor")
    if rev["context_mode"] not in model.CONTEXT_MODES:
        raise ReportError("bad-value", "reviewer.context_mode is not a lawful mode")


def _check_adjudication(result: dict, path: str) -> None:
    outcome = result["outcome"]
    rc = result["remediation_class"]
    rtk = result["repair_target_kind"]
    rtids = result["repair_target_ids"]
    if rtids is not None and not (isinstance(rtids, list) and all(isinstance(x, str) for x in rtids)):
        raise ReportError("bad-type", f"{path}.result.repair_target_ids must be a list of ids")
    if rtk is not None and rtk not in model.REVIEW_REPAIR_TARGET_KINDS:
        raise ReportError("bad-value", f"{path}.result.repair_target_kind is not a closed target kind")
    if outcome == "confirmed":
        if rc not in model.ADJUDICATION_REMEDIATION_CLASSES:
            raise ReportError(
                "missing-field",
                f"{path}.result confirmed outcome requires exactly one remediation class",
            )
        if rc == "review-process":
            if not isinstance(rtk, str) or not rtids:
                raise ReportError(
                    "missing-field",
                    f"{path}.result review-process remediation requires repair_target_kind and repair_target_ids",
                )
        elif rtk is not None or rtids:
            raise ReportError(
                "bad-value",
                f"{path}.result candidate-change remediation requires null repair targets",
            )
    else:
        if rc is not None or rtk is not None or rtids:
            raise ReportError(
                "bad-value",
                f"{path}.result outcome {outcome!r} requires all remediation fields null/empty",
            )


def lawful_result_kinds(role: str) -> frozenset:
    """The result kinds a report for `role` may carry (empty for unknown roles)."""
    return RESULT_KINDS.get(role, frozenset())


def lawful_result_outcomes(kind: str) -> tuple:
    """The lawful outcomes for a result kind (empty for unknown kinds)."""
    return _RESULT_OUTCOMES.get(kind, ())


def _check_result(result, path: str, allowed_kinds) -> None:
    if not isinstance(result, dict):
        raise ReportError("bad-type", f"{path}.result must be an object")
    kind = result.get("kind")
    if kind not in allowed_kinds:
        raise ReportError("bad-kind", f"{path}.result.kind {kind!r} is not lawful for this role")
    required = _RESULT_KEYS[kind]
    if set(result) != required:
        raise ReportError("bad-fields", f"{path}.result keys must be {sorted(required)}")
    outcome = result["outcome"]
    if outcome not in _RESULT_OUTCOMES[kind]:
        raise ReportError(
            "bad-value",
            f"{path}.result.outcome {outcome!r} is not lawful for kind {kind!r}",
        )
    _check_str_list(result["evidence_ids"], f"{path}.result.evidence_ids", non_empty=True)
    if kind == "finding-adjudication":
        _check_adjudication(result, path)
    elif kind == "fix-review":
        _check_str_list(result["verified_obligation_ids"], f"{path}.result.verified_obligation_ids", non_empty=False)
    elif kind == "review-repair":
        if not isinstance(result["repair_id"], str) or not result["repair_id"]:
            raise ReportError("bad-type", f"{path}.result.repair_id must be a non-empty string")
        _check_str_list(result["replacement_record_ids"], f"{path}.result.replacement_record_ids", non_empty=False)
    elif kind == "exemption-challenge":
        _check_str_list(
            result["hypothesis_assignment_ids"],
            f"{path}.result.hypothesis_assignment_ids",
            non_empty=False,
        )


def _check_assignments(assignments, role: str) -> None:
    if not isinstance(assignments, list):
        raise ReportError("bad-type", "assignments must be a list")
    allowed_kinds = RESULT_KINDS[role]
    for i, a in enumerate(assignments):
        path = f"assignments[{i}]"
        if not isinstance(a, dict) or set(a) != _ASSIGNMENT_KEYS:
            raise ReportError("bad-fields", f"{path} keys must be {sorted(_ASSIGNMENT_KEYS)}")
        if not isinstance(a["assignment_id"], str) or not a["assignment_id"]:
            raise ReportError("bad-type", f"{path}.assignment_id must be a non-empty string")
        if not isinstance(a["notes"], str):
            raise ReportError("bad-type", f"{path}.notes must be a string")
        _check_result(a["result"], path, allowed_kinds)


def _check_findings(findings) -> None:
    if not isinstance(findings, list):
        raise ReportError("bad-type", "findings must be a list")
    for i, f in enumerate(findings):
        path = f"findings[{i}]"
        if not isinstance(f, dict) or set(f) != _FINDING_KEYS:
            raise ReportError("bad-fields", f"{path} keys must be {sorted(_FINDING_KEYS)}")
        if not isinstance(f["title"], str) or not f["title"]:
            raise ReportError("bad-type", f"{path}.title must be a non-empty string")
        if not isinstance(f["description"], str):
            raise ReportError("bad-type", f"{path}.description must be a string")
        if f["severity"] not in model.SEVERITIES:
            raise ReportError("bad-value", f"{path}.severity is not a lawful severity")
        _check_str_list(f["locations"], f"{path}.locations", non_empty=True)
        _check_str_list(f["evidence_ids"], f"{path}.evidence_ids", non_empty=False)
        if not isinstance(f["source_assignment_id"], str) or not f["source_assignment_id"]:
            raise ReportError("bad-type", f"{path}.source_assignment_id must be a non-empty string")


def _check_structured_output(so, role: str) -> None:
    if role in ("impact-mapper-semantic", "impact-mapper-contract"):
        kind, required = "impact-map", {"kind", "subject_sha256", "record"}
    elif role == "scope-challenger":
        kind = "coverage-inventory"
        required = {
            "kind",
            "subject_sha256",
            "revised_obligations_sha256",
            "record",
            "revised_obligations",
        }
    else:
        kind, required = "assignment-results", {"kind"}
    if not isinstance(so, dict) or set(so) != required:
        raise ReportError(
            "bad-fields",
            f"structured_output for role {role} must have keys {sorted(required)}",
        )
    if so["kind"] != kind:
        raise ReportError("bad-value", f"structured_output.kind must be {kind!r} for role {role}")
    if kind == "assignment-results":
        return
    if not _is_sha256_str(so["subject_sha256"]):
        raise ReportError("bad-type", "structured_output.subject_sha256 must be a sha256 hex string")
    if not isinstance(so["record"], dict):
        raise ReportError("bad-type", "structured_output.record must be an object")
    if kind == "coverage-inventory":
        if not _is_sha256_str(so["revised_obligations_sha256"]):
            raise ReportError("bad-type", "structured_output.revised_obligations_sha256 must be a sha256 hex string")
        if not isinstance(so["revised_obligations"], list) or not all(
            isinstance(o, dict) for o in so["revised_obligations"]
        ):
            raise ReportError("bad-type", "structured_output.revised_obligations must be a list of objects")


def parse_report(raw: bytes, *, role: str) -> dict:
    """Strict-decode and validate one reviewer report."""
    if role not in RESULT_KINDS:
        raise ReportError("unknown-role", f"no report contract for role {role!r}")
    try:
        doc = model.strict_json_loads(bytes(raw), source="report")
    except (TypeError, ValueError) as exc:
        raise ReportError("invalid-json", str(exc)) from exc
    if not isinstance(doc, dict):
        raise ReportError("bad-type", "report is not an object")
    if set(doc) != _TOP_KEYS:
        missing = set(_TOP_KEYS) - set(doc)
        extra = set(doc) - set(_TOP_KEYS)
        raise ReportError("bad-fields", f"report key set diverges: missing={sorted(missing)} extra={sorted(extra)}")
    if doc["schema_version"] != 1:
        raise ReportError("bad-version", "schema_version must be 1")
    for f in ("review_session_id", "attestation_id", "snapshot_fingerprint", "dispatch_id"):
        if not isinstance(doc[f], str):
            raise ReportError("bad-type", f"{f} must be a string")
    if type(doc["snapshot_epoch"]) is not int or doc["snapshot_epoch"] < 1:
        raise ReportError("bad-type", "snapshot_epoch must be a positive int")
    _check_reviewer(doc["reviewer"])
    _check_assignments(doc["assignments"], role)
    _check_findings(doc["findings"])
    if not isinstance(doc["uncertainties"], list) or not all(isinstance(u, str) for u in doc["uncertainties"]):
        raise ReportError("bad-type", "uncertainties must be a list of strings")
    if doc["verdict"] not in model.REVIEW_VERDICTS:
        raise ReportError("bad-value", "verdict is not a lawful review verdict")
    _check_structured_output(doc["structured_output"], role)
    return doc


def derive_verdict(report: dict) -> str:
    """The lawful verdict from a parsed report; never derives "blocked"."""
    outcomes = [a["result"].get("outcome") for a in report["assignments"]]
    if "incomplete" in outcomes:
        return "incomplete"
    if report["findings"] or report["uncertainties"]:
        return "findings"
    if any(o not in _NON_FINDING_OUTCOMES for o in outcomes):
        return "findings"
    return "clean"


def obligation_outcomes(report: dict) -> dict[str, str]:
    """{obligation_id: outcome} from an obligation-reviewer report."""
    out: dict[str, str] = {}
    for a in report["assignments"]:
        r = a["result"]
        if r.get("kind") == "obligation":
            out[a["assignment_id"]] = r["outcome"]
    return out


def verified_obligations(report: dict) -> tuple[str, ...]:
    """verified_obligation_ids from a fix-reviewer report (empty otherwise)."""
    ids: list[str] = []
    for a in report["assignments"]:
        r = a["result"]
        if r.get("kind") == "fix-review":
            ids.extend(r["verified_obligation_ids"])
    return tuple(sorted(set(ids)))


def structured_subject(report: dict) -> dict | None:
    """structured_output for mapper/challenger reports, else None."""
    so = report["structured_output"]
    if isinstance(so, dict) and so.get("kind") in ("impact-map", "coverage-inventory"):
        return so
    return None


def report_to_action_data(action: str, report: dict, *, attestation: dict, findings: list) -> dict:
    """Build the ``data`` payload ``complete_action`` consumes for ``action``.

    The typed claims (structured_output, assignment_results, and for
    exemption challenges the derived exemption_outcome) ride the attestation
    dict for the kernel to pop and verify at install, matching the shipped
    exemption_outcome precedent. The attestation verdict is rewritten to the
    derived lawful verdict.
    """
    att = dict(attestation)
    att["structured_output"] = report["structured_output"]
    att["assignment_results"] = [{"assignment_id": a["assignment_id"], **a["result"]} for a in report["assignments"]]
    att["verdict"] = derive_verdict(report)
    if action in _MAPPER_ACTIONS:
        so = structured_subject(report)
        if so is None or so["kind"] != "impact-map":
            raise ReportError("bad-value", f"{action} requires an impact-map structured_output")
        return {"impact_map": so["record"], "attestation": att, "findings": list(findings)}
    if action == "challenge-coverage":
        so = structured_subject(report)
        if so is None or so["kind"] != "coverage-inventory":
            raise ReportError("bad-value", f"{action} requires a coverage-inventory structured_output")
        return {
            "coverage_inventory": so["record"],
            "revised_obligations": so["revised_obligations"],
            "attestation": att,
            "findings": list(findings),
        }
    if action == "run-exemption-challenge":
        outcomes = {r["outcome"] for r in att["assignment_results"] if r["kind"] == "exemption-challenge"}
        if len(outcomes) != 1:
            raise ReportError(
                "mixed-outcomes",
                "exemption-challenge results must share one outcome per dispatch",
            )
        att["exemption_outcome"] = next(iter(outcomes))
    if action in _PLURAL_ACTIONS:
        return {"attestations": [att], "findings": list(findings)}
    if action in _SINGLE_ACTIONS:
        return {"attestation": att, "findings": list(findings)}
    raise ReportError("unsupported-action", f"{action!r} has no report payload grammar")
