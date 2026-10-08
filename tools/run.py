#!/usr/bin/env python3
"""Canonical task runner for the Wild Bunch repo."""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

import shared_checkout
import style
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


def _web_dependencies(ctx: Ctx) -> None:
    binaries = WEB_DIR / "node_modules" / ".bin"
    if not (binaries / "eslint").exists() or not (binaries / "prettier").exists():
        _run(_npm_cmd("ci"), ctx)
        return

    lock = json.loads((WEB_DIR / "package-lock.json").read_text(encoding="utf-8"))
    tool_names = (
        "@eslint/js",
        "eslint",
        "eslint-config-prettier",
        "eslint-plugin-react-hooks",
        "prettier",
        "typescript-eslint",
    )
    mismatched = []
    for name in tool_names:
        lock_entry = lock.get("packages", {}).get(f"node_modules/{name}")
        installed_path = WEB_DIR / "node_modules" / Path(name) / "package.json"
        if lock_entry is None or not installed_path.is_file():
            mismatched.append(name)
            continue
        installed = json.loads(installed_path.read_text(encoding="utf-8"))
        if installed.get("version") != lock_entry.get("version"):
            mismatched.append(name)
    if mismatched:
        raise BusTargetError(
            "web style dependencies do not match the lockfile ("
            + ", ".join(mismatched)
            + "); repair: npm --prefix src/WildBunch.Web ci"
        )


def _style_files(ctx: Ctx, *, mutation: bool) -> dict[str, list[str]]:
    try:
        return style.select_files(ROOT, ctx.target_args, allow_default_scope=not mutation)
    except style.StylePathError as exc:
        raise BusTargetError(str(exc)) from exc


def _format(ctx: Ctx) -> None:
    groups = _style_files(ctx, mutation=ctx.mode == "apply")
    commands = style.formatter_commands(groups, check=ctx.mode == "check")
    before = {path: (ROOT / path).read_bytes() for paths in groups.values() for path in paths}
    for language, command in commands:
        if language == "web":
            _web_dependencies(ctx)
        try:
            _run(command, ctx)
        except subprocess.CalledProcessError:
            if ctx.mode == "check":
                print(
                    "[tools/run] repair: py -3 tools/run.py format --apply <diagnostic-path>",
                    file=sys.stderr,
                )
                print(
                    "[tools/run] focused recheck: py -3 tools/run.py format --check "
                    "<diagnostic-path>",
                    file=sys.stderr,
                )
            raise
    if ctx.mode == "apply":
        changed = [
            path for path, original in before.items() if (ROOT / path).read_bytes() != original
        ]
        if changed:
            print("[tools/run] formatted files:")
            for path in changed:
                print(f"  {path}")
        else:
            print("[tools/run] format apply found no changes")


def _lint(ctx: Ctx) -> None:
    groups = _style_files(ctx, mutation=False)
    for language, command in style.linter_commands(groups):
        if language == "web":
            _web_dependencies(ctx)
        try:
            _run(command, ctx)
        except subprocess.CalledProcessError:
            print(
                "[tools/run] repair: correct the reported lint diagnostics; "
                "this target never auto-fixes",
                file=sys.stderr,
            )
            print(
                "[tools/run] focused recheck: py -3 tools/run.py lint --check <diagnostic-path>",
                file=sys.stderr,
            )
            raise


def _check_python_format(ctx: Ctx) -> None:
    groups = style.select_files(ROOT, (), allow_default_scope=True)
    for language, command in style.formatter_commands(groups, check=True):
        if language == "python":
            _run(command, ctx)


def _check_python_lint(ctx: Ctx) -> None:
    groups = style.select_files(ROOT, (), allow_default_scope=True)
    for language, command in style.linter_commands(groups):
        if language == "python":
            _run(command, ctx)


def _check_web_style(ctx: Ctx) -> None:
    groups = style.select_files(ROOT, (), allow_default_scope=True)
    if "web" not in groups:
        return
    _web_dependencies(ctx)
    for language, command in style.formatter_commands(groups, check=True):
        if language == "web":
            _run(command, ctx)
    for language, command in style.linter_commands(groups):
        if language == "web":
            _run(command, ctx)


