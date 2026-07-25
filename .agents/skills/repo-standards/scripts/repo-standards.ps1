<#
.SYNOPSIS
  Run the repo-standards check/apply script.
.DESCRIPTION
  Checks the repo against the repo-standards surface manifest or applies missing
  surfaces. Use --check for a safe read-only report. Use --apply with --yes to
  create missing surfaces; add --force to overwrite existing drifted surfaces.
.EXAMPLE
  repo-standards.ps1 --check
.EXAMPLE
  repo-standards.ps1 --apply --yes --force
#>
[CmdletBinding()]
param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Remaining)
$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$pyArgs = @()
foreach ($arg in $Remaining) {
    switch ($arg) {
        '-Check' { $pyArgs += '--check' }
        '-Apply' { $pyArgs += '--apply' }
        '-Yes' { $pyArgs += '--yes' }
        '-Force' { $pyArgs += '--force' }
        '-AllowSharedCheckout' { $pyArgs += '--allow-shared-checkout' }
        default { $pyArgs += $arg }
    }
}

$python = "py"
$launchers = @('py', 'python', 'python3')
foreach ($l in $launchers) {
    if (Get-Command $l -ErrorAction SilentlyContinue) {
        $python = $l
        break
    }
}

if ($python -eq 'py') {
    & py -3 "$scriptDir\repo_standards.py" @pyArgs
} else {
    & $python "$scriptDir\repo_standards.py" @pyArgs
}
exit $LASTEXITCODE
