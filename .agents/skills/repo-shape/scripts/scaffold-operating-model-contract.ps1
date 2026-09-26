#!/usr/bin/env pwsh
[CmdletBinding()]
param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Remaining)
$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$python = if (Get-Command py -ErrorAction SilentlyContinue) { 'py' } elseif (Get-Command python -ErrorAction SilentlyContinue) { 'python' } else { 'python3' }
if ($python -eq 'py') { & py -3 "$scriptDir\scaffold_operating_model_contract.py" @Remaining } else { & $python "$scriptDir\scaffold_operating_model_contract.py" @Remaining }
exit $LASTEXITCODE
