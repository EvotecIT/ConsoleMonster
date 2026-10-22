<#
.SYNOPSIS
Checks a tail command's exact output mode in a fresh hidden native PowerShell console.
.DESCRIPTION
The caller launches this fixture through ConsoleMonster.TestHost host (CREATE_NEW_CONSOLE and SW_HIDE).
Only a synthetic file is read. Each scenario runs in a fresh process so capability detection has not been cached.
.PARAMETER ResultPath
The JSON receipt path, outside the repository.
.PARAMETER Scenario
PlainCount, LiveCount or SourceConstructionFailure.
.PARAMETER ModulePath
The module manifest or binary to import; defaults to the source development manifest.
.EXAMPLE
ConsoleMonster.TestHost.exe host powershell.exe -NoProfile -File ./Assert-TailModeHost.ps1 -ResultPath ./mode.json -Scenario PlainCount
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $ResultPath,
    [Parameter(Mandatory)][ValidateSet('PlainCount', 'LiveCount', 'SourceConstructionFailure')][string] $Scenario,
    [string] $ModulePath = (Join-Path $PSScriptRoot '../../ConsoleMonster.psd1')
)

$ErrorActionPreference = 'Stop'
$inheritedMode = $null
$fixturePath = "$ResultPath.log"
try {
    Add-Type -Path (Join-Path $PSScriptRoot 'TailModeConsole.cs')
    $inheritedMode = [ConsoleMonsterTailModeConsole]::Read()
    # Known detection inputs belong only to this owned child, without reading or logging its environment.
    foreach ($name in 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'BUILD_BUILDID', 'JENKINS_URL', 'GITLAB_CI', 'TEAMCITY_VERSION') {
        [Environment]::SetEnvironmentVariable($name, $null)
    }
    [Environment]::SetEnvironmentVariable('TERM', 'xterm-256color')
    [Environment]::SetEnvironmentVariable('CONSOLEMONSTER_MODE', 'interactive')
    [Environment]::SetEnvironmentVariable('CONSOLEMONSTER_EMOJI_WIDTH', '2')
    [Environment]::SetEnvironmentVariable('CONSOLEMONSTER_AMBIGUOUS_WIDTH', '1')
    Import-Module $ModulePath -Force
    $expected = $inheritedMode -band [uint32]4294967291
    [ConsoleMonsterTailModeConsole]::Set($expected)
    $failed = $false
    $failureId = $null
    if ($Scenario -eq 'SourceConstructionFailure') {
        try {
            Watch-ConsoleLog -Path $fixturePath -Count 1 -Plain -ErrorAction Stop
        } catch {
            $failed = $true
            $failureId = $_.FullyQualifiedErrorId
        }
        if (-not $failed -or $failureId -notlike '*FileNotFoundException*') {
            throw "Expected a missing-file source construction failure, got '$failureId'."
        }
    } else {
        [IO.File]::WriteAllText($fixturePath, "INFO literal [red]tail[/]`n", [Text.UTF8Encoding]::new($false))
        if ($Scenario -eq 'PlainCount') {
            $lines = @(Watch-ConsoleLog -Path $fixturePath -Count 1 -Plain)
            if ($lines.Count -ne 1 -or $lines[0] -cne 'INFO literal [red]tail[/]') {
                throw 'The plain native host did not return the original line.'
            }
        } else {
            Watch-ConsoleLog -Path $fixturePath -Count 1
        }
    }
    $actual = [ConsoleMonsterTailModeConsole]::Read()
    if ($actual -ne $expected) {
        throw "The command changed native output mode from $expected to $actual."
    }
    [pscustomobject]@{
        Runtime = $PSVersionTable.PSVersion.ToString()
        Scenario = $Scenario
        ModeBefore = $expected
        ModeAfter = $actual
        SourceFailed = $failed
        Status = 'Passed'
    } | ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8
} catch {
    $_ | Out-String | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    exit 1
} finally {
    if ($null -ne $inheritedMode) {
        [ConsoleMonsterTailModeConsole]::Set($inheritedMode)
    }
    if (Test-Path -LiteralPath $fixturePath) {
        Remove-Item -LiteralPath $fixturePath -Force
    }
}
