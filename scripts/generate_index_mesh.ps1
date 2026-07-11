<#
.SYNOPSIS
  Generate or validate the repo-wide INDEX.md mesh.

.DESCRIPTION
  This is a thin wrapper around generate_index_mesh.py that provides PowerShell
  convenience for calling the Python script. The Python script handles the
  actual index mesh generation and validation.

.PARAMETER Check
  Check mode: validate the index mesh without making changes (CI validation).

.EXAMPLE
  .\scripts\generate_index_mesh.ps1
  .\scripts\generate_index_mesh.ps1 -Check
#>
[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$PythonScript = Join-Path $ScriptDir 'generate_index_mesh.py'

if (-not (Test-Path $PythonScript)) {
    throw "Python script not found at $PythonScript"
}

# Find available Python launcher
$pythonLaunchers = @('py', 'python', 'python3')
$pythonLauncher = $null

foreach ($launcher in $pythonLaunchers) {
    try {
        $null = Get-Command $launcher -ErrorAction Stop
        $pythonLauncher = $launcher
        break
    } catch {
        # Try next launcher
    }
}

if (-not $pythonLauncher) {
    throw "No Python launcher found. Tried: $($pythonLaunchers -join ', '). Please install Python 3.12+ and ensure it's in your PATH."
}

# Ensure pathspec is available (needed for .gitignore parsing in generate_index_mesh.py)
$pathspecCheck = & $pythonLauncher -c "import pathspec" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "pathspec not found; installing from $ScriptDir/requirements.txt"
    & $pythonLauncher -m pip install -r (Join-Path $ScriptDir 'requirements.txt')
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install pathspec from requirements.txt"
    }
}

# Build arguments
$arguments = @($pythonLauncher, $PythonScript)

if ($Check) {
    $arguments += '--check'
}

# Call the Python script
$process = Start-Process -FilePath $pythonLauncher -ArgumentList $arguments[1..($arguments.Length - 1)] -Wait -PassThru -NoNewWindow

# Exit with the Python script's exit code
exit $process.ExitCode
