<#
.SYNOPSIS
  Run the repo-standards check/apply script.
.DESCRIPTION
  Checks the repo against the repo-standards surface manifest or applies missing
  surfaces. Use --check for a safe read-only report. Use --apply with --yes to
  create missing surfaces. Force deployment requires a named surface and the
  explicit local-customisation overwrite acknowledgement.
.EXAMPLE
  repo-standards.ps1 --check
.EXAMPLE
  repo-standards.ps1 --force completed-artifacts-doctrine --confirm-local-customisations-will-be-overwritten
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
        '-ConfirmLocalCustomisationsWillBeOverwritten' { $pyArgs += '--confirm-local-customisations-will-be-overwritten' }
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
