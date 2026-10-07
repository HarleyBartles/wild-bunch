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


class CiDiagnosticsError(RuntimeError):
    """One or more independent CI checks failed in diagnostic mode."""


def _run(cmd: list[str], ctx: Ctx) -> None:
    if ctx.verbose:
        print("+ " + " ".join(cmd))
    subprocess.run(cmd, cwd=ROOT, check=True)


def _dotnet_build_cmd() -> list[str]:
    return ["dotnet", "build"]


def _dotnet_test_cmd() -> list[str]:
    return ["dotnet", "test"]


def _npm_cmd(*args: str) -> list[str]:
    return [shutil.which("npm") or "npm", "--prefix", str(WEB_DIR), *args]


def _operating_standards_check(ctx: Ctx) -> None:
    _run([sys.executable, "scripts/check_operating_standards.py", "--check"], ctx)


def _agent_routers_check(ctx: Ctx) -> None:
    _run([sys.executable, "scripts/check_agent_routers.py", "--check"], ctx)


def _plugin_subscriptions_check(ctx: Ctx) -> None:
    _run([sys.executable, "scripts/check_plugin_subscriptions.py", "--check"], ctx)


def _test_script_behaviors(ctx: Ctx) -> None:
    _run([sys.executable, "-m", "pytest", "scripts/tests", "-q"], ctx)


def _test_tool_behaviors(ctx: Ctx) -> None:
    _run([sys.executable, "-m", "pytest", "tools/tests", "-q"], ctx)


def _activate_hook(ctx: Ctx) -> None:
    _run(["git", "config", "core.hooksPath", "githooks"], ctx)


def _build_dotnet(ctx: Ctx) -> None:
    _run(_dotnet_build_cmd(), ctx)


def _test_dotnet(ctx: Ctx) -> None:
    os.environ["ConnectionStrings__WildBunchPostgresDb"] = "Host=localhost;Port=5435;Database=wildbunch_dev;Username=postgres"
    _run(_dotnet_test_cmd(), ctx)


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
    ("operating-standards", _operating_standards_check, "repair the subscription or certification record"),
    ("agent-routers", _agent_routers_check, "repair the reported AGENTS.md router contract"),
    ("plugin-subscriptions", _plugin_subscriptions_check, "repair the native Codex plugin declaration"),
    ("script-behavior-tests", _test_script_behaviors, "py -3 -m pytest scripts/tests -q"),
    ("tool-behavior-tests", _test_tool_behaviors, "py -3 -m pytest tools/tests -q"),
    ("dotnet-build", _build_dotnet, "dotnet build"),
    ("dotnet-test", _test_dotnet, "dotnet test"),
    ("web", _build_web, "npm --prefix src/WildBunch.Web run build"),
    (
        "build-identity",
        _version_identity_check,
        "remove duplicate npm application-version fields or rebuild the web artifact",
    ),
    (
        "diff-check",
        _diff_check,
        "git diff --check HEAD (or git diff --check <commit>^ <commit> for hosted mode)",
    ),
)


def _ci_apply(ctx: Ctx) -> None:
    _activate_hook(ctx)
    _operating_standards_check(ctx)
    _agent_routers_check(ctx)
    _plugin_subscriptions_check(ctx)


def _ci_check(ctx: Ctx) -> None:
    failures: list[tuple[str, str]] = []
    for name, check, fix in CI_CHECKS:
        try:
            check(ctx)
        except Exception:
            if not ctx.diagnostics:
                raise
            failures.append((name, fix))
    if failures:
        print("[tools/run] diagnostic failures:", file=sys.stderr)
        for name, fix in failures:
            print(f"  {name}: {fix}", file=sys.stderr)
        raise CiDiagnosticsError("one or more CI checks failed")


TARGETS = {
    "ci": {"apply": _ci_apply, "check": _ci_check},
}


def _run_target(target: str, ctx: Ctx) -> None:
    print(f"[tools/run] === {target} ({ctx.mode})")
    TARGETS[target][ctx.mode](ctx)
    print(f"[tools/run] {target} {ctx.mode} passed")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Canonical task runner for the Wild Bunch repo")
    parser.add_argument("target", choices=list(TARGETS.keys()), help="target to run")
    group = parser.add_mutually_exclusive_group()
    group.add_argument("--apply", action="store_true", help="apply (write) mode")
    group.add_argument("--check", action="store_true", help="check (read-only) mode")
    parser.add_argument(
        "--allow-shared-checkout",
        action="store_true",
        help="allow writes in a shared/main checkout",
    )
    parser.add_argument(
        "--diagnostics",
        action="store_true",
        help="collect all independent failures (ci --check only)",
    )
    parser.add_argument("--verbose", "-v", action="store_true", help="print each sub-command")
    args = parser.parse_args(argv)

    if not args.apply and not args.check:
        args.check = True
    if args.allow_shared_checkout and not args.apply:
        print("error: --allow-shared-checkout requires --apply", file=sys.stderr)
        return 1
    if args.diagnostics and (args.apply or not args.check):
        print("error: --diagnostics requires ci --check", file=sys.stderr)
        return 1

    mode = "apply" if args.apply else "check"
    ctx = Ctx(
        mode=mode,
        allow_shared=args.allow_shared_checkout,
        verbose=args.verbose,
        diagnostics=args.diagnostics,
    )

    if args.apply:
        if not shared_checkout.approve_mutation(ROOT, SCRIPT_NAME, args.allow_shared_checkout):
            return 1

    try:
        _run_target(args.target, ctx)
    except (subprocess.CalledProcessError, CiDiagnosticsError, VersionIdentityError) as exc:
        print(f"[tools/run] target '{args.target}' failed: {exc}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
