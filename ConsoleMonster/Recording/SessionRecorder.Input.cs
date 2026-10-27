using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace ConsoleMonster.Sessions {
    public sealed partial class SessionRecorder {
        private const int ObservationLimit = 1024 * 1024;
        private readonly object _observationGate = new object();
        private readonly StringBuilder _observedOutput = new StringBuilder();
        private readonly Stopwatch _sessionClock = Stopwatch.StartNew();
        private bool _outputFinished;
        private bool SessionExpired => _options.SessionTimeout.HasValue && _sessionClock.Elapsed >= _options.SessionTimeout.Value;

        private void RespondToCursorQuery(string response) {
            if (_stopping || _disposed) { return; }
            try { SendInput(response); }
            catch (Exception error) when ((error is IOException || error is System.ComponentModel.Win32Exception) && (_stopping || (_terminal?.HasExited ?? true))) {
                // Final output still drains when its child no longer needs a terminal reply.
            }
        }

        /// <summary>Whether a timed Ctrl+C was sent by <see cref="WaitForExitWithInterrupt"/>.</summary>
        public bool Interrupted { get; private set; }

        /// <summary>Sends scripted input after a bounded delay. The configured session timeout includes this wait;
        /// cancellation, timeout or an early child exit stops the owned session and finalizes its recording.</summary>
        public void SendInputAfter(TimeSpan delay, string input, CancellationToken cancellationToken = default) {
            if (delay < TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(delay)); }
            if (input == null) { throw new ArgumentNullException(nameof(input)); }
            Stopwatch wait = Stopwatch.StartNew();
            try {
                while (wait.Elapsed < delay) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (SessionExpired) { throw new TimeoutException("The recorded child exceeded its configured lifetime before scripted input."); }
                    if (HasExited) { throw new IOException("The recorded child exited before scripted input."); }
                    cancellationToken.WaitHandle.WaitOne((int)Math.Min(20, Math.Max(1, (delay - wait.Elapsed).TotalMilliseconds)));
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (_disposed) { throw new ObjectDisposedException(nameof(SessionRecorder)); }
                if (SessionExpired) { throw new TimeoutException("The recorded child exceeded its configured lifetime before scripted input."); }
                if (HasExited) { throw new IOException("The recorded child exited before scripted input."); }
                SendBoundedInput(input, cancellationToken);
            } catch { Stop(); throw; }
        }

        private void ObserveOutput(string text) {
            lock (_observationGate) {
                _observedOutput.Append(text);
                if (_observedOutput.Length > ObservationLimit) { _observedOutput.Remove(0, _observedOutput.Length - ObservationLimit); }
                Monitor.PulseAll(_observationGate);
            }
        }

        /// <summary>Waits until the child emits literal text, including across UTF-8 reads. Searches the most recent
        /// one million characters, including output emitted before this call. Private recording controls are excluded.
        /// Timeout and cancellation stop the owned child and finalize its recording.</summary>
        public void WaitForOutput(string text, TimeSpan timeout, CancellationToken cancellationToken = default) {
            if (string.IsNullOrEmpty(text) || text.Length > ObservationLimit) { throw new ArgumentException("Use 1 to 1048576 characters.", nameof(text)); }
            if (timeout <= TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(timeout)); }
            if (_terminal == null) { throw new InvalidOperationException("The in-process adapter has no recorded child output to wait for."); }
            Stopwatch wait = Stopwatch.StartNew();
            try {
                lock (_observationGate) {
                    while (true) {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (_disposed) { throw new ObjectDisposedException(nameof(SessionRecorder)); }
                        if (wait.Elapsed >= timeout || SessionExpired) { throw new TimeoutException("The recorded child did not emit the expected output before the timeout."); }
                        if (_observedOutput.ToString().IndexOf(text, StringComparison.Ordinal) >= 0) { return; }
                        if (_outputFinished) { throw new IOException("The recorded child ended before emitting the expected output.", _readError); }
                        Monitor.Wait(_observationGate, 20);
                    }
                }
            } catch { Stop(); throw; }
        }
    }
}
