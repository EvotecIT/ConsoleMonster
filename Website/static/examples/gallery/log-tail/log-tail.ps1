# Reveal a bounded stream of sample log events, then search the run with the find bar.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
$events = @(
    [pscustomobject] @{ Time = '09:41:00'; Level = 'info'; Message = 'Worker started' }
    [pscustomobject] @{ Time = '09:41:01'; Level = 'info'; Message = 'Loaded 12 queued jobs' }
    [pscustomobject] @{ Time = '09:41:02'; Level = 'warn'
        Message = 'Cache cold; using local fallback' }
    [pscustomobject] @{ Time = '09:41:03'; Level = 'info'; Message = 'Cache warm; queue draining' }
    [pscustomobject] @{ Time = '09:41:04'; Level = 'pass'
        Message = 'Batch complete; no jobs lost' }
)
$labels = @{ info = 'INFO'; warn = 'WARN'; pass = 'OK' }
$roles = @{ info = 'accent'; warn = 'warn'; pass = 'pass' }

# Layout: construct the widgets shown on screen.
function New-DemoFrame {
    param(
        $Tick
    )

    New-ConsoleRows -Gap 1 -Content {
        '[accent bold]Application log[/] [muted]· worker events[/]'
        New-ConsolePanel -Title './logs/worker.log' -Content {
            foreach ($entry in $events[0..$Tick]) {
                $label = $labels[$entry.Level].PadRight(4)
                "[muted]$($entry.Time)[/] [$($roles[$entry.Level])]$label[/]  $($entry.Message)"
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

# Then read the run back: / finds text, n moves to the next match, Escape quits.
Show-ConsoleApp -Content {
    '[accent bold]Application log[/] [muted]· read the run back[/]'
    New-ConsoleLogRegion -WidgetId worker.log -NoElapsed -Height 7 -Content {
        foreach ($entry in $events) {
            @{ Message = $entry.Message; Level = $entry.Level }
        }
    }
}