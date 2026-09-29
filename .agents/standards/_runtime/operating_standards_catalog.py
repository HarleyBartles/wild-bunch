#!/usr/bin/env python3
"""Validate the Agent Operating Model standards catalog and its source resources."""

from __future__ import annotations

import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import surface_contracts


_ID = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
_REVISION = re.compile(r"^(?:[0-9a-f]{40}|[0-9a-f]{64})$")
_REQUIRED = {"id", "title", "surfaces", "resources", "check", "apply", "requires"}
_COMPOSITION_FIELDS = {
    "id",
    "origin",
    "implementation_root",
    "check",
    "apply",
    "generated_paths",
    "requires",
}


@dataclass(frozen=True)
class OperatingStandard:
    id: str
    title: str
    surfaces: tuple[str, ...]
    resources: tuple[str, ...]
    check: bool
    apply: bool
    requires: tuple[str, ...]


@dataclass(frozen=True)
class StandardsCatalog:
    version: int
    standards: tuple[OperatingStandard, ...]
    migration_surfaces: tuple[str, ...]


def _string_list(raw: Any, *, field: str, standard_id: str, allow_empty: bool = False) -> tuple[str, ...]:
    if not isinstance(raw, list) or (not raw and not allow_empty):
        raise ValueError(f"standard {standard_id} {field} must be a non-empty list")
    if any(not isinstance(item, str) or not item.strip() for item in raw):
        raise ValueError(f"standard {standard_id} {field} must contain non-empty strings")
    if len(raw) != len(set(raw)):
        raise ValueError(f"standard {standard_id} {field} contains duplicate values")
    return tuple(raw)


def _resource_path(raw: str, *, standard_id: str, source_root: Path) -> Path:
    normalized = raw.replace("\\", "/")
    path = Path(normalized)
    if (
        path.is_absolute()
        or normalized.startswith("/")
        or not path.parts
        or ".." in path.parts
        or normalized in {"", "."}
    ):
        raise ValueError(f"standard {standard_id} resource must be repository-relative without '..': {raw}")
    candidate = (source_root / path).resolve()
    try:
        candidate.relative_to(source_root.resolve())
    except ValueError as exc:
        raise ValueError(f"standard {standard_id} resource escapes marketplace source: {raw}") from exc
    if not candidate.is_file():
        raise ValueError(f"standard {standard_id} resource does not exist: {normalized}")
    return candidate


def _check_cycles(standards: dict[str, OperatingStandard]) -> None:
    done: set[str] = set()
    active: list[str] = []

    def visit(standard_id: str) -> None:
        if standard_id in active:
            cycle = " -> ".join([*active[active.index(standard_id) :], standard_id])
            raise ValueError(f"standard dependency cycle: {cycle}")
        if standard_id in done:
            return
        active.append(standard_id)
        for dependency in standards[standard_id].requires:
            if dependency not in standards:
                raise ValueError(f"standard {standard_id} has unknown dependency: {dependency}")
            visit(dependency)
        active.pop()
        done.add(standard_id)

    for standard_id in standards:
        visit(standard_id)


