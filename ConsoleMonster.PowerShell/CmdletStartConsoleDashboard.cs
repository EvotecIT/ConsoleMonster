using System;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Runs a console dashboard live while a script does the work.</para>
    /// <para type="description">The script runs in a runspace the module owns (or, with -Parallel, once per input item on a runspace pool, started as items arrive and limited by -ThrottleLimit). Inside it, $ConsoleSession and the Update-ConsoleLane, Update-ConsolePhase, Write-ConsoleLog and related cmdlets update the screen; Write-Host, Write-Warning, Write-Verbose, errors and Console.WriteLine go to the dashboard's log region instead of scribbling over the screen. The screen is redrawn at about 15 frames per second however many updates arrive. Ctrl+C stops the script and always restores the terminal; the dashboard then leaves the alternate screen and prints its final state (or -Summary). Script output is returned when the dashboard ends. In redirected or CI output the dashboard becomes append-only lines. Variables from the caller are not visible inside the script: pass them with -ArgumentList and a param() block.</para>
    /// <example>
    ///   <para>Three servers processed in parallel, three at a time</para>
    ///   <code>$d = New-ConsoleDashboard -Title 'Patch' { New-ConsolePanel -Title 'Servers' { New-ConsoleLanes }; New-ConsolePanel -Title 'Log' -Fill { New-ConsoleLog } }
    /// 'SRV1', 'SRV2', 'SRV3' | Start-ConsoleDashboard $d -Parallel -ThrottleLimit 3 {
    ///     param($Server)
    ///     Update-ConsoleLane -Key $Server -Status Running -Total 5
    ///     1..5 | ForEach-Object { Start-Sleep -Milliseconds 300; Update-ConsoleLane -Key $Server -Result Pass }
    ///     Write-Host "$Server patched"
    ///     Update-ConsoleLane -Key $Server -Complete
    /// }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "ConsoleDashboard")]
    public sealed class CmdletStartConsoleDashboard : PSCmdlet, IDisposable {
        private volatile bool _stopping;
        private LiveSession? _session;
        private DashboardRunspaceHost? _host;
        private string _script = string.Empty;
        private bool _finished;
        private HostQueueWriter? _hostQueue; // Outside a real console: narration lines waiting for the pipeline thread.

        /// <summary><para type="description">The dashboard from New-ConsoleDashboard (or any widget tree).</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public IWidget Dashboard { get; set; } = null!;

        /// <summary><para type="description">The work to run while the dashboard is live.</para></summary>
        [Parameter(Position = 1, Mandatory = true)]
        public ScriptBlock ScriptBlock { get; set; } = null!;

        /// <summary><para type="description">Arguments passed to the script (after the input item with -Parallel).</para></summary>
        [Parameter]
        public object[]? ArgumentList { get; set; }

        /// <summary><para type="description">Items processed with -Parallel; each item is the script's first argument.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public object? InputObject { get; set; }

        /// <summary><para type="description">Run the script once per input item on a runspace pool.</para></summary>
        [Parameter]
        public SwitchParameter Parallel { get; set; }

        /// <summary><para type="description">Maximum concurrent runspaces with -Parallel (default 4).</para></summary>
        [Parameter]
        public int ThrottleLimit { get; set; } = 4;

        /// <summary><para type="description">Modules to import into the dashboard runspaces.</para></summary>
        [Parameter]
        public string[]? ImportModule { get; set; }

        /// <summary><para type="description">Record every update as JSON lines for Start-ConsoleReplay.</para></summary>
        [Parameter]
        public string? RecordPath { get; set; }

        /// <summary><para type="description">A widget printed to scrollback after the dashboard ends (default: the dashboard's final state).</para></summary>
        [Parameter]
        public IWidget? Summary { get; set; }

        /// <summary><para type="description">Draws the dashboard inline, below the cursor, instead of on the alternate screen: output and log lines scroll above it and the final state stays in the scrollback.</para></summary>
        [Parameter]
        public SwitchParameter Inline { get; set; }

        /// <summary><para type="description">Leaves nothing behind when the dashboard ends (no final state in the scrollback).</para></summary>
        [Parameter]
        public SwitchParameter Transient { get; set; }

        /// <summary><para type="description">Redraw rate (default 15).</para></summary>
        [Parameter]
        public int FramesPerSecond { get; set; } = 15;

        private bool Cancelled => _stopping || (_session != null && _session.Cancellation.IsCancellationRequested);

        // Called from every wait loop on the pipeline thread (about every 50 ms): flushes queued host output.
        private bool Poll() {
            _hostQueue?.Drain(this);
            return Cancelled;
        }

        // Write-Progress from the dashboard runspaces shows as activities under the dashboard, unless the dashboard already
        // has an activity list for them. Empty, the list takes no space.
        private static IWidget WithProgress(IWidget dashboard) {
            if (Contains(dashboard, ActivityList.ProgressTarget)) {
                return dashboard;
            }
            Rows rows = new Rows();
            rows.Add(dashboard, LayoutSize.Fraction());
            rows.Add(new ActivityList());
            return rows;
        }

        private static bool Contains(IWidget widget, string name) {
            if (widget is Live.ILiveTarget target && target.Name == name) {
                return true;
            }
            if (widget is IContainer container) {
                foreach (IWidget child in container.Children) {
                    if (Contains(child, name)) {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <inheritdoc />
        protected override void BeginProcessing() {
            LiveSessionOptions options = new LiveSessionOptions {
                FramesPerSecond = FramesPerSecond,
                Mode = Inline.IsPresent ? LiveMode.Inline : LiveMode.FullScreen,
                PrintFinalState = !Transient.IsPresent,
                Summary = Summary,
                RecordPath = RecordPath == null ? null : SessionState.Path.GetUnresolvedProviderPathFromPSPath(RecordPath)
            };
            Capabilities.TerminalProfile profile = Terminal.GetCurrentProfile();
            if (HostOutput.UseHost(this, profile)) {
                // Jobs, remoting, the ISE, redirected output, transcripts: append-only lines through the host, never the
                // raw stdout handle (which is the job or remoting channel) and never a live screen.
                Capabilities.TerminalProfile hostProfile = HostOutput.HostProfile(this, profile, false);
                _hostQueue = new HostQueueWriter();
                options.Profile = hostProfile;
                options.Output = Terminal.CreateOutput(hostProfile, _hostQueue);
            }
            _script = ScriptBlock.ToString();
            _session = new LiveSession(WithProgress(Dashboard), options);
            try {
                _host = new DashboardRunspaceHost(_session, ImportModule);
                _session.Start();
                if (Parallel.IsPresent) {
                    _host.OpenPool(ThrottleLimit);
                }
            } catch {
                Finish();
                _hostQueue?.Drain(this, true);
                throw;
            }
        }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (!Parallel.IsPresent || InputObject == null || _host == null) {
                return;
            }
            object item = InputObject is PSObject ps ? ps.BaseObject : InputObject;
            try {
                _host.Submit(_script, item, ArgumentList, Poll);
            } catch {
                Finish();
                throw;
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            try {
                if (Parallel.IsPresent) {
                    _host?.WaitAll(Poll);
                } else {
                    _host?.Run(_script, ArgumentList, Poll);
                }
            } finally {
                Finish();
            }
            // The final state (or -Summary) and the last narration lines, written by Stop on this thread.
            _hostQueue?.Drain(this, true);
            if (_session != null) {
                ReportProblems(_session);
            }
            if (_host == null) {
                return;
            }
            foreach (ErrorRecord error in _host.Errors) {
                WriteError(error);
            }
            if (_host.Failure != null) {
                WriteError(new ErrorRecord(_host.Failure, "ConsoleMonsterDashboardScriptFailed", ErrorCategory.NotSpecified, null));
            }
            foreach (PSObject output in _host.Output) {
                WriteObject(output);
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            _stopping = true;
        }

        // Problems that happened behind the live screen are reported through the host once the screen is restored.
        private void ReportProblems(LiveSession session) {
            LiveSessionCounters counters = session.GetCounters();
            if (counters.Failure != null) {
                ErrorRecord record = new ErrorRecord(counters.Failure, "ConsoleMonsterDashboardRenderFailed", ErrorCategory.WriteError, null) {
                    ErrorDetails = new ErrorDetails("The console dashboard stopped early because drawing failed: " + counters.Failure.Message)
                };
                WriteError(record);
            }
            foreach (System.Collections.Generic.KeyValuePair<string, long> unknown in counters.UnknownTargets) {
                WriteWarning(LiveCmdlet.UnknownTargetMessage(session, unknown.Key, unknown.Value));
            }
            if (counters.ApplyErrors > 0) {
                string last = counters.LastError == null ? string.Empty : " Last error: " + counters.LastError.Message;
                WriteWarning(counters.ApplyErrors.ToString(System.Globalization.CultureInfo.InvariantCulture) + " dashboard update(s) failed and were skipped." + last);
            }
            if (counters.RenderErrors > 0) {
                WriteWarning(counters.RenderErrors.ToString(System.Globalization.CultureInfo.InvariantCulture) + " frame(s) could not be laid out at the window size and showed an error line instead.");
            }
        }

        /// <summary>Stops the session and releases runspaces; PowerShell calls it even when the pipeline is stopped.</summary>
        public void Dispose() => Finish();

        private void Finish() {
            if (_finished) {
                return;
            }
            _finished = true;
            _host?.Dispose();
            _session?.Stop();
            _session?.Dispose();
        }
    }
}
