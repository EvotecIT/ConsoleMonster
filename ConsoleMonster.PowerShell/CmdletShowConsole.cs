using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Shows objects, text and widgets in a readable view that fits the terminal.</summary>
    /// <remarks>
    /// <para>Pipe objects to choose a view automatically, or use View for a table, list, object tree, source code, JSON, error or Markdown. Property chooses fields and Width sets the available space. Use New-ConsoleChart, New-ConsoleRule and New-ConsoleTree to construct those widgets.</para>
    /// <para>Markdown document viewer
    /// ------------------------</para>
    /// <para>When Markdown with headings exceeds the screen height and goes directly to a rich terminal, the document viewer opens. It shows an outline and lets you fold sections. Press Ctrl+K to jump to a heading, / or Ctrl+F to search, and q to return to PowerShell.</para>
    /// <para>Markdown pager
    /// --------------</para>
    /// <para>Long Markdown without headings uses a scrolling pager in the same terminal conditions. Use Up and Down to scroll, PgUp and PgDn to move a page, and Home or End to jump to either end. Press / or Ctrl+F to search. After closing the search bar, use n and Shift+N to move between matches. Press q to return to PowerShell.</para>
    /// <para>Direct output and widget output
    /// -------------------------------</para>
    /// <para>Use NoPager to print Markdown directly. Short Markdown and output routed away from the interactive terminal also use direct output. AsWidget returns the view for composition or export. PassThru returns the original objects on the direct rendering path.</para>
    /// </remarks>
    /// <example><para>Show a few running processes</para><code>Get-Process | Select-Object -First 5 | Show-Console</code></example>
    /// <example><para>Read selected fields as a list</para><code>
    /// $showConsoleOptions = @{
    ///     View = 'List'
    ///     Property = @('ProcessName', 'Id', 'WorkingSet64')
    /// }
    /// Get-Process | Select-Object -First 3 | Show-Console @showConsoleOptions</code></example>
    /// <example><para>Inspect a file object's members with a bounded tree</para><code>
    /// $showConsoleOptions = @{
    ///     View = 'Tree'
    ///     Depth = 1
    ///     Property = @('Name', 'FullName', 'LastWriteTime')
    /// }
    /// Get-Item . | Show-Console @showConsoleOptions</code></example>
    /// <example><para>Show a PowerShell snippet with line numbers</para><code>
    /// $showConsoleOptions = @{
    ///     LineNumbers = $true
    ///     View = 'Code'
    ///     Language = 'PowerShell'
    ///     Content = {
    ///         'Get-Process | Select-Object -First 5'
    ///     }
    /// }
    /// Show-Console @showConsoleOptions</code></example>
    /// <example><para>Compose a panel with native PowerShell script blocks</para><code>
    /// Show-Console -Content {
    ///     $panelOptions = @{
    ///         Title = 'Build summary'
    ///         Content = {
    ///             $rowsOptions = @{
    ///                 Gap = 1
    ///                 Content = {
    ///                     $textOptions = @{
    ///                         Text = @('All checks passed')
    ///                         Role = 'Pass'
    ///                     }
    ///                     New-ConsoleText @textOptions
    ///                     New-ConsoleRule -Title 'Ready to review'
    ///                 }
    ///             }
    ///             New-ConsoleRows @rowsOptions
    ///         }
    ///     }
    ///     New-ConsolePanel @panelOptions
    /// }</code></example>
    /// <example><para>Return a plain-text document for another pipeline command</para><code>
    /// $showConsoleOptions = @{
    ///     AsWidget = $true
    ///     View = 'List'
    ///     Property = @('ProcessName', 'Id')
    /// }
    /// Get-Process | Select-Object -First 3 |
    ///     Show-Console @showConsoleOptions |
    ///     Export-ConsoleFrame -Format Text</code></example>
    /// <example>
    /// <para>Open a Markdown document with two headings. If it exceeds the terminal height, press Ctrl+K to jump to Results, then press q to return to PowerShell. Use NoPager to print the same document directly.</para>
    /// <code>
    /// $markdown = @(
    ///     '# Plan'
    ///     ''
    ///     1..60 | ForEach-Object { "- Planned check $_" }
    ///     ''
    ///     '# Results'
    ///     ''
    ///     1..60 | ForEach-Object { "- Result $_" }
    /// ) -join [Environment]::NewLine
    /// $markdown | Show-Console -View Markdown</code>
    /// </example>
    /// <example>
    /// <para>Read any of these gallery scripts to see Show-Console in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>
    /// Get-ConsoleDemo -Slug 'welcome-compact' | Select-Object -ExpandProperty Source
    /// Get-ConsoleDemo -Slug 'server-estate' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "Console", DefaultParameterSetName = "Objects", HelpUri = "https://consolemonster.dev/powershell/commands/show-console/")]
    public sealed partial class CmdletShowConsole : ConsoleDisplayCmdlet {
        /// <summary><para type="description">Auto, Table, List, Tree, Code, Json, Error or Markdown. Tree inspects object members; New-ConsoleTree builds node hierarchies.</para></summary>
        [Parameter, ValidateSet("Auto", "Table", "List", "Tree", "Code", "Json", "Error", "Markdown")]
        [PSDefaultValue(Help = "Auto")]
        public string View { get; set; } = "Auto";
        /// <inheritdoc />
        protected override string RequestedView => View;
    }
}
