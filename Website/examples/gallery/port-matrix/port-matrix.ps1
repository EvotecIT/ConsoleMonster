# See which application ports are healthy across a group of hosts.
param([switch] $StaticSnapshot, [ValidateRange(0, 0)][int] $Step = 0)
Import-Module ConsoleMonster

# Data: synthetic observations; no DNS, ping, socket or network operation runs here.
$hosts = @('web-01', 'web-02', 'api-01', 'sql-01', 'cache-01')
$ports = @('HTTPS 443', 'SQL 5432', 'Cache 6379')
$states = @(
    'Pass', 'Skip', 'Skip'
    'Pass', 'Skip', 'Skip'
    'Pass', 'Fail', 'Warn'
    'Skip', 'Pass', 'Skip'
    'Skip', 'Skip', 'Pass'
)

# Layout: a compact map provides the overview; a table explains exceptions.
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule '[bold]Application connectivity[/] [muted]· core services[/]'
    New-ConsolePanel -Title ($ports -join '   |   ') -Content {
        New-ConsoleCellMap -Status $states -Columns 3 -RowLabels $hosts -LegendLines 2
    }
    New-ConsolePanel -Title 'Exceptions to investigate' -Content {
        New-ConsoleStatusList -Content {
            New-ConsoleStatusItem 'api-01 → SQL 5432' -Status Fail -Detail 'Connection refused · check listener and access policy'
            New-ConsoleStatusItem 'api-01 → Cache 6379' -Status Warn -Detail '180 ms · expected under 50 ms'
        }
    }
    '[muted]Skipped cells are ports that host does not need.[/]'
}

# Behaviour: print the collected observations once; collectors belong before the layout.
$report | Show-Console
