#!/usr/bin/env python3
"""Typed contracts for consumer repository surfaces."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Literal


Severity = Literal["warning", "failure"]


@dataclass(frozen=True)
class Finding:
    severity: Severity
    code: str
    surface: str
    message: str
    repair: str


Presence = Literal["required", "optional", "forbidden"]
Ownership = Literal["consumer-authored", "consumer-generated"]
ApplyMode = Literal["create", "create-or-migrate", "manual-remediation", "delegate-generator"]
ForceReset = Literal["confirmed-template-restore", "unavailable"]

REGISTERED_VALIDATORS = frozenset(
    {
        "agents-router-contract",
        "command-declaration",
        "completed-artifacts-contract-v1",
        "contributing-contract",
        "file-exists",
        "gitignore-contract",
        "hook-contract",
        "marketplace-json-contract",
        "markdown-formatting-contract",
        "operating-model-contract",
        "must-be-absent",
        "optional-agents-router-contract",
        "playbook-set-contract",
        "repo-runbook-policy-contract",
        "review-contract",
        "runbook-set-contract",
        "shared-checkout-contract",
        "submodule-contract",
    }
)

_REQUIRED_FIELDS = frozenset({"id", "path", "presence", "ownership", "validator", "apply", "force_reset"})
_OPTIONAL_FIELDS = frozenset({"seed", "scaffold", "required_with"})
_ALLOWED_FIELDS = _REQUIRED_FIELDS | _OPTIONAL_FIELDS
_PRESENCE = frozenset({"required", "optional", "forbidden"})
_OWNERSHIP = frozenset({"consumer-authored", "consumer-generated"})
_APPLY = frozenset({"create", "create-or-migrate", "manual-remediation", "delegate-generator"})
_FORCE_RESET = frozenset({"confirmed-template-restore", "unavailable"})


@dataclass(frozen=True)
class SurfaceContract:
    id: str
    path: str
    presence: Presence
    ownership: Ownership
    validator: str
    apply: ApplyMode
    force_reset: ForceReset
    seed: str | None = None
    scaffold: str | None = None
    required_with: str | None = None


@dataclass(frozen=True)
class ManifestContract:
    version: int
    surfaces: tuple[SurfaceContract, ...]


def _required_string(raw: dict[str, object], field: str, index: int) -> str:
    value = raw.get(field)
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f"surface[{index}] requires non-empty {field}")
    return value


def _optional_string(raw: dict[str, object], field: str, index: int) -> str | None:
    value = raw.get(field)
    if value is None:
        return None
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f"surface[{index}] {field} must be null or a non-empty string")
    return value


def _relative_path(value: str, field: str, index: int) -> str:
    normalized = value.replace("\\", "/")
    path = Path(normalized)
    if path.is_absolute() or normalized.startswith("/") or ".." in path.parts or normalized in {"", "."}:
        raise ValueError(f"surface[{index}] {field} must be a repository-relative path without '..'")
    return normalized


def _parse_surface(raw: object, index: int) -> SurfaceContract:
    if not isinstance(raw, dict):
        raise ValueError(f"surface[{index}] must be an object")
    unknown = sorted(set(raw) - _ALLOWED_FIELDS)
    if unknown:
        raise ValueError(f"surface[{index}] has unknown field(s): {', '.join(unknown)}")
    missing = sorted(_REQUIRED_FIELDS - set(raw))
    if missing:
        raise ValueError(f"surface[{index}] missing required field(s): {', '.join(missing)}")

    presence = _required_string(raw, "presence", index)
    ownership = _required_string(raw, "ownership", index)
    validator = _required_string(raw, "validator", index)
    apply = _required_string(raw, "apply", index)
    force_reset = _required_string(raw, "force_reset", index)
    if ownership == "consumer-authored" and validator == "identity":
        raise ValueError("consumer-authored surfaces cannot use identity validation")
    if ownership == "consumer-authored" and apply == "overwrite":
        raise ValueError("consumer-authored surfaces cannot use unconditional overwrite apply")
    for field, value, allowed in (
        ("presence", presence, _PRESENCE),
        ("ownership", ownership, _OWNERSHIP),
        ("apply", apply, _APPLY),
        ("force_reset", force_reset, _FORCE_RESET),
    ):
        if value not in allowed:
            raise ValueError(f"surface[{index}] {field} has unsupported value: {value}")
    if validator not in REGISTERED_VALIDATORS:
        raise ValueError(f"surface[{index}] uses unregistered validator: {validator}")
    if presence == "forbidden" and apply != "manual-remediation":
        raise ValueError("forbidden surfaces require manual-remediation apply")

    seed = _optional_string(raw, "seed", index)
    scaffold = _optional_string(raw, "scaffold", index)
    required_with = _optional_string(raw, "required_with", index)
    surface_path = _relative_path(_required_string(raw, "path", index), "path", index)
    if seed is not None:
        seed = _relative_path(seed, "seed", index)
    if scaffold is not None:
        scaffold = _relative_path(scaffold, "scaffold", index)
    if force_reset == "confirmed-template-restore" and seed is None:
        raise ValueError("confirmed-template-restore requires a seed")

    return SurfaceContract(
        id=_required_string(raw, "id", index),
        path=surface_path,
        presence=presence,  # type: ignore[arg-type]
        ownership=ownership,  # type: ignore[arg-type]
        validator=validator,
        apply=apply,  # type: ignore[arg-type]
        force_reset=force_reset,  # type: ignore[arg-type]
        seed=seed,
        scaffold=scaffold,
        required_with=required_with,
    )


def load_manifest(path: Path) -> ManifestContract:
    """Load and strictly validate a repository-shape manifest."""

    raw = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(raw, dict):
        raise ValueError("manifest must be an object")
    unknown = sorted(set(raw) - {"version", "surfaces"})
    if unknown:
        raise ValueError(f"manifest has unknown field(s): {', '.join(unknown)}")
    if raw.get("version") != 3:
        raise ValueError("manifest version must be 3")
    surfaces_raw = raw.get("surfaces")
    if not isinstance(surfaces_raw, list):
        raise ValueError("manifest surfaces must be a list")
    surfaces = tuple(_parse_surface(item, index) for index, item in enumerate(surfaces_raw))
    ids = [surface.id for surface in surfaces]
    if len(ids) != len(set(ids)):
        raise ValueError("manifest surface ids must be unique")
    known = set(ids)
    for surface in surfaces:
        if surface.required_with is not None and surface.required_with not in known:
            raise ValueError(f"surface {surface.id} requires unknown surface {surface.required_with}")
    return ManifestContract(version=3, surfaces=surfaces)
