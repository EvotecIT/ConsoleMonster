Import-Module ConsoleMonster

# One lane per worker: its own bar, counts and current detail.
New-ConsoleLanes -WidgetId servers -Content {
    [pscustomobject] @{ Key = 'DC01'; Total = 40; Value = 40; Passed = 40; Status = 'Pass' }
    [pscustomobject] @{ Key = 'DC02'; Total = 40; Value = 22; Passed = 21; Warned = 1
        Status = 'Running'; Detail = 'replication' }
    [pscustomobject] @{ Key = 'DC03'; Total = 40; Value = 9; Passed = 7; Failed = 2
        Status = 'Running'; Detail = 'DNS records' }
    [pscustomobject] @{ Key = 'DC04'; Total = 40 }
} | Show-Console
