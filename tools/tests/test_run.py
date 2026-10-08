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
            ("passing", passing, "repair passing", "recheck passing"),
            ("first", first_failure, "repair first", "recheck first"),
            ("second", second_failure, "repair second", "recheck second"),
        ),
    )

    with pytest.raises(run.CiDiagnosticsError):
        run._ci_check(run.Ctx("check", False, diagnostics=True))

    assert visited == ["passing", "first", "second"]
    output = capsys.readouterr().err
    assert "first: repair first; recheck: recheck first" in output
    assert "second: repair second; recheck: recheck second" in output


def test_ci_rejects_apply_mode_before_running_work(monkeypatch) -> None:
    invoked: list[str] = []
    monkeypatch.setitem(run.TARGETS, "ci", {"check": lambda _ctx: invoked.append("check")})
    with pytest.raises(SystemExit) as exit_info:
        run.main(["ci", "--apply"])

    assert exit_info.value.code == 2
    assert invoked == []


def test_selected_target_without_mode_fails_before_target_work(monkeypatch, capsys) -> None:
    invoked: list[str] = []
    monkeypatch.setitem(
        run.TARGETS,
        "ci",
        {"apply": lambda _ctx: invoked.append("apply"), "check": lambda _ctx: invoked.append("check")},
    )

    status = run.main(["ci"])

    assert status != 0
    assert invoked == []
    assert "usage:" in capsys.readouterr().err


def test_bare_bus_invocation_lists_targets_without_running_work(monkeypatch, capsys) -> None:
    invoked: list[str] = []
    monkeypatch.setitem(
        run.TARGETS,
        "ci",
        {"apply": lambda _ctx: invoked.append("apply"), "check": lambda _ctx: invoked.append("check")},
    )

    status = run.main([])

    assert status == 0
    assert invoked == []
    assert "usage:" in capsys.readouterr().out


def test_ci_help_describes_its_work_without_running_it(monkeypatch, capsys) -> None:
    invoked: list[str] = []
    monkeypatch.setitem(
        run.TARGETS,
        "ci",
        {"apply": lambda _ctx: invoked.append("apply"), "check": lambda _ctx: invoked.append("check")},
    )

    with pytest.raises(SystemExit) as exit_info:
        run.main(["ci", "--help"])

    output = capsys.readouterr().out
    assert exit_info.value.code == 0
    assert "complete repository check gate" in output
    assert "--check" in output
    assert invoked == []


def test_dotnet_build_forwards_arguments_and_child_exit_status(tmp_path, monkeypatch, capfd) -> None:
    result_file = tmp_path / "arguments.txt"
    child = tmp_path / "child.py"
    child.write_text(
        "import pathlib, sys\n"
        f"pathlib.Path({str(result_file)!r}).write_text('\\n'.join(sys.argv[1:]), encoding='utf-8')\n"
        "print('build child output')\n"
        "raise SystemExit(29)\n",
        encoding="utf-8",
    )
    monkeypatch.setattr(
        run,
        "_dotnet_build_cmd",
        lambda arguments=(): [sys.executable, str(child), *arguments],
    )

    status = run.main(["dotnet-build", "--check", "--", "--no-restore", "-p:BuildProjectReferences=false"])

    captured = capfd.readouterr()
    assert status == 29
    assert result_file.read_text(encoding="utf-8").splitlines() == ["--no-restore", "-p:BuildProjectReferences=false"]
    assert "build child output" in captured.out


def test_setup_hooks_apply_changes_only_local_git_configuration(tmp_path, monkeypatch) -> None:
    _git_repo(tmp_path)
    monkeypatch.setattr(run, "ROOT", tmp_path)

    status = run.main(["setup-hooks", "--apply", "--allow-shared-checkout"])

    assert status == 0
    assert _git(tmp_path, "config", "--local", "--get", "core.hooksPath") == "githooks"
    assert _git(tmp_path, "status", "--porcelain") == ""


def test_setup_hooks_check_rejects_a_different_local_hook_path(tmp_path, monkeypatch, capsys) -> None:
    _git_repo(tmp_path)
    _git(tmp_path, "config", "--local", "core.hooksPath", "other-hooks")
    monkeypatch.setattr(run, "ROOT", tmp_path)

    status = run.main(["setup-hooks", "--check"])

    assert status != 0
    assert _git(tmp_path, "config", "--local", "--get", "core.hooksPath") == "other-hooks"
    assert "py -3 tools/run.py setup-hooks --apply" in capsys.readouterr().err


def test_ci_preserves_failing_child_output_and_exit_status(monkeypatch, capfd) -> None:
    child = [sys.executable, "-c", "import sys; print('child failure'); sys.exit(23)"]
    monkeypatch.setattr(
        run,
        "CI_CHECKS",
        (("child-check", lambda ctx: run._run(child, ctx), "fix the child", "py -3 tools/run.py ci --check"),),
    )

    status = run.main(["ci", "--check"])

    captured = capfd.readouterr()
    assert status == 23
    assert "child failure" in captured.out
    assert "target 'ci' failed" in captured.err


def test_ci_failure_names_the_failed_check_and_recheck_command(monkeypatch, capsys) -> None:
    visited: list[str] = []

    def fail(_ctx) -> None:
        visited.append("first")
        raise RuntimeError("invalid subscription")

    def should_not_run(_ctx) -> None:
        visited.append("second")

    monkeypatch.setattr(
        run,
        "CI_CHECKS",
        (
            ("first-check", fail, "repair the subscription", "py -3 tools/check_operating_standards.py --check"),
            ("second-check", should_not_run, "run the second check again", "py -3 tools/run.py ci --check"),
        ),
    )

    with pytest.raises(RuntimeError, match="invalid subscription"):
        run._ci_check(run.Ctx("check", False))

    assert visited == ["first"]
    error = capsys.readouterr().err
    assert "first-check" in error
    assert "repair the subscription" in error
    assert "focused recheck: py -3 tools/check_operating_standards.py --check" in error
    assert "full recheck: py -3 tools/run.py ci --check" in error


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
