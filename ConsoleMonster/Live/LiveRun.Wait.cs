using System;
using System.Diagnostics;
using System.Threading;

namespace ConsoleMonster.Live {
    public sealed partial class LiveRun {
        /// <summary>Waits for every item without finalizing the run. Cancellation and timeout leave it open.</summary>
        /// <param name="timeout">Maximum duration, or null to wait until all items finish.</param>
        /// <param name="cancellation">The caller's cancellation token.</param>
        /// <param name="pump">Optional host-output pump, invoked synchronously on the calling thread.</param>
        /// <returns>True when every item has finished; false on timeout.</returns>
        public bool Wait(TimeSpan? timeout = null, CancellationToken cancellation = default, Action? pump = null) {
            if (timeout < TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(timeout)); }
            var watch = Stopwatch.StartNew();
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(Cancellation, cancellation)) {
                while (true) {
                    linked.Token.ThrowIfCancellationRequested();
                    pump?.Invoke();
                    RunSnapshot state = Snapshot();
                    if (state.Done == state.Total || state.IsFinished) { return true; }
                    if (timeout.HasValue && watch.Elapsed >= timeout.Value) { return false; }
                    double remaining = timeout.HasValue ? (timeout.Value - watch.Elapsed).TotalMilliseconds : 100;
                    linked.Token.WaitHandle.WaitOne((int)Math.Max(1, Math.Min(100, Math.Ceiling(remaining))));
                }
            }
        }
    }
}
