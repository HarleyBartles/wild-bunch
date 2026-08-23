#!/usr/bin/env python3
"""Convenience wrapper around compile_metrics.py. (mutating)

This script exists to avoid the editor-buffer race that happens when the
`write` tool and the iterative-review scripts both touch `review-metrics.json`.
It delegates (re)compilation to `compile_metrics.py`, which writes the metrics
file without opening it in the IDE.

Contract:
- --help   prints usage and exits 0
- --check  reports whether the script is in a runnable state and exits 0
- --apply  (re)compiles review-metrics.json from review-state.json
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path


def _compile(state_path: Path, metrics_path: Path) -> int:
    script = Path(__file__).resolve().parent / "compile_metrics.py"
    return subprocess.run(
        [sys.executable, str(script), "--state", str(state_path), "--metrics", str(metrics_path)],
        check=False,
    ).returncode


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="(Re)compile review-metrics.json from review-state.json. (mutating)")
    parser.add_argument("--check", action="store_true", help="self-check; exits 0 if ready")
    parser.add_argument("--apply", action="store_true", help="(re)compile the metrics file")
    parser.add_argument("--metrics", help="Path to review-metrics.json")
    args = parser.parse_args(argv)

    if args.check:
        print("update_review_metrics.py is ready")
        return 0

    if not args.apply:
        print("--apply is required to compile review-metrics.json", file=sys.stderr)
        return 2

    if not args.metrics:
        print("--metrics is required with --apply", file=sys.stderr)
        return 2

    metrics_path = Path(args.metrics)
    state_path = metrics_path.with_name("review-state.json")
    return _compile(state_path, metrics_path)


if __name__ == "__main__":
    raise SystemExit(main())
