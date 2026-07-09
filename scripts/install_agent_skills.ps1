<#
.SYNOPSIS
  Install/refresh skills in .agents/skills from the agent-asset-marketplace submodule.

.DESCRIPTION
  This is a thin wrapper around install_agent_skills.py that provides PowerShell
  convenience for calling the Python implementation. The Python script handles
  content comparison, provenance tracking, and collision handling.

.PARAMETER Check
  Check mode: report what would change without making changes (CI validation).

.PARAMETER Force
  Force re-copy all skill directories even when provenance matches.

.EXAMPLE
  .\scripts\install_agent_skills.ps1
  .\scripts\install_agent_skills.ps1 -Check
  .\scripts\install_agent_skills.ps1 -Force
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$PythonScript = Join-Path $ScriptDir 'install_agent_skills.py'

if (-not (Test-Path $PythonScript)) {
    throw "Python script not found at $PythonScript"
}

# Build arguments
$arguments = @('py', '-3', $PythonScript)

if ($Check) {
    $arguments += '--check'
}

if ($Force) {
    $arguments += '--force'
}

# Call the Python script
$process = Start-Process -FilePath 'py' -ArgumentList $arguments[1..($arguments.Length - 1)] -Wait -PassThru

# Exit with the Python script's exit code
exit $process.ExitCode
