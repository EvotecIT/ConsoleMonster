using System;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates one log line with a level and a time, for New-ConsoleLog.</para>
    /// <para type="description">Use it inside the script block of New-ConsoleLog to show a log printed once, or pipe a log made earlier to Add-ConsoleItem with these entries. -Level takes info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Verbose, Debug, Success, Skipped). -At is the time shown at the start of the line: a TimeSpan, a number of seconds, or a number with a unit (500ms, 2s, 1.5m, 1h). The message is shown literally; rich text from New-ConsoleText keeps its colours. In a running dashboard, write to the log with Write-ConsoleLog instead.</para>
    /// </summary>
    /// <example>
    ///   <para>A short log printed once</para>
    ///   <code>
    /// New-ConsoleLog -Height 5 {
    ///     New-ConsoleLogEntry 'Discovered 4 domain controllers in 2 domains' -Level Info -At 2s
    ///     New-ConsoleLogEntry 'DC02: replication partner DC05 last success 3h ago' -Level Warning -At 31s
    ///     New-ConsoleLogEntry 'DC02: USN rollback check failed (event 2095)' -Level Error -At 44s
    /// } | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Entries from objects</para>
    ///   <code>
    /// $events = Import-Csv deploy-events.csv
    /// New-ConsoleLog {
    ///     foreach ($event in $events) { New-ConsoleLogEntry $event.Message -Level $event.Level -At $event.Seconds }
    /// } | Write-ConsoleWidget</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleLogEntry")]
    [OutputType(typeof(LogEntry))]
    public sealed class CmdletNewConsoleLogEntry : PSCmdlet {
        /// <summary><para type="description">The message (shown literally; rich text from New-ConsoleText keeps its colours).</para></summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowEmptyString]
        public object Message { get; set; } = string.Empty;

        /// <summary><para type="description">The level: info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Verbose, Debug, Success, Skipped). Default info.</para></summary>
        [Parameter]
        [Binding.LogLevelTransformation]
        [ArgumentCompleter(typeof(Binding.LogLevelCompleter))]
        public string Level { get; set; } = "info";

        /// <summary><para type="description">The time shown at the start of the line: a TimeSpan, seconds (2 or 1.5) or a number with a unit (500ms, 2s, 1.5m, 1h). Default 0.</para></summary>
        [Parameter]
        [Binding.TimeSpanTransformation]
        public TimeSpan At { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Text.RichText? rich = PSText.Rich(Message);
            WriteObject(rich != null ? new LogEntry(At, Level, rich) : new LogEntry(At, Level, PSText.PlainText(Message)));
        }
    }
}
