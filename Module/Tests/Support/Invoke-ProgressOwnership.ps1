param([Parameter(Mandatory)][string] $ModulePath, [switch] $ClosingHandoff, [switch] $StandaloneHandoff)
$ErrorActionPreference = 'Stop'

# Exercise the routing engine on real runspace threads. ImmediateDispatcher is the normal non-render-thread dispatcher:
# it lets this contract run with redirected output on Windows PowerShell 5.1, PowerShell 7 and Unix, without a test hook.
# Reflection reaches the internal engine/bridge constructor; assertions observe real ActivityLists and native progress.
$setup = @'
param($modulePath, [bool] $enable = $true)
$global:ProgressPreference = 'Continue'
Import-Module $modulePath
if ($enable) { Enable-ConsoleFormatting -Feature Progress }
$global:activities = [ConsoleMonster.Widgets.ActivityList]::new()
$global:engine = [ConsoleMonster.PowerShell.ProgressBridge].Assembly.GetType('ConsoleMonster.PowerShell.SessionProgress', $true)
$flags = [System.Reflection.BindingFlags] 'Static, NonPublic'
$global:report = $engine.GetMethod('Report', $flags)
$global:override = $engine.GetProperty('Override', $flags)
$constructor = [ConsoleMonster.PowerShell.ProgressBridge].GetConstructor([System.Reflection.BindingFlags] 'Instance, NonPublic', $null, [type[]] @([ConsoleMonster.Live.ILiveDispatcher]), $null)
$bridge = $constructor.Invoke(@([ConsoleMonster.Live.ImmediateDispatcher]::new($activities)))
$override.SetValue($null, $bridge, $null)
function global:Report-Progress {
    param([string] $Label, [bool] $Completed = $false)
    [bool] $report.Invoke($null, [object[]] @(7, -1, $Label, 'working', 50, -1, $Completed))
}
function global:End-ProgressPipeline {
    $engine.GetMethod('EndPipeline', [System.Reflection.BindingFlags] 'Static, NonPublic', $null, [type[]] @(), $null).Invoke($null, @())
}
'@

function Invoke-Owner {
    param([powershell] $Shell, [string] $Script)
    $Shell.Commands.Clear()
    $result = $Shell.AddScript($Script).Invoke()
    if ($Shell.HadErrors) {
        throw ($Shell.Streams.Error | Out-String)
    }
    $result
}

if ($ClosingHandoff -or $StandaloneHandoff) {
    if ($ClosingHandoff) { Add-Type -Path (Join-Path $PSScriptRoot 'RunspaceCloseBarrier.cs') }
    $owner = [powershell]::Create()
    $next = [powershell]::Create()
    $barrier = $null
    $lease = $null
    try {
        $null = $owner.AddScript($setup).AddArgument($ModulePath).AddArgument(-not $StandaloneHandoff).Invoke()
        $null = $next.AddScript($setup).AddArgument($ModulePath).AddArgument(-not $StandaloneHandoff).Invoke()
        if ($owner.HadErrors -or $next.HadErrors) { throw 'Closing runspace setup failed.' }
        if ($StandaloneHandoff) {
            Invoke-Owner $owner '$override.SetValue($null, $null, $null)'
            Invoke-Owner $next '$override.SetValue($null, $null, $null)'
            $lease = Invoke-Owner $owner '$engine.GetMethod(''TryEnterForeground'', [System.Reflection.BindingFlags] ''Static, NonPublic'').Invoke($null, @())'
            Invoke-Owner $next '"STANDALONE_BLOCKED=$($null -eq $engine.GetMethod(''TryEnterForeground'', [System.Reflection.BindingFlags] ''Static, NonPublic'').Invoke($null, @()))"'
            $lease.Dispose()
            Invoke-Owner $next '$global:lease = $engine.GetMethod(''TryEnterForeground'', [System.Reflection.BindingFlags] ''Static, NonPublic'').Invoke($null, @()); "STANDALONE_NEXT_PIPELINE=$($null -ne $lease)"; if ($lease) { $lease.Dispose() }'
            Invoke-Owner $next '"DISABLED_REPORT=$(Report-Progress Disabled)"'
            Invoke-Owner $next '$global:lease = $engine.GetMethod(''TryEnterForeground'', [System.Reflection.BindingFlags] ''Static, NonPublic'').Invoke($null, @()); "STANDALONE_AFTER_DISABLED=$($null -ne $lease)"; if ($lease) { $lease.Dispose() }'
            return
        }
        $lease = Invoke-Owner $owner '$engine.GetMethod(''TryEnterForeground'', [System.Reflection.BindingFlags] ''Static, NonPublic'').Invoke($null, @())'
        "OWNER_RESERVED=$($null -ne $lease)"
        $barrier = [RunspaceCloseBarrier]::new($owner.Runspace)
        $barrier.BeginClose()
        if (-not $barrier.WaitUntilClosing()) { throw 'Runspace did not begin closing.' }
        Invoke-Owner $next '"CLOSING_OWNER_BLOCKED=$($null -eq $engine.GetMethod(''TryEnterForeground'', [System.Reflection.BindingFlags] ''Static, NonPublic'').Invoke($null, @()))"; End-ProgressPipeline'
        $barrier.Release()
        if (-not $barrier.WaitUntilClosed()) { throw 'Runspace did not finish closing.' }
        Invoke-Owner $next '$global:lease = $engine.GetMethod(''TryEnterForeground'', [System.Reflection.BindingFlags] ''Static, NonPublic'').Invoke($null, @()); "CLOSED_OWNER_RELEASED=$($null -ne $lease)"; $lease.Dispose(); $override.SetValue($null, $null, $null); End-ProgressPipeline'
    } finally {
        if ($barrier) { $barrier.Dispose() }
        if ($lease) { $lease.Dispose() }
        $owner.Dispose()
        $next.Dispose()
    }
    return
}

