using System.Management.Automation;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Writes a line to the log region of a running dashboard.</para>
    /// <para type="description">-Level sets the tag: info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Success...). Errors, warnings, passes and skips carry a status marker; only warnings and errors are coloured. Write-Host in a dashboard runspace reaches the same log automatically.</para>
    /// <example>
    ///   <para>Log a warning</para>
    ///   <code>Write-ConsoleLog 'DC03 responded slowly' -Level warn</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "ConsoleLog")]
    public sealed class CmdletWriteConsoleLog : LiveCmdlet {
        /// <summary><para type="description">The message.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowEmptyString]
        public object Message { get; set; } = string.Empty;

        /// <summary><para type="description">The level: info, warn, err, verbose, debug, pass or skip. PowerShell's words work too: Information, Warning, Error, Verbose, Debug, Success, Skipped (and the status words of Write-ConsoleStatus).</para></summary>
        [Parameter]
        [Binding.LogLevelTransformation]
        [ArgumentCompleter(typeof(Binding.LogLevelCompleter))]
        public string Level { get; set; } = "info";

        /// <summary><para type="description">The log name; defaults to the dashboard's log region.</para></summary>
        [Parameter]
        public string Name { get; set; } = "$log";

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
            Send(new LiveEvent(Name, "write", text: text, state: Level, isMarkup: isMarkup));
        }
    }
}
