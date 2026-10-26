using System.Management.Automation;
using ConsoleMonster;
using ConsoleMonster.PowerShell;

// A compiled cmdlet: the current PowerShell runspace owns native commands and callbacks.
[Cmdlet(VerbsLifecycle.Invoke, "HelpExample")]
public sealed class HelpExample : PSCmdlet
{
    protected override void ProcessRecord()
    {
        InvokeCommand.InvokeScript(@"function Test-MailDomain {
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
            'evotec.xyz', 'evotec.pl' | Test-MailDomain -Selector selector1, selector2 | Show-ConsoleTable
            #>
            [CmdletBinding()]
            param(
                [Parameter(Mandatory, ValueFromPipeline)][string] $Domain,
                [string[]] $Selector = @('selector1'),
                [timespan] $Timeout = (New-TimeSpan -Seconds 5)
            )
            process { [pscustomobject] @{ Domain = $Domain; Checks = 5 } }
        }");
        if (ConsoleHostHelp.TryCreate("Test-MailDomain", InvokeCommand, out var widget,
            out var error) && widget != null)
        {
            Terminal.Write(widget);
        }
        else if (error != null)
        {
            WriteError(error);
        }
    }
}
