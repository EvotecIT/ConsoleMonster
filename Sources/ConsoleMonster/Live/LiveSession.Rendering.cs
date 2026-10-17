using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Text;

using ConsoleMonster.Canvas;

using ConsoleMonster.Capabilities;

using ConsoleMonster.Styling;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        private void RenderLoop() {
            FrameSchedule frames = new FrameSchedule(_options.FramesPerSecond);
            double period = frames.Period;
            TimeSpan heartbeat = Elapsed;
            Stopwatch schedule = Stopwatch.StartNew();
            try {
                while (IsRunning) {
                    // Apply everything queued, bounded by time rather than count: a flood must neither starve rendering
                    // nor cap throughput (a fixed per-tick count capped a 15 fps loop near 60k events/s).
                    Drain(int.MaxValue, Math.Max(20, period * 0.6));
                    if (_output!.SupportsLiveFrames) {
                        if (Volatile.Read(ref _paused) == 0) {
                            RenderFinal();
                        }
                    } else if (_options.StreamHeartbeat > TimeSpan.Zero && Elapsed - heartbeat >= _options.StreamHeartbeat) {
                        Heartbeat();
                        heartbeat = Elapsed;
                    }
                    if (_wake.WaitOne(frames.Next(schedule.Elapsed.TotalMilliseconds))) {
                        break;
                    }
                }
            } catch (Exception ex) {
                Error(ex);
                Volatile.Write(ref _failure, ex);
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
                _unknownTargets.AddOrUpdate(liveEvent.Target ?? string.Empty, 1, (name, count) => count + 1);
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
            if (_output == null || !_output.SupportsLiveFrames || _context == null) {
                return;
            }
            int width = _context.Profile.Width;
            int height = _context.Profile.Height;
            int left = 0;
            int top = 0;
            if (_options.WindowSource != null) {
                int[]? window = _options.WindowSource();
                if (window != null && window.Length == 4 && window[2] > 0 && window[3] > 0) {
                    left = window[0];
                    top = window[1];
                    width = window[2];
                    height = window[3];
                }
            } else if (TerminalSize.TryGetWindow(out int windowLeft, out int windowTop, out int currentWidth, out int currentHeight)) {
                width = currentWidth;
                height = currentHeight;
                left = windowLeft;
                top = windowTop;
            }
            CheckFont();
            lock (_outputGate) {
                if (_restored) {
                    return;
                }
                bool resized = _frame != null && (_frame.Width != width || _frame.Height != height);
                bool moved = left != _windowLeft || top != _windowTop;
                if (resized) {
                    // Coalesce drag-resizes: at most one full redraw per 100 ms settle period while the size keeps changing; the
                    // frame after the last change always redraws, so the final size is never missed.
                    if (!_resizeCoalescer.TryRedraw(RealElapsed)) {
                        return;
                    }
                    Interlocked.Increment(ref _resizes);
                    OnResized();
                }
                if (resized || moved || Interlocked.Exchange(ref _invalidate, 0) != 0) {
                    // Terminals reflow or scroll the old frame on resize; clear and redraw everything.
                    _output.Invalidate();
                }
                _windowLeft = left;
                _windowTop = top;
                RenderContext context = _context.WithClock(FramesRendered, Elapsed);
                try {
                    _frame = FrameComposer.Compose(_root, width, height, context, _frame);
                } catch (Exception ex) {
                    // A widget that cannot cope with this size must not end the session: show a one-line note instead.
                    Interlocked.Increment(ref _renderErrors);
                    _frame = RenderErrorFrame(width, height, context, ex);
                }
                _output.RenderFrame(_frame);
                Interlocked.Increment(ref _framesRendered);
            }
        }

        private static CellBuffer RenderErrorFrame(int width, int height, RenderContext context, Exception ex) {
            CellBuffer buffer = new CellBuffer(Math.Max(1, width), Math.Max(1, height));
            string text = "render error at " + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) + ": " + ex.GetType().Name;
            new Region(buffer).Write(0, 0, TextWrapper.Truncate(text, Math.Max(1, width - 1), context.Ellipsis), context.Style(ThemeToken.Warn));
            return buffer;
        }

        // After a size change in Windows Terminal the font may have changed with it (zoom), so widths are measured again.
        private void OnResized() {
            if (_context != null && _context.Profile.Kind == TerminalKind.WindowsTerminal && _output is VtOutput vt) {
                ReprobeWidths(vt);
            }
        }

        // conhost exposes the console font; when the user picks another face or size, glyph tier and widths are re-derived.
        private void CheckFont() {
            if (_fontWatcher == null || _context == null || !(_output is VtOutput vt)) {
                return;
            }
            if (!_fontWatcher.CheckChanged(out string? face, out int _)) {
                return;
            }
            TerminalProfile refreshed = new CapabilityDetector().RefreshFont(_context.Profile, face);
            _context = Terminal.CreateContext(refreshed, _context.Theme);
            ReprobeWidths(vt);
            Interlocked.Exchange(ref _invalidate, 1);
        }

        private void ReprobeWidths(VtOutput vt) {
            if (!_context!.Profile.ProbeWidths) {
                return;
            }
            TerminalWidthProbe.Invalidate();
            lock (_outputGate) {
                if (vt.TryProbeWidths(TimeSpan.FromMilliseconds(300), out TerminalWidthProfile measured, out string decision)) {
                    DisplayWidth.Profile = measured;
                }
                WidthDecision = decision;
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
