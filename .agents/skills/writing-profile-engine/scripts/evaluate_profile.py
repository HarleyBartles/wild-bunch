from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from datetime import date
from pathlib import Path
from typing import Any

SCRIPT_ROOT = Path(__file__).resolve().parent
if str(SCRIPT_ROOT) not in sys.path:
    sys.path.insert(0, str(SCRIPT_ROOT))

from _profile_common import (  # noqa: E402
    ProfileError,
    load_json,
    source_ids,
    validate_document,
)


SENTENCE_RE = re.compile(r"[^.!?]+[.!?]|[^.!?]+$")
WORD_RE = re.compile(r"\b[\w'-]+\b", re.UNICODE)
Signal = tuple[int, int, str, str]


def _phrase_pattern(phrase: str) -> re.Pattern[str]:
    pieces = [re.escape(piece) for piece in phrase.strip().split()]
    body = r"\s+".join(pieces)
    prefix = r"(?<!\w)" if phrase[:1].isalnum() else ""
    suffix = r"(?!\w)" if phrase[-1:].isalnum() else ""
    return re.compile(prefix + body + suffix, re.IGNORECASE | re.UNICODE)


def _phrase_signals(text: str, phrases: list[str], rule_id: str) -> list[Signal]:
    signals: list[Signal] = []
    for phrase in phrases:
        normalized = " ".join(phrase.casefold().split())
        for match in _phrase_pattern(phrase).finditer(text):
            signals.append((match.start(), match.end(), match.group(0), f"{rule_id}:{normalized}"))
    return sorted(signals, key=lambda item: (item[0], item[1], item[3]))


def _sentences(text: str) -> list[tuple[int, int, str]]:
    return [
        (item.start(), item.end(), item.group(0).strip())
        for item in SENTENCE_RE.finditer(text)
        if item.group(0).strip()
    ]


def _paragraphs(text: str) -> list[tuple[int, int, str]]:
    paragraphs: list[tuple[int, int, str]] = []
    for match in re.finditer(r"\S(?:.*?\S)?(?=\n\s*\n|\Z)", text, re.DOTALL):
        paragraphs.append((match.start(), match.end(), match.group(0)))
    return paragraphs


def _repetition_signals(text: str, rule: dict[str, Any]) -> list[Signal]:
    is_sentence = rule["kind"] == "sentence_repetition"
    items = _sentences(text) if is_sentence else _paragraphs(text)
    minimum = rule["minimum_sentences" if is_sentence else "minimum_paragraphs"]
    if len(items) < minimum:
        return []
    lengths = [len(WORD_RE.findall(item[2])) for item in items]
    opening_words = rule["opening_words"]
    openings = [" ".join(word.casefold() for word in WORD_RE.findall(item[2])[:opening_words]) for item in items]
    checks: list[str] = []
    if max(lengths) - min(lengths) <= rule["maximum_word_count_spread"]:
        checks.append("similar-length")
    if len(set(openings)) < len(openings):
        checks.append("repeated-opening")
    if len(checks) < 2:
        return []
    return [(start, end, value, f"{rule['id']}:{check}") for start, end, value in items for check in checks]


def _voice_value(voice_card: dict[str, Any], field: str) -> Any:
    value: Any = voice_card
    for part in field.split("."):
        if not isinstance(value, dict) or part not in value:
            return None
        value = value[part]
    return value


def _bounded_strings(
    value: Any,
    *,
    maximum: int,
    maximum_length: int,
    allowed: set[str] | None = None,
    required: bool = False,
) -> bool:
    if not isinstance(value, list) or (required and not value) or len(value) > maximum:
        return False
    if not all(
        isinstance(item, str) and 0 < len(item) <= maximum_length and (allowed is None or item in allowed)
        for item in value
    ):
        return False
    return len(value) == len(set(value))


def _exact_object(value: Any, keys: set[str]) -> bool:
    return isinstance(value, dict) and set(value) == keys


