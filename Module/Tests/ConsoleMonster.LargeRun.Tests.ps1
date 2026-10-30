Describe 'Large run progress' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:Runtime = [Diagnostics.Process]::GetCurrentProcess().Path
        $script:ModulePath = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
    }

    It 'maps property and script-block metadata, preserves pipeline results and returns a structured final snapshot' {
        $code = @'
Import-Module '__MODULE__' -Force
$items = @([pscustomobject]@{Name='Alpha';Target='host-1';Group='DNS'}, [pscustomobject]@{Name='Beta';Target='host-2';Group='DNS'}, [pscustomobject]@{Name='Gamma';Target='host-3';Group='Storage'})
$run = Start-ConsoleRun -InputObject $items -Label { $_.Name } -Target Target -Group Group -Title 'Batch checks' -NoLive -ProgressEvery 2 -ProgressIntervalSeconds 0 -ReportPath 'reports/checks.html'
try {
    Update-Console -Run $run -Index 0 -Status Running -Label 'collecting'
    Update-Console -Run $run -Index 0 -Status Pass
    Update-Console -Run $run -Index 1 -Status Warn -Detail 'Partial response [literal]'
    Update-Console -Run $run -Index 1 -Status Fail -Detail 'duplicate ignored'
    $value = 42
} finally { $summary = Complete-ConsoleRun $run -PassThru }
"COUNTS=$($summary.Passed),$($summary.Warned),$($summary.Failed),$($summary.Skipped)"
"METADATA=$($summary.Items[0].Item.Label),$($summary.Items[1].Item.Target),$($summary.Items[2].Item.Group)"
"VALUE=$value"
Complete-ConsoleRun $run
'@
        $output = (Invoke-TestChild -Runtime $script:Runtime -Script $code.Replace('__MODULE__', $script:ModulePath)) -join "`n"
        $output | Should -Match 'COUNTS=1,1,0,1'
        $output | Should -Match 'METADATA=Alpha,host-2,Storage'
        $output | Should -Match 'VALUE=42'
        $output | Should -Match 'reports/checks.html'
        $output | Should -Not -Match 'duplicate ignored'
        $output | Should -Not -Match ([char]27)
    }

    It 'rejects unsupported update state <State> before changing the run' -ForEach @(
        @{ State = 'Pending' }
        @{ State = 'Info' }
    ) {
        $code = @'
Import-Module '__MODULE__' -Force
$run = Start-ConsoleRun -InputObject @('one check') -NoLive -ProgressIntervalSeconds 0
try {
    try {
        Update-Console -Run $run -Index 0 -Status '__STATE__' -ErrorAction Stop
        throw 'The unsupported state was accepted'
    } catch {
        "STATE_ERROR=$($_.FullyQualifiedErrorId)"
    }
    "UNCHANGED=$($run.Snapshot().Done),$($run.Snapshot().Running.Count)"
    Update-Console -Run $run -Index 0 -Status Pass
    "VALID=$($run.Snapshot().Passed)"
} finally { Complete-ConsoleRun $run }
'@
        $output = (Invoke-TestChild -Runtime $script:Runtime -Script $code.Replace('__MODULE__', $script:ModulePath).Replace('__STATE__', $State)) -join "`n"
        $output | Should -Match 'STATE_ERROR=ConsoleMonsterInvalidUpdate'
        $output | Should -Match 'UNCHANGED=0,0'
        $output | Should -Match 'VALID=1'
    }

    It 'completes an offered run when its output pipeline rejects the handle' {
        $code = @'
Import-Module '__MODULE__' -Force
$script:offeredRun = $null
try {
    Start-ConsoleRun -InputObject @('unpublished') -NoLive -ErrorAction Stop | ForEach-Object {
        $script:offeredRun = $_
        throw 'Handle publication failed'
    }
} catch { "PUBLICATION_ERROR=$($_.Exception.Message)" }
if ($null -eq $script:offeredRun) { throw 'The pipeline did not receive the offered handle' }
try {
    $snapshot = $script:offeredRun.Snapshot()
    "STOPPED=$($snapshot.IsFinished),$($snapshot.Skipped)"
} finally { $script:offeredRun.Dispose() }
'@
        $output = (Invoke-TestChild -Runtime $script:Runtime -Script $code.Replace('__MODULE__', $script:ModulePath)) -join "`n"
        $output | Should -Match 'PUBLICATION_ERROR=Handle publication failed'
        $output | Should -Match 'STOPPED=True,1'
    }

    It 'preserves the initial host-output error and permits a subsequent run' {
        $code = @'
Import-Module '__MODULE__' -Force
$run = $null
try {
    $run = Start-ConsoleRun -InputObject @('unpublished') -NoLive -InformationAction Stop -ErrorAction Stop
    throw 'The host-output failure was ignored'
} catch {
    "HOST_ERROR=$($_.FullyQualifiedErrorId)"
}
"NO_HANDLE=$($null -eq $run)"
$next = Start-ConsoleRun -InputObject @('next check') -NoLive -ProgressIntervalSeconds 0
try { Update-Console -Run $next -Index 0 -Status Pass }
finally { $summary = Complete-ConsoleRun $next -PassThru }
"NEXT=$($summary.Passed),$($summary.IsFinished)"
'@
        $output = (Invoke-TestChild -Runtime $script:Runtime -Script $code.Replace('__MODULE__', $script:ModulePath)) -join "`n"
        $output | Should -Match 'HOST_ERROR=ActionPreferenceStop'
        $output | Should -Match 'NO_HANDLE=True'
        $output | Should -Match 'NEXT=1,True'
    }

    It 'accepts an empty run and completes without inventing work' {
        $code = @'
Import-Module '__MODULE__' -Force
$run = Start-ConsoleRun -InputObject @() -NoLive -ProgressIntervalSeconds 0
$summary = Complete-ConsoleRun $run -PassThru
"EMPTY=$($summary.Total),$($summary.Done),$($summary.IsFinished)"
'@
        $output = (Invoke-TestChild -Runtime $script:Runtime -Script $code.Replace('__MODULE__', $script:ModulePath)) -join "`n"
        $output | Should -Match 'EMPTY=0,0,True'
    }
    It 'receives timed host progress while a background worker is running' {
        $code = @'
Import-Module '__MODULE__' -Force
$run = Start-ConsoleRun -InputObject @('slow check') -NoLive -ProgressEvery 0 -ProgressIntervalSeconds 0.1
$worker = [powershell]::Create()
$null = $worker.AddScript('param($run) Start-Sleep -Milliseconds 800; $run.Finished(0, [ConsoleMonster.Styling.StatusKind]::Pass, "worker done")').AddArgument($run)
try {
    $pending = $worker.BeginInvoke()
    Complete-ConsoleRun -Run $run -Wait
    'WAIT_DONE'
    $null = $worker.EndInvoke($pending)
} finally {
    $worker.Dispose()
    Complete-ConsoleRun $run
}
'@
        $output = (Invoke-TestChild -Runtime $script:Runtime -Script $code.Replace('__MODULE__', $script:ModulePath)) -join "`n"
        $output | Should -Match '(?s)0/1.*?worker done.*?1 / 1 finished.*?WAIT_DONE'
        $output | Should -Not -Match ([char]27)
    }

    It 'retains deferred final host output until completion can be retried' {
        $code = @'
$ErrorActionPreference = 'Stop'
Import-Module '__MODULE__' -Force
Add-Type -Path '__FIXTURE__'
$run = Start-ConsoleRun -InputObject @('deferred check') -NoLive -ProgressIntervalSeconds 0
try {
$queues = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletCompleteConsoleRun')).Assembly.GetType('ConsoleMonster.PowerShell.LiveRunHostQueues').GetField('Queues', 'Static,NonPublic').GetValue($null)
$lookup = $queues.GetType().GetMethod('TryGetValue')
$arguments = [object[]]@($run.PSObject.BaseObject, $null)
if (-not $lookup.Invoke($queues, $arguments)) { throw 'The host writer was not registered' }
$writer = $arguments[1]
$gate = $writer.GetType().GetField('_gate', 'Instance,NonPublic').GetValue($writer)
$hold = New-Object HeldMonitor -ArgumentList $gate
try {
    try { Update-Console -Run $run -Index 0 -Status Pass -ErrorAction Stop }
    catch { "UPDATE_ERROR=$($_.Exception.Message)" }
    try {
        Complete-ConsoleRun $run -ErrorAction Stop
        throw 'Deferred output was reported complete'
    } catch { "DEFERRED_ERROR=$($_.Exception.Message)" }
    $arguments[1] = $null
    "RETAINED=$($lookup.Invoke($queues, $arguments))"
} finally { $hold.Dispose() }
"FLUSHED=$($run.Flush([TimeSpan]::FromSeconds(10)))"
$summary = Complete-ConsoleRun $run -PassThru
"FINISHED=$($summary.IsFinished),$($summary.Skipped)"
$arguments[1] = $null
"REMOVED=$(-not $lookup.Invoke($queues, $arguments))"
Complete-ConsoleRun $run
} finally { $run.Dispose() }
'@
        $fixture = Join-Path $PSScriptRoot 'Support/HeldMonitor.cs'
        $output = (Invoke-TestChild -Runtime $script:Runtime -Script $code.Replace('__MODULE__', $script:ModulePath).Replace('__FIXTURE__', $fixture)) -join "`n"
        $LASTEXITCODE | Should -Be 0 -Because $output
        $output | Should -Match 'DEFERRED_ERROR=Run output did not flush within five seconds'
        $output | Should -Match 'RETAINED=True'
        $output | Should -Match 'FLUSHED=True'
        $output | Should -Match 'FINISHED=True,0'
        $output | Should -Match 'REMOVED=True'
        ([regex]::Matches($output, '1 / 1 finished')).Count | Should -Be 1
    }

    It 'filters the PassThru snapshot by item Status, as the help shows' {
        Import-Module (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1') -Force
        $run = Start-ConsoleRun -InputObject a, b -NoLive
        Update-Console -Run $run -Index 0 -Status Pass
        Update-Console -Run $run -Index 1 -Status Fail -Detail 'refused'
        $summary = Complete-ConsoleRun $run -PassThru 6>$null
        $failed = @($summary.Items | Where-Object Status -eq Fail)
        $failed.Count | Should -Be 1
        $failed[0].Item.Label | Should -BeExactly 'b'
        $failed[0].Detail | Should -BeExactly 'refused'
        (Get-Help Complete-ConsoleRun -Examples | Out-String) | Should -Not -Match 'Where-Object State'
    }

    It 'releases a finalized host queue even when the host rejects its last output' {
        $code = @'
$ErrorActionPreference = 'Stop'
Import-Module '__MODULE__' -Force
$run = Start-ConsoleRun -InputObject @('final check') -NoLive -ProgressIntervalSeconds 0
try {
    try {
        Complete-ConsoleRun $run -InformationAction Stop -ErrorAction Stop
        throw 'The final host-output failure was ignored'
    } catch { "FINAL_HOST_ERROR=$($_.FullyQualifiedErrorId)" }
    $queues = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletCompleteConsoleRun')).Assembly.GetType('ConsoleMonster.PowerShell.LiveRunHostQueues').GetField('Queues', 'Static,NonPublic').GetValue($null)
    $arguments = [object[]]@($run.PSObject.BaseObject, $null)
    "REMOVED=$(-not $queues.GetType().GetMethod('TryGetValue').Invoke($queues, $arguments))"
    Complete-ConsoleRun $run
} finally { $run.Dispose() }
'@
        $output = (Invoke-TestChild -Runtime $script:Runtime -Script $code.Replace('__MODULE__', $script:ModulePath)) -join "`n"
        $LASTEXITCODE | Should -Be 0 -Because $output
        $output | Should -Match 'FINAL_HOST_ERROR=ActionPreferenceStop'
        $output | Should -Match 'REMOVED=True'
    }
}
