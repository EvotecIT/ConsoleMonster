# Animate worker progress bars and a small session dashboard.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
$workerCount = 8

# Layout: construct the widgets shown on screen.
function New-DemoFrame {
    param(
        $Tick
    )

    New-ConsoleRows -Gap 1 -Content {
        '[bold]Worker stress dashboard[/] [muted]· worker pool[/]'
        New-ConsoleColumns -Content {
            New-ConsolePanel -Title Workers -Content {
                foreach ($worker in 1..$workerCount) {
                    $value = [Math]::Min(100, 12 + $worker * 5 + $Tick * 17)
                    New-ConsoleProgress -WidgetId ('worker' + $worker) -Label ('worker-{0:00}' -f $worker) -Value $value -Total 100
                }
            }
            New-ConsolePanel -Title Session -Content {
                New-ConsoleKeyValue -Markup -InputObject ([ordered] @{
                        'Updates' = [string] (800 + $Tick * 320)
                        'Queue' = [string] (24 - $Tick * 5)
                        'Workers' = '8'
                        'Dropped' = '[pass]0[/]'
                    })
                New-ConsoleChart -Label 'updates/s' -Type Sparkline -Values @(120, 180, 160, 240, 210, (220 + $Tick * 15))
            }
        }
        '[muted]Ctrl+C stops the live run.[/]'
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
        Wait-ConsoleDashboard -Milliseconds 650
    }
}
