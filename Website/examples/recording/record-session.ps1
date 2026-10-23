param(
    [string] $OutputDirectory = (Join-Path $PWD 'session-demo'),
    [string] $ModulePath = 'ConsoleMonster'
)

$ErrorActionPreference = 'Stop'
Import-Module $ModulePath
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$OutputDirectory = (Resolve-Path $OutputDirectory).Path
$previousRoot = $env:CM_DEMO_ROOT
$previousDriver = $env:CM_DEMO_DRIVER
try {
    $env:CM_DEMO_ROOT = $OutputDirectory
    $demo = Join-Path $OutputDirectory 'demo'
    $null = New-Item -ItemType Directory -Path $demo
    git -C $demo init --quiet --initial-branch main
    foreach ($subject in @('Start sample app', 'Add synthetic fixtures', 'Render report table', 'Track build progress', 'Document local recording')) {
        Add-Content (Join-Path $demo 'history.txt') $subject
        git -C $demo add history.txt
        git -C $demo -c user.name='Demo' -c user.email='demo@example.invalid' -c commit.gpgsign=false commit --quiet -m $subject
        if ($LASTEXITCODE) { throw 'Cannot prepare the synthetic Git repository.' }
    }
    $env:CM_DEMO_DRIVER = Join-Path $OutputDirectory 'TypedSession.dll'
    Add-Type -Path (Join-Path $PSScriptRoot 'TypedSession.cs') -OutputAssembly $env:CM_DEMO_DRIVER
    Invoke-ConsoleRecording {
        [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
        $demo = Join-Path $env:CM_DEMO_ROOT 'demo'
        $module = (Get-Module ConsoleMonster).Path.Replace("'", "''")
        $directory = $demo.Replace("'", "''")
        $setup = @'
$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
Import-Module '__MODULE__'
Remove-Module PSReadLine -ErrorAction SilentlyContinue
Set-Location '__DEMO__'
function prompt { 'PS C:\demo> ' }
$env:GIT_PAGER = 'cat'
$env:API_TOKEN = 'demo-session-only-value'
if ($PSVersionTable.PSVersion.Major -ge 7) {
    $PSStyle.OutputRendering = 'Ansi'
    $PSStyle.Progress.View = 'Classic'
}
function Show-DemoReport {
    Write-Host 'Build report' -ForegroundColor Cyan
    $check = ([char] 0x2713).ToString()
    @(
        [pscustomobject]@{ Check = 'Compile'; Result = 'Passed'; Duration = '1.8 s' }
        [pscustomobject]@{ Check = 'Unit tests'; Result = "$check 24 passed"; Duration = '2.4 s' }
        [pscustomobject]@{ Check = 'Package'; Result = 'Ready'; Duration = '0.6 s' }
    ) | Format-ConsoleTable -Width 70
}
function Invoke-DemoBuild {
    Use-ConsoleProgress {
        foreach ($step in 1..20) {
            Write-Progress -Activity 'Building sample app' -Status "Step $step of 20" -PercentComplete ($step * 5)
            Start-Sleep -Milliseconds 250
        }
        Write-Progress -Activity 'Building sample app' -Completed
    }
    Write-Host 'Build succeeded. 24 tests passed.' -ForegroundColor Green
}
function Show-DemoToken {
    param([string] $Value)
    Write-Host ("API_TOKEN={0} (redacted)" -f $Value) -ForegroundColor DarkGray
}
Clear-Host
'@
        $setup = $setup.Replace('__MODULE__', $module).Replace('__DEMO__', $directory)
        $null = [Reflection.Assembly]::LoadFrom($env:CM_DEMO_DRIVER)
        $options = [ConsoleMonster.Sessions.SessionRecordingOptions]::new()
        $options.Path = Join-Path $env:CM_DEMO_ROOT 'typing.cast'
        $options.Shell = (Get-Process -Id $PID).Path
        $options.WorkingDirectory = $demo
        $options.Width = 88; $options.Height = 28
        $options.Redact.Add('demo-session-only-value')
        $options.Environment['CONSOLEMONSTER_MODE'] = 'interactive'
        $options.Environment['CONSOLEMONSTER_COLOR'] = 'truecolor'
        $options.Environment['CONSOLEMONSTER_GLYPHS'] = 'full'
        $options.Environment['TERM'] = 'xterm-256color'
        $options.Environment['CI'] = $null
        $options.Environment['NO_COLOR'] = $null
        foreach ($argument in @('-NoLogo', '-NoProfile', '-NoExit', '-EncodedCommand', [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($setup)))) {
            $options.Arguments.Add($argument)
        }
        $relay = [Delegate]::CreateDelegate([Action[string]], [TypedSession].GetMethod('Output'))
        $terminal = [ConsoleMonster.Sessions.SessionRecorder]::Start($options, $relay)
        try {
            [TypedSession]::WaitForPrompt()
            $commands = @('git log --oneline --graph --color -5', 'Invoke-DemoBuild', 'Show-DemoReport', 'Get-Item C:\demo\missing.json', 'Show-DemoToken $env:API_TOKEN')
            $chapters = @('Inspect', 'Build', 'Report', '', '')
            for ($index = 0; $index -lt $commands.Count; $index++) {
                if ($chapters[$index]) { Add-ConsoleRecordingMarker $chapters[$index] }
                foreach ($key in $commands[$index].ToCharArray()) {
                    $terminal.SendInput([string] $key)
                    Start-Sleep -Milliseconds 45
                }
                $terminal.SendInput("`r")
                [TypedSession]::WaitForPrompt()
                Start-Sleep -Milliseconds 1200
            }
            Start-Sleep -Milliseconds 2000
            $terminal.SendInput("exit`r")
            $terminal.WaitForExit()
        } finally { $terminal.Dispose() }

    } -Destination (Join-Path $OutputDirectory 'session.html') -Width 88 -Height 28 `
        -Name ('Inspect {0} Build {0} Report' -f [char] 0xB7) -IdleLimit 0 -RedactionText (([char] 0x2022).ToString() * 7) `
        -Redact 'demo-session-only-value', ([regex]::Escape([Environment]::MachineName))
} finally {
    $env:CM_DEMO_ROOT = $previousRoot
    $env:CM_DEMO_DRIVER = $previousDriver
}

$cast = Join-Path $OutputDirectory 'session.cast'
Export-ConsoleRecording -Path $cast -Destination (Join-Path $OutputDirectory 'session.svg')
Export-ConsoleRecording -Path $cast -FinalFrame -Theme Light -WindowChrome -Destination (Join-Path $OutputDirectory 'session-final-light.svg')
Export-ConsoleRecording -Path $cast -FinalFrame -Theme Dark -WindowChrome -Destination (Join-Path $OutputDirectory 'session-final-dark.svg')
