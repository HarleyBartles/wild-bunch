"""Shared read-only discovery and validation helpers for writing profiles."""

from __future__ import annotations

import json
import os
import re
from datetime import date
from pathlib import Path
from typing import Any


ENGINE_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_ROOT = ENGINE_ROOT.parent / "writing-style" / "references" / "profiles"
SOURCE_AUTHORITY = ENGINE_ROOT / "references" / "source-authority.json"
PROFILE_ID = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
SEMVER = re.compile(r"^\d+\.\d+\.\d+$")
EVIDENCE_CLASSES = {
    "well_supported_reader_fatigue",
    "plausible_emerging",
    "author_specific_preference",
    "weak_or_folk_heuristic",
}
PATTERN_STATUSES = {"active", "retired", "rejected"}
THRESHOLD_UNITS = {"draft", "section", "paragraph", "local_cluster"}
RULE_KINDS = {
    "normalized_phrase_occurrence",
    "sentence_repetition",
    "paragraph_repetition",
    "voice_card_comparison",
}
PREDICATE_KINDS = {"all_phrases", "any_phrase"}
VOICE_FIELDS = {
    "tendencies.directness",
    "tendencies.vocabulary_register",
    "tendencies.sentence_range",
}


class ProfileError(ValueError):
    """An actionable profile discovery, parsing, or validation error."""


def profiles_root(requested: Path | None) -> Path:
    root = (requested or DEFAULT_ROOT).resolve()
    if root.name == "profiles" and root.parent.name == "references":
        return root
    candidate = root / "references" / "profiles"
    if candidate.is_dir():
        return candidate.resolve()
    raise ProfileError(f"lawful references/profiles root not found: {root}")


def candidate_paths(requested: Path | None) -> list[Path]:
    root = profiles_root(requested)
    found: list[Path] = []
    for directory, names, files in os.walk(root, followlinks=False):
        current = Path(directory)
        for name in list(names) + list(files):
            entry = current / name
            if entry.is_symlink():
                target = entry.resolve()
                try:
                    target.relative_to(root)
                except ValueError as exc:
                    raise ProfileError(f"symlink escape rejected: {entry} -> {target}") from exc
        if "patterns.json" in files:
            path = (current / "patterns.json").resolve()
            try:
                path.relative_to(root)
            except ValueError as exc:
                raise ProfileError(f"profile path escapes references/profiles: {path}") from exc
            found.append(path)
    return sorted(found, key=lambda path: str(path).replace("\\", "/").casefold())


def discover(requested: Path | None) -> list[dict[str, Any]]:
    found: list[dict[str, Any]] = []
    for path in candidate_paths(requested):
        try:
            document = load_json(path)
        except ProfileError as exc:
            found.append(
                {
                    "id": None,
                    "version": None,
                    "kind": None,
                    "path": str(path),
                    "status": "invalid",
                    "error": str(exc),
                }
            )
            continue
        required = {"profile_id", "profile_kind", "version", "patterns"}
        missing = sorted(required - set(document))
        found.append(
            {
                "id": document.get("profile_id"),
                "version": document.get("version"),
                "kind": document.get("profile_kind"),
                "path": str(path),
                "status": "candidate" if not missing else "invalid",
                **({"error": f"missing discovery fields {missing}"} if missing else {}),
            }
        )
    return found


def load_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise ProfileError(f"{path}: invalid UTF-8 JSON: {exc}") from exc
    if not isinstance(value, dict):
        raise ProfileError(f"{path}: expected a JSON object")
    return value


def _walk(value: Any, path: str = "$"):
    if isinstance(value, dict):
        for key, child in value.items():
            yield f"{path}.{key}", key, child
            yield from _walk(child, f"{path}.{key}")
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield from _walk(child, f"{path}[{index}]")


