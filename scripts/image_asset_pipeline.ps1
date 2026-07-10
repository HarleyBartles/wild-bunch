<#
.SYNOPSIS
  Cut, normalize, stage, and promote generated image assets onto fixed canvases.

.DESCRIPTION
  This is a thin wrapper around image_asset_pipeline.py that provides PowerShell
  convenience for calling the Python script. The Python script handles the actual
  image processing operations.

.PARAMETER Arguments
  All arguments are passed through to the Python script. See the Python script
  for available commands and options:
  - cut-background: Cut one image away from a flat background
  - cut-background-tree: Cut every PNG in a tree away from a flat background
  - normalize: Normalize an image onto a fixed canvas
  - slice-sheet: Slice a turnaround sheet into individual views
  - stage-tiles: Cut tile art to transparent cutouts
  - promote-tiles: Copy staged tile PNGs into the matching sprites tree
  - promote-sprites: Cut and normalize building tree into final sprites

.EXAMPLE
  .\scripts\image_asset_pipeline.ps1 cut-background --input source.png --out cut.png
  .\scripts\image_asset_pipeline.ps1 normalize --input source.png --out normalized.png
  .\scripts\image_asset_pipeline.ps1 slice-sheet --input sheet.png --out-dir out-dir --names front,profile,rear
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$PythonScript = Join-Path $ScriptDir 'image_asset_pipeline.py'

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
    throw "No Python launcher found. Tried: $($pythonLaunchers -join ', '). Please install Python 3.11+ and ensure it's in your PATH."
}

# Build arguments
$pythonArgs = @($pythonLauncher, $PythonScript)
if ($Arguments) {
    $pythonArgs += $Arguments
}

# Call the Python script
$process = Start-Process -FilePath $pythonLauncher -ArgumentList $pythonArgs[1..($pythonArgs.Length - 1)] -Wait -PassThru

# Exit with the Python script's exit code
exit $process.ExitCode
