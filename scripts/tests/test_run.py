from __future__ import annotations

import importlib.util
from pathlib import Path
import sys

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


def test_ci_apply_only_materializes_mechanical_surfaces(monkeypatch) -> None:
    visited: list[str] = []
    mechanical_steps = (
        "_repo_standards_apply",
        "_skill_scripts_check",
        "_adr_freshness_apply",
    )
    product_steps = ("_build_dotnet", "_test_dotnet", "_build_web", "_diff_check")

    for name in mechanical_steps + product_steps:
        monkeypatch.setattr(run, name, lambda _ctx, step=name: visited.append(step))

    run._ci_apply(run.Ctx("apply", False))

    assert visited == list(mechanical_steps)


def test_commands_use_deployed_standards(monkeypatch) -> None:
    repo_standards = run._repo_standards_cmd("check", False)
    assert repo_standards[1] == ".agents/standards/_runtime/repo_standards.py"
    assert repo_standards[-1] == "--check"

    apply_standards = run._repo_standards_cmd("apply", True)
    assert apply_standards[-3:] == ["--apply", "--yes", "--allow-shared-checkout"]

    assert run._adr_freshness_cmd("check")[1:] == ["scripts/update_adr_freshness.py", "--check"]

    captured: list[str] = []
    monkeypatch.setattr(run, "_run", lambda command, _ctx: captured.extend(command))
    run._skill_scripts_check(run.Ctx("check", False))
    assert captured[1:] == ["scripts/validate_repo_skill_scripts.py", "--check"]