def unsafe_fields(value: Any) -> list[str]:
    errors: list[str] = []
    forbidden = (
        "detector",
        "ai_likelihood",
        "ai_probability",
        "authorship_verdict",
        "authorship_score",
        "forbidden_words",
        "banned_words",
        "never_use_tokens",
    )
    for path, key, child in _walk(value):
        normalized = re.sub(r"(?<!^)(?=[A-Z])", "_", key).replace("-", "_").lower()
        if any(term in normalized for term in forbidden):
            errors.append(f"{path}: prohibited detector, authorship, or universal-token semantics")
        if isinstance(child, str):
            lower = child.lower()
            if re.search(r"\b(always|never|every)\b.*\b(remove|delete|ban|forbid|omit|use)\b", lower):
                errors.append(f"{path}: prohibited universal token restriction")
    return errors


def _required_strings(value: Any, prefix: str, *, allow_empty: bool = False) -> list[str]:
    if not isinstance(value, list) or (not value and not allow_empty):
        return [f"{prefix}: expected {'an array' if allow_empty else 'a non-empty array'} of strings"]
    if any(not isinstance(item, str) or not item.strip() for item in value):
        return [f"{prefix}: expected non-empty strings"]
    if len(value) != len(set(value)):
        return [f"{prefix}: duplicate values are not allowed"]
    return []


def _string_set(value: Any) -> set[str]:
    if not isinstance(value, list):
        return set()
    return {item for item in value if isinstance(item, str)}


def _unexpected_fields(value: dict[str, Any], allowed: set[str], prefix: str) -> list[str]:
    unexpected = sorted(set(value) - allowed)
    return [f"{prefix}: unexpected fields {unexpected}"] if unexpected else []


def _is_integer(value: Any, minimum: int) -> bool:
    return type(value) is int and value >= minimum


def _validate_rule(rule: Any, prefix: str) -> list[str]:
    if not isinstance(rule, dict):
        return [f"{prefix}: expected object"]
    errors: list[str] = []
    if not isinstance(rule.get("id"), str) or not PROFILE_ID.fullmatch(rule["id"]):
        errors.append(f"{prefix}.id: expected stable lowercase-hyphenated ID")
    kind = rule.get("kind")
    if kind not in RULE_KINDS:
        return errors + [f"{prefix}.kind: unsupported value {kind!r}"]
    if kind == "normalized_phrase_occurrence":
        errors.extend(_unexpected_fields(rule, {"id", "kind", "phrases"}, prefix))
        errors.extend(_required_strings(rule.get("phrases"), f"{prefix}.phrases"))
    elif kind in {"sentence_repetition", "paragraph_repetition"}:
        count_field = "minimum_sentences" if kind == "sentence_repetition" else "minimum_paragraphs"
        errors.extend(
            _unexpected_fields(
                rule,
                {"id", "kind", count_field, "opening_words", "maximum_word_count_spread"},
                prefix,
            )
        )
        if not _is_integer(rule.get(count_field), 2):
            errors.append(f"{prefix}.{count_field}: expected integer >= 2")
        if not _is_integer(rule.get("opening_words"), 1):
            errors.append(f"{prefix}.opening_words: expected integer >= 1")
        spread = rule.get("maximum_word_count_spread")
        if not _is_integer(spread, 0):
            errors.append(f"{prefix}.maximum_word_count_spread: expected integer >= 0")
    else:
        field = rule.get("field")
        allowed = {"id", "kind", "field", "comparison"}
        if field != "tendencies.sentence_range":
            allowed.add("card_values")
        errors.extend(_unexpected_fields(rule, allowed, prefix))
        if field not in VOICE_FIELDS:
            errors.append(f"{prefix}.field: unsupported bounded voice-card field")
        comparison = rule.get("comparison")
        if not isinstance(comparison, dict):
            errors.append(f"{prefix}.comparison: expected object")
        elif field == "tendencies.sentence_range":
            errors.extend(_unexpected_fields(comparison, {"kind"}, f"{prefix}.comparison"))
            if "card_values" in rule:
                errors.append(f"{prefix}.card_values: not used for a declared sentence range")
            if comparison.get("kind") != "sentence_length_outside_range":
                errors.append(f"{prefix}.comparison.kind: sentence range requires sentence_length_outside_range")
        elif comparison.get("kind") == "normalized_phrase_occurrence":
            errors.extend(_unexpected_fields(comparison, {"kind", "phrases"}, f"{prefix}.comparison"))
            errors.extend(_required_strings(rule.get("card_values"), f"{prefix}.card_values"))
            errors.extend(_required_strings(comparison.get("phrases"), f"{prefix}.comparison.phrases"))
        else:
            errors.append(f"{prefix}.comparison.kind: unsupported value {comparison.get('kind')!r}")
    return errors


