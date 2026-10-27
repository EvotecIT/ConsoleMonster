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
    $presentationParameters = @{
        Path = (Join-Path $PSScriptRoot 'demo.ps1')
        Destination = $cast
        Width = 100
        Height = 28
        Seed = 42
        IdleLimitSeconds = 0
        Redact = ([regex]::Escape([Environment]::MachineName)), ([regex]::Escape($OutputDirectory))
    }
    Invoke-ConsolePresentation @presentationParameters
    Export-ConsoleRecording -Path $cast -Destination (Join-Path $OutputDirectory 'session.html')
    Export-ConsoleRecording -Path $cast -Destination (Join-Path $OutputDirectory 'session.svg')
    $recordingParameters = @{
        Path = $cast
        Format = 'Gif'
        Preset = $Preset
        WindowChrome = $true
        Destination = (Join-Path $OutputDirectory 'session.gif')
    }
    Export-ConsoleRecording @recordingParameters
    $recordingParameters = @{
        Path = $cast
        Format = 'Png'
        Preset = $Preset
        PerChapter = $true
        WindowChrome = $true
        Destination = (Join-Path $OutputDirectory 'slides')
    }
    Export-ConsoleRecording @recordingParameters
    $recordingParameters = @{
        Path = $cast
        FinalFrame = $true
        Theme = 'Light'
        WindowChrome = $true
        Destination = (Join-Path $OutputDirectory 'session-final-light.svg')
    }
    Export-ConsoleRecording @recordingParameters
    $recordingParameters = @{
        Path = $cast
        FinalFrame = $true
        Theme = 'Dark'
        WindowChrome = $true
        Destination = (Join-Path $OutputDirectory 'session-final-dark.svg')
    }
    Export-ConsoleRecording @recordingParameters
} finally {
    $env:CM_DEMO_ROOT = $previousRoot
}
