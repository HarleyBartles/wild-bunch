#!/usr/bin/env python3
"""Check or apply the repo-standards shape."""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

import _agents_md


def _stripped_env() -> dict[str, str]:
    env = os.environ.copy()
    env.pop("GIT_DIR", None)
    env.pop("GIT_WORK_TREE", None)
    env.pop("GIT_INDEX_FILE", None)
    return env


def _repo_root() -> Path:
    result = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True, env=_stripped_env(),
    )
    return Path(result.stdout.strip())


def _is_shared_checkout(repo_root: Path) -> bool:
    git_dir = subprocess.run(
        ["git", "rev-parse", "--absolute-git-dir"],
        cwd=repo_root, capture_output=True, text=True, check=True, env=_stripped_env(),
    ).stdout.strip()
    git_common = subprocess.run(
        ["git", "rev-parse", "--git-common-dir"],
        cwd=repo_root, capture_output=True, text=True, check=True, env=_stripped_env(),
    ).stdout.strip()
    # A linked worktree (shared checkout) has its git-dir under .git/worktrees/<name>
    # while the common dir is the main .git directory.
    return Path(git_dir).resolve() != Path(git_common).resolve()


def _is_submodule(repo_root: Path) -> bool:
    result = subprocess.run(
        ["git", "rev-parse", "--show-superproject-working-tree"],
        cwd=repo_root, capture_output=True, text=True, env=_stripped_env(),
    )
    return result.returncode == 0 and result.stdout.strip()


def _manifest_path() -> Path:
    return Path(__file__).resolve().parent.parent / "references" / "repository-shape-manifest.json"


def _load_exceptions(repo_root: Path) -> set[str]:
    exceptions: set[str] = set()
    policy = repo_root / ".agents" / "docs" / "repo-guide-policy.md"
    if not policy.is_file():
        return exceptions
    text = policy.read_text(encoding="utf-8")
    in_exceptions = False
    for line in text.splitlines():
        stripped = line.strip()
        if stripped.startswith("## "):
            in_exceptions = stripped.lower().startswith("## exceptions")
            continue
        if not in_exceptions:
            continue
        if stripped.startswith("-"):
            item = stripped.lstrip("-").strip().replace("`", "")
            # Allow "id - reason", "id -- reason", or "id — reason"
            for sep in (" -- ", " - ", " — ", " – "):
                if sep in item:
                    item = item.split(sep, 1)[0]
                    break
            if item:
                exceptions.add(item)
    return exceptions


def _template_path(surface: dict[str, object]) -> Path | None:
    source = surface.get("source")
    if not source:
        return None
    return Path(__file__).resolve().parent.parent / str(source)


def _scaffold_script_path(surface: dict[str, object]) -> Path | None:
    scaffold = surface.get("scaffold")
    if not scaffold:
        return None
    return Path(__file__).resolve().parent / str(scaffold)


