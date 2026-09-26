$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& py -3 "$scriptDir\remove_worktree.py" @args
exit $LASTEXITCODE