def _validate_predicate(predicate: Any, prefix: str) -> list[str]:
    if not isinstance(predicate, dict):
        return [f"{prefix}: expected object"]
    errors: list[str] = []
    errors.extend(_unexpected_fields(predicate, {"id", "kind", "scope", "phrases"}, prefix))
    if not isinstance(predicate.get("id"), str) or not PROFILE_ID.fullmatch(predicate["id"]):
        errors.append(f"{prefix}.id: expected stable lowercase-hyphenated ID")
    if predicate.get("kind") not in PREDICATE_KINDS:
        errors.append(f"{prefix}.kind: unsupported value {predicate.get('kind')!r}")
    if predicate.get("scope") not in {"text", "context", "text_and_context"}:
        errors.append(f"{prefix}.scope: unsupported value {predicate.get('scope')!r}")
    errors.extend(_required_strings(predicate.get("phrases"), f"{prefix}.phrases"))
    return errors


def _validate_pattern(pattern: Any, prefix: str, known_sources: set[str]) -> list[str]:
    if not isinstance(pattern, dict):
        return [f"{prefix}: expected object"]
    required = {
        "id",
        "family",
        "rationale",
        "observable_signals",
        "evidence_class",
        "contextual_threshold",
        "preserve_conditions",
        "preserve_predicates",
        "repair_guidance",
        "rules",
        "source_ids",
        "scope",
        "limitations",
        "version",
        "status",
        "first_observed",
        "reviewed_at",
        "review_after",
        "golden_case_ids",
    }
    errors: list[str] = []
    missing = sorted(required - set(pattern))
    if missing:
        errors.append(f"{prefix}: missing required fields {missing}")
    errors.extend(_unexpected_fields(pattern, required, prefix))
    pattern_id = pattern.get("id")
    if not isinstance(pattern_id, str) or not PROFILE_ID.fullmatch(pattern_id):
        errors.append(f"{prefix}.id: expected stable lowercase-hyphenated ID")
    if not isinstance(pattern.get("family"), str) or not pattern.get("family"):
        errors.append(f"{prefix}.family: expected non-empty string")
    if pattern.get("evidence_class") not in EVIDENCE_CLASSES:
        errors.append(f"{prefix}.evidence_class: unsupported value")
    if pattern.get("status") not in PATTERN_STATUSES:
        errors.append(f"{prefix}.status: unsupported value {pattern.get('status')!r}")
    if not isinstance(pattern.get("version"), str) or not SEMVER.fullmatch(pattern["version"]):
        errors.append(f"{prefix}.version: expected semantic version")
    for field in ("rationale", "repair_guidance", "scope", "limitations"):
        if not isinstance(pattern.get(field), str) or not pattern.get(field, "").strip():
            errors.append(f"{prefix}.{field}: expected non-empty string")
    errors.extend(_required_strings(pattern.get("observable_signals"), f"{prefix}.observable_signals"))
    errors.extend(_required_strings(pattern.get("preserve_conditions"), f"{prefix}.preserve_conditions"))
    errors.extend(_required_strings(pattern.get("source_ids"), f"{prefix}.source_ids"))
    errors.extend(_required_strings(pattern.get("golden_case_ids"), f"{prefix}.golden_case_ids"))
    unknown_sources = sorted(_string_set(pattern.get("source_ids")) - known_sources)
    if unknown_sources:
        errors.append(f"{prefix}.source_ids: unknown IDs {unknown_sources}")
    threshold = pattern.get("contextual_threshold")
    if not isinstance(threshold, dict):
        errors.append(f"{prefix}.contextual_threshold: expected object")
    else:
        threshold_allowed = {"unit", "minimum_count", "minimum_distinct_signals", "decision_rule"}
        if threshold.get("unit") == "local_cluster":
            threshold_allowed.add("window_words")
        errors.extend(_unexpected_fields(threshold, threshold_allowed, f"{prefix}.contextual_threshold"))
        if threshold.get("unit") not in THRESHOLD_UNITS:
            errors.append(f"{prefix}.contextual_threshold.unit: unsupported value")
        for field in ("minimum_count", "minimum_distinct_signals"):
            if not _is_integer(threshold.get(field), 1):
                errors.append(f"{prefix}.contextual_threshold.{field}: expected integer >= 1")
        if threshold.get("unit") == "local_cluster" and (not _is_integer(threshold.get("window_words"), 1)):
            errors.append(f"{prefix}.contextual_threshold.window_words: expected integer >= 1")
        if not isinstance(threshold.get("decision_rule"), str) or not threshold.get("decision_rule", "").strip():
            errors.append(f"{prefix}.contextual_threshold.decision_rule: expected non-empty string")
    rules = pattern.get("rules")
    if not isinstance(rules, list) or not rules:
        errors.append(f"{prefix}.rules: expected non-empty array")
    else:
        rule_ids: set[str] = set()
        for index, rule in enumerate(rules):
            errors.extend(_validate_rule(rule, f"{prefix}.rules[{index}]"))
            if isinstance(rule, dict) and isinstance(rule.get("id"), str):
                if rule["id"] in rule_ids:
                    errors.append(f"{prefix}.rules[{index}].id: duplicate {rule['id']}")
                rule_ids.add(rule["id"])
    predicates = pattern.get("preserve_predicates")
    if not isinstance(predicates, list):
        errors.append(f"{prefix}.preserve_predicates: expected array")
    else:
        for index, predicate in enumerate(predicates):
            errors.extend(_validate_predicate(predicate, f"{prefix}.preserve_predicates[{index}]"))
    dates: dict[str, date] = {}
    for field in ("first_observed", "reviewed_at", "review_after"):
        try:
            dates[field] = date.fromisoformat(pattern[field])
        except (KeyError, TypeError, ValueError):
            errors.append(f"{prefix}.{field}: expected ISO date")
    if {"first_observed", "reviewed_at", "review_after"} <= set(dates):
        if dates["first_observed"] > dates["reviewed_at"]:
            errors.append(f"{prefix}.first_observed: must not follow reviewed_at")
        if dates["reviewed_at"] >= dates["review_after"]:
            errors.append(f"{prefix}.review_after: must follow reviewed_at")
    return errors


