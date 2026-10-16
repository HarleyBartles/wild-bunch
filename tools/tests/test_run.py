from __future__ import annotations

import importlib.util
import subprocess
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
SPEC = importlib.util.spec_from_file_location("wild_bunch_run", ROOT / "tools" / "run.py")
assert SPEC is not None and SPEC.loader is not None
run = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = run
SPEC.loader.exec_module(run)


@pytest.mark.parametrize("selection", ["environment", "explicit"])
def test_main_planning_guard_uses_destination_on_codex_branch(tmp_path, monkeypatch, selection):
    def git(*arguments):
        subprocess.run(["git", *arguments], cwd=tmp_path, check=True, capture_output=True)

    git("init", "-q", "-b", "codex/promotion")
    tools = tmp_path / "tools"
    tools.mkdir()
    (tools / "check_planning_custody.py").write_bytes(
        (ROOT / "tools/check_planning_custody.py").read_bytes()
    )
    plan = tmp_path / ".agents/plans/live.md"
    plan.parent.mkdir(parents=True)
    plan.write_text("Live planning", encoding="utf-8")
    git("add", ".")
    monkeypatch.setattr(run, "ROOT", tmp_path)
    monkeypatch.setenv(
        "WILD_BUNCH_TARGET_BRANCH", "main" if selection == "environment" else "develop"
    )
    arguments = ["planning-custody", "--check"]
    if selection == "explicit":
        arguments.extend(["--target", "main"])

    assert run.main(arguments) == 1
    assert run.main(["planning-custody", "--check", "--target", "develop"]) == 0


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


def test_web_build_rejects_stale_generated_identity_after_build(tmp_path, monkeypatch) -> None:
    web = tmp_path / "src" / "WildBunch.Web"
    dist = web / "dist"
    dist.mkdir(parents=True)
    (tmp_path / "Directory.Build.props").write_text(
        "<Project><PropertyGroup><Version>0.1.0-dev.196</Version></PropertyGroup></Project>",
        encoding="utf-8",
    )
    (web / "package.json").write_text('{"name":"wildbunch-web"}', encoding="utf-8")
    (web / "package-lock.json").write_text(
        '{"packages":{"":{"name":"wildbunch-web"}}}', encoding="utf-8"
    )
    (dist / "version.json").write_text('{"version":"0.1.0-dev.195"}', encoding="utf-8")
    commands: list[list[str]] = []

    monkeypatch.setattr(run, "ROOT", tmp_path)
    monkeypatch.setattr(run, "WEB_DIR", web)
    monkeypatch.setattr(run, "_check_web_style", lambda _ctx: None)
    monkeypatch.setattr(run, "_run", lambda command, _ctx: commands.append(command))

    with pytest.raises(run.VersionIdentityError, match="contains '0.1.0-dev.195'"):
        run._build_web(run.Ctx("check", False))

    assert [command[-1] for command in commands] == [
        "typecheck",
        "test",
        "build",
    ]


def test_web_build_runs_browser_only_after_build_and_identity_validation(monkeypatch) -> None:
    steps: list[str] = []
    monkeypatch.setattr(run, "_check_web_style", lambda _ctx: steps.append("style"))
    monkeypatch.setattr(run, "_run", lambda command, _ctx: steps.append(command[-1]))
    monkeypatch.setattr(run, "_version_identity_check", lambda _ctx: steps.append("identity"))

    run._build_web(run.Ctx("check", False))

    assert steps == ["style", "typecheck", "test", "build", "identity", "test:browser"]


def test_selected_target_without_mode_fails_before_target_work(monkeypatch, capsys) -> None:
    invoked: list[str] = []
    monkeypatch.setitem(
        run.TARGETS,
        "ci",
        {
            "apply": lambda _ctx: invoked.append("apply"),
            "check": lambda _ctx: invoked.append("check"),
        },
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
        {
            "apply": lambda _ctx: invoked.append("apply"),
            "check": lambda _ctx: invoked.append("check"),
        },
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
        {
            "apply": lambda _ctx: invoked.append("apply"),
            "check": lambda _ctx: invoked.append("check"),
        },
    )

    with pytest.raises(SystemExit) as exit_info:
        run.main(["ci", "--help"])

    output = capsys.readouterr().out
    assert exit_info.value.code == 0
    assert "complete repository check gate" in output
    assert "--check" in output
    assert invoked == []


