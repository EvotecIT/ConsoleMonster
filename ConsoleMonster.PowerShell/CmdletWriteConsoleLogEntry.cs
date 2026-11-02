using System.Management.Automation;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes a line to the log region of a running dashboard.</para>
    /// <para type="description">-Level sets the tag: info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Success...). Errors, warnings, passes and skips carry a status marker; only warnings and errors are coloured. Write-Host in a dashboard runspace reaches the same log automatically.</para>
    /// </summary>
    /// <example>
    ///   <para>Log a warning</para>
    ///   <code>$dashboard = New-ConsoleDashboard -Title 'Connectivity' -Content { New-ConsoleLogRegion }
    /// Invoke-ConsoleDashboard $dashboard { Write-ConsoleLogEntry 'DC03 responded slowly' -Level warn }</code>
    /// </example>
    /// <example>
    ///   <para>Tag lines with the server they came from</para>
    ///   <code>$dashboard = New-ConsoleDashboard -Title 'Patching' -Content { New-ConsoleLogRegion -NoElapsed }
    /// Invoke-ConsoleDashboard $dashboard {
    ///     foreach ($server in 'DC01', 'DC02', 'FS01') {
    ///         Write-ConsoleLogEntry 'Installing KB5044284' -Tag $server
    ///     }
    ///     Write-ConsoleLogEntry 'Reboot pending' -Level warn -Tag 'FS01'
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Write, "ConsoleLogEntry")]
    public sealed class CmdletWriteConsoleLogEntry : LiveCmdlet {
        /// <summary><para type="description">The message.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowEmptyString]
        public object Message { get; set; } = string.Empty;

        /// <summary><para type="description">The level: info, warn, err, verbose, debug, pass or skip. PowerShell's words work too: Information, Warning, Error, Verbose, Debug, Success, Skipped (and the status words of Show-ConsoleStatusLine).</para></summary>
        [Parameter]
        [Binding.LogLevelTransformation]
        [ArgumentCompleter(typeof(Binding.LogLevelCompleter))]
        public string Level { get; set; } = "info";

        /// <summary><para type="description">The log name; defaults to the dashboard's log region. In a running app, the key of a log viewport.</para></summary>
        [Parameter]
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
