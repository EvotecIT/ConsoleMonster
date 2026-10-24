# Filter a synthetic host table as you type.
[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 2)] [int] $Step = 0,
    [hashtable] $AppOptions = @{}
)

Import-Module ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.
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
Show-ConsoleApp -Content {
    New-ConsoleRows -Gap 1 -Content {
        '[bold]Live filter[/] [muted]· host inventory[/]'

        New-ConsoleTextInput -Key Query -Label Filter -Value $query -Placeholder 'Host, role, site or status'
        # Live updates: rebuild this section when an input or button changes state.
        New-ConsoleBinding -Content {
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
