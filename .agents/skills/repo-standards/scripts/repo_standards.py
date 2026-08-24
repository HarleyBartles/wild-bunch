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
        capture_output=True,
        text=True,
        check=True,
        env=_stripped_env(),
    )
    return Path(result.stdout.strip())


# Allow importing the shared checkout helper from the script directory (so the
# skill is self-contained when installed/bundled) or from tools/ when running
# from source.
_SCRIPT_DIR = Path(__file__).resolve().parent
_SHARED_CHECKOUT_PATH: Path | None = None
if (_SCRIPT_DIR / "shared_checkout.py").is_file():
    _SHARED_CHECKOUT_PATH = _SCRIPT_DIR
else:
    for _parent in _SCRIPT_DIR.parents:
        _candidate = _parent / "tools" / "shared_checkout.py"
        if _candidate.is_file():
            _SHARED_CHECKOUT_PATH = _parent / "tools"
            break
if _SHARED_CHECKOUT_PATH is None:
    raise RuntimeError("shared_checkout.py not found; repo layout mismatch")
sys.path.insert(0, str(_SHARED_CHECKOUT_PATH))
import shared_checkout  # noqa: E402


_SCRIPT_NAME = "repo-standards"


def _is_submodule(repo_root: Path) -> bool:
    result = subprocess.run(
        ["git", "rev-parse", "--show-superproject-working-tree"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        env=_stripped_env(),
    )
    return result.returncode == 0 and result.stdout.strip()


def _is_ci() -> bool:
    """Return True when running in a CI environment.

    CI runners set CI=true or GITHUB_ACTIONS=true. Pre-commit hooks are a
    local-only surface and are not validated in CI.
    """
    env = os.environ
    ci = env.get("CI", "").lower()
    return ci in ("1", "true", "yes") or env.get("GITHUB_ACTIONS") is not None


def _manifest_path() -> Path:
    return Path(__file__).resolve().parent.parent / "references" / "repository-shape-manifest.json"


def _load_exceptions(repo_root: Path) -> set[str]:
    exceptions: set[str] = set()
    policy = repo_root / ".agents" / "doctrine" / "repo-runbook-policy.md"
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


def _check_hook_contract(hook_path: Path) -> list[str]:
    """Validate a pre-commit hook by the repo-standards contract, not by byte comparison."""
    findings: list[str] = []
    if not hook_path.is_file():
        findings.append("pre-commit hook is not a regular file")
        return findings

    # On POSIX the executable bit is required for git to run the hook.
    # On Windows/NT, os.access(X_OK) is not reliable, so we only require a
    # shebang as a plausibility check.
    if os.name == "nt":
        try:
            if hook_path.read_bytes()[:2] != b"#!":
                findings.append("pre-commit hook has no shebang")
        except OSError as exc:
            findings.append(f"pre-commit hook cannot be read: {exc}")
    elif not os.access(hook_path, os.X_OK):
        findings.append("pre-commit hook is not executable")

    try:
        text = hook_path.read_text(encoding="utf-8", errors="replace")
    except OSError as exc:
        findings.append(f"pre-commit hook cannot be read: {exc}")
        return findings

    # Scan non-comment, non-empty lines for the required contract elements.
    non_comment = [line for line in text.splitlines() if line.strip() and not line.strip().startswith("#")]

    if not _has_shell_guard(non_comment):
        findings.append("pre-commit hook missing errexit/nounset/pipefail guard")

    non_comment_text = "\n".join(non_comment)
    # Accept either the canonical 'ci --apply' or the legacy 'all --apply' alias,
    # which is a safe backwards-compatibility bridge for older checkouts.
    targets = ("tools/run.py ci --apply", "tools/run.py all --apply")
    ci_apply = any(t in non_comment_text for t in targets)
    if not ci_apply:
        for prefix in ("py -3", "python3", "python"):
            if any(f"{prefix} {t}" in non_comment_text for t in targets):
                ci_apply = True
                break
    if not ci_apply:
        findings.append("pre-commit hook must run 'tools/run.py ci --apply' (or 'all --apply')")
    return findings


def _has_shell_guard(non_comment: list[str]) -> bool:
    """Return True if the non-comment lines set errexit, nounset, and pipefail."""
    short_to_name = {"e": "errexit", "u": "nounset"}
    # Options that can appear as '-o <name>' or as their long form directly.
    long_options = {"pipefail", "errexit", "nounset"}
    enabled: set[str] = set()
    for line in non_comment:
        stripped = line.strip()
        if not stripped.startswith("set "):
            continue
        tokens = stripped.removeprefix("set").split()
        i = 0
        while i < len(tokens):
            token = tokens[i]
            if not token.startswith("-") or token.startswith("--"):
                i += 1
                continue
            # token is a short-option cluster like -e, -eu, -euo, or the
            # standalone -o. If it contains an 'o', the next token is the
            # long option name for that -o.
            has_o = "o" in token[1:]
            for ch in token[1:]:
                if ch in short_to_name:
                    enabled.add(short_to_name[ch])
            if has_o and i + 1 < len(tokens) and tokens[i + 1] in long_options:
                enabled.add(tokens[i + 1])
                i += 1
            i += 1
    return {"errexit", "nounset", "pipefail"}.issubset(enabled)


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

    if kind == "directory":
        if not full.is_dir() and not optional:
            findings.append(f"missing directory: {rel}")
        return findings

    if kind == "submodule":
        gitmodules = repo_root / ".gitmodules"
        if not gitmodules.is_file():
            findings.append(f"missing .gitmodules for submodule: {rel}")
            return findings
        if rel not in gitmodules.read_text(encoding="utf-8"):
            findings.append(f"missing submodule entry: {rel}")
            return findings
        submodule_git = repo_root / rel / ".git"
        submodule_module_dir = repo_root / ".git" / "modules" / rel.replace("/", "-")
        if not submodule_git.exists() and not submodule_module_dir.exists():
            findings.append(f"submodule not initialized: {rel}")
        return findings

    if kind == "hook":
        # Pre-commit hooks are local-only; CI does not install or validate them.
        if _is_ci():
            return findings
        hook_path = _git_hooks_dir(repo_root) / Path(rel).name
        if not hook_path.is_file():
            findings.append(f"missing hook: {rel}")
            return findings
        # Validate the hook contract rather than requiring the exact template.
        findings.extend(_check_hook_contract(hook_path))
        return findings

    if optional and not full.exists():
        return findings

    if scaffold is not None and scaffold.is_file():
        findings.extend(_run_scaffold_check(scaffold, repo_root))
        if surf_id in ("root-agents-md", "runbooks-agents-md") and full.is_file():
            findings.extend(_agents_md.validate_agents_md(full, repo_root))
        return findings

    if not full.exists():
        findings.append(f"missing: {rel}")
        return findings

    if template is not None and template.is_file():
        if surface.get("check_content", True):
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
    if kind == "directory":
        full = repo_root / rel
        if full.is_dir():
            print(f"skip {rel}: directory exists")
            return False
        if full.exists() and not full.is_dir():
            print(f"error: cannot create directory {rel}: a non-directory file already exists", file=sys.stderr)
            return False
        full.mkdir(parents=True, exist_ok=True)
        gitkeep = full / ".gitkeep"
        gitkeep.write_text("# placeholder to keep this directory in git\n", encoding="utf-8")
        print(f"wrote {rel}")
        return True

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
  %(prog)s --check                                report drift for every surface in the manifest
  %(prog)s --apply --yes                          create missing surfaces without prompting
  %(prog)s --apply --yes --force                  create missing surfaces and overwrite drifted ones
  %(prog)s --apply --yes --allow-shared-checkout  create missing surfaces in a shared/git-worktree checkout

exit codes:
  0  all surfaces present (or applied successfully)
  1  drift detected, apply aborted, or an error occurred

The manifest is read from references/repository-shape-manifest.json inside the
repo-standards skill. Exceptions declared in .agents/doctrine/repo-runbook-policy.md
under the ## Exceptions heading are skipped."""
    parser = argparse.ArgumentParser(
        description="Check or apply the repo-standards surface manifest. (mixed)",
        epilog=epilog,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument("--check", action="store_true", help="report drift only; do not write")
    parser.add_argument("--apply", action="store_true", help="create missing surfaces")
    parser.add_argument(
        "--yes",
        action="store_true",
        help=(
            "confirm applying surfaces; shared-checkout approval is still "
            "required separately in shared/worktree checkouts"
        ),
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="when applying, overwrite existing drifted surfaces (safe only for generated/template surfaces)",
    )
    parser.add_argument(
        "--allow-shared-checkout",
        action="store_true",
        help=(
            "Approve applying changes in the main shared checkout on the main branch. "
            "Linked worktrees are always approved. Only pass this if you intend to mutate this checkout."
        ),
    )
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    if _is_submodule(repo_root):
        print("error: repo-standards must not run inside a submodule", file=sys.stderr)
        return 1

    if not args.check and not args.apply:
        args.check = True

    if args.allow_shared_checkout and not args.apply:
        print("error: --allow-shared-checkout requires --apply", file=sys.stderr)
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

    if not args.yes:
        print(f"Will apply {len(unique_findings)} surfaces with drift: {unique_findings}")
        print("Add --yes to apply. Add --yes --force to overwrite existing drifted surfaces.")
        return 1

    if not shared_checkout.approve_mutation(repo_root, _SCRIPT_NAME, args.allow_shared_checkout):
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
