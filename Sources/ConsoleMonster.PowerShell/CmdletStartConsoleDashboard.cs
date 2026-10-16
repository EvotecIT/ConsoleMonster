using System;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;

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

        /// <summary><para type="description">Redraw rate (default 15).</para></summary>
        [Parameter]
        public int FramesPerSecond { get; set; } = 15;

        private bool Cancelled => _stopping || (_session != null && _session.Cancellation.IsCancellationRequested);

        /// <inheritdoc />
        protected override void BeginProcessing() {
            LiveSessionOptions options = new LiveSessionOptions {
                FramesPerSecond = FramesPerSecond,
                Summary = Summary,
                RecordPath = RecordPath == null ? null : SessionState.Path.GetUnresolvedProviderPathFromPSPath(RecordPath)
            };
            _script = ScriptBlock.ToString();
            _session = new LiveSession(Dashboard, options);
            try {
                _host = new DashboardRunspaceHost(_session, ImportModule);
                _session.Start();
                if (Parallel.IsPresent) {
                    _host.OpenPool(ThrottleLimit);
                }
            } catch {
                Finish();
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
                _host.Submit(_script, item, ArgumentList, () => Cancelled);
            } catch {
                Finish();
                throw;
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            try {
                if (Parallel.IsPresent) {
                    _host?.WaitAll(() => Cancelled);
                } else {
                    _host?.Run(_script, ArgumentList, () => Cancelled);
                }
            } finally {
                Finish();
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
