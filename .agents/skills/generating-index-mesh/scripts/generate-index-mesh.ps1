$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& py -3 "$scriptDir\generate_index_mesh.py" @args
