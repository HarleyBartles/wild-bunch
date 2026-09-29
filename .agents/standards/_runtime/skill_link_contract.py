#!/usr/bin/env python3
"""Resolve workflow skill links against the consumer-visible namespace."""

from __future__ import annotations

import json
import re
from pathlib import Path

from surface_contracts import Finding


def _frontmatter_name(skill_file: Path) -> str | None:
    text = skill_file.read_text(encoding="utf-8")
    if not text.startswith("---"):
        return None
    parts = text.split("---", 2)
    if len(parts) < 3:
        return None
    match = re.search(r"(?im)^name:\s*['\"]?([^'\"\n]+)", parts[1])
    return match.group(1).strip() if match else None


def composition_paths(root: Path) -> tuple[list[Path], list[Path]]:
    """Return locally mapped workflow documents, with conventional fallback homes."""
    policy = root / ".agents/doctrine/repo-runbook-policy.md"
    mapped: dict[str, list[Path]] = {"runbook": [], "playbook": []}
    if policy.is_file():
        section = ""
        for line in policy.read_text(encoding="utf-8").splitlines():
            heading = re.match(r"^##\s+Standard (runbooks|playbooks)\s*$", line.strip(), re.I)
            if heading:
                section = "runbook" if heading.group(1).lower() == "runbooks" else "playbook"
                continue
            if line.startswith("## "):
                section = ""
                continue
            if not section or not line.strip().startswith("|") or "---" in line:
                continue
            cells = [cell.strip().strip("`") for cell in line.strip().strip("|").split("|")]
            if len(cells) >= 3 and cells[2].lower() in {"required", "optional"}:
                candidate = (root / cells[1]).resolve()
                if candidate.is_relative_to(root.resolve()):
                    mapped[section].append(candidate)
    fallback = {
        "runbook": sorted((root / ".agents/runbooks").glob("*.md")),
        "playbook": sorted((root / ".agents/playbooks").glob("*.md")),
    }
    return (mapped["runbook"] or fallback["runbook"], mapped["playbook"] or fallback["playbook"])


def has_custom_composition_paths(root: Path) -> bool:
    policy = root / ".agents/doctrine/repo-runbook-policy.md"
    if not policy.is_file():
        return False
    runbooks, playbooks = composition_paths(root)
    expected_runbooks = (root / ".agents/runbooks").resolve()
    expected_playbooks = (root / ".agents/playbooks").resolve()
    return any(path.parent.resolve() != expected_runbooks for path in runbooks) or any(
        path.parent.resolve() != expected_playbooks for path in playbooks
    )


def required_composition_paths(root: Path, kind: str) -> list[Path]:
    policy = root / ".agents/doctrine/repo-runbook-policy.md"
    if not policy.is_file():
        return []
    section = ""
    required: list[Path] = []
    for line in policy.read_text(encoding="utf-8").splitlines():
        heading = re.match(r"^##\s+Standard (runbooks|playbooks)\s*$", line.strip(), re.I)
        if heading:
            section = "runbook" if heading.group(1).lower() == "runbooks" else "playbook"
            continue
        if line.startswith("## "):
            section = ""
            continue
        if section != kind or not line.strip().startswith("|") or "---" in line:
            continue
        cells = [cell.strip().strip("`") for cell in line.strip().strip("|").split("|")]
        if len(cells) >= 3 and cells[2].lower() == "required":
            required.append((root / cells[1]).resolve())
    return required


def _visible_skills(root: Path) -> tuple[set[str], list[Finding]]:
    skills = root / ".agents/skills"
    visible: set[str] = set()
    findings: list[Finding] = []
    for path in skills.iterdir() if skills.is_dir() else ():
        if not path.is_dir() or not (path / "SKILL.md").is_file():
            continue
        visible.add(path.name)
        frontmatter_name = _frontmatter_name(path / "SKILL.md")
        if frontmatter_name:
            visible.add(frontmatter_name)
    try:
        data = json.loads((root / ".agents/plugins/marketplace.json").read_text(encoding="utf-8"))
        for name in data.get("repo", {}).get("local_skills", []):
            if isinstance(name, str):
                skill_file = skills / name / "SKILL.md"
                if not skill_file.is_file():
                    findings.append(
                        Finding(
                            "failure",
                            "missing-repo-local-skill",
                            ".agents/plugins/marketplace.json",
                            f"declared repo-local skill is not installed: {name}",
                            "create the declared local skill or remove its exact declaration",
                        )
                    )
                    continue
                if _frontmatter_name(skill_file) != name:
                    findings.append(
                        Finding(
                            "failure",
                            "repo-local-skill-name-mismatch",
                            skill_file.relative_to(root).as_posix(),
                            f"declared repo-local skill name does not match frontmatter: {name}",
                            "align the directory, declaration, and SKILL.md name exactly",
                        )
                    )
    except (OSError, json.JSONDecodeError, AttributeError):
        pass
    return visible, findings


def check_skill_links(repo_root: Path) -> list[Finding]:
    _, findings = _visible_skills(repo_root)
    local = set()
    try:
        data = json.loads((repo_root / ".agents/plugins/marketplace.json").read_text(encoding="utf-8"))
        local = set(data.get("repo", {}).get("local_skills", []))
    except (OSError, json.JSONDecodeError, AttributeError):
        pass
    runbooks, playbooks = composition_paths(repo_root)
    for paths in (runbooks, playbooks):
        for path in paths:
            if not path.is_file():
                continue
            text = path.read_text(encoding="utf-8")
            sections = {
                match.group(1).strip(): match.group(2)
                for match in re.finditer(r"(?ms)^##\s+(.+?)\s*\n(.*?)(?=^##\s+|\Z)", text)
            }
            exact_names: set[str] = set()
            for heading in ("Required repository-owned skills", "Optional repository-owned skills"):
                section = sections.get(heading, "")
                exact_names.update(re.findall(r"`([A-Za-z0-9][A-Za-z0-9_.+-]*)`", section))
            for skill in sorted(exact_names):
                if skill not in local:
                    findings.append(
                        Finding(
                            "failure",
                            "non-local-repository-skill",
                            path.relative_to(repo_root).as_posix(),
                            f"workflow declares ambient or undeclared skill as repository-owned: {skill}",
                            "declare genuine local skills in repo.local_skills or "
                            "express ambient needs as capabilities",
                        )
                    )
    return findings
