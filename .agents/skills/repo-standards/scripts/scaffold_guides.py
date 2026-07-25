#!/usr/bin/env python3
"""Scaffold the repo-local .agents/guides/ set.

The script uses the mapping in .agents/docs/repo-guide-policy.md when present;
otherwise it falls back to the standard guide names under .agents/guides/.
"""

from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
from pathlib import Path


GUIDE_TITLES: dict[str, str] = {
    "design-guide.md": "Design guide",
    "planning-guide.md": "Planning guide",
    "implementing-guide.md": "Implementation guide",
    "code-review-guide.md": "Code review guide",
    "marketplace-generation-guide.md": "Marketplace generation guide",
    "skill-authoring-guide.md": "Skill authoring guide",
    "security-guide.md": "Security guide",
    "testing-guide.md": "Testing guide",
    "pr-guide.md": "Pull request guide",
    "code-style-guide.md": "Code style guide",
    "contributing-guide.md": "Contributing guide",
}


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


def _parse_repo_guide_policy(policy_path: Path) -> dict[str, Path] | None:
    if not policy_path.is_file():
        return None
    text = policy_path.read_text(encoding="utf-8")
    mapping: dict[str, Path] = {}
    for line in text.splitlines():
        if not line.strip().startswith("|"):
            continue
        parts = [p.strip().strip("`") for p in line.split("|") if p.strip() != ""]
        if len(parts) >= 2 and parts[0] in GUIDE_TITLES:
            mapping[parts[0]] = Path(parts[1])
    return mapping if mapping else None


def _default_mapping() -> dict[str, Path]:
    return {name: Path(".agents/guides") / name for name in GUIDE_TITLES}


def _guide_content(name: str) -> str:
    title = GUIDE_TITLES.get(name, name.replace("-", " ").title())
    return (
        f"# {title}\n\n"
        f"This is the repo-local {title.lower()}. "
        "It documents repo-specific conventions, commands, and exceptions.\n\n"
        "<!-- Add repo-specific guidance here. -->\n"
    )


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check               verify that all mapped guides exist
  %(prog)s                       create any missing mapped guides
  %(prog)s --force               overwrite all mapped guides with scaffolds

The guide list is read from the table in .agents/docs/repo-guide-policy.md
under ## Standard-to-local mapping if it exists, otherwise the standard guide
set under .agents/guides/ is used.

exit codes:
  0  all mapped guides are present or were written
  1  one or more mapped guides are missing"""
    parser = argparse.ArgumentParser(
        description="Scaffold the repo-local .agents/guides/ set.",
        epilog=epilog,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Report missing guides without writing",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Overwrite existing guide files",
    )
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    policy_path = repo_root / ".agents" / "docs" / "repo-guide-policy.md"
    mapping = _parse_repo_guide_policy(policy_path) or _default_mapping()

    missing: list[str] = []
    written: list[str] = []
    for standard_name, local_path in mapping.items():
        guide_path = repo_root / local_path
        if guide_path.is_file():
            if args.check:
                continue
            if not args.force:
                continue
        if args.check:
            missing.append(local_path.as_posix())
            continue
        guide_path.parent.mkdir(parents=True, exist_ok=True)
        guide_path.write_text(_guide_content(standard_name), encoding="utf-8", newline="\n")
        written.append(local_path.as_posix())

    if args.check:
        if missing:
            for path in missing:
                print(f"DRIFT: {path} missing")
            return 1
        print("OK all mapped guides present")
        return 0

    if written:
        for path in written:
            print(f"wrote {path}")
    else:
        print("All mapped guides already exist; use --force to overwrite")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
