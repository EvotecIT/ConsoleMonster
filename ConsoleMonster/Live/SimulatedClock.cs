using System;
using System.Diagnostics;
using System.Threading;

namespace ConsoleMonster.Live {
    /// <summary>
    /// A run clock the work moves forward, for simulated runs (demos, recordings, replays of logged work, tests). A live
    /// session reads it through <see cref="LiveSessionOptions.Clock"/> (give it <see cref="Read"/>, or
    /// <see cref="ToSessionOptions"/>), so the header clock, the summary bar's elapsed time and time left, log and activity
    /// times and the run result's duration all follow it. <see cref="Wait"/> moves the clock forward in small steps,
    /// sleeping a share of the time set by <see cref="Speed"/> so the screen moves smoothly; at speed 0 it never sleeps,
    /// which makes a run as fast as the machine and its frames repeatable. Reading it is safe from any thread; move it
    /// forward from one thread at a time (the run's own work), or two waits may each add their last step.
    /// </summary>
    /// <example>
    /// <code>
    /// SimulatedClock clock = new SimulatedClock(speed: 4);
    /// context.Run(dashboard, run => {
    ///     clock.Wait(2.5); // two and a half seconds on the dashboard's clock, about 0.6 s of real time
    ///     return new RunResult("Done");
    /// }, clock.ToSessionOptions());
    /// </code>
    /// </example>
    public sealed class SimulatedClock {
        private static readonly long Step = TimeSpan.FromMilliseconds(250).Ticks;
        // Real time and paced ticks are cumulative, so coarse sleeps never add up into a slower run;
        // time added instantly by Advance is not charged to later waits.
        private readonly Stopwatch _real = new Stopwatch();
        private readonly object _gate = new object();
        private long _ticks;
        private long _waited;
        private long _held;
        // The live session the clock runs in: the running session it first sees, kept after Ctrl+C (when the session no
        // longer counts as current) so a stopped run is noticed.
        private LiveSession? _session;

        /// <summary>Creates a clock at zero.</summary>
        /// <param name="speed">Simulated seconds per real second while waiting; 0 or less never sleeps.</param>
        public SimulatedClock(double speed = 0) {
            Speed = double.IsNaN(speed) ? 0 : speed;
        }

        /// <summary>Simulated seconds per real second while waiting; 0 or less never sleeps.</summary>
        public double Speed { get; }

        /// <summary>The time on the clock.</summary>
        public TimeSpan Elapsed => TimeSpan.FromTicks(Interlocked.Read(ref _ticks));

        /// <summary>The time on the clock in seconds.</summary>
        public double Seconds => Elapsed.TotalSeconds;

        /// <summary>The time on the clock: the reader for <see cref="LiveSessionOptions.Clock"/>.</summary>
        /// <returns>The time.</returns>
        public TimeSpan Read() => Elapsed;

        /// <summary>Moves the clock forward at once, without waiting.</summary>
        /// <param name="seconds">The seconds to add (negative values are ignored).</param>
        public void Advance(double seconds) => Advance(TimeSpan.FromTicks(ToTicks(seconds)));

        /// <summary>Moves the clock forward at once, without waiting.</summary>
        /// <param name="time">The time to add (negative values are ignored).</param>
        public void Advance(TimeSpan time) {
            if (time.Ticks > 0) {
                Interlocked.Add(ref _ticks, time.Ticks);
            }
        }

        /// <summary>Moves the clock forward by a time, sleeping in proportion to <see cref="Speed"/>; see <see cref="WaitUntil"/>.</summary>
        /// <param name="seconds">The simulated seconds to wait.</param>
        /// <param name="cancellationToken">Cuts the sleeps short (the clock still moves); see <see cref="WaitUntil"/>.</param>
        public void Wait(double seconds, CancellationToken cancellationToken = default) => WaitUntil(Seconds + seconds, cancellationToken);

