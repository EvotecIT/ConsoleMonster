Import-Module ConsoleMonster

# Record a run, replay it and save it as a page. The dashboard is defined once: the run and the replay
# both draw into its widgets by name.
$dashboardParameters = @{
    Title = '[bold]Deploy[/]'
    Subtitle = 'web farm · 4 servers'
}
$dashboard = New-ConsoleDashboard @dashboardParameters -Content {
    New-ConsoleStatusList -Layout Strip -WidgetId phases -Content { 'Stage', 'Swap', 'Verify' }
    New-ConsolePanel -Title 'Servers' -Content {
        New-ConsoleStatusList -WidgetId servers -Content { 'web01', 'web02', 'web03', 'web04' }
    }
    New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion }
}
$recording = Join-Path ([IO.Path]::GetTempPath()) 'deploy.jsonl'

# 1. Run it, writing every update to the recording.
Invoke-ConsoleDashboard $dashboard -RecordingPath $recording {
    foreach ($phase in 'Stage', 'Swap', 'Verify') {
        Update-Console -Key $phase -Status Running -WidgetId 'phases'
        foreach ($server in 'web01', 'web02', 'web03', 'web04') {
            Update-Console -WidgetId servers -Key $server -Status Running -Detail $phase
            Wait-ConsoleDashboard -Seconds 0.25
            Update-Console -WidgetId servers -Key $server -Status Pass -Detail $phase
        }
        Write-ConsoleLogEntry "$phase done on 4 servers"
        Update-Console -Key $phase -Status Pass -WidgetId 'phases'
    }
}

# 2. Play it back at twice the speed, into the same dashboard.
Show-ConsoleReplay -PlaybackMode Automatic $dashboard $recording -Speed 2

# 3. Save it as a page with a player.
$recordingParameters = @{
    Format = 'Html'
    Path = (Join-Path ([IO.Path]::GetTempPath()) 'deploy.html')
    Width = 100
    Height = 16
}
$null = Export-ConsoleRecording $dashboard $recording @recordingParameters
