#!/usr/bin/env python3
"""Normalize review workspace files to UTF-8 (no BOM).

The iterative-review orchestrator calls this helper between review nodes so that
every file a later subagent reads is plain UTF-8. It converts UTF-16LE/BE, UTF-8
with BOM, and other encodings detected by a leading BOM into plain UTF-8 without
a BOM.

(mixed: --check is read-only; --apply is mutating.)
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

UTF8_BOM = b"\xef\xbb\xbf"
UTF16LE_BOM = b"\xff\xfe"
UTF16BE_BOM = b"\xfe\xff"


def _detect_encoding(raw: bytes) -> tuple[str, bool]:
    """Return (encoding, needs_rewrite) for the leading bytes."""
    if raw.startswith(UTF8_BOM):
        return "utf-8-sig", True
    if raw.startswith(UTF16LE_BOM):
        return "utf-16-le", True
    if raw.startswith(UTF16BE_BOM):
        return "utf-16-be", True
    return "utf-8", False


def _normalize(path: Path) -> tuple[bool, str | None]:
    """Return (rewritten, error) for a single file."""
    try:
        raw = path.read_bytes()
    except OSError as exc:
        return False, f"cannot read {path}: {exc}"

    if not raw:
        return False, None

    encoding, needs_rewrite = _detect_encoding(raw)
    if not needs_rewrite:
        try:
            raw.decode("utf-8")
        except UnicodeDecodeError as exc:
            return False, f"{path} is not valid UTF-8 or a known BOM encoding: {exc}"
        return False, None

    try:
        text = raw.decode(encoding)
    except UnicodeDecodeError as exc:
        return False, f"{path} claimed {encoding} but failed to decode: {exc}"

    normalized = text.encode("utf-8")
    path.write_bytes(normalized)
    return True, None


def _collect_paths(directory: Path, patterns: list[str]) -> list[Path]:
    if not directory.is_dir():
        return []
    paths: set[Path] = set()
    for pattern in patterns:
        for p in directory.rglob(pattern):
            if p.is_file() and p.stat().st_size > 0:
                paths.add(p)
    return sorted(paths)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Normalize review workspace files to UTF-8 (no BOM).",
        epilog="(mixed: default is a read-only check; --apply rewrites files.)",
    )
    parser.add_argument("paths", nargs="*", help="files or directories to normalize")
    parser.add_argument(
        "--source",
        type=Path,
        help="directory to scan for review inputs",
    )
    parser.add_argument(
        "--patterns",
        default="*.md,*.txt,*.json",
        help="comma-separated glob patterns to match under --source (default: *.md,*.txt,*.json)",
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="rewrite non-UTF-8 files in-place",
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="report non-UTF-8 files without writing",
    )
    args = parser.parse_args(argv)

    if args.apply and args.check:
        print("error: --apply and --check are mutually exclusive", file=sys.stderr)
        return 1

    targets: list[Path] = []
    if args.paths:
        for p in args.paths:
            path = Path(p)
            if path.is_dir():
                targets.extend(_collect_paths(path, args.patterns.split(",")))
            else:
                targets.append(path)
    else:
        source = args.source or Path(".")
        if source.is_dir():
            targets = _collect_paths(source, args.patterns.split(","))
        else:
            targets = [source]

    drift = False
    for path in targets:
        rewritten, err = _normalize(path)
        if err:
            print(f"FAIL {path}: {err}", file=sys.stderr)
            drift = True
            continue
        if not rewritten:
            if args.check:
                print(f"OK {path}")
            continue
        if args.apply:
            print(f"REWROTE {path}")
        else:
            print(f"DRIFT {path}")
        drift = True

    if not args.apply and not drift:
        print("No encoding drift found.")

    return 1 if drift and not args.apply else 0


if __name__ == "__main__":
    sys.exit(main())
