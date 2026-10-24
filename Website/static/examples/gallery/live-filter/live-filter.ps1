# Filter a synthetic host table as you type.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
$hosts = @(
    [pscustomobject] @{ Host = 'web-01'; Role = 'Web'; Site = 'North'; Status = 'Pass' }
    [pscustomobject] @{ Host = 'web-02'; Role = 'Web'; Site = 'West'; Status = 'Warn' }
    [pscustomobject] @{ Host = 'sql-01'; Role = 'Database'; Site = 'North'; Status = 'Pass' }
    [pscustomobject] @{ Host = 'sql-02'; Role = 'Database'; Site = 'West'; Status = 'Fail' }
    [pscustomobject] @{ Host = 'job-01'; Role = 'Worker'; Site = 'West'; Status = 'Running' }
)
# Layout: construct the widgets shown on screen.
$content = {
    New-ConsoleRows -Gap 1 -Content {
        '[bold]Live filter[/] [muted]· host inventory[/]'

        New-ConsoleTextInput -Name Query -Label Filter -Value '' -Placeholder 'Host, role, site or status'
        # Rebuild this section when an input or button changes state.
        New-ConsoleBinding -Content {
            param($State)
            $queryText = [string] $State.Query
            $rows = @($hosts | Where-Object -FilterScript {
                    ($_.Host, $_.Role, $_.Site, $_.Status -join ' ').IndexOf($queryText, [StringComparison]::OrdinalIgnoreCase) -ge 0
                })
            "$($rows.Count) of $($hosts.Count) hosts"
            New-ConsoleTable -InputObject $rows -Property Host, Role, Site, Status
            if ($rows.Count -eq 0) { '[muted]No matching hosts. Backspace to broaden the filter.[/]' }
        }
    }
}

# Behaviour: edit controls to update the view; Escape exits the app.
Show-ConsoleApp -Content $content
