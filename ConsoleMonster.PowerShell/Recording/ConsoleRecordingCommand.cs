using System;
using System.Collections.Concurrent;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Threading;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary>Common PowerShell argument translation and terminal relay for recording commands.</summary>
    public abstract class ConsoleRecordingCommand : PSCmdlet, IDisposable {
        private SessionRecorder? _recorder;
        private volatile bool _stopping;
        /// <summary>Whether PowerShell cancelled this command.</summary>
        protected bool IsStopping => _stopping;
        /// <summary>The current source/package module entrypoint, including its script commands when present.</summary>
        protected string ModuleImportPath {
            get {
                string? moduleBase = MyInvocation.MyCommand.Module?.ModuleBase;
                string? manifest = moduleBase == null ? null : System.IO.Path.Combine(moduleBase, "ConsoleMonster.psd1");
                return manifest != null && File.Exists(manifest) ? manifest : typeof(ConsoleRecordingCommand).Assembly.Location;
            }
        }
        /// <summary><para type="description">Local asciicast v2 output; omitted names are timestamped.</para></summary>
        [Parameter] public string? Path { get; set; }
        /// <summary><para type="description">Folder for automatic cast names.</para></summary>
        [Parameter] public string? Folder { get; set; }
        /// <summary><para type="description">Recording title.</para></summary>
        [Parameter] public string Name { get; set; } = "Console session";
        /// <summary><para type="description">Shell executable. The default is the current PowerShell executable.</para></summary>
        [Parameter] public string? Shell { get; set; }
        /// <summary><para type="description">Initial terminal width in cells.</para></summary>
        [Parameter] [ValidateRange(2, 1000)] public int Width { get; set; } = 100;
        /// <summary><para type="description">Initial terminal height in cells.</para></summary>
        [Parameter] [ValidateRange(2, 1000)] public int Height { get; set; } = 30;
        /// <summary><para type="description">Additional regular expressions to redact from recorded output.</para></summary>
        [Parameter] public string[] Redact { get; set; } = Array.Empty<string>();
        /// <summary><para type="description">Maximum cast bytes. At the cap recording stops with a warning.</para></summary>
        [Parameter] [ValidateRange(1024, long.MaxValue)] public long MaxBytes { get; set; } = 64 * 1024 * 1024;
        /// <summary><para type="description">Longest playback idle gap in seconds; zero keeps original timing.</para></summary>
        [Parameter] [ValidateRange(0, 86400)] public double IdleLimit { get; set; } = 2;
        /// <summary><para type="description">Explicit limited fallback: PowerShell host output in this process; no native terminal capture.</para></summary>
        [Parameter] public SwitchParameter InProcess { get; set; }
        /// <summary><para type="description">Maximum child lifetime in seconds.</para></summary>
        [Parameter] [ValidateRange(1, 604800)] public int TimeoutSeconds { get; set; } = 86400;

        /// <summary>Maps PowerShell paths and arguments into the shared recording API.</summary>
        protected SessionRecordingOptions Options() {
            SessionRecordingOptions options = new SessionRecordingOptions {
                Path = Path == null ? null : SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path),
                Name = Name, Shell = Shell ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName,
                Width = Width, Height = Height, MaxBytes = MaxBytes, IdleLimit = TimeSpan.FromSeconds(IdleLimit),
                WorkingDirectory = SessionState.Path.CurrentFileSystemLocation.Path, InProcess = InProcess.IsPresent, Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };
            if (Folder != null) { options.Folder = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Folder); }
            foreach (string pattern in Redact) { options.Redact.Add(pattern); }
            return options;
        }
        /// <summary>Runs a PTY and drains host output on the pipeline thread.</summary>
        protected string Run(SessionRecordingOptions options, bool interactive) {
            ConcurrentQueue<string> output = new ConcurrentQueue<string>();
            string? title = null;
            TerminalInputRelay? input = null;
            try {
                try { title = Host.UI.RawUI.WindowTitle; Host.UI.RawUI.WindowTitle = "● REC " + options.Name; } catch (Exception error) when (error is IOException || error is NotSupportedException || error is NotImplementedException) { }
                if (interactive && Console.IsInputRedirected) { throw new InvalidOperationException("Interactive recording needs a terminal. Use Invoke-ConsoleRecording for scripts."); }
                _recorder = SessionRecorder.Start(options, text => output.Enqueue(text));
                if (interactive) { input = new TerminalInputRelay(); }
                int width = options.Width, height = options.Height;
                bool warned = false;
                System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
                while (!_recorder.HasExited && !_stopping) {
                    if (clock.Elapsed > options.Timeout) { throw new TimeoutException("The recorded child exceeded TimeoutSeconds."); }
                    Drain(output);
                    if (title != null) { Host.UI.RawUI.WindowTitle = "● REC " + options.Name; }
                    if (interactive) {
                        string bytes = input!.ReadAvailable();
                        if (bytes.Length > 0) { _recorder.SendInput(bytes); }
                        int newWidth = Console.WindowWidth, newHeight = Console.WindowHeight;
                        if (newWidth >= 2 && newHeight >= 2 && (width != newWidth || height != newHeight)) { _recorder.Resize(newWidth, newHeight); width = newWidth; height = newHeight; }
                    }
                    if (_recorder.LimitReached && !warned) { WriteWarning("The recording reached MaxBytes. The child is still running; further output is not recorded."); warned = true; }
                    Thread.Sleep(20);
                }
                if (_stopping) { _recorder.Stop(); } else { _recorder.WaitForExit(); }
                Drain(output);
                if (_recorder.LimitReached && !warned) { WriteWarning("The recording reached MaxBytes and was finalized at the cap."); }
                if (_recorder.ExitCode != 0 && !_stopping) { WriteError(new ErrorRecord(new InvalidOperationException("The recorded child exited with code " + _recorder.ExitCode + "."), "ConsoleRecordingChildFailed", ErrorCategory.OperationStopped, _recorder.Path)); }
                return _recorder.Path;
            } finally {
                _recorder?.Dispose();
                input?.Dispose();
                if (title != null) { try { Host.UI.RawUI.WindowTitle = title; } catch (IOException) { } }
            }
        }
        private void Drain(ConcurrentQueue<string> output) {
            while (output.TryDequeue(out string? text)) { Host.UI.Write(text); }
        }
        /// <inheritdoc/>
        protected override void StopProcessing() { _stopping = true; _recorder?.Stop(); }
        /// <summary>Releases this command's recorder, including after cancellation.</summary>
        public void Dispose() { _recorder?.Dispose(); }
    }
}
