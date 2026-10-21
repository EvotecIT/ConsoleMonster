Import-Module ConsoleMonster

# Three servers deployed in parallel. Each runspace updates its own lane by name; Write-Host from any of them lands
# in the log panel instead of writing over the screen.
$dashboard = New-ConsoleDashboard -Title 'Deploy' {
    New-ConsolePanel -Title 'Servers' -Caption '3 workers' { New-ConsoleLanes -Name servers -Lane 'SRV1', 'SRV2', 'SRV3' -Total 4 }
    New-ConsolePanel -Title 'Log' -Fill { New-ConsoleLog }
}

'SRV1', 'SRV2', 'SRV3' | Start-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 3 {
    param($Server)
    $null = Get-Random -SetSeed ([int] $Server[-1])
    foreach ($step in 'copying files', 'stopping the site', 'swapping folders', 'starting the site') {
        Update-ConsoleLane -Name servers -Key $Server -Status Running -Detail $step
        Start-Sleep -Milliseconds (400 + (Get-Random -Minimum 0 -Maximum 500))
        Update-ConsoleLane -Name servers -Key $Server -Result Pass
    }
    Write-Host "$Server done"
    Update-ConsoleLane -Name servers -Key $Server -Complete
}