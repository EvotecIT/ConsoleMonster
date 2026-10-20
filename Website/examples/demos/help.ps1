# Show-ConsoleHelp reads the help PowerShell already has: here a function's comment-based help.
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

    .EXAMPLE
    Test-MailDomain -Domain evotec.xyz

    .EXAMPLE
    'evotec.xyz', 'evotec.pl' | Test-MailDomain -Selector selector1, selector2 | Format-ConsoleTable
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline)][string] $Domain,
        [string[]] $Selector = @('selector1'),
        [timespan] $Timeout = (New-TimeSpan -Seconds 5)
    )
    process { [pscustomobject] @{ Domain = $Domain; Checks = 5 } }
}
#endregion
#region hidden
Write-Host 'PS> Show-ConsoleHelp Test-MailDomain' -ForegroundColor Yellow
Start-Sleep -Milliseconds 900
#endregion
Show-ConsoleHelp Test-MailDomain
