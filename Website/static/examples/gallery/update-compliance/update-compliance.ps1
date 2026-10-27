# Check monthly patch compliance across a server fleet and call out overdue hosts.
Import-Module -Name ConsoleMonster

# Data: fictional servers and a fixed report date; no update agent or WSUS server is queried.
$servers = @(
    [pscustomobject] @{ Host = 'dc-01'; Ring = 'Pilot'; State = 'Compliant'; Missing = 0
        LastPatched = '2026-10-15'; Days = 5; Status = 'Pass' }
    [pscustomobject] @{ Host = 'dc-02'; Ring = 'Broad'; State = 'Compliant'; Missing = 0
        LastPatched = '2026-10-16'; Days = 4; Status = 'Pass' }
    [pscustomobject] @{ Host = 'fs-01'; Ring = 'Broad'; State = 'Pending reboot'; Missing = 0
        LastPatched = '2026-10-14'; Days = 6; Status = 'Warn' }
    [pscustomobject] @{ Host = 'fs-02'; Ring = 'Broad'; State = 'Compliant'; Missing = 0
        LastPatched = '2026-10-16'; Days = 4; Status = 'Pass' }
    [pscustomobject] @{ Host = 'sql-01'; Ring = 'Broad'; State = 'Missing updates'; Missing = 3
        LastPatched = '2026-09-12'; Days = 38; Status = 'Fail' }
    [pscustomobject] @{ Host = 'sql-02'; Ring = 'Broad'; State = 'Compliant'; Missing = 0
        LastPatched = '2026-10-17'; Days = 3; Status = 'Pass' }
    [pscustomobject] @{ Host = 'web-01'; Ring = 'Pilot'; State = 'Compliant'; Missing = 0
        LastPatched = '2026-10-15'; Days = 5; Status = 'Pass' }
    [pscustomobject] @{ Host = 'web-02'; Ring = 'Broad'; State = 'Pending reboot'; Missing = 0
        LastPatched = '2026-10-15'; Days = 5; Status = 'Warn' }
    [pscustomobject] @{ Host = 'app-01'; Ring = 'Broad'; State = 'Compliant'; Missing = 0
        LastPatched = '2026-10-16'; Days = 4; Status = 'Pass' }
    [pscustomobject] @{ Host = 'app-02'; Ring = 'Broad'; State = 'Failed'; Missing = 5
        LastPatched = '2026-08-19'; Days = 62; Status = 'Fail' }
    [pscustomobject] @{ Host = 'print-01'; Ring = 'Broad'; State = 'Missing updates'; Missing = 2
        LastPatched = '2026-10-02'; Days = 18; Status = 'Warn' }
    [pscustomobject] @{ Host = 'hv-01'; Ring = 'Pilot'; State = 'Compliant'; Missing = 0
        LastPatched = '2026-10-15'; Days = 5; Status = 'Pass' }
)
$states = @(
    [pscustomobject] @{ Label = 'Compliant'; Role = 'Pass' }
    [pscustomobject] @{ Label = 'Pending reboot'; Role = 'Info' }
    [pscustomobject] @{ Label = 'Missing updates'; Role = 'Warn' }
    [pscustomobject] @{ Label = 'Failed'; Role = 'Fail' }
)

# Behaviour: count each state, keep the hosts that need work and flag anything unpatched for 30+ days.
$byState = foreach ($state in $states) {
    $count = @($servers | Where-Object -Property State -EQ -Value $state.Label).Count
    [pscustomobject] @{ Label = $state.Label; Value = $count; Role = $state.Role }
}
$attention = @($servers | Where-Object -Property State -NE -Value 'Compliant')
$overdue = @($servers | Where-Object -Property Days -GT -Value 30)

# Layout: the fleet split first, then the hosts to fix, then the overdue call-out.
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule -Title '[bold]Windows Update compliance[/] [muted]· 2026-10 cumulative · as of 2026-10-20[/]'
    New-ConsolePanel -Title "Fleet · $($servers.Count) servers" -Content {
        $byState | New-ConsoleChart -Type Stacked
    }
    New-ConsoleTable -InputObject $attention -Property Host, Ring, State, Missing, LastPatched,
        Days, Status -Expand
    New-ConsolePanel -Title 'Overdue · unpatched for more than 30 days' -Content {
        New-ConsoleStatusList -Content {
            foreach ($server in $overdue) {
                [pscustomobject] @{ Label = $server.Host; Status = 'Fail'
                    Detail = "$($server.Days) days · $($server.State.ToLowerInvariant()) · $($server.Missing) missing" }
            }
        }
    }
}

# Behaviour: print once; schedule the overdue hosts into the next maintenance window.
$report | Show-Console
