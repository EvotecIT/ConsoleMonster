param(
    [string] $OutputDirectory = (Join-Path $PWD 'session-demo'),
    [string] $ModulePath = 'ConsoleMonster'
)

$ErrorActionPreference = 'Stop'
Import-Module $ModulePath
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$destination = Join-Path $OutputDirectory 'session.html'

Invoke-ConsoleRecording {
    $escape = [char] 27
    [Console]::WriteLine("${escape}[1;36mCONSOLE SESSION${escape}[0m")
    [Console]::WriteLine('Native commands, progress and chapters')
    [Console]::WriteLine('')

    Add-ConsoleRecordingMarker 'Inspect'
    [Console]::WriteLine("${escape}[36m> git --version${escape}[0m")
    git --version
    [Console]::WriteLine("${escape}[36m> dotnet --version${escape}[0m")
    dotnet --version
    [Console]::WriteLine('')

    Add-ConsoleRecordingMarker 'Build'
    foreach ($step in 1..8) {
        $bar = ('#' * $step).PadRight(8, '-')
        [Console]::Write("`r${escape}[33mBuild [$bar] $step/8${escape}[0m")
        Start-Sleep -Milliseconds 140
    }
    [Console]::WriteLine('')
    [Console]::WriteLine('')

    Add-ConsoleRecordingMarker 'Review'
    [Console]::WriteLine('api_key=demo-only-secret')
    [Console]::WriteLine("${escape}[32mBuild finished. Recording stays local.${escape}[0m")
} -Destination $destination -Width 60 -Height 16 -Name 'Console session' `
    -IdleLimit 1 -Redact ([regex]::Escape([Environment]::MachineName))

$cast = Join-Path $OutputDirectory 'session.cast'
Export-ConsoleRecording -Path $cast -Destination (Join-Path $OutputDirectory 'session.svg')
Export-ConsoleRecording -Path $cast -FinalFrame -Theme Light -WindowChrome -Destination (Join-Path $OutputDirectory 'session-final-light.svg')
Export-ConsoleRecording -Path $cast -FinalFrame -Theme Dark -WindowChrome -Destination (Join-Path $OutputDirectory 'session-final-dark.svg')
