# Stress test: thirty-two workers, session telemetry and captured tool output.

Import-Module -Name ConsoleMonster

# Data: synthetic telemetry samples; no machine counters, services or files are read.
$workers = 32
$actions = 'parsing', 'resolving', 'evaluating', 'scoring', 'writing'
$sampleRates = @(
    18000, 25919, 22838, 19757, 27676, 24595, 21514, 18433, 26352, 23271, 20190, 28109,
    25028, 21947, 18866, 26785, 23704, 20623, 28542, 25461, 22380, 19299, 27218, 24137,
    21056, 28975, 25894, 22813, 19732, 27651, 24570, 21489, 18408, 26327, 23246, 20165,
    28084, 25003, 21922, 18841, 26760, 23679, 20598, 28517, 25436, 22355, 19274, 27193
)
$sampleCpu = @(
    42, 59, 76, 93, 110, 53, 70, 87, 104, 47, 64, 81,
    98, 115, 58, 75, 92, 109, 52, 69, 86, 103, 46, 63,
    80, 97, 114, 57, 74, 91, 108, 51, 68, 85, 102, 45,
    62, 79, 96, 113, 56, 73, 90, 107, 50, 67, 84, 101
)
$messages = 'cache miss for key 412', 'retrying request 28 (attempt 2)', 'connection pool at 76% capacity', 'GC pause 8 ms', 'flushed 960 records'
$culture = [cultureinfo]::InvariantCulture
# Layout: all thirty-two lanes, the update sparkline, CPU chart and session counters remain visible.
function New-StressFrame {
    param(
        [int] $Tick
    )

    $samples = $Tick * 8
    $posted = $Tick * 128000L
    $lanes = New-ConsoleLanes -WidgetId workers -Content {
        for ($id = 0; $id -lt $workers; $id++) {
            $lane = @{
                Key = "w$id"
                Label = 'worker {0:00}' -f ($id + 1)
                Total = 100000
            }
            if ($tick -gt 0) {
                $done = if ($tick -eq 6) { 100000 } else { $tick * 3200 + $id * 97 % 400 }
                $failed = [int][Math]::Truncate($done / 97)
                $warned = [int][Math]::Truncate($done / 17)
                $lane.Done = $done
                $lane.Failed = $failed
                $lane.Warned = $warned
                $lane.Passed = $done - $failed - $warned
                $lane.Status = if ($tick -eq 6) { 'Pass' } else { 'Running' }
                $lane.Detail = $actions[($id + $tick) % $actions.Count] + ' item ' + $done.ToString('#,0', $culture)
            }
            New-ConsoleLane @lane
        }
    }
    $stats = [ordered]@{
        'Updates posted' = $posted.ToString('#,0', $culture)
        'Updates applied' = $posted.ToString('#,0', $culture)
        'Update rate' = if ($samples) { $sampleRates[$samples - 1].ToString('#,0', $culture) + '/s' } else { '-' }
        Frames = ($tick * 15).ToString('#,0', $culture)
        'Frame rate' = if ($tick) { '15.0 fps' } else { '-' }
        CPU = if ($samples) { $sampleCpu[$samples - 1].ToString('#,0', $culture) + '%' } else { '-' }
        'CPU share' = if ($samples) { ($sampleCpu[$samples - 1] / 8).ToString('0.0', $culture) + '% of 8 cores' } else { '-' }
        'Stray writes' = ($tick * 4).ToString($culture)
        'Dropped / errors' = '0 / 0'
    }
    $dashboard = New-ConsoleDashboard -Title '[bold]Stress test[/]' -Subtitle '32 workers · captured tool output · 6s' -Footer '[muted]32 workers    15 frames/s    Ctrl+C stop[/]' -Content {
        New-ConsoleColumns -Content {
            New-ConsolePanel -Title Workers -Weight 2.2 -Content { $lanes }
            New-ConsolePanel -Title Session -Content {
                New-ConsoleKeyValue -WidgetId stats -InputObject $stats
                New-ConsoleRule -Title 'updates/s'
                $values = @($sampleRates | Select-Object -First $samples)
                New-ConsoleSparkline -WidgetId rate -Height 3 -Unit '/s' -Value $values
                New-ConsoleRule -Title 'CPU % (100% = one core)'
                New-ConsoleLineChart -WidgetId cpu -Height 6 -Minimum 0 -Unit '%' -Content {
                    New-ConsoleChartSeries -Key cpu -Role Accent2 -Value @($sampleCpu | Select-Object -First $samples)
                }
            }
        }
        New-ConsolePanel -Title 'Captured output' -Fill -Content {
            New-ConsoleLogRegion -Height 6 -NoElapsed -Content {
                for ($i = 0; $i -lt $tick * 4; $i++) {
                    $level = if ($i % 7 -eq 0) { 'err' } else { 'info' }
                    New-ConsoleLogEntry -Message ('stray-' + ($i % 4) + "-${i}: FakeLib: " + $messages[$i % 5]) -Level $level
                }
            }
        }
    }
    $dashboard.ShowElapsed = $false
    $dashboard
}

# Behaviour: show seven samples through the ordinary live session.
$frames = @(0..6 | ForEach-Object -Process { New-StressFrame -Tick $_ })
$screen = New-ConsoleRegion -WidgetId stress -Widget $frames[0]
Invoke-ConsoleDashboard -Dashboard $screen -ArgumentList (, $frames) -ScriptBlock {
    param($Frames)
    foreach ($frame in $Frames) {
        Set-ConsoleRegion -WidgetId stress -Widget $frame
        Wait-ConsoleDashboard -Milliseconds 1000
    }
}
