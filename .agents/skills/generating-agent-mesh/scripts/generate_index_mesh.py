import argparse
import os
import re
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from urllib.parse import quote, unquote


def _stripped_env() -> dict[str, str]:
    env = os.environ.copy()
    env.pop("GIT_DIR", None)
    env.pop("GIT_WORK_TREE", None)
    env.pop("GIT_INDEX_FILE", None)
    return env


def _repo_root() -> Path:
    """Return the repo root from git, or the parent of the tools dir as a fallback."""
    result = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True,
        text=True,
        check=True,
        env=_stripped_env(),
    )
    return Path(result.stdout.strip())


# Import the shared checkout helper from the repo's tools/ directory. The only
# bundled copy lives inside the repo-standards skill; other skills rely on
# repo-standards having deployed tools/shared_checkout.py.
_SCRIPT_DIR = Path(__file__).resolve().parent
_SHARED_CHECKOUT_PATH: Path | None = None
for _parent in _SCRIPT_DIR.parents:
    _candidate = _parent / "tools" / "shared_checkout.py"
    if _candidate.is_file():
        _SHARED_CHECKOUT_PATH = _parent / "tools"
        break
if _SHARED_CHECKOUT_PATH is None:
    raise RuntimeError("tools/shared_checkout.py not found; run repo-standards --apply")
sys.path.insert(0, str(_SHARED_CHECKOUT_PATH))
import shared_checkout  # noqa: E402


_SCRIPT_NAME = "generate-index-mesh"


def _powershell_cmd() -> list[str]:
    for name in ("pwsh", "powershell"):
        if shutil.which(name):
            return [name, "-NoProfile", "-File"]
    return ["powershell", "-NoProfile", "-File"]


def _run_index_mesh_extra_hook(repo_root: Path, check: bool) -> list[str]:
    """Run the repo-supplied INDEX.md extra hook if one exists.

    The hook receives the repo root and, in check mode, --check:
        scripts/generate_index_mesh_extra.sh [--check] <repo-root>
    It may post-process or append content to specific INDEX.md files.
    """
    hook_sh = repo_root / "scripts" / "generate_index_mesh_extra.sh"
    hook_ps1 = repo_root / "scripts" / "generate_index_mesh_extra.ps1"

    # Prefer .ps1 on Windows and .sh elsewhere, but allow fallback.
    if sys.platform == "win32" and hook_ps1.is_file():
        cmd = _powershell_cmd() + [str(hook_ps1)]
        if check:
            cmd.append("-Check")
    elif hook_sh.is_file():
        cmd = ["bash", str(hook_sh)]
        if check:
            cmd.append("--check")
    elif hook_ps1.is_file():
        cmd = _powershell_cmd() + [str(hook_ps1)]
        if check:
            cmd.append("-Check")
    else:
        return []

    cmd.append(str(repo_root))

    result = subprocess.run(
        cmd,
        cwd=repo_root,
        capture_output=True,
        text=True,
        env=_stripped_env(),
    )
    errors: list[str] = []
    for line in (result.stdout + result.stderr).splitlines():
        line = line.strip()
        if line:
            errors.append(line)
    if result.returncode != 0:
        if not errors:
            errors.append("repo-specific INDEX.md hook failed")
    return errors


# Set at import from git. Use configure_root() or --repo-root to override before any work runs.
ROOT = _repo_root()
EXCLUDED_DIR_NAMES = {".git", ".worktrees", "__pycache__", ".pytest_cache", ".superpowers"}
EXCLUDED_ROOT_NAMES = {".git", ".worktrees", "__pycache__", ".superpowers"}
EXCLUDED_FILE_NAMES = {".git", ".gitkeep"}
INDEX_FILE_NAMES = {"INDEX.md", "INDEX.json"}
THIRD_PARTY_ROOT = ROOT / "sources" / "third_party"
SKILL_ZIPS_ROOT = ROOT / "generated" / "skill-zips"
NON_CANONICAL_GUARD_ROOTS = {ROOT / ".agents" / "docs" / "superpowers"}


