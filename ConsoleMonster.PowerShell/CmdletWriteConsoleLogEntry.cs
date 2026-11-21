using System.Management.Automation;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>Writes a line to the log region of a running dashboard.</summary>
    /// <remarks><para>-Level sets the tag: info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Success...). Errors, warnings, passes and skips carry a status marker; only warnings and errors are coloured. Write-Host in a dashboard runspace reaches the same log automatically.</para></remarks>
    /// <example>
    ///   <para>Log a warning</para>
    ///   <code>$dashboardOptions2 = @{
    ///     Title = 'Connectivity'
    ///     Content = { New-ConsoleLogRegion }
    /// }
    /// $dashboard = New-ConsoleDashboard @dashboardOptions2
    /// $dashboardOptions = @{
    ///     Dashboard = $dashboard
    ///     ScriptBlock = {
    ///         $logEntryOptions = @{
    ///             Message = 'DC03 responded slowly'
    ///             Level = 'warn'
    ///         }
    ///         Write-ConsoleLogEntry @logEntryOptions
    ///     }
    /// }
    /// Invoke-ConsoleDashboard @dashboardOptions</code>
    /// </example>
    /// <example>
    ///   <para>Tag lines with the server they came from</para>
    ///   <code>$dashboardOptions2 = @{
    ///     Title = 'Patching'
    ///     Content = { New-ConsoleLogRegion -NoElapsed }
    /// }
    /// $dashboard = New-ConsoleDashboard @dashboardOptions2
    /// $dashboardOptions = @{
    ///     Dashboard = $dashboard
    ///     ScriptBlock = {
    ///         foreach ($server in 'DC01', 'DC02', 'FS01') {
    ///             $logEntryOptions2 = @{
    ///                 Message = 'Installing KB5044284'
    ///                 Tag = $server
    ///             }
    ///             Write-ConsoleLogEntry @logEntryOptions2
    ///         }
    ///         $logEntryOptions = @{
    ///             Message = 'Reboot pending'
    ///             Level = 'warn'
    ///             Tag = 'FS01'
    ///         }
    ///         Write-ConsoleLogEntry @logEntryOptions
    ///     }
    /// }
    /// Invoke-ConsoleDashboard @dashboardOptions</code>
    /// </example>
    /// <example>
    /// <para>Read the gallery script to see Write-ConsoleLogEntry in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>Get-ConsoleDemo -Slug 'prompt-tour' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Write, "ConsoleLogEntry", HelpUri = "https://consolemonster.dev/powershell/commands/write-consolelogentry/")]
    public sealed class CmdletWriteConsoleLogEntry : LiveCmdlet {
        /// <summary><para type="description">Text appended to the dashboard log region or application log viewport named by WidgetId.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowEmptyString]
        public object Message { get; set; } = string.Empty;

        /// <summary><para type="description">The level: info, warn, err, verbose, debug, pass or skip. PowerShell's words work too: Information, Warning, Error, Verbose, Debug, Success, Skipped (and the status words of Show-ConsoleStatusLine).</para></summary>
        [Parameter]
        [Binding.LogLevelTransformation]
        [ArgumentCompleter(typeof(Binding.LogLevelCompleter))]
        [PSDefaultValue(Help = "info")]
        public string Level { get; set; } = "info";

        /// <summary><para type="description">The log name; defaults to the dashboard's log region. In a running app, the key of a log viewport.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$log")]
        public string WidgetId { get; set; } = "$log";

        /// <summary><para type="description">The source of the line (a job, server or component), shown in its own column with a colour that stays the same for that source, and matched by New-ConsoleLogRegion -Tag.</para></summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string? Tag { get; set; }

        /// <summary><para type="description">Highlights the message: Smart (numbers, booleans, GUIDs, IP addresses, URLs, paths, dates, DNs, quoted strings) or a language such as Json or PowerShell. The message stays literal.</para></summary>
        [Parameter]
        public string? Highlight { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            string? text = PSText.EventText(Message, out bool isMarkup);
            if (!string.IsNullOrEmpty(Highlight) && !isMarkup && text != null) {
                Highlighting.ITextHighlighter highlighter = Highlighting.Highlighters.Get(Highlight) ?? throw new PSArgumentException("Unknown highlight language '" + Highlight + "'. Use Smart or one of: " + string.Join(", ", Highlighting.Highlighters.Names) + ".");
                text = Highlighting.HighlightStyles.ToMarkup(text, highlighter);
                isMarkup = true;
            }
            Send(new LiveEvent(WidgetId, "write", key: Tag, text: text, status: Level, isMarkup: isMarkup));
        }
    }
}
