[CmdletBinding()]
param(
    [switch]$Check,
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$PythonScript = Join-Path $ScriptDir 'generate_index_mesh_extra.py'

$args = @()
if ($Check) { $args += '--check' }
$args += $RepoRoot

& py -3 $PythonScript @args
