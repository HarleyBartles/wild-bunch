#!/usr/bin/env python3
"""Validate that skill-bundled Python scripts follow the CLI contract.

Skill `scripts/*.py` contains two lanes:

- CLI scripts have an `if __name__ == "__main__":` guard and must support
  `--help` and `--check`.
- Helper modules do not have that guard and must be importable, have a leading
  module docstring, and must not use `argparse`.
"""

from __future__ import annotations

import argparse
import ast
import subprocess
import sys
from pathlib import Path


def _repo_root() -> Path:
    result = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True,
        text=True,
        check=True,
    )
    return Path(result.stdout.strip())


ROOT = _repo_root()
SCRIPTS_GLOB = ".agents/skills/*/scripts/*.py"


class Report:
    def __init__(self) -> None:
        self.cli_ok: list[str] = []
        self.cli_warn: list[str] = []
        self.cli_fail: list[str] = []
        self.helper_ok: list[str] = []
        self.helper_fail: list[str] = []

    def record(self, lane: str, status: str, path: Path, detail: str) -> None:
        rel = path.relative_to(ROOT).as_posix()
        line = f"{lane:6} {status:4} {rel}: {detail}"
        print(line)
        bucket = getattr(self, f"{lane.lower()}_{status.lower()}")
        bucket.append(rel)


def _is_cli(path: Path) -> bool:
    """A script is a CLI if it has the standard main guard."""
    return 'if __name__ == "__main__":' in path.read_text(encoding="utf-8")


def _run_help(path: Path) -> tuple[int, str]:
    result = subprocess.run(
        [sys.executable, str(path), "--help"],
        capture_output=True,
        text=True,
        timeout=30,
    )
    return result.returncode, result.stdout + result.stderr


def _run_check(path: Path) -> tuple[int, str]:
    result = subprocess.run(
        [sys.executable, str(path), "--check"],
        capture_output=True,
        text=True,
        timeout=60,
    )
    return result.returncode, result.stdout + result.stderr


def _classifies(help_text: str) -> bool:
    lowered = help_text.lower()
    return any(word in lowered for word in ["read-only", "mutating", "mixed"])


def _validate_cli(path: Path, report: Report) -> None:
    help_rc, help_text = _run_help(path)
    if help_rc != 0:
        report.record("CLI", "FAIL", path, f"--help exited {help_rc}")
        return
    if "usage" not in help_text.lower():
        report.record("CLI", "FAIL", path, "--help output does not contain 'usage:'")
        return
    if not _classifies(help_text):
        report.record("CLI", "WARN", path, "--help does not declare read-only/mutating/mixed classification")

    # The validator's own --check would recurse, so we only validate its --help.
    if path.name == "validate_skill_scripts.py":
        status = "OK" if _classifies(help_text) else "WARN"
        report.record("CLI", status, path, "--help and --check contract supported")
        return

    check_rc, _ = _run_check(path)
    if check_rc == 2:
        report.record("CLI", "FAIL", path, "--check exits 2 (unrecognized argument); contract requires --check support")
        return

    if _classifies(help_text):
        report.record("CLI", "OK", path, f"--help and --check respond ({check_rc})")
    else:
        report.record("CLI", "OK", path, f"--help and --check respond ({check_rc}); add classification to help text")


def _validate_helper(path: Path, report: Report) -> None:
    text = path.read_text(encoding="utf-8")
    try:
        tree = ast.parse(text)
    except SyntaxError as exc:
        report.record("HELPER", "FAIL", path, f"not importable: {exc}")
        return

    if not ast.get_docstring(tree):
        report.record("HELPER", "FAIL", path, "missing module docstring")
        return

    for node in ast.walk(tree):
        if isinstance(node, ast.Import) and any(alias.name == "argparse" for alias in node.names):
            report.record("HELPER", "FAIL", path, "helper must not import argparse")
            return
        if isinstance(node, ast.ImportFrom) and node.module == "argparse":
            report.record("HELPER", "FAIL", path, "helper must not import argparse")
            return

    report.record("HELPER", "OK", path, "helper contract satisfied")


def _validate_one(path: Path, report: Report) -> None:
    if _is_cli(path):
        _validate_cli(path, report)
    else:
        _validate_helper(path, report)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Validate first-party skill-bundled Python scripts follow the --help/--check contract. (read-only)"
    )
    parser.add_argument(
        "--check",
        action="store_true",
        default=True,
        help="run validation and report drift (default, read-only)",
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="alias for --check; this validator is read-only (read-only)",
    )
    args = parser.parse_args(argv)

    report = Report()
    scripts = sorted(ROOT.glob(SCRIPTS_GLOB))
    if not scripts:
        print("no skill scripts found", file=sys.stderr)
        return 1

    for path in scripts:
        _validate_one(path, report)

    print()
    print(
        f"CLI OK: {len(report.cli_ok)}  CLI WARN: {len(report.cli_warn)}  "
        f"CLI FAIL: {len(report.cli_fail)}  HELPER OK: {len(report.helper_ok)}  "
        f"HELPER FAIL: {len(report.helper_fail)}"
    )
    if report.cli_fail or report.helper_fail:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
