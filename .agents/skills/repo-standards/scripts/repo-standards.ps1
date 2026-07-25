<#
.SYNOPSIS
  Run the repo-standards check/apply script.
#>
[CmdletBinding()]
param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Remaining)
$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$python = "py"
$launchers = @('py', 'python', 'python3')
foreach ($l in $launchers) {
    if (Get-Command $l -ErrorAction SilentlyContinue) {
        $python = $l
        break
    }
}

& $python "$scriptDir\repo_standards.py" @Remaining
exit $LASTEXITCODE
