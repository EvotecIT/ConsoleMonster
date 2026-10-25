using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Displays the table representation through the shared Show-Console implementation.
    /// </para>
    /// <para type="description">Table and List are idiomatic shortcuts to the same view construction and output path. AsWidget and AsString capture the result; export uses Export-ConsoleFrame.</para>
    /// </summary>
    /// <example><para>Display pipeline input</para><code>Get-Service | Show-ConsoleTable</code></example>
    [Cmdlet(VerbsCommon.Show, "ConsoleTable", DefaultParameterSetName = "Objects")]
    public sealed partial class CmdletShowConsoleTable : ConsoleDisplayCmdlet {
        /// <inheritdoc />
        protected override string RequestedView => "Table";
    }
}
