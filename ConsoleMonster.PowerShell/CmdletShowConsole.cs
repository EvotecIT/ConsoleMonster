using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Displays pipeline input in its automatic or explicit representation.
    /// </para>
    /// <para type="description">Table and List are idiomatic shortcuts to the same view construction and output path. AsWidget and AsString capture the result; export uses Export-ConsoleFrame.</para>
    /// </summary>
    /// <example><para>Display pipeline input</para><code>Get-Service | Show-Console</code></example>
    [Cmdlet(VerbsCommon.Show, "Console", DefaultParameterSetName = "Objects")]
    public sealed partial class CmdletShowConsole : ConsoleDisplayCmdlet {
        /// <summary><para type="description">Auto, Table, List, Tree, Chart, Code, Json, Error, Markdown or Rule.</para></summary>
        [Parameter, ValidateSet("Auto", "Table", "List", "Tree", "Chart", "Code", "Json", "Error", "Markdown", "Rule")]
        public string View { get; set; } = "Auto";
        /// <inheritdoc />
        protected override string RequestedView => View;
    }
}
