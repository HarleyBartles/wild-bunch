#!/usr/bin/env pwsh
# Thin launcher for scaffold_review.py. Run with --help to see usage.
[CmdletBinding()]
param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Remaining)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$check = $false
$force = $false
$extra = @()
foreach ($arg in $Remaining) {
    if ($arg -eq '-Check' -or $arg -eq '--check') {
        $check = $true
    } elseif ($arg -eq '-Force' -or $arg -eq '--force') {
        $force = $true
    } elseif ($arg -ne '') {
        $extra += $arg
    }
}

$pyArgs = @($extra)
if ($check) { $pyArgs = @('--check') + $pyArgs }
if ($force) { $pyArgs = @('--force') + $pyArgs }

$python = $null
foreach ($l in @('py', 'python', 'python3')) {
    if (Get-Command $l -ErrorAction SilentlyContinue) {
        $python = $l
        break
    }
}
if (-not $python) {
    throw "No Python interpreter found"
}

if ($python -eq 'py') {
    & py -3 "$scriptDir\scaffold_review.py" @pyArgs
} else {
    & $python "$scriptDir\scaffold_review.py" @pyArgs
}
exit $LASTEXITCODE
