Import-Module ConsoleMonster

# Record a run, replay it and save it as a page. The dashboard is defined once: the run and the replay
# both draw into its widgets by name.
$dashboard = New-ConsoleDashboard -Title '[bold]Deploy[/]' -Subtitle 'web farm · 4 servers' {
    New-ConsolePhases -Phase 'Stage', 'Swap', 'Verify'
    New-ConsolePanel -Title 'Servers' { New-ConsoleStatusList -Name servers -Item 'web01', 'web02', 'web03', 'web04' }
    New-ConsolePanel -Title 'Log' -Fill { New-ConsoleLog }
}
$recording = Join-Path ([IO.Path]::GetTempPath()) 'deploy.jsonl'

# 1. Run it, writing every update to the recording.
Start-ConsoleDashboard $dashboard -RecordPath $recording {
    foreach ($phase in 'Stage', 'Swap', 'Verify') {
        Update-ConsolePhase -Key $phase -Status Running
        foreach ($server in 'web01', 'web02', 'web03', 'web04') {
            Update-ConsoleStatus -Name servers -Key $server -Status Running -Detail $phase
            Start-Sleep -Milliseconds 250
            Update-ConsoleStatus -Name servers -Key $server -Status Pass -Detail $phase
        }
        Write-ConsoleLog "$phase done on 4 servers"
        Update-ConsolePhase -Key $phase -Status Pass
    }
}

# 2. Play it back at twice the speed, into the same dashboard.
Start-ConsoleReplay $dashboard $recording -Speed 2

# 3. Save it as a page with a player.
Export-ConsoleRecording $dashboard $recording -Format Html -Destination (Join-Path ([IO.Path]::GetTempPath()) 'deploy.html') -Width 100 -Height 16