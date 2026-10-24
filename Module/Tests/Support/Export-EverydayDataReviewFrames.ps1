<#
.SYNOPSIS
Saves native and enabled default-output ANSI/text captures for everyday data views on a Unix terminal.
.DESCRIPTION
Runs the deterministic view samples through PowerShell's real Out-Default before and after enabling Output.
The child process uses the existing terminal test harness. Windows-only views require native Windows validation.
.PARAMETER OutputDirectory
Directory in which to retain the synthetic review captures.
.PARAMETER Width
Terminal width in cells (default 120).
.EXAMPLE
./Export-EverydayDataReviewFrames.ps1 -OutputDirectory ./everyday-review
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $OutputDirectory,
    [ValidateRange(20, 240)][int] $Width = 120
)

$ErrorActionPreference = 'Stop'
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$modulePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../ConsoleMonster.psd1'))
$samplePath = Join-Path $PSScriptRoot 'Get-EverydayViewSample.ps1'
$views = @('json-flat', 'json-nested', 'json-envelope', 'json-envelopes', 'json-budgets', 'json-records', 'json-empty-records', 'json-large', 'csv', 'csv-wide', 'command', 'command-grouped', 'module', 'history', 'ping', 'ip', 'dns-host')
$source = @'
$ErrorActionPreference = 'Stop'
[Threading.Thread]::CurrentThread.CurrentCulture = [cultureinfo]'en-US'
[Threading.Thread]::CurrentThread.CurrentUICulture = [cultureinfo]'en-US'
[Threading.Thread]::CurrentThread.CurrentUICulture = [cultureinfo]'en-US'
Import-Module '__MODULE__'
. '__SAMPLE__'
foreach ($view in '__VIEWS__' -split ',') {
    Write-Host "### $view before ###"
    Get-EverydayViewSample $view | Microsoft.PowerShell.Core\Out-Default
    Enable-ConsoleFormatting -Feature Output
    Write-Host "### $view after ###"
    Get-EverydayViewSample $view | Out-Default
    Disable-ConsoleFormatting -Feature Output
}
'@
$source = $source.Replace('__MODULE__', $modulePath.Replace("'", "''"))
$source = $source.Replace('__SAMPLE__', $samplePath.Replace("'", "''"))
$source = $source.Replace('__VIEWS__', ($views -join ','))
$scriptPath = Join-Path $OutputDirectory 'capture.ps1'
$ansiPath = Join-Path $OutputDirectory 'all-views.ansi'
[IO.File]::WriteAllText($scriptPath, $source, [Text.UTF8Encoding]::new($true))
& python3 (Join-Path $PSScriptRoot 'formatting_pty.py') ([Diagnostics.Process]::GetCurrentProcess().Path) $scriptPath $ansiPath 120 $Width
if ($LASTEXITCODE -ne 0) { throw "Terminal capture failed with exit code $LASTEXITCODE." }
$ansi = [IO.File]::ReadAllText($ansiPath)
$matches = [regex]::Matches($ansi, '### ([a-z-]+) (before|after) ###')
for ($i = 0; $i -lt $matches.Count; $i++) {
    $match = $matches[$i]
    $start = $match.Index + $match.Length
    $end = if ($i + 1 -lt $matches.Count) { $matches[$i + 1].Index } else { $ansi.Length }
    $frame = $ansi.Substring($start, $end - $start).TrimStart("`r", "`n")
    $name = $match.Groups[1].Value + '-' + $match.Groups[2].Value
    [IO.File]::WriteAllText((Join-Path $OutputDirectory "$name.ansi"), $frame)
    $plain = $frame -replace '\x1b\][^\x07\x1b]*(?:\x07|\x1b\\)', '' -replace '\x1b\[[0-?]*[ -/]*[@-~]', ''
    [IO.File]::WriteAllText((Join-Path $OutputDirectory "$name.txt"), $plain.Replace("`r", '').Trim() + "`n")
}
if ($matches.Count -ne 2 * $views.Count) { throw 'One or more view captures are missing.' }
Write-Host "Saved $($views.Count) before/after pairs at $OutputDirectory"
