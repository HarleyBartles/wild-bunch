"""Tests for validate_repo_local_skills.py."""

from __future__ import annotations

from pathlib import Path
import sys

import pytest

sys.path.insert(0, str(Path(__file__).parent.parent))

from validate_repo_local_skills import validate_repo_local_skills


def _write_skill(
    root: Path,
    directory_name: str,
    *,
    name: str | None = None,
    description: str = "Use when validating Wild Bunch local skills.",
    metadata: str | None = None,
    body: str | None = None,
) -> Path:
    skill_dir = root / directory_name
    skill_dir.mkdir(parents=True)
    frontmatter = "\n".join(
        [
            "---",
            f"name: {name or directory_name}",
            f"description: {description}",
            metadata
            or "\n".join(
                [
                    "metadata:",
                    "  status: active",
                    "  scope: Validate reserved repo-local Wild Bunch skills.",
                    "  use_when:",
                    "    - Use when the Wild Bunch repo defines local skill wrappers.",
                    "  do_not_use_when:",
                    "    - Do not use when validating non-reserved directories.",
                ]
            ),
            "---",
            "",
        ]
    )
    (skill_dir / "SKILL.md").write_text(
        frontmatter + (body or "See [notes](references/notes.md).\n"),
        encoding="utf-8",
    )
    return skill_dir


def test_accepts_a_valid_reserved_local_skill_and_ignores_other_skill_directories(
    tmp_path: Path,
):
    skills_root = tmp_path / "skills"
    valid_skill = _write_skill(skills_root, "wild-bunch-valid")
    references_dir = valid_skill / "references"
    references_dir.mkdir()
    (references_dir / "notes.md").write_text("reference", encoding="utf-8")
    _write_skill(skills_root, "other-skill")

    assert validate_repo_local_skills(skills_root) == []


def test_rejects_reserved_local_skill_directory_without_an_entrypoint(tmp_path: Path):
    skills_root = tmp_path / "skills"
    (skills_root / "wild-bunch-missing-entrypoint").mkdir(parents=True)

    assert validate_repo_local_skills(skills_root) == [
        "wild-bunch-missing-entrypoint: SKILL.md is required"
    ]


def test_rejects_reserved_local_skill_directory_without_lowercase_hyphen_name(
    tmp_path: Path,
):
    skills_root = tmp_path / "skills"
    skill_dir = _write_skill(skills_root, "wild-bunch-Invalid_Name")
    references_dir = skill_dir / "references"
    references_dir.mkdir()
    (references_dir / "notes.md").write_text("reference", encoding="utf-8")

    assert validate_repo_local_skills(skills_root) == [
        "wild-bunch-Invalid_Name: directory name must use lowercase-hyphen format"
    ]


@pytest.mark.parametrize(
    ("directory_name", "name", "expected_error"),
    [
        ("wild-bunch-directory-mismatch", "wild-bunch-another-name", "name must match"),
        (
            "wild-bunch-bad-description",
            None,
            "description must begin with 'Use when'",
        ),
    ],
)
def test_rejects_invalid_skill_name_and_description_contract(
    tmp_path: Path,
    directory_name: str,
    name: str | None,
    expected_error: str,
):
    skills_root = tmp_path / "skills"
    skill_dir = _write_skill(
        skills_root,
        directory_name,
        name=name,
        description=(
            "Should not be accepted."
            if "description" in directory_name
            else "Use when validating Wild Bunch local skills."
        ),
    )
    references_dir = skill_dir / "references"
    references_dir.mkdir()
    (references_dir / "notes.md").write_text("reference", encoding="utf-8")

    assert expected_error in validate_repo_local_skills(skills_root)[0]


