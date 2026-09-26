#!/usr/bin/env python3
"""(mixed: resolve scratch custody; --check previews, --apply writes markers)"""

from __future__ import annotations

import argparse
import hashlib
import os
import re
import shutil
import subprocess
import tempfile
from pathlib import Path


IDENTITY_FILE = ".plan-identity"


def _git(cwd: Path, *args: str) -> str:
    return subprocess.run(
        ["git", *args],
        cwd=cwd,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        check=True,
    ).stdout.strip()


def _sanitize(value: str) -> str:
    cleaned = re.sub(r"[^A-Za-z0-9._-]+", "-", value.strip()).strip("-.")
    return cleaned or "unnamed"


def _identity_segment(label: str, identity: str) -> str:
    digest = hashlib.sha256(os.path.normcase(identity).encode("utf-8")).hexdigest()[:8]
    return f"{_sanitize(label)}-{digest}"


def _atomic_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", newline="\n", dir=path.parent, delete=False) as handle:
        handle.write(content)
        temporary = Path(handle.name)
    os.replace(temporary, path)


def resolve_plan(plan_file: str | None, cwd: Path) -> tuple[Path | None, Path]:
    if plan_file:
        plan = Path(plan_file)
        if not plan.is_absolute():
            root = Path(_git(cwd, "rev-parse", "--show-toplevel"))
            plan = root / plan
        plan = plan.resolve()
        if not plan.is_file():
            raise FileNotFoundError(f"no such plan file: {plan}")
        repo_root = Path(_git(plan.parent, "rev-parse", "--show-toplevel"))
        return plan, repo_root
    return None, Path(_git(cwd, "rev-parse", "--show-toplevel"))


def workspace_path(plan_file: str | None, cwd: Path, apply: bool) -> Path:
    plan, repo_root = resolve_plan(plan_file, cwd)
    common_dir = Path(_git(repo_root, "rev-parse", "--git-common-dir"))
    if not common_dir.is_absolute():
        common_dir = (repo_root / common_dir).resolve()
    main_checkout = common_dir.parent
    scratch_parent = main_checkout.parent
    branch_name = _git(repo_root, "rev-parse", "--abbrev-ref", "HEAD")
    repo_name = _identity_segment(main_checkout.name, str(main_checkout.resolve()))
    branch = _identity_segment(branch_name, branch_name)
    workspace_root = scratch_parent / "_agent-scratch" / repo_name / branch

    legacy_roots = (
        scratch_parent / "_agent-scratch" / _sanitize(main_checkout.name) / _sanitize(branch_name),
        scratch_parent / "_agent-scratch" / _sanitize(branch_name),
    )
    if apply and not workspace_root.exists():
        for legacy_root in legacy_roots:
            if legacy_root.is_dir():
                workspace_root.parent.mkdir(parents=True, exist_ok=True)
                shutil.copytree(legacy_root, workspace_root)
                break

    if apply:
        workspace_root.mkdir(parents=True, exist_ok=True)
        workspace_identity = f"{repo_root.resolve()}\n{branch_name}"
        root_marker = workspace_root / ".workspace-identity"
        if root_marker.exists() and root_marker.read_text(encoding="utf-8").strip() != workspace_identity:
            raise RuntimeError(f"workspace root identity collision at {workspace_root}")
        if not root_marker.exists():
            _atomic_text(root_marker, workspace_identity)

    if plan is None:
        return workspace_root

    identity = str(plan)
    stem = _sanitize(plan.stem)
    existing: list[tuple[int, Path]] = []
    if workspace_root.is_dir():
        pattern = re.compile(rf"^{re.escape(stem)}(?:-([0-9]+))?$")
        for path in workspace_root.iterdir():
            if not path.is_dir() or not (match := pattern.fullmatch(path.name)):
                continue
            existing.append((int(match.group(1) or "1"), path))
    for _, path in sorted(existing):
        marker = path / IDENTITY_FILE
        marker_identity = os.path.normcase(marker.read_text(encoding="utf-8").strip()) if marker.is_file() else None
        if marker_identity == os.path.normcase(identity):
            candidate = path
            break
    else:
        base_candidate = workspace_root / stem
        if base_candidate.is_dir() and not (base_candidate / IDENTITY_FILE).exists():
            candidate = base_candidate
        else:
            used = {number for number, _ in existing}
            counter = 1
            while counter in used:
                counter += 1
            suffix = "" if counter == 1 else f"-{counter}"
            candidate = workspace_root / f"{stem}{suffix}"
    marker = candidate / IDENTITY_FILE

    if apply:
        candidate.mkdir(parents=True, exist_ok=True)
        if marker.exists():
            existing = marker.read_text(encoding="utf-8").strip()
            if os.path.normcase(existing) != os.path.normcase(identity):
                raise RuntimeError(f"workspace identity collision at {candidate}")
        else:
            _atomic_text(marker, identity)
        _atomic_text(workspace_root / "current-plan.txt", candidate.name)
    return candidate


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="mixed: resolve scratch custody; --check previews and --apply writes markers"
    )
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="resolve without writing (default)")
    mode.add_argument("--apply", action="store_true", help="create/adopt the workspace and markers")
    parser.add_argument("plan_file", nargs="?", help="optional implementation plan")
    return parser


def main() -> int:
    args = build_parser().parse_args()
    try:
        print(workspace_path(args.plan_file, Path.cwd(), args.apply))
    except (FileNotFoundError, subprocess.CalledProcessError, RuntimeError) as exc:
        print(str(exc), file=__import__("sys").stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
