using System;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary>Common PowerShell argument translation and terminal relay for recording commands.</summary>
    public abstract class ConsoleRecordingCommand : NoPipelineInputCmdlet, IDisposable {
        private SessionRecorder? _recorder;
        private RecordingOutputRelay? _relay;
        private volatile bool _stopping;
        /// <summary>Whether PowerShell cancelled this command.</summary>
        protected bool IsStopping => _stopping;
        /// <summary>The current source/package module entrypoint, including its script commands when present.</summary>
        protected string ModuleImportPath {
            get => PSModuleImport.PathFor(this);
        }
        /// <summary><para type="description">Local asciicast v2 output; omitted names are timestamped.</para></summary>
        [Parameter(ParameterSetName = "Script")] public string? RecordingPath { get; set; }
        /// <summary><para type="description">Recording title.</para></summary>
        [Parameter] public string Title { get; set; } = "Console session";
        /// <summary><para type="description">Shell executable. The default is the current PowerShell executable.</para></summary>
        [Parameter] public string? Shell { get; set; }
        /// <summary><para type="description">Initial terminal width in cells.</para></summary>
        [Parameter] [ValidateRange(0, 1000)] public int Width { get; set; }
        /// <summary><para type="description">Initial terminal height in cells.</para></summary>
        [Parameter] [ValidateRange(2, 1000)] public int Height { get; set; } = 30;
        /// <summary><para type="description">Additional regular expressions to redact from recorded output.</para></summary>
        [Parameter] public string[] Redact { get; set; } = Array.Empty<string>();
        /// <summary><para type="description">Visible replacement for matched output (default [REDACTED]). No control characters.</para></summary>
        [Parameter, PSDefaultValue(Value = "[REDACTED]")] public string RedactionText { get; set; } = "[REDACTED]";
        /// <summary><para type="description">Maximum cast bytes. At the cap recording stops with a warning.</para></summary>
        [Parameter] [ValidateRange(1024, long.MaxValue)] public long MaxBytes { get; set; } = 64 * 1024 * 1024;
        /// <summary><para type="description">Asciicast player idle-limit hint in seconds. Source timestamps are always preserved; zero disables the hint.</para></summary>
        [Parameter] [ValidateRange(0, 86400)] public double IdleLimitSeconds { get; set; } = 2;
        /// <summary><para type="description">Explicit limited fallback: PowerShell host output in this process; no native terminal capture.</para></summary>
        [Parameter] public SwitchParameter InProcess { get; set; }
        /// <summary><para type="description">Maximum child lifetime in seconds.</para></summary>
        [Parameter] [ValidateRange(0.01, 604800)] public double TimeoutSeconds { get; set; } = 86400;

        /// <summary>Maps PowerShell paths and arguments into the shared recording API.</summary>
        protected SessionRecordingOptions Options() {
            SessionRecordingOptions options = new SessionRecordingOptions {
                Path = RecordingPath == null ? null : SessionState.Path.GetUnresolvedProviderPathFromPSPath(RecordingPath),
                Title = Title, Shell = Shell ?? System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName, RedactionText = RedactionText,
                Width = Width, Height = Height, MaxBytes = MaxBytes, IdleLimit = TimeSpan.FromSeconds(IdleLimitSeconds),
                WorkingDirectory = SessionState.Path.CurrentFileSystemLocation.Path, InProcess = InProcess.IsPresent, Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };
            foreach (string pattern in Redact) { options.Redact.Add(pattern); }
            return options;
        }
        /// <summary>Runs a PTY and drains host output on the pipeline thread.</summary>
        protected string Run(SessionRecordingOptions options, bool interactive) {
            RecordingOutputRelay output = _relay = new RecordingOutputRelay();
            Task? completion = null;
            string? title = null;
            TerminalInputRelay? input = null;
            try {
                try { title = Host.UI.RawUI.WindowTitle; Host.UI.RawUI.WindowTitle = "● REC " + options.Title; } catch (Exception error) when (error is IOException || error is NotSupportedException || error is NotImplementedException) { }
                if (interactive && Console.IsInputRedirected) { throw new InvalidOperationException("Interactive recording needs a terminal. Use Invoke-ConsoleRecording for scripts."); }
                _recorder = SessionRecorder.Start(options, output.Receive);
                // Finalization joins the reader. Keep the pipeline draining while that reader can be backpressured.
                completion = Task.Run(() => _recorder.WaitForExit(output.Token));
                if (interactive) { input = new TerminalInputRelay(); }
                int width = options.Width, height = options.Height;
                bool warned = false;
                while (!completion.IsCompleted && !_stopping) {
                    // The worker enforces child lifetime; this loop must also drain after a successful exit.
                    output.Drain(Host.UI.Write);
                    if (interactive && !_recorder.HasExited) {
                        string bytes = input!.ReadAvailable();
                        if (bytes.Length > 0) { _recorder.SendInput(bytes); }
                        int newWidth = Console.WindowWidth, newHeight = Console.WindowHeight;
                        if (newWidth >= 2 && newHeight >= 2 && (width != newWidth || height != newHeight)) { _recorder.Resize(newWidth, newHeight); width = newWidth; height = newHeight; }
                    }
                    if (_recorder.LimitReached && !warned) { WriteWarning("The recording reached MaxBytes. The child is still running; further output is not recorded."); warned = true; }
                    Thread.Sleep(20);
                }
                if (_stopping) { output.Stop(); _recorder.Stop(); }
                try { completion.GetAwaiter().GetResult(); }
                catch (OperationCanceledException) when (_stopping) { }
                while (output.Drain(Host.UI.Write)) { }
                if (_recorder.LimitReached && !warned) { WriteWarning("The recording reached MaxBytes and was finalized at the cap."); }
                if (_recorder.ExitCode != 0 && !_stopping) { WriteError(new ErrorRecord(new InvalidOperationException("The recorded child exited with code " + _recorder.ExitCode + "."), "ConsoleRecordingChildFailed", ErrorCategory.OperationStopped, _recorder.Path)); }
                return _recorder.Path;
            } finally {
                RestoreAndFinish(input, () => { output.Stop(); _recorder?.Dispose(); }, () => {
                    try {
                        // Observe the worker after releasing a blocked producer, including when host writes fail.
                        if (completion != null) { try { completion.GetAwaiter().GetResult(); } catch (Exception) { /* The pipeline error or cancellation is already being propagated. */ } }
                    } finally {
                        _recorder = null;
                        _relay = null;
                        output.Dispose();
                    }
                }, () => {
                    if (title != null) { try { Host.UI.RawUI.WindowTitle = title; } catch (Exception error) when (error is IOException || error is NotSupportedException || error is NotImplementedException) { } }
                });
            }
        }

        // The physical terminal must be usable even when recorder finalization or host cleanup fails.
        private static void RestoreAndFinish(IDisposable? input, Action finishRecorder, Action finishOutput, Action restoreTitle) {
            try { input?.Dispose(); }
            finally {
                try { finishRecorder(); }
                finally {
                    try { finishOutput(); }
                    finally { restoreTitle(); }
                }
            }
        }
        /// <inheritdoc/>
        protected override void StopProcessing() { _stopping = true; _relay?.Stop(); _recorder?.Stop(); }
        /// <summary>Releases this command's recorder, including after cancellation.</summary>
        public void Dispose() { _relay?.Stop(); _recorder?.Dispose(); }
    }
}
