[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Name,
    [Parameter(Mandatory = $true)][ValidateSet('local', 'marketplace')][string]$Custody,
    [Parameter(Mandatory = $true)][ValidateSet('first_party', 'skills-with-source', 'skills-with-citation')][string]$Lane,
    [switch]$Check,
    [switch]$AllowSharedCheckout
)

$arguments = @('--name', $Name, '--custody', $Custody, '--lane', $Lane)
if ($Check) { $arguments += '--check' }
if ($AllowSharedCheckout) { $arguments += '--allow-shared-checkout' }

$scriptPath = Join-Path $PSScriptRoot 'new_skill.py'
& py -3 $scriptPath @arguments
exit $LASTEXITCODE
