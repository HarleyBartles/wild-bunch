#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ps_script="$script_dir/dev-servers.ps1"

if [[ ! -f "$ps_script" ]]; then
  echo "PowerShell script not found at $ps_script" >&2
  exit 1
fi

if command -v pwsh >/dev/null 2>&1; then
  shell_cmd=pwsh
elif command -v powershell.exe >/dev/null 2>&1; then
  shell_cmd=powershell.exe
elif command -v powershell >/dev/null 2>&1; then
  shell_cmd=powershell
else
  echo "No PowerShell launcher found. Please install PowerShell and ensure it is in PATH." >&2
  exit 1
fi

exec "$shell_cmd" -File "$ps_script" "$@"
