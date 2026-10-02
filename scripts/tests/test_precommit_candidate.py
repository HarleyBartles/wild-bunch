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
                "generated_paths": ["docs/decisions/README.md"],
            }
        ),
        encoding="utf-8",
    )
    (root / "tools/gate.py").write_text(
        "from pathlib import Path\n"
        "import sys\n"
        "root = Path.cwd()\n"
        "if '--apply' in sys.argv:\n"
        "    out = root / 'docs/decisions/README.md'\n"
        "    out.parent.mkdir(parents=True, exist_ok=True)\n"
        "    out.write_bytes(b'fresh\\r\\n')\n"
        "    out.write_bytes(out.read_bytes().replace(b'\\r\\n', b'\\n'))\n"
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


def test_hook_preserves_unrelated_work_and_stages_declared_generated_output(tmp_path):
    _fixture(tmp_path)
    (tmp_path / "candidate.cfg").write_text("good\n", encoding="utf-8")
    _git(tmp_path, "add", "candidate.cfg")
    (tmp_path / "unrelated.txt").write_text("keep unstaged\n", encoding="utf-8")
    (tmp_path / "future.txt").write_text("keep untracked\n", encoding="utf-8")

    result = _run_hook(tmp_path)

    assert result.returncode == 0, result.stderr
    assert _git(tmp_path, "show", ":docs/decisions/README.md") == "fresh"
    assert (tmp_path / "unrelated.txt").read_text(encoding="utf-8") == "keep unstaged\n"
    assert (tmp_path / "future.txt").read_text(encoding="utf-8") == "keep untracked\n"
    assert "unrelated.txt" in _git(tmp_path, "status", "--short")
    assert "future.txt" in _git(tmp_path, "status", "--short")


def test_hook_stages_normalized_declared_output_and_preserves_unstaged_crlf_bytes(tmp_path):
    _fixture(tmp_path)
    (tmp_path / "candidate.cfg").write_text("good\n", encoding="utf-8")
    _git(tmp_path, "add", "candidate.cfg")
    notes = tmp_path / "notes.md"
    notes.write_bytes(b"keep these CRLF bytes\r\n")

    result = _run_hook(tmp_path)

    assert result.returncode == 0, result.stderr
    assert _git(tmp_path, "show", ":candidate.cfg") == "good"
    generated = tmp_path / "docs/decisions/README.md"
    assert generated.read_bytes() == b"fresh\n"
    assert _git(tmp_path, "show", ":docs/decisions/README.md") == "fresh"
    assert notes.read_bytes() == b"keep these CRLF bytes\r\n"
    status = _git(tmp_path, "status", "--short")
    assert "candidate.cfg" in status
    assert "notes.md" in status
