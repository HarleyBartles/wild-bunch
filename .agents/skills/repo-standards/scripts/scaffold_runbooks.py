#!/usr/bin/env python3
"""Scaffold the repo-local .agents/runbooks/ set.

The script uses the mapping in .agents/doctrine/repo-runbook-policy.md when present;
otherwise it falls back to the standard runbook names under .agents/runbooks/.
"""

from __future__ import annotations

import argparse
import os
import subprocess
from pathlib import Path


RUNBOOK_TITLES: dict[str, str] = {
    "design.md": "Design runbook",
    "planning.md": "Planning runbook",
    "implementing.md": "Implementation runbook",
    "code-review.md": "Code review runbook",
    "marketplace-generation.md": "Marketplace generation runbook",
    "skill-authoring.md": "Skill authoring runbook",
    "security.md": "Security runbook",
    "testing.md": "Testing runbook",
    "pr.md": "Pull request runbook",
    "code-style.md": "Code style runbook",
    "repo-doctrine.md": "Repo doctrine runbook",
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


def _parse_repo_runbook_policy(policy_path: Path) -> dict[str, Path] | None:
    if not policy_path.is_file():
        return None
    text = policy_path.read_text(encoding="utf-8")
    mapping: dict[str, Path] = {}
    for line in text.splitlines():
        if not line.strip().startswith("|"):
            continue
        parts = [p.strip().strip("`") for p in line.split("|") if p.strip() != ""]
        if len(parts) >= 2 and parts[0] in RUNBOOK_TITLES:
            mapping[parts[0]] = Path(parts[1])
    return mapping if mapping else None


def _default_mapping() -> dict[str, Path]:
    return {name: Path(".agents/runbooks") / name for name in RUNBOOK_TITLES}


def _template_dir() -> Path:
    return Path(__file__).resolve().parent.parent / "templates"


def _runbook_content(name: str) -> str:
    template = _template_dir() / name
    if template.is_file():
        return template.read_text(encoding="utf-8")
    title = RUNBOOK_TITLES.get(name, name.replace("-", " ").title())
    return (
        f"# {title}\n\n"
        f"This is the repo-local {title.lower()}. "
        "It documents repo-specific conventions, commands, and exceptions.\n\n"
        "<!-- Add repo-specific guidance here. -->\n"
    )


def main(argv: list[str] | None = None) -> int:
    epilog = """\
examples:
  %(prog)s --check               verify that all mapped runbooks exist
  %(prog)s                       create any missing mapped runbooks
  %(prog)s --force               overwrite all mapped runbooks with scaffolds

The runbook list is read from the table in .agents/doctrine/repo-runbook-policy.md
under ## Standard-to-local mapping if it exists, otherwise the standard runbook
set under .agents/runbooks/ is used.

exit codes:
  0  all mapped runbooks are present or were written
  1  one or more mapped runbooks are missing"""
    parser = argparse.ArgumentParser(
        description="Scaffold the repo-local .agents/runbooks/ set. (mixed)",
        epilog=epilog,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Report missing runbooks without writing",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Overwrite existing runbook files",
    )
    args = parser.parse_args(argv)

    repo_root = _repo_root()
    policy_path = repo_root / ".agents" / "doctrine" / "repo-runbook-policy.md"
    mapping = _parse_repo_runbook_policy(policy_path) or _default_mapping()

    missing: list[str] = []
    written: list[str] = []
    for standard_name, local_path in mapping.items():
        runbook_path = repo_root / local_path
        if runbook_path.is_file():
            if args.check:
                continue
            if not args.force:
                continue
        if args.check:
            missing.append(local_path.as_posix())
            continue
        runbook_path.parent.mkdir(parents=True, exist_ok=True)
        with runbook_path.open("w", encoding="utf-8", newline="\n") as f:
            f.write(_runbook_content(standard_name))
        written.append(local_path.as_posix())

    if args.check:
        if missing:
            for path in missing:
                print(f"DRIFT: {path} missing")
            return 1
        print("OK all mapped runbooks present")
        return 0

    if written:
        for path in written:
            print(f"wrote {path}")
    else:
        print("All mapped runbooks already exist; use --force to overwrite")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