def load_catalog(
    catalog_path: Path, manifest_path: Path, source_root: Path, *, validate_resources: bool = True
) -> StandardsCatalog:
    """Load and validate catalog structure, surface ownership, dependencies, and resources."""

    raw = json.loads(catalog_path.read_text(encoding="utf-8-sig"))
    if not isinstance(raw, dict) or set(raw) != {"version", "standards", "migration_surfaces"}:
        raise ValueError("catalog must contain only version, standards, and migration_surfaces")
    if raw["version"] != 1:
        raise ValueError("catalog version must be 1")
    rows = raw["standards"]
    if not isinstance(rows, list) or not rows:
        raise ValueError("catalog standards must be a non-empty list")
    migration_surfaces = _string_list(
        raw["migration_surfaces"],
        field="migration_surfaces",
        standard_id="catalog",
        allow_empty=True,
    )

    standards: list[OperatingStandard] = []
    ids: set[str] = set()
    assigned: dict[str, str] = {}
    for index, row in enumerate(rows):
        if not isinstance(row, dict) or set(row) != _REQUIRED:
            raise ValueError(f"standard[{index}] must contain exactly: {', '.join(sorted(_REQUIRED))}")
        standard_id = row["id"]
        title = row["title"]
        if not isinstance(standard_id, str) or not _ID.fullmatch(standard_id):
            raise ValueError(f"standard[{index}] id must be kebab-case")
        if standard_id in ids:
            raise ValueError(f"duplicate standard id: {standard_id}")
        ids.add(standard_id)
        if not isinstance(title, str) or not title.strip():
            raise ValueError(f"standard {standard_id} title must be non-empty")
        surfaces = _string_list(row["surfaces"], field="surfaces", standard_id=standard_id)
        resources = _string_list(row["resources"], field="resources", standard_id=standard_id)
        requires = _string_list(row["requires"], field="requires", standard_id=standard_id, allow_empty=True)
        for surface_id in surfaces:
            if surface_id in assigned:
                raise ValueError(f"surface {surface_id} assigned more than once")
            assigned[surface_id] = standard_id
        if validate_resources:
            for resource in resources:
                _resource_path(resource, standard_id=standard_id, source_root=source_root)
        for capability in ("check", "apply"):
            if not isinstance(row[capability], bool):
                raise ValueError(f"standard {standard_id} {capability} must be a boolean")
        if not row["check"]:
            raise ValueError(f"standard {standard_id} must support check")
        standards.append(
            OperatingStandard(
                standard_id,
                title,
                surfaces,
                resources,
                row["check"],
                row["apply"],
                requires,
            )
        )

    manifest = surface_contracts.load_manifest(manifest_path)
    expected_surfaces = {surface.id for surface in manifest.surfaces}
    actual_surfaces = set(assigned)
    duplicate_migration_surfaces = actual_surfaces & set(migration_surfaces)
    if duplicate_migration_surfaces:
        raise ValueError(
            f"surface assigned as standard and migration input: {', '.join(sorted(duplicate_migration_surfaces))}"
        )
    actual_surfaces.update(migration_surfaces)
    unknown_surfaces = actual_surfaces - expected_surfaces
    if unknown_surfaces:
        raise ValueError(f"catalog contains unknown surface(s): {', '.join(sorted(unknown_surfaces))}")
    uncovered = expected_surfaces - actual_surfaces
    if uncovered:
        raise ValueError(f"catalog does not assign surface(s): {', '.join(sorted(uncovered))}")
    by_id = {standard.id: standard for standard in standards}
    _check_cycles(by_id)
    return StandardsCatalog(version=1, standards=tuple(standards), migration_surfaces=migration_surfaces)


def _safe_repo_path(value: Any, *, field: str, standard_id: str) -> None:
    if not isinstance(value, str) or not value or "\x00" in value:
        raise ValueError(f"standard {standard_id} {field} must be a non-empty repository-relative path")
    normalized = value.replace("\\", "/")
    path = Path(normalized)
    if (
        path.is_absolute()
        or normalized.startswith(("/", "!", ":"))
        or re.match(r"^[A-Za-z]:", normalized)
        or ".." in path.parts
        or normalized in {"", "."}
    ):
        raise ValueError(f"standard {standard_id} {field} must be repository-relative without '..'")


def _command_vector(value: Any, *, field: str, standard_id: str, allow_empty: bool) -> tuple[str, ...]:
    if not isinstance(value, list) or (not value and not allow_empty):
        requirement = "may be empty or" if allow_empty else "must be"
        raise ValueError(f"standard {standard_id} {field} command {requirement} a vector")
    if any(not isinstance(part, str) or not part or "\x00" in part for part in value):
        raise ValueError(f"standard {standard_id} {field} command vector must contain non-empty strings")
    return tuple(value)


