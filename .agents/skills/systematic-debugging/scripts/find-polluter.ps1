#!/usr/bin/env pwsh
param(
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$RemainingArgs
)

$ErrorActionPreference = 'Stop'

if ($RemainingArgs.Count -ne 2) {
  Write-Output "Usage: $PSCommandPath <file_to_check> <test_pattern>"
  Write-Output "Example: $PSCommandPath '.git' 'src/**/*.test.ts'"
  exit 1
}

$pollutionCheck = $RemainingArgs[0]
$testPattern = $RemainingArgs[1]

Write-Output "Searching for test that creates: $pollutionCheck"
Write-Output "Test pattern: $testPattern"
Write-Output ""

$repoRoot = (Get-Location).Path.TrimEnd('\', '/')
$testFiles = Get-ChildItem -Path . -Recurse -File -ErrorAction SilentlyContinue |
  ForEach-Object {
    $relative = $_.FullName.Substring($repoRoot.Length).TrimStart('\', '/')
    $relative = $relative -replace '\\', '/'
    if ($relative -like $testPattern -or "./$relative" -like $testPattern) {
      $relative
    }
  } |
  Sort-Object

$total = @($testFiles).Count
Write-Output "Found $total test files"
Write-Output ""

$count = 0
foreach ($testFile in $testFiles) {
  $count++

  if (Test-Path -LiteralPath $pollutionCheck) {
    Write-Output "Pollution already exists before test $count/$total"
    Write-Output "Skipping: $testFile"
    continue
  }

  Write-Output "[$count/$total] Testing: $testFile"

  & npm test $testFile *> $null

  if (Test-Path -LiteralPath $pollutionCheck) {
    Write-Output ""
    Write-Output "FOUND POLLUTER!"
    Write-Output "Test: $testFile"
    Write-Output "Created: $pollutionCheck"
    Write-Output ""
    Write-Output "Pollution details:"
    Get-Item -Force -LiteralPath $pollutionCheck | Format-Table Mode,Length,FullName -AutoSize
    Write-Output ""
    Write-Output "To investigate:"
    Write-Output "  npm test $testFile    # Run just this test"
    Write-Output "  Get-Content $testFile # Review test code"
    exit 1
  }
}

Write-Output ""
Write-Output "No polluter found - all tests clean!"
exit 0