        /// <summary>
        /// Moves the clock forward to a time, in steps of a quarter of a simulated second, sleeping in proportion to
        /// <see cref="Speed"/>. A time already passed returns at once. Once the token is cancelled, or the live session the
        /// clock runs in (the running session it first sees) is stopped with Ctrl+C, the clock moves on without sleeping,
        /// so the work reaches its next cancellation check, or its end, without delay. A full-viewport recording target
        /// with skipped delays captures each simulated step after queued updates, without real-time sampling or sleeping.
        /// </summary>
        /// <param name="seconds">The time on the clock to reach, in seconds.</param>
        /// <param name="cancellationToken">Cuts the sleeps short.</param>
        public void WaitUntil(double seconds, CancellationToken cancellationToken = default) {
            long goal = ToTicks(seconds);
            if (Speed > 0) {
                StartReal();
            }
            while (Interlocked.Read(ref _ticks) < goal) {
                long now = Interlocked.Read(ref _ticks);
                long step = Math.Min(Step, goal - now);
                if (Speed > 0 && !RecordsViewportSteps()) {
                    long due = (long)(Interlocked.Add(ref _waited, step) / Speed) + Interlocked.Read(ref _held);
                    long wait = due - _real.Elapsed.Ticks;
                    if (wait > 0) {
                        Sleep(TimeSpan.FromTicks(wait), cancellationToken);
                    }
                }
                Interlocked.Add(ref _ticks, step);
                CaptureStep(cancellationToken);
            }
        }

        /// <summary>Waits real time with the clock standing still (a finished screen that stays a moment); never at speed 0.
        /// A full-viewport target with skipped delays captures a final checkpoint instead, including a zero-duration hold.</summary>
        /// <param name="seconds">The real seconds to wait.</param>
        /// <param name="cancellationToken">Cuts the wait short.</param>
        public void Hold(double seconds, CancellationToken cancellationToken = default) {
            if (double.IsNaN(seconds) || seconds < 0) { return; }
            if (RecordsViewportSteps()) {
                CaptureStep(cancellationToken);
                return;
            }
            if (Speed <= 0 || seconds <= 0) {
                return;
            }
            StartReal();
            long before = _real.Elapsed.Ticks;
            Sleep(TimeSpan.FromTicks(ToTicks(seconds)), cancellationToken);
            // Later waits are due after the time actually held, so they neither rush to catch up nor wait it again.
            Interlocked.Add(ref _held, _real.Elapsed.Ticks - before);
        }

        private void StartReal() {
            lock (_gate) {
                if (!_real.IsRunning) {
                    _real.Start();
                }
            }
        }

        private bool RecordsViewportSteps() {
            LiveSession? current = LiveSession.Current;
            // Keep the same association as Sleep, even when recording bypasses real-time waits.
            if (current != null) { Interlocked.Exchange(ref _session, current); }
            return current?.RecordsViewportSteps == true;
        }

        private static void CaptureStep(CancellationToken cancellationToken) {
            LiveSession? session = LiveSession.Current;
            if (session?.RecordsViewportSteps != true || cancellationToken.IsCancellationRequested) { return; }
            try { session.DelayCore(TimeSpan.Zero, cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || session.Cancellation.IsCancellationRequested) {
                // Cancellation retains the clock's ordinary contract: advance to the caller's next cancellation check.
            }
        }

        /// <summary>Session options whose clock is this one.</summary>
        /// <returns>New options.</returns>
        public LiveSessionOptions ToSessionOptions() => new LiveSessionOptions { Clock = Read };

        /// <summary>Converts the clock to session options whose clock it is (PowerShell passes a clock where a run takes options this way).</summary>
        /// <param name="clock">The clock.</param>
        public static implicit operator LiveSessionOptions(SimulatedClock clock) => clock?.ToSessionOptions()!;

        /// <inheritdoc />
        public override string ToString() => Widgets.NumberFormat.Clock(Elapsed);

        private static long ToTicks(double seconds) => double.IsNaN(seconds) ? 0 : (long)Math.Round(Math.Max(0, Math.Min(seconds, TimeSpan.MaxValue.TotalSeconds / 2)) * TimeSpan.TicksPerSecond);

        // Sleeps unless the token is cancelled or the clock's live session has been stopped, waking as soon as either happens.
        private void Sleep(TimeSpan span, CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested) {
                return;
            }
            // A newer running session takes over (a clock used for another run); a cancelled one stays, so it is noticed.
            LiveSession? current = LiveSession.Current;
            if (current != null) {
                Interlocked.Exchange(ref _session, current);
            }
            LiveSession? session = Volatile.Read(ref _session);
            if (session == null) {
                if (cancellationToken.CanBeCanceled) {
                    cancellationToken.WaitHandle.WaitOne(span);
                } else {
                    Thread.Sleep(span);
                }
                return;
            }
            if (session.Cancellation.IsCancellationRequested) {
                return;
            }
            if (cancellationToken.CanBeCanceled) {
                WaitHandle.WaitAny(new[] { session.Cancellation.WaitHandle, cancellationToken.WaitHandle }, span);
            } else {
                session.Cancellation.WaitHandle.WaitOne(span);
            }
        }
    }
}