def test_dotnet_build_forwards_arguments_and_child_exit_status(
    tmp_path, monkeypatch, capfd
) -> None:
    result_file = tmp_path / "arguments.txt"
    child = tmp_path / "child.py"
    child.write_text(
        "import pathlib, sys\n"
        f"pathlib.Path({str(result_file)!r}).write_text("
        "'\\n'.join(sys.argv[1:]), encoding='utf-8')\n"
        "print('build child output')\n"
        "raise SystemExit(29)\n",
        encoding="utf-8",
    )
    monkeypatch.setattr(
        run,
        "_dotnet_build_cmd",
        lambda arguments=(): [sys.executable, str(child), *arguments],
    )

    status = run.main(
        ["dotnet-build", "--check", "--", "--no-restore", "-p:BuildProjectReferences=false"]
    )

    captured = capfd.readouterr()
    assert status == 29
    assert result_file.read_text(encoding="utf-8").splitlines() == [
        "--no-restore",
        "-p:BuildProjectReferences=false",
    ]
    assert "build child output" in captured.out


def test_setup_hooks_apply_changes_only_local_git_configuration(tmp_path, monkeypatch) -> None:
    _git_repo(tmp_path)
    monkeypatch.setattr(run, "ROOT", tmp_path)

    status = run.main(["setup-hooks", "--apply", "--allow-shared-checkout"])

    assert status == 0
    assert _git(tmp_path, "config", "--local", "--get", "core.hooksPath") == "githooks"
    assert _git(tmp_path, "status", "--porcelain") == ""


def test_setup_hooks_check_rejects_a_different_local_hook_path(
    tmp_path, monkeypatch, capsys
) -> None:
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
        (
            (
                "child-check",
                lambda ctx: run._run(child, ctx),
                "fix the child",
                "py -3 tools/run.py ci --check",
            ),
        ),
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
            (
                "first-check",
                fail,
                "repair the subscription",
                "py -3 tools/check_operating_standards.py --check",
            ),
            (
                "second-check",
                should_not_run,
                "run the second check again",
                "py -3 tools/run.py ci --check",
            ),
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


def test_ci_lint_failure_stops_before_behavioral_tests(monkeypatch, capfd) -> None:
    commands: list[list[str]] = []

    def run_command(command: list[str], _ctx) -> None:
        commands.append(command)
        if "ruff" in command and "check" in command:
            raise subprocess.CalledProcessError(1, command)

    monkeypatch.setattr(run, "_run", run_command)

    status = run.main(["ci", "--check"])

    captured = capfd.readouterr()
    assert status != 0
    assert any("ruff" in command and "check" in command for command in commands)
    assert not any("dotnet" in command and "build" in command for command in commands)
    assert not any("typecheck" in command for command in commands)
    assert not any("pytest" in command for command in commands)
    assert not any("dotnet" in command and "test" in command for command in commands)
    assert not any("npm" in command and "test" in command for command in commands)
    assert "target 'ci' failed" in captured.err


def test_ci_builds_and_checks_identity_before_chromium(monkeypatch, capsys) -> None:
    visited: list[str] = []
    identity_checks: list[Path] = []
    checks = []

    for name, check, repair, recheck in run.CI_CHECKS:
        if name in {"web-tests", "web-build", "build-identity", "web-browser-tests"}:

            def record(ctx, *, lane=name, callback=check):
                visited.append(lane)
                callback(ctx)

            checks.append((name, record, repair, recheck))
        else:
            checks.append((name, lambda _ctx: None, repair, recheck))

    def fail_browser_command(command: list[str], _ctx) -> None:
        if "test:browser" in command:
            raise subprocess.CalledProcessError(1, command)

    monkeypatch.setattr(run, "check_version_identity", identity_checks.append)
    monkeypatch.setattr(run, "CI_CHECKS", tuple(checks))
    monkeypatch.setattr(run, "_run", fail_browser_command)

    with pytest.raises(subprocess.CalledProcessError):
        run._ci_check(run.Ctx("check", False))

    assert visited == ["web-tests", "web-build", "build-identity", "web-browser-tests"]
    assert identity_checks == [run.ROOT]
    assert "check 'web-browser-tests' failed" in capsys.readouterr().err


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


def _release_repo(root: Path) -> str:
    _git_repo(root)
    (root / ".gitignore").write_text("artifacts/release/\n", encoding="utf-8")
    (root / "Directory.Build.props").write_text(
        "<Project><PropertyGroup><Version>0.1.0-dev.197</Version></PropertyGroup></Project>",
        encoding="utf-8",
    )
    web = root / "src" / "WildBunch.Web"
    web.mkdir(parents=True)
    (web / "package.json").write_text('{"name":"web"}', encoding="utf-8")
    (web / "package-lock.json").write_text('{"packages":{"":{"name":"web"}}}', encoding="utf-8")
    (root / "src" / "WildBunch.Api").mkdir(parents=True)
    (root / "src" / "WildBunch.Api" / "WildBunch.Api.csproj").write_text(
        "<Project />", encoding="utf-8"
    )
    _git(root, "add", ".")
    _git(root, "commit", "-m", "release source")
    return _git(root, "rev-parse", "HEAD")


