"""Tests for install_agent_skills.py"""

import json
import shutil
from pathlib import Path
from tempfile import TemporaryDirectory

import pytest

# Import the module under test
import sys
sys.path.insert(0, str(Path(__file__).parent.parent))
from install_agent_skills import (
    _can_skip_sync,
    _load_json,
    _files_identical,
    _load_provenance,
    _has_skill_dirs,
    main,
)


def test_configured_plugins_are_part_of_the_sync_skip_condition():
    provenance = {
        "sha": "a" * 40,
        "syncedPlugins": ["old-plugin"],
        "syncedSkillNames": ["old-skill"],
        "syncedSkills": 1,
        "syncedSkillHashes": {"old-skill": "hash"},
    }

    assert _can_skip_sync(
        provenance, "a" * 40, ["old-plugin"], {"old-skill"}, {"old-skill": "hash"}
    )
    assert not _can_skip_sync(
        provenance,
        "a" * 40,
        ["replacement-plugin"],
        {"old-skill"},
        {"old-skill": "hash"},
    )
    assert not _can_skip_sync(
        provenance,
        "a" * 40,
        ["old-plugin"],
        {"replacement-skill"},
        {"replacement-skill": "hash"},
    )
    assert not _can_skip_sync(
        provenance,
        "a" * 40,
        ["old-plugin"],
        {"old-skill"},
        {"old-skill": "changed-hash"},
    )


