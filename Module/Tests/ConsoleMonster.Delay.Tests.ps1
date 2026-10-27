Describe 'Dashboard delays and recording checkpoints' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:DelayModule = (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1').Replace("'", "''")
        $script:DelayRuntime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        function Invoke-DelayChild {
            param([Parameter(Mandatory)][string] $Script)
            $setup = "`$ErrorActionPreference = 'Stop'`nImport-Module '$script:DelayModule' -Force -ErrorAction Stop`n"
            $output = Invoke-TestChild -Runtime $script:DelayRuntime -Script ($setup + $Script) -TimeoutSeconds 20
            if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
            $output -join "`n"
        }
    }

    It 'takes the pause in seconds, as Start-Sleep does' {
        Import-Module (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1') -Force
        $parameter = (Get-Command Wait-ConsoleDashboard).Parameters['Seconds']
        $parameter.ParameterType | Should -Be ([double])
        (Get-Command Wait-ConsoleDashboard).Parameters.ContainsKey('Milliseconds') | Should -BeFalse
        Invoke-DelayChild -Script @'
$dashboard = New-ConsoleDashboard -Title 'Pause' -Content { New-ConsoleProgress -WidgetId overall -Total 1 }
$watch = [System.Diagnostics.Stopwatch]::StartNew()
Invoke-ConsoleDashboard -Dashboard $dashboard -NoLive -ScriptBlock { Wait-ConsoleDashboard 0.2 } | Out-Null
if ($watch.Elapsed.TotalMilliseconds -lt 150) { throw 'The fractional pause did not wait.' }
'PAUSED-OK'
'@ | Should -Match 'PAUSED-OK'
    }

    It 'reports a missing dashboard and rejects negative seconds' {
        Invoke-DelayChild -Script @'
try {
    Wait-ConsoleDashboard -Seconds 0 -ErrorAction Stop
    throw 'The missing session was accepted.'
} catch {
    if ($_.FullyQualifiedErrorId -notlike 'ConsoleMonsterNoSession*') { throw }
}
try {
    Wait-ConsoleDashboard -Seconds -1 -ErrorAction Stop
    throw 'A negative delay was accepted.'
} catch {
    if ($_.FullyQualifiedErrorId -notlike 'ParameterArgumentValidationError*') { throw }
}
'validated'
'@ | Should -BeExactly 'validated'
    }

    It 'uses an explicit session without writing output and returns safely after cancellation' {
        Invoke-DelayChild -Script @'
$target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Testing.TestTerminal', $true))::Target(32, 12)
$target.SkipDelays = $true
$options = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Live.LiveSessionOptions', $true))::new()
$options.Target = $target
$options.CaptureConsole = $false
$options.HandleCancelKey = $false
$options.PrintFinalState = $false
$slot = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Widgets.RegionWidget', $true))::new('body')
$session = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Live.LiveSession', $true))::new($slot, $options)
try {
    $session.Start()
    $session.Region('body').Set(([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Widgets.TextBlock', $true))::Plain('explicit'))
    if (@(Wait-ConsoleDashboard -Seconds 10 -Session $session).Count) { throw 'The wait wrote pipeline output.' }
    if ($target.Screens.Frames.Count -ne 1 -or $target.Screens.LastFrame.ToPlainText() -ne 'explicit') { throw 'The explicit session was not captured.' }
    $session.Cancel()
    if (@(Wait-ConsoleDashboard -Seconds 0 -Session $session).Count) { throw 'Cancellation wrote pipeline output.' }
    if ($target.CheckpointCount -ne 1) { throw 'Cancellation captured another checkpoint.' }
} finally { $session.Dispose() }
'explicit'
'@ | Should -BeExactly 'explicit'
    }

    It 'captures ordered updates from a dashboard child runspace using its session target' {
        Invoke-DelayChild -Script @'
$target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Testing.TestTerminal', $true))::Target(32, 12)
$target.SkipDelays = $true
$previous = [ConsoleMonster.Terminal]::Target
$scope = [ConsoleMonster.Terminal]::Use($target)
try {
    $slot = New-ConsoleRegion -WidgetId body
    $result = @(Invoke-ConsoleDashboard $slot -Transient {
        Update-Console -WidgetId body -Content { New-ConsoleText 'first' }
        Wait-ConsoleDashboard -Seconds 10
        Update-Console -WidgetId body -Content { New-ConsoleText 'second' }
        Wait-ConsoleDashboard -Seconds 0
    })
    if ($result.Count) { throw 'The child wait wrote pipeline output.' }
} finally { $scope.Dispose() }
if (-not [object]::ReferenceEquals($previous, [ConsoleMonster.Terminal]::Target)) { throw 'The target scope leaked.' }
if ($target.CheckpointCount -ne 2 -or $target.Screens.Frames.Count -ne 2) { throw 'Expected exactly two checkpoints.' }
if ($target.Screens.Frames[0].ToPlainText() -ne 'first' -or $target.Screens.Frames[1].ToPlainText() -ne 'second') { throw 'Checkpoint order or data changed.' }
'ordered'
'@ | Should -BeExactly 'ordered'
    }

    It 'preserves an explicit event time in the live log and its checkpoint' {
        Invoke-DelayChild -Script @'
$target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Testing.TestTerminal', $true))::Target(48, 12)
$target.SkipDelays = $true
$scope = [ConsoleMonster.Terminal]::Use($target)
try {
    $log = New-ConsoleLogRegion -WidgetId log -Height 1
    Invoke-ConsoleDashboard $log -Transient {
        Send-ConsoleEvent -WidgetId log -Operation write -Text 'source timestamp' -Status info -TimeMilliseconds 123000
        Wait-ConsoleDashboard -Seconds 0
    }
} finally { $scope.Dispose() }
$entries = @($log.Entries | Where-Object Text -EQ 'source timestamp')
if ($entries.Count -ne 1 -or $entries[0].Time.TotalMilliseconds -ne 123000) { throw 'The explicit event time was lost.' }
if ($target.Screens.Frames.Count -ne 1) { throw 'Expected one log checkpoint.' }
$text = $target.Screens.LastFrame.ToPlainText()
if (-not $text.Contains('02:03') -or -not $text.Contains('source timestamp')) { throw "The checkpoint lost its timestamp: $text" }
'timestamp'
'@ | Should -BeExactly 'timestamp'
    }
}
