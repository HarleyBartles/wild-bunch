#!/usr/bin/env bash
# Thin aggregator: runs the standard repo-standards scaffolds in order.
# Run with --help for usage.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ "$#" -gt 0 ] && { [ "$1" = "--help" ] || [ "$1" = "-h" ]; }; then
    cat <<'USAGE'
Usage: scaffold-all.sh [--check] [--force]

Runs the standard repo-standards scaffolds in order:
  scaffold-repo-runbook-policy, scaffold-runbooks, scaffold-review,
  scaffold-contributing, scaffold-gitignore,
  scaffold-agents-md, scaffold-marketplace-json

Options:
  --check   Report drift without writing
  --force   Overwrite existing scaffolded surfaces

Each scaffolded surface has its own --help; pass the individual script name
with --help to learn what it writes and validates.
USAGE
    exit 0
fi

for script in scaffold-repo-runbook-policy scaffold-runbooks scaffold-review scaffold-contributing scaffold-gitignore scaffold-agents-md scaffold-marketplace-json; do
    echo "==> running ${script}"
    "${SCRIPT_DIR}/${script}.sh" "$@"
done
