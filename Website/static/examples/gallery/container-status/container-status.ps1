# Watch a container host: state, restarts, CPU and memory against each limit, with an event feed.
Import-Module -Name ConsoleMonster

# Data: four fictional samples, two seconds apart; no container engine is contacted.
$containers = @(
    [pscustomobject] @{ Name = 'web'; Image = 'nginx:1.27'; Limit = 256; Cpu = @(8, 9, 12, 10)
        Memory = @(96, 98, 101, 99); Restarts = @(0, 0, 0, 0) }
    [pscustomobject] @{ Name = 'api'; Image = 'adatum/api:4.2'; Limit = 512; Cpu = @(34, 41, 3, 22)
        Memory = @(410, 468, 120, 190); Restarts = @(2, 2, 3, 3) }
    [pscustomobject] @{ Name = 'worker'; Image = 'adatum/worker:4.2'; Limit = 896
        Cpu = @(55, 61, 72, 78); Memory = @(690, 742, 801, 846); Restarts = @(0, 0, 0, 0) }
    [pscustomobject] @{ Name = 'db'; Image = 'postgres:16'; Limit = 2048; Cpu = @(18, 22, 25, 21)
        Memory = @(1210, 1225, 1240, 1236); Restarts = @(0, 0, 0, 0) }
    [pscustomobject] @{ Name = 'cache'; Image = 'redis:7.4'; Limit = 512; Cpu = @(3, 4, 4, 3)
        Memory = @(152, 153, 153, 154); Restarts = @(0, 0, 0, 0) }
    [pscustomobject] @{ Name = 'queue'; Image = 'rabbitmq:3.13'; Limit = 1024; Cpu = @(6, 7, 9, 8)
        Memory = @(301, 305, 309, 312); Restarts = @(1, 1, 1, 1) }
)
$events = @()

# Layout: one complete screen per sample; restarts and memory pressure also go to the event feed.
function New-ContainerFrame {
    param(
        [int] $Tick
    )

    $clock = '10:00:{0:00}' -f ($Tick * 2)
    $rows = foreach ($container in $containers) {
        $memoryPercent = [math]::Round(100 * $container.Memory[$Tick] / $container.Limit)
        $restarted = $Tick -gt 0 -and $container.Restarts[$Tick] -gt $container.Restarts[$Tick - 1]
        $status = if ($restarted -or $memoryPercent -ge 90) { 'Fail' }
        elseif ($memoryPercent -ge 80) { 'Warn' }
        else { 'Pass' }
        if ($restarted) {
            $script:events = @([pscustomobject] @{ Label = "$clock $($container.Name)"
                Status = 'Fail'
                Detail = "restarted after exit 137 · restart $($container.Restarts[$Tick])" }) + $script:events
        }
        elseif ($memoryPercent -ge 90) {
            $script:events = @([pscustomobject] @{ Label = "$clock $($container.Name)"
                Status = 'Warn'
                Detail = "memory at $memoryPercent% of its $($container.Limit) MiB limit" }) + $script:events
        }
        [pscustomobject] @{
            Container = $container.Name
            Image = $container.Image
            State = $(if ($restarted) { 'restarting' } else { 'running' })
            Restarts = $container.Restarts[$Tick]
            'CPU %' = $container.Cpu[$Tick]
            'Memory MiB' = $container.Memory[$Tick]
            'Memory %' = $memoryPercent
            Status = $status
        }
    }
    $restarting = @($rows | Where-Object -Property State -EQ -Value 'restarting').Count
    $memoryTotal = ($rows | Measure-Object -Property 'Memory MiB' -Sum).Sum
    $limitTotal = ($containers | Measure-Object -Property Limit -Sum).Sum
    $feed = @($script:events | Select-Object -First 4)

    New-ConsoleRows -Gap 1 -Content {
        New-ConsoleRule -Title "[bold]Containers[/] [muted]· docker-01.adatum.example.test · refresh 2s · $clock[/]"
        ("[pass]$($rows.Count - $restarting) running[/]   [warn]$restarting restarting[/]   " +
            "[muted]memory $('{0:N0}' -f $memoryTotal) of $('{0:N0}' -f $limitTotal) MiB reserved[/]")
        New-ConsoleTable -InputObject $rows -Property Container, Image, State, Restarts, 'CPU %',
            'Memory MiB', 'Memory %', Status -Expand
        New-ConsoleColumns -Content {
            New-ConsolePanel -Title 'CPU %' -Content {
                $chartParameters = @{
                    WidgetId = 'cpu'
                    Type = 'Bar'
                    Maximum = 100
                    Warning = 70
                    Critical = 90
                }
                $rows |
                    ForEach-Object -Process {
                        [pscustomobject] @{ Label = $_.Container; Value = $_.'CPU %' }
                    } |
                        New-ConsoleChart @chartParameters
            }
            New-ConsolePanel -Title 'Memory % of limit' -Content {
                $chartParameters = @{
                    WidgetId = 'memory'
                    Type = 'Bar'
                    Maximum = 100
                    Warning = 80
                    Critical = 90
                }
                $rows |
                    ForEach-Object -Process {
                        [pscustomobject] @{ Label = $_.Container; Value = $_.'Memory %' }
                    } |
                        New-ConsoleChart @chartParameters
            }
        }
        New-ConsolePanel -Title 'Events' -Content {
            if ($feed.Count -eq 0) {
                '[muted]No restarts or memory warnings yet.[/]'
            }
            else {
                New-ConsoleStatusList -Content { $feed }
            }
        }
    }
}

# Behaviour: build each sample's screen, then replay them in a bounded live dashboard.
$frames = @(0..3 | ForEach-Object -Process { New-ContainerFrame -Tick $_ })
$screen = New-ConsoleRegion -WidgetId containers -Content $frames[0]
Invoke-ConsoleDashboard -Dashboard $screen -ArgumentList (, $frames) -ScriptBlock {
    param($Frames)
    foreach ($frame in $Frames) {
        if ($ConsoleSession.Cancellation.IsCancellationRequested) {
            break
        }
        Update-Console -WidgetId containers -Content $frame
        Wait-ConsoleDashboard -Seconds 2
    }
}
