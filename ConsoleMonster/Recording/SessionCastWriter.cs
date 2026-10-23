using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using ConsoleMonster.Serialization;

namespace ConsoleMonster.Sessions {
    /// <summary>Serializes sanitized output, resize events and private chapter controls into asciicast v2.</summary>
    internal sealed class SessionCastWriter : IDisposable {
        internal const string ControlPrefix = "\u001b]777;ConsoleMonster;";
        private readonly SessionRecordingOptions _options;
        private readonly RecordingRedactor _redactor;
        private readonly FileStream _file;
        private readonly FileStream _metadata;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly StringBuilder _pending = new StringBuilder();
        private readonly List<object?> _markers = new List<object?>();
        private double _lastReal;
        private double _time;
        private bool _disposed;
        private bool _suppressLongRecord;
        public string Path { get; }
        public bool LimitReached { get; private set; }
        public bool StopRequested { get; private set; }
        public string Mode { get; }
        public DateTimeOffset Started { get; } = DateTimeOffset.UtcNow;

        public SessionCastWriter(SessionRecordingOptions options, string mode) {
            options.Validate();
            _options = options;
            _redactor = new RecordingRedactor(options.Redact, options.RedactionText);
            Mode = mode;
            Path = System.IO.Path.GetFullPath(options.Path ?? System.IO.Path.Combine(options.Folder, "ConsoleRecording-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".cast"));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            string header = "{\"version\":2,\"width\":" + options.Width + ",\"height\":" + options.Height + ",\"timestamp\":" + Started.ToUnixTimeSeconds() + ",\"title\":" + Json(_redactor.Apply(options.Name)) + ",\"idle_time_limit\":" + Seconds(options.IdleLimit.TotalSeconds) + ",\"env\":{\"TERM\":\"xterm-256color\"},\"consolemonster\":{\"recording\":true,\"mode\":" + Json(mode) + ",\"host\":" + Json(_redactor.Apply(Environment.MachineName)) + "}}\n";
            if (Encoding.UTF8.GetByteCount(header) > options.MaxBytes) { throw new ArgumentException("The cast header exceeds MaxBytes. Use a shorter name or a larger cap."); }
            if (File.Exists(Path + ".json")) { throw new IOException("The recording sidecar already exists: " + Path + ".json"); }
            _file = new FileStream(Path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            try { _metadata = new FileStream(Path + ".json", FileMode.CreateNew, FileAccess.Write, FileShare.Read); }
            catch { _file.Dispose(); throw; }
            Write(header);
        }

        // Output is buffered through a line boundary: patterns may span any number of PTY reads. Extremely long
        // unbroken records are suppressed as a whole, rather than leaking a token across a bounded buffer boundary.
        public void Append(string text) {
            if (_disposed || LimitReached || StopRequested) { return; }
            foreach (char c in text) {
                if (_suppressLongRecord) {
                    if (c == '\n') { _suppressLongRecord = false; }
                    continue;
                }
                _pending.Append(c);
                if (c == '\n' || c == '\r' || (c == '\a' && _pending.ToString().IndexOf(ControlPrefix, StringComparison.Ordinal) >= 0)) { FlushPending(); }
                else if (_pending.Length >= 65536) {
                    _pending.Clear();
                    _suppressLongRecord = true;
                    Event("o", "[REDACTED: output record exceeds 64 KiB]\r\n");
                }
            }
        }

        private void FlushPending() {
            string text = _pending.ToString();
            _pending.Clear();
            int from = 0;
            while (true) {
                int start = text.IndexOf(ControlPrefix, from, StringComparison.Ordinal);
                if (start < 0) { Event("o", _redactor.Apply(text.Substring(from))); break; }
                Event("o", _redactor.Apply(text.Substring(from, start - from)));
                int end = text.IndexOf('\a', start + ControlPrefix.Length);
                if (end < 0) { Event("o", "[REDACTED: incomplete recording control]"); break; }
                string control = text.Substring(start + ControlPrefix.Length, end - start - ControlPrefix.Length);
                if (control == "stop") { StopRequested = true; }
                else if (control.StartsWith("marker;", StringComparison.Ordinal)) {
                    string label;
                    try { label = Encoding.UTF8.GetString(Convert.FromBase64String(control.Substring(7))); }
                    catch (FormatException) { label = "Chapter"; }
                    Marker(label);
                }
                from = end + 1;
            }
        }

        public void Resize(int width, int height) { if (!_disposed) { Event("r", width + "x" + height); } }
        public void Marker(string label) {
            if (_disposed || LimitReached || StopRequested) { return; }
            string safe = _redactor.Apply(label.Replace('\r', ' ').Replace('\n', ' '));
            Event("m", safe);
            _markers.Add(new object?[] { (long)(_time * 1000), safe });
        }
        private void Event(string kind, string value) {
            if (value.Length == 0 || LimitReached || StopRequested) { return; }
            double real = _clock.Elapsed.TotalSeconds;
            double gap = Math.Max(0, real - _lastReal);
            _time += _options.IdleLimit > TimeSpan.Zero ? Math.Min(gap, _options.IdleLimit.TotalSeconds) : gap;
            _lastReal = real;
            Write("[" + Seconds(_time) + "," + Json(kind) + "," + Json(value) + "]\n");
        }
        private void Write(string text) {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            if (_file.Length + bytes.Length > _options.MaxBytes) { LimitReached = true; return; }
            _file.Write(bytes, 0, bytes.Length);
            _file.Flush();
        }
        internal static string Json(string text) => MiniJson.Write(text);
        internal static string Seconds(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        public void Dispose() {
            if (_disposed) { return; }
            FlushPending();
            _disposed = true;
            _file.Dispose();
            string metadata = MiniJson.Write(new Dictionary<string, object?> {
                ["format"] = "consolemonster-session", ["version"] = 1, ["recording"] = true,
                ["started"] = Started.ToString("O", CultureInfo.InvariantCulture), ["host"] = _redactor.Apply(Environment.MachineName),
                ["mode"] = Mode, ["sizeLimitReached"] = LimitReached, ["markers"] = _markers
            });
            // Never overwrite a pre-existing sidecar.
            using (FileStream stream = _metadata) {
                byte[] bytes = Encoding.UTF8.GetBytes(metadata);
                stream.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