def _load_tracked() -> tuple[set[Path], set[Path], set[Path]]:
    """Return (tracked_dirs, tracked_files, content_dirs) from git ls-files."""
    result = subprocess.run(
        ["git", "ls-files"],
        cwd=ROOT,
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
        path = ROOT / line
        tracked_files.add(path)
        tracked_dirs.add(path.parent)
        for parent in path.parents:
            if parent == ROOT:
                break
            tracked_dirs.add(parent)
    # The repo root itself is an implicit target (root INDEX.md) even though
    # no tracked file lives directly at the root.
    tracked_dirs.add(ROOT)
    content_dirs: set[Path] = {ROOT}
    for f in tracked_files:
        if f.name in INDEX_FILE_NAMES:
            continue
        for parent in f.parents:
            content_dirs.add(parent)
            if parent == ROOT:
                break
    return tracked_dirs, tracked_files, content_dirs


def _load_ignored_index_paths(tracked_dirs: set[Path]) -> set[str]:
    """Return the set of relative INDEX.md paths that git would ignore.

    Batch the check via `git check-ignore --no-index --stdin` so the number of
    git subprocesses does not scale with the number of directories.
    """
    candidates = sorted((d / "INDEX.md").relative_to(ROOT).as_posix() for d in tracked_dirs)
    ignored: set[str] = set()
    if not candidates:
        return ignored
    input_bytes = b"\x00".join(c.encode("utf-8") for c in candidates) + b"\x00"
    result = subprocess.run(
        ["git", "check-ignore", "--no-index", "-z", "--stdin"],
        input=input_bytes,
        cwd=ROOT,
        capture_output=True,
        env=_stripped_env(),
    )
    if result.returncode not in (0, 1):
        raise subprocess.CalledProcessError(result.returncode, result.args, output=result.stdout, stderr=result.stderr)
    if result.stdout:
        for raw_path in result.stdout.rstrip(b"\x00").split(b"\x00"):
            decoded = raw_path.decode("utf-8")
            if decoded:
                ignored.add(decoded)
    return ignored


TRACKED_DIRS, TRACKED_FILES, CONTENT_DIRS = _load_tracked()
IGNORED_INDEX_PATHS = _load_ignored_index_paths(TRACKED_DIRS)


@dataclass(frozen=True)
class IndexTarget:
    path: Path
    lines: list[str]


LINK_PATTERN = re.compile(r"\[([^\]]+)\]\(([^)]+)\)")


def is_skill_root(path: Path) -> bool:
    return (path / "SKILL.md").exists() or (path / "overlay.yaml").exists()


def is_under(path: Path, ancestor: Path) -> bool:
    return path == ancestor or ancestor in path.parents


def is_non_canonical_guard(path: Path) -> bool:
    return any(is_under(path, root) for root in NON_CANONICAL_GUARD_ROOTS)


def is_index_ignored(path: Path) -> bool:
    """Return True if an INDEX.md inside this directory would be ignored by git."""
    rel = (path / "INDEX.md").relative_to(ROOT).as_posix()
    return rel in IGNORED_INDEX_PATHS


def should_descend(child: Path) -> bool:
    return (
        child.name not in EXCLUDED_DIR_NAMES
        and not is_skill_root(child)
        and (child == THIRD_PARTY_ROOT or not is_under(child, THIRD_PARTY_ROOT))
        and not is_under(child, SKILL_ZIPS_ROOT)
        and not is_non_canonical_guard(child)
        and not is_index_ignored(child)
        and child in CONTENT_DIRS
    )


def should_index(path: Path) -> bool:
    if path == ROOT:
        return True
    if is_index_ignored(path):
        return False
    if is_non_canonical_guard(path):
        return False
    relative = path.relative_to(ROOT)
    if any(part in EXCLUDED_ROOT_NAMES for part in relative.parts):
        return False
    if is_under(path, THIRD_PARTY_ROOT) and path != THIRD_PARTY_ROOT:
        return False
    if is_under(path, SKILL_ZIPS_ROOT):
        return False
    if path not in CONTENT_DIRS:
        return False
    return not is_skill_root(path)


def rel_link(current: Path, target: Path, label: str | None = None) -> str:
    rel = quote(target.relative_to(current).as_posix(), safe="/#")
    return f"[{label or target.name}]({rel})"


def dir_link(current: Path, child: Path) -> str | None:
    skill_md = child / "SKILL.md"
    if skill_md.exists():
        target = quote(skill_md.relative_to(current).as_posix(), safe="/#")
        return f"[{child.name}]({target})"
    if should_index(child):
        target = quote(child.relative_to(current).as_posix() + "/INDEX.md", safe="/#")
        return f"[{child.name}]({target})"
    target = quote(child.relative_to(current).as_posix() + "/", safe="/#")
    return f"[{child.name}]({target})"


def render_index(path: Path) -> str:
    dirs = []
    files = []
    for entry in sorted(path.iterdir(), key=lambda p: (not p.is_dir(), p.name.casefold(), p.name)):
        if entry.name in INDEX_FILE_NAMES:
            continue
        if entry.is_dir():
            if entry.name in EXCLUDED_DIR_NAMES:
                continue
            if is_non_canonical_guard(entry):
                continue
            if is_index_ignored(entry):
                continue
            if is_skill_root(path):
                continue
            if entry not in CONTENT_DIRS and not is_skill_root(entry):
                continue
            dirs.append(entry)
        else:
            if entry.name in EXCLUDED_FILE_NAMES:
                continue
            if entry not in TRACKED_FILES:
                continue
            files.append(entry)

    lines: list[str] = ["# INDEX.md", ""]
    lines.append(
        "This index is generated by the `generating-agent-mesh` skill "
        "(`.agents/skills/generating-agent-mesh/scripts/generate-index-mesh`)."
    )
    lines.append("")

    if dirs:
        lines.append("## Directories")
        for child in dirs:
            link = dir_link(path, child)
            if link is not None:
                lines.append(f"- {link}")
        lines.append("")

    if files:
        lines.append("## Files")
        for child in files:
            lines.append(f"- {rel_link(path, child)}")
        lines.append("")

    if not dirs and not files:
        lines.append("No child entries.")
        lines.append("")

    return "\n".join(lines).rstrip() + "\n"


def resolve_link_target(current: Path, target: str) -> Path | None:
    if target.startswith(("http://", "https://", "mailto:")):
        return None
    clean_target = target.split("#", 1)[0]
    if not clean_target:
        return None
    clean_target = unquote(clean_target)
    candidates = [
        (current.parent / clean_target).resolve(),
        (ROOT / clean_target).resolve(),
    ]
    for resolved in candidates:
        if not is_under(resolved, ROOT):
            continue
        if target.endswith("/"):
            if resolved.is_dir():
                return resolved
            continue
        if resolved.exists():
            return resolved
    return None


def validate_rendered_links(path: Path, rendered: str) -> list[str]:
    failures: list[str] = []
    for label, raw_target in LINK_PATTERN.findall(rendered):
        resolved = resolve_link_target(path, raw_target)
        if resolved is None:
            continue
        if raw_target.endswith("/"):
            if not resolved.is_dir():
                failures.append(f"broken-link: {path.relative_to(ROOT)} -> {raw_target}")
            continue
        if not resolved.exists():
            failures.append(f"broken-link: {path.relative_to(ROOT)} -> {raw_target}")
    return failures


def walk_index_targets() -> list[IndexTarget]:
    targets: list[IndexTarget] = []
    for dirpath, dirnames, _filenames in os.walk(ROOT):
        current = Path(dirpath)
        dirnames[:] = sorted(
            (name for name in dirnames if should_descend(current / name)),
            key=lambda name: (name.casefold(), name),
        )
        if should_index(current):
            targets.append(IndexTarget(path=current / "INDEX.md", lines=render_index(current).splitlines()))
    return targets


def configure_root(repo_root: Path) -> None:
    global ROOT, THIRD_PARTY_ROOT, SKILL_ZIPS_ROOT, NON_CANONICAL_GUARD_ROOTS, TRACKED_DIRS, TRACKED_FILES, CONTENT_DIRS, IGNORED_INDEX_PATHS  # noqa: E501
    ROOT = repo_root
    THIRD_PARTY_ROOT = ROOT / "sources" / "third_party"
    SKILL_ZIPS_ROOT = ROOT / "generated" / "skill-zips"
    NON_CANONICAL_GUARD_ROOTS = {ROOT / ".agents" / "docs" / "superpowers"}
    TRACKED_DIRS, TRACKED_FILES, CONTENT_DIRS = _load_tracked()
    IGNORED_INDEX_PATHS = _load_ignored_index_paths(TRACKED_DIRS)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Generate or validate the repo-wide INDEX.md mesh. (mixed)")
    parser.add_argument("--check", action="store_true", help="validate without writing")
    parser.add_argument("--apply", action="store_true", help="generate INDEX.md files")
    parser.add_argument(
        "--allow-shared-checkout",
        action="store_true",
        help="Approve generating INDEX.md files in a shared or git-worktree checkout. "
        "Only pass this if you intend to mutate this checkout.",
    )
    parser.add_argument("--repo-root", type=Path, default=None, help="repo root to process")
    args = parser.parse_args(argv)

    if args.repo_root:
        configure_root(args.repo_root.resolve())

    # Re-validate we are not in a submodule
    result = subprocess.run(
        ["git", "rev-parse", "--show-superproject-working-tree"],
        cwd=ROOT,
        capture_output=True,
        text=True,
        env=_stripped_env(),
    )
    if result.returncode == 0 and result.stdout.strip():
        raise RuntimeError("This script must not run inside a git submodule")

    if not args.check and not args.apply:
        args.check = True

    if args.allow_shared_checkout and not args.apply:
        print("error: --allow-shared-checkout requires --apply", file=sys.stderr)
        return 1

    if not args.check and not shared_checkout.approve_mutation(ROOT, _SCRIPT_NAME, args.allow_shared_checkout):
        return 1

    targets = walk_index_targets()
    expected_paths = {target.path for target in targets}
    actual_paths = {
        path
        for path in ROOT.rglob("*")
        if path.is_file()
        and path.name == "INDEX.md"
        and (not is_under(path, THIRD_PARTY_ROOT) or path == THIRD_PARTY_ROOT / "INDEX.md")
        and not is_under(path, SKILL_ZIPS_ROOT)
        and not is_non_canonical_guard(path.parent)
        and path.parent in TRACKED_DIRS
    }
    unexpected = sorted(path for path in actual_paths if path not in expected_paths)
    missing = sorted(path for path in expected_paths if path not in actual_paths)

    if args.check:
        mismatches: list[str] = []
        for target in targets:
            if not target.path.exists():
                mismatches.append(f"missing: {target.path.relative_to(ROOT)}")
                continue
            raw = target.path.read_bytes()
            if b"\r" in raw:
                mismatches.append(f"stale: {target.path.relative_to(ROOT)} (CRLF line endings, needs LF normalization)")
                continue
            current = raw.decode("utf-8")
            rendered = "\n".join(target.lines).rstrip() + "\n"
            # The optional repo-specific extra hook may append content, so the
            # generated mesh is treated as the required prefix of the file.
            if not current.startswith(rendered):
                mismatches.append(f"stale: {target.path.relative_to(ROOT)}")
            mismatches.extend(validate_rendered_links(target.path, current))
        if unexpected:
            mismatches.extend(f"unexpected: {path.relative_to(ROOT)}" for path in unexpected)
        if missing:
            mismatches.extend(f"missing: {path.relative_to(ROOT)}" for path in missing)
        mismatches.extend(
            f"ERROR: generate_index_mesh_extra hook: {msg}" for msg in _run_index_mesh_extra_hook(ROOT, check=True)
        )
        if mismatches:
            raise ValueError("INDEX mesh is stale or inconsistent:\n" + "\n".join(mismatches))
        print(f"OK index mesh: {len(targets)} indexes current")
        return 0

    written = 0
    for target in targets:
        rendered = "\n".join(target.lines).rstrip() + "\n"
        target.path.parent.mkdir(parents=True, exist_ok=True)
        with target.path.open("w", encoding="utf-8", newline="\n") as handle:
            handle.write(rendered)
        written += 1

    obsolete = sorted(path for path in actual_paths if path not in expected_paths)
    for path in obsolete:
        path.unlink()

    # Run the repo-specific hook after writing the generated mesh but before
    # link validation, so any appended/post-processed content is validated too.
    hook_errors = _run_index_mesh_extra_hook(ROOT, check=False)
    if hook_errors:
        raise ValueError("ERROR: generate_index_mesh_extra hook failed:\n" + "\n".join(hook_errors))

    link_failures: list[str] = []
    for target in targets:
        current = target.path.read_text(encoding="utf-8")
        link_failures.extend(validate_rendered_links(target.path, current))
    if link_failures:
        raise ValueError("INDEX mesh produced broken links:\n" + "\n".join(link_failures))
    print(f"Wrote index mesh: {written} files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
