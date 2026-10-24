[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 2)] [int] $Step = 0
)

Import-Module ConsoleMonster
# A parallel dry run: the delay and one failure are synthetic. No server is contacted.
$servers = @('web-01', 'web-02', 'web-03', 'job-01', 'sql-01', 'sql-02')
function New-PatchFrame($Tick) {
    New-ConsoleRows -Gap 1 {
        '[bold]Parallel patch run[/] [muted]· dry run, throttle 3[/]'
        $rows = foreach ($server in $servers) {
            $stage = @('Queued', 'Patching', 'Finished')[$Tick]
            $result = if ($Tick -lt 2) { '-' } elseif ($server -eq 'web-03') { 'Retry needed' } else { 'Pass' }
            [pscustomobject] @{ Host = $server; Stage = $stage; Result = $result }
        }
        New-ConsoleTable -InputObject $rows -Property Host, Stage, Result
        if ($Tick -eq 2) { '[pass]5 passed[/]  [warn]1 retry needed[/]  [muted]0 real hosts changed[/]' }
        else { '[muted]Synthetic hosts only. Ctrl+C cancels active work.[/]' }
    }
}
if ($StaticSnapshot -or [Console]::IsInputRedirected) {
    New-PatchFrame $Step | Write-ConsoleWidget
    return
}
$results = $servers | ForEach-ConsoleItem -Parallel -ThrottleLimit 3 -TimeoutSeconds 3 -Title 'Patch preview' -ErrorAction SilentlyContinue -ErrorVariable failures {
    Start-Sleep -Milliseconds 350
    if ($_ -eq 'web-03') { throw "$_ synthetic restart check failed" }
    "$_ preview passed"
}
# $results contains successes; $failures retains error records including the failed host.
# Keep both for retry/reporting. No patch command runs here.
New-PatchFrame 2 | Write-ConsoleWidget
