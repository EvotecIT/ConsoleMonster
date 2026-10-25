using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ConsoleMonster.Export;

namespace ConsoleMonster.Sessions {
    /// <summary>
    /// Records a child terminal to local asciicast v2. Raw input is forwarded but never stored, so password input
    /// remains private when applications disable echo. Secret patterns are applied to output before disk writes.
    /// </summary>
    public sealed class SessionRecorder : IDisposable {
        private readonly object _gate = new object();
        private readonly object _lifetime = new object();
        private readonly SessionCastWriter _writer;
        private readonly SessionRecordingOptions _options;
        private IPseudoTerminal? _terminal;
        private Thread? _reader;
        private Exception? _readError;
        private volatile bool _disposed;
        private volatile bool _stopping;
        private bool _captureEnabled = true;

        // Demo setup still reaches its readiness observer but never the recording.
        internal void SetCaptureEnabled(bool enabled) {
            lock (_gate) {
                if (enabled && !_captureEnabled) { _writer.ResumeCapture(); }
                _captureEnabled = enabled;
            }
        }

        private SessionRecorder(SessionRecordingOptions options, Action<string>? output) {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            options.Validate();
            _captureEnabled = options.CaptureInitiallyEnabled;
            Output = output;
            _writer = new SessionCastWriter(options, options.InProcess ? "in-process PowerShell output only" : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ConPTY" : "openpty");
            try {
                if (!options.InProcess) {
                    _terminal = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? (IPseudoTerminal)new WindowsPseudoTerminal(options) : new UnixPseudoTerminal(options);
                }
                if (_terminal != null) {
                    _reader = new Thread(ReadOutput) { IsBackground = true, Name = "ConsoleMonster recording output" };
                    _reader.Start();
                }
            } catch { _terminal?.Dispose(); _writer.Dispose(); throw; }
        }

        /// <summary>Starts a recorded child process, or a labelled capture sink for a PowerShell host adapter.</summary>
        public static SessionRecorder Start(SessionRecordingOptions options, Action<string>? output = null) => new SessionRecorder(options, output);
        /// <summary>The local cast file.</summary>
        public string Path => _writer.Path;
        /// <summary>The capture mode, also stored in the cast header and sidecar.</summary>
        public string Mode => _writer.Mode;
        /// <summary>Whether capture reached its configured byte cap.</summary>
        public bool LimitReached => _writer.LimitReached;
        /// <summary>Whether the child exited or Stop was called.</summary>
        public bool HasExited => _stopping || (_terminal?.HasExited ?? _disposed);
        /// <summary>Child exit code; available after WaitForExit.</summary>
        public int ExitCode { get; private set; }
        /// <summary>Whether the recorded child sent Stop-ConsoleRecording.</summary>
        public bool StopRequested => _writer.StopRequested;
        /// <summary>Live unredacted terminal output, with private recording controls consumed. Subscribers must not persist it.</summary>
        public event Action<string>? Output;

        private void ReadOutput() {
            Decoder decoder = new UTF8Encoding(false).GetDecoder();
            byte[] bytes = new byte[8192]; char[] chars = new char[Encoding.UTF8.GetMaxCharCount(8192)];
            try {
                while (true) {
                    int count = _terminal!.Read(bytes);
                    if (count <= 0) { break; }
                    int length = decoder.GetChars(bytes, 0, count, chars, 0, false);
                    string text = new string(chars, 0, length);
                    string visible;
                    lock (_gate) { visible = _captureEnabled ? _writer.Append(text) : text; }
                    if (visible.Length > 0) { Output?.Invoke(visible); }
                    if (_writer.StopRequested) { _stopping = true; _terminal.Stop(); }
                }
            } catch (IOException) when (_stopping || (_terminal?.HasExited ?? true)) {
                // A closed PTY is EOF, including Windows broken-pipe on child exit.
            } catch (Exception error) { _readError = error; _stopping = true; _terminal?.Stop(); }
        }

        /// <summary>Forwards UTF-8 input without recording it. Applications control password echo through the PTY.</summary>
        public void SendInput(string input) {
            if (_disposed) { throw new ObjectDisposedException(nameof(SessionRecorder)); }
            if (_terminal == null) { throw new InvalidOperationException("The in-process adapter has no terminal input."); }
            _terminal.Write(Encoding.UTF8.GetBytes(input ?? throw new ArgumentNullException(nameof(input))));
        }
        /// <summary>Resizes the child terminal and records the new dimensions.</summary>
        public void Resize(int width, int height) {
            if (width < 2 || height < 2 || width > 1000 || height > 1000 || (long)width * height > 250000) { throw new ArgumentOutOfRangeException(nameof(width)); }
            lock (_gate) {
                if (_disposed) { throw new ObjectDisposedException(nameof(SessionRecorder)); }
                _terminal?.Resize(width, height);
                _writer.Resize(width, height);
            }
        }
        /// <summary>Adds a chapter without printing it to the terminal.</summary>
        public void AddMarker(string label) { lock (_gate) { _writer.Marker(label ?? throw new ArgumentNullException(nameof(label))); } }
        /// <summary>Adds an export hold without waiting during capture. A label also starts a chapter.</summary>
        public void AddMarker(TimeSpan hold, string? label = null) { lock (_gate) { _writer.Marker(label ?? string.Empty, hold, null, label != null); } }
        /// <summary>Records the reading pace for subsequent exported output.</summary>
        public void SetPace(RecordingPace pace) { lock (_gate) { _writer.Marker(string.Empty, TimeSpan.Zero, pace, false); } }
        /// <summary>Writes PowerShell host output into an explicitly selected in-process recording.</summary>
        public void CapturePowerShellOutput(string text) {
            if (!_options.InProcess) { throw new InvalidOperationException("Only the in-process PowerShell adapter can submit output."); }
            lock (_gate) { if (_disposed) { throw new ObjectDisposedException(nameof(SessionRecorder)); } _writer.Append(text); }
        }
        /// <summary>Waits for the child, drains its output, and finalizes both local files. Cancellation stops the owned child.</summary>
        public void WaitForExit(CancellationToken cancellationToken = default) {
            if (_terminal == null) { return; }
            Stopwatch clock = Stopwatch.StartNew();
            try {
                while (!_terminal.HasExited && !_stopping) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (clock.Elapsed > _options.Timeout) { throw new TimeoutException("The recorded child exceeded its configured timeout."); }
                    Thread.Sleep(20);
                }
                ExitCode = _writer.StopRequested ? 0 : _terminal.ExitCode;
            } finally { Dispose(); }
            if (_readError != null) { throw new IOException("Reading the recorded terminal failed.", _readError); }
        }
        /// <summary>Stops the owned child and finalizes the recording. Does not affect other terminals.</summary>
        public void Stop() { _stopping = true; Dispose(); }
        /// <summary>Creates the private OSC chapter control for a child shell to write to its output.</summary>
        public static string MarkerSequence(string label) => SessionCastWriter.ControlPrefix + "marker;" + Convert.ToBase64String(Encoding.UTF8.GetBytes(label ?? throw new ArgumentNullException(nameof(label)))) + "\a";
        /// <summary>Private OSC export-hold control for any recorded child script. A label also starts a chapter.</summary>
        public static string MarkerSequence(TimeSpan hold, string? label = null) => SessionCastWriter.ControlPrefix + "timeline;" + Convert.ToBase64String(Encoding.UTF8.GetBytes(SessionMarkerData.Write(label ?? string.Empty, hold, null, label != null))) + "\a";
        /// <summary>Private OSC stop control for the recorded child shell.</summary>
        public static string StopSequence => SessionCastWriter.ControlPrefix + "stop\a";
        /// <summary>Stops and disposes this recorder's transport and files.</summary>
        public void Dispose() {
            if (_reader == Thread.CurrentThread) {
                // A subscriber may request Stop while handling live output. The waiting/calling thread performs
                // teardown after this callback returns, so ConPTY can keep draining on its reader thread.
                _stopping = true; _terminal?.Stop(); return;
            }
            lock (_lifetime) { Finish(); }
        }
        private void Finish() {
            if (_disposed) { return; }
            _stopping = true;
            _terminal?.Stop();
            // Closing ConPTY must happen concurrently with the output reader, which drains its final writes.
            if (_terminal is WindowsPseudoTerminal windows) { windows.Complete(); }
            if (_terminal is UnixPseudoTerminal unix) { unix.Complete(); }
            if (_reader != null && _reader != Thread.CurrentThread && !_reader.Join(TimeSpan.FromSeconds(10))) {
                _terminal?.Dispose();
                if (!_reader.Join(TimeSpan.FromSeconds(2))) { throw new IOException("The recording output reader did not finish."); }
            }
            _terminal?.Dispose();
            lock (_gate) { if (!_disposed) { _writer.Dispose(); _disposed = true; } }
        }
    }
}
