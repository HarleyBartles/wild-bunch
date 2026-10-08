#!/usr/bin/env python3
"""Canonical task runner for the Wild Bunch repo."""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

import shared_checkout
from versioning import VersionIdentityError, check_version_identity


ROOT = Path(__file__).resolve().parent.parent
SCRIPT_NAME = "tools/run"
WEB_DIR = ROOT / "src" / "WildBunch.Web"


@dataclass(frozen=True)
class Ctx:
    mode: str  # "apply" or "check"
    allow_shared: bool
    verbose: bool = False
    diagnostics: bool = False
    target_args: tuple[str, ...] = ()


class CiDiagnosticsError(RuntimeError):
    """One or more independent CI checks failed in diagnostic mode."""


class BusTargetError(RuntimeError):
    """A target rejected its current repository state with an actionable cause."""


def _run(cmd: list[str], ctx: Ctx) -> None:
    if ctx.verbose:
        print("+ " + " ".join(cmd))
    subprocess.run(cmd, cwd=ROOT, check=True)


def _dotnet_build_cmd(arguments: tuple[str, ...] = ()) -> list[str]:
    return ["dotnet", "build", *arguments]


def _dotnet_test_cmd(arguments: tuple[str, ...] = ()) -> list[str]:
    return ["dotnet", "test", *arguments]


def _npm_cmd(*args: str) -> list[str]:
    return [shutil.which("npm") or "npm", "--prefix", str(WEB_DIR), *args]


def _operating_standards_check(ctx: Ctx) -> None:
    _run([sys.executable, "tools/check_operating_standards.py", "--check"], ctx)


def _agent_routers_check(ctx: Ctx) -> None:
    _run([sys.executable, "tools/check_agent_routers.py", "--check"], ctx)


def _plugin_subscriptions_check(ctx: Ctx) -> None:
    _run([sys.executable, "tools/check_plugin_subscriptions.py", "--check"], ctx)


def _test_script_behaviors(ctx: Ctx) -> None:
    _run([sys.executable, "-m", "pytest", "scripts/tests", "-q", *ctx.target_args], ctx)


def _test_tool_behaviors(ctx: Ctx) -> None:
    _run([sys.executable, "-m", "pytest", "tools/tests", "-q", *ctx.target_args], ctx)


def _build_dotnet(ctx: Ctx) -> None:
    _run(_dotnet_build_cmd(ctx.target_args), ctx)


def _test_dotnet(ctx: Ctx) -> None:
    os.environ["ConnectionStrings__WildBunchPostgresDb"] = "Host=localhost;Port=5435;Database=wildbunch_dev;Username=postgres"
    _run(_dotnet_test_cmd(ctx.target_args), ctx)


def _build_web(ctx: Ctx) -> None:
    _run(_npm_cmd("ci"), ctx)
    _run(_npm_cmd("run", "typecheck"), ctx)
    _run(_npm_cmd("run", "test"), ctx)
    _run(_npm_cmd("run", "build"), ctx)


def _version_identity_check(_ctx: Ctx) -> None:
    try:
        check_version_identity(ROOT)
    except VersionIdentityError as exc:
        print(f"[tools/run] build identity: {exc}", file=sys.stderr)
        raise


def _diff_check(ctx: Ctx) -> None:
    hosted_commit = os.environ.get("REPO_STANDARDS_HOSTED_COMMIT")
    if hosted_commit:
        _run(["git", "diff", "--check", f"{hosted_commit}^", hosted_commit], ctx)
        return
    _run(["git", "diff", "--check", "HEAD"], ctx)


