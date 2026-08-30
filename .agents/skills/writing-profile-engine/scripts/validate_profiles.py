from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from _profile_common import ProfileError, discover, source_ids, validate_document


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate writing profiles without mutation (read-only).")
    parser.add_argument("--root", type=Path, help="Skill root or references/profiles root")
    parser.add_argument("--json", action="store_true", help="Emit machine-readable JSON")
    parser.add_argument("--check", action="store_true", help="Explicit read-only validation mode (default)")
    args = parser.parse_args()
    errors: list[str] = []
    warnings: list[str] = []
    try:
        profiles = discover(args.root)
        known_sources = source_ids()
        for profile in profiles:
            item_errors, item_warnings = validate_document(Path(profile["path"]), known_sources)
            errors.extend(item_errors)
            warnings.extend(item_warnings)
    except ProfileError as exc:
        errors.append(str(exc))
        profiles = []
    payload = {
        "schema_version": 1,
        "status": "invalid" if errors else "valid",
        "profiles_checked": len(profiles),
        "errors": sorted(errors),
        "warnings": sorted(warnings),
    }
    if args.json:
        print(json.dumps(payload, ensure_ascii=False, sort_keys=True))
    else:
        for warning in payload["warnings"]:
            print(f"WARNING: {warning}")
        for error in payload["errors"]:
            print(f"ERROR: {error}", file=sys.stderr)
        print(f"{payload['status']}: {payload['profiles_checked']} profile(s) checked")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
