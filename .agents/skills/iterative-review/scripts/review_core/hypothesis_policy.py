#!/usr/bin/env python3
"""Sealed hypothesis-derivation policy (Plan 3 Task 4).

``SealedHypothesisDerivationPolicy`` implements
``policy.HypothesisDerivationPolicy`` from the digest-bound reference document
``references/hypothesis-derivation-policy.v1.json``. For each obligation it
emits, per family the document declares for the obligation's category, one
``claim`` and one ``counterexample`` assignment; high-risk obligations gain the
document's ``high_risk_extra_families``. Statement templates interpolate only
``{surface}``, ``{category}``, and ``{obligation_id}`` - derivation is pure
string interpolation over the canonical document, never a model call. Loading
fails closed on any malformed or incomplete document.
"""

from __future__ import annotations

import re
from pathlib import Path

from . import model, policy

_POLICY_DOC = Path(__file__).resolve().parent.parent.parent / "references" / "hypothesis-derivation-policy.v1.json"

_POLICY_ID = "review-core-hypothesis-derivation"

_TOP_KEYS = frozenset({"schema_version", "policy_id", "version", "families", "high_risk_extra_families"})
_FAMILY_KEYS = frozenset({"name", "claim", "counterexample"})
_PLACEHOLDER_RE = re.compile(r"\{([^{}]*)\}")
_ALLOWED_PLACEHOLDERS = frozenset({"surface", "category", "obligation_id"})


class HypothesisPolicyError(Exception):
    """Raised when the sealed document or a derivation fails closed."""


def _fail(message: str) -> None:
    raise HypothesisPolicyError(message)


def _check_template(template: str, *, source: str, where: str) -> None:
    for match in _PLACEHOLDER_RE.finditer(template):
        if match.group(1) not in _ALLOWED_PLACEHOLDERS:
            _fail(f"{source}: {where} uses disallowed placeholder {{{match.group(1)}}}")
    try:
        template.format(surface="s", category="c", obligation_id="o")
    except (KeyError, IndexError, ValueError) as exc:
        _fail(f"{source}: {where} is not a valid template: {exc}")


def _check_family(entry: object, *, source: str, where: str) -> dict:
    if not isinstance(entry, dict) or set(entry) != _FAMILY_KEYS:
        _fail(f"{source}: {where} must be {{name, claim, counterexample}}")
    for key in _FAMILY_KEYS:
        if not isinstance(entry[key], str) or not entry[key]:
            _fail(f"{source}: {where}.{key} must be a non-empty string")
    _check_template(entry["claim"], source=source, where=f"{where}.claim")
    _check_template(entry["counterexample"], source=source, where=f"{where}.counterexample")
    return entry


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

    families = doc.get("families")
    if not isinstance(families, dict):
        _fail(f"{source}: families must be an object")
    if set(families) != set(model.OBLIGATION_CATEGORIES):
        _fail(f"{source}: families must cover exactly the obligation categories")
    seen_names: set[str] = set()
    for category, entries in families.items():
        if not isinstance(entries, list) or not entries:
            _fail(f"{source}: families[{category!r}] must be a non-empty list")
        for i, entry in enumerate(entries):
            checked = _check_family(entry, source=source, where=f"families[{category!r}][{i}]")
            if checked["name"] in seen_names:
                _fail(f"{source}: duplicate family name {checked['name']!r}")
            seen_names.add(checked["name"])

    extra = doc.get("high_risk_extra_families")
    if not isinstance(extra, list):
        _fail(f"{source}: high_risk_extra_families must be a list")
    for i, entry in enumerate(extra):
        checked = _check_family(entry, source=source, where=f"high_risk_extra_families[{i}]")
        if checked["name"] in seen_names:
            _fail(f"{source}: duplicate family name {checked['name']!r}")
        seen_names.add(checked["name"])
    return doc


class SealedHypothesisDerivationPolicy:
    """``policy.HypothesisDerivationPolicy`` backed by the sealed document."""

    def __init__(self, *, document_bytes: bytes | None = None) -> None:
        raw = document_bytes if document_bytes is not None else _POLICY_DOC.read_bytes()
        try:
            doc = model.strict_json_loads(raw, source=_POLICY_DOC.name)
        except ValueError as exc:
            raise HypothesisPolicyError(f"{_POLICY_DOC.name}: malformed JSON: {exc}") from exc
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

    def derive(self, *, obligation: dict) -> tuple[dict, ...]:
        category = obligation["category"]
        entries = self._doc["families"].get(category)
        if entries is None:
            raise HypothesisPolicyError(f"no hypothesis family for category {category!r}")
        if obligation["risk"] == "high":
            entries = list(entries) + list(self._doc["high_risk_extra_families"])

        # A hypothesis assignment inherits the obligation's effective floor:
        # the computed scope/risk floor raised by any declared override.
        floor_t, floor_r = policy.obligation_floor(
            obligation["scope_level"], obligation["risk"], obligation["consequences"]
        )
        if model.CAPABILITY_TIERS.index(obligation["minimum_capability_tier"]) > model.CAPABILITY_TIERS.index(floor_t):
            floor_t = obligation["minimum_capability_tier"]
        if model.REASONING_FLOORS.index(obligation["minimum_reasoning_floor"]) > model.REASONING_FLOORS.index(floor_r):
            floor_r = obligation["minimum_reasoning_floor"]

        surface = ", ".join(sorted(obligation["surfaces"]))
        out = []
        for family in entries:
            for polarity in model.HYPOTHESIS_POLARITIES:
                out.append(
                    {
                        "family": family["name"],
                        "polarity": polarity,
                        "statement": family[polarity].format(
                            surface=surface,
                            category=category,
                            obligation_id=obligation["obligation_id"],
                        ),
                        "derivation_policy_sha256": self._sha256,
                        "minimum_capability_tier": floor_t,
                        "minimum_reasoning_floor": floor_r,
                    }
                )
        return tuple(out)
