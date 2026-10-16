using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            try {
                while (IsRunning) {
                    Stopwatch tick = Stopwatch.StartNew();
                    // Apply everything queued, bounded by time rather than count: a flood must neither starve rendering
                    // nor cap throughput (a fixed per-tick count capped a 15 fps loop near 60k events/s).
                    Drain(int.MaxValue, Math.Max(20, period * 0.6));
                    if (_output!.SupportsLiveFrames) { RenderFinal(); }
                    else if (_options.StreamHeartbeat > TimeSpan.Zero && Elapsed - heartbeat >= _options.StreamHeartbeat) {
                        Heartbeat();
                        heartbeat = Elapsed;
                    }
                    int delay = Math.Max(1, (int)Math.Ceiling(period - tick.Elapsed.TotalMilliseconds));
                    if (_wake.WaitOne(delay)) { break; }
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
                            _output.WriteLine(StyledLine.From(description));
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
                lock (_outputGate) { _output!.WriteLine(StyledLine.From(string.Join(" | ", lines))); }
            }
        }
    }
}
