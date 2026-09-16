"""Contracts for hosted CI parity with the canonical repository check."""

from pathlib import Path
import json


ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
TRACKED_HOOK = ROOT / "githooks" / "pre-commit"
UPSTREAM_HOOK = ROOT / ".agents" / "skills" / "repo-standards" / "templates" / "pre-commit"


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


def test_pre_commit_hook_has_tracked_custody() -> None:
    assert TRACKED_HOOK.read_bytes() == UPSTREAM_HOOK.read_bytes()


def test_hosted_ci_executes_the_tracked_hook() -> None:
    text = WORKFLOW.read_text(encoding="utf-8")

    assert "fetch-depth: 2" in text
    assert "REPO_STANDARDS_HOSTED_COMMIT: HEAD" in text
    assert "run: githooks/pre-commit" in text
    assert "cp githooks/pre-commit" not in text
