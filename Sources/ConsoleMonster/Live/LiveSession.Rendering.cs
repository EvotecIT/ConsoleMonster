using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Text;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        private void RenderLoop() {
            double period = 1000.0 / _options.FramesPerSecond;
            TimeSpan heartbeat = Elapsed;
            // Frames are scheduled against absolute deadlines (frame n is due at n x period). Windows wait timeouts round
            // up to the system timer tick (~15.6 ms), so sleeping "period minus work" every frame drifted to ~12.5 fps at a
            // 15 fps setting; with deadlines a late wake-up shortens the next wait instead, and the average meets the rate.
            Stopwatch schedule = Stopwatch.StartNew();
            long frame = 0;
            try {
                while (IsRunning) {
                    // Apply everything queued, bounded by time rather than count: a flood must neither starve rendering
                    // nor cap throughput (a fixed per-tick count capped a 15 fps loop near 60k events/s).
                    Drain(int.MaxValue, Math.Max(20, period * 0.6));
                    if (_output!.SupportsLiveFrames) {
                        RenderFinal();
                    } else if (_options.StreamHeartbeat > TimeSpan.Zero && Elapsed - heartbeat >= _options.StreamHeartbeat) {
                        Heartbeat();
                        heartbeat = Elapsed;
                    }
                    frame++;
                    double now = schedule.Elapsed.TotalMilliseconds;
                    double wait = (frame * period) - now;
                    if (wait < -period) {
                        // Too far behind (a stall or a slow terminal): resynchronise instead of rendering a burst of frames.
                        frame = (long)(now / period);
                        wait = 0;
                    }
                    if (_wake.WaitOne(wait >= 1 ? (int)wait : 0)) {
                        break;
                    }
                }
            } catch (Exception ex) {
                Error(ex);
                RequestCancellation();
            } finally {
                bool finish;
                lock (_lifecycle) {
                    _renderExited = true;
                    finish = !_callerStopping;
                }
                if (finish) { FinishStop(); }
            }
        }

        private void Drain(int maximum, double budgetMilliseconds = double.MaxValue) {
            Stopwatch budget = Stopwatch.StartNew();
            for (int i = 0; i < maximum && _queue.TryDequeue(out LiveEvent? liveEvent); i++) {
                ApplyOne(liveEvent);
                if ((i & 255) == 255 && budget.Elapsed.TotalMilliseconds > budgetMilliseconds) {
                    break;
                }
            }
        }

        private void ApplyOne(LiveEvent liveEvent) {
            if (!TryGetTarget(liveEvent.Target, out ILiveTarget? target)) {
                Interlocked.Increment(ref _eventsDropped);
                return;
            }
            try {
                target!.Apply(liveEvent);
            } catch (Exception ex) {
                Interlocked.Increment(ref _applyErrors);
                Error(ex);
                return;
            }
            Interlocked.Increment(ref _eventsApplied);
            if (_recorder != null) {
                Attempt(() => _recorder.Write(liveEvent));
            }
            if (_output != null && !_output.SupportsLiveFrames) {
                Attempt(() => {
                    string? description = target!.Describe(liveEvent);
                    if (description != null) {
                        lock (_outputGate) {
                            _output.WriteLine(StyledLine.From(Stamp(description)));
                        }
                    }
                });
            }
        }

        private void RenderFinal() {
            if (_output == null || !_output.SupportsLiveFrames || _context == null) { return; }
            int width = _context.Profile.Width;
            int height = _context.Profile.Height;
            if (TerminalSize.TryGet(out int currentWidth, out int currentHeight)) {
                width = currentWidth;
                height = currentHeight;
            }
            lock (_outputGate) {
                if (_restored) { return; }
                if (Interlocked.Exchange(ref _invalidate, 0) != 0 || (_frame != null && (_frame.Width != width || _frame.Height != height))) {
                    _output.Invalidate();
                }
                _frame = FrameComposer.Compose(_root, width, height, _context.WithClock(FramesRendered, Elapsed), _frame);
                _output.RenderFrame(_frame);
                Interlocked.Increment(ref _framesRendered);
            }
        }

        private void Heartbeat() {
            List<string> lines = new List<string>();
            foreach (ILiveTarget target in _targets.Values) {
                if (target is ILiveSummary summary) {
                    Attempt(() => {
                        string? line = summary.GetSummaryLine();
                        if (line != null) { lines.Add(line); }
                    });
                }
            }
            if (lines.Count > 0) {
                lock (_outputGate) {
                    _output!.WriteLine(StyledLine.From(Stamp(string.Join(" | ", lines))));
                }
            }
        }

        private string Stamp(string line) {
            if (!_streamTimestamps) {
                return line;
            }
            return "[" + FormatClock(Elapsed) + "] " + line;
        }

        /// <summary>Formats a session clock value as <c>mm:ss</c>, or <c>h:mm:ss</c> from one hour.</summary>
        internal static string FormatClock(TimeSpan elapsed) {
            if (elapsed < TimeSpan.Zero) {
                elapsed = TimeSpan.Zero;
            }
            int hours = (int)elapsed.TotalHours;
            string minutesSeconds = elapsed.Minutes.ToString("00", CultureInfo.InvariantCulture) + ":" + elapsed.Seconds.ToString("00", CultureInfo.InvariantCulture);
            return hours > 0 ? hours.ToString(CultureInfo.InvariantCulture) + ":" + minutesSeconds : minutesSeconds;
        }
    }
}
