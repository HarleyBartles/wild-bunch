#!/usr/bin/env python3
"""check_scope_honesty.py - compare the branch diff to the declared PR scope.

This is a concrete, scriptable version of the `scope-honesty` node. It checks
that every changed file is plausibly described by the PR body or the governing
plan/spec/roadmap. It is not a deep design review; it catches obvious scope drift.

(mixed: default runs the drift scan; --check is a self-check; --apply writes the off-repo log.)
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path, PurePosixPath


def _load(path: Path) -> dict:
    try:
        with path.open("r", encoding="utf-8-sig") as f:
            return json.load(f)
    except FileNotFoundError:
        raise SystemExit(f"ERROR: state file not found: {path}")
    except json.JSONDecodeError as e:
        raise SystemExit(f"ERROR: invalid state JSON in {path}: {e}")


def _changed_files(diff_path: Path | None) -> list[str]:
    if not diff_path or not diff_path.exists():
        return []
    text = diff_path.read_text(encoding="utf-8")
    pairs = re.findall(r"^diff --git a/(.+) b/(.+)$", text, re.M)
    return list(dict.fromkeys(f for pair in pairs for f in pair))


def _pr_text(scratch: Path) -> str:
    for name in ("pr_description.txt", "pr_description"):
        p = scratch / name
        if p.exists():
            return p.read_text(encoding="utf-8")
    return ""


def _governing_texts(scratch: Path, extras: list[Path] | None) -> list[str]:
    texts = []
    if extras:
        for p in extras:
            if p.exists():
                texts.append(p.read_text(encoding="utf-8"))
    return texts


def _path_mentioned(path: str, corpora: list[str]) -> bool:
    """Return True if the path, any parent path, or filename is found in any corpus."""
    p = PurePosixPath(path)
    variants: set[str] = {path, p.name}
    if p.parent != p and p.parent.name and p.parent.name != ".":
        variants.add(f"{p.parent.name}/{p.name}")
    # Add every parent directory path so a surface-level mention covers bulk changes.
    parent = p.parent
    while parent != parent.parent and parent.name and parent.name != ".":
        variants.add(str(parent))
        parent = parent.parent
    needle = {v.lower() for v in variants if v and len(v) > 1}
    for text in corpora:
        lowered = text.lower()
        for part in needle:
            if part and part in lowered:
                return True
    return False


def _check(state_path: Path, extras: list[Path] | None, apply: bool) -> tuple[bool, list[str]]:
    state = _load(state_path)
    scratch = Path(state.get("scratch_dir", state_path.parent))
    raw_diff = state.get("diff_path")
    diff_path = Path(raw_diff) if raw_diff else scratch / "review.diff"
    if not diff_path.exists():
        diff_path = _find_diff_path(scratch)

    changed = _changed_files(diff_path)
    pr_text = _pr_text(scratch)
    governing = _governing_texts(scratch, extras)
    corpora = [pr_text] + governing

    drift: list[str] = []
    for f in changed:
        if not _path_mentioned(f, corpora):
            drift.append(f)

    if not apply:
        return not drift, drift

    log_path = scratch / "review-log-scope-honesty.md"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    with log_path.open("w", encoding="utf-8", newline="\n") as out:
        out.write("## Inputs\n\n")
        diff_label = str(diff_path) if diff_path else "not found"
        out.write(f"- diff: `{diff_label}`\n")
        out.write(f"- PR description: `{scratch / 'pr_description.txt'}`\n")
        for extra in extras or []:
            out.write(f"- governing doc: `{extra}`\n")
        out.write(f"\n## Changed files: {len(changed)}\n\n")
        for f in changed:
            out.write(f"- `{f}`\n")
        if drift:
            out.write("\n## Drift\n\n")
            out.write("The following changed files are not mentioned in the PR body or governing documents:\n\n")
            for f in drift:
                out.write(f"- `{f}`\n")
            out.write("\nscope-honesty: drift\n")
        else:
            out.write("\n## Result\n\nAll changed files are covered by the PR description or governing documents.\n\n")
            out.write("scope-honesty: clean\n")

    return not drift, drift


def _find_diff_path(scratch: Path) -> Path | None:
    candidates = sorted(scratch.glob("review-*..*.diff"))
    if not candidates:
        return None
    if len(candidates) == 1:
        return candidates[0]
    return max(candidates, key=lambda p: p.stat().st_mtime)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Compare the branch diff to the declared PR scope. (mixed)",
        epilog="(mixed: default runs the drift scan; --check is a self-check; --apply writes the off-repo log.)",
    )
    parser.add_argument("--state", help="Path to review-state.json")
    parser.add_argument("--plan", type=Path, action="append", default=[], help="Governing plan file")
    parser.add_argument("--spec", type=Path, action="append", default=[], help="Governing spec file")
    parser.add_argument("--roadmap", type=Path, action="append", default=[], help="Governing roadmap file")
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Write review-log-scope-honesty.md and exit non-zero on drift",
    )
    parser.add_argument("--check", action="store_true", help="Validate CLI contract only")
    args = parser.parse_args(argv)

    if args.check:
        print("check_scope_honesty.py: --check ok")
        return 0

    if not args.state:
        parser.error("--state is required unless --check is used")

    extras = list(args.plan) + list(args.spec) + list(args.roadmap)
    clean, drift = _check(Path(args.state), extras, args.apply)

    if args.apply:
        if clean:
            print("scope-honesty: clean")
            return 0
        print(f"scope-honesty: drift ({len(drift)} file(s) not mentioned)")
        return 1

    print(f"Drift files: {len(drift)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
