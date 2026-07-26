[CmdletBinding()]
param(
    [switch]$Check,
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$SkillsRoot,
    [Parameter(Mandatory = $true, Position = 1, ValueFromRemainingArguments = $true)]
    [string[]]$Prefixes
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$PythonScript = Join-Path $ScriptDir 'validate_local_skills_extra.py'

$args = @()
if ($Check) { $args += '--check' }
$args += $SkillsRoot
$args += $Prefixes

& py -3 $PythonScript @args
