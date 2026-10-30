# Run synthetic work with a concurrency limit and retain failures for retry.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.
# A parallel dry run: the delay and one failure are synthetic. No server is contacted.
# Data: fictional inputs kept in memory.
$servers = @('web-01', 'web-02', 'web-03', 'job-01', 'sql-01', 'sql-02')
# Layout: construct the widgets shown on screen.
function New-PatchSummary {
    param(
        [object[]] $Outcomes
    )

    New-ConsoleRows -Gap 1 -Content {
        '[bold]Parallel patch run[/] [muted]· throttle 3[/]'
        $rows = foreach ($server in $servers) {
            $outcome = $Outcomes | Where-Object -Property Host -EQ -Value $server
            $stage = if ($outcome) { 'Finished' } else { 'Queued' }
            $result = if ($outcome) { $outcome.Result } else { '-' }
            [pscustomobject] @{ Host = $server; Stage = $stage; Result = $result }
        }
        New-ConsoleTable -InputObject $rows -Property Host, Stage, Result
        if ($Outcomes.Count -gt 0) {
            $passed = @($Outcomes | Where-Object -Property Result -EQ -Value Pass).Count
            "[pass]$passed passed[/]  [warn]$($Outcomes.Count - $passed) retry needed[/]"
        }
        else {
            '[muted]Ctrl+C cancels active work.[/]'
        }
    }
}
# Behaviour: run the actual parallel workload; keep success values and errors for retry/reporting.
New-PatchSummary -Outcomes @() | Show-Console
$runOptions = @{
    Parallel = $true
    ThrottleLimit = 3
    TimeoutSeconds = 3
    Title = 'Patch run'
    ErrorAction = 'SilentlyContinue'
    ErrorVariable = 'failures'
}
$results = @($servers | Invoke-ConsoleRun @runOptions -ScriptBlock {
        Start-Sleep -Milliseconds 350
        if ($_ -eq 'web-03') {
            throw "$_ restart check failed"
        }
        [pscustomobject] @{ Host = $_; Message = "$_ preview passed" }
    })
# $results contains successes; $failures retains error records including the failed host.
# Keep both for retry/reporting. No patch command runs here.
$outcomes = foreach ($server in $servers) {
    [pscustomobject] @{ Host = $server
        Result = if ($server -in $results.Host) { 'Pass' } else { 'Retry needed' } }
}
New-PatchSummary -Outcomes @($outcomes) | Show-Console
