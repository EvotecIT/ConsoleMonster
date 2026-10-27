using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ConsoleMonster.Sessions;
using ConsoleMonster.Export;

namespace ConsoleMonster.Demos {
    /// <summary>Types scripted demonstrations into a real PowerShell PTY and records its actual output.</summary>
    public static class ConsolePresentation {
        /// <summary>Runs every step. The caller controls the child environment and reproducible input data through options.</summary>
        public static string Record(DemoScript script, DemoOptions? options = null, CancellationToken cancellationToken = default) {
            using (DemoSession session = new DemoSession(script, options ?? new DemoOptions(), cancellationToken)) {
                foreach (DemoStep step in script.Steps) { session.Execute(step, false); }
                return session.Path;
            }
        }
        /// <summary>Runs a live demo. Enter runs, Space types, b goes back, s skips and q quits. Back does not undo shell side effects.</summary>
        /// <param name="script">The PowerShell demo.</param>
        /// <param name="options">Child and recording settings.</param>
        /// <param name="readKey">Optional key source; defaults to the current terminal.</param>
        /// <param name="stepChanged">Step counter callback, outside the captured child output.</param>
        /// <param name="cancellationToken">Stops the owned child when cancelled.</param>
        public static string Present(DemoScript script, DemoOptions? options = null, Func<ConsoleKeyInfo>? readKey = null,
            Action<int, int>? stepChanged = null, CancellationToken cancellationToken = default) {
            using (DemoSession session = new DemoSession(script, options ?? new DemoOptions(), cancellationToken)) {
                List<int> commands = new List<int>();
                for (int i = 0; i < script.Steps.Count; i++) { if (script.Steps[i].Kind == DemoStepKind.Command && !script.Steps[i].Hidden) { commands.Add(i); } }
                int index = 0, position = 0;
                while (index < commands.Count) {
                    cancellationToken.ThrowIfCancellationRequested();
                    while (position < commands[index]) { session.Execute(script.Steps[position++], false); }
                    stepChanged?.Invoke(index + 1, commands.Count);
                    ConsoleKeyInfo key;
                    if (readKey != null) { key = readKey(); }
                    else {
                        while (!Console.KeyAvailable) { cancellationToken.ThrowIfCancellationRequested(); Thread.Sleep(20); }
                        key = Console.ReadKey(true);
                    }
                    DemoPresenterAction action = DemoPresenter.Action(key);
                    if (action == DemoPresenterAction.Quit) { break; }
                    if (action == DemoPresenterAction.Type) { session.Execute(script.Steps[commands[index]], true); continue; }
                    if (action == DemoPresenterAction.Back) {
                        session.ClearInput(); index = Math.Max(0, index - 1); position = commands[index]; continue;
                    }
                    if (action == DemoPresenterAction.Run) { session.Execute(script.Steps[commands[index]], false, true); }
                    else if (action != DemoPresenterAction.Skip) { continue; }
                    else { session.ClearInput(); }
                    position = commands[index] + 1; index++;
                }
                // Instructions after the final command (for example a closing pause) still apply.
                if (index == commands.Count) { while (position < script.Steps.Count) { session.Execute(script.Steps[position++], false); } }
                return session.Path;
            }
        }
    }

