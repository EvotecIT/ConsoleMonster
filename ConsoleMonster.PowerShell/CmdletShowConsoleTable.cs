using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows objects as a table with fitted columns, readable values and optional grouping.</para>
    /// <para type="description">Pipe objects and choose columns with Property, including calculated properties. GroupBy adds group headings, ShowTotals sums numeric columns, and Wrap keeps long text readable. Narrow output uses labelled cards below CardsBelow. This command displays the table immediately. Use New-ConsoleTable to place a table in a layout or pass it to Export-ConsoleFrame. Enable-ConsoleFormatting is the separate opt-in for ordinary prompt output.</para>
    /// </summary>
    /// <example><para>Show processes with the columns you need</para><code>Get-Process | Select-Object -First 8 |
    ///     Show-ConsoleTable -Property ProcessName, Id, WorkingSet64 -Title 'Running processes'</code></example>
    /// <example><para>Group a small inventory and total its numeric columns</para><code>$inventory = @(
    ///     [pscustomobject]@{ Site = 'East'; Server = 'WEB01'; Cores = 4 }
    ///     [pscustomobject]@{ Site = 'East'; Server = 'SQL01'; Cores = 8 }
    ///     [pscustomobject]@{ Site = 'West'; Server = 'WEB02'; Cores = 4 }
    /// )
    /// $inventory | Show-ConsoleTable -Property Site, Server, Cores -GroupBy Site -ShowTotals</code></example>
    /// <example><para>Calculate a memory column in megabytes</para><code>Get-Process | Select-Object -First 5 | Show-ConsoleTable -Property ProcessName, @{
    ///     Name = 'MemoryMB'
    ///     Expression = { [math]::Round($_.WorkingSet64 / 1MB, 1) }
    /// }</code></example>
    /// <example><para>Wrap long notes in a narrow report</para><code>@(
    ///     [pscustomobject]@{ Task = 'Backup'; Notes = 'Verified the report and checked the retained files.' }
    ///     [pscustomobject]@{ Task = 'Review'; Notes = 'Ready for the next scheduled maintenance window.' }
    /// ) | Show-ConsoleTable -Property Task, Notes -Width 60 -CardsBelow 0 -Wrap</code></example>
    /// <example><para>Give health states a visible status marker</para><code>@(
    ///     [pscustomobject]@{ Check = 'Storage'; Status = 'Pass'; Detail = '42 GB free' }
    ///     [pscustomobject]@{ Check = 'Queue'; Status = 'Warn'; Detail = '12 items waiting' }
    /// ) | Show-ConsoleTable -Property Check, Status, Detail -StatusProperty Status</code></example>
    /// <example><para>Display objects and keep them in the pipeline</para><code>Get-Process | Select-Object -First 5 |
    ///     Show-ConsoleTable -Property ProcessName, Id -PassThru |
    ///     Measure-Object</code></example>
    [Cmdlet(VerbsCommon.Show, "ConsoleTable", DefaultParameterSetName = "Objects")]
    public sealed partial class CmdletShowConsoleTable : ConsoleDisplayCmdlet {
        /// <inheritdoc />
        protected override string RequestedView => "Table";
    }
}
