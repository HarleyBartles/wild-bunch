$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& py -3 "$scriptDir\new_worktree.py" @args
