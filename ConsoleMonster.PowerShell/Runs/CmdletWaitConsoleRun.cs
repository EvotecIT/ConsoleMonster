using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Waits for worker updates while pumping run progress to the PowerShell host.</para>
    /// <para type="description">Use after starting background workers that call the handle's Started and Finished methods. Waits until all items have finished and receives queued host output on the pipeline thread, including timed CI progress while workers are slow. It does not schedule work or print the final summary. Call Complete-ConsoleRun in finally after joining the workers. Ctrl+C signals the handle's Cancellation token so workers can drain and report their last outcomes.</para>
    /// </summary>
    /// <example><para>Wait for an existing background runner and retain its results</para><code>try { Wait-ConsoleRun $run; $results = Receive-Job $workerJob -Wait } finally { Complete-ConsoleRun $run }</code></example>
    [Cmdlet(VerbsLifecycle.Wait, "ConsoleRun")]
    public sealed class CmdletWaitConsoleRun : PSCmdlet {
        private volatile bool _stopping;
        /// <summary><para type="description">The handle shared with background workers.</para></summary>
        [Parameter(Mandatory = true, Position = 0)] public LiveRun Run { get; set; } = null!;
        /// <inheritdoc />
        protected override void EndProcessing() {
            while (!_stopping && !Stopping) {
                LiveRunHostQueues.Drain(Run, this);
                RunSnapshot snapshot = Run.Snapshot();
                if (snapshot.Done == snapshot.Total || snapshot.IsFinished) {
                    // Flush serializes with Finished, including its publication after the tracker's state changed.
                    LiveRunHostQueues.Drain(Run, this);
                    return;
                }
                Thread.Sleep(100);
            }
        }
        /// <inheritdoc />
        protected override void StopProcessing() { _stopping = true; Run?.Cancel(); }
    }
}