$first = [powershell]::Create()
$second = [powershell]::Create()
$third = [powershell]::Create()
$oldLease = $null
$nativeReady = [System.Threading.ManualResetEventSlim]::new()
$nativeContinue = [System.Threading.ManualResetEventSlim]::new()
$nativePending = $null
try {
    $null = $first.AddScript($setup).AddArgument($ModulePath).Invoke()
    $null = $second.AddScript($setup).AddArgument($ModulePath).Invoke()
    if ($first.HadErrors -or $second.HadErrors) { throw 'Progress runspace setup failed.' }
    Invoke-Owner $first '"FIRST_HANDLED=$(Report-Progress Alpha)"'
    Invoke-Owner $second @'
$handled = Report-Progress Beta
"SECOND_HANDLED=$handled"
if (-not $handled) { Microsoft.PowerShell.Utility\Write-Progress -Id 7 -Activity Beta -PercentComplete 50 }
$handled = Report-Progress Beta -Completed $true
if (-not $handled) { Microsoft.PowerShell.Utility\Write-Progress -Id 7 -Activity Beta -Completed }
$engine.GetMethod('Suspend', [System.Reflection.BindingFlags] 'Static, NonPublic').Invoke($null, @())
End-ProgressPipeline
Disable-ConsoleFormatting
'@
    "SECOND_NATIVE=$(@($second.Streams.Progress | Where-Object { $_.Activity -eq 'Beta' }).Count -gt 0)"
    Invoke-Owner $first @'
"FIRST_AFTER_SECOND=$($activities.Count)"
"FIRST_UPDATED=$(Report-Progress AlphaUpdated)"
"FIRST_LABEL=$([ConsoleMonster.Export.ConsoleExporter]::ToText($activities).Contains('AlphaUpdated'))"
$null = Report-Progress AlphaUpdated -Completed $true
"FIRST_COMPLETED=$($activities.Count)"
$override.SetValue($null, $null, $null)
End-ProgressPipeline
'@
    Invoke-Owner $second @'
"DISABLED_HANDLED=$(Report-Progress DisabledBeta) DISABLED_ACTIVITIES=$($activities.Count)"
End-ProgressPipeline
Enable-ConsoleFormatting -Feature Progress
"SECOND_AFTER_RELEASE=$(Report-Progress BetaAfterRelease) SECOND_ACTIVITIES=$($activities.Count)"
'@
    Invoke-Owner $first 'End-ProgressPipeline; Disable-ConsoleFormatting'
    Invoke-Owner $second '"SECOND_SURVIVED=$($activities.Count) SECOND_LABEL=$([ConsoleMonster.Export.ConsoleExporter]::ToText($activities).Contains(''BetaAfterRelease''))"'
    Invoke-Owner $first @'
Enable-ConsoleFormatting -Feature Progress
$constructor = [ConsoleMonster.PowerShell.ProgressBridge].GetConstructor([System.Reflection.BindingFlags] 'Instance, NonPublic', $null, [type[]] @([ConsoleMonster.Live.ILiveDispatcher]), $null)
$override.SetValue($null, $constructor.Invoke(@([ConsoleMonster.Live.ImmediateDispatcher]::new($activities))), $null)
"NATIVE_STARTED=$(Report-Progress AlphaNative)"
'@
    $first.Runspace.SessionStateProxy.SetVariable('nativeReady', $nativeReady)
    $first.Runspace.SessionStateProxy.SetVariable('nativeContinue', $nativeContinue)
    $first.Commands.Clear()
    $nativePending = $first.AddScript('"NATIVE_STARTED=$(Report-Progress AlphaNative)"; $nativeReady.Set(); if (-not $nativeContinue.Wait(15000)) { throw ''Native handoff was not released.'' }; "NATIVE_UPDATE_AFTER_RELEASE=$(Report-Progress AlphaNative) NATIVE_COMPLETION_AFTER_RELEASE=$(Report-Progress AlphaNative -Completed $true) NATIVE_CUSTOM_ACTIVITIES=$($activities.Count)"').BeginInvoke()
    if (-not $nativeReady.Wait(15000)) { throw 'Native activity did not start.' }
    Invoke-Owner $second '$null = Report-Progress BetaAfterRelease -Completed $true; $override.SetValue($null, $null, $null); End-ProgressPipeline'
    $nativeContinue.Set()
    $first.EndInvoke($nativePending)
    $nativePending = $null
    if ($first.HadErrors) { throw ($first.Streams.Error | Out-String) }
    Invoke-Owner $second @'
$constructor = [ConsoleMonster.PowerShell.ProgressBridge].GetConstructor([System.Reflection.BindingFlags] 'Instance, NonPublic', $null, [type[]] @([ConsoleMonster.Live.ILiveDispatcher]), $null)
$override.SetValue($null, $constructor.Invoke(@([ConsoleMonster.Live.ImmediateDispatcher]::new($activities))), $null)
$null = Report-Progress BetaClosing
'@
    # Closing the foreground owner also releases the terminal; an unrelated closed runspace must not clear its successor.
    $second.Dispose()
    Invoke-Owner $first @'
Enable-ConsoleFormatting -Feature Progress
$constructor = [ConsoleMonster.PowerShell.ProgressBridge].GetConstructor([System.Reflection.BindingFlags] 'Instance, NonPublic', $null, [type[]] @([ConsoleMonster.Live.ILiveDispatcher]), $null)
$override.SetValue($null, $constructor.Invoke(@([ConsoleMonster.Live.ImmediateDispatcher]::new($activities))), $null)
"AFTER_OWNER_CLOSED=$(Report-Progress AlphaAfterClose)"
'@
    $oldLease = Invoke-Owner $first '$global:foregroundLease = $engine.GetMethod(''TryEnterForeground'', [System.Reflection.BindingFlags] ''Static, NonPublic'').Invoke($null, @()); $global:foregroundLease'
    Invoke-Owner $first '$override.SetValue($null, $null, $null); End-ProgressPipeline'
    $null = $third.AddScript($setup).AddArgument($ModulePath).Invoke()
    if ($third.HadErrors) { throw 'Third runspace setup failed.' }
    Invoke-Owner $third '"SCOPE_BLOCKED=$($null -eq $engine.GetMethod(''TryEnterForeground'', [System.Reflection.BindingFlags] ''Static, NonPublic'').Invoke($null, @()))"'
    Invoke-Owner $third 'End-ProgressPipeline'
    $first.Dispose()
    Invoke-Owner $third '$global:foregroundLease = $engine.GetMethod(''TryEnterForeground'', [System.Reflection.BindingFlags] ''Static, NonPublic'').Invoke($null, @()); "SCOPE_AFTER_CLOSE=$($null -ne $foregroundLease)"'
    $oldLease.Dispose()
    $competitor = [powershell]::Create()
    try {
        $null = $competitor.AddScript($setup).AddArgument($ModulePath).Invoke()
        Invoke-Owner $competitor '"STALE_SCOPE_KEPT_SUCCESSOR=$($null -eq $engine.GetMethod(''TryEnterForeground'', [System.Reflection.BindingFlags] ''Static, NonPublic'').Invoke($null, @()))"'
    } finally { $competitor.Dispose() }
    Invoke-Owner $third '"SUCCESSOR_AFTER_OLD_SCOPE=$(Report-Progress Gamma)"; $override.SetValue($null, $null, $null); $foregroundLease.Dispose(); End-ProgressPipeline'
} finally {
    $nativeContinue.Set()
    if ($nativePending) { $first.EndInvoke($nativePending) }
    if ($oldLease) { $oldLease.Dispose() }
    $first.Dispose()
    $second.Dispose()
    $third.Dispose()
    $nativeReady.Dispose()
    $nativeContinue.Dispose()
}
