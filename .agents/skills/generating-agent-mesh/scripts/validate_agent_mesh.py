#!/usr/bin/env python3
"""Validate the repo-wide agent mesh.

Checks that local markdown links in mesh surfaces resolve inside the repo, that
active doctrine files are reachable from an INDEX.md or AGENTS.md link in an
ancestor directory, and runs a repo-specific extra hook if present.
"""

from __future__ import annotations

import argparse
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path
from urllib.parse import unquote


LINK_PATTERN = re.compile(r"\[([^\]]+)\]\(([^)]+)\)")
FRONTMATTER_PATTERN = re.compile(r"^---\s*\n(.*?)\n---\s*\n", re.DOTALL)

EXCLUDED_DIR_NAMES = {
    ".git",
    ".worktrees",
    "__pycache__",
    ".pytest_cache",
    ".superpowers",
    "marketplace-source",
    "third_party",  # retained upstream snapshots are not repo-owned mesh
}

NON_SOURCE_SUFFIXES = {
    ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".zip", ".tar", ".gz",
    ".tgz", ".bz2", ".xz", ".7z", ".rar", ".exe", ".dll", ".so", ".dylib",
    ".pyc", ".pyo", ".pyd", ".pdf", ".docx", ".xlsx", ".pptx", ".otf", ".ttf",
    ".woff", ".woff2", ".eot", ".mp3", ".mp4", ".mov", ".avi", ".webm",
    ".ogg", ".wav", ".flac", ".DS_Store", ".db", ".sqlite", ".sqlite3",
    ".lockb",
}

MESH_LINK_FILE_NAMES = {"INDEX.md", "AGENTS.md", "SKILL.md"}


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


def _load_tracked(repo_root: Path) -> tuple[set[Path], set[Path]]:
    result = subprocess.run(
        ["git", "ls-files"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        check=True,
        env=_stripped_env(),
    )
    tracked_dirs: set[Path] = set()
    tracked_files: set[Path] = set()
    for line in result.stdout.splitlines():
        if not line.strip():
            continue
        path = repo_root / line
        tracked_files.add(path)
        tracked_dirs.add(path.parent)
        for parent in path.parents:
            if parent == repo_root:
                break
            tracked_dirs.add(parent)
    return tracked_dirs, tracked_files


def _changed_files(ref: str, repo_root: Path) -> list[Path]:
    for args in (
        ["git", "diff", "--name-only", "--diff-filter=ACMR", f"{ref}..."],
        ["git", "diff", "--name-only", ref],
    ):
        result = subprocess.run(
            args,
            cwd=repo_root,
            capture_output=True,
            text=True,
            env=_stripped_env(),
        )
        if result.returncode == 0:
            return [repo_root / line for line in result.stdout.splitlines() if line.strip()]
    raise RuntimeError(f"Could not determine changed files for ref {ref!r}")


def _is_under(path: Path, ancestor: Path) -> bool:
    return path == ancestor or ancestor in path.parents


def _should_examine(path: Path, repo_root: Path) -> bool:
    if not path.exists():
        return False
    try:
        rel = path.relative_to(repo_root)
    except ValueError:
        return False
    for part in rel.parts:
        if part in EXCLUDED_DIR_NAMES:
            return False
    if path.is_dir():
        return False
    if path.suffix.lower() in NON_SOURCE_SUFFIXES:
        return False
    return True


def _link_candidates(current_file: Path, raw_target: str, repo_root: Path) -> list[Path]:
    if raw_target.startswith(("http://", "https://", "mailto:")):
        return []
    clean = raw_target.split("#", 1)[0]
    if not clean:
        return []
    clean = unquote(clean).lstrip("/")
    candidates: list[Path] = []
    seen: set[Path] = set()
    for base in (current_file.parent, repo_root):
        try:
            resolved = (base / clean).resolve()
        except (OSError, ValueError):
            continue
        if resolved in seen:
            continue
        seen.add(resolved)
        if _is_under(resolved, repo_root):
            candidates.append(resolved)
    return candidates


def _is_mesh_link_file(path: Path, repo_root: Path) -> bool:
    if path.suffix.lower() not in {".md", ".markdown"}:
        return False
    if path.name in MESH_LINK_FILE_NAMES:
        return True
    try:
        rel = path.relative_to(repo_root)
    except ValueError:
        return False
    # Any markdown under a docs/ tree is part of the mesh we link-check.
    if len(rel.parts) > 1 and any(part == "docs" for part in rel.parts[:-1]):
        return True
    return False


def _collect_link_findings(repo_root: Path, files: list[Path]) -> list[str]:
    findings: list[str] = []
    for path in files:
        if not _should_examine(path, repo_root) or not _is_mesh_link_file(path, repo_root):
            continue
        rel = path.relative_to(repo_root)
        try:
            content = path.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        for _label, raw_target in LINK_PATTERN.findall(content):
            candidates = _link_candidates(path, raw_target, repo_root)
            if not candidates:
                continue
            if raw_target.endswith("/"):
                if not any(c.is_dir() for c in candidates):
                    findings.append(f"broken link: {rel.as_posix()} -> {raw_target}")
                continue
            if not any(c.exists() for c in candidates):
                findings.append(f"broken link: {rel.as_posix()} -> {raw_target}")
    return findings


def _is_active_doctrine(path: Path) -> bool:
    if path.suffix.lower() not in {".md", ".markdown"}:
        return False
    try:
        content = path.read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError):
        return False
    match = FRONTMATTER_PATTERN.match(content)
    if not match:
        return False
    return re.search(r"^\s*status:\s*active\s*$", match.group(1), re.MULTILINE | re.IGNORECASE) is not None


