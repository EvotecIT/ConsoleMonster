Import-Module ConsoleMonster

# A parallel assessment run, live: every server is processed in its own runspace (4 at a time).
# Each runspace updates its lane, records rule results and writes to the log with Write-Host / Write-Warning.

$servers = 1..10 | ForEach-Object { 'DC{0:00}.ad.adatum.com' -f $_ }
$rules = 'LDAP signing required', 'SMB signing enforced', 'Replication latency per partner', 'DNS scavenging',
'Time source hierarchy', 'Kerberos AES-only encryption', 'Audit policy baseline', 'Print Spooler disabled'

$dashboard = New-ConsoleDashboard -Title '[bold]Nightly checks[/]' -Subtitle "ad.adatum.com · $($servers.Count) domain controllers" -Gap 1 -Content {
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title 'Domain controllers' -Weight 2.3 -Content { New-ConsoleLanes -WidgetId lanes -Lane $servers -Total $rules.Count }
        New-ConsolePanel -Title 'Results' -Content {
            New-ConsoleBarChart -Stacked -WidgetId results -InputObject ([ordered] @{ Passed = 0; Warnings = 0; Failed = 0 })
            New-ConsoleRule
            New-ConsoleSparkline -WidgetId pace -Label 'rules' -Height 3
        }
    }
    New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion }
}

$servers | Invoke-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 4 -ArgumentList (, $rules) {
    param($Server, $Rules)
    Update-ConsoleLane -Key $Server -Status Running -Detail $Rules[0]
    foreach ($rule in $Rules) {
        Start-Sleep -Milliseconds (Get-Random -Minimum 150 -Maximum 700)
        $roll = Get-Random -Maximum 100
        $result = if ($roll -lt 6) { 'Fail' } elseif ($roll -lt 18) { 'Warn' } else { 'Pass' }
        Update-ConsoleLane -Key $Server -Result $result -Detail $rule
        Add-ConsoleValue -WidgetId results -Key @{ Pass = 'Passed'; Warn = 'Warnings'; Fail = 'Failed' }[$result] -Value 1
        Add-ConsoleValue -WidgetId pace -Value (Get-Random -Minimum 4 -Maximum 12)
        if ($result -eq 'Fail') {
            Write-Warning "$Server`: $rule failed"
        }
    }
    Update-ConsoleLane -Key $Server -Complete
    Write-Host "$Server finished"
}
