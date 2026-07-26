#!/usr/bin/env python3
"""Validate repo-local skills under declared prefixes.

Called by the `refreshing-installed-skills` skill as:
    scripts/validate_local_skills_extra.sh [--check] <skills-root> <prefix> ...
"""

from __future__ import annotations

import argparse
import re
import sys
from collections.abc import Mapping
from pathlib import Path
from urllib.parse import urlparse

import yaml


REQUIRED_METADATA_KEYS = {"status", "scope", "use_when", "do_not_use_when"}
FORBIDDEN_MARKETPLACE_METADATA_KEYS = {
    "source-id",
    "source-path",
    "provenance-name",
    "source-category",
    "owner",
}
REPO_LOCAL_SKILL_NAME_PATTERN = re.compile(r"[a-z0-9]+(?:-[a-z0-9]+)*")
FRONTMATTER_DELIMITER = "---"
MARKDOWN_LINK_PATTERN = re.compile(r"(?<!!)\[[^\]]+\]\(([^)]+)\)")


def _reserved_skill_dirs(skills_root: Path, prefixes: list[str]) -> list[Path]:
    if not skills_root.is_dir():
        return []
    return sorted(
        path
        for path in skills_root.iterdir()
        if path.is_dir() and any(path.name.startswith(p) for p in prefixes)
    )


def _split_frontmatter(skill_path: Path) -> tuple[str, str]:
    content = skill_path.read_text(encoding="utf-8")
    lines = content.splitlines()
    if not lines or lines[0].strip() != FRONTMATTER_DELIMITER:
        raise ValueError("missing YAML frontmatter")

    for index in range(1, len(lines)):
        if lines[index].strip() == FRONTMATTER_DELIMITER:
            return "\n".join(lines[1:index]), "\n".join(lines[index + 1 :])

    raise ValueError("missing closing YAML frontmatter delimiter")


def _load_frontmatter(skill_path: Path) -> tuple[dict[str, object], str]:
    frontmatter_text, body = _split_frontmatter(skill_path)
    try:
        document = yaml.safe_load(frontmatter_text)
    except yaml.YAMLError as error:
        raise ValueError(f"frontmatter YAML is invalid: {error}") from error

    if not isinstance(document, dict):
        raise ValueError("frontmatter must decode to a mapping")

    return document, body


def _count_words(body: str) -> int:
    return len(body.split())


def _is_relative_markdown_target(target: str) -> bool:
    if not target or target.startswith("#"):
        return False

    parsed = urlparse(target)
    if parsed.scheme or target.startswith("/"):
        return False

    return True


def _missing_markdown_reference(skill_dir: Path, body: str) -> list[str]:
    errors: list[str] = []
    for raw_target in MARKDOWN_LINK_PATTERN.findall(body):
        target = raw_target.strip().strip("<>")
        if not _is_relative_markdown_target(target):
            continue

        path_part = target.split("#", 1)[0].split("?", 1)[0]
        if not path_part:
            continue

        reference_path = skill_dir / Path(path_part)
        if not reference_path.exists():
            errors.append(
                f"{skill_dir.name}: missing Markdown reference '{path_part}'"
            )

    return errors


def _validate_required_metadata(
    skill_dir: Path, metadata: Mapping[str, object]
) -> list[str]:
    errors: list[str] = []
    for key in sorted(REQUIRED_METADATA_KEYS):
        if key not in metadata:
            errors.append(f"{skill_dir.name}: metadata.{key} is required")
    return errors


def _validate_forbidden_metadata(
    skill_dir: Path, metadata: Mapping[str, object]
) -> list[str]:
    errors: list[str] = []
    for key in sorted(FORBIDDEN_MARKETPLACE_METADATA_KEYS):
        if key in metadata:
            errors.append(f"{skill_dir.name}: metadata.{key} is forbidden")
    return errors


def _validate_reserved_skill(skill_dir: Path) -> list[str]:
    errors: list[str] = []
    skill_path = skill_dir / "SKILL.md"

    if not REPO_LOCAL_SKILL_NAME_PATTERN.fullmatch(skill_dir.name):
        errors.append(
            f"{skill_dir.name}: directory name must use lowercase-hyphen format"
        )

    if not skill_path.is_file():
        errors.append(f"{skill_dir.name}: SKILL.md is required")
        return errors

    try:
        frontmatter, body = _load_frontmatter(skill_path)
    except (OSError, UnicodeDecodeError, ValueError) as error:
        return [f"{skill_dir.name}: {error}"]

    name = frontmatter.get("name")
    if name != skill_dir.name:
        errors.append(f"{skill_dir.name}: name must match directory name")

    description = frontmatter.get("description")
    if not isinstance(description, str) or not description.startswith("Use when"):
        errors.append(
            f"{skill_dir.name}: description must begin with 'Use when'"
        )

    metadata = frontmatter.get("metadata")
    if not isinstance(metadata, Mapping):
        errors.append(f"{skill_dir.name}: metadata must be a mapping")
    else:
        errors.extend(_validate_required_metadata(skill_dir, metadata))
        errors.extend(_validate_forbidden_metadata(skill_dir, metadata))

    if (skill_dir / "agents" / "openai.yaml").exists():
        errors.append(f"{skill_dir.name}: agents/openai.yaml is forbidden")

    if _count_words(body) > 500:
        errors.append(f"{skill_dir.name}: body exceeds 500 words")

    errors.extend(_missing_markdown_reference(skill_dir, body))
    return errors


def validate_local_skills(skills_root: Path, prefixes: list[str]) -> list[str]:
    """Return stable contract errors for every local skill directory matching prefixes."""
    errors: list[str] = []
    for skill_dir in _reserved_skill_dirs(skills_root, prefixes):
        errors.extend(_validate_reserved_skill(skill_dir))
    return sorted(errors)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Validate repo-local skills under declared prefixes."
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Validation is read-only (already the default).",
    )
    parser.add_argument(
        "skills_root",
        type=Path,
        help="Path to the skills root directory (e.g. .agents/skills).",
    )
    parser.add_argument(
        "prefixes",
        nargs="+",
        help="One or more local skill prefixes (e.g. wild-bunch-).",
    )
    args = parser.parse_args(argv)

    errors = validate_local_skills(args.skills_root, args.prefixes)
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
