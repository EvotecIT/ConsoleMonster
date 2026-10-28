using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Displays the table representation through the shared Show-Console implementation.
    /// </para>
    /// <para type="description">Three ways to use ConsoleMonster output: Automatic: Enable-ConsoleFormatting makes ordinary output readable for known and registered types. Explicit: pipe any objects to Show-ConsoleTable to choose columns, a title and grouping, or to Show-ConsoleObject to explore their members. These commands do not change session formatting; Show-ConsoleObject -StaticSnapshot avoids interactive input in scripts. Native Format-Table, Format-List and Format-Wide: FormatCommands restyles direct console output while keeping PowerShell's column choices; it is included by default or can be selected with -Feature FormatCommands. You do not need both automatic and explicit formatting. The -Feature parameter requires ConsoleMonster 2.1.0 or later.</para>
    /// <para type="description">Table and List are idiomatic shortcuts to the same view construction and output path. AsWidget and AsString capture the result; export uses Export-ConsoleFrame.</para>
    /// </summary>
    /// <example><para>Display pipeline input</para><code>Get-Service | Show-ConsoleTable</code></example>
    [Cmdlet(VerbsCommon.Show, "ConsoleTable", DefaultParameterSetName = "Objects")]
    public sealed partial class CmdletShowConsoleTable : ConsoleDisplayCmdlet {
        /// <inheritdoc />
        protected override string RequestedView => "Table";
    }
}
