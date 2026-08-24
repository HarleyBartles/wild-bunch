#!/usr/bin/env python3
"""select_lenses.py - discover and select reviewer lens profiles for a PR. (mixed)"""

from __future__ import annotations

import argparse
import fnmatch
import json
import os
import re
import sys
from pathlib import Path, PurePath


def _user_agent_root() -> Path:
    """Return the user-global agents directory, honoring DEVIN_AGENTS."""
    env = os.environ.get("DEVIN_AGENTS")
    if env:
        return Path(env)
    if sys.platform != "win32":
        return Path.home() / ".config" / "devin" / "agents"
    return Path(os.environ.get("APPDATA", Path.home() / "AppData" / "Roaming")) / "devin" / "agents"


def _reviewer_paths() -> list[Path]:
    """Return candidate reviewer-*.md paths in precedence order."""
    roots = [
        _user_agent_root(),
        Path(".devin/agents"),
        Path(".agents/agents"),
        Path(__file__).parents[3] / "skills" / "selecting-a-subagent" / "assets",
    ]
    seen = set()
    results = []
    for root in roots:
        if not root.exists():
            continue
        for p in sorted(root.glob("reviewer-*.md")):
            if p.name in seen:
                continue
            seen.add(p.name)
            results.append(p)
    return results


def _applies_to(text: str) -> dict:
    """Parse the ## Applies to section from a reviewer profile."""
    section_match = re.search(r"## Applies to(.*?)\n(?:## |\Z)", text, re.S)
    if not section_match:
        return {}
    section = section_match.group(1)

    def _list_items(name: str) -> list[str]:
        pattern = re.compile(rf"- {re.escape(name)}:\s*\n((?:[ \t]+- [^\n]*(?:\n|\Z))+)", re.S)
        m = pattern.search(section)
        if not m:
            return []
        lines = m.group(1).strip().splitlines()
        return [line.strip("- ").strip().strip("`") for line in lines if line.strip().startswith("-")]

    return {
        "globs": _list_items("globs"),
        "keywords": _list_items("keywords"),
        "inputs": _list_items("inputs"),
    }


COMMON_INPUTS = {
    "<diff_path>",
    "<pr_description>",
    "<scan_findings>",
    "<review-log-reviewer-fast>",
}


def _changed_files(diff_path: Path | None) -> list[str]:
    if not diff_path or not diff_path.exists():
        return []
    text = diff_path.read_text(encoding="utf-8")
    pairs = re.findall(r"^diff --git a/(.+) b/(.+)$", text, re.M)
    return list(dict.fromkeys(f for pair in pairs for f in pair))


def _match_parts(pattern_parts: list[str], path_parts: tuple[str, ...]) -> bool:
    """Match a glob pattern split on '/' against path parts, supporting '**'."""
    if not pattern_parts:
        return not path_parts
    head, *tail = pattern_parts
    if head == "**":
        for i in range(len(path_parts) + 1):
            if _match_parts(tail, path_parts[i:]):
                return True
        return False
    if not path_parts:
        return False
    if fnmatch.fnmatchcase(path_parts[0], head):
        return _match_parts(tail, path_parts[1:])
    return False


def _glob_match(pattern: str, path: str) -> bool:
    return _match_parts(pattern.split("/"), PurePath(path).parts)


def _matches(rule: dict, changed: list[str], diff_text: str, pr_text: str, provided_inputs: set[str]) -> bool:
    for inp in rule.get("inputs", []):
        if inp not in COMMON_INPUTS and inp in provided_inputs:
            return True
    for pattern in rule.get("globs", []):
        if any(_glob_match(pattern, f) for f in changed):
            return True
    for keyword in rule.get("keywords", []):
        if keyword.lower() in diff_text.lower() or keyword.lower() in pr_text.lower():
            return True
    return False


def _lenses_path(state: dict) -> Path:
    return Path(state["scratch_dir"]) / "lenses.jsonl"