    // Owns one PTY and observes semantic readiness on the reader thread. No PowerShell callbacks run there.
    internal sealed class DemoSession : IDisposable {
        private readonly object _gate = new object();
        private readonly SemanticPromptDetector _prompts = new SemanticPromptDetector();
        private readonly StringBuilder _received = new StringBuilder();
        private readonly DemoOptions _options;
        private readonly CancellationToken _token;
        private readonly DemoTypingClock _clock;
        private readonly SessionRecorder _recorder;
        private readonly string _supportDirectory;
        private volatile bool _hidden = true;
        private bool _typed;
        private string? _pendingCommand;
        internal string Path => _recorder.Path;
        internal DemoSession(DemoScript script, DemoOptions options, CancellationToken token) {
            if (script == null) { throw new ArgumentNullException(nameof(script)); }
            _options = options; _token = token; _clock = new DemoTypingClock(options.Seed);
            if (options.StepTimeout <= TimeSpan.Zero || double.IsNaN(options.Jitter) || options.Jitter < 0 || options.Jitter > 1) { throw new ArgumentOutOfRangeException(nameof(options)); }
            SessionRecordingOptions source = options.Recording ?? throw new ArgumentNullException(nameof(options.Recording));
            if (source.InProcess || source.Arguments.Count != 0) { throw new ArgumentException("Demos require their own PowerShell PTY bootstrap; use Shell and Environment instead of Arguments or InProcess."); }
            SessionRecordingOptions child = new SessionRecordingOptions {
                Path = source.Path, Folder = source.Folder, Title = source.Title, Width = source.Width, Height = source.Height,
                Shell = source.Shell ?? "pwsh", WorkingDirectory = source.WorkingDirectory, IdleLimit = source.IdleLimit,
                MaxBytes = source.MaxBytes, RedactionText = source.RedactionText, Timeout = source.Timeout, CaptureInitiallyEnabled = false
            };
            foreach (string pattern in source.Redact) { child.Redact.Add(pattern); }
            foreach (KeyValuePair<string, string?> item in source.Environment) { child.Environment[item.Key] = item.Value; }
            if (script.SourcePath != null) { child.Environment["CM_DEMO_SCRIPT_PATH"] = script.SourcePath; }
            child.Arguments.Add("-NoLogo"); child.Arguments.Add("-NoProfile"); child.Arguments.Add("-EncodedCommand");
            _supportDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ConsoleMonsterDemo-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_supportDirectory);
            try {
                using (Stream inputSource = typeof(ConsolePresentation).Assembly.GetManifestResourceStream("ConsoleMonster.Demos.TerminalInputRelay.cs")!)
                using (StreamReader sourceReader = new StreamReader(inputSource)) {
                    // Compile one maintained input implementation for the child's own PowerShell runtime.
                    // A short file path also stays below Windows' command-line and environment-size limits.
                    string code = sourceReader.ReadToEnd().Replace("namespace ConsoleMonster.Sessions", "namespace ConsoleMonster.DemoChild")
                        .Replace("internal sealed class TerminalInputRelay", "public sealed class TerminalInputRelay");
                    string sourcePath = System.IO.Path.Combine(_supportDirectory, "TerminalInputRelay.cs");
                    File.WriteAllText(sourcePath, code, new UTF8Encoding(false));
                    child.Environment["CM_DEMO_INPUT_SOURCE"] = sourcePath;
                }
                using (Stream stream = typeof(ConsolePresentation).Assembly.GetManifestResourceStream("ConsoleMonster.Demos.DemoShell.ps1")!)
                using (StreamReader reader = new StreamReader(stream)) {
                    child.Arguments.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(reader.ReadToEnd())));
                }
                _recorder = SessionRecorder.Start(child, OnOutput);
                try { Wait(() => _prompts.PromptCount > 0, "the initial OSC 133 prompt (PowerShell 7 is required)"); }
                catch { _recorder.Dispose(); throw; }
            } catch { Directory.Delete(_supportDirectory, true); throw; }
        }
        private void OnOutput(string text) {
            bool display = !_hidden;
            lock (_gate) {
                _prompts.Feed(text, c => _received.Append(c), () => _received.Clear());
                if (_received.Length > 1024 * 1024) { _received.Remove(0, _received.Length - 512 * 1024); }
                Monitor.PulseAll(_gate);
            }
            if (display) { _options.Output?.Invoke(text); }
        }
        internal void Execute(DemoStep step, bool typeOnly, bool forceRun = false) {
            _token.ThrowIfCancellationRequested();
            if (!step.Hidden) { EnsureVisible(); }
            switch (step.Kind) {
                case DemoStepKind.Chapter: _recorder.AddMarker(step.Text); return;
                case DemoStepKind.Hold: _recorder.AddMarker(RecordingTimeline.FromMilliseconds(double.Parse(step.Text, CultureInfo.InvariantCulture) * 1000)); return;
                case DemoStepKind.Pace: _recorder.SetPace((RecordingPace)Enum.Parse(typeof(RecordingPace), step.Text, true)); return;
                case DemoStepKind.Pause:
                    Delay(TimeSpan.FromSeconds(double.Parse(step.Text, CultureInfo.InvariantCulture)));
                    long checkpoint; lock (_gate) { checkpoint = _prompts.PromptCount; }
                    _recorder.SendInput("\u001d");
                    Wait(() => _prompts.PromptCount > checkpoint, "the pause checkpoint"); return;
                case DemoStepKind.Wait:
                    Regex pattern = new Regex(step.Text, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                    Wait(() => pattern.IsMatch(_received.ToString()), "output matching " + step.Text); return;
                case DemoStepKind.Narration:
                    RunCommand("Write-Host " + "'" + step.Text.Replace("'", "''") + "'", step, false, false); return;
                default: RunCommand(step.Text, step, typeOnly || (step.TypeOnly && !forceRun), step.Hidden); return;
            }
        }
        private void RunCommand(string command, DemoStep step, bool typeOnly, bool hidden) {
            if (_typed && _pendingCommand != command) { ClearInput(); }
            if (hidden) { ClearInput(); _hidden = true; _recorder.SetCaptureEnabled(false); }
            try {
                long before;
                lock (_gate) { before = _prompts.PromptCount; if (!_typed) { _received.Clear(); _prompts.BeginInput(); } }
                if (!_typed) {
                    if (hidden) { _recorder.SendInput(command.Replace("\n", "\r")); }
                    else {
                        var elements = StringInfo.GetTextElementEnumerator(command);
                        while (elements.MoveNext()) {
                            Delay(_clock.Next(step.WordsPerMinute, _options.Jitter));
                            _recorder.SendInput(elements.GetTextElement() == "\n" ? "\r" : elements.GetTextElement());
                        }
                    }
                    _typed = true; _pendingCommand = command;
                }
                if (typeOnly) { return; }
                _recorder.SendInput("\u001e"); _typed = false; _pendingCommand = null;
                Wait(() => _prompts.PromptCount > before, "OSC 133 prompt after source line " + step.Line);
                if (hidden && _prompts.LastExitCode.HasValue && _prompts.LastExitCode.Value != 0) {
                    throw new InvalidOperationException("Hidden demo setup failed at source line " + step.Line + ". Run that setup separately to inspect its error.");
                }
            } finally { if (hidden) { _typed = false; _pendingCommand = null; } }
        }
        private void EnsureVisible() {
            if (!_hidden) { return; }
            long before; lock (_gate) { before = _prompts.PromptCount; }
            _hidden = false; _recorder.SetCaptureEnabled(true);
            _recorder.SendInput("\u0015");
            Wait(() => _prompts.PromptCount > before, "the visible demo prompt");
        }
        internal void ClearInput() {
            if (!_typed) { return; }
            long before;
            lock (_gate) { before = _prompts.PromptCount; }
            _recorder.SendInput("\u0015"); _typed = false; _pendingCommand = null;
            Wait(() => _prompts.PromptCount > before, "the prompt after discarding typed input");
        }
        private void Delay(TimeSpan duration) { if (_token.WaitHandle.WaitOne(duration)) { _token.ThrowIfCancellationRequested(); } }
        private void Wait(Func<bool> ready, string description) {
            Stopwatch time = Stopwatch.StartNew();
            lock (_gate) {
                while (!ready()) {
                    _token.ThrowIfCancellationRequested();
                    if (_recorder.HasExited) { throw new IOException("The demo shell exited while waiting for " + description + "."); }
                    if (time.Elapsed >= _options.StepTimeout) { throw new TimeoutException("Timed out waiting for " + description + "."); }
                    Monitor.Wait(_gate, 20);
                }
            }
        }
        public void Dispose() {
            try { _recorder.Dispose(); }
            finally { if (Directory.Exists(_supportDirectory)) { Directory.Delete(_supportDirectory, true); } }
        }
    }
}
