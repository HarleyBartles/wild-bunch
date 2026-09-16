from pathlib import Path
import re
import unittest


REPO_ROOT = Path(__file__).resolve().parents[2]
RUNBOOK_HEADINGS = (
    "## When",
    "## Required skills",
    "## Composition",
    "## Doctrine and contracts",
    "## Local commands and paths",
    "## Evidence contract",
    "## Prohibited combinations",
)


def section(text: str, heading: str) -> str:
    match = re.search(
        rf"^{re.escape(heading)}\s*$\n(.*?)(?=^## |\Z)",
        text,
        flags=re.MULTILINE | re.DOTALL,
    )
    if match is None:
        raise AssertionError(f"missing section {heading}")
    return match.group(1)


class RepoGuidanceContractsTests(unittest.TestCase):
    def test_authored_runbooks_use_the_composition_manifest(self) -> None:
        runbooks = sorted((REPO_ROOT / ".agents" / "runbooks").glob("*.md"))
        authored = [path for path in runbooks if path.name != "INDEX.md"]

        self.assertTrue(authored)
        for path in authored:
            text = path.read_text(encoding="utf-8")
            headings = re.findall(r"^## .+$", text, flags=re.MULTILINE)
            self.assertEqual(
                list(RUNBOOK_HEADINGS), headings, path.relative_to(REPO_ROOT)
            )

            required = section(text, "## Required skills")
            composition = section(text, "## Composition")
            evidence = section(text, "## Evidence contract")
            self.assertRegex(required, r"(?m)^- `/[a-z0-9-]+`", path.relative_to(REPO_ROOT))
            declared_skills = set(re.findall(r"`/([a-z0-9-]+)`", required))
            composed_skills = set(re.findall(r"`/([a-z0-9-]+)`", composition))
            self.assertEqual(
                set(), composed_skills - declared_skills, path.relative_to(REPO_ROOT)
            )
            self.assertGreaterEqual(
                len(re.findall(r"(?m)^\d+\. ", composition)),
                2,
                path.relative_to(REPO_ROOT),
            )
            self.assertRegex(evidence, r"(?m)^- \[ \] ", path.relative_to(REPO_ROOT))

    def test_unslop_profiles_live_under_contracts(self) -> None:
        self.assertEqual([], list((REPO_ROOT / ".agents" / "unslop").rglob("*.md")))
        self.assertEqual(
            [],
            list(
                (
                    REPO_ROOT / "src" / "WildBunch.Web" / ".agents" / "unslop"
                ).rglob("*.md")
            ),
        )
        self.assertTrue((REPO_ROOT / ".agents" / "contracts" / "unslop").is_dir())
        self.assertTrue(
            (
                REPO_ROOT
                / "src"
                / "WildBunch.Web"
                / ".agents"
                / "contracts"
                / "unslop"
                / "play-surface-ui.md"
            ).is_file()
        )

    def test_completed_artifacts_doctrine_matches_the_standard_template(self) -> None:
        doctrine = REPO_ROOT / ".agents" / "doctrine" / "completed-artifacts.md"
        template = (
            REPO_ROOT
            / ".agents"
            / "skills"
            / "repo-standards"
            / "templates"
            / "completed-artifacts.md"
        )

        self.assertEqual(template.read_bytes(), doctrine.read_bytes())


if __name__ == "__main__":
    unittest.main()
