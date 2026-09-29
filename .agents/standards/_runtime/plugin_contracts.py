#!/usr/bin/env python3
"""Validate consumer plugin prerequisites without inventorying plugin skills."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

from surface_contracts import Finding


@dataclass(frozen=True)
class ConsumerContract:
    surface_exceptions: tuple[str, ...]
    unslop_profile_roots: tuple[str, ...]


def load_consumer_contract(repo_root: Path) -> ConsumerContract:
    path = repo_root / ".agents/contracts/agent-operating-model.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    exceptions = data.get("surface_exceptions", [])
    roots = data.get("unslop_profile_roots", [])
    return ConsumerContract(
        surface_exceptions=tuple(item["id"] for item in exceptions if isinstance(item, dict) and "id" in item),
        unslop_profile_roots=tuple(item for item in roots if isinstance(item, str)),
    )


def load_legacy_surface_exceptions(repo_root: Path, known_surface_ids: set[str]) -> set[str]:
    """Read the explicit legacy contract used to derive a reviewable migration preview."""

    path = repo_root / ".agents/contracts/agent-operating-model.json"
    if not path.is_file():
        raise ValueError(
            "no authoritative legacy surface contract: .agents/contracts/agent-operating-model.json is missing"
        )
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ValueError(f"legacy operating-model contract cannot be read: {exc}") from exc
    if not isinstance(data, dict) or data.get("version") != 1:
        raise ValueError("legacy operating-model contract must be a version-1 object")
    exceptions = data.get("surface_exceptions")
    if not isinstance(exceptions, list):
        raise ValueError("legacy operating-model surface_exceptions must be a list")
    result: set[str] = set()
    for entry in exceptions:
        if not isinstance(entry, dict) or set(entry) != {"id", "reason"}:
            raise ValueError("legacy surface exceptions must contain exactly id and reason")
        surface_id, reason = entry.get("id"), entry.get("reason")
        if not isinstance(surface_id, str) or surface_id not in known_surface_ids:
            raise ValueError(f"legacy contract names unknown surface exception: {surface_id!r}")
        if not isinstance(reason, str) or not reason.strip():
            raise ValueError(f"legacy surface exception {surface_id} requires a reason")
        if surface_id in result:
            raise ValueError(f"legacy contract repeats surface exception: {surface_id}")
        result.add(surface_id)
    return result


def check_plugin_contract(repo_root: Path, config: ConsumerContract) -> list[Finding]:
    """Compatibility no-op: ambient plugin subscriptions never declare repo standards."""
    del repo_root, config
    return []