def _voice_card_authorized(voice_card: dict[str, Any]) -> bool:
    if not _exact_object(
        voice_card,
        {"schema_version", "profile_id", "version", "scope", "derivation", "tendencies", "choices", "limitations"},
    ):
        return False
    scope = voice_card.get("scope")
    derivation = voice_card.get("derivation")
    tendencies = voice_card.get("tendencies")
    choices = voice_card.get("choices")
    if not _exact_object(scope, {"task_boundary", "genres", "audiences"}) or not _exact_object(
        derivation,
        {"basis", "authorization", "sample_count", "derived_at", "source_retained", "retention_boundary"},
    ):
        return False
    if not _exact_object(
        tendencies,
        {
            "sentence_range",
            "directness",
            "vocabulary_register",
            "tolerated_fragments",
            "rhetorical_devices",
            "formatting_norms",
        },
    ) or not _exact_object(choices, {"prefer", "avoid"}):
        return False
    sentence_range = tendencies["sentence_range"]
    if not _exact_object(sentence_range, {"typical_min_words", "typical_max_words"}):
        return False
    minimum = sentence_range["typical_min_words"]
    maximum = sentence_range["typical_max_words"]
    if not (type(minimum) is int and type(maximum) is int and 1 <= minimum <= maximum <= 100):
        return False
    try:
        date.fromisoformat(derivation["derived_at"])
    except (TypeError, ValueError):
        return False
    if not (
        type(voice_card["schema_version"]) is int
        and voice_card["schema_version"] == 1
        and isinstance(voice_card["profile_id"], str)
        and bool(re.fullmatch(r"[a-z0-9]+(?:-[a-z0-9]+)*", voice_card["profile_id"]))
        and isinstance(voice_card["version"], str)
        and bool(re.fullmatch(r"\d+\.\d+\.\d+", voice_card["version"]))
        and _bounded_strings(scope["genres"], maximum=8, maximum_length=80)
        and _bounded_strings(scope["audiences"], maximum=8, maximum_length=80)
        and type(derivation["sample_count"]) is int
        and 0 <= derivation["sample_count"] <= 100
        and derivation["source_retained"] is False
        and tendencies["directness"] in {"low", "balanced", "high"}
        and _bounded_strings(
            tendencies["vocabulary_register"],
            maximum=5,
            maximum_length=15,
            allowed={"plain", "technical", "formal", "informal", "domain-specific"},
        )
        and type(tendencies["tolerated_fragments"]) is bool
        and _bounded_strings(
            tendencies["rhetorical_devices"],
            maximum=7,
            maximum_length=24,
            allowed={
                "contrast",
                "em-dash",
                "parallelism",
                "refrain",
                "rhetorical-question",
                "triad",
                "understatement",
            },
        )
        and _bounded_strings(tendencies["formatting_norms"], maximum=8, maximum_length=80)
        and _bounded_strings(choices["prefer"], maximum=12, maximum_length=120)
        and _bounded_strings(choices["avoid"], maximum=12, maximum_length=120)
        and _bounded_strings(voice_card["limitations"], maximum=8, maximum_length=180, required=True)
    ):
        return False
    basis = derivation.get("basis")
    authorization = derivation.get("authorization")
    provenance_ok = (basis, authorization) in {
        ("current_task_text", "current_task_user"),
        ("explicit_preferences", "explicit_user_preference"),
    }
    return bool(
        scope.get("task_boundary") == "current_task"
        and provenance_ok
        and ((basis == "current_task_text" and derivation["sample_count"] >= 1) or derivation["sample_count"] == 0)
        and derivation.get("source_retained") is False
        and derivation.get("retention_boundary") == "no_source_storage"
    )


