# Animate worker progress bars and a small session dashboard.
[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 4)] [int] $Step = 0
)

Import-Module ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.
# All values are synthetic. This example never connects to a host.

# Data: fictional inputs kept in memory.
$workerCount = 8

# Layout: construct the widgets shown on screen.
function New-DemoFrame($Tick) {
    New-ConsoleRows -Gap 1 {
        '[bold]Worker stress dashboard[/] [muted]· worker pool[/]'
        New-ConsoleColumns {
            New-ConsolePanel -Title Workers {
                foreach ($worker in 1..$workerCount) {
                    $value = [Math]::Min(100, 12 + $worker * 5 + $Tick * 17)
                    New-ConsoleProgress -Name ('worker' + $worker) -Label ('worker-{0:00}' -f $worker) -Value $value -Total 100
                }
            }
            New-ConsolePanel -Title Session {
                New-ConsoleKeyValue -Markup -Data ([ordered] @{
                    'Updates' = [string] (800 + $Tick * 320)
                    'Queue' = [string] (24 - $Tick * 5)
                    'Workers' = '8'
                    'Dropped' = '[pass]0[/]'
                })
                New-ConsoleSparkline -Label 'updates/s' -Value @(120,180,160,240,210,(220 + $Tick * 15))
            }
        }
        '[muted]Ctrl+C stops the live run.[/]'
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
