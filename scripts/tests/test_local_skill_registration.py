"""Contracts for explicitly registered repository-local skills."""

import json
from pathlib import Path

import yaml


REPO_ROOT = Path(__file__).resolve().parents[2]
MARKETPLACE_PATH = REPO_ROOT / ".agents" / "plugins" / "marketplace.json"
PROVENANCE_PATH = REPO_ROOT / ".agents" / "skills" / ".provenance.json"
SKILLS_ROOT = REPO_ROOT / ".agents" / "skills"


def _frontmatter_name(skill_path: Path) -> str:
    text = skill_path.read_text(encoding="utf-8")
    _, frontmatter, _ = text.split("---", 2)
    return yaml.safe_load(frontmatter)["name"]


def test_local_skill_custody_uses_exact_registration_only():
    marketplace = json.loads(MARKETPLACE_PATH.read_text(encoding="utf-8"))
    provenance = json.loads(PROVENANCE_PATH.read_text(encoding="utf-8"))
    registered = marketplace["repo"]["local_skills"]

    assert sorted(registered) == sorted(provenance["localSkills"])
    assert len(registered) == len(set(registered))

    for name in registered:
        skill_path = SKILLS_ROOT / name / "SKILL.md"
        assert skill_path.is_file(), f"registered local skill is missing: {name}"
        assert _frontmatter_name(skill_path) == name

    for suffix in ("py", "ps1", "sh"):
        assert not (REPO_ROOT / "scripts" / f"validate_local_skills_extra.{suffix}").exists()


def test_marketplace_manifest_is_configuration_not_narrative():
    marketplace = json.loads(MARKETPLACE_PATH.read_text(encoding="utf-8"))

    assert "notes" not in marketplace
