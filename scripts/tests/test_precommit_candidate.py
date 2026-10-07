from __future__ import annotations

import json
import os
from pathlib import Path
import shutil
import subprocess

import pytest


REPO_ROOT = Path(__file__).resolve().parents[2]
HOOK = REPO_ROOT / "githooks" / "pre-commit"


def _git(root: Path, *args: str) -> str:
    result = subprocess.run(["git", *args], cwd=root, text=True, capture_output=True, check=True)
    return result.stdout.strip()


def _fixture(root: Path) -> None:
    if shutil.which("bash") is None:
        pytest.skip("Bash is required to exercise the tracked hook")
    _git(root, "init", "-b", "main")
    _git(root, "config", "user.name", "Test")
    _git(root, "config", "user.email", "test@example.invalid")
    (root / "githooks").mkdir()
    (root / "tools").mkdir()
    (root / "githooks/pre-commit").write_bytes(HOOK.read_bytes())
    (root / ".agents/contracts").mkdir(parents=True)
    (root / ".agents/contracts/repo-standards-commands.json").write_text(
        json.dumps(
            {
                "apply": ["@python", "tools/gate.py", "--apply"],
                "check": ["@python", "tools/gate.py", "--check"],
            }
        ),
        encoding="utf-8",
    )
    (root / "tools/gate.py").write_text(
        "from pathlib import Path\n"
        "import sys\n"
        "root = Path.cwd()\n"
        "if (root / 'intent.txt').exists():\n"
        "    raise SystemExit(29)\n"
        "if '--apply' in sys.argv:\n"
        "    out = root / 'docs/decisions/README.md'\n"
        "    out.parent.mkdir(parents=True, exist_ok=True)\n"
        "    out.write_bytes(b'fresh\\n')\n"
        "elif (root / 'docs/decisions/README.md').read_bytes() != b'fresh\\n':\n"
        "    raise SystemExit(18)\n"
        "elif (root / 'candidate.cfg').read_text(encoding='utf-8').strip() != 'good':\n"
        "    raise SystemExit(17)\n",
        encoding="utf-8",
    )
    (root / "candidate.cfg").write_text("initial\n", encoding="utf-8")
    (root / "notes.md").write_text("baseline\n", encoding="utf-8")
    (root / "docs/decisions").mkdir(parents=True)
    (root / "docs/decisions/README.md").write_bytes(b"stale\r\n")
    _git(root, "add", "githooks", "tools", ".agents", "candidate.cfg", "notes.md", "docs")
    _git(root, "commit", "-m", "fixture")
    _git(root, "config", "core.hooksPath", "githooks")


def _run_hook(root: Path) -> subprocess.CompletedProcess[str]:
    environment = os.environ.copy()
    environment.pop("REPO_STANDARDS_HOSTED_COMMIT", None)
    return subprocess.run(
        ["bash", "githooks/pre-commit"], cwd=root, text=True, capture_output=True, env=environment
    )


def test_hook_checks_staged_candidate_and_restores_unstaged_repair(tmp_path):
    _fixture(tmp_path)
    (tmp_path / "candidate.cfg").write_text("reject\n", encoding="utf-8")
    _git(tmp_path, "add", "candidate.cfg")
    generated = tmp_path / "docs/decisions/README.md"
    generated.write_bytes(b"fresh\n")
    _git(tmp_path, "add", "docs/decisions/README.md")
    (tmp_path / "candidate.cfg").write_text("good\n", encoding="utf-8")
    notes = tmp_path / "notes.md"
    notes.write_bytes(b"preserve failure-side CRLF bytes\r\n")
    future = tmp_path / "future-failure.txt"
    future.write_bytes(b"preserve failure-side untracked bytes\r\n")

    result = _run_hook(tmp_path)

    assert result.returncode == 17
    assert (tmp_path / "candidate.cfg").read_text(encoding="utf-8") == "good\n"
    assert _git(tmp_path, "show", ":candidate.cfg") == "reject"
    assert notes.read_bytes() == b"preserve failure-side CRLF bytes\r\n"
    assert future.read_bytes() == b"preserve failure-side untracked bytes\r\n"


def test_hook_rejects_stale_generated_content_without_repairing_or_staging_it(tmp_path):
    _fixture(tmp_path)
    (tmp_path / "candidate.cfg").write_text("good\n", encoding="utf-8")
    _git(tmp_path, "add", "candidate.cfg")
    generated = tmp_path / "docs/decisions/README.md"
    before = generated.read_bytes()
    staged_tree = _git(tmp_path, "write-tree")

    result = _run_hook(tmp_path)

    assert result.returncode == 18
    assert generated.read_bytes() == before
    assert _git(tmp_path, "show", ":docs/decisions/README.md") == "stale"
    assert _git(tmp_path, "write-tree") == staged_tree
    assert "docs/decisions/README.md" not in _git(tmp_path, "diff", "--cached", "--name-only")


