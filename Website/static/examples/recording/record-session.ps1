#Requires -Version 7.0

param(
    [string] $OutputDirectory = (Join-Path $PWD 'session-demo'),
    [string] $ModulePath = 'ConsoleMonster',
    [ValidateSet('Docs', 'Slide16x9', 'Slide4x3', 'Social')]
    [string] $Preset = 'Slide16x9'
)

$ErrorActionPreference = 'Stop'
Import-Module $ModulePath
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$OutputDirectory = (Resolve-Path $OutputDirectory).Path
$previousRoot = $env:CM_DEMO_ROOT
try {
    # Every run gets a fresh synthetic repository. No machine data is shown.
    $env:CM_DEMO_ROOT = Join-Path $OutputDirectory ('fixture-' + [guid]::NewGuid().ToString('N') + '/demo')
    $cast = Join-Path $OutputDirectory 'session.cast'
    Invoke-ConsolePresentation -Path (Join-Path $PSScriptRoot 'demo.ps1') -Destination $cast -Width 100 -Height 28 -Seed 42 -IdleLimitSeconds 0 `
        -Redact ([regex]::Escape([Environment]::MachineName)), ([regex]::Escape($OutputDirectory))
    Export-ConsoleRecording -Path $cast -Destination (Join-Path $OutputDirectory 'session.html')
    Export-ConsoleRecording -Path $cast -Destination (Join-Path $OutputDirectory 'session.svg')
    Export-ConsoleRecording -Path $cast -Format Gif -Preset $Preset -WindowChrome -Destination (Join-Path $OutputDirectory 'session.gif')
    Export-ConsoleRecording -Path $cast -Format Png -Preset $Preset -PerChapter -WindowChrome -Destination (Join-Path $OutputDirectory 'slides')
    Export-ConsoleRecording -Path $cast -FinalFrame -Theme Light -WindowChrome -Destination (Join-Path $OutputDirectory 'session-final-light.svg')
    Export-ConsoleRecording -Path $cast -FinalFrame -Theme Dark -WindowChrome -Destination (Join-Path $OutputDirectory 'session-final-dark.svg')
} finally {
    $env:CM_DEMO_ROOT = $previousRoot
}
