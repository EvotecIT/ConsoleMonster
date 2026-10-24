# Patch a forty-host wave with bounded concurrency, timeouts and retained failures.
[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 3)] [int] $Step = 0
)
Import-Module ConsoleMonster

# Data: all hosts, outcomes and delays are synthetic. No remote command runs.
$servers = @(1..40 | ForEach-Object { 'web{0:00}' -f $_ })
$operations = @('Stage update', 'Apply update', 'Restart', 'Verify health')
$restartFailures = @(13, 26, 39)
$timeoutHost = 22
$throttle = 8
$timeoutSeconds = 4

# Layout: a wave summary, every host, eight lane details and retained failures.
function New-PatchFrame([int] $Phase, $Actual) {
    $done = @(0, 8, 24, 40)[$Phase]
    $active = if ($Phase -in 0, 3) { 0 } else { 8 }
    $states = @(foreach ($n in 1..40) {
        if ($Actual) { $Actual[$n - 1].Status }
        elseif ($n -le $done) { if ($n -in $restartFailures -or $n -eq $timeoutHost) { 'Fail' } else { 'Pass' } }
        elseif ($n -le ($done + $active)) { 'Running' }
        else { 'Pending' }
    })
    $failed = @($states | Where-Object { $_ -eq 'Fail' }).Count
    New-ConsoleRows -Gap 1 {
        New-ConsoleRule '[bold]Patching the web farm[/]'
        '40 hosts · throttle 8 · per-host timeout 4s'
        New-ConsoleSummaryBar -Done $done -Total 40 -Counter ([ordered] @{
            passed = $done - $failed; failed = $failed; running = $active; pending = 40 - $done - $active
        })
        New-ConsolePanel -Title 'Host outcomes' {
            New-ConsoleCellMap -Status $states -Columns 10 -LegendLines 2 -RowLabels 'web01–10', 'web11–20', 'web21–30', 'web31–40'
        }
        New-ConsolePanel -Title $(if ($Phase -eq 3) { 'Last eight hosts' } else { 'Worker lanes' }) {
            New-ConsoleLanes {
                foreach ($lane in 0..7) {
                    $n = if ($Phase -eq 3) { 33 + $lane } else { $done + $lane + 1 }
                    $status = $states[$n - 1]
                    $finished = $Phase -eq 3
                    $progress = if ($finished) { 4 } elseif ($Phase -eq 0) { 0 } else { 1 + $lane % 3 }
                    $detail = if ($finished) {
                        if ($status -eq 'Fail') { 'Restart verification failed' } else { 'Health verified' }
                    } elseif ($Phase -eq 0) { 'Waiting for a worker' } else { $operations[$progress - 1] }
                    $passed = if ($finished) { if ($status -eq 'Pass') { 4 } else { 3 } } else { $progress }
                    $laneFailed = if ($finished -and $status -eq 'Fail') { 1 } else { 0 }
                    New-ConsoleLane $servers[$n - 1] -Total 4 -Done $progress -Status $status -Passed $passed -Failed $laneFailed -Detail $detail
                }
            }
        }
        New-ConsolePanel -Title 'Retained failures' {
            if (-not $failed) { '[muted]No failures recorded[/]' }
            else {
                New-ConsoleStatusList {
                    foreach ($n in 1..40) {
                        if ($states[$n - 1] -eq 'Fail') {
                            $detail = if ($n -eq $timeoutHost) { '4s timeout · retry separately' } else { 'Restart verification failed' }
                            New-ConsoleStatusItem $servers[$n - 1] -Status Fail -Detail $detail
                        }
                    }
                }
            }
        }
        if ($Phase -eq 3) { '[bold]Wave finished[/] · failed hosts remain available for a separate retry' }
        else { '[muted]Ctrl+C cancels active work and skips queued hosts[/]' }
    }
}

# Behaviour: the native run monitor owns scheduling, cancellation and the live UI.
if ($StaticSnapshot -or [Console]::IsInputRedirected) {
    New-PatchFrame $Step | Write-ConsoleWidget
    return
}
$successes = @($servers | ForEach-ConsoleItem -Parallel -ThrottleLimit $throttle -TimeoutSeconds $timeoutSeconds -Title 'Patching the web farm' -ErrorAction SilentlyContinue -ErrorVariable retainedErrors {
    $n = [int] $_.Substring(3)
    Start-Sleep -Milliseconds (500 + $n * 137 % 900)
    if ($n -in $using:restartFailures) { throw "$_ restart verification failed" }
    if ($n -eq $using:timeoutHost) { Start-Sleep -Seconds 10 }
    $_
})
# Keep both success objects and ErrorRecords; never retry automatically.
$outcomes = foreach ($server in $servers) {
    [pscustomobject] @{ Host = $server; Status = if ($server -in $successes) { 'Pass' } else { 'Fail' } }
}
New-PatchFrame 3 $outcomes | Write-ConsoleWidget
