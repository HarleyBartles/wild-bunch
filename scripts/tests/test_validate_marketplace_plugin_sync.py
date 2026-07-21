"""Tests for marketplace plugin sync validation."""

from pathlib import Path
import sys

import pytest

sys.path.insert(0, str(Path(__file__).parent.parent))

from validate_marketplace_plugin_sync import validate_marketplace_plugin_sync


def _marketplace(*plugin_names: str) -> dict[str, object]:
    return {
        "name": "wild-bunch",
        "plugins": [
            {
                "name": plugin_name,
                "policy": {"installation": "INSTALLED_BY_DEFAULT"},
            }
            for plugin_name in plugin_names
        ]
    }


def _provenance(plugin_names: list[str], sha: str = "a" * 40) -> dict[str, object]:
    return {
        "syncedPlugins": plugin_names,
        "syncedSkillNames": ["installed-skill"],
        "syncedSkills": 1,
        "syncedSkillHashes": {"installed-skill": "expected-hash"},
        "sha": sha,
    }


def test_accepts_provenance_that_matches_default_plugin_configuration():
    marketplace = _marketplace("first-plugin", "second-plugin")
    provenance = _provenance(["first-plugin", "second-plugin"])

    assert validate_marketplace_plugin_sync(
        marketplace, provenance, "a" * 40, {"installed-skill": "expected-hash"}
    ) == [
        "first-plugin",
        "second-plugin",
    ]


def test_rejects_stale_provenance_when_the_configured_plugin_changes():
    marketplace = _marketplace("first-plugin", "replacement-plugin")
    provenance = _provenance(["first-plugin", "removed-plugin"])

    with pytest.raises(ValueError, match="syncedPlugins"):
        validate_marketplace_plugin_sync(
            marketplace, provenance, "a" * 40, {"installed-skill": "expected-hash"}
        )


def test_rejects_duplicate_default_plugin_names():
    marketplace = _marketplace("duplicate-plugin", "duplicate-plugin")
    provenance = _provenance(["duplicate-plugin", "duplicate-plugin"])

    with pytest.raises(ValueError, match="unique"):
        validate_marketplace_plugin_sync(
            marketplace, provenance, "a" * 40, {"installed-skill": "expected-hash"}
        )


def test_rejects_provenance_from_a_different_submodule_commit():
    marketplace = _marketplace("only-plugin")
    provenance = _provenance(["only-plugin"], sha="a" * 40)

    with pytest.raises(ValueError, match="gitlink"):
        validate_marketplace_plugin_sync(
            marketplace, provenance, "b" * 40, {"installed-skill": "expected-hash"}
        )


def test_rejects_a_manifest_for_a_different_repository():
    marketplace = _marketplace("only-plugin")
    marketplace["name"] = "other-repository"
    provenance = _provenance(["only-plugin"])

    with pytest.raises(ValueError, match="wild-bunch"):
        validate_marketplace_plugin_sync(
            marketplace, provenance, "a" * 40, {"installed-skill": "expected-hash"}
        )


def test_rejects_missing_or_unrecorded_generated_skill_provenance():
    marketplace = _marketplace("only-plugin")
    provenance = _provenance(["only-plugin"])
    provenance["syncedSkillNames"] = ["missing-skill"]

    with pytest.raises(ValueError, match="missing-skill"):
        validate_marketplace_plugin_sync(marketplace, provenance, "a" * 40, set())


def test_rejects_invalid_generated_skill_provenance():
    marketplace = _marketplace("only-plugin")
    provenance = _provenance(["only-plugin"])
    del provenance["syncedSkillNames"]

    with pytest.raises(ValueError, match="syncedSkillNames"):
        validate_marketplace_plugin_sync(
            marketplace, provenance, "a" * 40, {"installed-skill": "expected-hash"}
        )


def test_installed_skill_hashes_require_a_skill_entrypoint(monkeypatch, tmp_path):
    import validate_marketplace_plugin_sync as plugin_sync

    skills_root = tmp_path / "skills"
    (skills_root / "invalid-skill").mkdir(parents=True)
    valid_skill = skills_root / "valid-skill"
    valid_skill.mkdir()
    (valid_skill / "SKILL.md").write_text("# Valid skill")
    monkeypatch.setattr(plugin_sync, "SKILLS_ROOT", skills_root)

    assert set(plugin_sync._installed_skill_hashes()) == {"valid-skill"}


def test_rejects_changed_generated_skill_content():
    marketplace = _marketplace("only-plugin")
    provenance = _provenance(["only-plugin"])

    with pytest.raises(ValueError, match="content"):
        validate_marketplace_plugin_sync(
            marketplace, provenance, "a" * 40, {"installed-skill": "changed-hash"}
        )
