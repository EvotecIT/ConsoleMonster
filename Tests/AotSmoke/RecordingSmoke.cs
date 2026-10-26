using System;
using System.IO;
using System.Text;
using System.Text.Json;
using ConsoleMonster.Sessions;

internal static class RecordingSmoke {
    internal static void Run() {
        string folder = Path.Combine(Path.GetTempPath(), "ConsoleMonster-aot-recording-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(folder, "session.cast");
        var options = new SessionRecordingOptions { Path = path, InProcess = true, Width = 120, Height = 40 };
        options.Redact.Add("synthetic-secret");
        string line = new string('a', 16383) + "\U0001f680" + new string('b', 200 * 1024 - 16385);
        string prefix = new string('x', 65530);
        try {
            using (SessionRecorder recorder = SessionRecorder.Start(options)) {
                recorder.CapturePowerShellOutput(line.Substring(0, 16384));
                recorder.CapturePowerShellOutput(line.Substring(16384) + "\n");
                recorder.CapturePowerShellOutput(prefix + "synthe");
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream, Encoding.UTF8)) {
                    if (reader.ReadToEnd().Contains("synthe", StringComparison.Ordinal)) {
                        throw new InvalidOperationException("The live recording contains a partial secret.");
                    }
                }
                if (new FileInfo(path).Length <= 1000) { throw new InvalidOperationException("Long output did not stream."); }
                recorder.CapturePowerShellOutput("tic-secret\npassword='" + new string('q', 70000));
                recorder.CapturePowerShellOutput("'\n");
            }
            var output = new StringBuilder();
            string[] events = File.ReadAllLines(path);
            for (int i = 1; i < events.Length; i++) {
                using JsonDocument json = JsonDocument.Parse(events[i]);
                if (json.RootElement[1].GetString() == "o") { output.Append(json.RootElement[2].GetString()); }
            }
            if (output.ToString() != line + "\n" + prefix + "[REDACTED]\n[REDACTED]\n") {
                throw new InvalidOperationException("Native recording changed long output or failed secret redaction.");
            }
            if (SessionRecording.Read(path).Width != 120) { throw new InvalidOperationException("Recording header changed."); }
        } finally {
            if (Directory.Exists(folder)) { Directory.Delete(folder, true); }
        }
    }
}
