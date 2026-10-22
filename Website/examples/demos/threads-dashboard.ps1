Import-Module ConsoleMonster

# The same twelve servers, each runspace updating its own lane; Write-Host from any of them goes to the log panel.
$servers = 1..12 | ForEach-Object { 'web{0:d2}' -f $_ }
$dashboard = New-ConsoleDashboard -Title 'Patching the web farm' {
    New-ConsolePanel -Title 'Servers' -Caption '12 at once' { New-ConsoleLanes -Name servers -Lane $servers -Total 4 }
    New-ConsolePanel -Title 'Log' -Fill { New-ConsoleLog }
}

$servers | Start-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 12 {
    param($Server)
    foreach ($step in 'copying', 'stopping', 'swapping', 'starting') {
        Update-ConsoleLane -Name servers -Key $Server -Status Running -Detail $step
        Start-Sleep -Milliseconds (Get-Random -Minimum 40 -Maximum 240)
        Update-ConsoleLane -Name servers -Key $Server -Result Pass
    }
    Write-Host "$Server done"
    Update-ConsoleLane -Name servers -Key $Server -Complete
}