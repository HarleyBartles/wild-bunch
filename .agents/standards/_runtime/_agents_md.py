"""Shared helpers for router AGENTS.md validation."""

from __future__ import annotations

import re
from pathlib import Path


HEADING_PATTERN = re.compile(r"^#{1,2}\s+(.+)$", re.MULTILINE)
LINK_PATTERN = re.compile(r"\[([^\]]+)\]\(([^)]+)\)")

CORE_SECTIONS = (
    "Repository purpose",
    "Source-of-truth split",
    "Build and test commands",
    "Routing pointers",
    "Maintenance responsibility",
)

CANONICAL_TOPICS = {
    "Repository purpose": {"repository purpose"},
    "Source-of-truth split": {"source of truth split", "source-of-truth split"},
    "Publication proof": {"publication proof"},
    "Build and test commands": {"build and test commands", "build-and-test-commands"},
    "Testing instructions": {"testing instructions", "testing guide", "testing"},
    "Code style guidelines": {"code style guidelines", "code style guide", "code style"},
    "Review guidelines": {"review guidelines", "review guide", "review"},
    "PR instructions": {"pr instructions", "pr guide", "pull request instructions"},
    "Contributing": {"contributing", "contributing guide"},
    "Security considerations": {"security considerations", "security guide", "security"},
    "Routing pointers": {"routing pointers"},
    "Maintenance responsibility": {"maintenance responsibility"},
}


def _normalize_heading(text: str) -> str:
    return re.sub(r"[^a-z0-9]+", " ", text.strip().lower()).strip()


def _heading_set(text: str) -> set[str]:
    return {_normalize_heading(m.group(1)) for m in HEADING_PATTERN.finditer(text)}


def _extract_section(text: str, section_title: str) -> str:
    pattern = re.compile(
        rf"^##\s+{re.escape(section_title)}\s*\n(.*?)(?=\n##\s|\Z)",
        re.DOTALL | re.MULTILINE | re.IGNORECASE,
    )
    match = pattern.search(text)
    return match.group(1) if match else ""


def _resolve_link(current_file: Path, raw_target: str, repo_root: Path) -> Path | None:
    if raw_target.startswith(("http://", "https://", "mailto:")):
        return None
    clean = raw_target.split("#", 1)[0]
    if not clean:
        return None
    clean = clean.lstrip("/")
    for base in (current_file.parent, repo_root):
        try:
            resolved = (base / clean).resolve()
        except (OSError, ValueError):
            continue
        try:
            resolved.relative_to(repo_root)
        except ValueError:
            continue
        if resolved.is_file():
            return resolved
    return None


def _topic_coverage(headings: set[str]) -> set[str]:
    covered: set[str] = set()
    for topic, aliases in CANONICAL_TOPICS.items():
        normalized_aliases = {_normalize_heading(alias) for alias in aliases}
        if any(any(alias in heading for heading in headings) for alias in normalized_aliases):
            covered.add(topic)
    return covered


def validate_agents_md(agents_path: Path, repo_root: Path) -> list[str]:
    """Return a list of DRIFT messages for the root AGENTS.md file."""
    findings: list[str] = []

    if not agents_path.is_file():
        findings.append("AGENTS.md missing")
        return findings

    try:
        text = agents_path.read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError) as exc:
        findings.append(f"cannot read AGENTS.md: {exc}")
        return findings

    headings = _heading_set(text)
    for section in CORE_SECTIONS:
        if _normalize_heading(section) not in headings:
            findings.append(f"AGENTS.md missing core section: {section}")

    routing_section = _extract_section(text, "Routing pointers")
    if not routing_section.strip():
        findings.append("AGENTS.md missing Routing pointers section body")
    else:
        links = LINK_PATTERN.findall(routing_section)
        if not links:
            findings.append("AGENTS.md Routing pointers section has no links")
        seen: set[Path] = set()
        linked_paths: set[str] = set()
        routed_headings: set[str] = set()
        for label, raw_target in links:
            clean_target = raw_target.split("#", 1)[0].lstrip("/").replace("\\", "/")
            linked_paths.add(clean_target)
            resolved = _resolve_link(agents_path, raw_target, repo_root)
            if resolved is None:
                findings.append(f"AGENTS.md broken link: {label} -> {raw_target}")
                continue
            if resolved in seen:
                continue
            seen.add(resolved)
            if resolved.is_file():
                try:
                    target_text = resolved.read_text(encoding="utf-8")
                except (OSError, UnicodeDecodeError):
                    continue
                routed_headings.update(_heading_set(target_text))

        all_headings = headings | routed_headings
        covered = _topic_coverage(all_headings)
        missing_topics = set(CANONICAL_TOPICS.keys()) - covered
        if missing_topics:
            findings.append(f"AGENTS.md missing canonical topic coverage: {', '.join(sorted(missing_topics))}")

    return findings


def validate_scope_pointer_router(text: str, agents_path: Path, repo_root: Path) -> list[str]:
    """Validate the intentionally minimal, single-sentence scoped-router form."""
    findings: list[str] = []
    lines = [line.strip() for line in text.splitlines() if line.strip()]
    if len(lines) != 1:
        findings.append("scope-pointer router must contain exactly one sentence on one line")
        return findings

    sentence = lines[0]
    scope_match = re.search(
        r"\b(?:for|when)\s+(?:work|working)\s+(?:under|in)\s+`([^`]+)`",
        sentence,
        re.IGNORECASE,
    )
    try:
        expected_scope = agents_path.resolve().parent.relative_to(repo_root.resolve()).as_posix().rstrip("/") + "/"
    except ValueError:
        expected_scope = ""
    if scope_match is None or scope_match.group(1).replace("\\", "/").rstrip("/") + "/" != expected_scope:
        findings.append(f"scope-pointer router must name its containing tree: `{expected_scope}`")

    links = LINK_PATTERN.findall(sentence)
    if not links:
        findings.append("scope-pointer router must link to at least one local guidance file")
    else:
        for label, target in links:
            is_external = target.startswith(("http://", "https://", "mailto:"))
            if is_external or _resolve_link(agents_path, target, repo_root) is None:
                findings.append(f"scope-pointer router link does not resolve locally: {label} -> {target}")

    sentence_text = LINK_PATTERN.sub(lambda match: match.group(1), sentence)
    sentence_text = re.sub(r"`[^`]*`", "", sentence_text)
    endings = re.findall(r"[.!?](?=\s|$)", sentence_text)
    if len(endings) != 1 or not sentence_text.rstrip().endswith("."):
        findings.append("scope-pointer router must be one sentence ending with a period")
    return findings
