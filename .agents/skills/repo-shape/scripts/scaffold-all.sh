#!/usr/bin/env bash
# Thin aggregator: runs the standard repo-standards scaffolds in order.
# Run with --help for usage.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ "$#" -gt 0 ] && { [ "$1" = "--help" ] || [ "$1" = "-h" ]; }; then
    cat <<'USAGE'
Usage: scaffold-all.sh [--check]

Runs the standard repo-standards scaffolds in order:
  scaffold-repo-runbook-policy, scaffold-runbooks, scaffold-playbooks, scaffold-review,
  scaffold-contributing, scaffold-gitignore,
  scaffold-agents-md, scaffold-marketplace-json, scaffold-operating-model-contract

Options:
  --check   Report drift without writing
Each scaffolded surface has its own --help; pass the individual script name
with --help to learn what it writes and validates.
USAGE
    exit 0
fi

for arg in "$@"; do
    if [ "$arg" = "--force" ]; then
        echo "Direct scaffold force is disabled; use confirmed repo-standards --force <surface-id>." >&2
        exit 1
    fi
done

for script in scaffold-repo-runbook-policy scaffold-runbooks scaffold-playbooks scaffold-review scaffold-contributing scaffold-gitignore scaffold-agents-md scaffold-marketplace-json scaffold-operating-model-contract; do
    echo "==> running ${script}"
    "${SCRIPT_DIR}/${script}.sh" "$@"
done
