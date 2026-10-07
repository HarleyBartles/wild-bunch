from __future__ import annotations

import importlib.util
from pathlib import Path
import sys
import subprocess

import pytest


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
SPEC = importlib.util.spec_from_file_location("wild_bunch_run", ROOT / "tools" / "run.py")
assert SPEC is not None and SPEC.loader is not None
run = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = run
SPEC.loader.exec_module(run)


def test_diagnostics_collects_independent_ci_failures(monkeypatch, capsys) -> None:
    visited: list[str] = []

    def passing(_ctx) -> None:
        visited.append("passing")

    def first_failure(_ctx) -> None:
        visited.append("first")
        raise RuntimeError("first failed")

    def second_failure(_ctx) -> None:
        visited.append("second")
        raise RuntimeError("second failed")

    monkeypatch.setattr(
        run,
        "CI_CHECKS",
        (
            ("passing", passing, "repair passing"),
            ("first", first_failure, "repair first"),
            ("second", second_failure, "repair second"),
        ),
    )

    with pytest.raises(run.CiDiagnosticsError):
        run._ci_check(run.Ctx("check", False, diagnostics=True))

    assert visited == ["passing", "first", "second"]
    output = capsys.readouterr().err
    assert "first: repair first" in output
    assert "second: repair second" in output


def test_diagnostics_is_rejected_outside_ci_check(capsys) -> None:
    assert run.main(["ci", "--apply", "--diagnostics"]) == 1
    assert "--diagnostics requires ci --check" in capsys.readouterr().err


def _git(root: Path, *args: str) -> str:
    result = subprocess.run(["git", *args], cwd=root, check=True, capture_output=True, text=True)
    return result.stdout.strip()


def _git_repo(root: Path) -> None:
    _git(root, "init", "-b", "main")
    _git(root, "config", "user.name", "Test")
    _git(root, "config", "user.email", "test@example.invalid")
    _git(root, "config", "core.autocrlf", "false")
    (root / "candidate.txt").write_bytes(b"initial\n")
    _git(root, "add", "candidate.txt")
    _git(root, "commit", "-m", "baseline")


def _assert_diff_check(root: Path, monkeypatch, *, hosted: bool, fails: bool) -> None:
    monkeypatch.setattr(run, "ROOT", root)
    if hosted:
        monkeypatch.setenv("REPO_STANDARDS_HOSTED_COMMIT", "HEAD")
    else:
        monkeypatch.delenv("REPO_STANDARDS_HOSTED_COMMIT", raising=False)

    if fails:
        with pytest.raises(subprocess.CalledProcessError):
            run._diff_check(run.Ctx("check", False))
    else:
        run._diff_check(run.Ctx("check", False))


@pytest.mark.parametrize("hosted", [False, True], ids=["local-staged-candidate", "hosted-commit"])
def test_whitespace_check_rejects_trailing_whitespace_in_candidate(tmp_path, monkeypatch, hosted):
    _git_repo(tmp_path)
    (tmp_path / "candidate.txt").write_bytes(b"candidate has trailing space \n")
    _git(tmp_path, "add", "candidate.txt")
    if hosted:
        _git(tmp_path, "commit", "-m", "whitespace candidate")

    _assert_diff_check(tmp_path, monkeypatch, hosted=hosted, fails=True)


@pytest.mark.parametrize("hosted", [False, True], ids=["local-staged-candidate", "hosted-commit"])
def test_whitespace_check_accepts_clean_candidate(tmp_path, monkeypatch, hosted):
    _git_repo(tmp_path)
    (tmp_path / "candidate.txt").write_bytes(b"clean candidate\n")
    _git(tmp_path, "add", "candidate.txt")
    if hosted:
        _git(tmp_path, "commit", "-m", "clean candidate")

    _assert_diff_check(tmp_path, monkeypatch, hosted=hosted, fails=False)
