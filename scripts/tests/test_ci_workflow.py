"""Contracts for hosted CI parity with the canonical repository check."""

from pathlib import Path
import json


ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"


def test_hosted_ci_enters_through_the_declared_canonical_gate() -> None:
    text = WORKFLOW.read_text(encoding="utf-8")
    declaration = json.loads(
        (ROOT / ".agents" / "contracts" / "repo-standards-commands.json").read_text(
            encoding="utf-8"
        )
    )

    assert "run: githooks/pre-commit" in text
    assert declaration["check"] == [
        "@python",
        "tools/run.py",
        "ci",
        "--check",
        "--diagnostics",
    ]


def test_hosted_ci_executes_the_tracked_hook() -> None:
    text = WORKFLOW.read_text(encoding="utf-8")

    assert "fetch-depth: 2" in text
    assert "REPO_STANDARDS_HOSTED_COMMIT: HEAD" in text
    assert "run: githooks/pre-commit" in text
    assert "cp githooks/pre-commit" not in text


def test_generated_paths_cover_consumer_owned_refresh_outputs_without_mesh() -> None:
    declaration = json.loads(
        (ROOT / ".agents" / "contracts" / "repo-standards-commands.json").read_text(
            encoding="utf-8"
        )
    )
    assert declaration["generated_paths"] == [
        ".agents/skills/**",
        ".agents/agents/**",
        ".agents/standards/**",
        "docs/decisions/README.md",
    ]
