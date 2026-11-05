Import-Module ConsoleMonster

# Typed at an interactive prompt, help longer than the screen opens in the document viewer:
# tabs, an outline, folding sections, find (/), Ctrl+K to go to a parameter, and related commands.
#region more
function Test-MailDomain {
    <#
    .SYNOPSIS
    Checks the mail records of a domain: MX, SPF, DMARC, DKIM and MTA-STS.

    .DESCRIPTION
    Resolves each record, compares it with the recommended settings and returns one result per check.

    .PARAMETER Domain
    The domain to check, for example evotec.xyz.

    .PARAMETER Selector
    The DKIM selectors to look up.

    .PARAMETER Timeout
    How long to wait for each DNS answer.

    .PARAMETER Fix
    Writes the recommended records for the checks that fail.

    .EXAMPLE
    Test-MailDomain -Domain evotec.xyz

    Checks one domain with the default DKIM selector.

    .EXAMPLE
    'evotec.xyz', 'evotec.pl' | Test-MailDomain -Selector selector1, selector2 | Show-ConsoleTable

    Checks two domains and both DKIM selectors, shown as one table.

    .EXAMPLE
    Test-MailDomain -Domain evotec.xyz -Fix -WhatIf

    Shows the records -Fix would write without writing them.

    .LINK
    Show-ConsoleTable

    .LINK
    https://consolemonster.dev/
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, ValueFromPipeline)][string] $Domain,
        [string[]] $Selector = @('selector1'),
        [timespan] $Timeout = (New-TimeSpan -Seconds 5),
        [switch] $Fix
    )
    process { [pscustomobject] @{ Domain = $Domain; Checks = 5 } }
}
#endregion

Show-ConsoleHelp Test-MailDomain