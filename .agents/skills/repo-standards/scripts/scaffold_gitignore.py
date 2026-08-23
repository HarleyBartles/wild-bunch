#!/usr/bin/env python3
"""Ensure the repo's .gitignore is free of stale in-repo SDD rules."""

from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path


STALE_RULE_PATTERNS = {
    ".agents/superpowers/sdd/**",
    "!.agents/superpowers/sdd/.gitignore",
}

# Full block the old scaffold used to append; removed in apply mode if contiguous.
STALE_RULE_BLOCK = """# Superpowers sdd/ is a local-only session workspace.
# Track only the directory scaffold (.gitignore); ignore all session contents at any depth.
# plans/ and specs/ are fully repo resident and not governed by this block.
.agents/superpowers/sdd/**
!.agents/superpowers/sdd/.gitignore"""

SDD_GITIGNORE_PATH = Path(".agents") / "superpowers" / "sdd" / ".gitignore"


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


def _has_stale_root_rule(content: str) -> bool:
    lines = {line.strip() for line in content.splitlines()}
    return bool(STALE_RULE_PATTERNS & lines)


def _has_stale_block(content: str) -> bool:
    return STALE_RULE_BLOCK in content


def _remove_stale_root_rule(content: str) -> str:
    # Remove the exact contiguous block the old scaffold used to append.
    if _has_stale_block(content):
        content = content.replace(STALE_RULE_BLOCK, "")
    # Remove any remaining stale pattern lines that may have been added manually.
    lines = content.splitlines()
    cleaned = [line for line in lines if line.strip() not in STALE_RULE_PATTERNS]
    text = "\n".join(cleaned).rstrip()
    if text:
        text += "\n"
    return text


def _stale_sdd_scaffold_exists(repo_root: Path) -> bool:
    sdd_gitignore = repo_root / SDD_GITIGNORE_PATH
    return sdd_gitignore.is_file() or (repo_root / ".agents" / "superpowers" / "sdd").is_dir()


def _remove_stale_sdd_scaffold(repo_root: Path) -> list[str]:
    sdd_dir = repo_root / ".agents" / "superpowers" / "sdd"
    removed: list[str] = []
    if not sdd_dir.exists():
        return removed
    sdd_gitignore = sdd_dir / ".gitignore"
    if sdd_gitignore.is_file():
        sdd_gitignore.unlink()
        removed.append(sdd_gitignore.relative_to(repo_root).as_posix())
    # Remove empty parent .agents/superpowers/sdd directory.
    try:
        sdd_dir.rmdir()
        removed.append(sdd_dir.relative_to(repo_root).as_posix())
    except OSError:
        pass
    return removed


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check               verify the root .gitignore has no stale sdd rule
  %(prog)s                       remove any stale sdd rule or in-repo sdd scaffold
  %(prog)s --force               same as without --force (accepted for uniform CLI)

The SDD workspace now lives outside the repo at:

  <repo-root>/../_agent-scratch/<branch>/<plan-basename>/

This script only cleans up stale in-repo ignore rules. It removes any root
.gitignore block that mentions .agents/superpowers/sdd and deletes any
.agents/superpowers/sdd/.gitignore directory that still exists from an older
layout.

exit codes:
  0  no stale rules (or clean-up applied)
  1  drift detected or files could not be written"""
    parser = argparse.ArgumentParser(
        description="Ensure the repo's .gitignore is free of stale in-repo SDD rules. (mixed)",
        epilog=epilog,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Report drift without writing",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Accepted for a uniform scaffold interface; has no destructive effect",
    )
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    root_gitignore_path = repo_root / ".gitignore"

    drift_messages: list[str] = []

    if not root_gitignore_path.is_file():
        drift_messages.append("DRIFT: .gitignore missing")
    else:
        root_content = root_gitignore_path.read_text(encoding="utf-8")
        if _has_stale_root_rule(root_content):
            drift_messages.append("DRIFT: .gitignore contains stale sdd rule")

    if _stale_sdd_scaffold_exists(repo_root):
        drift_messages.append(f"DRIFT: stale {SDD_GITIGNORE_PATH.as_posix()} or .agents/superpowers/sdd/ present")

    if args.check:
        if drift_messages:
            for msg in drift_messages:
                print(msg)
            return 1
        print("OK .gitignore: no stale sdd rules")
        return 0

    removed_files: list[str] = []

    if drift_messages:
        if not root_gitignore_path.is_file():
            root_gitignore_path.write_text("", encoding="utf-8", newline="\n")
            print(f"wrote {root_gitignore_path.relative_to(repo_root).as_posix()}")
        else:
            root_content = root_gitignore_path.read_text(encoding="utf-8")
            cleaned = _remove_stale_root_rule(root_content)
            if cleaned != root_content:
                with root_gitignore_path.open("w", encoding="utf-8", newline="\n") as f:
                    f.write(cleaned)
                print(f"updated {root_gitignore_path.relative_to(repo_root).as_posix()}")

        removed_files.extend(_remove_stale_sdd_scaffold(repo_root))
        if removed_files:
            for p in removed_files:
                print(f"removed {p}")

    if drift_messages:
        return 0

    print("OK .gitignore: no stale sdd rules")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