CI_CHECKS = (
    (
        "diff-check",
        _diff_check,
        "correct the whitespace errors reported by git diff",
        "git diff --check HEAD (or git diff --check <commit>^ <commit> for hosted mode)",
    ),
    (
        "operating-standards",
        _operating_standards_check,
        "correct the reported subscription or certification finding",
        "py -3 tools/check_operating_standards.py --check",
    ),
    (
        "agent-routers",
        _agent_routers_check,
        "correct the reported AGENTS.md router finding",
        "py -3 tools/check_agent_routers.py --check",
    ),
    (
        "plugin-subscriptions",
        _plugin_subscriptions_check,
        "correct the reported native Codex plugin declaration finding",
        "py -3 tools/check_plugin_subscriptions.py --check",
    ),
    (
        "script-behavior-tests",
        _test_script_behaviors,
        "correct the failing tracked-hook or standalone script behavior test",
        "py -3 -m pytest scripts/tests -q",
    ),
    (
        "tool-behavior-tests",
        _test_tool_behaviors,
        "correct the failing command-bus behavior test or implementation",
        "py -3 -m pytest tools/tests -q",
    ),
    (
        "dotnet-build",
        _build_dotnet,
        "correct the reported compiler or build error",
        "py -3 tools/run.py dotnet-build --check",
    ),
    (
        "dotnet-test",
        _test_dotnet,
        "correct the failing .NET test or implementation",
        "py -3 tools/run.py dotnet-test --check",
    ),
    (
        "web",
        _build_web,
        "correct the reported web typecheck, test, or build failure",
        "py -3 tools/run.py web --check",
    ),
    (
        "build-identity",
        _version_identity_check,
        "remove duplicate authored version metadata or rebuild the web artifact",
        "py -3 tools/run.py web --check",
    ),
)


def _python_tests(ctx: Ctx) -> None:
    _test_script_behaviors(ctx)
    _test_tool_behaviors(ctx)


def _setup_hooks_apply(ctx: Ctx) -> None:
    _run(["git", "config", "--local", "core.hooksPath", "githooks"], ctx)


def _setup_hooks_check(_ctx: Ctx) -> None:
    result = subprocess.run(
        ["git", "config", "--local", "--get", "core.hooksPath"],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0 or result.stdout.strip() != "githooks":
        print("error: Git hooks are not configured to use githooks", file=sys.stderr)
        print("repair: py -3 tools/run.py setup-hooks --apply", file=sys.stderr)
        raise BusTargetError("local core.hooksPath must be githooks")
    print("OK local core.hooksPath is githooks")


def _ci_check(ctx: Ctx) -> None:
    failures: list[tuple[str, str, str]] = []
    for name, check, repair, recheck in CI_CHECKS:
        try:
            check(ctx)
        except Exception as exc:
            if not ctx.diagnostics:
                print(f"[tools/run] check '{name}' failed: {exc}", file=sys.stderr)
                print(f"[tools/run] repair: {repair}", file=sys.stderr)
                print(f"[tools/run] focused recheck: {recheck}", file=sys.stderr)
                print("[tools/run] full recheck: py -3 tools/run.py ci --check", file=sys.stderr)
                raise
            failures.append((name, repair, recheck))
    if failures:
        print("[tools/run] diagnostic failures:", file=sys.stderr)
        for name, repair, recheck in failures:
            print(f"  {name}: {repair}; recheck: {recheck}", file=sys.stderr)
        raise CiDiagnosticsError("one or more CI checks failed")


TARGETS = {
    "ci": {"check": _ci_check},
    "dotnet-build": {"check": _build_dotnet},
    "dotnet-test": {"check": _test_dotnet},
    "web": {"check": _build_web},
    "python-tests": {"check": _python_tests},
    "setup-hooks": {"apply": _setup_hooks_apply, "check": _setup_hooks_check},
}

TARGET_DESCRIPTIONS = {
    "ci": (
        "Run the complete repository check gate. --check validates the selected candidate, "
        "including Python tests, .NET build/tests, web checks/build, version identity, and whitespace. "
        "Prerequisites: Python 3, .NET SDK, Node.js/npm, and the configured PostgreSQL test service. "
        "Checks may create ignored build outputs but do not repair maintained files. "
        "Manual --diagnostics continues after failures for troubleshooting; the commit/CI gate is fail-fast. "
        "Target-specific arguments: none."
    ),
    "dotnet-build": (
        "Build the .NET solution without modifying maintained source. Mode: --check. "
        "Prerequisite: the .NET SDK. Build outputs are disposable ignored files. "
        "Arguments after -- are forwarded to `dotnet build`."
    ),
    "dotnet-test": (
        "Run the .NET test suites, including PostgreSQL-backed integration tests. Mode: --check. "
        "Prerequisites: the .NET SDK and PostgreSQL at localhost:5435. "
        "Arguments after -- are forwarded to `dotnet test`."
    ),
    "web": (
        "Install locked dependencies, typecheck, test, and build the web app. Mode: --check. "
        "Prerequisites: Node.js/npm. `npm ci` may create ignored node_modules and build outputs. "
        "Target-specific arguments: none."
    ),
    "python-tests": (
        "Run repository script and command-bus behavior tests. Mode: --check. "
        "Prerequisite: Python 3 with pytest. Arguments after -- are forwarded to both pytest suites."
    ),
    "setup-hooks": (
        "Set or verify this checkout's Git hook path. --apply sets local core.hooksPath to githooks; "
        "--check verifies that exact value. --apply changes local Git configuration, not tracked files. "
        "Prerequisite: Git. Target-specific arguments: none."
    ),
}

TARGET_ARGUMENTS = {"dotnet-build", "dotnet-test", "python-tests"}


def _run_target(target: str, ctx: Ctx) -> None:
    print(f"[tools/run] === {target} ({ctx.mode})")
    TARGETS[target][ctx.mode](ctx)
    print(f"[tools/run] {target} {ctx.mode} passed")


def _root_parser() -> argparse.ArgumentParser:
    target_list = "\n".join(f"  {name}: {summary}" for name, summary in TARGET_DESCRIPTIONS.items())
    parser = argparse.ArgumentParser(
        description=f"Wild Bunch repository command bus. Available targets:\n{target_list}"
    )
    parser.add_argument("target", nargs="?", choices=list(TARGETS), help="named repository operation")
    return parser


def _target_parser(target: str) -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog=f"{Path(sys.argv[0]).name} {target}",
        description=TARGET_DESCRIPTIONS[target],
    )
    modes = TARGETS[target]
    group = parser.add_mutually_exclusive_group()
    if "apply" in modes:
        group.add_argument("--apply", action="store_true", help="apply the target's documented changes")
    if "check" in modes:
        group.add_argument("--check", action="store_true", help="check without changing maintained files")
    if target == "setup-hooks":
        parser.add_argument(
            "--allow-shared-checkout",
            action="store_true",
            help="allow hook setup in a shared checkout",
        )
    if target == "ci":
        parser.add_argument(
            "--diagnostics",
            action="store_true",
            help="manual troubleshooting only: continue after independent failures; not the commit/CI gate",
        )
    parser.add_argument("--verbose", "-v", action="store_true", help="print each sub-command")
    if target in TARGET_ARGUMENTS:
        parser.add_argument("target_args", nargs=argparse.REMAINDER, help="arguments forwarded to the selected target")
    return parser


