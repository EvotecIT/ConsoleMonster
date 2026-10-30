param(
    [int] $Seconds = 20,
    [string] $ResultPath
)
Import-Module ConsoleMonster

# Stress test from PowerShell, all in runspaces the dashboard owns:
# - 32 worker runspaces post lane updates as fast as the cmdlets allow (thousands per second in total);
# - 2 "fake library" runspaces write to Console.Out / Console.Error at random, as .NET libraries do;
# - 2 runspaces call Write-Host in a loop;
# - 1 coordinator samples statistics and CPU.
# Nothing may corrupt the screen: output is captured into the log region and the screen redraws ~15 times a second.

$workers = 0..31 | ForEach-Object { 'w{0:00}' -f $_ }
$dashboard = New-ConsoleDashboard -Title '[bold]Stress test[/]' -Subtitle "32 worker runspaces · Console.WriteLine · Write-Host · $Seconds s" -Content {
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title 'Workers' -Weight 2.2 -Content { New-ConsoleLanes -WidgetId workers -Content {
            foreach ($worker in $workers) { @{ Key = $worker; Total = 250000 } }
        } }
        New-ConsolePanel -Title 'Session' -Content {
            New-ConsoleKeyValue -WidgetId stats -InputObject ([ordered] @{
                    'Updates posted' = 0
                    'Update rate'    = '-'
                    'Frames'         = 0
                    'CPU'            = '-'
                    'Dropped/errors' = '0 / 0'
                })
            New-ConsoleRule 'updates/s'
            New-ConsoleChart -WidgetId rate -Height 3 -Type Sparkline
        }
    }
    New-ConsolePanel -Title 'Captured output' -Fill -Content { New-ConsoleLogRegion }
}

$roles = @('coordinator') + $workers + @('library1', 'library2', 'host1', 'host2')
$results = $roles | Invoke-ConsoleDashboard $dashboard -Parallel -ThrottleLimit $roles.Count -ArgumentList $Seconds {
    param($Role, $Seconds)
    $end = [DateTime]::UtcNow.AddSeconds($Seconds)
    # One seed per role (the sum of its characters), so every run of the stress test makes the same choices.
    $null = Get-Random -SetSeed (([int[]] [char[]] $Role | Measure-Object -Sum).Sum)
    if ($Role -eq 'coordinator') {
        $process = [System.Diagnostics.Process]::GetCurrentProcess()
        $cpuStart = $process.TotalProcessorTime
        $wall = [System.Diagnostics.Stopwatch]::StartNew()
        $lastPosted = 0
        $lastCpu = $cpuStart
        $lastWall = 0.0
        $peak = 0
        while ($wall.Elapsed.TotalSeconds -lt $Seconds -and -not $ConsoleSession.Cancellation.IsCancellationRequested) {
            Start-Sleep -Milliseconds 500
            $process.Refresh()
            $now = $wall.Elapsed.TotalSeconds
            $posted = $ConsoleSession.EventsPosted
            $rate = ($posted - $lastPosted) / ($now - $lastWall)
            $core = ($process.TotalProcessorTime - $lastCpu).TotalMilliseconds / (($now - $lastWall) * 10)
            if ($rate -gt $peak) {
                $peak = $rate
            }
            $lastPosted = $posted
            $lastCpu = $process.TotalProcessorTime
            $lastWall = $now
            Update-Console -WidgetId stats -Key 'Updates posted' -Value ('{0:N0}' -f $posted)
            Update-Console -WidgetId stats -Key 'Update rate' -Value ('{0:N0}/s' -f $rate)
            Update-Console -WidgetId stats -Key 'Frames' -Value ('{0:N0}' -f $ConsoleSession.FramesRendered)
            Update-Console -WidgetId stats -Key 'CPU' -Value (('{0:N0}% ({1:N1} cores)' -f $core, ($core / 100)))
            Update-Console -WidgetId stats -Key 'Dropped/errors' -Value ('{0} / {1}' -f $ConsoleSession.EventsDropped, $ConsoleSession.ApplyErrors)
            Update-Console -WidgetId rate -Value $rate -Append
        }
        $process.Refresh()
        $elapsed = $wall.Elapsed.TotalSeconds
        $cpu = ($process.TotalProcessorTime - $cpuStart).TotalMilliseconds / ($elapsed * 10)
        [pscustomobject] @{
            Runtime     = $PSVersionTable.PSVersion.ToString()
            Seconds     = [math]::Round($elapsed, 1)
            Posted      = $ConsoleSession.EventsPosted
            AverageRate = [math]::Round($ConsoleSession.EventsPosted / $elapsed)
            PeakRate    = [math]::Round($peak)
            Frames      = $ConsoleSession.FramesRendered
            Fps         = [math]::Round($ConsoleSession.FramesRendered / $elapsed, 1)
            CpuOneCore  = [math]::Round($cpu, 1)
            CpuMachine  = [math]::Round($cpu / [Environment]::ProcessorCount, 1)
            Dropped     = $ConsoleSession.EventsDropped
            ApplyErrors = $ConsoleSession.ApplyErrors
        }
    } elseif ($Role -like 'w*') {
        Update-Console -WidgetId workers -Key $Role -Status Running
        while ([DateTime]::UtcNow -lt $end -and -not $ConsoleSession.Cancellation.IsCancellationRequested) {
            $result = if ((Get-Random -Maximum 100) -lt 4) { 'Fail' } else { 'Pass' }
            Update-Console -WidgetId workers -Key $Role -Result $result
        }
        Update-Console -WidgetId workers -Key $Role -Complete
    } elseif ($Role -like 'library*') {
        while ([DateTime]::UtcNow -lt $end -and -not $ConsoleSession.Cancellation.IsCancellationRequested) {
            Start-Sleep -Milliseconds (Get-Random -Minimum 50 -Maximum 300)
            if ((Get-Random -Maximum 10) -eq 0) {
                [Console]::Error.WriteLine("FakeLib ERROR: socket reset $(Get-Random -Maximum 999)")
            } else {
                [Console]::WriteLine("FakeLib: cache miss $(Get-Random -Maximum 9999)")
            }
        }
    } else {
        $n = 0
        while ([DateTime]::UtcNow -lt $end -and -not $ConsoleSession.Cancellation.IsCancellationRequested) {
            $n++
            Write-Host "$Role`: Write-Host line $n"
            Start-Sleep -Milliseconds (Get-Random -Minimum 100 -Maximum 400)
        }
    }
}
$results | Format-List
if ($ResultPath) {
    $results | ConvertTo-Json | Set-Content -Path $ResultPath -Encoding UTF8
}