def _links_from(index_file: Path, repo_root: Path) -> set[Path]:
    try:
        content = index_file.read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError):
        return set()
    targets: set[Path] = set()
    for _label, raw_target in LINK_PATTERN.findall(content):
        candidates = _link_candidates(index_file, raw_target, repo_root)
        for resolved in candidates:
            if resolved.exists():
                targets.add(resolved)
                if resolved.is_dir():
                    targets.add(resolved)
                break
    return targets


def _collect_doctrine_route_findings(repo_root: Path, files: list[Path]) -> list[str]:
    doctrine_files = [p for p in files if _is_active_doctrine(p)]
    if not doctrine_files:
        return []
    findings: list[str] = []
    index_links: dict[Path, set[Path]] = {}
    for doctrine in doctrine_files:
        rel = doctrine.relative_to(repo_root)
        routed = False
        for ancestor in doctrine.parents:
            if not _is_under(ancestor, repo_root):
                break
            for idx_name in ("INDEX.md", "AGENTS.md"):
                idx = ancestor / idx_name
                if not idx.is_file():
                    continue
                if idx not in index_links:
                    index_links[idx] = _links_from(idx, repo_root)
                if doctrine in index_links[idx] or doctrine.parent in index_links[idx]:
                    routed = True
                    break
            if routed:
                break
        if not routed:
            findings.append(f"active doctrine not routed: {rel.as_posix()}")
    return findings


def _collect_retired_token_findings(
    repo_root: Path, files: list[Path], retired_tokens: tuple[str, ...] = ()
) -> list[str]:
    findings: list[str] = []
    for token in retired_tokens:
        for path in files:
            if not _should_examine(path, repo_root):
                continue
            try:
                content = path.read_text(encoding="utf-8")
            except (OSError, UnicodeDecodeError):
                continue
            if token in content:
                findings.append(f"retired token {token!r} in {path.relative_to(repo_root).as_posix()}")
    return findings


def _powershell_cmd() -> list[str]:
    for name in ("pwsh", "powershell"):
        if shutil.which(name):
            return [name, "-NoProfile", "-File"]
    return ["powershell", "-NoProfile", "-File"]


def _run_extra_hook(repo_root: Path, changed_from: str | None, check: bool) -> list[str]:
    findings: list[str] = []
    hook_sh = repo_root / "scripts" / "validate_agent_mesh_extra.sh"
    hook_ps1 = repo_root / "scripts" / "validate_agent_mesh_extra.ps1"

    # Prefer .ps1 on Windows and .sh elsewhere, but allow fallback.
    if sys.platform == "win32" and hook_ps1.is_file():
        cmd = _powershell_cmd() + [str(hook_ps1)]
        if check:
            cmd.append("-Check")
        if changed_from:
            cmd.extend(["-ChangedFrom", changed_from])
    elif hook_sh.is_file():
        cmd = ["bash", str(hook_sh)]
        if check:
            cmd.append("--check")
        if changed_from:
            cmd.extend(["--changed-from", changed_from])
    elif hook_ps1.is_file():
        cmd = _powershell_cmd() + [str(hook_ps1)]
        if check:
            cmd.append("-Check")
        if changed_from:
            cmd.extend(["-ChangedFrom", changed_from])
    else:
        return findings

    result = subprocess.run(
        cmd,
        cwd=repo_root,
        capture_output=True,
        text=True,
        env=_stripped_env(),
    )
    for line in result.stdout.splitlines():
        if line.startswith("DRIFT:") or line.startswith("drift:"):
            findings.append(line[6:].strip())
    if result.returncode != 0:
        for line in result.stdout.splitlines() + result.stderr.splitlines():
            line = line.strip()
            if line and not line.startswith("DRIFT:") and not line.startswith("drift:"):
                findings.append(f"validate_agent_mesh_extra hook: {line}")
    return findings


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Validate the repo-wide agent mesh")
    parser.add_argument(
        "--check",
        action="store_true",
        help="Report drift without writing (validation is always read-only)",
    )
    parser.add_argument(
        "--changed-from",
        type=str,
        default=None,
        help="Only examine files changed since this git ref",
    )
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    _tracked_dirs, tracked_files = _load_tracked(repo_root)

    if args.changed_from:
        files = _changed_files(args.changed_from, repo_root)
    else:
        files = list(tracked_files)

    files = [p for p in files if _should_examine(p, repo_root)]

    findings: list[str] = []
    findings.extend(_collect_link_findings(repo_root, files))
    findings.extend(_collect_doctrine_route_findings(repo_root, files))
    findings.extend(_collect_retired_token_findings(repo_root, files))
    findings.extend(_run_extra_hook(repo_root, args.changed_from, args.check))

    if findings:
        for finding in findings:
            print(f"DRIFT: {finding}", file=sys.stderr)
        return 1

    print("OK agent mesh")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
