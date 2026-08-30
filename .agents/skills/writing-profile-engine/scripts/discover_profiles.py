from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from _profile_common import ProfileError, discover


def main() -> int:
    parser = argparse.ArgumentParser(description="Discover lawful writing profiles (read-only).")
    parser.add_argument("--root", type=Path, help="Skill root or references/profiles root")
    parser.add_argument("--json", action="store_true", help="Emit machine-readable JSON")
    parser.add_argument("--check", action="store_true", help="Explicit read-only discovery mode (default)")
    args = parser.parse_args()
    try:
        profiles = discover(args.root)
    except ProfileError as exc:
        print(str(exc), file=sys.stderr)
        return 2
    if args.json:
        print(json.dumps({"schema_version": 1, "profiles": profiles}, ensure_ascii=False, sort_keys=True))
    else:
        for profile in profiles:
            if profile["status"] == "invalid":
                print(f"invalid {profile['path']}: {profile['error']}")
            else:
                print(f"{profile['id']} {profile['version']} {profile['kind']} {profile['path']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
