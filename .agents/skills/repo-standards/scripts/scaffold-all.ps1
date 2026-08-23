#!/usr/bin/env pwsh
# Thin aggregator: runs the standard repo-standards scaffolds in order.
# Run with --help for usage.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$helpArgs = @('-?', '--help', '-h', '/?')
foreach ($a in $args) {
    if ($helpArgs -contains $a) {
        @'
Usage: scaffold-all.ps1 [--check] [--force]

Runs the standard repo-standards scaffolds in order:
  scaffold-repo-runbook-policy, scaffold-runbooks, scaffold-review,
  scaffold-contributing, scaffold-gitignore,
  scaffold-agents-md, scaffold-marketplace-json

Options:
  --check   Report drift without writing
  --force   Overwrite existing scaffolded surfaces

Each scaffold has its own --help; pass the individual script name
with --help to learn what it writes and validates.
'@ | Write-Output
        exit 0
    }
}

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$scripts = @('scaffold-repo-runbook-policy', 'scaffold-runbooks', 'scaffold-review', 'scaffold-contributing', 'scaffold-gitignore', 'scaffold-agents-md', 'scaffold-marketplace-json')

foreach ($name in $scripts) {
    Write-Host "==> running ${name}"
    & "${ScriptDir}/${name}.ps1" @args
    if (-not $?) {
        exit 1
    }
}
