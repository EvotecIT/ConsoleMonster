# Follow CPU and memory pressure in a bounded top-like dashboard.
Import-Module -Name ConsoleMonster

# Data: synthetic counters, not the machine's real process list.
$processes = @(
    [pscustomobject] @{ Name = 'build-worker'; Pid = 4102; Cpu = 42; Memory = 860 }
    [pscustomobject] @{ Name = 'test-runner'; Pid = 4107; Cpu = 28; Memory = 512 }
    [pscustomobject] @{ Name = 'api-host'; Pid = 4120; Cpu = 12; Memory = 220 }
    [pscustomobject] @{ Name = 'file-watcher'; Pid = 4131; Cpu = 3; Memory = 48 }
)
$cpuHistory = @(38, 45, 51, 48, 56)

# Layout: build a complete frame from one set of counters.
function New-ProcessFrame {
    param(
        $Tick
    )

    $rows = foreach ($process in $processes) {
        [pscustomobject] @{ Name = $process.Name; Pid = $process.Pid; 'CPU %' = $process.Cpu + $Tick * 3; 'Memory MiB' = $process.Memory + $Tick * 16 }
    }
    New-ConsoleRows -Gap 1 -Content {
        New-ConsoleRule -Title '[bold]Process monitor[/] [muted]· refresh 1s[/]'
        New-ConsoleText -Markup "[accent]CPU $((56 + $Tick * 5))%[/]   Memory $((1640 + $Tick * 64)) MiB   [muted]4 processes[/]"
        New-ConsoleTable -InputObject $rows -Property Name, Pid, 'CPU %', 'Memory MiB' -Expand
        New-ConsolePanel -Title 'CPU history' -Content {
            New-ConsoleChart -Title 'CPU %' -Type Sparkline -Values @($cpuHistory + (56 + $Tick * 5))
        }
        '[muted]Ctrl+C stops monitoring. Final counters stay in scrollback.[/]'
    }
}

# Behaviour: update each frame in a bounded live run.
$frames = @(0..2 | ForEach-Object -Process { New-ProcessFrame -Tick $_ })
$screen = New-ConsoleRegion -WidgetId processes -Content $frames[0]
Invoke-ConsoleDashboard -Dashboard $screen -ScriptBlock {
    $Frames = $using:frames
    foreach ($frame in $Frames) {
        if ($ConsoleSession.Cancellation.IsCancellationRequested) {
            break
        }
        Update-Console -WidgetId processes -Content $frame
        Wait-ConsoleDashboard -Seconds 1
    }
}
