#!/usr/bin/env python3
"""Check tracked AGENTS.md size, scope, required routes, and local links."""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path, PurePosixPath
from urllib.parse import unquote, urlsplit

ROOT_ROUTES = (
    ".agents/contracts/operating-standards.json",
    ".agents/contracts/standards-certification.md",
)
LINK = re.compile(r"(?<!!)\[[^\]]*\]\(([^)]+)\)")


def _check_links(root: Path, relative: str, text: str) -> list[str]:
    findings: list[str] = []
    source = PurePosixPath(relative)
    for raw in LINK.findall(text):
        target = raw.strip().split(maxsplit=1)[0].strip("<>")
        parsed = urlsplit(target)
        if parsed.scheme or parsed.netloc or target.startswith("#"):
            continue
        local = unquote(parsed.path)
        if not local:
            continue
        candidate = (root / Path(*source.parent.parts) / Path(local)).resolve()
        try:
            candidate.relative_to(root.resolve())
        except ValueError:
            findings.append(f"{relative}: local link escapes the repository: {target}")
            continue
        if not candidate.exists():
            findings.append(f"{relative}: missing local link target: {target}")
    return findings


def check_routers(root: Path, routers: dict[str, str]) -> list[str]:
    """Check supplied tracked router contents; useful for focused fixtures."""
    findings: list[str] = []
    root_text = routers.get("AGENTS.md")
    if root_text is None:
        findings.append("tracked AGENTS.md files must include root AGENTS.md")
    for relative, text in sorted(routers.items()):
        lines = text.splitlines()
        is_root = relative == "AGENTS.md"
        budget = 40 if is_root else 15
        if len(lines) > budget:
            findings.append(f"{relative}: {len(lines)} lines exceeds {budget}-line budget")
        if not is_root:
            statements = [
                line.strip() for line in lines if line.strip() and not line.lstrip().startswith("#")
            ]
            scope = PurePosixPath(relative).parent.as_posix()
            if (
                len(statements) != 1
                or scope not in statements[0]
                or not re.search(r"\bwhen\b", statements[0], re.IGNORECASE)
                or not re.search(r"\b(read|follow|consult)\b", statements[0], re.IGNORECASE)
                or not statements[0].endswith((".", "!", "?"))
            ):
                findings.append(
                    f"{relative}: scoped router must be one scoped sentence with a read condition"
                )
        for route in ROOT_ROUTES if is_root else ():
            if route not in text:
                findings.append(f"AGENTS.md must route to {route}")
        findings.extend(_check_links(root, relative, text))
    return findings


def _tracked_routers(root: Path) -> dict[str, str]:
    result = subprocess.run(["git", "ls-files", "-z"], cwd=root, capture_output=True, check=True)
    paths = [item.decode("utf-8") for item in result.stdout.split(b"\0") if item]
    routers: dict[str, str] = {}
    for relative in paths:
        if PurePosixPath(relative).name == "AGENTS.md":
            routers[relative] = (root / Path(relative)).read_text(encoding="utf-8")
    return routers


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Check tracked AGENTS.md routers. (read-only)")
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--check", action="store_true", help="check without writing")
    args = parser.parse_args(argv)
    try:
        findings = check_routers(args.repo_root, _tracked_routers(args.repo_root))
    except (OSError, UnicodeError, subprocess.CalledProcessError) as exc:
        print(f"ERROR: cannot inspect tracked AGENTS.md files: {exc}", file=sys.stderr)
        return 1
    for finding in findings:
        print(f"ERROR: {finding}", file=sys.stderr)
    if findings:
        return 1
    print(
        f"OK router structure and links ({len(_tracked_routers(args.repo_root))} tracked router(s))"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