def test_hook_success_preserves_candidate_and_unrelated_work_without_applying(tmp_path):
    _fixture(tmp_path)
    (tmp_path / "candidate.cfg").write_text("good\n", encoding="utf-8")
    _git(tmp_path, "add", "candidate.cfg")
    generated = tmp_path / "docs/decisions/README.md"
    generated.write_bytes(b"fresh\n")
    _git(tmp_path, "add", "docs/decisions/README.md")
    (tmp_path / "unrelated.txt").write_text("keep unstaged\n", encoding="utf-8")
    (tmp_path / "future.txt").write_bytes(b"keep untracked CRLF\r\n")
    head = _git(tmp_path, "rev-parse", "HEAD")
    staged_tree = _git(tmp_path, "write-tree")
    notes = tmp_path / "notes.md"
    notes.write_bytes(b"keep these CRLF bytes\r\n")

    result = _run_hook(tmp_path)

    assert result.returncode == 0, result.stderr
    assert _git(tmp_path, "rev-parse", "HEAD") == head
    assert _git(tmp_path, "write-tree") == staged_tree
    assert _git(tmp_path, "show", ":candidate.cfg") == "good"
    assert generated.read_bytes() == b"fresh\n"
    assert _git(tmp_path, "show", ":docs/decisions/README.md") == "fresh"
    assert notes.read_bytes() == b"keep these CRLF bytes\r\n"
    assert (tmp_path / "unrelated.txt").read_text(encoding="utf-8") == "keep unstaged\n"
    assert (tmp_path / "future.txt").read_bytes() == b"keep untracked CRLF\r\n"
    status = _git(tmp_path, "status", "--short")
    assert "M  candidate.cfg" in status
    assert "M  docs/decisions/README.md" in status
    assert "unrelated.txt" in status
    assert "future.txt" in status
    assert "notes.md" in status


@pytest.mark.parametrize(
    ("candidate", "expected_status"),
    [("good", 0), ("reject", 17)],
    ids=["successful-check", "failed-check"],
)
def test_hook_restores_intent_to_add_file_and_unstaged_edits(tmp_path, candidate, expected_status):
    _fixture(tmp_path)
    generated = tmp_path / "docs/decisions/README.md"
    generated.write_bytes(b"fresh\n")
    _git(tmp_path, "add", "docs/decisions/README.md")
    (tmp_path / "candidate.cfg").write_text(f"{candidate}\n", encoding="utf-8")
    _git(tmp_path, "add", "candidate.cfg")
    notes = tmp_path / "notes.md"
    notes.write_bytes(b"preserve unrelated unstaged bytes\r\n")
    intent = tmp_path / "intent.txt"
    intent.write_bytes(b"preserve intent-to-add content\r\n")
    _git(tmp_path, "add", "-N", "intent.txt")
    head = _git(tmp_path, "rev-parse", "HEAD")
    staged_tree = _git(tmp_path, "write-tree")

    result = _run_hook(tmp_path)

    assert result.returncode == expected_status, result.stderr
    assert _git(tmp_path, "rev-parse", "HEAD") == head
    assert _git(tmp_path, "write-tree") == staged_tree
    assert _git(tmp_path, "show", ":candidate.cfg") == candidate
    assert notes.read_bytes() == b"preserve unrelated unstaged bytes\r\n"
    assert intent.read_bytes() == b"preserve intent-to-add content\r\n"
    assert " A intent.txt" in _git(tmp_path, "status", "--short")
    assert _git(tmp_path, "diff", "--cached", "--name-only").splitlines() == [
        "candidate.cfg",
        "docs/decisions/README.md",
    ]
    assert "intent.txt" in _git(tmp_path, "diff", "--", "intent.txt")


def test_hosted_validation_keeps_detached_head_and_index_unchanged(tmp_path):
    _fixture(tmp_path)
    generated = tmp_path / "docs/decisions/README.md"
    generated.write_bytes(b"fresh\n")
    _git(tmp_path, "add", "docs/decisions/README.md")
    (tmp_path / "candidate.cfg").write_text("good\n", encoding="utf-8")
    _git(tmp_path, "add", "candidate.cfg", "docs/decisions/README.md")
    _git(tmp_path, "commit", "-m", "fresh generated candidate")
    _git(tmp_path, "checkout", "--detach", "HEAD")
    head = _git(tmp_path, "rev-parse", "HEAD")
    staged_tree = _git(tmp_path, "write-tree")
    environment = os.environ.copy()
    environment["REPO_STANDARDS_HOSTED_COMMIT"] = "HEAD"

    result = subprocess.run(
        ["bash", "githooks/pre-commit"], cwd=tmp_path, text=True, capture_output=True, env=environment
    )

    assert result.returncode == 0, result.stderr
    assert _git(tmp_path, "rev-parse", "HEAD") == head
    assert _git(tmp_path, "write-tree") == staged_tree
    assert _git(tmp_path, "status", "--porcelain") == ""


def test_hosted_validation_failure_keeps_detached_head_and_index_unchanged(tmp_path):
    _fixture(tmp_path)
    _git(tmp_path, "checkout", "--detach", "HEAD")
    head = _git(tmp_path, "rev-parse", "HEAD")
    staged_tree = _git(tmp_path, "write-tree")
    environment = os.environ.copy()
    environment["REPO_STANDARDS_HOSTED_COMMIT"] = "HEAD"

    result = subprocess.run(
        ["bash", "githooks/pre-commit"], cwd=tmp_path, text=True, capture_output=True, env=environment
    )

    assert result.returncode == 18
    assert _git(tmp_path, "rev-parse", "HEAD") == head
    assert _git(tmp_path, "write-tree") == staged_tree
    assert _git(tmp_path, "status", "--porcelain") == ""
