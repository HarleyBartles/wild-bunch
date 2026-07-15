#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
python_script="$script_dir/image_asset_pipeline.py"

if [[ ! -f "$python_script" ]]; then
  echo "Python script not found at $python_script" >&2
  exit 1
fi

if command -v python3 >/dev/null 2>&1; then
  python_launcher=(python3)
elif command -v python >/dev/null 2>&1; then
  python_launcher=(python)
elif command -v py >/dev/null 2>&1; then
  python_launcher=(py -3)
else
  echo "No Python launcher found. Please install Python 3.11+ and ensure it is in PATH." >&2
  exit 1
fi

"${python_launcher[@]}" "$python_script" "$@"
