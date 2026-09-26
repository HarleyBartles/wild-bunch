#!/usr/bin/env bash
# Thin launcher for repo_standards.py. Run with --help to see usage.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
for python in python3 python; do
    if command -v "$python" >/dev/null 2>&1; then
        exec "$python" "${SCRIPT_DIR}/repo_standards.py" "$@"
    fi
done
echo "No Python interpreter found" >&2
exit 1
