# Stress test: thirty-two workers, session telemetry and captured tool output.
param(
    [switch]$StaticSnapshot,
    [ValidateRange(0, 6)][int]$Step = 0
)

Import-Module ConsoleMonster

# Data: fixed snapshot samples; the bounded live workload touches no services or files.
$workers = 32
$seconds = 6
$actions = 'parsing', 'resolving', 'evaluating', 'scoring', 'writing'
$sampleRates = @(0..47 | ForEach-Object { 18000 + ($_ * 7919 % 11000) })
$sampleCpu = @(0..47 | ForEach-Object { 42 + ($_ * 17 % 74) })
$messages = 'cache miss for key 412', 'retrying request 28 (attempt 2)', 'connection pool at 76% capacity', 'GC pause 8 ms', 'flushed 960 records'
$culture = [cultureinfo]::InvariantCulture
$snapshot = $StaticSnapshot -or [Console]::IsInputRedirected
$tick = if ($snapshot) { $Step } else { 0 }
$samples = $tick * 8
$posted = $tick * 128000L

# Layout: all thirty-two lanes, the update sparkline, CPU chart and session counters remain visible.
$lanes = New-ConsoleLanes -Name workers {
    foreach ($id in 0..31) {
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
$dashboard = New-ConsoleDashboard -Title '[bold]Stress test[/]' -Subtitle '32 workers · captured tool output · 6s' -Footer '[muted]32 workers    15 frames/s    Ctrl+C stop[/]' {
    New-ConsoleColumns {
        New-ConsolePanel -Title Workers -Weight 2.2 { $lanes }
        New-ConsolePanel -Title Session {
            New-ConsoleKeyValue -Name stats -Data $stats
            New-ConsoleRule 'updates/s'
            $values = @($sampleRates | Select-Object -First $samples)
            New-ConsoleSparkline -Name rate -Height 3 -Unit '/s' -Value $values
            New-ConsoleRule 'CPU % (100% = one core)'
            New-ConsoleLineChart -Name cpu -Height 6 -Minimum 0 -Unit '%' {
                New-ConsoleChartSeries cpu -Tone Accent2 -Value @($sampleCpu | Select-Object -First $samples)
            }
        }
    }
    New-ConsolePanel -Title 'Captured output' -Fill {
        New-ConsoleLog -Height 6 -NoTime {
            for ($i = 0; $i -lt $tick * 4; $i++) {
                $level = if ($i % 7 -eq 0) { 'err' } else { 'info' }
                New-ConsoleLogEntry ('stray-' + ($i % 4) + "-${i}: FakeLib: " + $messages[$i % 5]) -Level $level
            }
        }
    }
}
$dashboard.ShowClock = $false

# Behaviour: each runspace posts native lane updates; worker zero samples real session and process counters.
if ($snapshot) {
    $dashboard | Write-ConsoleWidget
    return
}
0..31 | Start-ConsoleDashboard $dashboard -Parallel -ThrottleLimit $workers -ArgumentList $seconds, $actions, $messages {
    param($Id, $Seconds, $Actions, $Messages)
    $culture = [cultureinfo]::InvariantCulture
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $null = Get-Random -SetSeed ($Id * 7919)
    $key = "w$Id"
    $done = 0
    $lastSample = 0.0
    $lastPosted = 0L
    $process = Get-Process -Id $PID
    $lastCpu = $process.TotalProcessorTime.TotalSeconds
    Update-ConsoleLane -Name workers -Key $key -Status Running -Detail starting
    try {
        while ($watch.Elapsed.TotalSeconds -lt $Seconds -and -not $ConsoleSession.Cancellation.IsCancellationRequested) {
            # Batch equal outcomes without giving up result counters or queue coalescing.
            $fail = (Get-Random -Minimum 0 -Maximum 3)
            $warn = (Get-Random -Minimum 1 -Maximum 5)
            $pass = 40 - $fail - $warn
            Update-ConsoleLane -Name workers -Key $key -Result Pass -Steps $pass -Detail ($Actions[$done % 5] + " item $done")
            if ($warn) { Update-ConsoleLane -Name workers -Key $key -Result Warn -Steps $warn }
            if ($fail) { Update-ConsoleLane -Name workers -Key $key -Result Fail -Steps $fail }
            $done += 40
            if ($Id -lt 4 -and $done % 400 -eq 0) {
                $line = "stray-${Id}-${done}: FakeLib: " + $Messages[($done / 400) % $Messages.Count]
                if ($done % 2800 -eq 0) { Write-Warning $line } else { Write-Host $line }
            }
            $now = $watch.Elapsed.TotalSeconds
            if ($Id -eq 0 -and $now - $lastSample -ge 0.25) {
                $process.Refresh()
                $cpuNow = $process.TotalProcessorTime.TotalSeconds
                $interval = $now - $lastSample
                $core = ($cpuNow - $lastCpu) / $interval * 100
                $posted = $ConsoleSession.EventsPosted
                $rate = ($posted - $lastPosted) / $interval
                Add-ConsoleValue -Name rate -Value $rate
                Add-ConsoleValue -Name cpu -Key cpu -Value $core
                Set-ConsoleValue -Name stats -Key 'Updates posted' -Value $posted.ToString('#,0', $culture)
                Set-ConsoleValue -Name stats -Key 'Updates applied' -Value $ConsoleSession.EventsApplied.ToString('#,0', $culture)
                Set-ConsoleValue -Name stats -Key 'Update rate' -Value ($rate.ToString('#,0', $culture) + '/s')
                Set-ConsoleValue -Name stats -Key Frames -Value $ConsoleSession.FramesRendered.ToString('#,0', $culture)
                Set-ConsoleValue -Name stats -Key 'Frame rate' -Value (($ConsoleSession.FramesRendered / $now).ToString('0.0', $culture) + ' fps')
                Set-ConsoleValue -Name stats -Key CPU -Value ($core.ToString('#,0', $culture) + '%')
                Set-ConsoleValue -Name stats -Key 'CPU share' -Value (($core / [Environment]::ProcessorCount).ToString('0.0', $culture) + '% of ' + [Environment]::ProcessorCount + ' cores')
                Set-ConsoleValue -Name stats -Key 'Stray writes' -Value 'capturing 4 writers'
                Set-ConsoleValue -Name stats -Key 'Dropped / errors' -Value ($ConsoleSession.EventsDropped.ToString() + ' / ' + $ConsoleSession.ApplyErrors)
                $lastSample = $now
                $lastCpu = $cpuNow
                $lastPosted = $posted
            }
            Start-Sleep -Milliseconds 8
        }
        Update-ConsoleLane -Name workers -Key $key -Status Pass
    } finally {
        $process.Dispose()
    }
}
