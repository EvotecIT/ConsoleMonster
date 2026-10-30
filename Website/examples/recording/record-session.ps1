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
        ScriptPath = (Join-Path $PSScriptRoot 'demo.ps1')
        Path = $cast
        Width = 100
        Height = 28
        Seed = 42
        IdleLimitSeconds = 0
        Redact = ([regex]::Escape([Environment]::MachineName)), ([regex]::Escape($OutputDirectory))
    }
    Show-ConsolePresentation @presentationParameters
    Export-ConsoleRecording -RecordingPath $cast -Path (Join-Path $OutputDirectory 'session.html')
    Export-ConsoleRecording -RecordingPath $cast -Path (Join-Path $OutputDirectory 'session.svg')
    $recordingParameters = @{
        RecordingPath = $cast
        Format = 'Gif'
        Preset = $Preset
        ChromeStyle = 'MacOS'
        Path = (Join-Path $OutputDirectory 'session.gif')
    }
    Export-ConsoleRecording @recordingParameters
    $recordingParameters = @{
        RecordingPath = $cast
        Format = 'Png'
        Preset = $Preset
        PerChapter = $true
        ChromeStyle = 'MacOS'
        Path = (Join-Path $OutputDirectory 'slides')
    }
    Export-ConsoleRecording @recordingParameters
    $recordingParameters = @{
        RecordingPath = $cast
        FinalFrame = $true
        Theme = 'Light'
        ChromeStyle = 'MacOS'
        Path = (Join-Path $OutputDirectory 'session-final-light.svg')
    }
    Export-ConsoleRecording @recordingParameters
    $recordingParameters = @{
        RecordingPath = $cast
        FinalFrame = $true
        Theme = 'Dark'
        ChromeStyle = 'MacOS'
        Path = (Join-Path $OutputDirectory 'session-final-dark.svg')
    }
    Export-ConsoleRecording @recordingParameters
} finally {
    $env:CM_DEMO_ROOT = $previousRoot
}