def _check_dotnet_format(ctx: Ctx) -> None:
    groups = style.select_files(ROOT, (), allow_default_scope=True)
    for language, command in style.formatter_commands(groups, check=True):
        if language == "dotnet":
            _run(command, ctx)


def _check_dotnet_lint(ctx: Ctx) -> None:
    groups = style.select_files(ROOT, (), allow_default_scope=True)
    for language, command in style.linter_commands(groups):
        if language == "dotnet":
            _run(command, ctx)


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
    os.environ["ConnectionStrings__WildBunchPostgresDb"] = (
        "Host=localhost;Port=5435;Database=wildbunch_dev;Username=postgres"
    )
    _run(_dotnet_test_cmd(ctx.target_args), ctx)


def _build_web(ctx: Ctx) -> None:
    _check_web_style(ctx)
    _run(_npm_cmd("run", "typecheck"), ctx)
    _run(_npm_cmd("run", "test"), ctx)
    _run(_npm_cmd("run", "build"), ctx)


def _web_typecheck(ctx: Ctx) -> None:
    _run(_npm_cmd("run", "typecheck"), ctx)


def _web_test(ctx: Ctx) -> None:
    _run(_npm_cmd("run", "test"), ctx)


def _web_build(ctx: Ctx) -> None:
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
        "python-format",
        _check_python_format,
        "run py -3 tools/run.py format --apply <diagnostic-path> and review the mechanical diff",
        "py -3 tools/run.py format --check <diagnostic-path>",
    ),
    (
        "python-lint",
        _check_python_lint,
        "correct Ruff diagnostics without using automatic lint fixes",
        "py -3 tools/run.py lint --check <diagnostic-path>",
    ),
    (
        "web-style",
        _check_web_style,
        "format the reported source mechanically, then correct ESLint diagnostics explicitly",
        "py -3 tools/run.py format --check <diagnostic-path> and "
        "py -3 tools/run.py lint --check <diagnostic-path>",
    ),
    (
        "dotnet-format",
        _check_dotnet_format,
        "run py -3 tools/run.py format --apply <diagnostic-path> and review the mechanical diff",
        "py -3 tools/run.py format --check <diagnostic-path>",
    ),
    (
        "dotnet-analyzers",
        _check_dotnet_lint,
        "correct SDK analyzer diagnostics explicitly",
        "py -3 tools/run.py lint --check <diagnostic-path>",
    ),
    (
        "dotnet-build",
        _build_dotnet,
        "correct the reported compiler or build error",
        "py -3 tools/run.py dotnet-build --check",
    ),
    (
        "web-typecheck",
        _web_typecheck,
        "correct the reported TypeScript error",
        "npm --prefix src/WildBunch.Web run typecheck",
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
        "dotnet-test",
        _test_dotnet,
        "correct the failing .NET test or implementation",
        "py -3 tools/run.py dotnet-test --check",
    ),
    (
        "web-tests",
        _web_test,
        "correct the failing web behavior test",
        "npm --prefix src/WildBunch.Web run test",
    ),
    (
        "web-build",
        _web_build,
        "correct the reported web build error",
        "npm --prefix src/WildBunch.Web run build",
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
    "format": {"check": _format, "apply": _format},
    "lint": {"check": _lint},
    "dotnet-build": {"check": _build_dotnet},
    "dotnet-test": {"check": _test_dotnet},
    "web": {"check": _build_web},
    "python-tests": {"check": _python_tests},
    "setup-hooks": {"apply": _setup_hooks_apply, "check": _setup_hooks_check},
}

