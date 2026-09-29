#!/usr/bin/env python3
"""Run the pinned skill-script validator when repository-owned scripts exist."""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
VALIDATOR = ROOT / ".agents/plugins/marketplace-source/skills/repo-shape/scripts/validate_skill_scripts.py"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Validate repository-owned skill scripts. (read-only)")
    parser.add_argument("--check", action="store_true", help="validate scripts without writing")
    args = parser.parse_args(argv)
    if not args.check:
        parser.error("--check is required")

    skill_root = ROOT / ".agents/skills"
    if not any(skill_root.glob("*/scripts/*.py")):
        print("OK skill scripts: no repository-owned Python skill scripts to validate")
        return 0

    return subprocess.run(
        [sys.executable, str(VALIDATOR), "--root", str(skill_root), "--check"],
        cwd=ROOT,
        check=False,
    ).returncode


if __name__ == "__main__":
    raise SystemExit(main())
