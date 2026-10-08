#!/usr/bin/env python3
"""Check v2 subscription structure and local certification references.

The schema follows the optional checker published with Agent Operating Model
at commit b481f98ae90aa45e5271d10fe1f7aaeb6c7047aa. This check cannot certify
the truth or quality of a repository's semantic assessment.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path, PurePosixPath, PureWindowsPath

RECORD = ".agents/contracts/operating-standards.json"
ID = re.compile(r"^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$")
COMMIT = re.compile(r"^(?:[0-9a-f]{40}|[0-9a-f]{64})$")


def _relative_path(value: object, *, fragment: bool = False) -> bool:
    if not isinstance(value, str) or not value:
        return False
    file_path, marker, section = value.partition("#")
    if marker and (not fragment or not section or "#" in section):
        return False
    posix = PurePosixPath(file_path)
    windows = PureWindowsPath(file_path)
    return bool(
        file_path
        and not file_path.startswith("/")
        and "\\" not in file_path
        and ":" not in file_path
        and not windows.anchor
        and not windows.drive
        and posix.as_posix() == file_path
        and not any(part in {".", ".."} for part in posix.parts)
        and not file_path.endswith("/")
    )


def validate_record(value: object) -> list[str]:
    """Return structural findings for a v2 subscription object."""
    if not isinstance(value, dict):
        return ["subscription record must be a JSON object"]
    findings: list[str] = []
    if set(value) != {"version", "standards"}:
        findings.append("record must contain only version and standards")
    if value.get("version") != 2 or isinstance(value.get("version"), bool):
        findings.append("record version must be numeric 2")
    standards = value.get("standards")
    if not isinstance(standards, list):
        return [*findings, "standards must be an array"]

    seen: set[str] = set()
    for index, entry in enumerate(standards):
        label = f"standards[{index}]"
        if not isinstance(entry, dict):
            findings.append(f"{label} must be an object")
            continue
        if set(entry) != {"id", "source", "certification"}:
            findings.append(f"{label} must contain only id, source, and certification")
        standard_id = entry.get("id")
        if not isinstance(standard_id, str) or not ID.fullmatch(standard_id):
            findings.append(f"{label}.id must be lowercase kebab-case")
        elif standard_id in seen:
            findings.append(f"{label}.id duplicates {standard_id!r}")
        else:
            seen.add(standard_id)

        source = entry.get("source")
        if not isinstance(source, dict):
            findings.append(f"{label}.source must be an object")
        else:
            if set(source) != {"repository", "commit", "definition"}:
                findings.append(f"{label}.source must contain repository, commit, and definition")
            repository = source.get("repository")
            if not isinstance(repository, str) or not repository.strip():
                findings.append(f"{label}.source.repository must be a non-empty string")
            if not isinstance(source.get("commit"), str) or not COMMIT.fullmatch(source["commit"]):
                findings.append(
                    f"{label}.source.commit must be a full lowercase immutable Git object ID"
                )
            if not _relative_path(source.get("definition")):
                findings.append(f"{label}.source.definition must be a normalized relative path")
        if not _relative_path(entry.get("certification"), fragment=True):
            findings.append(
                f"{label}.certification must be a relative file path with an optional "
                "section fragment"
            )
    return findings


def _slug(value: str) -> str:
    value = value.strip().strip("#").strip().strip("`").lower()
    return re.sub(r"[^a-z0-9]+", "-", value).strip("-")


def check_repository(root: Path) -> list[str]:
    path = root / RECORD
    try:
        record = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return [f"missing subscription record: {RECORD}"]
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        return [f"{RECORD} is not readable JSON: {exc}"]

    if isinstance(record, dict) and record.get("version") == 1:
        return [f"{RECORD} uses legacy version 1; migration is explicit repository work"]
    findings = validate_record(record)
    if findings:
        return findings
    if not record["standards"]:
        return []
    router = root / "AGENTS.md"
    if not router.is_file():
        findings.append(
            "selected standards require root AGENTS.md to route subscription and certification"
        )
    else:
        text = router.read_text(encoding="utf-8")
        for route in (RECORD, ".agents/contracts/standards-certification.md"):
            if route not in text:
                findings.append(f"root AGENTS.md does not route to {route}")

    for index, entry in enumerate(record["standards"]):
        reference = entry["certification"]
        file_part, _, fragment = reference.partition("#")
        candidate = (root / Path(*PurePosixPath(file_part).parts)).resolve()
        try:
            candidate.relative_to(root.resolve())
        except ValueError:
            findings.append(f"standards[{index}].certification resolves outside the repository")
            continue
        if not candidate.is_file():
            findings.append(f"standards[{index}].certification file is missing: {file_part}")
            continue
        if fragment:
            try:
                headings = {
                    _slug(match.group(1))
                    for line in candidate.read_text(encoding="utf-8").splitlines()
                    if (match := re.match(r"^#{1,6}\s+(.+?)\s*#*\s*$", line))
                }
            except (OSError, UnicodeError) as exc:
                findings.append(f"standards[{index}].certification is unreadable: {exc}")
                continue
            if fragment not in headings:
                findings.append(f"standards[{index}].certification section is missing: {reference}")
    return findings


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Check v2 subscription structure without certifying semantics. (read-only)"
    )
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--check", action="store_true", help="check without writing or fetching")
    args = parser.parse_args(argv)
    findings = check_repository(args.repo_root)
    for finding in findings:
        print(f"ERROR: {finding}", file=sys.stderr)
    if findings:
        return 1
    print(
        "OK subscription structure and certification references; semantic compliance "
        "is not assessed"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
