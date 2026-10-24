# Run synthetic work with a concurrency limit and retain failures for retry.
[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 2)] [int] $Step = 0
)

Import-Module ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.
# A parallel dry run: the delay and one failure are synthetic. No server is contacted.
# Data: fictional inputs kept in memory.
$servers = @('web-01', 'web-02', 'web-03', 'job-01', 'sql-01', 'sql-02')
# Layout: construct the widgets shown on screen.
function New-PatchFrame($Tick) {
    New-ConsoleRows -Gap 1 -Content {
        '[bold]Parallel patch run[/] [muted]· throttle 3[/]'
        $rows = foreach ($server in $servers) {
            $stage = @('Queued', 'Patching', 'Finished')[$Tick]
            $result = if ($Tick -lt 2) { '-' } elseif ($server -eq 'web-03') { 'Retry needed' } else { 'Pass' }
            [pscustomobject] @{ Host = $server; Stage = $stage; Result = $result }
        }
        New-ConsoleTable -InputObject $rows -Property Host, Stage, Result
        if ($Tick -eq 2) { '[pass]5 passed[/]  [warn]1 retry needed[/]' }
        else { '[muted]Ctrl+C cancels active work.[/]' }
    }
}
# Live updates: print one state headlessly, or run the bounded simulation.
if ($StaticSnapshot -or [Console]::IsInputRedirected) {
    New-PatchFrame $Step | Show-Console
    return
}
$results = $servers | Invoke-ConsoleRun -Parallel -ThrottleLimit 3 -TimeoutSeconds 3 -Title 'Patch run' -ErrorAction SilentlyContinue -ErrorVariable failures {
    Start-Sleep -Milliseconds 350
    if ($_ -eq 'web-03') { throw "$_ restart check failed" }
    "$_ preview passed"
}
# $results contains successes; $failures retains error records including the failed host.
# Keep both for retry/reporting. No patch command runs here.
New-PatchFrame 2 | Show-Console
