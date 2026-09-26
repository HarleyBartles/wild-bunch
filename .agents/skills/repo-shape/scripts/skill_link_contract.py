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
    visible, findings = _visible_skills(repo_root)
    for directory, heading, composition in (
        ("runbooks", "Required skills", "Composition"),
        ("playbooks", "Required skills", "Composition"),
    ):
        for path in (
            sorted((repo_root / ".agents" / directory).glob("*.md"))
            if (repo_root / ".agents" / directory).is_dir()
            else ()
        ):
            text = path.read_text(encoding="utf-8")
            sections = re.split(r"(?m)^##\s+", text)
            required = next((part for part in sections if part.startswith(heading)), "")
            comp = next((part for part in sections if part.startswith(composition)), "")
            required_names = set(re.findall(r"`([A-Za-z0-9][A-Za-z0-9_.+-]*)`", required))
            comp_names = set(re.findall(r"`([A-Za-z0-9][A-Za-z0-9_.+-]*)`", comp))
            comp_names -= {"main", "HEAD", "completed-awaiting-retirement", "Composition"}
            for skill in sorted(required_names | comp_names):
                if skill in {"runbooks", "playbooks", "Composition"} or "/" in skill:
                    continue
                if skill not in visible:
                    findings.append(
                        Finding(
                            "failure",
                            "dead-skill-link",
                            path.relative_to(repo_root).as_posix(),
                            f"workflow references missing skill: {skill}",
                            "install a provider plugin or declare a repo-local skill",
                        )
                    )
                elif skill in comp_names and skill not in required_names:
                    findings.append(
                        Finding(
                            "failure",
                            "unlisted-composition-skill",
                            path.relative_to(repo_root).as_posix(),
                            f"Composition invokes skill not listed in Required skills: {skill}",
                            "add the exact skill to Required skills",
                        )
                    )
    return findings