def _git_hooks_dir(repo_root: Path) -> Path:
    result = subprocess.run(
        ["git", "rev-parse", "--git-path", "hooks"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        check=True,
        env=_stripped_env(),
    )
    return Path(result.stdout.strip())


def _check_surface_content(repo_root: Path, rel: str, template: Path | None) -> list[str]:
    findings: list[str] = []
    full = repo_root / rel
    if not full.is_file():
        findings.append(f"missing: {rel}")
        return findings
    if template is not None and template.is_file():
        expected = template.read_bytes()
        actual = full.read_bytes()
        if expected != actual:
            findings.append(f"drift: {rel}")
    return findings


def _run_scaffold_check(scaffold: Path, repo_root: Path) -> list[str]:
    findings: list[str] = []
    result = subprocess.run(
        [sys.executable, str(scaffold), "--check"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        env=_stripped_env(),
    )
    output = result.stdout + result.stderr
    for line in output.splitlines():
        if line.startswith("DRIFT:"):
            findings.append(line[6:].strip())
    if result.returncode != 0 and not findings:
        findings.append(f"scaffold check failed: {scaffold.name}")
    return findings


def _check_surface(repo_root: Path, surface: dict[str, object], exceptions: set[str]) -> list[str]:
    findings: list[str] = []
    rel = str(surface["path"])
    surf_id = str(surface.get("id", ""))
    if surf_id in exceptions or rel in exceptions:
        return findings
    kind = str(surface.get("kind", "file"))
    optional = bool(surface.get("optional", False))
    template = _template_path(surface)
    scaffold = _scaffold_script_path(surface)
    full = repo_root / rel

    if kind == "submodule":
        gitmodules = repo_root / ".gitmodules"
        if not gitmodules.is_file():
            findings.append(f"missing .gitmodules for submodule: {rel}")
            return findings
        if rel not in gitmodules.read_text(encoding="utf-8"):
            findings.append(f"missing submodule entry: {rel}")
            return findings
        if not (repo_root / rel / ".git").exists() and not (repo_root / ".git" / "modules" / rel.replace("/", "-")).exists():
            findings.append(f"submodule not initialized: {rel}")
        return findings

    if kind == "hook":
        hook_path = _git_hooks_dir(repo_root) / Path(rel).name
        if not hook_path.is_file():
            findings.append(f"missing hook: {rel}")
            return findings
        if template is not None and template.is_file():
            expected = template.read_bytes()
            actual = hook_path.read_bytes()
            if expected != actual:
                findings.append(f"drift: {rel}")
        return findings

    if optional and not full.exists():
        return findings

    if scaffold is not None and scaffold.is_file():
        findings.extend(_run_scaffold_check(scaffold, repo_root))
        if surf_id in ("root-agents-md", "guides-agents-md") and full.is_file():
            findings.extend(_agents_md.validate_agents_md(full, repo_root))
        return findings

    if not full.exists():
        findings.append(f"missing: {rel}")
        return findings

    if template is not None and template.is_file():
        findings.extend(_check_surface_content(repo_root, rel, template))
    return findings


def _apply_surface(repo_root: Path, surface: dict[str, object], exceptions: set[str], force: bool) -> bool:
    rel = str(surface["path"])
    surf_id = str(surface.get("id", ""))
    if surf_id in exceptions or rel in exceptions:
        return False
    kind = str(surface.get("kind", "file"))
    template = _template_path(surface)
    scaffold = _scaffold_script_path(surface)
    if kind in ("file", "hook") and template is not None:
        if kind == "hook":
            full = _git_hooks_dir(repo_root) / Path(rel).name
        else:
            full = repo_root / rel
        if full.is_file() and not force:
            print(f"skip {rel}: exists; use --force to overwrite")
            return False
        full.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(template, full)
        if kind == "hook":
            full.chmod(0o755)
        print(f"wrote {rel}")
        return True
    if scaffold is not None and scaffold.is_file():
        cmd = [sys.executable, str(scaffold)]
        if force:
            cmd.append("--force")
        result = subprocess.run(
            cmd,
            cwd=repo_root,
            capture_output=True,
            text=True,
            env=_stripped_env(),
        )
        if result.returncode != 0:
            print(f"error applying {rel}: {result.stderr or result.stdout}", file=sys.stderr)
            return False
        print(result.stdout.strip())
        return True
    return False


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check              report drift for every surface in the manifest
  %(prog)s --apply --yes        create missing surfaces without prompting
  %(prog)s --apply --yes --force  create missing surfaces and overwrite drifted ones

exit codes:
  0  all surfaces present (or applied successfully)
  1  drift detected, apply aborted, or an error occurred

The manifest is read from references/repository-shape-manifest.json inside the
repo-standards skill. Exceptions declared in .agents/docs/repo-guide-policy.md
under the ## Exceptions heading are skipped."""
    parser = argparse.ArgumentParser(
        description="Check or apply the repo-standards surface manifest.",
        epilog=epilog,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument("--check", action="store_true", help="report drift only; do not write")
    parser.add_argument("--apply", action="store_true", help="create missing surfaces")
    parser.add_argument("--yes", action="store_true", help="skip the interactive approval prompt before applying")
    parser.add_argument("--force", action="store_true", help="when applying, overwrite existing drifted surfaces (safe only for generated/template surfaces)")
    parser.add_argument("--allow-shared-checkout", action="store_true", help="allow writes in a shared/git-worktree checkout")
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    if _is_submodule(repo_root):
        print("error: repo-standards must not run inside a submodule", file=sys.stderr)
        return 1

    manifest = json.loads(_manifest_path().read_text(encoding="utf-8"))
    surfaces = manifest.get("surfaces", [])
    exceptions = _load_exceptions(repo_root)

    findings: list[str] = []
    for surface in surfaces:
        findings.extend(_check_surface(repo_root, surface, exceptions))

    # Deduplicate while preserving order
    seen = set()
    unique_findings: list[str] = []
    for f in findings:
        if f not in seen:
            seen.add(f)
            unique_findings.append(f)

    if args.check or not args.apply:
        if unique_findings:
            for f in unique_findings:
                print(f"DRIFT: {f}")
            return 1
        print("OK repo-standards: all surfaces present")
        return 0

    if args.allow_shared_checkout:
        print("warning: --allow-shared-checkout is an override and requires human approval before applying changes", file=sys.stderr)
    if not args.allow_shared_checkout and _is_shared_checkout(repo_root):
        print("error: shared checkout; use --allow-shared-checkout to override", file=sys.stderr)
        return 1

    if not args.yes:
        print(f"Will apply {len(unique_findings)} surfaces with drift: {unique_findings}")
        print("Add --yes to apply. Add --yes --force to overwrite existing drifted surfaces.")
        return 1

    applied = 0
    for surface in surfaces:
        if _check_surface(repo_root, surface, exceptions):
            if _apply_surface(repo_root, surface, exceptions, args.force):
                applied += 1
    print(f"OK repo-standards: applied {applied} surface(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
