using System.Management.Automation;
using System;
using System.Runtime.CompilerServices;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Finishes a run dashboard and prints its full summary into scrollback.</para>
    /// <para type="description">Marks unfinished items skipped, releases held log lines, restores the terminal and prints totals, groups, full wrapped problems and the report path. Safe to call more than once. If deferred host output cannot finish within five seconds, reports a timeout and retains it for a later call. Put it in finally so cancellation or errors restore the terminal. PassThru returns the structured snapshot; ordinary results and the script's exit code are unchanged.</para>
    /// </summary>
    /// <example><para>Keep a structured final result</para><code>$summary = Complete-ConsoleRun $run -PassThru; $summary.Items | Where-Object State -eq Fail</code></example>
    [Cmdlet(VerbsLifecycle.Complete, "ConsoleRun")]
    [OutputType(typeof(RunSnapshot))]
    public sealed class CmdletCompleteConsoleRun : PSCmdlet {
        /// <summary><para type="description">The handle returned by New-ConsoleRun.</para></summary>
        [Parameter(Mandatory = true, Position = 0)] public LiveRun Run { get; set; } = null!;
        /// <summary><para type="description">Return the final structured snapshot.</para></summary>
        [Parameter] public SwitchParameter PassThru { get; set; }
        /// <inheritdoc />
        protected override void EndProcessing() {
            RunSnapshot snapshot = Run.Complete();
            LiveRunHostQueues.Drain(Run, this, true);
            if (PassThru.IsPresent) { WriteObject(snapshot); }
        }
    }

    // The existing host writer queues from the render thread; only a cmdlet's pipeline thread writes host records.
    internal static class LiveRunHostQueues {
        private static readonly ConditionalWeakTable<LiveRun, HostQueueWriter> Queues = new ConditionalWeakTable<LiveRun, HostQueueWriter>();
        internal static void Add(LiveRun run, HostQueueWriter writer) => Queues.Add(run, writer);
        internal static void Remove(LiveRun run) => Queues.Remove(run);
        internal static void Drain(LiveRun run, PSCmdlet cmdlet, bool final = false) {
            if (Queues.TryGetValue(run, out HostQueueWriter? writer)) {
                // Rendering is asynchronous. A barrier keeps the last accepted line from waiting for the next cmdlet.
                bool flushed = run.Flush(TimeSpan.FromSeconds(5));
                try { writer.Drain(cmdlet, final && flushed); }
                finally { if (final && flushed) { Remove(run); } }
                if (run.OutputFailure != null) { throw new InvalidOperationException("Run output failed before it could flush.", run.OutputFailure); }
                if (!flushed) {
                    throw new InvalidOperationException(final
                        ? "Run output did not flush within five seconds. Call Complete-ConsoleRun again after pending cleanup finishes."
                        : "Run output did not flush within five seconds.");
                }
            }
        }
    }
}
