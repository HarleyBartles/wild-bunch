#!/usr/bin/env python3
"""diff_slicer.py - slice a branch diff for each selected reviewer lens. (mixed)"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path, PurePath


# Re-use the glob/parsing helpers from select_lenses.py.
_select_lenses_dir = Path(__file__).resolve().parent
if str(_select_lenses_dir) not in sys.path:
    sys.path.insert(0, str(_select_lenses_dir))
import select_lenses  # noqa: E402

_applies_to = select_lenses._applies_to
_changed_files = select_lenses._changed_files
_find_diff_path = select_lenses._find_diff_path
_glob_match = select_lenses._glob_match
_load_state = select_lenses._load_state


FULL_DIFF_LENSES = {"reviewer-fast"}


def _installed_mirror_paths(changed: list[str], repo_root: Path) -> set[str]:
    """Return installed .agents/skills paths whose source also appears in the diff."""
    if not (repo_root / "codex-marketplace" / "plugins").exists():
        return set()
    mirrors = set()
    for path in changed:
        if not path.startswith(".agents/skills/"):
            continue
        rel = PurePath(path).relative_to(".agents/skills")
        parts = rel.parts
        if len(parts) < 2:
            continue
        skill = parts[0]
        rest = "/".join(parts[1:])
        source = f"codex-marketplace/plugins/**/skills/{skill}/{rest}"
        if any(_glob_match(source, c) for c in changed):
            mirrors.add(path)
    return mirrors


def _slice_hunks(diff_text: str, include_globs: list[str], exclude_paths: set[str]) -> str:
    """Return the diff restricted to hunks matching include_globs and not excluded."""
    out: list[str] = []
    current: list[str] = []
    current_paths: set[str] = set()
    include = False
    for raw_line in diff_text.splitlines(keepends=True):
        line = raw_line.rstrip("\n")
        match = re.match(r"^diff --git a/(.+) b/(.+)$", line)
        if match:
            if current and include:
                out.extend(current)
            a, b = match.groups()
            current = [raw_line]
            current_paths = {a, b}
            include = any(_glob_match(g, p) for g in include_globs for p in current_paths if p)
            if a in exclude_paths or b in exclude_paths:
                include = False
        else:
            current.append(raw_line)
    if current and include:
        out.extend(current)
    return "".join(out)


def _read_lenses(path: Path) -> list[dict]:
    entries: list[dict] = []
    with path.open("r", encoding="utf-8") as f:
        for line_number, line in enumerate(f, start=1):
            if not line.strip():
                continue
            try:
                entries.append(json.loads(line))
            except json.JSONDecodeError as e:
                raise SystemExit(f"ERROR: invalid JSON in {path} at line {line_number}: {e}")
    return entries


def _write_lenses(path: Path, lenses: list[dict]) -> None:
    with path.open("w", encoding="utf-8", newline="\n") as f:
        for entry in lenses:
            f.write(json.dumps(entry, ensure_ascii=False) + "\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Slice a branch diff per reviewer lens. (mixed)")
    parser.add_argument("--state", help="Path to review-state.json")
    parser.add_argument("--apply", action="store_true", help="Write sliced diffs and update lenses.jsonl")
    parser.add_argument("--check", action="store_true", help="Validate CLI contract only")
    args = parser.parse_args(argv)

    if args.check:
        print("diff_slicer.py: --check ok")
        return 0

    if not args.state:
        parser.error("--state is required unless --check is used")

    state = _load_state(Path(args.state))
    scratch = Path(state["scratch_dir"])
    diff_path = Path(state.get("diff_path")) if state.get("diff_path") else _find_diff_path(scratch)
    if not diff_path or not diff_path.exists():
        raise SystemExit(f"ERROR: full diff not found at {diff_path}")

    diff_text = diff_path.read_text(encoding="utf-8")
    changed = _changed_files(diff_path)
    repo_root = Path(state.get("repo_root", Path.cwd()))
    exclude_paths = _installed_mirror_paths(changed, repo_root)

    lenses_path = scratch / "lenses.jsonl"
    lenses = _read_lenses(lenses_path) if lenses_path.exists() else []

    for entry in lenses:
        lens = entry.get("lens")
        profile_path = entry.get("profile_path")
        if not lens or not profile_path:
            raise SystemExit(f"ERROR: lens entry missing 'lens' or 'profile_path': {entry}")
        profile = Path(profile_path)
        try:
            text = profile.read_text(encoding="utf-8")
        except FileNotFoundError:
            raise SystemExit(f"ERROR: profile not found: {profile}")
        rule = _applies_to(text)
        globs = rule.get("globs", [])
        if lens in FULL_DIFF_LENSES or not globs:
            if args.apply:
                entry["diff_path"] = str(diff_path)
            else:
                print(f"{lens} -> full diff ({len(diff_text)} bytes)")
            continue
        sliced = _slice_hunks(diff_text, globs, exclude_paths)
        if args.apply:
            slice_path = scratch / f"lens-{lens}-{diff_path.name}"
            slice_path.write_text(sliced, encoding="utf-8", newline="\n")
            entry["diff_path"] = str(slice_path)
        else:
            print(f"{lens} -> would write {len(sliced)} bytes")

    if args.apply:
        _write_lenses(lenses_path, lenses)
        print(f"Sliced {len(lenses)} lens diff(s) in {scratch}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