def test_sync_cli_migrates_legacy_provenance_without_pruning_unclassified_skills(monkeypatch):
    """Legacy provenance must record generated names without deleting unknown skills."""
    with TemporaryDirectory() as tmpdir:
        repo_root = Path(tmpdir) / "repo"
        marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
        marketplace_path.parent.mkdir(parents=True)
        marketplace_path.write_text(
            json.dumps(
                {
                    "plugins": [
                        {
                            "name": "marketplace-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        }
                    ]
                }
            )
        )

        submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
        plugins_root = submodule_root / "codex-marketplace" / "plugins"
        source_skill = plugins_root / "marketplace-plugin" / "skills" / "vendored-skill"
        source_skill.mkdir(parents=True)
        (source_skill / "SKILL.md").write_text("# Vendored skill")

        skills_root = repo_root / ".agents" / "skills"
        vendored_skill = skills_root / "vendored-skill"
        shutil.copytree(source_skill, vendored_skill)
        unclassified_legacy_skill = skills_root / "removed-legacy-skill"
        unclassified_legacy_skill.mkdir()
        (unclassified_legacy_skill / "SKILL.md").write_text("# Legacy skill")
        provenance_path = skills_root / ".provenance.json"
        provenance_path.write_text(
            json.dumps(
                {
                    "sha": "marketplace-sha",
                    "syncedPlugins": ["marketplace-plugin"],
                    "syncedSkills": 2,
                }
            )
        )

        import install_agent_skills

        monkeypatch.setattr(install_agent_skills, "REPO_ROOT", repo_root)
        monkeypatch.setattr(install_agent_skills, "MARKETPLACE_JSON_PATH", marketplace_path)
        monkeypatch.setattr(install_agent_skills, "SUBMODULE_ROOT", submodule_root)
        monkeypatch.setattr(install_agent_skills, "PLUGINS_ROOT", plugins_root)
        monkeypatch.setattr(install_agent_skills, "SKILLS_ROOT", skills_root)
        monkeypatch.setattr(install_agent_skills, "PROVENANCE_PATH", provenance_path)
        monkeypatch.setattr(install_agent_skills, "_get_submodule_sha", lambda: "marketplace-sha")
        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py"])

        assert main() == 0
        assert unclassified_legacy_skill.is_dir()
        assert json.loads(provenance_path.read_text())["syncedSkillNames"] == ["vendored-skill"]
        assert b"\r\n" not in provenance_path.read_bytes()

        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py", "--check"])
        assert main() == 0


def test_sync_cli_converges_when_a_default_plugin_has_no_skills_directory(monkeypatch):
    """Configured defaults without skills must still be recorded in provenance."""
    with TemporaryDirectory() as tmpdir:
        repo_root = Path(tmpdir) / "repo"
        marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
        marketplace_path.parent.mkdir(parents=True)
        marketplace_path.write_text(
            json.dumps(
                {
                    "plugins": [
                        {
                            "name": "skills-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        },
                        {
                            "name": "tool-only-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        },
                    ]
                }
            )
        )

        submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
        plugins_root = submodule_root / "codex-marketplace" / "plugins"
        source_skill = plugins_root / "skills-plugin" / "skills" / "vendored-skill"
        source_skill.mkdir(parents=True)
        (source_skill / "SKILL.md").write_text("# Vendored skill")
        (plugins_root / "tool-only-plugin").mkdir()

        skills_root = repo_root / ".agents" / "skills"
        provenance_path = skills_root / ".provenance.json"

        import install_agent_skills

        monkeypatch.setattr(install_agent_skills, "REPO_ROOT", repo_root)
        monkeypatch.setattr(install_agent_skills, "MARKETPLACE_JSON_PATH", marketplace_path)
        monkeypatch.setattr(install_agent_skills, "SUBMODULE_ROOT", submodule_root)
        monkeypatch.setattr(install_agent_skills, "PLUGINS_ROOT", plugins_root)
        monkeypatch.setattr(install_agent_skills, "SKILLS_ROOT", skills_root)
        monkeypatch.setattr(install_agent_skills, "PROVENANCE_PATH", provenance_path)
        monkeypatch.setattr(install_agent_skills, "_get_submodule_sha", lambda: "marketplace-sha")
        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py"])

        assert main() == 0
        assert json.loads(provenance_path.read_text())["syncedPlugins"] == [
            "skills-plugin",
            "tool-only-plugin",
        ]

        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py", "--check"])
        assert main() == 0


def test_sync_cli_rejects_a_default_plugin_missing_from_the_source(monkeypatch, capsys):
    """A missing configured plugin is not an intentional tool-only plugin."""
    with TemporaryDirectory() as tmpdir:
        repo_root = Path(tmpdir) / "repo"
        marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
        marketplace_path.parent.mkdir(parents=True)
        marketplace_path.write_text(
            json.dumps(
                {
                    "plugins": [
                        {
                            "name": "missing-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        }
                    ]
                }
            )
        )

        submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
        plugins_root = submodule_root / "codex-marketplace" / "plugins"
        plugins_root.mkdir(parents=True)

        import install_agent_skills

        monkeypatch.setattr(install_agent_skills, "REPO_ROOT", repo_root)
        monkeypatch.setattr(install_agent_skills, "MARKETPLACE_JSON_PATH", marketplace_path)
        monkeypatch.setattr(install_agent_skills, "SUBMODULE_ROOT", submodule_root)
        monkeypatch.setattr(install_agent_skills, "PLUGINS_ROOT", plugins_root)
        monkeypatch.setattr(install_agent_skills, "SKILLS_ROOT", repo_root / ".agents" / "skills")
        monkeypatch.setattr(
            install_agent_skills,
            "PROVENANCE_PATH",
            repo_root / ".agents" / "skills" / ".provenance.json",
        )
        monkeypatch.setattr(install_agent_skills, "_get_submodule_sha", lambda: "marketplace-sha")
        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py"])

        assert main() == 1
        assert "missing-plugin" in capsys.readouterr().out


def test_sync_cli_rejects_a_source_skill_without_an_entrypoint(monkeypatch, capsys):
    """A malformed source skill must fail instead of creating perpetual drift."""
    with TemporaryDirectory() as tmpdir:
        repo_root = Path(tmpdir) / "repo"
        marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
        marketplace_path.parent.mkdir(parents=True)
        marketplace_path.write_text(
            json.dumps(
                {
                    "plugins": [
                        {
                            "name": "marketplace-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        }
                    ]
                }
            )
        )

        submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
        plugins_root = submodule_root / "codex-marketplace" / "plugins"
        (plugins_root / "marketplace-plugin" / "skills" / "malformed-skill").mkdir(
            parents=True
        )

        import install_agent_skills

        monkeypatch.setattr(install_agent_skills, "REPO_ROOT", repo_root)
        monkeypatch.setattr(install_agent_skills, "MARKETPLACE_JSON_PATH", marketplace_path)
        monkeypatch.setattr(install_agent_skills, "SUBMODULE_ROOT", submodule_root)
        monkeypatch.setattr(install_agent_skills, "PLUGINS_ROOT", plugins_root)
        monkeypatch.setattr(install_agent_skills, "SKILLS_ROOT", repo_root / ".agents" / "skills")
        monkeypatch.setattr(
            install_agent_skills,
            "PROVENANCE_PATH",
            repo_root / ".agents" / "skills" / ".provenance.json",
        )
        monkeypatch.setattr(install_agent_skills, "_get_submodule_sha", lambda: "marketplace-sha")
        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py"])

        assert main() == 1
        assert "malformed-skill" in capsys.readouterr().out


def test_check_cli_reports_provenance_drift_after_a_config_only_plugin_change(monkeypatch, capsys):
    """A new plugin name must require a provenance refresh even when skills match."""
    with TemporaryDirectory() as tmpdir:
        repo_root = Path(tmpdir) / "repo"
        marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
        marketplace_path.parent.mkdir(parents=True)
        marketplace_path.write_text(
            json.dumps(
                {
                    "plugins": [
                        {
                            "name": "replacement-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        }
                    ]
                }
            )
        )

        submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
        plugins_root = submodule_root / "codex-marketplace" / "plugins"
        source_skill = plugins_root / "replacement-plugin" / "skills" / "shared-skill"
        source_skill.mkdir(parents=True)
        (source_skill / "SKILL.md").write_text("# Shared skill")

        skills_root = repo_root / ".agents" / "skills"
        skills_root.mkdir(parents=True)
        destination_skill = skills_root / "shared-skill"
        shutil.copytree(source_skill, destination_skill)
        provenance_path = skills_root / ".provenance.json"
        provenance_path.write_text(
            json.dumps(
                {
                    "sha": "marketplace-sha",
                    "syncedPlugins": ["removed-plugin"],
                    "syncedSkills": 1,
                }
            )
        )

        import install_agent_skills

        monkeypatch.setattr(install_agent_skills, "REPO_ROOT", repo_root)
        monkeypatch.setattr(install_agent_skills, "MARKETPLACE_JSON_PATH", marketplace_path)
        monkeypatch.setattr(install_agent_skills, "SUBMODULE_ROOT", submodule_root)
        monkeypatch.setattr(install_agent_skills, "PLUGINS_ROOT", plugins_root)
        monkeypatch.setattr(install_agent_skills, "SKILLS_ROOT", skills_root)
        monkeypatch.setattr(install_agent_skills, "PROVENANCE_PATH", provenance_path)
        monkeypatch.setattr(install_agent_skills, "_get_submodule_sha", lambda: "marketplace-sha")
        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py", "--check"])

        assert main() == 1

        output = capsys.readouterr().out
        assert "CHECK: Would update marketplace skill provenance" in output
        assert "CHECK: Changes would be made" in output


def test_check_cli_reports_mismatched_recorded_skill_names(monkeypatch, capsys):
    """A corrupt generated-skill baseline must not suppress a required sync."""
    with TemporaryDirectory() as tmpdir:
        repo_root = Path(tmpdir) / "repo"
        marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
        marketplace_path.parent.mkdir(parents=True)
        marketplace_path.write_text(
            json.dumps(
                {
                    "plugins": [
                        {
                            "name": "marketplace-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        }
                    ]
                }
            )
        )

        submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
        plugins_root = submodule_root / "codex-marketplace" / "plugins"
        source_skill = plugins_root / "marketplace-plugin" / "skills" / "vendored-skill"
        source_skill.mkdir(parents=True)
        (source_skill / "SKILL.md").write_text("# Vendored skill")

        skills_root = repo_root / ".agents" / "skills"
        shutil.copytree(source_skill, skills_root / "vendored-skill")
        provenance_path = skills_root / ".provenance.json"
        provenance_path.write_text(
            json.dumps(
                {
                    "sha": "marketplace-sha",
                    "syncedPlugins": ["marketplace-plugin"],
                    "syncedSkills": 1,
                    "syncedSkillNames": ["wrong-skill"],
                }
            )
        )

        import install_agent_skills

        monkeypatch.setattr(install_agent_skills, "REPO_ROOT", repo_root)
        monkeypatch.setattr(install_agent_skills, "MARKETPLACE_JSON_PATH", marketplace_path)
        monkeypatch.setattr(install_agent_skills, "SUBMODULE_ROOT", submodule_root)
        monkeypatch.setattr(install_agent_skills, "PLUGINS_ROOT", plugins_root)
        monkeypatch.setattr(install_agent_skills, "SKILLS_ROOT", skills_root)
        monkeypatch.setattr(install_agent_skills, "PROVENANCE_PATH", provenance_path)
        monkeypatch.setattr(install_agent_skills, "_get_submodule_sha", lambda: "marketplace-sha")
        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py", "--check"])

        assert main() == 1

        output = capsys.readouterr().out
        assert "CHECK: Would update marketplace skill provenance" in output
        assert "CHECK: Changes would be made" in output


def test_check_cli_reports_mismatched_recorded_skill_count(monkeypatch, capsys):
    """A corrupt generated-skill count must not suppress a provenance refresh."""
    with TemporaryDirectory() as tmpdir:
        repo_root = Path(tmpdir) / "repo"
        marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
        marketplace_path.parent.mkdir(parents=True)
        marketplace_path.write_text(
            json.dumps(
                {
                    "plugins": [
                        {
                            "name": "marketplace-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        }
                    ]
                }
            )
        )

        submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
        plugins_root = submodule_root / "codex-marketplace" / "plugins"
        source_skill = plugins_root / "marketplace-plugin" / "skills" / "vendored-skill"
        source_skill.mkdir(parents=True)
        (source_skill / "SKILL.md").write_text("# Vendored skill")

        skills_root = repo_root / ".agents" / "skills"
        shutil.copytree(source_skill, skills_root / "vendored-skill")
        provenance_path = skills_root / ".provenance.json"
        provenance_path.write_text(
            json.dumps(
                {
                    "sha": "marketplace-sha",
                    "syncedPlugins": ["marketplace-plugin"],
                    "syncedSkills": 2,
                    "syncedSkillNames": ["vendored-skill"],
                }
            )
        )

        import install_agent_skills

        monkeypatch.setattr(install_agent_skills, "REPO_ROOT", repo_root)
        monkeypatch.setattr(install_agent_skills, "MARKETPLACE_JSON_PATH", marketplace_path)
        monkeypatch.setattr(install_agent_skills, "SUBMODULE_ROOT", submodule_root)
        monkeypatch.setattr(install_agent_skills, "PLUGINS_ROOT", plugins_root)
        monkeypatch.setattr(install_agent_skills, "SKILLS_ROOT", skills_root)
        monkeypatch.setattr(install_agent_skills, "PROVENANCE_PATH", provenance_path)
        monkeypatch.setattr(install_agent_skills, "_get_submodule_sha", lambda: "marketplace-sha")
        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py", "--check"])

        assert main() == 1

        output = capsys.readouterr().out
        assert "CHECK: Would update marketplace skill provenance" in output
        assert "CHECK: Changes would be made" in output


def test_check_cli_reports_changed_vendored_skill_content(monkeypatch, capsys):
    """Matching provenance must not hide a changed vendored skill projection."""
    with TemporaryDirectory() as tmpdir:
        repo_root = Path(tmpdir) / "repo"
        marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
        marketplace_path.parent.mkdir(parents=True)
        marketplace_path.write_text(
            json.dumps(
                {
                    "plugins": [
                        {
                            "name": "marketplace-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        }
                    ]
                }
            )
        )

        submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
        plugins_root = submodule_root / "codex-marketplace" / "plugins"
        source_skill = plugins_root / "marketplace-plugin" / "skills" / "vendored-skill"
        source_skill.mkdir(parents=True)
        (source_skill / "SKILL.md").write_text("# Source skill")

        skills_root = repo_root / ".agents" / "skills"
        destination_skill = skills_root / "vendored-skill"
        destination_skill.mkdir(parents=True)
        (destination_skill / "SKILL.md").write_text("# Changed projection")
        provenance_path = skills_root / ".provenance.json"
        provenance_path.write_text(
            json.dumps(
                {
                    "sha": "marketplace-sha",
                    "syncedPlugins": ["marketplace-plugin"],
                    "syncedSkills": 1,
                    "syncedSkillNames": ["vendored-skill"],
                }
            )
        )

        import install_agent_skills

        monkeypatch.setattr(install_agent_skills, "REPO_ROOT", repo_root)
        monkeypatch.setattr(install_agent_skills, "MARKETPLACE_JSON_PATH", marketplace_path)
        monkeypatch.setattr(install_agent_skills, "SUBMODULE_ROOT", submodule_root)
        monkeypatch.setattr(install_agent_skills, "PLUGINS_ROOT", plugins_root)
        monkeypatch.setattr(install_agent_skills, "SKILLS_ROOT", skills_root)
        monkeypatch.setattr(install_agent_skills, "PROVENANCE_PATH", provenance_path)
        monkeypatch.setattr(install_agent_skills, "_get_submodule_sha", lambda: "marketplace-sha")
        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py", "--check"])

        assert main() == 1

        output = capsys.readouterr().out
        assert "CHECK: Would copy skill: vendored-skill" in output
        assert "CHECK: Changes would be made" in output


def test_check_cli_reports_missing_vendored_skills_when_custody_skills_remain(monkeypatch, capsys):
    """A custody skill must not make a missing vendored projection look current."""
    with TemporaryDirectory() as tmpdir:
        repo_root = Path(tmpdir) / "repo"
        marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
        marketplace_path.parent.mkdir(parents=True)
        marketplace_path.write_text(
            json.dumps(
                {
                    "plugins": [
                        {
                            "name": "marketplace-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        }
                    ]
                }
            )
        )

        submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
        plugins_root = submodule_root / "codex-marketplace" / "plugins"
        source_skill = plugins_root / "marketplace-plugin" / "skills" / "vendored-skill"
        source_skill.mkdir(parents=True)
        (source_skill / "SKILL.md").write_text("# Vendored skill")

        skills_root = repo_root / ".agents" / "skills"
        custody_skill = skills_root / "custody-skill"
        custody_skill.mkdir(parents=True)
        (custody_skill / "SKILL.md").write_text("# Custody skill")
        provenance_path = skills_root / ".provenance.json"
        provenance_path.write_text(
            json.dumps(
                {
                    "sha": "marketplace-sha",
                    "syncedPlugins": ["marketplace-plugin"],
                    "syncedSkills": 1,
                }
            )
        )

        import install_agent_skills

        monkeypatch.setattr(install_agent_skills, "REPO_ROOT", repo_root)
        monkeypatch.setattr(install_agent_skills, "MARKETPLACE_JSON_PATH", marketplace_path)
        monkeypatch.setattr(install_agent_skills, "SUBMODULE_ROOT", submodule_root)
        monkeypatch.setattr(install_agent_skills, "PLUGINS_ROOT", plugins_root)
        monkeypatch.setattr(install_agent_skills, "SKILLS_ROOT", skills_root)
        monkeypatch.setattr(install_agent_skills, "PROVENANCE_PATH", provenance_path)
        monkeypatch.setattr(install_agent_skills, "_get_submodule_sha", lambda: "marketplace-sha")
        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py", "--check"])

        assert main() == 1

        output = capsys.readouterr().out
        assert "CHECK: Would copy skill: vendored-skill" in output
        assert "CHECK: Changes would be made" in output


def test_sync_cli_preserves_custody_skills_while_repairing_vendored_projection(monkeypatch):
    """Normal sync must not remove local custody while restoring marketplace skills."""
    with TemporaryDirectory() as tmpdir:
        repo_root = Path(tmpdir) / "repo"
        marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
        marketplace_path.parent.mkdir(parents=True)
        marketplace_path.write_text(
            json.dumps(
                {
                    "plugins": [
                        {
                            "name": "marketplace-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        }
                    ]
                }
            )
        )

        submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
        plugins_root = submodule_root / "codex-marketplace" / "plugins"
        source_skill = plugins_root / "marketplace-plugin" / "skills" / "vendored-skill"
        source_skill.mkdir(parents=True)
        (source_skill / "SKILL.md").write_text("# Vendored skill")

        skills_root = repo_root / ".agents" / "skills"
        custody_skill = skills_root / "custody-skill"
        custody_skill.mkdir(parents=True)
        (custody_skill / "SKILL.md").write_text("# Custody skill")
        provenance_path = skills_root / ".provenance.json"
        provenance_path.write_text(
            json.dumps(
                {
                    "sha": "marketplace-sha",
                    "syncedPlugins": ["marketplace-plugin"],
                    "syncedSkills": 1,
                    "syncedSkillNames": ["vendored-skill"],
                }
            )
        )

        import install_agent_skills

        monkeypatch.setattr(install_agent_skills, "REPO_ROOT", repo_root)
        monkeypatch.setattr(install_agent_skills, "MARKETPLACE_JSON_PATH", marketplace_path)
        monkeypatch.setattr(install_agent_skills, "SUBMODULE_ROOT", submodule_root)
        monkeypatch.setattr(install_agent_skills, "PLUGINS_ROOT", plugins_root)
        monkeypatch.setattr(install_agent_skills, "SKILLS_ROOT", skills_root)
        monkeypatch.setattr(install_agent_skills, "PROVENANCE_PATH", provenance_path)
        monkeypatch.setattr(install_agent_skills, "_get_submodule_sha", lambda: "marketplace-sha")
        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py"])

        assert main() == 0
        assert (skills_root / "vendored-skill").is_dir()
        assert custody_skill.is_dir()


def test_sync_cli_does_not_overwrite_reserved_local_skill_from_marketplace(monkeypatch):
    with TemporaryDirectory() as tmpdir:
        repo_root = Path(tmpdir) / "repo"
        marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
        marketplace_path.parent.mkdir(parents=True)
        marketplace_path.write_text(
            json.dumps(
                {
                    "plugins": [
                        {
                            "name": "marketplace-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        }
                    ]
                }
            )
        )

        submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
        plugins_root = submodule_root / "codex-marketplace" / "plugins"
        source_skill = (
            plugins_root
            / "marketplace-plugin"
            / "skills"
            / "wild-bunch-project-doctrine"
        )
        source_skill.mkdir(parents=True)
        (source_skill / "SKILL.md").write_text("# Marketplace doctrine")

        skills_root = repo_root / ".agents" / "skills"
        local_skill = skills_root / "wild-bunch-project-doctrine"
        local_skill.mkdir(parents=True)
        (local_skill / "SKILL.md").write_text("# Local doctrine")
        provenance_path = skills_root / ".provenance.json"

        import install_agent_skills

        monkeypatch.setattr(install_agent_skills, "REPO_ROOT", repo_root)
        monkeypatch.setattr(install_agent_skills, "MARKETPLACE_JSON_PATH", marketplace_path)
        monkeypatch.setattr(install_agent_skills, "SUBMODULE_ROOT", submodule_root)
        monkeypatch.setattr(install_agent_skills, "PLUGINS_ROOT", plugins_root)
        monkeypatch.setattr(install_agent_skills, "SKILLS_ROOT", skills_root)
        monkeypatch.setattr(install_agent_skills, "PROVENANCE_PATH", provenance_path)
        monkeypatch.setattr(install_agent_skills, "_get_submodule_sha", lambda: "marketplace-sha")
        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py"])

        assert main() == 0
        assert (local_skill / "SKILL.md").read_text() == "# Local doctrine"
        assert "wild-bunch-project-doctrine" not in json.loads(
            provenance_path.read_text()
        )["syncedSkillNames"]


def test_sync_cli_preserves_reserved_local_skill_after_marketplace_removal(monkeypatch):
    with TemporaryDirectory() as tmpdir:
        repo_root = Path(tmpdir) / "repo"
        marketplace_path = repo_root / ".agents" / "plugins" / "marketplace.json"
        marketplace_path.parent.mkdir(parents=True)
        marketplace_path.write_text(
            json.dumps(
                {
                    "plugins": [
                        {
                            "name": "marketplace-plugin",
                            "policy": {"installation": "INSTALLED_BY_DEFAULT"},
                        }
                    ]
                }
            )
        )

        submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
        plugins_root = submodule_root / "codex-marketplace" / "plugins"
        source_skill = plugins_root / "marketplace-plugin" / "skills" / "vendored-skill"
        source_skill.mkdir(parents=True)
        (source_skill / "SKILL.md").write_text("# Vendored skill")

        skills_root = repo_root / ".agents" / "skills"
        local_skill = skills_root / "wild-bunch-project-doctrine"
        local_skill.mkdir(parents=True)
        local_skill.joinpath("SKILL.md").write_text("# Local doctrine")
        provenance_path = skills_root / ".provenance.json"
        provenance = {
            "sha": "marketplace-sha",
            "syncedPlugins": ["marketplace-plugin"],
            "syncedSkillNames": ["vendored-skill", "wild-bunch-project-doctrine"],
            "syncedSkills": 2,
            "syncedSkillHashes": {
                "vendored-skill": "old-vendored-hash",
                "wild-bunch-project-doctrine": "old-marketplace-hash",
            },
        }
        provenance_path.write_text(json.dumps(provenance))

        import install_agent_skills

        monkeypatch.setattr(install_agent_skills, "REPO_ROOT", repo_root)
        monkeypatch.setattr(install_agent_skills, "MARKETPLACE_JSON_PATH", marketplace_path)
        monkeypatch.setattr(install_agent_skills, "SUBMODULE_ROOT", submodule_root)
        monkeypatch.setattr(install_agent_skills, "PLUGINS_ROOT", plugins_root)
        monkeypatch.setattr(install_agent_skills, "SKILLS_ROOT", skills_root)
        monkeypatch.setattr(install_agent_skills, "PROVENANCE_PATH", provenance_path)
        monkeypatch.setattr(install_agent_skills, "_get_submodule_sha", lambda: "marketplace-sha")
        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py"])

        assert main() == 0
        assert local_skill.joinpath("SKILL.md").read_text() == "# Local doctrine"
        assert json.loads(provenance_path.read_text())["syncedSkillNames"] == [
            "vendored-skill"
        ]

        monkeypatch.setattr(sys, "argv", ["install_agent_skills.py", "--check"])
        assert main() == 0


class TestLoadJson:
    """Tests for _load_json function."""

    def test_load_valid_json(self):
        """Should load and parse a valid JSON file."""
        with TemporaryDirectory() as tmpdir:
            test_file = Path(tmpdir) / "test.json"
            test_data = {"key": "value", "number": 42}
            with open(test_file, "w") as f:
                json.dump(test_data, f)
            
            result = _load_json(test_file)
            assert result == test_data

    def test_load_missing_file(self):
        """Should raise FileNotFoundError for missing file."""
        with TemporaryDirectory() as tmpdir:
            test_file = Path(tmpdir) / "nonexistent.json"
            
            with pytest.raises(FileNotFoundError):
                _load_json(test_file)

    def test_load_invalid_json(self):
        """Should raise JSONDecodeError for invalid JSON."""
        with TemporaryDirectory() as tmpdir:
            test_file = Path(tmpdir) / "invalid.json"
            with open(test_file, "w") as f:
                f.write("{ invalid json }")
            
            with pytest.raises(json.JSONDecodeError):
                _load_json(test_file)


class TestFilesIdentical:
    """Tests for _files_identical function."""

    def test_identical_directories(self):
        """Should return True for directories with identical content."""
        with TemporaryDirectory() as tmpdir:
            dir1 = Path(tmpdir) / "dir1"
            dir2 = Path(tmpdir) / "dir2"
            dir1.mkdir()
            dir2.mkdir()
            
            # Create identical files
            (dir1 / "file1.txt").write_text("content")
            (dir2 / "file1.txt").write_text("content")
            (dir1 / "subdir").mkdir()
            (dir2 / "subdir").mkdir()
            (dir1 / "subdir" / "file2.txt").write_text("nested")
            (dir2 / "subdir" / "file2.txt").write_text("nested")
            
            assert _files_identical(dir1, dir2) is True

    def test_different_file_content(self):
        """Should return False for directories with different file content."""
        with TemporaryDirectory() as tmpdir:
            dir1 = Path(tmpdir) / "dir1"
            dir2 = Path(tmpdir) / "dir2"
            dir1.mkdir()
            dir2.mkdir()
            
            (dir1 / "file1.txt").write_text("content1")
            (dir2 / "file1.txt").write_text("content2")
            
            assert _files_identical(dir1, dir2) is False

    def test_different_file_structure(self):
        """Should return False for directories with different file structure."""
        with TemporaryDirectory() as tmpdir:
            dir1 = Path(tmpdir) / "dir1"
            dir2 = Path(tmpdir) / "dir2"
            dir1.mkdir()
            dir2.mkdir()
            
            (dir1 / "file1.txt").write_text("content")
            (dir2 / "file2.txt").write_text("content")
            
            assert _files_identical(dir1, dir2) is False

    def test_one_directory_missing(self):
        """Should return False if one directory doesn't exist."""
        with TemporaryDirectory() as tmpdir:
            dir1 = Path(tmpdir) / "dir1"
            dir2 = Path(tmpdir) / "dir2"
            dir1.mkdir()
            
            assert _files_identical(dir1, dir2) is False
            assert _files_identical(dir2, dir1) is False

    def test_empty_directories(self):
        """Should return True for two empty directories."""
        with TemporaryDirectory() as tmpdir:
            dir1 = Path(tmpdir) / "dir1"
            dir2 = Path(tmpdir) / "dir2"
            dir1.mkdir()
            dir2.mkdir()
            
            assert _files_identical(dir1, dir2) is True

    def test_different_file_sizes(self):
        """Should return False for files with different sizes (optimization check)."""
        with TemporaryDirectory() as tmpdir:
            dir1 = Path(tmpdir) / "dir1"
            dir2 = Path(tmpdir) / "dir2"
            dir1.mkdir()
            dir2.mkdir()
            
            (dir1 / "file1.txt").write_text("short")
            (dir2 / "file1.txt").write_text("much longer content")
            
            assert _files_identical(dir1, dir2) is False


class TestLoadProvenance:
    """Tests for _load_provenance function."""

    def test_load_existing_provenance(self):
        """Should load existing provenance file."""
        with TemporaryDirectory() as tmpdir:
            provenance_path = Path(tmpdir) / ".provenance.json"
            test_provenance = {
                "sha": "abc123",
                "syncedAt": "2026-07-09T10:00:00Z",
                "syncedPlugins": ["plugin1"],
                "syncedSkills": 5
            }
            with open(provenance_path, "w") as f:
                json.dump(test_provenance, f)
            
            # Mock the PROVENANCE_PATH
            import install_agent_skills
            original_path = install_agent_skills.PROVENANCE_PATH
            install_agent_skills.PROVENANCE_PATH = provenance_path
            
            try:
                result = _load_provenance()
                assert result == test_provenance
            finally:
                install_agent_skills.PROVENANCE_PATH = original_path

    def test_load_missing_provenance(self):
        """Should return None when provenance file doesn't exist."""
        with TemporaryDirectory() as tmpdir:
            provenance_path = Path(tmpdir) / ".provenance.json"
            
            import install_agent_skills
            original_path = install_agent_skills.PROVENANCE_PATH
            install_agent_skills.PROVENANCE_PATH = provenance_path
            
            try:
                result = _load_provenance()
                assert result is None
            finally:
                install_agent_skills.PROVENANCE_PATH = original_path

    def test_load_invalid_provenance(self):
        """Should return None for malformed provenance file."""
        with TemporaryDirectory() as tmpdir:
            provenance_path = Path(tmpdir) / ".provenance.json"
            with open(provenance_path, "w") as f:
                f.write("{ invalid json }")
            
            import install_agent_skills
            original_path = install_agent_skills.PROVENANCE_PATH
            install_agent_skills.PROVENANCE_PATH = provenance_path
            
            try:
                result = _load_provenance()
                assert result is None
            finally:
                install_agent_skills.PROVENANCE_PATH = original_path


class TestHasSkillDirs:
    """Tests for _has_skill_dirs function."""

    def test_has_skill_directories(self):
        """Should return True when skill directories exist."""
        with TemporaryDirectory() as tmpdir:
            skills_root = Path(tmpdir) / "skills"
            skills_root.mkdir()
            (skills_root / "skill1").mkdir()
            (skills_root / "skill2").mkdir()
            
            import install_agent_skills
            original_path = install_agent_skills.SKILLS_ROOT
            install_agent_skills.SKILLS_ROOT = skills_root
            
            try:
                result = _has_skill_dirs()
                assert result is True
            finally:
                install_agent_skills.SKILLS_ROOT = original_path

    def test_no_skill_directories(self):
        """Should return False when no skill directories exist."""
        with TemporaryDirectory() as tmpdir:
            skills_root = Path(tmpdir) / "skills"
            skills_root.mkdir()
            
            import install_agent_skills
            original_path = install_agent_skills.SKILLS_ROOT
            install_agent_skills.SKILLS_ROOT = skills_root
            
            try:
                result = _has_skill_dirs()
                assert result is False
            finally:
                install_agent_skills.SKILLS_ROOT = original_path

    def test_provenance_file_is_not_a_skill_directory(self):
        """Should return False when provenance is the only entry in skills root."""
        with TemporaryDirectory() as tmpdir:
            skills_root = Path(tmpdir) / "skills"
            skills_root.mkdir()
            (skills_root / ".provenance.json").write_text("{}")

            import install_agent_skills
            original_path = install_agent_skills.SKILLS_ROOT
            install_agent_skills.SKILLS_ROOT = skills_root

            try:
                assert _has_skill_dirs() is False
            finally:
                install_agent_skills.SKILLS_ROOT = original_path

    def test_skills_root_missing(self):
        """Should return False when skills root doesn't exist."""
        with TemporaryDirectory() as tmpdir:
            skills_root = Path(tmpdir) / "skills"
            
            import install_agent_skills
            original_path = install_agent_skills.SKILLS_ROOT
            install_agent_skills.SKILLS_ROOT = skills_root
            
            try:
                result = _has_skill_dirs()
                assert result is False
            finally:
                install_agent_skills.SKILLS_ROOT = original_path


class TestRegressionNoPruneUnchangedSkills:
    """Regression test for the bug where unchanged skills were incorrectly pruned.
    
    This test ensures that when skills are already synced and the script runs again
    without --force, unchanged skills are NOT pruned from the synced skills list.
    This prevents the failure mode where skills from installed plugins disappear
    on subsequent sync runs.
    """

    def test_unchanged_skills_not_pruned_on_re_sync(self):
        """Should NOT prune unchanged skills when re-syncing without --force.
        
        Regression test for the bug where skills were only added to synced_skill_names
        if they were actually copied, causing unchanged skills to be pruned on subsequent runs.
        """
        with TemporaryDirectory() as tmpdir:
            # Setup mock directory structure
            repo_root = Path(tmpdir) / "repo"
            repo_root.mkdir()
            
            marketplace_json = repo_root / ".agents" / "plugins" / "marketplace.json"
            marketplace_json.parent.mkdir(parents=True, exist_ok=True)
            
            submodule_root = repo_root / ".agents" / "plugins" / "marketplace-source"
            submodule_root.mkdir(parents=True, exist_ok=True)
            
            plugins_root = submodule_root / "codex-marketplace" / "plugins"
            plugins_root.mkdir(parents=True, exist_ok=True)
            
            skills_root = repo_root / ".agents" / "skills"
            skills_root.mkdir(parents=True, exist_ok=True)
            
            provenance_path = skills_root / ".provenance.json"
            
            # Create marketplace.json with one plugin
            marketplace_data = {
                "plugins": [
                    {
                        "name": "test-plugin",
                        "policy": {"installation": "INSTALLED_BY_DEFAULT"}
                    }
                ]
            }
            with open(marketplace_json, "w") as f:
                json.dump(marketplace_data, f)
            
            # Create plugin skills directory with one skill
            plugin_skills_dir = plugins_root / "test-plugin" / "skills"
            plugin_skills_dir.mkdir(parents=True, exist_ok=True)
            
            skill_dir = plugin_skills_dir / "test-skill"
            skill_dir.mkdir()
            (skill_dir / "SKILL.md").write_text("# Test Skill")
            
            # Create initial provenance with the skill already synced
            provenance_data = {
                "sha": "initial-sha",
                "syncedAt": "2026-07-10T00:00:00Z",
                "syncedPlugins": ["test-plugin"],
                "syncedSkills": 1
            }
            with open(provenance_path, "w") as f:
                json.dump(provenance_data, f)
            
            # Copy the skill to skills root (simulating previous sync)
            dest_skill_dir = skills_root / "test-skill"
            shutil.copytree(skill_dir, dest_skill_dir)
            
            # Mock the paths in the module
            import install_agent_skills
            original_paths = {
                "REPO_ROOT": install_agent_skills.REPO_ROOT,
                "MARKETPLACE_JSON_PATH": install_agent_skills.MARKETPLACE_JSON_PATH,
                "SUBMODULE_ROOT": install_agent_skills.SUBMODULE_ROOT,
                "PLUGINS_ROOT": install_agent_skills.PLUGINS_ROOT,
                "SKILLS_ROOT": install_agent_skills.SKILLS_ROOT,
                "PROVENANCE_PATH": install_agent_skills.PROVENANCE_PATH,
            }
            
            install_agent_skills.REPO_ROOT = repo_root
            install_agent_skills.MARKETPLACE_JSON_PATH = marketplace_json
            install_agent_skills.SUBMODULE_ROOT = submodule_root
            install_agent_skills.PLUGINS_ROOT = plugins_root
            install_agent_skills.SKILLS_ROOT = skills_root
            install_agent_skills.PROVENANCE_PATH = provenance_path
            
            try:
                # Mock _get_submodule_sha to return a different SHA (simulating submodule update)
                original_get_sha = install_agent_skills._get_submodule_sha
                install_agent_skills._get_submodule_sha = lambda: "new-sha"
                
                # Run sync without force
                skills_synced, plugins_synced, changes_made = install_agent_skills._sync_skills(
                    force=False,
                    check_mode=False
                )
                
                # Restore original function
                install_agent_skills._get_submodule_sha = original_get_sha
                
                # The skill should still be in the skills directory (not pruned)
                assert dest_skill_dir.exists(), "Unchanged skill was incorrectly pruned"
                assert (dest_skill_dir / "SKILL.md").exists()
                
                # The skill should be tracked in the new provenance
                new_provenance = _load_provenance()
                assert new_provenance is not None
                assert new_provenance.get("syncedSkills") >= 1, "Skill count dropped below expected"
                
            finally:
                # Restore original paths
                for key, value in original_paths.items():
                    setattr(install_agent_skills, key, value)
