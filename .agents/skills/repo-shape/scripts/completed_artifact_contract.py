#!/usr/bin/env python3
"""Semantic lifecycle contract for completed planning artifacts."""

from __future__ import annotations

import re
from pathlib import Path

from surface_contracts import Finding


def check_completed_artifact_doctrine(path: Path, repo_root: Path | None = None) -> list[Finding]:
    del repo_root
    if not path.is_file():
        return [
            Finding(
                "failure",
                "missing-completed-artifacts-doctrine",
                path.as_posix(),
                "completed-artifacts doctrine is missing",
                "create the doctrine",
            )
        ]
    text = path.read_text(encoding="utf-8")
    live = re.sub(r"<!--.*?-->", "", text, flags=re.DOTALL)
    required = {"Scope", "Doctrine", "Ownership"}
    headings = set(re.findall(r"(?m)^##\s+(.+?)\s*$", live))
    findings: list[Finding] = []
    for heading in sorted(required - headings):
        findings.append(
            Finding(
                "failure",
                "missing-lifecycle-surface",
                path.as_posix(),
                f"completed-artifacts doctrine is missing {heading}",
                f"add the {heading} lifecycle section",
            )
        )
    lowered = live.lower()
    if any(
        term in lowered
        for term in ("rejected candidate completes", "scratch completes", "rejected-candidate completion")
    ):
        findings.append(
            Finding(
                "failure",
                "invalid-completion-trigger",
                path.as_posix(),
                "scratch or rejected candidates cannot complete governed artifacts",
                "reserve completion for accepted repository work",
            )
        )
    for invariant, code in (
        ("successor", "missing-successor-retirement"),
        ("completed-awaiting-retirement", "missing-completion-marking"),
        ("git history", "missing-history-retention"),
        ("abandon", "missing-abandonment-path"),
        ("promot", "missing-promotion-before-removal"),
    ):
        if invariant not in lowered:
            findings.append(
                Finding(
                    "failure",
                    code,
                    path.as_posix(),
                    f"lifecycle doctrine does not state the {invariant} invariant",
                    "state the lifecycle invariant explicitly",
                )
            )
    return findings
