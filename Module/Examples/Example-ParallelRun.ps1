Import-Module ConsoleMonster

# A parallel assessment run, live: every server is processed in its own runspace (4 at a time).
# Each runspace updates its lane, records rule results and writes to the log with Write-Host / Write-Warning.

$servers = 1..10 | ForEach-Object { 'DC{0:00}.ad.adatum.com' -f $_ }
$rules = 'LDAP signing required', 'SMB signing enforced', 'Replication latency per partner', 'DNS scavenging',
'Time source hierarchy', 'Kerberos AES-only encryption', 'Audit policy baseline', 'Print Spooler disabled'

$dashboard = New-ConsoleDashboard -Title '[bold]Nightly checks[/]' -Subtitle "ad.adatum.com · $($servers.Count) domain controllers" -Gap 1 -Content {
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title 'Domain controllers' -Weight 2.3 -Content { New-ConsoleLanes -WidgetId lanes -Content {
            foreach ($server in $servers) { @{ Key = $server; Total = $rules.Count } }
        } }
        New-ConsolePanel -Title 'Results' -Content {
            New-ConsoleChart -WidgetId results -InputObject ([ordered] @{ Passed = 0; Warnings = 0; Failed = 0 }) -Type Stacked
            New-ConsoleRule
            New-ConsoleChart -WidgetId pace -Title 'rules' -Height 3 -Type Sparkline
        }
    }
    New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion }
}

$servers | Invoke-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 4 -ArgumentList (, $rules) {
    param($Server, $Rules)
    Update-Console -Key $Server -Status Running -Detail $Rules[0] -WidgetId 'lanes'
    foreach ($rule in $Rules) {
        Start-Sleep -Milliseconds (Get-Random -Minimum 150 -Maximum 700)
        $roll = Get-Random -Maximum 100
        $result = if ($roll -lt 6) { 'Fail' } elseif ($roll -lt 18) { 'Warn' } else { 'Pass' }
        Update-Console -Key $Server -Result $result -Detail $rule -WidgetId 'lanes'
        Update-Console -WidgetId results -Key @{ Pass = 'Passed'; Warn = 'Warnings'; Fail = 'Failed' }[$result] -Increment 1
        Update-Console -WidgetId pace -Value (Get-Random -Minimum 4 -Maximum 12) -Append
        if ($result -eq 'Fail') {
            Write-Warning "$Server`: $rule failed"
        }
    }
    Update-Console -Key $Server -Complete -WidgetId 'lanes'
    Write-Host "$Server finished"
}