def _voice_rule_signals(
    text: str,
    rule: dict[str, Any],
    voice_card: dict[str, Any] | None,
) -> tuple[list[Signal], bool]:
    if not isinstance(voice_card, dict) or not _voice_card_authorized(voice_card):
        return [], False
    value = _voice_value(voice_card, rule["field"])
    comparison = rule["comparison"]
    if rule["field"] == "tendencies.sentence_range":
        supported = isinstance(value, dict) and {
            "typical_min_words",
            "typical_max_words",
        } <= set(value)
    else:
        values = rule["card_values"]
        supported = value in values if isinstance(value, str) else bool(set(value or []) & set(values))
    if not supported:
        return [], False
    if comparison["kind"] == "normalized_phrase_occurrence":
        return _phrase_signals(text, comparison["phrases"], rule["id"]), True
    minimum = value["typical_min_words"]
    maximum = value["typical_max_words"]
    signals = [
        (start, end, sentence, f"{rule['id']}:outside-range")
        for start, end, sentence in _sentences(text)
        if not minimum <= len(WORD_RE.findall(sentence)) <= maximum
    ]
    return signals, True


def _rule_signals(
    text: str,
    rules: list[dict[str, Any]],
    voice_card: dict[str, Any] | None,
) -> tuple[list[Signal], int, bool]:
    signals: list[Signal] = []
    supported_voice_rules = 0
    has_voice_rules = False
    for rule in rules:
        if rule["kind"] == "normalized_phrase_occurrence":
            signals.extend(_phrase_signals(text, rule["phrases"], rule["id"]))
        elif rule["kind"] in {"sentence_repetition", "paragraph_repetition"}:
            signals.extend(_repetition_signals(text, rule))
        else:
            has_voice_rules = True
            found, supported = _voice_rule_signals(text, rule, voice_card)
            signals.extend(found)
            supported_voice_rules += int(supported)
    unique = sorted(set(signals), key=lambda item: (item[0], item[1], item[3]))
    return unique, supported_voice_rules, has_voice_rules


def _best_threshold_group(text: str, signals: list[Signal], threshold: dict[str, Any]) -> list[Signal]:
    if not signals:
        return []
    unit = threshold["unit"]
    groups: list[list[Signal]] = []
    if unit in {"draft", "section"}:
        groups = [signals]
    elif unit == "paragraph":
        for start, end, _ in _paragraphs(text):
            groups.append([item for item in signals if start <= item[0] < end])
    else:
        word_starts = [match.start() for match in WORD_RE.finditer(text)]

        def word_index(position: int) -> int:
            return sum(start < position for start in word_starts)

        window = threshold["window_words"]
        for signal in signals:
            origin = word_index(signal[0])
            groups.append([item for item in signals if word_index(item[0]) - origin <= window and item[0] >= signal[0]])
    eligible = [
        group
        for group in groups
        if len(group) >= threshold["minimum_count"]
        and len({item[3] for item in group}) >= threshold["minimum_distinct_signals"]
    ]
    return min(eligible, key=lambda group: (group[-1][1] - group[0][0], group[0][0])) if eligible else []


def _predicate_matches(predicate: dict[str, Any], text: str, context: str) -> bool:
    scope = predicate["scope"]
    haystack = text if scope == "text" else context if scope == "context" else f"{text}\n{context}"
    matches = [bool(_phrase_pattern(phrase).search(haystack)) for phrase in predicate["phrases"]]
    return all(matches) if predicate["kind"] == "all_phrases" else any(matches)


def _finding(
    pattern: dict[str, Any],
    finding_type: str,
    text: str,
    matches: list[Signal],
) -> dict[str, Any]:
    if matches:
        start = min(item[0] for item in matches)
        end = max(item[1] for item in matches)
        evidence = text[start:end]
    else:
        start, end, evidence = 0, len(text), text
    return {
        "type": finding_type,
        "pattern_id": pattern["id"],
        "evidence": evidence,
        "span": {"start": start, "end": end},
        "rationale": pattern["rationale"],
        "preserve_when": pattern["preserve_conditions"],
        "repair": pattern["repair_guidance"],
        "confidence": None,
    }


