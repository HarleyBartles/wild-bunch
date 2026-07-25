<#
.SYNOPSIS
  Refresh installed skills from the plugin source.
#>
[CmdletBinding()]
param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Remaining)
$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$python = $null
foreach ($l in @('py', 'python', 'python3')) {
    if (Get-Command $l -ErrorAction SilentlyContinue) {
        $python = $l
        break
    }
}
if (-not $python) { throw "No Python interpreter found" }
if ($python -eq 'py') {
    & py -3 "$scriptDir\refresh_installed_skills.py" @Remaining
} else {
    & $python "$scriptDir\refresh_installed_skills.py" @Remaining
}
exit $LASTEXITCODE
