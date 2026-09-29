#!/usr/bin/env python3
"""Check or apply the repo-standards shape."""

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import stat
import subprocess
import sys
from pathlib import Path
from typing import NamedTuple

import document_contracts
import plugin_contracts
import operating_standards_catalog
import operating_standards_dispatch
import skill_link_contract
import surface_contracts


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


def _markdown_snapshot(repo_root: Path) -> dict[Path, bytes]:
    return {
        path: path.read_bytes()
        for path in repo_root.rglob("*.md")
        if ".git" not in path.relative_to(repo_root).parts and path.is_file()
    }


def _check_markdown_outputs(repo_root: Path, before: dict[Path, bytes]) -> None:
    formatter = repo_root / ".agents/standards/markdown-formatting/markdown-formatting/scripts/format_markdown.py"
    if not formatter.is_file():
        formatter = Path(__file__).resolve().parents[2] / "markdown-formatting/scripts/format_markdown.py"
    contract = repo_root / ".agents/contracts/markdown-formatting.json"
    if not formatter.is_file() or not contract.is_file():
        return
    after = _markdown_snapshot(repo_root)
    changed = sorted(path for path, content in after.items() if before.get(path) != content)
    if not changed:
        return
    relative = [path.relative_to(repo_root).as_posix() for path in changed]
    subprocess.run([sys.executable, str(formatter), "--check-files", *relative], cwd=repo_root, check=True)


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


class CommandDeclaration(NamedTuple):
    apply: tuple[tuple[str, ...], ...]
    check: tuple[tuple[str, ...], ...]
    generated_paths: tuple[str, ...]


