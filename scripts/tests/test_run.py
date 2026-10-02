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
        "_activate_hook",
        "_operating_standards_check",
        "_agent_routers_check",
        "_plugin_subscriptions_check",
        "_adr_freshness_apply",
    )
    product_steps = ("_build_dotnet", "_test_dotnet", "_build_web", "_diff_check")

    for name in mechanical_steps + product_steps:
        monkeypatch.setattr(run, name, lambda _ctx, step=name: visited.append(step))

    run._ci_apply(run.Ctx("apply", False))

    assert visited == list(mechanical_steps)


def test_ci_check_invokes_repository_owned_contracts(monkeypatch) -> None:
    captured: list[list[str]] = []
    monkeypatch.setattr(run, "_run", lambda command, _ctx: captured.append(command))
    for check in (run._operating_standards_check, run._agent_routers_check, run._plugin_subscriptions_check):
        check(run.Ctx("check", False))
    assert [command[1:] for command in captured] == [
        ["scripts/check_operating_standards.py", "--check"],
        ["scripts/check_agent_routers.py", "--check"],
        ["scripts/check_plugin_subscriptions.py", "--check"],
    ]


def test_ci_apply_activates_tracked_hook(monkeypatch) -> None:
    captured: list[list[str]] = []
    monkeypatch.setattr(run, "_run", lambda command, _ctx: captured.append(command))
    run._activate_hook(run.Ctx("apply", False))
    assert captured == [["git", "config", "core.hooksPath", "githooks"]]
