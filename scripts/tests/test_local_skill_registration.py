"""Behavior for repository-authored skill custody."""

from pathlib import Path

import yaml


REPO_ROOT = Path(__file__).resolve().parents[2]
SKILLS_ROOT = REPO_ROOT / ".agents" / "skills"


def _frontmatter_name(skill_path: Path) -> str:
    text = skill_path.read_text(encoding="utf-8")
    _, frontmatter, _ = text.split("---", 2)
    return yaml.safe_load(frontmatter)["name"]


def test_each_repository_skill_declares_its_directory_name():
    for skill_path in sorted(SKILLS_ROOT.glob("*/SKILL.md")):
        name = skill_path.parent.name
        assert skill_path.is_file(), f"registered local skill is missing: {name}"
        assert _frontmatter_name(skill_path) == name


def test_marketplace_manifest_is_configuration_not_narrative():
    import json

    marketplace = json.loads((REPO_ROOT / ".agents/plugins/marketplace.json").read_text(encoding="utf-8"))
    assert "notes" not in marketplace
    assert "repo" not in marketplace
