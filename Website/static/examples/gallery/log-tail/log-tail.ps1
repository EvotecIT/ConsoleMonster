# Reveal a bounded stream of sample log events in a live region.
[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 4)] [int] $Step = 0
)

Import-Module ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.
# All values are synthetic. This example never connects to a host.

# Data: fictional inputs kept in memory.
$messages = @(
    '[muted]09:41:00[/] [accent]INFO[/]  Worker started'
    '[muted]09:41:01[/] [pass]INFO[/]  Loaded 12 queued jobs'
    '[muted]09:41:02[/] [warn]WARN[/]  Cache cold; using local fallback'
    '[muted]09:41:03[/] [pass]INFO[/]  Cache warm; queue draining'
    '[muted]09:41:04[/] [pass]INFO[/]  Batch complete; no jobs lost'
)
# Layout: construct the widgets shown on screen.
function New-DemoFrame($Tick) {
    New-ConsoleRows -Gap 1 {
        '[bold]Application log[/] [muted]· worker events[/]'
        New-ConsolePanel -Title './logs/worker.log' {
            foreach ($line in $messages[0..$Tick]) { $line }
        }
        "[muted]$($Tick + 1) / 5 events · Ctrl+C stops the live run[/]"
    }
}

# Live updates: print one state headlessly, or run the bounded simulation.
if ($StaticSnapshot -or [Console]::IsInputRedirected) {
    New-DemoFrame $Step | Write-ConsoleWidget
    return
}
$frames = @(0..4 | ForEach-Object { New-DemoFrame $_ })
$screen = New-ConsoleRegion -Name demo -Widget $frames[0]
Start-ConsoleDashboard $screen -ArgumentList (, $frames) {
    param($Frames)
    foreach ($frame in $Frames) {
        if ($ConsoleSession.Cancellation.IsCancellationRequested) { break }
        Set-ConsoleRegion -Name demo -Widget $frame
        Start-Sleep -Milliseconds 650
    }
}
