#!/usr/bin/env python3
"""Canonical task runner for the Wild Bunch repo."""

from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

import shared_checkout


ROOT = Path(__file__).resolve().parent.parent
SCRIPT_NAME = "tools/run"
WEB_DIR = ROOT / "src" / "WildBunch.Web"


@dataclass(frozen=True)
class Ctx:
    mode: str  # "apply" or "check"
    allow_shared: bool
    verbose: bool = False


def _run(cmd: list[str], ctx: Ctx) -> None:
    if ctx.verbose:
        print("+ " + " ".join(cmd))
    subprocess.run(cmd, cwd=ROOT, check=True)


def _repo_standards_cmd(mode: str, allow_shared: bool) -> list[str]:
    cmd = [
        sys.executable,
        ".agents/skills/repo-standards/scripts/repo_standards.py",
        f"--{mode}",
        "--yes",
    ]
    if mode == "apply" and allow_shared:
        cmd.append("--allow-shared-checkout")
    return cmd


def _skills_cmd(mode: str, allow_shared: bool) -> list[str]:
    cmd = [
        sys.executable,
        ".agents/skills/refreshing-installed-skills/scripts/refresh_installed_skills.py",
        f"--{mode}",
    ]
    if mode == "apply" and allow_shared:
        cmd.append("--allow-shared-checkout")
    return cmd


def _mesh_generate_cmd(mode: str, allow_shared: bool) -> list[str]:
    cmd = [
        sys.executable,
        ".agents/skills/generating-agent-mesh/scripts/generate_index_mesh.py",
        f"--{mode}",
    ]
    if mode == "apply" and allow_shared:
        cmd.append("--allow-shared-checkout")
    return cmd


def _mesh_validate_cmd() -> list[str]:
    return [
        sys.executable,
        ".agents/skills/generating-agent-mesh/scripts/validate_agent_mesh.py",
        "--check",
    ]


def _dotnet_build_cmd() -> list[str]:
    return ["dotnet", "build"]


def _dotnet_test_cmd() -> list[str]:
    return ["dotnet", "test"]


def _npm_cmd(*args: str) -> list[str]:
    return [shutil.which("npm") or "npm", "--prefix", str(WEB_DIR), *args]


def _repo_standards_apply(ctx: Ctx) -> None:
    _run(_repo_standards_cmd("apply", ctx.allow_shared), ctx)
    _run(_repo_standards_cmd("check", ctx.allow_shared), ctx)


def _repo_standards_check(ctx: Ctx) -> None:
    _run(_repo_standards_cmd("check", ctx.allow_shared), ctx)


def _skills_apply(ctx: Ctx) -> None:
    _run(_skills_cmd("apply", ctx.allow_shared), ctx)
    _run(_skills_cmd("check", ctx.allow_shared), ctx)


def _skills_check(ctx: Ctx) -> None:
    _run(_skills_cmd("check", ctx.allow_shared), ctx)


def _mesh_apply(ctx: Ctx) -> None:
    _run(_mesh_generate_cmd("apply", ctx.allow_shared), ctx)
    _run(_mesh_generate_cmd("check", ctx.allow_shared), ctx)
    _run(_mesh_validate_cmd(), ctx)


def _mesh_check(ctx: Ctx) -> None:
    _run(_mesh_generate_cmd("check", ctx.allow_shared), ctx)
    _run(_mesh_validate_cmd(), ctx)


def _build_dotnet(ctx: Ctx) -> None:
    _run(_dotnet_build_cmd(), ctx)


def _test_dotnet(ctx: Ctx) -> None:
    _run(_dotnet_test_cmd(), ctx)


def _build_web(ctx: Ctx) -> None:
    _run(_npm_cmd("ci"), ctx)
    _run(_npm_cmd("run", "typecheck"), ctx)
    _run(_npm_cmd("run", "test"), ctx)
    _run(_npm_cmd("run", "build"), ctx)


def _diff_check(ctx: Ctx) -> None:
    _run(["git", "diff", "--check", "--", ".", ":(exclude).agents/skills"], ctx)


def _ci_apply(ctx: Ctx) -> None:
    _repo_standards_apply(ctx)
    _skills_apply(ctx)
    _mesh_apply(ctx)
    _build_dotnet(ctx)
    _test_dotnet(ctx)
    _build_web(ctx)
    _diff_check(ctx)


def _ci_check(ctx: Ctx) -> None:
    _repo_standards_check(ctx)
    _skills_check(ctx)
    _mesh_check(ctx)
    _build_dotnet(ctx)
    _test_dotnet(ctx)
    _build_web(ctx)
    _diff_check(ctx)


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
    parser.add_argument("--verbose", "-v", action="store_true", help="print each sub-command")
    args = parser.parse_args(argv)

    if not args.apply and not args.check:
        args.check = True
    if args.allow_shared_checkout and not args.apply:
        print("error: --allow-shared-checkout requires --apply", file=sys.stderr)
        return 1

    mode = "apply" if args.apply else "check"
    ctx = Ctx(mode=mode, allow_shared=args.allow_shared_checkout, verbose=args.verbose)

    if args.apply:
        if not shared_checkout.approve_mutation(ROOT, SCRIPT_NAME, args.allow_shared_checkout):
            return 1

    try:
        _run_target(args.target, ctx)
    except subprocess.CalledProcessError as exc:
        print(f"[tools/run] target '{args.target}' failed: {exc}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
