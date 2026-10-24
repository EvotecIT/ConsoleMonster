using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Starts a bounded run dashboard for work scheduled by your script.</para>
    /// <para type="description">Declares the queued items and returns a LiveRun handle. Use Update-ConsoleRun as workers start or finish and Complete-ConsoleRun in a finally block. Finished items stream above the live dashboard; failures and warnings remain pinned. The dashboard uses at most 40 percent of terminal height by default. CI and redirected hosts use append-only lines with periodic totals. This command does not execute work or change its results or exit code. Name, Target and Group accept property names or script blocks evaluated once, before the session starts. Interactive keys are opt-in: p pauses the log, f filters it to problems, g toggles groups, and ? shows help.</para>
    /// </summary>
    /// <example>
    /// <para>Track a script without changing its output</para>
    /// <code>
    /// $files = @(Get-ChildItem *.log)
    /// $run = New-ConsoleRun -Items $files -Title 'Archive logs' -Name Name -Target DirectoryName
    /// try {
    ///     for ($i = 0; $i -lt $files.Count; $i++) {
    ///         Update-ConsoleRun $run -Index $i -State Running -Phase 'archiving'
    ///         try { Compress-Archive $files[$i].FullName "$($files[$i].FullName).zip" -ErrorAction Stop; Update-ConsoleRun $run -Index $i -State Pass }
    ///         catch { Update-ConsoleRun $run -Index $i -State Fail -Message $_.Exception.Message }
    ///     }
    /// } finally { Complete-ConsoleRun $run }
    /// </code>
    /// </example>
    /// <example><para>Use append-only output in CI</para><code>$run = New-ConsoleRun -Items $checks -Name Name -Target Server -Group Category -NoLive -ProgressEvery 20</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleRun")]
    [OutputType(typeof(LiveRun))]
    public sealed class CmdletNewConsoleRun : PSCmdlet {
        /// <summary><para type="description">The complete input list. Updates identify each item by zero-based position.</para></summary>
        [Parameter(Mandatory = true)] [AllowEmptyCollection]
        public PSObject[] Items { get; set; } = Array.Empty<PSObject>();
        /// <summary><para type="description">The literal run title.</para></summary>
        [Parameter] public string Title { get; set; } = "Run";
        /// <summary><para type="description">A name property or script block ($_ is the item). Default the item's display title.</para></summary>
        [Parameter] public object? Name { get; set; }
        /// <summary><para type="description">An optional target property or script block.</para></summary>
        [Parameter] public object? Target { get; set; }
        /// <summary><para type="description">An optional group property or script block.</para></summary>
        [Parameter] public object? Group { get; set; }
        /// <summary><para type="description">Force append-only output without cursor movement.</para></summary>
        [Parameter] public SwitchParameter NoLive { get; set; }
        /// <summary><para type="description">Enable optional p/f/g/? keys when keyboard input is available.</para></summary>
        [Parameter] public SwitchParameter InteractiveKeys { get; set; }
        /// <summary><para type="description">Show the throughput trend next to its rate.</para></summary>
        [Parameter] public SwitchParameter Trend { get; set; }
        /// <summary><para type="description">Maximum terminal height percentage used by the live dashboard (default 40).</para></summary>
        [Parameter] [ValidateRange(1, 100)] public int HeightPercent { get; set; } = 40;
        /// <summary><para type="description">Print CI progress after this many items (default 25). Zero disables the count trigger.</para></summary>
        [Parameter] [ValidateRange(0, int.MaxValue)] public int ProgressEvery { get; set; } = 25;
        /// <summary><para type="description">Print CI progress at this interval in seconds (default 10). Zero disables the timer.</para></summary>
        [Parameter] [ValidateRange(0, 86400)] public double ProgressSeconds { get; set; } = 10;
        /// <summary><para type="description">Optional report location shown in the final summary.</para></summary>
        [Parameter] public string? ReportPath { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            List<RunItem> items = new List<RunItem>();
            foreach (PSObject item in Items) {
                items.Add(new RunItem(Read(item, Name, PSWidgets.ToText(item)), Read(item, Target, ""), Read(item, Group, "")));
            }
            TerminalProfile profile = Terminal.GetCurrentProfile();
            bool host = HostOutput.UseHost(this, profile);
            HostQueueWriter? queue = host ? new HostQueueWriter() : null;
            LiveSessionOptions session = new LiveSessionOptions { CaptureConsole = !host, Discoverable = false };
            if (host) { session.Profile = profile.WithMode(OutputMode.Plain); session.Output = new LineOutput(queue!, session.Profile); }
            LiveRun run = new LiveRun(items, new LiveRunOptions {
                Title = Title, NoLive = NoLive.IsPresent || host, HeightFraction = HeightPercent / 100.0,
                InteractiveKeys = InteractiveKeys.IsPresent, ShowTrend = Trend.IsPresent, ProgressEvery = ProgressEvery,
                ProgressInterval = TimeSpan.FromSeconds(ProgressSeconds), ReportPath = ReportPath, Session = session
            });
            try {
                if (queue != null) { LiveRunHostQueues.Add(run, queue); LiveRunHostQueues.Drain(run, this); }
                WriteObject(run);
            } catch {
                // Until publication succeeds, the caller has no handle with which to stop the session.
                try { run.Dispose(); }
                catch { /* Cleanup must not replace the original host or pipeline error. */ }
                finally { LiveRunHostQueues.Remove(run); }
                throw;
            }
        }

        private static string Read(PSObject item, object? selector, string fallback) {
            if (selector is PSObject wrapped) { selector = wrapped.BaseObject; }
            if (selector is ScriptBlock block) {
                Collection<PSObject> values = block.InvokeWithContext(null, new List<PSVariable> { new PSVariable("_", item) });
                return values.Count == 0 ? "" : PSWidgets.ToText(values[0]);
            }
            if (selector is string property) { return PSWidgets.ToText(item.Properties[property]?.Value); }
            return fallback;
        }
    }
}
