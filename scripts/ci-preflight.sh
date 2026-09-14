#!/usr/bin/env bash
set -euo pipefail

repo_root="$(git -C "$(dirname -- "${BASH_SOURCE[0]}")" rev-parse --show-toplevel)"
args=(ci --check)
if [[ "${1:-}" == "--diagnostics" ]]; then
  args+=(--diagnostics)
fi
python3 "$repo_root/tools/run.py" "${args[@]}"
