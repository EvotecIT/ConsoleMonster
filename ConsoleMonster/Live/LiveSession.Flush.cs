using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        /// <summary>Waits until earlier accepted events have reached output. Useful for a host's pipeline-thread output pump; it does not force a frame redraw.</summary>
        /// <param name="timeout">Maximum wait. The render thread cannot synchronously wait for itself.</param>
        /// <returns>False if the session is closed or the timeout expired.</returns>
        public bool Flush(TimeSpan timeout) {
            CheckFlushTimeout(timeout);
            Task<bool> signal = QueueFlush();
            return signal.Wait(timeout) && signal.Result;
        }

        internal static void CheckFlushTimeout(TimeSpan timeout) {
            if (timeout < TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue) { throw new ArgumentOutOfRangeException(nameof(timeout)); }
        }

        internal Task<bool> QueueFlush() {
            if (Thread.CurrentThread == _thread) { throw new InvalidOperationException("A render callback cannot wait for the render thread."); }
            if (!IsRunning) { return Task.FromResult(false); }
            TaskCompletionSource<bool> signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!TryPost(new LiveEvent("$flush", "flush") { Payload = signal })) { signal.TrySetResult(false); }
            return signal.Task;
        }
    }
}
