<#
.SYNOPSIS
  Validate the repo-wide agent mesh.
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
if (-not $python) {
    throw "No Python interpreter found"
}

if ($python -eq 'py') {
    & py -3 "$scriptDir\validate_agent_mesh.py" @Remaining
} else {
    & $python "$scriptDir\validate_agent_mesh.py" @Remaining
}
exit $LASTEXITCODE