def _validate_goldens(
    path: Path,
    pattern_ids: set[str],
    profile_id: str,
    profile_version: str,
) -> tuple[list[str], set[str]]:
    errors: list[str] = []
    coverage: set[tuple[str, str]] = set()
    case_ids: set[str] = set()
    try:
        golden = load_json(path)
    except ProfileError as exc:
        return [str(exc)], set()
    required_top = {"schema_version", "fatigue_profile_id", "profile_version", "fixture_provenance", "cases"}
    missing_top = sorted(required_top - set(golden))
    if missing_top:
        errors.append(f"{path}: missing required fields {missing_top}")
    errors.extend(_unexpected_fields(golden, required_top, str(path)))
    if type(golden.get("schema_version")) is not int or golden.get("schema_version") != 1:
        errors.append(f"{path}.schema_version: expected 1")
    if golden.get("fatigue_profile_id") != profile_id:
        errors.append(f"{path}.fatigue_profile_id: expected {profile_id!r}")
    if golden.get("profile_version") != profile_version:
        errors.append(f"{path}.profile_version: expected {profile_version!r}")
    if not isinstance(golden.get("fixture_provenance"), str) or not golden.get("fixture_provenance", "").strip():
        errors.append(f"{path}.fixture_provenance: expected non-empty string")
    cases = golden.get("cases")
    if not isinstance(cases, list) or not cases:
        return [f"{path}.cases: expected non-empty array"], set()
    for index, case in enumerate(cases):
        prefix = f"{path}.cases[{index}]"
        if not isinstance(case, dict):
            errors.append(f"{prefix}: expected object")
            continue
        required = {
            "id",
            "input",
            "context",
            "tags",
            "expected_classification",
            "applicable_pattern_ids",
            "expected_findings",
            "rationale",
            "expected_repair_principle",
        }
        missing = sorted(required - set(case))
        if missing:
            errors.append(f"{prefix}: missing required fields {missing}")
            continue
        errors.extend(_unexpected_fields(case, required | {"voice_card"}, prefix))
        for field in ("input", "context", "rationale", "expected_repair_principle"):
            if not isinstance(case.get(field), str):
                errors.append(f"{prefix}.{field}: expected string")
        errors.extend(_required_strings(case.get("tags"), f"{prefix}.tags"))
        if "voice_card" in case and not isinstance(case["voice_card"], dict):
            errors.append(f"{prefix}.voice_card: expected object")
        case_id = case.get("id")
        if not isinstance(case_id, str) or not PROFILE_ID.fullmatch(case_id):
            errors.append(f"{prefix}.id: expected stable lowercase-hyphenated ID")
        elif case_id in case_ids:
            errors.append(f"{prefix}.id: duplicate {case_id}")
        else:
            case_ids.add(case_id)
        classification = case.get("expected_classification")
        if classification not in {"repair", "preserve", "abstain"}:
            errors.append(f"{prefix}.expected_classification: unsupported value")
        applicable = case.get("applicable_pattern_ids")
        if not isinstance(applicable, list) or not applicable:
            errors.append(f"{prefix}.applicable_pattern_ids: expected non-empty array")
            applicable = []
        else:
            errors.extend(_required_strings(applicable, f"{prefix}.applicable_pattern_ids"))
        applicable_ids = _string_set(applicable)
        unknown = sorted(applicable_ids - pattern_ids)
        if unknown:
            errors.append(f"{prefix}.applicable_pattern_ids: unknown IDs {unknown}")
        for pattern_id in applicable_ids & pattern_ids:
            if classification in {"repair", "preserve", "abstain"}:
                coverage.add((pattern_id, classification))
        findings = case.get("expected_findings")
        if not isinstance(findings, list) or not findings:
            errors.append(f"{prefix}.expected_findings: expected non-empty array")
            continue
        for finding_index, finding in enumerate(findings):
            finding_prefix = f"{prefix}.expected_findings[{finding_index}]"
            if not isinstance(finding, dict):
                errors.append(f"{finding_prefix}: expected object")
                continue
            required_finding = {"type", "pattern_ids", "evidence", "rationale"}
            missing_finding = sorted(required_finding - set(finding))
            if missing_finding:
                errors.append(f"{finding_prefix}: missing required fields {missing_finding}")
            errors.extend(_unexpected_fields(finding, required_finding, finding_prefix))
            if finding.get("type") not in {"repair", "preserve", "abstain"}:
                errors.append(f"{finding_prefix}.type: unsupported value")
            if finding.get("type") != classification:
                errors.append(f"{finding_prefix}.type: must match expected_classification")
            ids = finding.get("pattern_ids")
            if not isinstance(ids, list) or not ids:
                errors.append(f"{finding_prefix}.pattern_ids: expected non-empty array")
            else:
                errors.extend(_required_strings(ids, f"{finding_prefix}.pattern_ids"))
                finding_ids = _string_set(ids)
                unknown = sorted(finding_ids - pattern_ids)
                if unknown:
                    errors.append(f"{finding_prefix}.pattern_ids: unknown IDs {unknown}")
                outside_case = sorted(finding_ids - applicable_ids)
                if outside_case:
                    errors.append(
                        f"{finding_prefix}.pattern_ids: IDs {outside_case} must be listed in applicable_pattern_ids"
                    )
    missing_coverage = sorted(
        f"{pattern_id}:{kind}"
        for pattern_id in pattern_ids
        for kind in ("repair", "preserve", "abstain")
        if (pattern_id, kind) not in coverage
    )
    if missing_coverage:
        errors.append(f"{path}: missing golden coverage {missing_coverage}")
    return errors, case_ids


