#Requires -Version 7.0

param(
    [string] $OutputDirectory = (Join-Path $PWD 'session-demo'),
    [string] $ModulePath = 'ConsoleMonster'
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
    Invoke-ConsoleDemo -Path (Join-Path $PSScriptRoot 'demo.ps1') -Destination $cast -Width 100 -Height 28 -Seed 42 -IdleLimit 0 `
        -Redact ([regex]::Escape([Environment]::MachineName)), ([regex]::Escape($OutputDirectory))
    Export-ConsoleRecording -Path $cast -Destination (Join-Path $OutputDirectory 'session.html')
    Export-ConsoleRecording -Path $cast -Destination (Join-Path $OutputDirectory 'session.svg')
    Export-ConsoleRecording -Path $cast -Format Gif -Preset Docs -WindowChrome -Destination (Join-Path $OutputDirectory 'session.gif')
    Export-ConsoleRecording -Path $cast -Format Png -Preset Slide16x9 -PerChapter -WindowChrome -Destination (Join-Path $OutputDirectory 'slides')
    if (Get-Command ffmpeg -CommandType Application -ErrorAction Ignore) {
        Export-ConsoleRecording -Path $cast -Format Mp4 -Preset Slide16x9 -WindowChrome -Destination (Join-Path $OutputDirectory 'session.mp4')
    }
    Export-ConsoleRecording -Path $cast -FinalFrame -Theme Light -WindowChrome -Destination (Join-Path $OutputDirectory 'session-final-light.svg')
    Export-ConsoleRecording -Path $cast -FinalFrame -Theme Dark -WindowChrome -Destination (Join-Path $OutputDirectory 'session-final-dark.svg')
} finally {
    $env:CM_DEMO_ROOT = $previousRoot
}