TARGET_DESCRIPTIONS = {
    "ci": (
        "Run the complete repository check gate in cheapest-first order: whitespace and repository "
        "contracts; Python, web, and .NET format/lint; .NET build and web typecheck; then behavior "
        "tests and web build. A normal gate stops at the first failure. Prerequisites: Python 3 "
        "with "
        "repo requirements, .NET SDK 10.0.301, Node.js 20.19+ with npm, and PostgreSQL at "
        "localhost:5435 for .NET integration tests. Checks may create ignored dependency/build "
        "outputs but do not repair maintained files. Manual --diagnostics continues after "
        "independent failures; it is never used by commit/CI gates. "
        "Target-specific arguments: none."
    ),
    "format": (
        "Check formatting without changes or apply formatting to explicit supported file paths; "
        "--all is required for a mutating whole-repository pass. Omitting paths in --check checks "
        "the full supported scope. C# uses `dotnet format whitespace WildBunch.sln --include`, "
        "Python uses `python -m ruff format`, and web JavaScript/TypeScript/TSX/SCSS "
        "uses `npm --prefix "
        "src/WildBunch.Web exec -- prettier`. Markdown, generated, "
        "dependency, vendor, and build files are excluded. --apply changes only selected source "
        "files and reports changed paths. Prerequisites: .NET SDK for C#, Ruff from "
        "`tools/requirements.txt` for Python, and locked npm dependencies for web files."
    ),
    "lint": (
        "Run check-only language diagnostics; this target never applies lint fixes. Omitted paths "
        "check the full supported scope. Python uses `python -m ruff check`, web "
        "JavaScript/TypeScript/TSX "
        "uses `npm --prefix src/WildBunch.Web exec -- eslint --config "
        "src/WildBunch.Web/eslint.config.js` with type-aware TypeScript rules and core "
        "React Hooks rules, "
        "and C# uses "
        "`dotnet format analyzers` with SDK analyzers. Markdown, generated, dependency, vendor, "
        "and build files are excluded. Correct diagnostics explicitly, then rerun this target. "
        "Prerequisites: Python with `tools/requirements.txt`, locked web dependencies for TS/TSX, "
        "and .NET SDK for C#."
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
        "Install locked dependencies, format-check, lint, typecheck, test, and build the web app. "
        "Mode: --check. "
        "Prerequisites: Node.js/npm. `npm ci` may create ignored node_modules and build outputs. "
        "Target-specific arguments: none."
    ),
    "python-tests": (
        "Run repository script and command-bus behavior tests. Mode: --check. "
        "Prerequisite: Python 3 with pytest. Arguments after -- are forwarded to both "
        "pytest suites."
    ),
    "setup-hooks": (
        "Set or verify this checkout's Git hook path. --apply sets local core.hooksPath to "
        "githooks; --check verifies that exact value. --apply changes local Git configuration, "
        "not tracked files. "
        "Prerequisite: Git. Target-specific arguments: none."
    ),
}

TARGET_ARGUMENTS = {"format", "lint", "dotnet-build", "dotnet-test", "python-tests"}


def _run_target(target: str, ctx: Ctx) -> None:
    print(f"[tools/run] === {target} ({ctx.mode})")
    TARGETS[target][ctx.mode](ctx)
    print(f"[tools/run] {target} {ctx.mode} passed")


def _root_parser() -> argparse.ArgumentParser:
    target_list = "\n".join(f"  {name}: {summary}" for name, summary in TARGET_DESCRIPTIONS.items())
    parser = argparse.ArgumentParser(
        description=f"Wild Bunch repository command bus. Available targets:\n{target_list}"
    )
    parser.add_argument(
        "target", nargs="?", choices=list(TARGETS), help="named repository operation"
    )
    return parser


def _target_parser(target: str) -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog=f"{Path(sys.argv[0]).name} {target}",
        description=TARGET_DESCRIPTIONS[target],
    )
    modes = TARGETS[target]
    group = parser.add_mutually_exclusive_group()
    if "apply" in modes:
        group.add_argument(
            "--apply", action="store_true", help="apply the target's documented changes"
        )
    if "check" in modes:
        group.add_argument(
            "--check", action="store_true", help="check without changing maintained files"
        )
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
            help="manual troubleshooting only: continue after independent failures; "
            "not the commit/CI gate",
        )
    if target == "format":
        parser.add_argument(
            "--all",
            action="store_true",
            help="explicitly select all supported repository files for format --apply",
        )
    parser.add_argument("--verbose", "-v", action="store_true", help="print each sub-command")
    if target in TARGET_ARGUMENTS:
        parser.add_argument(
            "target_args",
            nargs=argparse.REMAINDER,
            help=(
                "file paths for format/lint or arguments forwarded to the selected target; "
                "format --apply requires paths or --all"
            ),
        )
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
    if getattr(args, "all", False):
        args.target_args = (*args.target_args, "--all")

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