def main(argv: list[str] | None = None) -> int:
    arguments = list(sys.argv[1:] if argv is None else argv)
    root_parser = _root_parser()
    if not arguments or arguments in (["--help"], ["-h"]):
        root_parser.print_help()
        return 0

    target = arguments[0]
    if target not in TARGETS:
        root_parser.parse_args(arguments)
        return 2
    parser = _target_parser(target)
    args = parser.parse_args(arguments[1:])
    args.apply = getattr(args, "apply", False)
    args.check = getattr(args, "check", False)
    target_args = tuple(getattr(args, "target_args", ()))
    args.target_args = target_args[1:] if target_args[:1] == ("--",) else target_args

    if not args.apply and not args.check:
        parser.print_usage(sys.stderr)
        print("error: select --check, --apply, or --help", file=sys.stderr)
        return 2
    allow_shared_checkout = getattr(args, "allow_shared_checkout", False)
    if allow_shared_checkout and not args.apply:
        print("error: --allow-shared-checkout requires --apply", file=sys.stderr)
        return 1
    diagnostics = getattr(args, "diagnostics", False)
    if diagnostics and (args.apply or not args.check):
        print("error: --diagnostics requires ci --check", file=sys.stderr)
        return 1

    mode = "apply" if args.apply else "check"
    ctx = Ctx(
        mode=mode,
        allow_shared=allow_shared_checkout,
        verbose=args.verbose,
        diagnostics=diagnostics,
        target_args=args.target_args,
    )

    if args.apply:
        if not shared_checkout.approve_mutation(ROOT, SCRIPT_NAME, allow_shared_checkout):
            return 1

    try:
        _run_target(target, ctx)
    except subprocess.CalledProcessError as exc:
        print(f"[tools/run] target '{target}' failed: {exc}", file=sys.stderr)
        return exc.returncode or 1
    except (BusTargetError, CiDiagnosticsError, VersionIdentityError) as exc:
        print(f"[tools/run] target '{target}' failed: {exc}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
