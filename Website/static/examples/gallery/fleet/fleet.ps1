# Explore a small fleet dashboard with an interactive inventory filter.
[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 2)] [int] $Step = 0,
    [hashtable] $AppOptions = @{}
)

Import-Module ConsoleMonster
$AppOptions.StaticSnapshot = $StaticSnapshot -or ([Console]::IsInputRedirected -and -not $AppOptions.InputSource)

# Data: fictional inputs kept in memory.
$hosts = @(
    [pscustomobject] @{ Host = 'web-01'; Role = 'Web'; Site = 'North'; Status = 'Pass' }
    [pscustomobject] @{ Host = 'web-02'; Role = 'Web'; Site = 'West'; Status = 'Warn' }
    [pscustomobject] @{ Host = 'sql-01'; Role = 'Database'; Site = 'North'; Status = 'Pass' }
    [pscustomobject] @{ Host = 'sql-02'; Role = 'Database'; Site = 'West'; Status = 'Fail' }
    [pscustomobject] @{ Host = 'job-01'; Role = 'Worker'; Site = 'West'; Status = 'Running' }
)
$query = @('', 'sql', 'missing')[$Step]
# Layout: construct the widgets shown on screen.
Show-ConsoleApp {
    New-ConsoleRows -Gap 1 {
        '[bold]Fleet health[/] [muted]· synthetic inventory[/]'
        New-ConsolePanel -Title 'Fleet summary' {
            '[pass]2 healthy[/]  [warn]1 warning[/]  [fail]1 offline[/]  [accent]1 running[/]'
        }

        New-ConsoleTextInput -Name Query -Label Filter -Value $query -Placeholder 'Host, role, site or status'
        # Live updates: rebuild this section when an input or button changes state.
        New-ConsoleBinding {
            param($State)
            $queryText = [string] $State.Query
            $rows = @($hosts | Where-Object {
                ($_.Host, $_.Role, $_.Site, $_.Status -join ' ').IndexOf($queryText, [StringComparison]::OrdinalIgnoreCase) -ge 0
            })
            "$($rows.Count) of $($hosts.Count) hosts"
            New-ConsoleTable -InputObject $rows -Property Host, Role, Site, Status
            if ($rows.Count -eq 0) { '[muted]No matching hosts. Backspace to broaden the filter.[/]' }
        }
    }
} @AppOptions
