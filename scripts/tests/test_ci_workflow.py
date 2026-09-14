"""Contracts for hosted CI parity with the canonical repository check."""

from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"


def test_hosted_ci_checks_the_installed_skill_projection() -> None:
    text = WORKFLOW.read_text(encoding="utf-8")

    assert "refresh_installed_skills.py --check" in text
    assert "validate_local_skills_extra" not in text
