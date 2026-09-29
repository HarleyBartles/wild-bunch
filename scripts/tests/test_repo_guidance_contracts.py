from pathlib import Path
import json
import unittest

REPO_ROOT = Path(__file__).resolve().parents[2]


class RepoGuidanceContractsTests(unittest.TestCase):
    def test_refresh_provenance_keeps_only_declared_subscriptions_and_local_skills(self) -> None:
        manifest = json.loads((REPO_ROOT / ".agents/plugins/marketplace.json").read_text(encoding="utf-8"))
        provenance = json.loads((REPO_ROOT / ".agents/skills/.provenance.json").read_text(encoding="utf-8"))
        plugins = [plugin["name"] for plugin in manifest["plugins"]]
        self.assertEqual(["game-studio", "dotnet-pack", "architecture-pack", "frontend-pack"], plugins)
        self.assertEqual(plugins, provenance["syncedPlugins"])
        self.assertEqual(set(manifest["repo"]["local_skills"]), set(provenance["localSkills"]))
        for skill in manifest["repo"]["local_skills"]:
            self.assertTrue((REPO_ROOT / ".agents/skills" / skill / "SKILL.md").is_file(), skill)

    def test_unslop_profiles_live_under_contracts(self) -> None:
        self.assertEqual([], list((REPO_ROOT / ".agents" / "unslop").rglob("*.md")))
        self.assertEqual(
            [],
            list((REPO_ROOT / "src" / "WildBunch.Web" / ".agents" / "unslop").rglob("*.md")),
        )
        self.assertTrue((REPO_ROOT / ".agents" / "contracts" / "unslop").is_dir())
        self.assertTrue(
            (REPO_ROOT / "src" / "WildBunch.Web" / ".agents" / "contracts" / "unslop" / "play-surface-ui.md").is_file()
        )

    def test_completed_artifacts_doctrine_declares_two_slice_custody(self) -> None:
        doctrine = REPO_ROOT / ".agents" / "doctrine" / "completed-artifacts.md"
        text = doctrine.read_text(encoding="utf-8")
        self.assertIn("completed-awaiting-retirement", text)
        self.assertIn("successor slice", text)
        self.assertIn("explicit recorded decision", text)
        self.assertIn("planning-artifact lifecycle capability owns", text)


if __name__ == "__main__":
    unittest.main()