def _fake_release_build(command: list[str], _ctx) -> None:
    if command[0] != "dotnet" and "ci" in command:
        return
    if command[0] == "dotnet":
        output = Path(command[command.index("-o") + 1])
    else:
        output = Path(command[command.index("--outDir") + 1])
    output.mkdir(parents=True, exist_ok=True)
    (output / "artifact.txt").write_text("built bytes\n", encoding="utf-8")
    if command[0] != "dotnet":
        (output / "version.json").write_text('{"version":"0.1.0-dev.197"}', encoding="utf-8")


def test_release_verify_builds_and_rechecks_immutable_source_bundle(tmp_path, monkeypatch):
    source_commit = _release_repo(tmp_path)
    commands: list[list[str]] = []
    monkeypatch.setattr(run, "ROOT", tmp_path)
    monkeypatch.setattr(run, "WEB_DIR", tmp_path / "src" / "WildBunch.Web")

    def fake_build(command, ctx):
        commands.append(command)
        _fake_release_build(command, ctx)

    monkeypatch.setattr(run, "_run", fake_build)

    assert run.main(["release-verify", "--check"]) == 0
    bundle = tmp_path / "artifacts" / "release" / source_commit
    manifest = __import__("json").loads((bundle / "release-manifest.json").read_text())
    assert manifest["sourceCommit"] == source_commit
    assert manifest["version"] == "0.1.0-dev.197"
    assert set(manifest["artifacts"]) == {"api", "web-public", "web-playtest"}
    assert len(commands) == 4
    assert commands[0][-1] == "ci"
    assert commands[1][0] == "dotnet" and commands[1][1] == "publish"
    assert "run" in commands[2] and "build" in commands[2]
    assert "run" in commands[3] and "build:playtest" in commands[3]

    assert run.main(["release-verify", "--check"]) == 0
    assert len(commands) == 4


def test_release_verify_stops_before_staging_when_locked_install_fails(tmp_path, monkeypatch):
    _release_repo(tmp_path)
    monkeypatch.setattr(run, "ROOT", tmp_path)
    monkeypatch.setattr(run, "WEB_DIR", tmp_path / "src" / "WildBunch.Web")
    calls: list[list[str]] = []

    def fail_install(command, _ctx):
        calls.append(command)
        raise subprocess.CalledProcessError(1, command)

    monkeypatch.setattr(run, "_run", fail_install)

    assert run.main(["release-verify", "--check"]) == 1
    assert len(calls) == 1 and calls[0][-1] == "ci"
    assert not (tmp_path / "artifacts" / "release").exists()


def test_release_verify_rejects_dirty_source_before_any_build(tmp_path, monkeypatch):
    _release_repo(tmp_path)
    monkeypatch.setattr(run, "ROOT", tmp_path)
    monkeypatch.setattr(run, "WEB_DIR", tmp_path / "src" / "WildBunch.Web")
    commands: list[list[str]] = []
    monkeypatch.setattr(run, "_run", lambda command, _ctx: commands.append(command))
    (tmp_path / "untracked.txt").write_text("dirty\n", encoding="utf-8")

    assert run.main(["release-verify", "--check"]) != 0
    assert commands == []


@pytest.mark.parametrize(
    "arguments", [["release-verify", "--apply"], ["release-verify", "--check", "--unknown"]]
)
def test_release_verify_rejects_unsupported_modes_before_build(tmp_path, monkeypatch, arguments):
    monkeypatch.setattr(run, "ROOT", tmp_path)
    calls: list[list[str]] = []
    monkeypatch.setattr(run, "_run", lambda command, _ctx: calls.append(command))

    with pytest.raises(SystemExit):
        run.main(arguments)

    assert calls == []


def test_release_verify_rejects_tampered_bundle_without_rebuilding(tmp_path, monkeypatch):
    source_commit = _release_repo(tmp_path)
    monkeypatch.setattr(run, "ROOT", tmp_path)
    monkeypatch.setattr(run, "WEB_DIR", tmp_path / "src" / "WildBunch.Web")
    monkeypatch.setattr(run, "_run", _fake_release_build)
    assert run.main(["release-verify", "--check"]) == 0
    bundle = tmp_path / "artifacts" / "release" / source_commit
    (bundle / "api" / "artifact.txt").write_text("tampered\n", encoding="utf-8")
    original = (bundle / "api" / "artifact.txt").read_text(encoding="utf-8")
    calls: list[list[str]] = []
    monkeypatch.setattr(run, "_run", lambda command, _ctx: calls.append(command))

    assert run.main(["release-verify", "--check"]) != 0
    assert calls == []
    assert (bundle / "api" / "artifact.txt").read_text(encoding="utf-8") == original


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
