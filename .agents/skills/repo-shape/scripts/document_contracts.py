#!/usr/bin/env python3
"""Semantic validators for consumer-owned operating-model documents."""

from __future__ import annotations

import re
from pathlib import Path
from typing import Callable

from surface_contracts import Finding
import _agents_md
import completed_artifact_contract


_COMMENT = re.compile(r"<!--.*?-->", re.DOTALL)
_PLACEHOLDER = re.compile(r"(?:choose|todo|tbd|replace|fill|list).*(?:later|here|this)?", re.IGNORECASE)


def live_markdown(text: str) -> str:
    text = _COMMENT.sub("", text)
    lines: list[str] = []
    fenced = False
    for line in text.splitlines():
        if line.strip().startswith("```"):
            fenced = not fenced
            continue
        if not fenced:
            lines.append(line)
    return "\n".join(lines)


def _sections(path: Path) -> dict[str, str]:
    live = live_markdown(path.read_text(encoding="utf-8"))
    matches = list(re.finditer(r"(?m)^##\s+(.+?)\s*$", live))
    return {
        match.group(1).strip(): live[
            match.end() : matches[index + 1].start() if index + 1 < len(matches) else None
        ].strip()
        for index, match in enumerate(matches)
    }


def _finding(code: str, path: Path, message: str, repair: str) -> Finding:
    return Finding("failure", code, path.as_posix(), message, repair)


def _require_sections(path: Path, names: tuple[str, ...]) -> list[Finding]:
    if not path.is_file():
        return [
            _finding("missing-document", path, f"missing document: {path}", "create the required consumer document")
        ]
    sections = _sections(path)
    findings: list[Finding] = []
    for name in names:
        body = sections.get(name, "")
        if not body or _PLACEHOLDER.fullmatch(body.strip("- \t.")):
            code = "empty-" + name.lower().replace(" ", "-")
            findings.append(
                _finding(code, path, f"section has no live content: {name}", f"write repository-owned {name} content")
            )
    return findings


_COMPOSITION = (
    "When",
    "Required skills",
    "Composition",
    "Doctrine and contracts",
    "Local commands and paths",
    "Evidence contract",
    "Prohibited combinations",
)


def check_runbook(path: Path, repo_root: Path) -> list[Finding]:
    del repo_root
    return _require_sections(path, (*_COMPOSITION, "Playbook routing"))


def check_playbook(path: Path, repo_root: Path) -> list[Finding]:
    del repo_root
    return _require_sections(path, (*_COMPOSITION, "Runbook routing"))


def check_review(path: Path, repo_root: Path) -> list[Finding]:
    del repo_root
    return _require_sections(path, ("Pre-review reading", "Workflow routing", "First-class review concerns"))


def check_contributing(path: Path, repo_root: Path) -> list[Finding]:
    del repo_root
    if not path.is_file():
        return [
            _finding("missing-document", path, f"missing document: {path}", "create the required consumer document")
        ]
    sections = _sections(path)
    findings: list[Finding] = []
    if not live_markdown(path.read_text(encoding="utf-8")).strip():
        findings.append(
            _finding(
                "empty-contributing-entry",
                path,
                "contribution entrypoint has no live content",
                "document the repository contribution route",
            )
        )
    if not (sections.get("Workflow routing") or sections.get("Contributor workflow")):
        findings.append(
            _finding(
                "empty-workflow-routing",
                path,
                "contribution entrypoint lacks workflow routing",
                "document the contributor workflow",
            )
        )
    return findings


def check_policy(path: Path, repo_root: Path) -> list[Finding]:
    del repo_root
    return _require_sections(path, ("Standard runbooks", "Standard playbooks"))


def check_agents(path: Path, repo_root: Path) -> list[Finding]:
    if not path.exists():
        return []
    return [
        _finding("invalid-agents-router", path, message, "repair the router's mandatory sections and links")
        for message in _agents_md.validate_agents_md(path, repo_root)
    ]


def check_optional_router(path: Path, repo_root: Path) -> list[Finding]:
    del repo_root
    if not path.exists():
        return []
    live = live_markdown(path.read_text(encoding="utf-8")).strip()
    if not live or not re.search(r"(?m)^#\s+\S", live):
        return [
            _finding(
                "empty-scoped-router",
                path,
                "scoped AGENTS.md has no live routing content",
                "add a title and routing guidance or remove the optional file",
            )
        ]
    return []


def check_runbook_set(path: Path, repo_root: Path) -> list[Finding]:
    if not path.is_dir():
        return [_finding("missing-runbook-set", path, "runbook directory is missing", "scaffold the standard runbooks")]
    return [
        finding
        for item in sorted(path.glob("*.md"))
        if item.name not in {"AGENTS.md", "INDEX.md"}
        for finding in check_runbook(item, repo_root)
    ]


def check_playbook_set(path: Path, repo_root: Path) -> list[Finding]:
    if not path.is_dir():
        return [
            _finding("missing-playbook-set", path, "playbook directory is missing", "scaffold the standard playbooks")
        ]
    return [
        finding
        for item in sorted(path.glob("*.md"))
        if item.name not in {"AGENTS.md", "INDEX.md"}
        for finding in check_playbook(item, repo_root)
    ]


DocumentValidator = Callable[[Path, Path], list[Finding]]
DOCUMENT_VALIDATORS: dict[str, DocumentValidator] = {
    "completed-artifacts-contract-v1": completed_artifact_contract.check_completed_artifact_doctrine,
    "agents-router-contract": check_agents,
    "optional-agents-router-contract": check_optional_router,
    "runbook-set-contract": check_runbook_set,
    "playbook-set-contract": check_playbook_set,
    "repo-runbook-policy-contract": check_policy,
    "review-contract": check_review,
    "contributing-contract": check_contributing,
}
