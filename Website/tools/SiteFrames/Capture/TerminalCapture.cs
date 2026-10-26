using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using ConsoleMonster.Sessions;
using SiteFrames.Vt;

namespace SiteFrames.Capture {
    /// <summary>Site recipes over the same recorder shipped to terminal users. No transport or reader lives here.</summary>
    internal static class TerminalCapture {
        internal static List<OutputChunk> Run(string executable, IEnumerable<string> arguments, int width, int height,
            IDictionary<string, string?> environment, string workingDirectory, TimeSpan timeout, TimeSpan? interrupt = null,
            string? inputText = null, string? readyMarker = null, string? capturePath = null, string? completionText = null,
            string? readyText = null, string? inputKeys = null, Action<string>? recording = null, TimeSpan? inputDelay = null) {
            string folder = Path.Combine(Path.GetTempPath(), "cmsite-session-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var options = new SessionRecordingOptions {
                Shell = executable, Width = width, Height = height, WorkingDirectory = workingDirectory,
                Timeout = timeout, SessionTimeout = timeout, Path = Path.Combine(folder, "capture.cast"), RespondToCursorQueries = true
            };
            foreach (string argument in arguments) { options.Arguments.Add(argument); }
            foreach (var entry in environment) { options.Environment[entry.Key] = entry.Value; }
            CapturePins.Apply(options.Environment);
            try {
                using (SessionRecorder recorder = SessionRecorder.Start(options)) {
                    if (readyText != null && inputKeys != null) {
                        recorder.WaitForOutput(readyText, timeout);
                        recorder.SendInputAfter(inputDelay ?? TimeSpan.Zero, inputKeys);
                    }
                    if (inputText != null) {
                        if (readyMarker != null) { recorder.WaitForOutput(readyMarker, timeout); }
                        recorder.SendInput(inputText);
                        if (completionText != null) {
                            recorder.SendInputAfter(TimeSpan.FromMilliseconds(250), completionText);
                        }
                    }
                    if (interrupt.HasValue) { recorder.WaitForExitWithInterrupt(interrupt.Value); }
                    else { recorder.WaitForExit(); }
                    if (interrupt.HasValue && !recorder.Interrupted) { throw new InvalidOperationException("Capture exited before Ctrl+C: " + executable); }
                    List<OutputChunk> chunks = Read(options.Path);
                    CaptureRun.Observe(PowerShellRunner.Bytes(chunks), width, height);
                    if (capturePath != null) { File.WriteAllBytes(capturePath, PowerShellRunner.Bytes(chunks)); }
                    if (recorder.LimitReached) { throw new IOException("Capture reached the recorder byte limit: " + executable); }
                    if (recorder.ExitCode != 0) { throw new InvalidOperationException("Capture process exited with code " + recorder.ExitCode + ": " + executable + "\n" + Encoding.UTF8.GetString(PowerShellRunner.Bytes(chunks))); }
                    recording?.Invoke(File.ReadAllText(options.Path));
                    return chunks;
                }
            } catch (TimeoutException error) {
                string output = File.Exists(options.Path) ? Encoding.UTF8.GetString(PowerShellRunner.Bytes(Read(options.Path))) : string.Empty;
                if (output.Length > 8000) { output = output[^8000..]; }
                throw new TimeoutException(error.Message + " Child: " + executable + "\n" + output, error);
            } finally {
                if (capturePath != null && File.Exists(options.Path)) { File.WriteAllBytes(capturePath, PowerShellRunner.Bytes(Read(options.Path))); }
                Site.RemoveTemporary(folder);
            }
        }

        private static List<OutputChunk> Read(string path) {
            var chunks = new List<OutputChunk>();
            foreach (string line in File.ReadLines(path)) {
                if (!line.StartsWith("[", StringComparison.Ordinal)) { continue; }
                using JsonDocument json = JsonDocument.Parse(line);
                JsonElement entry = json.RootElement;
                if (entry[1].GetString() == "o") {
                    chunks.Add(new OutputChunk((long)Math.Round(entry[0].GetDouble() * 1000), Encoding.UTF8.GetBytes(entry[2].GetString()!)));
                }
            }
            return chunks;
        }
    }
}
