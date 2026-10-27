Import-Module ConsoleMonster

# The same twelve servers, each runspace updating its own lane; Write-Host from any of them goes to the log panel.
$servers = 1..12 | ForEach-Object { 'web{0:d2}' -f $_ }
$dashboard = New-ConsoleDashboard -Title 'Patching the web farm' -Content {
    New-ConsolePanel -Title 'Servers' -HeaderRight '12 at once' -Content {
        New-ConsoleLanes -WidgetId servers -Lane $servers -Total 4
    }
    New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion }
}

$servers | Invoke-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 12 {
    param($Server)
    foreach ($step in 'copying', 'stopping', 'swapping', 'starting') {
        Update-Console -WidgetId servers -Key $Server -Status Running -Detail $step
        Start-Sleep -Milliseconds (Get-Random -Minimum 40 -Maximum 240)
        Update-Console -WidgetId servers -Key $Server -Result Pass
    }
    Write-Host "$Server done"
    Update-Console -WidgetId servers -Key $Server -Complete
}