def evaluate_text(
    profile_path: Path,
    text: str,
    *,
    context: str | None,
    voice_card: dict[str, Any] | None,
) -> dict[str, Any]:
    profile_path = Path(profile_path)
    profile = load_json(profile_path)
    errors, validation_warnings = validate_document(profile_path, source_ids(), require_goldens=False)
    if errors:
        raise ProfileError("invalid profile: " + "; ".join(errors))
    raw = text.encode("utf-8")
    warnings = list(validation_warnings)
    findings: list[dict[str, Any]] = []
    context_missing = not context or context.lower().startswith("no audience")
    if not text.strip():
        findings.append(
            {
                "type": "abstain",
                "pattern_id": "profile-context",
                "evidence": "",
                "span": {"start": 0, "end": 0},
                "rationale": "Empty input cannot support a writing-profile judgment.",
                "preserve_when": [],
                "repair": "Supply the text to evaluate.",
                "confidence": None,
            }
        )
    for pattern in profile["patterns"]:
        if pattern["status"] != "active":
            continue
        expired = date.fromisoformat(pattern["review_after"]) < date.today()
        if expired:
            warnings.append(f"{pattern['id']}: review expired; repair downgraded to candidate")
        signals, supported_voice_rules, has_voice_rules = _rule_signals(text, pattern["rules"], voice_card)
        if context_missing:
            if text.strip():
                findings.append(_finding(pattern, "abstain", text, signals))
            continue
        if has_voice_rules and supported_voice_rules == 0:
            if voice_card is not None or signals:
                findings.append(_finding(pattern, "abstain", text, signals))
            continue
        if signals and any(
            _predicate_matches(predicate, text, context or "") for predicate in pattern["preserve_predicates"]
        ):
            findings.append(_finding(pattern, "preserve", text, signals))
            continue
        threshold_matches = _best_threshold_group(text, signals, pattern["contextual_threshold"])
        if threshold_matches:
            findings.append(_finding(pattern, "candidate" if expired else "repair", text, threshold_matches))
    findings.sort(key=lambda item: (item["pattern_id"], item["span"]["start"], item["type"]))
    status = (
        "abstained"
        if findings and all(item["type"] == "abstain" for item in findings)
        else "findings"
        if findings
        else "clear"
    )
    return {
        "schema_version": 1,
        "profile_id": profile["profile_id"],
        "profile_version": profile["version"],
        "input_sha256": hashlib.sha256(raw).hexdigest(),
        "status": status,
        "findings": findings,
        "warnings": sorted(set(warnings)),
    }


def main() -> int:
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description="Evaluate a UTF-8 text file against one writing profile (read-only).")
    parser.add_argument("--profile", type=Path, help="Path to patterns.json")
    parser.add_argument("--input", type=Path, help="UTF-8 text file to inspect")
    parser.add_argument("--voice-card", type=Path, help="Optional bounded voice-card JSON")
    parser.add_argument("--context", type=Path, help="Optional UTF-8 task-context file")
    parser.add_argument("--json", action="store_true", help="Emit machine-readable JSON")
    parser.add_argument("--check", action="store_true", help="Check read-only CLI readiness without input files")
    args = parser.parse_args()
    if args.check and not args.profile and not args.input:
        print("ready: read-only writing profile evaluator")
        return 0
    if not args.profile or not args.input:
        parser.error("--profile and --input are required for evaluation")
    try:
        text = args.input.read_text(encoding="utf-8")
        context = args.context.read_text(encoding="utf-8") if args.context else None
        voice_card = load_json(args.voice_card) if args.voice_card else None
        result = evaluate_text(args.profile, text, context=context, voice_card=voice_card)
    except (OSError, UnicodeError, ProfileError) as exc:
        print(str(exc), file=sys.stderr)
        return 2
    if args.json:
        print(json.dumps(result, ensure_ascii=False, sort_keys=True))
    else:
        print(f"{result['status']}: {len(result['findings'])} finding(s)")
        for finding in result["findings"]:
            print(f"{finding['type']} {finding['pattern_id']}: {finding['evidence']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