def _load_state(state_path: Path) -> dict:
    try:
        with state_path.open("r", encoding="utf-8-sig") as f:
            return json.load(f)
    except FileNotFoundError:
        raise SystemExit(f"ERROR: state file not found: {state_path}")
    except json.JSONDecodeError as e:
        raise SystemExit(f"ERROR: invalid state JSON in {state_path}: {e}")


def _find_diff_path(scratch: Path) -> Path | None:
    candidates = sorted(scratch.glob("review-*..*.diff"))
    if not candidates:
        return None
    if len(candidates) == 1:
        return candidates[0]
    return max(candidates, key=lambda p: p.stat().st_mtime)


def _select(state: dict, provided_inputs: set[str]) -> list[dict]:
    scratch = Path(state["scratch_dir"])
    if "diff_path" in state:
        diff_path = Path(state["diff_path"])
        if not diff_path.exists():
            diff_path = _find_diff_path(scratch)
    else:
        diff_path = _find_diff_path(scratch)
    for pr_candidate in (scratch / "pr_description", scratch / "pr_description.txt"):
        if pr_candidate.exists():
            pr_path = pr_candidate
            break
    else:
        pr_path = scratch / "pr_description"
    if not diff_path or not diff_path.exists():
        raise SystemExit(f"ERROR: review diff not found in {scratch}")
    diff_text = diff_path.read_text(encoding="utf-8")
    pr_text = pr_path.read_text(encoding="utf-8") if pr_path.exists() else ""
    changed = _changed_files(diff_path)

    selected = []
    for profile in _reviewer_paths():
        text = profile.read_text(encoding="utf-8")
        rule = _applies_to(text)
        lens = profile.stem
        if _matches(rule, changed, diff_text, pr_text, provided_inputs) and lens not in {
            "reviewer-strong",
            "reviewer-fixes",
        }:
            selected.append(
                {
                    "lens": lens,
                    "profile_path": str(profile.resolve()),
                    "output_path": str((scratch / f"review-log-{lens}.md").resolve()),
                }
            )
    selected = [s for s in selected if s["lens"] != "reviewer-fast"]
    return selected


def _select_filtered(
    state: dict,
    provided_inputs: set[str],
    include: set[str] | None,
    exclude: set[str] | None,
) -> list[dict]:
    selected = _select(state, provided_inputs)
    if include:
        selected = [s for s in selected if s["lens"] in include]
    if exclude:
        selected = [s for s in selected if s["lens"] not in exclude]
    return selected


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Select reviewer lenses for a PR. (mixed)")
    parser.add_argument("--state", help="Path to review-state.json")
    parser.add_argument(
        "--input",
        action="append",
        default=[],
        help="Lens-specific input token that is provided (repeatable)",
    )
    parser.add_argument(
        "--lens",
        action="append",
        default=[],
        help="Explicitly include only these lens names (repeatable)",
    )
    parser.add_argument(
        "--exclude",
        action="append",
        default=[],
        help="Exclude these lens names (repeatable)",
    )
    parser.add_argument("--apply", action="store_true", help="Write lenses.jsonl to the scratch dir")
    parser.add_argument("--check", action="store_true", help="Validate CLI contract only")
    args = parser.parse_args(argv)

    if args.check:
        print("select_lenses.py: --check ok")
        return 0

    if not args.state:
        parser.error("--state is required unless --check is used")

    state = _load_state(Path(args.state))
    provided_inputs = set(args.input)
    include = set(args.lens) if args.lens else None
    exclude = set(args.exclude) if args.exclude else None
    selected = _select_filtered(state, provided_inputs, include, exclude)

    out_path = _lenses_path(state)
    if args.apply:
        out_path.parent.mkdir(parents=True, exist_ok=True)
        with out_path.open("w", encoding="utf-8", newline="\n") as f:
            for entry in selected:
                f.write(json.dumps(entry, ensure_ascii=False) + "\n")
        print(f"Wrote {out_path} with {len(selected)} lens(es)")
    else:
        for entry in selected:
            print(entry["lens"], "->", entry["output_path"])
    return 0


if __name__ == "__main__":
    sys.exit(main())
