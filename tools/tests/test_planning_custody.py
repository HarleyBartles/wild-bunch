from __future__ import annotations

import subprocess
import sys
from pathlib import Path

import pytest

CHECKER = Path(__file__).resolve().parents[1] / "check_planning_custody.py"


def git(root: Path, *arguments: str) -> str:
    return subprocess.check_output(["git", *arguments], cwd=root, text=True).strip()


@pytest.fixture
def repository(tmp_path: Path) -> Path:
    git(tmp_path, "init", "-q", "-b", "codex/candidate")
    git(tmp_path, "config", "user.name", "Custody test")
    git(tmp_path, "config", "user.email", "custody@example.invalid")
    (tmp_path / "README.md").write_text("Product orientation\n", encoding="utf-8")
    git(tmp_path, "add", "README.md")
    git(tmp_path, "-c", "core.hooksPath=", "commit", "-qm", "base")
    return tmp_path


def stage(root: Path, name: str) -> None:
    path = root / name
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("Live work\n", encoding="utf-8")
    git(root, "add", "--", name)


def check(root: Path, *arguments: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(CHECKER), *arguments],
        cwd=root,
        text=True,
        capture_output=True,
        check=False,
    )


@pytest.mark.parametrize("home", ["plans", "specs", "roadmaps", "investigations"])
def test_main_rejects_each_indexed_planning_home(repository: Path, home: str) -> None:
    name = f".agents/{home}/current work.md"
    stage(repository, name)

    result = check(repository, "--check", "--target", "main")

    assert result.returncode == 1, result.stderr
    assert name in result.stderr
    assert "main" in result.stderr
    assert (repository / name).read_text(encoding="utf-8") == "Live work\n"
    assert git(repository, "diff", "--cached", "--name-only") == name


def test_develop_permits_live_planning(repository: Path) -> None:
    stage(repository, ".agents/plans/live.md")
    stage(repository, ".agents/investigations/open.md")

    result = check(repository, "--check", "--target", "develop")

    assert result.returncode == 0, result.stderr


def test_main_permits_durable_guidance_and_deferred_feature_matrix(repository: Path) -> None:
    for name in [".agents/doctrine/custody.md", "docs/decisions/ADR-0001.md", "docs/features.md"]:
        stage(repository, name)

    result = check(repository, "--check", "--target", "main")

    assert result.returncode == 0, result.stderr


def test_main_rejects_planning_already_in_base(repository: Path) -> None:
    name = ".agents/specs/inherited.md"
    stage(repository, name)
    git(repository, "-c", "core.hooksPath=", "commit", "-qm", "inherited planning")
    stage(repository, "docs/change.md")

    result = check(repository, "--check", "--target", "main")

    assert result.returncode == 1
    assert name in result.stderr


def test_staged_deletion_is_accepted_despite_head_and_unstaged_file(repository: Path) -> None:
    name = ".agents/plans/retired.md"
    stage(repository, name)
    git(repository, "-c", "core.hooksPath=", "commit", "-qm", "old plan")
    git(repository, "rm", "--cached", "--", name)

    result = check(repository, "--check", "--target", "main")

    assert result.returncode == 0, result.stderr
    assert (repository / name).exists()
    assert git(repository, "diff", "--cached", "--diff-filter=D", "--name-only") == name


def test_indexed_addition_is_rejected_even_when_worktree_file_is_missing(repository: Path) -> None:
    name = ".agents/roadmaps/new.md"
    stage(repository, name)
    (repository / name).unlink()

    result = check(repository, "--check", "--target", "main")

    assert result.returncode == 1
    assert name in result.stderr


@pytest.mark.parametrize(
    "arguments",
    [[], ["--check"], ["--target", "main"], ["--check", "--target", "main", "--repair"]],
)
def test_invalid_request_is_rejected_before_repository_access(tmp_path: Path, arguments) -> None:
    result = check(tmp_path, *arguments)

    assert result.returncode == 2
    assert "usage:" in result.stderr
    assert "not a git repository" not in result.stderr


def test_valid_request_fails_when_index_cannot_be_read(tmp_path: Path) -> None:
    result = check(tmp_path, "--check", "--target", "main")

    assert result.returncode != 0
    assert "index" in result.stderr.lower()


def test_invocation_in_subdirectory_still_checks_whole_index(repository: Path) -> None:
    name = ".agents/investigations/root.md"
    stage(repository, name)
    child = repository / "tools"
    child.mkdir()

    result = check(child, "--check", "--target", "main")

    assert result.returncode == 1
    assert name in result.stderr
