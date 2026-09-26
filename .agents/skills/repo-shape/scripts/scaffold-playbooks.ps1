#!/usr/bin/env pwsh
# Thin launcher for scaffold_playbooks.py. Run with --help to see usage.
[CmdletBinding()]
param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Remaining)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$python = if (Get-Command py -ErrorAction SilentlyContinue) { 'py' } elseif (Get-Command python -ErrorAction SilentlyContinue) { 'python' } else { 'python3' }
if ($python -eq 'py') { & py -3 "$scriptDir\scaffold_playbooks.py" @Remaining } else { & $python "$scriptDir\scaffold_playbooks.py" @Remaining }
exit $LASTEXITCODE
