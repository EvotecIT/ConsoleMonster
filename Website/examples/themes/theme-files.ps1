<#
.SYNOPSIS
Compare a built-in theme with a team file using the same report.
.DESCRIPTION
Draws illustrative status, table and log widgets twice, then leaves the team
theme active. Download the two team JSON files beside this script. An optional
export folder receives starting JSON files for all four built-in presets.
.PARAMETER BaseTheme
Dark or Light, matching the background of your terminal.
.PARAMETER Path
A JSON or PSD1 theme to apply. Defaults to team-dark.json or team-light.json
beside this script, according to BaseTheme.
.PARAMETER Width
The report width in terminal columns. Use 120 or 60 to compare layouts.
.PARAMETER ExportDirectory
Optional folder to create for Dark, Light, HighContrast and ColorBlindSafe JSON
exports. Existing files with those preset names are replaced.
.EXAMPLE
./theme-files.ps1 -BaseTheme Dark -Width 120
.EXAMPLE
./theme-files.ps1 -BaseTheme Light -Width 60 -ExportDirectory ./presets
.OUTPUTS
Draws the report to the host; returns no pipeline objects.
#>
[CmdletBinding()]
param(
    [ValidateSet('Dark', 'Light')]
    [string] $BaseTheme = 'Dark',

    [string] $Path,

    [ValidateRange(40, 240)]
    [int] $Width = 120,

    [string] $ExportDirectory
)

Import-Module ConsoleMonster -ErrorAction Stop

if (-not $Path) {
    $Path = Join-Path $PSScriptRoot ('team-' + $BaseTheme.ToLowerInvariant() + '.json')
}

if ($ExportDirectory) {
    $null = New-Item -ItemType Directory -Path $ExportDirectory -Force
    foreach ($preset in 'Dark', 'Light', 'HighContrast', 'ColorBlindSafe') {
        Set-ConsoleTheme -Name $preset
        Export-ConsoleTheme -Path (Join-Path $ExportDirectory ($preset + '.json'))
    }
}

$targets = @(
    [pscustomobject]@{ Target = 'api-east'; Status = 'Pass'; Detail = 'Ready for rollout' }
    [pscustomobject]@{ Target = 'api-west'; Status = 'Fail'; Detail = 'Health probe timed out' }
    [pscustomobject]@{ Target = 'worker-01'; Status = 'Warn'; Detail = 'Restart scheduled' }
)

$themeReport = New-ConsoleRows -Gap 1 {
    New-ConsoleText -Markup '[accent]Build queue[/] [muted]sample data[/]'
    New-ConsolePanel -Title 'Checks' {
        New-ConsoleStatusList {
            New-ConsoleStatusItem 'Restore packages' -Status Pass -Detail 'complete'
            New-ConsoleStatusItem 'Run smoke checks' -Status Warn -Detail '1 retry'
            New-ConsoleStatusItem 'Deploy api-west' -Status Fail -Detail 'blocked'
            New-ConsoleStatusItem 'Publish report' -Status Pending -Detail 'queued'
        }
    }
    $targets | New-ConsoleTable -Property Target, Status, Detail -StatusProperty Status -Title 'Targets' -Expand
    New-ConsolePanel -Title 'Recent log' {
        New-ConsoleLog -Height 3 {
            New-ConsoleLogEntry 'Loaded deployment plan' -Level Info -At 1s
            New-ConsoleLogEntry 'Smoke check needed one retry' -Level Warning -At 3s
            New-ConsoleLogEntry 'api-west health probe timed out' -Level Error -At 5s
        }
    }
}

Set-ConsoleTheme -Name $BaseTheme
New-ConsoleRule -Title ('Before: built-in ' + $BaseTheme) | Write-ConsoleWidget -Width $Width
$themeReport | Write-ConsoleWidget -Width $Width

Set-ConsoleTheme -Path $Path
New-ConsoleRule -Title ('After: team ' + $BaseTheme.ToLowerInvariant()) | Write-ConsoleWidget -Width $Width
$themeReport | Write-ConsoleWidget -Width $Width
