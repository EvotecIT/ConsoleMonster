<#
.SYNOPSIS
Selects the Pester files for one CI shard.
.DESCRIPTION
Shard 1 runs the examples suite, the longest single file. The remaining files are sorted by path and dealt round-robin
across the other shards, so every file runs in exactly one shard and new files are picked up without a list to maintain.
Runs on Windows PowerShell 5.1 and PowerShell 7.
#>
param(
    [Parameter(Mandatory)] [ValidateRange(1, 64)] [int] $Shard,
    [Parameter(Mandatory)] [ValidateRange(2, 64)] [int] $Count,
    [string] $Root = (Join-Path $PSScriptRoot '../../Module/Tests')
)

$ErrorActionPreference = 'Stop'
if ($Shard -gt $Count) { throw "Shard $Shard is outside 1..$Count." }
$examples = 'ConsoleMonster.Examples.Tests.ps1'
$files = @(Get-ChildItem -LiteralPath $Root -Filter '*.Tests.ps1' -File -Recurse | Sort-Object { $_.FullName.ToLowerInvariant() })
if ($Shard -eq 1) {
    $selected = @($files | Where-Object Name -EQ $examples)
} else {
    $rest = @($files | Where-Object Name -NE $examples)
    $selected = @(for ($index = 0; $index -lt $rest.Count; $index++) {
        if ($index % ($Count - 1) -eq $Shard - 2) { $rest[$index] }
    })
}
if ($selected.Count -eq 0) { throw "Pester shard $Shard/$Count selected no test files." }
Write-Host "Pester shard $Shard/$Count runs $($selected.Count) of $($files.Count) files:"
$selected | ForEach-Object { Write-Host "  $($_.Name)" }
$selected.FullName
