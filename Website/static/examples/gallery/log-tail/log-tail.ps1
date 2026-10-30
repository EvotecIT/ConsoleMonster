# Reveal a bounded stream of sample log events in a live region.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
$messages = @(
    '[muted]09:41:00[/] [accent]INFO[/]  Worker started'
    '[muted]09:41:01[/] [pass]INFO[/]  Loaded 12 queued jobs'
    '[muted]09:41:02[/] [warn]WARN[/]  Cache cold; using local fallback'
    '[muted]09:41:03[/] [pass]INFO[/]  Cache warm; queue draining'
    '[muted]09:41:04[/] [pass]INFO[/]  Batch complete; no jobs lost'
)
# Layout: construct the widgets shown on screen.
function New-DemoFrame {
    param(
        $Tick
    )

    New-ConsoleRows -Gap 1 -Content {
        '[bold]Application log[/] [muted]· worker events[/]'
        New-ConsolePanel -Title './logs/worker.log' -Content {
            foreach ($line in $messages[0..$Tick]) {
                $line
            }
        }
        "[muted]$($Tick + 1) / 5 events · Ctrl+C stops the live run[/]"
    }
}

# Behaviour: update each frame in a bounded live run.
$frames = @(0..4 | ForEach-Object -Process { New-DemoFrame -Tick $_ })
$screen = New-ConsoleRegion -WidgetId demo -Content $frames[0]
Invoke-ConsoleDashboard -Dashboard $screen -ArgumentList (, $frames) -ScriptBlock {
    param($Frames)
    foreach ($frame in $Frames) {
        if ($ConsoleSession.Cancellation.IsCancellationRequested) {
            break
        }
        Update-Console -WidgetId demo -Content $frame
        Wait-ConsoleDashboard -Seconds 0.65
    }
}
