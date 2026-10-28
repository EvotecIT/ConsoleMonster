using System;
using System.Management.Automation;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Pauses dashboard work, or captures its next checkpoint during a recording.</para>
    /// <para type="description">Waits cancellably on the resolved live session and writes no output. A recording target with SkipDelays enabled captures earlier queued updates instead of waiting.</para>
    /// </summary>
    /// <example>
    /// <para>Update progress between cancellable pauses</para>
    /// <code>
    /// $dashboard = New-ConsoleDashboard -Title 'Scan' -Content {
    ///     New-ConsoleProgress -WidgetId overall -Label Targets -Total 3
    /// }
    /// Invoke-ConsoleDashboard -Dashboard $dashboard -ScriptBlock {
    ///     foreach ($count in 1..3) {
    ///         Update-Console -WidgetId overall -Value $count
    ///         Wait-ConsoleDashboard -Seconds 0.25
    ///     }
    /// }</code>
    /// </example>
    [Cmdlet(VerbsLifecycle.Wait, "ConsoleDashboard")]
    public sealed class CmdletWaitConsoleDashboard : NoPipelineLiveCmdlet {
        private volatile LiveSession? _waitingSession;
        private volatile bool _stopping;

        /// <summary><para type="description">The pause in seconds, fractions allowed (0.25 is a quarter second); zero captures a checkpoint without waiting.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateRange(0, 2147483.0)]
        public double Seconds { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            LiveSession? session = ResolveSession();
            if (session == null) {
                WriteError(new ErrorRecord(new PSInvalidOperationException("No console dashboard is running. Use Invoke-ConsoleDashboard first."),
                    "ConsoleMonsterNoSession", ErrorCategory.InvalidOperation, null));
                return;
            }
            _waitingSession = session;
            try {
                if (_stopping) { session.Cancel(); }
                session.Delay(TimeSpan.FromSeconds(Seconds));
            } catch (OperationCanceledException) when (session.Cancellation.IsCancellationRequested) {
                // Stopping a dashboard or its pipeline cancels the pause without adding pipeline output or errors.
            } finally {
                _waitingSession = null;
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            _stopping = true;
            _waitingSession?.Cancel();
        }
    }
}
