using System;
using System.IO;
using System.Text;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Text;

namespace ConsoleMonster.Live {
    public sealed partial class LiveSession {
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
                        ResolveLogTarget();
                        TerminalProfile profile = _options.Profile ?? Terminal.Profile;
                        _context = Terminal.CreateContext(profile, _options.Theme);
                        _output = _options.Output ?? Terminal.CreateOutput(profile);
                        _clockStart = _options.Clock?.Invoke() ?? TimeSpan.Zero;
                        _watch.Start();
                        Volatile.Write(ref _state, 1);
                        _guard = new TerminalGuard(RestoreTerminal, _options.HandleCancelKey);
                        _guardRegistration = _guard.Cancellation.Register(RequestCancellation);
                        if (_options.RecordWriter != null) { _recorder = new LiveRecorder(_options.RecordWriter); }
                        else if (_options.RecordPath != null) {
                            StreamWriter writer = new StreamWriter(_options.RecordPath, false, new UTF8Encoding(false));
                            try { _recorder = new LiveRecorder(writer, false); }
                            catch { writer.Dispose(); throw; }
                        }
                        if (_options.CaptureConsole) {
                            lock (CurrentGate) {
                                if (_captureOwner != null) { throw new InvalidOperationException("Another session already captures Console.Out/Error."); }
                                _captureOwner = this;
                                _capture = new ConsoleCapture(CaptureLine);
                            }
                        }
                        _output.BeginLive();
                        lock (CurrentGate) { Running.Add(this); }
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

        private void RequestCancellation() {
            lock (_lifecycle) {
                if (_state >= 2) { return; }
                CloseAdmission();
                Volatile.Write(ref _state, 2);
                _wake.Set();
            }
            CancelWork();
        }

        private void CancelWork() {
            try { _cancellation.Cancel(); }
            catch (Exception ex) { Error(ex); }
        }

        private void CaptureLine(string line, string level) {
            // Capture owns its own admission during shutdown, until both partial buffers have been flushed.
            if (Volatile.Read(ref _logTarget) == null) { _unroutedLines.Enqueue(line); }
            else { Enqueue(new LiveEvent("$log", "write", text: line, state: level)); }
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
                Attempt(RestoreCapture);
                Attempt(() => Drain(int.MaxValue));
                if (_output != null && _context != null) { Attempt(RenderFinal); }
                Attempt(RestoreTerminal);
                _guardRegistration.Dispose();
                Attempt(() => _guard?.Dispose());
                Attempt(() => _recorder?.Dispose());
                if (_output != null && _context != null) {
                    Attempt(() => {
                        while (_unroutedLines.TryDequeue(out string? line)) { _output.WriteLine(StyledLine.From(line)); }
                        IWidget? summary = _options.Summary ?? (_options.PrintFinalState ? _root : null);
                        if (summary != null) {
                            _output.WriteBlock(FrameComposer.RenderBlock(summary, _context.Profile.Width, _context.WithClock(FramesRendered, Elapsed)));
                        }
                    });
                }
                if (_options.Output == null) { Attempt(() => _output?.Dispose()); }
                Attempt(() => _stoppedElapsed = ReadElapsed());
                _watch.Stop();
                lock (CurrentGate) { Running.Remove(this); }
                lock (_lifecycle) {
                    Volatile.Write(ref _state, 3);
                    _wake.Dispose();
                }
            }
        }

        private void Attempt(Action action) {
            try { action(); }
            catch (Exception ex) { Error(ex); }
        }
    }
}
