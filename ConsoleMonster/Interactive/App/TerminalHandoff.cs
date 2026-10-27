using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Sessions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>Hands a running app's terminal to an owned child PTY and restores its screen, raw input and focus afterwards. No recording or input history is written.</summary>
    public sealed class TerminalHandoff {
        private readonly object _gate = new object();
        private readonly StringBuilder _lastOutput = new StringBuilder();
        private IInputSource? _input;
        private LiveSession? _session;
        private TerminalProfile? _profile;
        private int _thread;
        private bool _running;
        private CancellationToken _appCancellation;

        /// <summary>Maximum retained output characters. Old output is dropped from the front; no child input is retained.</summary>
        public int OutputCapacity { get; set; } = 256 * 1024;
        /// <summary>The last child's bounded terminal output, including its colours and terminal sequences.</summary>
        public string LastOutput { get { lock (_gate) { return _lastOutput.ToString(); } } }
        /// <summary>Whether a child currently owns the terminal.</summary>
        public bool Running { get { lock (_gate) { return _running; } } }

        /// <summary>Shows the last child's terminal screen until any key is pressed. Resizes redraw the output; focus and app modes are retained.</summary>
        public void ShowOutput(CancellationToken cancellationToken = default) {
            LiveSession session;
            IInputSource input;
            TerminalProfile profile;
            lock (_gate) {
                if (_session == null || _input == null || _profile == null || _thread != Thread.CurrentThread.ManagedThreadId || _running) { throw new InvalidOperationException("Output view must run on its owning app thread with no active child."); }
                session = _session; input = _input; profile = _profile;
            }
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _appCancellation);
            cancellationToken = cancellation.Token;
            using LivePause pause = session.Pause();
            using IDisposable? mouse = MouseReporting.SuspendCurrent();
            int width = profile.Width, height = profile.Height;
            if (TerminalSize.TryGet(out int actualWidth, out int actualHeight) && actualWidth > 0 && actualHeight > 0) { width = actualWidth; height = actualHeight; }
            void Draw() {
                var screen = new VtScreen(width, height, keepScrollback: false);
                screen.Feed(LastOutput);
                CellBuffer frame = screen.Snapshot();
                var region = new Region(frame);
                for (int x = 0; x < width; x++) { region.Put(x, height - 1, " ", Style.Plain); }
                region.Write(0, height - 1, Text.TextWrapper.Truncate("Last output · any key returns", width, "…"), Style.Plain.With(TextDecoration.Reverse));
                pause.Present(frame);
            }
            try {
                Draw();
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();
                    InputEvent? next = input.Read(TimeSpan.FromMilliseconds(30), cancellationToken);
                    if (next?.Kind == InputEventKind.Key || next?.Kind == InputEventKind.Paste) { return; }
                    if (next?.Kind == InputEventKind.Resize && next.Width > 0 && next.Height > 0) { width = next.Width; height = next.Height; Draw(); }
                }
            } finally { session.Invalidate(); }
        }

        internal IDisposable Attach(IInputSource input, LiveSession session, TerminalProfile profile, CancellationToken appCancellation = default) {
            lock (_gate) {
                if (_session != null) { throw new InvalidOperationException("This terminal handoff already belongs to an app."); }
                _input = input; _session = session; _profile = profile; _thread = Thread.CurrentThread.ManagedThreadId;
                _appCancellation = appCancellation;
                return new Attachment(this);
            }
        }

        /// <summary>Runs a child on a real terminal. Call on the owning app thread from its registry or callback. Nonzero exit codes are returned; startup and transport errors escape after restoration.</summary>
        public int SuspendForChild(TerminalChildOptions options, CancellationToken cancellationToken = default) {
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            if (OutputCapacity < 1024 || OutputCapacity > 16 * 1024 * 1024) { throw new ArgumentOutOfRangeException(nameof(OutputCapacity)); }
            IInputSource input;
            LiveSession session;
            TerminalProfile profile;
            lock (_gate) {
                if (_session == null || _input == null || _profile == null || _thread != Thread.CurrentThread.ManagedThreadId) { throw new InvalidOperationException("Terminal handoff must run on its owning app thread."); }
                if (_running) { throw new InvalidOperationException("A child already owns this terminal."); }
                if (!(_input is IConsoleInputMode) || Console.IsInputRedirected || Console.IsOutputRedirected) { throw new InvalidOperationException("Terminal handoff requires the app's physical TTY input and output."); }
                input = _input; session = _session; profile = _profile;
                _running = true;
            }
            try {
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _appCancellation);
                cancellationToken = cancellation.Token;
                cancellationToken.ThrowIfCancellationRequested();
                var child = new SessionRecordingOptions { Shell = options.FileName, WorkingDirectory = options.WorkingDirectory, Width = profile.Width, Height = profile.Height, Timeout = options.Timeout };
                if (TerminalSize.TryGet(out int width, out int height) && width > 1 && height > 1) { child.Width = width; child.Height = height; }
                foreach (string argument in options.Arguments) { child.Arguments.Add(argument); }
                foreach (var variable in options.Environment) { child.Environment.Add(variable); }
                child.Validate();
                using IDisposable screen = session.SuspendTerminal();
                using IDisposable? mouse = MouseReporting.SuspendCurrent();
                var mode = (IConsoleInputMode)input;
                mode.Suspend();
                try {
                    using ITerminalWriter output = TerminalWriterFactory.Create(profile);
                    using IPseudoTerminal terminal = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? (IPseudoTerminal)new WindowsPseudoTerminal(child) : new UnixPseudoTerminal(child);
                    lock (_gate) { _lastOutput.Clear(); }
                    return Pump(terminal, child, output, cancellationToken);
                } finally { mode.Resume(); }
            } finally { lock (_gate) { _running = false; } }
        }

        private int Pump(IPseudoTerminal terminal, SessionRecordingOptions options, ITerminalWriter output, CancellationToken cancellation) {
            Exception? readerError = null;
            var reader = new Thread(() => {
                try {
                    Decoder decoder = new UTF8Encoding(false).GetDecoder();
                    var bytes = new byte[8192];
                    var chars = new char[Encoding.UTF8.GetMaxCharCount(bytes.Length)];
                    while (true) {
                        int length;
                        try { length = terminal.Read(bytes); }
                        catch (IOException) when (terminal.HasExited) {
                            // Windows reports a closed ConPTY output pipe as EOF after the child exits.
                            break;
                        }
                        if (length <= 0) { break; }
                        string text = new string(chars, 0, decoder.GetChars(bytes, 0, length, chars, 0, false));
                        AppendOutput(text);
                        output.Write(text); output.Flush();
                    }
                } catch (Exception error) { Interlocked.Exchange(ref readerError, error); }
            }) { IsBackground = true, Name = "ConsoleMonster child output" };
            reader.Start();
            try {
                using var relay = new TerminalInputRelay(preserveOutputProcessing: true);
                Stopwatch elapsed = Stopwatch.StartNew();
                int width = options.Width, height = options.Height;
                while (!terminal.HasExited) {
                    cancellation.ThrowIfCancellationRequested();
                    Exception? failure = Interlocked.CompareExchange(ref readerError, null, null);
                    if (failure != null) { throw new IOException("Child terminal output failed.", failure); }
                    if (elapsed.Elapsed > options.Timeout) { throw new TimeoutException("The child terminal exceeded its time limit."); }
                    string keys = relay.ReadAvailable();
                    // Resize may arrive with the same input batch. Apply it before delivering that batch.
                    if (TerminalSize.TryGet(out int nextWidth, out int nextHeight) && nextWidth > 1 && nextHeight > 1 && (nextWidth != width || nextHeight != height)) {
                        terminal.Resize(nextWidth, nextHeight); width = nextWidth; height = nextHeight;
                    }
                    if (keys.Length > 0) { terminal.Write(Encoding.UTF8.GetBytes(keys)); }
                    cancellation.WaitHandle.WaitOne(10);
                }
                terminal.Complete();
                if (!reader.Join(TimeSpan.FromSeconds(10))) { terminal.Dispose(); if (!reader.Join(TimeSpan.FromSeconds(2))) { throw new IOException("Child output did not drain."); } }
                if (readerError != null) { throw new IOException("Child terminal output failed.", readerError); }
                return terminal.ExitCode;
            } finally {
                terminal.Stop();
                terminal.Complete();
                if (reader.IsAlive && !reader.Join(TimeSpan.FromSeconds(10))) { terminal.Dispose(); reader.Join(TimeSpan.FromSeconds(2)); }
            }
        }

        private void AppendOutput(string text) {
            lock (_gate) {
                _lastOutput.Append(text);
                if (_lastOutput.Length > OutputCapacity) { _lastOutput.Remove(0, _lastOutput.Length - OutputCapacity); }
            }
        }

        private sealed class Attachment : IDisposable {
            private TerminalHandoff? _owner;
            internal Attachment(TerminalHandoff owner) { _owner = owner; }
            public void Dispose() {
                TerminalHandoff? owner = Interlocked.Exchange(ref _owner, null);
                if (owner == null) { return; }
                lock (owner._gate) { owner._session = null; owner._input = null; owner._profile = null; owner._thread = 0; owner._appCancellation = default; }
            }
        }
    }
}
