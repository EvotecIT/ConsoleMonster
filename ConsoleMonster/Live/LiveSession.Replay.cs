using System;
using System.IO;
using System.Threading;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        /// <summary>Replays events in order with scaled original timing, then drains and restores the session.</summary>
        /// <param name="root">Target widget tree.</param>
        /// <param name="jsonLines">Borrowed recording input.</param>
        /// <param name="options">Session options; ReplayDelay can supply a deterministic scheduler.</param>
        /// <param name="speed">Playback multiplier; zero or negative disables waits.</param>
        /// <param name="ct">Playback cancellation.</param>
        public static void Replay(IWidget root, TextReader jsonLines, LiveSessionOptions? options = null, double speed = 1.0, CancellationToken ct = default) {
            if (double.IsNaN(speed) || double.IsInfinity(speed)) { throw new ArgumentOutOfRangeException(nameof(speed)); }
            if (jsonLines == null) { throw new ArgumentNullException(nameof(jsonLines)); }
            LiveSessionOptions configuration = options ?? new LiveSessionOptions();
            using (LiveSession session = new LiveSession(root, configuration)) {
                if (speed > 0) {
                    session._timelineScale = speed;
                }
                session.Start();
                using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct, session.Cancellation)) {
                    TimeSpan baseline = session.RealElapsed;
                    TimeSpan requestedDelay = TimeSpan.Zero;
                    foreach (LiveEvent liveEvent in LiveRecordingReader.Read(jsonLines)) {
                        linked.Token.ThrowIfCancellationRequested();
                        if (speed > 0) {
                            double milliseconds = Math.Max(0, liveEvent.Time / speed);
                            if (milliseconds > TimeSpan.MaxValue.TotalMilliseconds) { throw new FormatException("Replay time is too large."); }
                            TimeSpan due = TimeSpan.FromMilliseconds(milliseconds);
                            TimeSpan elapsed = configuration.ReplayDelay == null ? session.RealElapsed - baseline : requestedDelay;
                            TimeSpan remaining = due - elapsed;
                            if (remaining > TimeSpan.Zero) {
                                if (configuration.ReplayDelay != null) { configuration.ReplayDelay(remaining, linked.Token); }
                                else { WaitForReplay(remaining, linked.Token); }
                                requestedDelay += remaining;
                            }
                        }
                        linked.Token.ThrowIfCancellationRequested();
                        session.Post(liveEvent);
                    }
                }
            }
        }

        private static void WaitForReplay(TimeSpan delay, CancellationToken cancellation) {
            // WaitHandle timeouts have a signed-32-bit millisecond limit.
            while (delay > TimeSpan.Zero) {
                TimeSpan slice = delay > TimeSpan.FromSeconds(30) ? TimeSpan.FromSeconds(30) : delay;
                if (cancellation.WaitHandle.WaitOne(slice)) { cancellation.ThrowIfCancellationRequested(); }
                delay -= slice;
            }
        }
    }
}
