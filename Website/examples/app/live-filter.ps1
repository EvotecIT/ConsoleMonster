<#
.SYNOPSIS
Filters a synthetic host inventory after every edit.
.DESCRIPTION
Builds a native PowerShell application with a Query input, matching count and
ordinary table. Matching is literal and case-insensitive across all columns.
Import ConsoleMonster before running this example.
.PARAMETER AppOptions
Optional Show-ConsoleApp parameters, such as StaticSnapshot for redirected output.
.OUTPUTS
None. The application draws to the terminal or the supplied capture.
.EXAMPLE
./live-filter.ps1
.EXAMPLE
./live-filter.ps1 -AppOptions @{ StaticSnapshot = $true }
#>
[CmdletBinding()]
param([hashtable] $AppOptions = @{})

Import-Module ConsoleMonster

#region more
$hosts = @(
    [pscustomobject] @{ Host = 'WEB01'; Role = 'Web'; Site = 'North'; Status = 'Pass' }
    [pscustomobject] @{ Host = 'WEB02'; Role = 'Web'; Site = 'West'; Status = 'Warn' }
    [pscustomobject] @{ Host = 'SQL01'; Role = 'Database'; Site = 'North'; Status = 'Pass' }
    [pscustomobject] @{ Host = 'SQL02'; Role = 'Database'; Site = 'West'; Status = 'Fail' }
    [pscustomobject] @{ Host = 'FS01'; Role = 'Files'; Site = 'North'; Status = 'Pass' }
    [pscustomobject] @{ Host = 'JOB01'; Role = 'Worker'; Site = 'West'; Status = 'Running' }
)
$matchingHosts = {
    param($State)
    $query = [string] $State.Query
    $hosts | Where-Object {
        $_.Host.IndexOf($query, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $_.Role.IndexOf($query, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $_.Site.IndexOf($query, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $_.Status.IndexOf($query, [StringComparison]::OrdinalIgnoreCase) -ge 0
    }
}
#endregion

Show-ConsoleApp -Content {
    New-ConsoleRows -Gap 1 -Content {
        '[bold]Host inventory[/] [muted]· synthetic data[/]'
        New-ConsoleTextInput -Key Query -Label Query -Placeholder 'Host, role, site or status'
        New-ConsoleBinding -Content {
            param($State)
            $matches = @(& $matchingHosts $State)
            "[bold]$($matches.Count)[/] of $($hosts.Count) hosts"
            $matches |
                New-ConsoleTable -Property Host, Role, Site,
                    Status -StatusProperty Status -NoHeaderSeparator
            if ($matches.Count -eq 0) {
                '[muted]No matching hosts. Backspace to broaden the query.[/]'
            }
        }
    }
} @AppOptions
