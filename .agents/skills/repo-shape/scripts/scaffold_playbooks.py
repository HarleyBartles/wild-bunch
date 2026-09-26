#!/usr/bin/env python3
"""Scaffold the repo-local .agents/playbooks/ set."""

from __future__ import annotations

import argparse
import os
import re
import subprocess
from pathlib import Path


PLAYBOOK_TITLES: dict[str, str] = {
    "marketplace-generation.md": "Marketplace generation playbook",
    "skill-authoring.md": "Skill authoring playbook",
    "security.md": "Security playbook",
    "testing.md": "Testing playbook",
    "code-style.md": "Code style playbook",
    "repo-doctrine.md": "Repo doctrine playbook",
}


def _stripped_env() -> dict[str, str]:
    env = os.environ.copy()
    for name in ("GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE"):
        env.pop(name, None)
    return env


def _repo_root() -> Path:
    result = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True, check=True, env=_stripped_env()
    )
    return Path(result.stdout.strip())


def _parse_policy(path: Path) -> dict[str, Path] | None:
    if not path.is_file():
        return None
    mapping: dict[str, Path] = {}
    in_playbooks = False
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("## "):
            in_playbooks = line.strip() == "## Standard playbooks"
            continue
        if not in_playbooks or not line.strip().startswith("|"):
            continue
        parts = [part.strip().strip("`") for part in line.split("|") if part.strip()]
        if len(parts) >= 2 and parts[0] in PLAYBOOK_TITLES and (len(parts) < 3 or parts[2].lower() != "optional"):
            mapping[parts[0]] = Path(parts[1])
    return mapping or None


def _default_mapping() -> dict[str, Path]:
    return {
        "code-style.md": Path(".agents/playbooks/code-style.md"),
        "testing.md": Path(".agents/playbooks/testing.md"),
    }


def _playbook_content(name: str) -> str:
    template = Path(__file__).resolve().parent.parent / "templates" / name
    if template.is_file():
        return re.sub(
            r"<!--.*?-->",
            "Repository-specific binding may extend this required section.",
            template.read_text(encoding="utf-8"),
            flags=re.DOTALL,
        )
    title = PLAYBOOK_TITLES.get(name, name.replace("-", " ").title())
    sections = (
        ("When", "The class of change or trigger this playbook covers."),
        ("Required skills", "The capability skills this topical composition invokes."),
        ("Composition", "Order or conditions under which the capabilities apply."),
        ("Doctrine and contracts", "Local truths and shapes that constrain this composition."),
        ("Local commands and paths", "Repository commands, paths, and exceptions."),
        ("Evidence contract", "What the combined workflow must prove before it is complete."),
        ("Prohibited combinations", "Combinations explicitly not legitimate here, or `none`."),
        (
            "Runbook routing",
            "Optionally link `.agents/runbooks/*.md` stage roots that commonly route here; "
            "use `None.` when standalone.",
        ),
    )
    body = f"# {title}\n\nThis starter playbook composes the named repository concern.\n"
    for heading, prompt in sections:
        body += f"\n## {heading}\n\n{prompt}\n"
    return body


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Scaffold the repo-local .agents/playbooks/ set. (mixed)")
    parser.add_argument("--check", action="store_true", help="Report missing playbooks without writing")
    parser.add_argument("--force", action="store_true", help="Overwrite existing playbook files")
    args = parser.parse_args(argv)
    if args.force:
        print("ERROR: direct scaffold force is disabled; use confirmed repo-standards --force <surface-id>")
        return 1
    root = _repo_root()
    mapping = _parse_policy(root / ".agents/doctrine/repo-runbook-policy.md") or _default_mapping()
    missing: list[str] = []
    written: list[str] = []
    for name, relative in mapping.items():
        path = root / relative
        if path.is_file() and (args.check or not args.force):
            continue
        if args.check:
            missing.append(relative.as_posix())
            continue
        path.parent.mkdir(parents=True, exist_ok=True)
        with path.open("w", encoding="utf-8", newline="\n") as handle:
            handle.write(_playbook_content(name))
        written.append(relative.as_posix())
    if missing:
        for path in missing:
            print(f"DRIFT: {path} missing")
        return 1
    if args.check:
        print("OK all mapped playbooks present")
    elif written:
        for path in written:
            print(f"wrote {path}")
    else:
        print("Mapped playbooks exist; normal apply preserves them; use confirmed coordinator force for restore")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
