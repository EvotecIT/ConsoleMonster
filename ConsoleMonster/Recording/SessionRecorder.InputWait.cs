using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace ConsoleMonster.Sessions {
    public sealed partial class SessionRecorder {
        // A timeout must remain able to stop the child while its input pipe is full, including while a
        // terminal response holds the input gate. The timer only stops transport; the caller owns disposal.
        private void SendBoundedInput(string input, CancellationToken cancellation, TimeSpan? operationTimeout = null) {
            Stopwatch wait = Stopwatch.StartNew();
            int settlement = 0; // active, complete, timed out, cancelled
            object timerGate = new object();
            Timer? timer = null;

            TimeSpan? Remaining() {
                TimeSpan? remaining = operationTimeout.HasValue ? operationTimeout.Value - wait.Elapsed : null;
                if (_options.SessionTimeout.HasValue) {
                    TimeSpan session = _options.SessionTimeout.Value - _sessionClock.Elapsed;
                    if (!remaining.HasValue || session < remaining.Value) { remaining = session; }
                }
                return remaining;
            }
            bool Expired() => Remaining() is TimeSpan left && left <= TimeSpan.Zero;
            void Abort(int reason) {
                if (Interlocked.CompareExchange(ref settlement, reason, 0) != 0) { return; }
                lock (_lifetime) {
                    if (_disposed) { return; }
                    _stopping = true;
                    _terminal?.Stop();
                    // Closing ConPTY releases blocked input writes while its reader drains output.
                    if (_terminal is WindowsPseudoTerminal windows) { windows.Complete(); }
                }
            }
            void Schedule() {
                lock (timerGate) {
                    if (Volatile.Read(ref settlement) != 0) { return; }
                    TimeSpan? left = Remaining();
                    if (left.HasValue) { timer!.Change((int)Math.Min(int.MaxValue, Math.Max(1, Math.Ceiling(left.Value.TotalMilliseconds))), Timeout.Infinite); }
                }
            }
            void Check() {
                cancellation.ThrowIfCancellationRequested();
                if (Expired() || Volatile.Read(ref settlement) == 2) { throw new TimeoutException("The recorded child did not accept scripted input before its deadline."); }
                if (_disposed) { throw new ObjectDisposedException(nameof(SessionRecorder)); }
                if (_terminal == null) { throw new InvalidOperationException("The in-process adapter has no terminal input."); }
                if (HasExited) { throw new IOException("The recorded child stopped before accepting scripted input."); }
            }

            using (CancellationTokenRegistration registration = cancellation.Register(() => Abort(3))) {
                timer = new Timer(_ => { if (Expired()) { Abort(2); } else { Schedule(); } }, null, Timeout.Infinite, Timeout.Infinite);
                try {
                    Schedule();
                    lock (_inputGate) {
                        Check();
                        _terminal!.Write(Encoding.UTF8.GetBytes(input));
                    }
                    Check();
                } catch (Exception error) when (cancellation.IsCancellationRequested || Volatile.Read(ref settlement) == 3) {
                    throw new OperationCanceledException("Scripted terminal input was cancelled.", error, cancellation);
                } catch (Exception error) when (Expired() || Volatile.Read(ref settlement) == 2) {
                    throw new TimeoutException("The recorded child did not accept scripted input before its deadline.", error);
                } finally {
                    lock (timerGate) {
                        Interlocked.CompareExchange(ref settlement, 1, 0);
                        timer.Dispose();
                    }
                }
            }
            if (Volatile.Read(ref settlement) == 3) { cancellation.ThrowIfCancellationRequested(); }
            if (Volatile.Read(ref settlement) == 2) { throw new TimeoutException("The recorded child did not accept scripted input before its deadline."); }
        }
    }
}
