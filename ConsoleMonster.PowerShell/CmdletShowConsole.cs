using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows objects, text and widgets in a readable view that fits the terminal.</para>
    /// <para type="description">Pipe objects to choose a view automatically, or use View for a table, list, object tree, source code, JSON, error or Markdown. Property chooses fields; Width sets the available space. Use New-ConsoleChart, New-ConsoleRule and New-ConsoleTree to construct those widgets. AsWidget returns a view for layout or Export-ConsoleFrame; PassThru returns the original objects after showing them.</para>
    /// </summary>
    /// <example><para>Show a few running processes</para><code>Get-Process | Select-Object -First 5 | Show-Console</code></example>
    /// <example><para>Read selected fields as a list</para><code>Get-Process | Select-Object -First 3 | Show-Console -View List -Property ProcessName, Id, WorkingSet64</code></example>
    /// <example><para>Inspect a file object's members with a bounded tree</para><code>Get-Item . | Show-Console -View Tree -Depth 1 -Property Name, FullName, LastWriteTime</code></example>
    /// <example><para>Show a PowerShell snippet with line numbers</para><code>Show-Console -View Code -Language PowerShell -LineNumbers -Content {
    ///     'Get-Process | Select-Object -First 5'
    /// }</code></example>
    /// <example><para>Compose a panel with native PowerShell script blocks</para><code>Show-Console -Content {
    ///     New-ConsolePanel -Title 'Build summary' -Content {
    ///         New-ConsoleRows -Gap 1 -Content {
    ///             New-ConsoleText 'All checks passed' -Role Pass
    ///             New-ConsoleRule -Title 'Ready to review'
    ///         }
    ///     }
    /// }</code></example>
    /// <example><para>Return a plain-text document for another pipeline command</para><code>Get-Process | Select-Object -First 3 |
    ///     Show-Console -View List -Property ProcessName, Id -AsWidget |
    ///     Export-ConsoleFrame -Format Text</code></example>
    [Cmdlet(VerbsCommon.Show, "Console", DefaultParameterSetName = "Objects")]
    public sealed partial class CmdletShowConsole : ConsoleDisplayCmdlet {
        /// <summary><para type="description">Auto, Table, List, Tree, Code, Json, Error or Markdown. Tree inspects object members; New-ConsoleTree builds node hierarchies.</para></summary>
        [Parameter, ValidateSet("Auto", "Table", "List", "Tree", "Code", "Json", "Error", "Markdown")]
        public string View { get; set; } = "Auto";
        /// <inheritdoc />
        protected override string RequestedView => View;
    }
}