@pytest.mark.parametrize(
    ("missing_key", "expected_error"),
    [
        ("status", "metadata.status is required"),
        ("scope", "metadata.scope is required"),
        ("use_when", "metadata.use_when is required"),
        ("do_not_use_when", "metadata.do_not_use_when is required"),
    ],
)
def test_requires_each_repo_local_metadata_field(
    tmp_path: Path, missing_key: str, expected_error: str
):
    skills_root = tmp_path / "skills"
    metadata_lines = {
        "status": "  status: active",
        "scope": "  scope: Validate reserved repo-local Wild Bunch skills.",
        "use_when": "  use_when:\n    - Use when the Wild Bunch repo defines local skill wrappers.",
        "do_not_use_when": "  do_not_use_when:\n    - Do not use when validating non-reserved directories.",
    }
    metadata = "\n".join(
        ["metadata:"]
        + [
            line
            for key, line in metadata_lines.items()
            if key != missing_key
        ]
    )
    skill_dir = _write_skill(
        skills_root,
        f"wild-bunch-missing-{missing_key.replace('_', '-')}",
        metadata=metadata,
    )
    references_dir = skill_dir / "references"
    references_dir.mkdir()
    (references_dir / "notes.md").write_text("reference", encoding="utf-8")

    assert expected_error in validate_repo_local_skills(skills_root)[0]


def test_rejects_marketplace_provenance_fields(tmp_path: Path):
    skills_root = tmp_path / "skills"
    metadata = "\n".join(
        [
            "metadata:",
            "  status: active",
            "  scope: Validate reserved repo-local Wild Bunch skills.",
            "  use_when:",
            "    - Use when the Wild Bunch repo defines local skill wrappers.",
            "  do_not_use_when:",
            "    - Do not use when validating non-reserved directories.",
            "  source-id: marketplace-skill",
            "  source-path: skills/marketplace-skill/SKILL.md",
            "  provenance-name: Marketplace Skill",
            "  source-category: first_party",
            "  owner: Harley Bartles",
        ]
    )
    skill_dir = _write_skill(
        skills_root,
        "wild-bunch-marketplace-provenance",
        metadata=metadata,
    )
    references_dir = skill_dir / "references"
    references_dir.mkdir()
    (references_dir / "notes.md").write_text("reference", encoding="utf-8")

    errors = validate_repo_local_skills(skills_root)

    assert any("metadata.source-id is forbidden" in error for error in errors)
    assert any("metadata.source-path is forbidden" in error for error in errors)
    assert any("metadata.provenance-name is forbidden" in error for error in errors)
    assert any("metadata.source-category is forbidden" in error for error in errors)
    assert any("metadata.owner is forbidden" in error for error in errors)


def test_rejects_reserved_openai_agents_yaml(tmp_path: Path):
    skills_root = tmp_path / "skills"
    skill_dir = _write_skill(skills_root, "wild-bunch-openai-agents")
    references_dir = skill_dir / "references"
    references_dir.mkdir()
    (references_dir / "notes.md").write_text("reference", encoding="utf-8")
    agents_dir = skill_dir / "agents"
    agents_dir.mkdir()
    (agents_dir / "openai.yaml").write_text("name: reserved", encoding="utf-8")

    assert "agents/openai.yaml is forbidden" in validate_repo_local_skills(skills_root)[0]


def test_rejects_malformed_yaml_frontmatter(tmp_path: Path):
    skills_root = tmp_path / "skills"
    skill_dir = skills_root / "wild-bunch-malformed-yaml"
    skill_dir.mkdir(parents=True)
    (skill_dir / "SKILL.md").write_text(
        "\n".join(
            [
                "---",
                "name: wild-bunch-malformed-yaml",
                "description: Use when validating Wild Bunch local skills.",
                "metadata:",
                "  status: active",
                "  use_when: [broken",
                "---",
                "",
                "body",
            ]
        ),
        encoding="utf-8",
    )

    assert "frontmatter YAML is invalid" in validate_repo_local_skills(skills_root)[0]


def test_rejects_missing_relative_markdown_references(tmp_path: Path):
    skills_root = tmp_path / "skills"
    _write_skill(
        skills_root,
        "wild-bunch-broken-link",
        body="See [missing](references/missing.md).\n",
    )

    assert "missing Markdown reference" in validate_repo_local_skills(skills_root)[0]


def test_rejects_body_over_word_limit(tmp_path: Path):
    skills_root = tmp_path / "skills"
    skill_dir = _write_skill(
        skills_root,
        "wild-bunch-too-wordy",
        body=("word " * 501).strip(),
    )
    references_dir = skill_dir / "references"
    references_dir.mkdir()
    (references_dir / "notes.md").write_text("reference", encoding="utf-8")

    assert "body exceeds 500 words" in validate_repo_local_skills(skills_root)[0]
