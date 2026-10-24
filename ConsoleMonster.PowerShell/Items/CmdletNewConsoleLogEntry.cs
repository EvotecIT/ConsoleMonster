using System;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates one log line with a level and a time, for New-ConsoleLogRegion.</para>
    /// <para type="description">Use it inside the script block of New-ConsoleLogRegion to show a log printed once, or pipe a log made earlier to Add-ConsoleItem with these entries. -Level takes info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Verbose, Debug, Success, Skipped). -AtSeconds is the source-relative time in seconds, including fractions. The message is shown literally; rich text from New-ConsoleText keeps its colours. In a running dashboard, write to the log with Write-ConsoleLogEntry instead.</para>
    /// </summary>
    /// <example>
    ///   <para>A short log printed once</para>
    ///   <code>
    /// New-ConsoleLogRegion -Height 5 -Content {
    ///     New-ConsoleLogEntry 'Discovered 4 domain controllers in 2 domains' -Level Info -AtSeconds 2
    ///     New-ConsoleLogEntry 'DC02: replication partner DC05 last success 3h ago' -Level Warning -AtSeconds 31
    ///     New-ConsoleLogEntry 'DC02: USN rollback check failed (event 2095)' -Level Error -AtSeconds 44
    /// } | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Entries from objects</para>
    ///   <code>
    /// $events = Import-Csv deploy-events.csv
    /// New-ConsoleLogRegion -Content {
    ///     foreach ($event in $events) { New-ConsoleLogEntry $event.Message -Level $event.Level -AtSeconds $event.Seconds }
    /// } | Show-Console</code>
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

        /// <summary><para type="description">Source-relative seconds shown at the start of the line; default 0. Fractional seconds are accepted.</para></summary>
        [Parameter]
        public double AtSeconds { get; set; }
        private System.TimeSpan At => System.TimeSpan.FromSeconds(AtSeconds);

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Text.RichText? rich = PSText.Rich(Message);
            WriteObject(rich != null ? new LogEntry(At, Level, rich) : new LogEntry(At, Level, PSText.PlainText(Message)));
        }
    }
}
