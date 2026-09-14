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
_COMMAND_DECLARATION = Path(".agents/contracts/repo-standards-commands.json")


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


def _surface_is_explicitly_excepted(surface: dict[str, object], exceptions: set[str]) -> bool:
    rel = str(surface["path"])
    surf_id = str(surface.get("id", ""))
    return surf_id in exceptions or rel in exceptions


def _enabled_surface_ids(surfaces: list[dict[str, object]], exceptions: set[str]) -> set[str]:
    """Return manifest surface ids enabled after explicit exceptions and dependencies."""
    by_id = {str(surface.get("id", "")): surface for surface in surfaces if surface.get("id")}
    enabled = {
        surf_id for surf_id, surface in by_id.items() if not _surface_is_explicitly_excepted(surface, exceptions)
    }
    changed = True
    while changed:
        changed = False
        for surf_id in tuple(enabled):
            required_with = by_id[surf_id].get("required_with")
            if required_with and str(required_with) not in enabled:
                enabled.remove(surf_id)
                changed = True
    return enabled


def _required_with_findings(surfaces: list[dict[str, object]], exceptions: set[str]) -> list[str]:
    """Reject exception sets that leave a dependent surface without its prerequisite."""
    by_id = {str(surface.get("id", "")): surface for surface in surfaces if surface.get("id")}
    findings: list[str] = []
    for surface in surfaces:
        surf_id = str(surface.get("id", ""))
        required_with = str(surface.get("required_with", ""))
        if not surf_id or not required_with or required_with not in by_id:
            continue
        prerequisite_enabled = not _surface_is_explicitly_excepted(surface, exceptions)
        dependent_enabled = not _surface_is_explicitly_excepted(by_id[required_with], exceptions)
        if dependent_enabled and not prerequisite_enabled:
            findings.append(f"{required_with} requires {surf_id}; except {required_with} as well or restore {surf_id}")
    return findings


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


def _check_declared_commands(repo_root: Path) -> tuple[dict[str, list[str]] | None, list[str]]:
    path = repo_root / _COMMAND_DECLARATION
    if not path.is_file():
        return None, [f"missing consumer command declaration: {_COMMAND_DECLARATION.as_posix()}"]
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        return None, [f"consumer command declaration cannot be read: {exc}"]
    if not isinstance(data, dict):
        return None, ["consumer command declaration must be a JSON object"]
    findings: list[str] = []
    commands: dict[str, list[str]] = {}
    for capability, switch in (("apply", "--apply"), ("check", "--check")):
        command = data.get(capability)
        if not isinstance(command, list) or not command or not all(isinstance(item, str) for item in command):
            findings.append(f"consumer command declaration has invalid {capability} command")
            continue
        if switch not in command:
            findings.append(f"declared {capability} command is missing {switch}")
            continue
        commands[capability] = command
    return (commands if len(commands) == 2 else None), findings


def _check_hook_contract(hook_path: Path, repo_root: Path) -> list[str]:
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

    declaration, declaration_findings = _check_declared_commands(repo_root)
    findings.extend(declaration_findings)
    declaration_marker = _COMMAND_DECLARATION.as_posix()
    if declaration_marker not in text.replace("\\", "/"):
        findings.append("pre-commit hook must source the consumer command declaration")
    apply_marker = "run_declared apply"
    check_marker = "run_declared check"
    apply_index = text.find(apply_marker)
    check_index = text.find(check_marker)
    if apply_index < 0:
        findings.append("pre-commit hook must invoke the declared apply capability")
    if check_index < 0:
        findings.append("pre-commit hook must invoke the declared check capability")
    if apply_index >= 0 and check_index >= 0 and apply_index >= check_index:
        findings.append("pre-commit hook must invoke apply before check")
    if declaration is not None and apply_index >= 0 and check_index >= 0:
        if "required_switch" not in text:
            findings.append("pre-commit hook command runner must validate declared switches")
    if not _retains_canonical_hook_contract(text):
        findings.append("pre-commit hook must retain the canonical staged-snapshot contract command skeleton")
    return findings


def _retains_canonical_hook_contract(text: str) -> bool:
    """Require the canonical executable body; customize commands via its declaration."""
    template = Path(__file__).resolve().parent.parent / "templates" / "pre-commit"
    if not template.is_file():
        return False
    required = template.read_text(encoding="utf-8", errors="replace").splitlines()
    actual = text.splitlines()
    return actual == required


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


def _check_surface(
    repo_root: Path,
    surface: dict[str, object],
    exceptions: set[str],
    enabled_surface_ids: set[str] | None = None,
) -> list[str]:
    findings: list[str] = []
    rel = str(surface["path"])
    surf_id = str(surface.get("id", ""))
    if _surface_is_explicitly_excepted(surface, exceptions):
        return findings
    if enabled_surface_ids is not None and surf_id and surf_id not in enabled_surface_ids:
        return findings
    kind = str(surface.get("kind", "file"))
    optional = bool(surface.get("optional", False))
    template = _template_path(surface)
    scaffold = _scaffold_script_path(surface)
    full = repo_root / rel

    if kind == "command-declaration":
        _, declaration_findings = _check_declared_commands(repo_root)
        findings.extend(declaration_findings)
        return findings

    if kind == "directory":
        if not full.is_dir() and not optional:
            findings.append(f"missing directory: {rel}")
        return findings

    if kind == "absent":
        if full.exists():
            findings.append(f"retired path remains: {rel}")
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
        findings.extend(_check_hook_contract(hook_path, repo_root))
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


def _apply_surface(
    repo_root: Path,
    surface: dict[str, object],
    exceptions: set[str],
    force: bool,
    enabled_surface_ids: set[str] | None = None,
) -> bool:
    rel = str(surface["path"])
    surf_id = str(surface.get("id", ""))
    if _surface_is_explicitly_excepted(surface, exceptions):
        return False
    if enabled_surface_ids is not None and surf_id and surf_id not in enabled_surface_ids:
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
    enabled_surface_ids = _enabled_surface_ids(surfaces, exceptions)
    dependency_findings = _required_with_findings(surfaces, exceptions)

    findings: list[str] = list(dependency_findings)
    for surface in surfaces:
        findings.extend(_check_surface(repo_root, surface, exceptions, enabled_surface_ids))

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

    if dependency_findings:
        for finding in dependency_findings:
            print(f"DRIFT: {finding}")
        print("error: invalid repo-standards exception dependency", file=sys.stderr)
        return 1

    if not args.yes:
        print(f"Will apply {len(unique_findings)} surfaces with drift: {unique_findings}")
        print("Add --yes to apply. Add --yes --force to overwrite existing drifted surfaces.")
        return 1

    if not shared_checkout.approve_mutation(repo_root, _SCRIPT_NAME, args.allow_shared_checkout):
        return 1

    _, declaration_findings = _check_declared_commands(repo_root)
    if "repo-standards-commands" in enabled_surface_ids and declaration_findings:
        for finding in declaration_findings:
            print(f"DRIFT: {finding}")
        print(
            "error: supply a valid consumer command declaration before applying repo-standards",
            file=sys.stderr,
        )
        return 1

    applied = 0
    for surface in surfaces:
        if _check_surface(repo_root, surface, exceptions, enabled_surface_ids):
            if _apply_surface(repo_root, surface, exceptions, args.force, enabled_surface_ids):
                applied += 1

    print(f"OK repo-standards: applied {applied} surface(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
