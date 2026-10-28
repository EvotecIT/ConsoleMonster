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
            string? readyText = null, string? inputKeys = null, Action<string>? recording = null, TimeSpan? inputDelay = null,
            bool hostedOutput = false) {
            string folder = Path.Combine(Path.GetTempPath(), "cmsite-session-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var options = new SessionRecordingOptions {
                Shell = executable, Width = width, Height = height, WorkingDirectory = workingDirectory,
                Timeout = timeout, SessionTimeout = timeout, Path = Path.Combine(folder, "capture.cast"), RespondToCursorQueries = true
            };
            foreach (string argument in arguments) { options.Arguments.Add(argument); }
            foreach (var entry in environment) { options.Environment[entry.Key] = entry.Value; }
            CapturePins.Apply(options.Environment);
            string hostedPath = Path.Combine(folder, "hosted.cast");
            bool captureWritten = false;
            // Cooperative checkpoint workers record their encoder's actual VT before native terminal repaint coalescing.
            if (hostedOutput) { options.Environment["SITEFRAMES_OUTPUT_RECORDING"] = hostedPath; }
            try {
                using (SessionRecorder recorder = SessionRecorder.Start(options)) {
                    if (readyText != null && inputKeys != null) {
                        recorder.WaitForOutput(readyText, timeout);
                        recorder.SendInputAfter(inputDelay ?? TimeSpan.Zero, inputKeys);
                    }
                    if (inputText != null) {
                        if (readyMarker != null) { recorder.WaitForOutput(readyMarker, timeout); }
                        recorder.SendInputAfter(TimeSpan.Zero, inputText);
                        if (completionText != null) {
                            recorder.SendInputAfter(TimeSpan.FromMilliseconds(250), completionText);
                        }
                    }
                    if (interrupt.HasValue) { recorder.WaitForExitWithInterrupt(interrupt.Value); }
                    else { recorder.WaitForExit(); }
                    if (interrupt.HasValue && !recorder.Interrupted) { throw new InvalidOperationException("Capture exited before Ctrl+C: " + executable); }
                    List<OutputChunk> native = Read(options.Path);
                    if (recorder.ExitCode != 0) { throw new InvalidOperationException("Capture process exited with code " + recorder.ExitCode + ": " + executable + "\n" + Encoding.UTF8.GetString(PowerShellRunner.Bytes(native))); }
                    string recordedPath = hostedOutput ? hostedPath : options.Path;
                    if (!File.Exists(recordedPath)) { throw new InvalidDataException("The capture worker did not finalize its hosted recording."); }
                    List<OutputChunk> chunks = Normalize(Read(recordedPath), width, height);
                    CaptureRun.Observe(PowerShellRunner.Bytes(chunks), width, height);
                    if (capturePath != null) { File.WriteAllBytes(capturePath, PowerShellRunner.Bytes(chunks)); captureWritten = true; }
                    if (recorder.LimitReached) { throw new IOException("Capture reached the recorder byte limit: " + executable); }
                    recording?.Invoke(File.ReadAllText(recordedPath));
                    return chunks;
                }
            } catch (TimeoutException error) {
                string output = File.Exists(options.Path) ? Encoding.UTF8.GetString(PowerShellRunner.Bytes(Read(options.Path))) : string.Empty;
                if (output.Length > 8000) { output = output[^8000..]; }
                throw new TimeoutException(error.Message + " Child: " + executable + "\n" + output, error);
            } finally {
                try {
                    string retainedPath = hostedOutput && File.Exists(hostedPath) ? hostedPath : options.Path;
                    // Failed captures retain their original bytes for diagnostics; they never receive a success receipt.
                    if (!captureWritten && capturePath != null && File.Exists(retainedPath)) { File.WriteAllBytes(capturePath, PowerShellRunner.Bytes(Read(retainedPath))); }
                } finally { Site.RemoveTemporary(folder); }
            }
        }

        private static List<OutputChunk> Normalize(List<OutputChunk> source, int width, int height) {
            var result = new List<OutputChunk>();
            var normalizer = new VtOutputNormalizer(width, height);
            using var bytes = new MemoryStream();
            void Flush() {
                if (bytes.Length == 0) { return; }
                result.Add(new OutputChunk(0, Encoding.UTF8.GetBytes(normalizer.Normalize(Encoding.UTF8.GetString(bytes.ToArray())))));
                bytes.SetLength(0); bytes.Position = 0;
            }
            foreach (OutputChunk chunk in source) {
                bytes.Write(chunk.Data);
                if (chunk.Hold > TimeSpan.Zero) { Flush(); result.Add(new OutputChunk(0, Array.Empty<byte>(), chunk.Hold)); }
            }
            Flush(); return result;
        }

        private static List<OutputChunk> Read(string path) {
            var chunks = new List<OutputChunk>();
            foreach (string line in File.ReadLines(path)) {
                if (!line.StartsWith("[", StringComparison.Ordinal)) { continue; }
                using JsonDocument json = JsonDocument.Parse(line);
                JsonElement entry = json.RootElement;
                if (entry[1].GetString() == "o") {
                    chunks.Add(new OutputChunk((long)Math.Round(entry[0].GetDouble() * 1000), Encoding.UTF8.GetBytes(entry[2].GetString()!)));
                } else if (entry[1].GetString() == "cm") {
                    var marker = SessionMarkerData.Read(TimeSpan.FromSeconds(entry[0].GetDouble()), entry[2].GetString()!);
                    if (marker.Hold > TimeSpan.Zero) { chunks.Add(new OutputChunk(0, Array.Empty<byte>(), marker.Hold)); }
                }
            }
            return chunks;
        }
    }
}
