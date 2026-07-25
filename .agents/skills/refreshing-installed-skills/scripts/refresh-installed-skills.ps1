$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& py -3 "$scriptDir\refresh_installed_skills.py" @args