def _is_submodule(repo_root: Path) -> bool:
    result = subprocess.run(
        ["git", "rev-parse", "--show-superproject-working-tree"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        env=_stripped_env(),
    )
    return result.returncode == 0 and result.stdout.strip()


def _manifest_path() -> Path:
    return Path(__file__).resolve().parent.parent / "references" / "repository-shape-manifest.json"


def _coordinator_surface(surface: surface_contracts.SurfaceContract) -> dict[str, object]:
    """Bridge explicit version-3 contracts to the current coordinator lanes."""

    kind_by_validator = {
        "command-declaration": "command-declaration",
        "hook-contract": "hook",
        "must-be-absent": "absent",
        "submodule-contract": "submodule",
    }
    return {
        "id": surface.id,
        "path": surface.path,
        "kind": kind_by_validator.get(surface.validator, "file"),
        "source": surface.seed,
        "scaffold": surface.scaffold,
        "optional": surface.presence == "optional",
        "required_with": surface.required_with,
        "validator": surface.validator,
        "apply_mode": surface.apply,
        "force_reset": surface.force_reset,
    }


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


def _template_path(surface: dict[str, object], implementation_root: Path | None = None) -> Path | None:
    source = surface.get("source")
    if not source:
        return None
    root = implementation_root or Path(__file__).resolve().parent.parent
    return root / str(source)


def _scaffold_script_path(surface: dict[str, object], implementation_root: Path | None = None) -> Path | None:
    scaffold = surface.get("scaffold")
    if not scaffold:
        return None
    root = implementation_root / "scripts" if implementation_root else Path(__file__).resolve().parent
    return root / str(scaffold)


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


def _configured_hooks_path(repo_root: Path) -> str | None:
    result = subprocess.run(
        ["git", "config", "--get", "core.hooksPath"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        env=_stripped_env(),
    )
    if result.returncode != 0:
        return None
    return result.stdout.strip().replace("\\", "/").rstrip("/")


def _check_hooks_path(repo_root: Path, expected: str) -> list[str]:
    configured = _configured_hooks_path(repo_root)
    if configured != expected:
        actual = configured or "<unset>"
        return [f"core.hooksPath must be {expected}; found {actual}"]
    resolved = subprocess.run(
        ["git", "rev-parse", "--path-format=absolute", "--git-path", "hooks"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        check=True,
        env=_stripped_env(),
    )
    actual_path = Path(resolved.stdout.strip()).resolve()
    expected_path = (repo_root / expected).resolve()
    if actual_path != expected_path:
        return [f"core.hooksPath does not resolve to tracked hooks directory: {expected}"]
    return []


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


def _scaffold_environment(repo_root: Path, implementation_root: Path | None) -> dict[str, str]:
    env = _stripped_env()
    if implementation_root is not None:
        runtime = repo_root / ".agents" / "standards" / "_runtime"
        if runtime.is_dir():
            env["PYTHONPATH"] = os.pathsep.join(value for value in (str(runtime), env.get("PYTHONPATH", "")) if value)
    return env


def _run_scaffold_check(scaffold: Path, repo_root: Path, implementation_root: Path | None = None) -> list[str]:
    findings: list[str] = []
    result = subprocess.run(
        [sys.executable, str(scaffold), "--check"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        env=_scaffold_environment(repo_root, implementation_root),
    )
    output = result.stdout + result.stderr
    for line in output.splitlines():
        if line.startswith("DRIFT:"):
            findings.append(line[6:].strip())
    if result.returncode != 0 and not findings:
        findings.append(f"scaffold check failed: {scaffold.name}")
    return findings


def _check_declared_commands(repo_root: Path) -> tuple[CommandDeclaration | None, list[str]]:
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
    commands: dict[str, tuple[tuple[str, ...], ...]] = {}
    for capability, switch in (("apply", "--apply"), ("check", "--check")):
        raw = data.get(capability)
        if isinstance(raw, list) and raw and all(isinstance(item, str) for item in raw):
            vectors = [raw]
        elif (
            isinstance(raw, list)
            and raw
            and all(
                isinstance(vector, list) and len(vector) >= 2 and all(isinstance(item, str) and item for item in vector)
                for vector in raw
            )
        ):
            vectors = raw
        else:
            findings.append(f"consumer command declaration has invalid {capability} command")
            continue
        if any(switch not in vector for vector in vectors):
            findings.append(f"declared {capability} command is missing {switch}")
            continue
        commands[capability] = tuple(tuple(vector) for vector in vectors)
    generated_paths = data.get("generated_paths")
    valid_generated_paths: list[str] = []
    if not isinstance(generated_paths, list) or not generated_paths:
        findings.append("consumer command declaration has invalid generated_paths")
    else:
        for value in generated_paths:
            normalized = value.replace("\\", "/") if isinstance(value, str) else ""
            path = Path(normalized) if normalized else None
            if (
                not normalized
                or normalized in {"*", "**", ".", "./*", "./**"}
                or normalized.startswith("/")
                or normalized.startswith((":", "!"))
                or re.match(r"^[A-Za-z]:/", normalized)
                or "\x00" in normalized
                or path is None
                or ".." in path.parts
            ):
                findings.append(f"consumer command declaration has invalid generated_paths entry: {value!r}")
            else:
                valid_generated_paths.append(normalized)
    declaration = None
    if len(commands) == 2 and not findings:
        declaration = CommandDeclaration(
            apply=commands["apply"],
            check=commands["check"],
            generated_paths=tuple(valid_generated_paths),
        )
    return declaration, findings


def _check_hook_contract(
    hook_path: Path,
    repo_root: Path,
    platform_name: str | None = None,
    executable: bool | None = None,
) -> list[str]:
    """Validate a pre-commit hook by the repo-standards contract, not by byte comparison."""
    findings: list[str] = []
    if not hook_path.is_file():
        findings.append("pre-commit hook is not a regular file")
        return findings

    # On POSIX the executable bit is required for git to run the hook.
    # On Windows/NT, os.access(X_OK) is not reliable, so we only require a
    # shebang as a plausibility check.
    if (platform_name or os.name) == "nt":
        try:
            if hook_path.read_bytes()[:2] != b"#!":
                findings.append("pre-commit hook has no shebang")
        except OSError as exc:
            findings.append(f"pre-commit hook cannot be read: {exc}")
    elif not (
        executable
        if executable is not None
        else bool(hook_path.stat().st_mode & (stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH))
    ):
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
    """Require the hook's behavior contract while allowing repository-owned prose/customization."""
    required = (
        "set -euo pipefail",
        ".agents/contracts/repo-standards-commands.json",
        "run_declared apply",
        "git add -A",
        "run_declared check",
        "REPO_STANDARDS_HOSTED_COMMIT",
        "trap",
    )
    if any(marker not in text for marker in required):
        return False
    forbidden = ("exit 0", "set +e", "run_declared() { :; }", "if false; then")
    return not any(marker in text for marker in forbidden)


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


def _live_markdown_lines(text: str) -> list[str]:
    """Return lines outside fenced code blocks and HTML comments."""
    out: list[str] = []
    fence: str | None = None
    for line in text.splitlines():
        stripped = line.lstrip()
        if fence is None and (stripped.startswith("```") or stripped.startswith("~~~")):
            fence = stripped[:3]
            continue
        if fence is not None:
            if stripped.startswith(fence):
                fence = None
            continue
        out.append(line)
    prose = "\n".join(out)
    prose = re.sub(r"<!--.*?-->|<!--.*", "", prose, flags=re.DOTALL)
    return prose.splitlines()


_COMPOSITION_HEADINGS = (
    "When",
    "Composition",
    "Doctrine and contracts",
    "Local commands and paths",
    "Evidence contract",
    "Prohibited combinations",
)
_MARKDOWN_LINK = re.compile(r"\[[^\]]+\]\(([^)#]+)(?:#[^)]+)?\)")


def _composition_files(directory: Path) -> list[Path]:
    if not directory.is_dir():
        return []
    return [path for path in sorted(directory.glob("*.md")) if path.name != "AGENTS.md"]


def _section_links(path: Path, heading: str) -> list[Path]:
    lines = _live_markdown_lines(path.read_text(encoding="utf-8"))
    in_section = False
    links: list[Path] = []
    for line in lines:
        stripped = line.strip()
        if stripped.startswith("## "):
            in_section = stripped == f"## {heading}"
            continue
        if not in_section:
            continue
        for target in _MARKDOWN_LINK.findall(line):
            if "://" not in target:
                links.append((path.parent / target).resolve())
    return links


def _playbook_composition_links(path: Path, playbook_dir: Path) -> set[Path]:
    resolved_dir = playbook_dir.resolve()
    return {
        target
        for target in _section_links(path, "Composition")
        if target.parent == resolved_dir and target.suffix.lower() == ".md"
    }


def _find_composition_cycle(edges: dict[Path, set[Path]]) -> list[Path] | None:
    visited: set[Path] = set()
    active: set[Path] = set()
    stack: list[Path] = []

    def visit(node: Path) -> list[Path] | None:
        visited.add(node)
        active.add(node)
        stack.append(node)
        for target in sorted(edges.get(node, set())):
            if target not in edges:
                continue
            if target in active:
                start = stack.index(target)
                return [*stack[start:], target]
            if target not in visited:
                cycle = visit(target)
                if cycle:
                    return cycle
        stack.pop()
        active.remove(node)
        return None

    for node in sorted(edges):
        if node not in visited:
            cycle = visit(node)
            if cycle:
                return cycle
    return None


def _check_composition_graph(
    repo_root: Path, *, include_runbooks: bool = True, include_playbooks: bool = True
) -> list[str]:
    """Validate runbook roots, topical playbooks, and their explicit edges."""
    mapped_runbooks, mapped_playbooks = skill_link_contract.composition_paths(repo_root)
    all_runbooks = [path for path in mapped_runbooks if path.is_file() and path.name.lower() != "agents.md"]
    all_playbooks = [path for path in mapped_playbooks if path.is_file() and path.name.lower() != "agents.md"]
    runbooks = all_runbooks if include_runbooks else []
    playbooks = all_playbooks if include_playbooks else []
    if not runbooks and not playbooks:
        return []

    findings: list[str] = []
    required_by_kind = ((runbooks, "Playbook routing"), (playbooks, "Runbook routing"))
    for paths, kind_heading in required_by_kind:
        for path in paths:
            live = [line.strip() for line in _live_markdown_lines(path.read_text(encoding="utf-8"))]
            for heading in _COMPOSITION_HEADINGS + (kind_heading,):
                if f"## {heading}" not in live:
                    findings.append(
                        f"{path.relative_to(repo_root).as_posix()}: missing '## {heading}' composition section"
                    )
            new_contract = all(
                f"## {heading}" in live
                for heading in (
                    "Required capabilities",
                    "Optional capabilities",
                    "Required repository-owned skills",
                    "Optional repository-owned skills",
                )
            )
            if not new_contract and "## Required skills" not in live:
                findings.append(
                    f"{path.relative_to(repo_root).as_posix()}: missing required capability section "
                    "(legacy 'Required skills' is temporarily accepted)"
                )

    runbook_set = {path.resolve() for path in all_runbooks}
    playbook_set = {path.resolve() for path in all_playbooks}
    edges: dict[Path, set[Path]] = {path.resolve(): set(_section_links(path, "Playbook routing")) for path in runbooks}
    parents: dict[Path, set[Path]] = {
        path.resolve(): set(_section_links(path, "Runbook routing")) for path in playbooks
    }
    composition_edges = {path.resolve(): _playbook_composition_links(path, path.parent) for path in playbooks}

    for runbook, targets in edges.items():
        for target in targets:
            if target not in playbook_set:
                findings.append(
                    f"{runbook.relative_to(repo_root).as_posix()}: playbook target does not resolve: {target}"
                )
            elif include_playbooks and runbook not in parents.get(target, set()):
                findings.append(
                    f"{target.relative_to(repo_root).as_posix()}: missing reciprocal Runbook routing link to "
                    f"{runbook.relative_to(repo_root).as_posix()}"
                )
    for playbook, sources in parents.items():
        for source in sources:
            if source not in runbook_set:
                findings.append(
                    f"{playbook.relative_to(repo_root).as_posix()}: runbook target does not resolve: {source}"
                )
            elif include_runbooks and playbook not in edges.get(source, set()):
                findings.append(
                    f"{source.relative_to(repo_root).as_posix()}: missing reciprocal Playbook routing link to "
                    f"{playbook.relative_to(repo_root).as_posix()}"
                )
    for playbook, targets in composition_edges.items():
        for target in targets:
            if target not in playbook_set:
                findings.append(
                    f"{playbook.relative_to(repo_root).as_posix()}: playbook composition target does not resolve: "
                    f"{target.relative_to(repo_root).as_posix()}"
                )
    cycle = _find_composition_cycle(composition_edges)
    if cycle:
        rendered = " -> ".join(path.relative_to(repo_root).as_posix() for path in cycle)
        findings.append(f"playbook composition cycle: {rendered}")
    return findings


def _check_surface(
    repo_root: Path,
    surface: dict[str, object],
    exceptions: set[str],
    enabled_surface_ids: set[str] | None = None,
    implementation_root: Path | None = None,
) -> list[str]:
    findings: list[str] = []
    rel = str(surface["path"])
    surf_id = str(surface.get("id", ""))
    if _surface_is_explicitly_excepted(surface, exceptions):
        return findings
    opted_in = (repo_root / ".agents/contracts/operating-standards.json").is_file()
    if surf_id == "repo-plugin-subscriptions" and not opted_in:
        return findings
    if enabled_surface_ids is not None and surf_id and surf_id not in enabled_surface_ids:
        return findings
    if surf_id == "unslop-contract" and implementation_root is None:
        return findings
    kind = str(surface.get("kind", "file"))
    optional = bool(surface.get("optional", False))
    template = _template_path(surface, implementation_root)
    scaffold = _scaffold_script_path(surface, implementation_root)
    full = repo_root / rel

    composition_kind = {"runbook-set": "runbook", "playbook-set": "playbook"}.get(surf_id)
    if composition_kind and skill_link_contract.has_custom_composition_paths(repo_root):
        paths = skill_link_contract.composition_paths(repo_root)[0 if composition_kind == "runbook" else 1]
        required = skill_link_contract.required_composition_paths(repo_root, composition_kind)
        findings.extend(
            f"missing required {composition_kind}: {path.relative_to(repo_root).as_posix()}"
            for path in required
            if not path.is_file()
        )
        validator = (
            document_contracts.check_runbook if composition_kind == "runbook" else document_contracts.check_playbook
        )
        for path in paths:
            if path.is_file():
                findings.extend(item.message for item in validator(path, repo_root))
        return findings

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
        hook_path = repo_root / rel
        if not hook_path.is_file():
            findings.append(f"missing hook: {rel}")
        else:
            findings.extend(_check_hook_contract(hook_path, repo_root))
        findings.extend(_check_hooks_path(repo_root, str(Path(rel).parent).replace("\\", "/")))
        return findings

    if optional and not full.exists() and surf_id != "unslop-contract":
        return findings

    validator = document_contracts.DOCUMENT_VALIDATORS.get(str(surface.get("validator", "")))

    if scaffold is not None and scaffold.is_file():
        findings.extend(_run_scaffold_check(scaffold, repo_root, implementation_root))
        if validator is not None and surf_id not in {"review-entry", "contributing-entry", "repo-runbook-policy"}:
            findings.extend(item.message for item in validator(full, repo_root))
        return findings

    if not full.exists():
        findings.append(f"missing: {rel}")
        return findings

    if validator is not None:
        findings.extend(item.message for item in validator(full, repo_root))
        return findings

    if template is not None and template.is_file() and surf_id != "tools-shared-checkout":
        findings.extend(_check_surface_content(repo_root, rel, template))
    return findings


def _apply_surface(
    repo_root: Path,
    surface: dict[str, object],
    exceptions: set[str],
    force: bool,
    enabled_surface_ids: set[str] | None = None,
    implementation_root: Path | None = None,
) -> bool:
    rel = str(surface["path"])
    surf_id = str(surface.get("id", ""))
    if surf_id in {"runbook-set", "playbook-set"} and skill_link_contract.has_custom_composition_paths(repo_root):
        return False
    if _surface_is_explicitly_excepted(surface, exceptions):
        return False
    opted_in = (repo_root / ".agents/contracts/operating-standards.json").is_file()
    if surf_id == "repo-plugin-subscriptions" and not opted_in:
        return False
    if enabled_surface_ids is not None and surf_id and surf_id not in enabled_surface_ids:
        return False
    kind = str(surface.get("kind", "file"))
    template = _template_path(surface, implementation_root)
    scaffold = _scaffold_script_path(surface, implementation_root)
    if scaffold is not None and scaffold.is_file() and not force:
        result = subprocess.run(
            [sys.executable, str(scaffold)],
            cwd=repo_root,
            capture_output=True,
            text=True,
            env=_scaffold_environment(repo_root, implementation_root),
        )
        if result.returncode != 0:
            print(f"error applying {rel}: {result.stderr or result.stdout}", file=sys.stderr)
            return False
        print(result.stdout.strip())
        return True
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
        full = repo_root / rel
        if full.is_file() and not force:
            if kind == "hook":
                subprocess.run(
                    ["git", "config", "core.hooksPath", str(Path(rel).parent).replace("\\", "/")],
                    cwd=repo_root,
                    check=True,
                    env=_stripped_env(),
                )
            print(f"skip {rel}: exists; use targeted repo-standards force deployment with confirmation")
            return False
        full.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(template, full)
        if kind == "hook":
            full.chmod(0o755)
            subprocess.run(
                ["git", "config", "core.hooksPath", str(Path(rel).parent).replace("\\", "/")],
                cwd=repo_root,
                check=True,
                env=_stripped_env(),
            )
        print(f"wrote {rel}")
        return True
    return False


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check                                report drift for every surface in the manifest
  %(prog)s --apply --yes                          create missing surfaces without prompting
  %(prog)s --force <surface-id>                          targeted confirmed template deployment
  %(prog)s --apply --yes --allow-shared-checkout  create missing surfaces in the main shared checkout

exit codes:
  0  all surfaces present (or applied successfully)
  1  drift detected, apply aborted, or an error occurred

The manifest is read from references/repository-shape-manifest.json inside the
repo-standards skill. Consumer exceptions are declared as {id, reason} objects in
.agents/contracts/agent-operating-model.json. Legacy ## Exceptions entries in
.agents/doctrine/repo-runbook-policy.md are read only as a compatibility fallback
while the contract is absent."""
    parser = argparse.ArgumentParser(
        description="Check or apply the repo-standards surface manifest. (mixed)",
        epilog=epilog,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument("--check", action="store_true", help="report drift only; do not write")
    parser.add_argument("--apply", action="store_true", help="create missing surfaces")
    parser.add_argument("--standard", help="dispatch one declared standard and its dependencies")
    parser.add_argument("--run-standard", help=argparse.SUPPRESS)
    parser.add_argument(
        "--yes",
        action="store_true",
        help=("confirm applying surfaces; writing in the main shared checkout also requires --allow-shared-checkout"),
    )
    parser.add_argument(
        "--force",
        action="append",
        nargs="?",
        const="",
        help="force-deploy a named surface template; repeat for multiple surfaces",
    )
    parser.add_argument(
        "--confirm-local-customisations-will-be-overwritten",
        action="store_true",
        help="acknowledge that targeted force deployment overwrites local customizations",
    )
    parser.add_argument(
        "--allow-shared-checkout",
        action="store_true",
        help=(
            "Acknowledge intentional changes in the main shared checkout on any branch. "
            "Linked worktrees do not need this flag."
        ),
    )
    args = parser.parse_args(argv)

    force_targets = set(args.force or [])
    if args.force is not None:
        if "" in force_targets:
            print("error: --force requires a surface id; bare --force is invalid", file=sys.stderr)
            return 1
        if args.apply or args.check:
            print("error: --force is a standalone targeted deployment mode", file=sys.stderr)
            return 1
        args.apply = True
        args.yes = True

    repo_root = _repo_root()
    if _is_submodule(repo_root):
        print("error: repo-standards must not run inside a submodule", file=sys.stderr)
        return 1

    if not args.check and not args.apply:
        args.check = True

    if args.allow_shared_checkout and not args.apply:
        print("error: --allow-shared-checkout requires --apply", file=sys.stderr)
        return 1

    if args.run_standard:
        composition_path = repo_root / ".agents/contracts/operating-standards.json"
        if not composition_path.is_file():
            print("error: --run-standard requires operating-standards.json", file=sys.stderr)
            return 1
        try:
            catalog = operating_standards_dispatch.load_catalog(Path(__file__))
            composition = json.loads(composition_path.read_text(encoding="utf-8"))
            operating_standards_catalog.validate_composition(composition, catalog)
            entries = {entry["id"]: entry for entry in composition["standards"]}
            if args.run_standard not in entries:
                raise ValueError(f"standard is not declared in this repository: {args.run_standard}")
            standard = next(item for item in catalog.standards if item.id == args.run_standard)
            implementation_root = (repo_root / entries[args.run_standard]["implementation_root"]).resolve()
            try:
                implementation_root.relative_to(repo_root.resolve())
            except ValueError as exc:
                raise ValueError("selected standard implementation root escapes repository") from exc
            if not implementation_root.is_dir():
                raise ValueError(f"selected standard implementation root is missing: {implementation_root}")
            missing_resources = [
                resource
                for resource in standard.resources
                if not operating_standards_dispatch.resource_destination(implementation_root, resource).is_file()
            ]
            if missing_resources:
                raise ValueError(f"selected standard resources are unavailable: {', '.join(missing_resources)}")
            manifest = surface_contracts.load_manifest(_manifest_path())
            by_id = {surface.id: _coordinator_surface(surface) for surface in manifest.surfaces}
            selected_surfaces = [by_id[surface_id] for surface_id in standard.surfaces]
            selected_ids = set(standard.surfaces)
            composition_findings = _check_composition_graph(
                repo_root,
                include_runbooks=bool({"runbook-set", "runbooks-agents-md"} & selected_ids),
                include_playbooks=bool({"playbook-set", "playbooks-agents-md"} & selected_ids),
            )
            if args.apply and not args.yes:
                print("error: --apply requires --yes", file=sys.stderr)
                return 1
            if args.apply and not shared_checkout.approve_mutation(repo_root, _SCRIPT_NAME, args.allow_shared_checkout):
                return 1
            if args.check:
                findings = [
                    *composition_findings,
                    *[
                        finding
                        for surface in selected_surfaces
                        for finding in _check_surface(repo_root, surface, set(), selected_ids, implementation_root)
                    ],
                ]
                for finding in findings:
                    print(f"DRIFT: [{args.run_standard}] {finding}")
                return 1 if findings else 0
            applied = 0
            for surface in selected_surfaces:
                if _check_surface(repo_root, surface, set(), selected_ids, implementation_root):
                    applied += int(_apply_surface(repo_root, surface, set(), False, selected_ids, implementation_root))
            counterpart_id = (
                "playbook-composition" if args.run_standard == "runbook-composition" else "runbook-composition"
            )
            counterpart_selected = counterpart_id in entries
            unresolved = (
                []
                if counterpart_selected
                else _check_composition_graph(
                    repo_root,
                    include_runbooks=bool({"runbook-set", "runbooks-agents-md"} & selected_ids),
                    include_playbooks=bool({"playbook-set", "playbooks-agents-md"} & selected_ids),
                )
            )
            for surface in selected_surfaces:
                unresolved.extend(_check_surface(repo_root, surface, set(), selected_ids, implementation_root))
            if unresolved:
                for finding in dict.fromkeys(unresolved):
                    print(f"DRIFT: [{args.run_standard}] {finding}")
                print("error: repo-standards apply did not converge", file=sys.stderr)
                return 1
            print(f"OK repo-standards: applied standard {args.run_standard} ({applied} surface(s))")
            return 0
        except (OSError, ValueError, json.JSONDecodeError, KeyError, StopIteration) as exc:
            print(f"DRIFT: [operating-standards] {exc}", file=sys.stderr)
            return 1

    composition_path = repo_root / ".agents/contracts/operating-standards.json"
    if composition_path.is_file():
        if force_targets:
            print("error: --force is not supported by composition dispatch", file=sys.stderr)
            return 1
        if args.apply and not args.yes:
            print("error: --apply requires --yes", file=sys.stderr)
            return 1
        if args.apply and not shared_checkout.approve_mutation(repo_root, _SCRIPT_NAME, args.allow_shared_checkout):
            return 1
        try:
            catalog = operating_standards_dispatch.load_catalog(Path(__file__))
            completed = operating_standards_dispatch.dispatch(
                repo_root,
                catalog,
                composition_path,
                mode="apply" if args.apply else "check",
                standard_id=args.standard,
                allow_shared_checkout=args.allow_shared_checkout,
            )
            selected_surface_ids = {
                surface_id
                for standard in catalog.standards
                if standard.id in completed
                for surface_id in standard.surfaces
            }
            composition_findings = _check_composition_graph(
                repo_root,
                include_runbooks=bool({"runbook-set", "runbooks-agents-md"} & selected_surface_ids),
                include_playbooks=bool({"playbook-set", "playbooks-agents-md"} & selected_surface_ids),
            )
        except (OSError, ValueError) as exc:
            print(f"DRIFT: [operating-standards] {exc}", file=sys.stderr)
            return 1
        if composition_findings:
            for finding in composition_findings:
                print(f"DRIFT: {finding}")
            return 1
        verb = "applied" if args.apply else "checked"
        print(f"OK repo-standards: {verb} {len(completed)} declared standard(s)")
        return 0

    if args.standard:
        print("error: --standard requires .agents/contracts/operating-standards.json", file=sys.stderr)
        return 1

    manifest = surface_contracts.load_manifest(_manifest_path())
    surfaces = [_coordinator_surface(surface) for surface in manifest.surfaces]
    known_surface_ids = {str(surface.get("id", "")) for surface in surfaces}
    unknown_force_targets = force_targets - known_surface_ids
    if unknown_force_targets:
        print(f"error: unknown force surface id(s): {', '.join(sorted(unknown_force_targets))}", file=sys.stderr)
        return 1
    if force_targets:
        force_preflight: list[str] = []
        by_id = {str(surface.get("id", "")): surface for surface in surfaces}
        for target in sorted(force_targets):
            surface = by_id[target]
            if surface.get("force_reset") != "confirmed-template-restore":
                force_preflight.append(f"{target}: force reset is unavailable")
                continue
            template = _template_path(surface)
            if template is None or not template.is_file():
                force_preflight.append(f"{target}: force reset seed is unavailable")
        if force_preflight:
            for finding in force_preflight:
                print(f"error: {finding}", file=sys.stderr)
            return 1
        targets = ", ".join(sorted(force_targets))
        warning = f"WARNING: are you sure? This will force overwrite repo-local customisations for: {targets}"
        print(warning, file=sys.stderr)
        if not args.confirm_local_customisations_will_be_overwritten:
            if not sys.stdin.isatty():
                print(
                    "error: targeted --force requires --confirm-local-customisations-will-be-overwritten",
                    file=sys.stderr,
                )
                return 1
            if input("Type 'yes' to continue: ").strip().lower() != "yes":
                print("error: force deployment cancelled", file=sys.stderr)
                return 1
    plugin_findings: list[surface_contracts.Finding] = []
    consumer_contract_path = repo_root / ".agents/contracts/agent-operating-model.json"
    if consumer_contract_path.is_file():
        try:
            consumer_contract = plugin_contracts.load_consumer_contract(repo_root)
        except (OSError, json.JSONDecodeError, KeyError, TypeError) as exc:
            plugin_findings.append(
                surface_contracts.Finding(
                    "failure",
                    "invalid-operating-model-contract",
                    "operating-model-contract",
                    f"consumer operating-model contract cannot be loaded: {exc}",
                    "run scaffold-operating-model-contract and repair the reported contract fields",
                )
            )
            exceptions = _load_exceptions(repo_root)
        else:
            exceptions = set(consumer_contract.surface_exceptions)
    else:
        exceptions = _load_exceptions(repo_root)
    enabled_surface_ids = _enabled_surface_ids(surfaces, exceptions)
    dependency_findings = _required_with_findings(surfaces, exceptions)

    graph_findings = _check_composition_graph(repo_root)
    findings: list[str] = [*dependency_findings, *graph_findings]
    for surface in surfaces:
        findings.extend(_check_surface(repo_root, surface, exceptions, enabled_surface_ids))
    if "marketplace-json" not in exceptions:
        findings.extend(item.message for item in skill_link_contract.check_skill_links(repo_root))

    structured_findings = [
        surface_contracts.Finding(
            severity="failure",
            code="surface-contract",
            surface="repository",
            message=message,
            repair="run the owning scaffold or repair the named consumer contract",
        )
        for message in findings
    ]
    # Deduplicate structured findings while preserving diagnostic order.
    unique_findings = list(dict.fromkeys([*structured_findings, *plugin_findings]))
    warnings = [finding for finding in unique_findings if finding.severity == "warning"]
    failures = [finding for finding in unique_findings if finding.severity == "failure"]
    for finding in warnings:
        print(f"WARN: [{finding.code}] {finding.message}; {finding.repair}")

    if args.check or not args.apply:
        if failures:
            for finding in failures:
                print(f"DRIFT: [{finding.code}] {finding.message}; {finding.repair}")
            return 1
        print("OK repo-standards: all surfaces present")
        return 0

    plugin_failures = [finding for finding in plugin_findings if finding.severity == "failure"]
    if dependency_findings or plugin_failures:
        for finding in dependency_findings:
            print(f"DRIFT: {finding}")
        for finding in plugin_failures:
            print(f"DRIFT: [{finding.code}] {finding.message}; {finding.repair}")
        print("error: invalid repo-standards exception dependency", file=sys.stderr)
        return 1

    if not args.yes:
        print(f"Will apply {len(failures)} surfaces with drift: {[finding.message for finding in failures]}")
        print("Add --yes to apply. Use a confirmed named force target for template deployment.")
        return 1

    if not shared_checkout.approve_mutation(repo_root, _SCRIPT_NAME, args.allow_shared_checkout):
        return 1

    markdown_before = _markdown_snapshot(repo_root)

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
        if force_targets and str(surface.get("id", "")) not in force_targets:
            continue
        if force_targets or _check_surface(repo_root, surface, exceptions, enabled_surface_ids):
            if _apply_surface(repo_root, surface, exceptions, bool(force_targets), enabled_surface_ids):
                applied += 1

    # Contract-level convergence: reload consumer configuration and rerun the
    # complete check after every mutation lane.
    if consumer_contract_path.is_file():
        refreshed_contract = plugin_contracts.load_consumer_contract(repo_root)
        refreshed_exceptions = set(refreshed_contract.surface_exceptions)
    else:
        refreshed_contract = plugin_contracts.ConsumerContract(tuple(exceptions))
        refreshed_exceptions = set(exceptions)
    refreshed_enabled = _enabled_surface_ids(surfaces, refreshed_exceptions)
    refreshed_graph = _check_composition_graph(repo_root)
    unresolved = [*_required_with_findings(surfaces, refreshed_exceptions), *refreshed_graph]
    for surface in surfaces:
        unresolved.extend(_check_surface(repo_root, surface, refreshed_exceptions, refreshed_enabled))
    if "operating-model-contract" in known_surface_ids and "marketplace-json" not in refreshed_exceptions:
        unresolved.extend(item.message for item in skill_link_contract.check_skill_links(repo_root))
        unresolved.extend(
            finding.message
            for finding in plugin_contracts.check_plugin_contract(repo_root, refreshed_contract)
            if finding.severity == "failure"
        )
    if unresolved:
        for finding in dict.fromkeys(unresolved):
            print(f"DRIFT: {finding}")
        print("error: repo-standards apply did not converge", file=sys.stderr)
        return 1

    _check_markdown_outputs(repo_root, markdown_before)
    print(f"OK repo-standards: applied {applied} surface(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
