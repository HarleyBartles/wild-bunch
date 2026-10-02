from pathlib import Path
import unittest

REPO_ROOT = Path(__file__).resolve().parents[2]


class RepoGuidanceContractsTests(unittest.TestCase):
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


if __name__ == "__main__":
    unittest.main()