def validate_composition(data: Any, catalog: StandardsCatalog) -> None:
    """Validate an explicit marketplace and repository-owned standards composition."""

    if not isinstance(data, dict) or set(data) != {"version", "standards"}:
        raise ValueError("composition must contain only version and standards")
    if data["version"] != 1:
        raise ValueError("composition version must be 1")
    entries = data["standards"]
    if not isinstance(entries, list):
        raise ValueError("composition standards must be a list")

    catalog_by_id = {standard.id: standard for standard in catalog.standards}
    by_id: dict[str, dict[str, Any]] = {}
    for index, entry in enumerate(entries):
        if not isinstance(entry, dict):
            raise ValueError(f"standard entry[{index}] must be an object")
        standard_id = entry.get("id")
        if not isinstance(standard_id, str) or not _ID.fullmatch(standard_id):
            raise ValueError(f"standard entry[{index}] id must be kebab-case")
        if standard_id in by_id:
            raise ValueError(f"duplicate standard id in composition: {standard_id}")
        origin = entry.get("origin")
        if origin not in {"marketplace", "repository"}:
            raise ValueError(f"standard {standard_id} origin must be marketplace or repository")
        if origin == "repository" and standard_id in catalog_by_id:
            raise ValueError(f"repository-owned standard id is reserved by the marketplace catalog: {standard_id}")
        if origin == "repository" and "revision" in entry:
            raise ValueError(f"repository-owned standard cannot set revision: {standard_id}")
        expected_fields = _COMPOSITION_FIELDS | ({"revision"} if origin == "marketplace" else set())
        if set(entry) != expected_fields:
            raise ValueError(f"standard {standard_id} has invalid fields for origin {origin}")
        _safe_repo_path(entry["implementation_root"], field="implementation_root", standard_id=standard_id)
        _command_vector(entry["check"], field="check", standard_id=standard_id, allow_empty=False)
        apply = _command_vector(entry["apply"], field="apply", standard_id=standard_id, allow_empty=True)
        generated_paths = _string_list(
            entry["generated_paths"], field="generated_paths", standard_id=standard_id, allow_empty=True
        )
        for path in generated_paths:
            _safe_repo_path(path, field="generated_paths", standard_id=standard_id)
        requires = _string_list(entry["requires"], field="requires", standard_id=standard_id, allow_empty=True)
        if origin == "marketplace":
            if standard_id not in catalog_by_id:
                raise ValueError(f"unknown standard in catalog: {standard_id}")
            revision = entry["revision"]
            if not isinstance(revision, str) or not _REVISION.fullmatch(revision):
                raise ValueError(f"standard {standard_id} revision must be a pinned commit")
            source_standard = catalog_by_id[standard_id]
            if not source_standard.apply and apply:
                raise ValueError(f"standard {standard_id} does not define an apply command")
            if source_standard.apply and not apply:
                raise ValueError(f"standard {standard_id} requires an apply command")
            missing_requirements = set(source_standard.requires) - set(requires)
            if missing_requirements:
                raise ValueError(
                    f"standard {standard_id} missing required standard(s): {', '.join(sorted(missing_requirements))}"
                )
        by_id[standard_id] = entry

    for standard_id, entry in by_id.items():
        for dependency in entry["requires"]:
            if dependency not in by_id:
                raise ValueError(f"standard {standard_id} has unknown required standard: {dependency}")

    completed: set[str] = set()
    active: list[str] = []

    def visit(standard_id: str) -> None:
        if standard_id in active:
            cycle = " -> ".join([*active[active.index(standard_id) :], standard_id])
            raise ValueError(f"composition dependency cycle: {cycle}")
        if standard_id in completed:
            return
        active.append(standard_id)
        for dependency in by_id[standard_id]["requires"]:
            visit(dependency)
        active.pop()
        completed.add(standard_id)

    for standard_id in by_id:
        visit(standard_id)