def validate_document(
    path: Path,
    known_sources: set[str],
    *,
    require_goldens: bool = True,
) -> tuple[list[str], list[str]]:
    errors: list[str] = []
    warnings: list[str] = []
    try:
        document = load_json(path)
    except ProfileError as exc:
        return [str(exc)], warnings
    required = {"schema_version", "profile_id", "profile_kind", "version", "reviewed_at", "review_after", "patterns"}
    missing = sorted(required - set(document))
    if missing:
        return [f"{path}: missing required fields {missing}"], warnings
    errors.extend(_unexpected_fields(document, required, str(path)))
    if type(document["schema_version"]) is not int or document["schema_version"] != 1:
        errors.append(f"{path}.schema_version: expected 1")
    if not isinstance(document["profile_id"], str) or not PROFILE_ID.fullmatch(document["profile_id"]):
        errors.append(f"{path}.profile_id: expected stable lowercase-hyphenated ID")
    if document["profile_kind"] not in {"fatigue", "voice"}:
        errors.append(f"{path}.profile_kind: unsupported kind {document['profile_kind']!r}")
    if not isinstance(document["version"], str) or not SEMVER.fullmatch(document["version"]):
        errors.append(f"{path}.version: expected semantic version")
    try:
        reviewed = date.fromisoformat(document["reviewed_at"])
        review_after = date.fromisoformat(document["review_after"])
        if reviewed >= review_after:
            errors.append(f"{path}.review_after: must follow reviewed_at")
        if review_after < date.today():
            warnings.append(f"{path}.review_after: expired; recommendations downgrade to candidate")
    except (TypeError, ValueError):
        errors.append(f"{path}: reviewed_at and review_after must be ISO dates")
    patterns = document.get("patterns")
    if not isinstance(patterns, list) or not patterns:
        return errors + [f"{path}.patterns: expected non-empty array"], warnings
    ids: set[str] = set()
    for index, pattern in enumerate(patterns):
        prefix = f"{path}.patterns[{index}]"
        errors.extend(_validate_pattern(pattern, prefix, known_sources))
        if isinstance(pattern, dict) and isinstance(pattern.get("id"), str):
            if pattern["id"] in ids:
                errors.append(f"{prefix}.id: duplicate {pattern['id']}")
            ids.add(pattern["id"])
    errors.extend(f"{path}{item[1:]}" for item in unsafe_fields(document))
    if require_goldens:
        golden_path = path.with_name("goldens.json")
        if not golden_path.is_file():
            errors.append(f"{golden_path}: required golden file is missing")
        else:
            golden_errors, golden_ids = _validate_goldens(
                golden_path,
                ids,
                document["profile_id"],
                document["version"],
            )
            errors.extend(golden_errors)
            for index, pattern in enumerate(patterns):
                if not isinstance(pattern, dict):
                    continue
                unknown = sorted(_string_set(pattern.get("golden_case_ids")) - golden_ids)
                if unknown:
                    errors.append(f"{path}.patterns[{index}].golden_case_ids: unknown IDs {unknown}")
    return errors, warnings


def source_ids() -> set[str]:
    register = load_json(SOURCE_AUTHORITY)
    sources = register.get("sources")
    if not isinstance(sources, list):
        raise ProfileError(f"{SOURCE_AUTHORITY}.sources: expected array")
    ids = {item.get("id") for item in sources if isinstance(item, dict)}
    if None in ids or any(not isinstance(item, str) or not PROFILE_ID.fullmatch(item) for item in ids):
        raise ProfileError(f"{SOURCE_AUTHORITY}.sources: every source requires a stable ID")
    return ids
