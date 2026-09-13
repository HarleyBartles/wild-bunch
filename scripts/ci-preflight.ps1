[CmdletBinding()]
param([switch]$Diagnostics)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

Push-Location -LiteralPath $repoRoot
try {
    $arguments = @('-3', 'tools\run.py', 'ci', '--check')
    if ($Diagnostics) { $arguments += '--diagnostics' }
    & py @arguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
