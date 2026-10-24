using System;
using System.IO;
using System.Text;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Text;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
        private bool _onConsole;
        private volatile bool _taskbarUsed;
        private Styling.ITheme? _theme;

        /// <summary>Starts the dedicated background render thread. Repeated calls while running are harmless.</summary>
        public void Start() {
            bool cleanup = false;
            try {
                lock (_lifecycle) {
                    if (_state == 1) { return; }
                    if (_state != 0) { throw new InvalidOperationException("A live session cannot be restarted."); }
                    if (_options.RecordPath != null && _options.RecordWriter != null) {
                        throw new ArgumentException("Specify RecordPath or RecordWriter, not both.");
                    }
                    try {
                        // Capability detection can enable Windows VT mode. Save the caller's mode before resolving
                        // the profile or theme, so cancellation and normal shutdown restore the state from entry.
                        _guard = new TerminalGuard(RestoreTerminal, _options.HandleCancelKey);
                        ResolveLogTarget();
                        ITerminalTarget target = _options.Target ?? Terminal.Target;
                        _recordingTarget = target as RecordingTarget;
                        _delayClock = _recordingTarget?.SkipDelays == true && _options.Clock == null;
                        bool console = target is ConsoleTarget;
                        ITerminalWriter? targetWriter = console ? null : target.Writer;
                        _onConsole = _options.Output == null && targetWriter == null;
                        _theme = _options.Theme ?? target.Theme;
                        TerminalProfile profile = _options.Profile != null ? Terminal.ApplyProfilePolicy(_options.Profile) : Terminal.GetTargetProfile(target);
                        if (_options.Output?.Profile.Mode == OutputMode.Plain) {
                            profile = profile.WithMode(OutputMode.Plain);
                        } else if (_options.AppendOnly) {
                            profile = profile.WithMode(profile.ColorDepth == ColorDepth.None || profile.Mode == OutputMode.Plain ? OutputMode.Plain : OutputMode.Stream);
                        }
                        if (_options.Culture != null) {
                            profile = profile.WithCulture(_options.Culture);
                        }
                        _context = Terminal.CreateContext(profile, _theme);
                        _streamTimestamps = _options.StreamTimestamps ?? profile.IsContinuousIntegration;
                        _output = _options.Output ?? (_options.Mode == LiveMode.Inline ? Terminal.CreateInlineOutput(profile, targetWriter) : Terminal.CreateOutput(profile, targetWriter));
                        _clockStart = _options.Clock?.Invoke() ?? TimeSpan.Zero;
                        _watch.Start();
                        Volatile.Write(ref _state, 1);
                        _guardRegistration = _guard.Cancellation.Register(() => RequestCancellation(_options.StopOnCancellation));
                        _guard.Cancellation.ThrowIfCancellationRequested();
                        if (_options.RecordWriter != null) { _recorder = new LiveRecorder(_options.RecordWriter); }
                        else if (_options.RecordPath != null) {
                            StreamWriter writer = OpenRecording(_options.RecordPath);
                            try { _recorder = new LiveRecorder(writer, false); }
                            catch { writer.Dispose(); throw; }
                        } else if (_options.AutoRecord > 0) {
                            _recorder = OpenAutoRecording();
                        }
                        if (_options.CaptureConsole) {
                            lock (CurrentGate) {
                                if (_captureOwner != null) {
                                    throw new InvalidOperationException("Another live session is running and captures console output; stop it before starting a new one.");
                                }
                                _captureOwner = this;
                                _capture = new ConsoleCapture(CaptureLine);
                            }
                        }
                        // Everything that can fail without touching the screen has been validated above.
                        _output.BeginLive();
                        _began = true;
                        ApplyWidthProbe(profile);
                        if (profile.Kind == TerminalKind.Conhost && _onConsole && _output is VtOutput) {
                            _fontWatcher = new ConsoleFontWatcher();
                            _fontWatcher.CheckChanged(out _, out _);
                        }
                        if (_onConsole) {
                            Terminal.ClaimConsole(this);
                        }
                        if (_options.Discoverable) {
                            lock (CurrentGate) { Running.Add(this); }
                        }
                        _thread = new Thread(RenderLoop) { IsBackground = true, Name = "ConsoleMonster render" };
                        _thread.Start();
                    } catch {
                        cleanup = true;
                        CloseAdmission();
                        Volatile.Write(ref _state, 2);
                        throw;
                    }
                }
            } catch {
                // Cancellation registrations may synchronously wait for callbacks that acquire _lifecycle.
                if (cleanup) { FinishStop(); }
                throw;
            }
        }

        // Measures emoji, ZWJ, flag and ambiguous widths once per process on the alternate screen (samples are erased and
        // the first frame clears the screen), keeps explicit CONSOLEMONSTER_*_WIDTH settings, and makes the result the
        // process-wide measurement profile. An ambiguous-wide terminal also gets ASCII glyphs, since box drawing and
        // block elements are ambiguous too.
        private void ApplyWidthProbe(TerminalProfile profile) {
            if (!profile.ProbeWidths || !_onConsole || !(_output is VtOutput vt)) {
                return;
            }
            bool measuredOk = vt.TryProbeWidths(TimeSpan.FromMilliseconds(500), out TerminalWidthProfile measured, out string decision);
            WidthDecision = decision;
            if (!measuredOk) {
                return;
            }
            TerminalWidthProfile configured = profile.WidthProfile;
            if (IsExplicitWidth("CONSOLEMONSTER_EMOJI_WIDTH")) {
                measured = measured.WithEmojiWidth(configured.EmojiWidth);
            }
            if (IsExplicitWidth("CONSOLEMONSTER_AMBIGUOUS_WIDTH")) {
                measured = measured.WithAmbiguousWidth(configured.AmbiguousWidth);
            }
            DisplayWidth.Profile = measured;
            if (measured.AmbiguousWidth == 2 && profile.Glyphs != GlyphTier.Ascii) {
                TerminalProfile downgraded = profile.WithGlyphs(GlyphTier.Ascii).WithWidthProfile(measured).WithDecision("glyphs downgraded: the probe measured ambiguous width 2");
                _context = Terminal.CreateContext(downgraded, _theme);
            }
        }

        private static StreamWriter OpenRecording(string path) {
            try {
                return new StreamWriter(path, false, new UTF8Encoding(false));
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException) {
                throw new IOException("Cannot write the recording to '" + path + "': " + ex.Message, ex);
            }
        }

        private static bool IsExplicitWidth(string name) {
            string? value = Environment.GetEnvironmentVariable(name);
            return value == "1" || value == "2";
        }

        /// <summary>
        /// Stops admission, cancels work and joins for up to StopTimeout (two seconds by default).
        /// Accepted events are drained on the stopping caller after joining, before restoration.
        /// If user code blocks the render thread, console capture is restored immediately and final cleanup is deferred
        /// until that callback returns. A call from the render thread also defers final cleanup to its loop exit.
        /// </summary>
        public void Stop() {
            Thread? thread;
            lock (_lifecycle) {
                if (_state == 3) { return; }
                if (_state < 2) {
                    _callerStopping = Thread.CurrentThread != _thread;
                    CloseAdmission();
                    Volatile.Write(ref _state, 2);
                }
                thread = _thread;
                _wake.Set();
            }
            CancelWork();
            if (Thread.CurrentThread == thread) { return; }
            if (thread != null && !thread.Join(_options.StopTimeout)) {
                lock (_lifecycle) { _cleanupDeferred = true; }
                Error(new TimeoutException("The render callback has not returned; final cleanup is deferred."));
                RestoreCapture();
                bool finish;
                lock (_lifecycle) {
                    finish = _callerStopping && _renderExited;
                    _callerStopping = false;
                }
                if (finish) { FinishStop(); }
                return;
            }
            FinishStop();
        }

        /// <summary>Signals cooperative cancellation, stopping immediately only when StopOnCancellation is true. Call Stop after the producer has drained.</summary>
        public void Cancel() => RequestCancellation(_options.StopOnCancellation);

        private void RequestCancellation() => RequestCancellation(true);

        private void RequestCancellation(bool stop) {
            lock (_lifecycle) {
                if (_state >= 2) { return; }
                if (stop) {
                    CloseAdmission();
                    Volatile.Write(ref _state, 2);
                    _wake.Set();
                }
            }
            CancelWork();
        }

        private void CancelWork() {
            try { _cancellation.Cancel(); }
            catch (Exception ex) { Error(ex); }
        }

        private void CaptureLine(string line, string level) {
            // Capture owns its own admission during shutdown, until both partial buffers have been flushed.
            Enqueue(new LiveEvent("$log", "write", text: line, state: level));
        }

        private void RestoreCapture() {
            lock (CurrentGate) {
                _capture?.Dispose();
                _capture = null;
                if (ReferenceEquals(_captureOwner, this)) { _captureOwner = null; }
            }
        }

        private void RestoreTerminal() {
            RestoreCapture();
            if (_taskbarUsed) {
                // Ctrl+C, an exception or process exit: the tab and taskbar must not keep showing progress.
                TaskbarProgress.Clear();
            }
            lock (_outputGate) {
                if (_restored) { return; }
                _restored = true;
                _output?.EndLive(); // Outputs flush their writers here; there is no separate Flush contract.
            }
        }

        private void FinishStop() {
            lock (_finishGate) {
                if (Volatile.Read(ref _state) == 3 || _finishing) { return; }
                _finishing = true;
                CancelWork();
                // A producer admitted just before Stop must finish publishing before the final drain.
                SpinWait producers = new SpinWait();
                while (Volatile.Read(ref _admission) != int.MinValue) { producers.SpinOnce(); }
                Attempt(RestoreCapture, fatal: true);
                // A failed event must not abandon later accepted events or their flush waiters during shutdown.
                while (_queue.TryDequeue(out LiveEvent? pending)) { Attempt(() => ApplyOne(pending), fatal: true); }
                Attempt(() => RecordSlotSnapshots(true), fatal: RecordingRequired);
                // A start that failed before BeginLive never owned the screen: no final frame, no summary.
                bool drawn = _began && _output != null && _context != null;
                if (drawn) { Attempt(RenderFinal, fatal: true); }
                Attempt(RestoreTerminal, fatal: true);
                if (_taskbarUsed) {
                    Attempt(TaskbarProgress.Clear);
                }
                if (_options.NotifyOnFinish && _onConsole) {
                    Attempt(TaskbarProgress.Bell);
                }
                _guardRegistration.Dispose();
                Attempt(() => _recorder?.Dispose(), fatal: RecordingRequired);
                CellBuffer? summaryFrame = null;
                if (drawn && _output != null && _context != null) {
                    IWidget? summary = _options.Summary ?? (_options.PrintFinalState ? _root : null);
                    // User rendering can resolve a profile, write a late block or request Snapshot. Keep it outside the
                    // physical output lock; ownership and the closing queue remain in place until the final flush ends.
                    if (summary != null) {
                        Attempt(() => summaryFrame = FrameComposer.RenderBlock(summary, _context.Profile.Width, _context.WithClock(FramesRendered, Elapsed)), fatal: true);
                    }
                }
                lock (_outputGate) {
                    if (drawn && _output != null && _context != null) {
                        Attempt(() => {
                            FlushUnrouted();
                            if (summaryFrame != null) { _output.WriteBlock(summaryFrame); }
                            // A summary widget or an output callback can itself emit a one-shot write during cleanup.
                            FlushUnrouted();
                        }, fatal: true);
                    }
                    if (_options.Output == null) { Attempt(() => _output?.Dispose(), fatal: true); }
                    // Final scrollback still needs VT processing. Restore the original native mode after its last write.
                    Attempt(() => _guard?.Dispose(), fatal: true);
                    _outputFinished = true;
                    // A new normal writer may start only after accepted blocks, scrollback and restoration are done.
                    Terminal.ReleaseConsole(this);
                }
                Attempt(() => _stoppedElapsed = Scale(ReadElapsed()));
                _watch.Stop();
                lock (CurrentGate) { Running.Remove(this); }
                lock (_lifecycle) {
                    Volatile.Write(ref _state, 3);
                    _wake.Dispose();
                    _outputStopped.TrySetResult(true);
                }
            }
        }

        private void Attempt(Action action, bool fatal = false) {
            try { action(); }
            catch (Exception ex) {
                if (fatal) { Fail(ex); }
                else { Error(ex); }
            }
        }
    }
}
