using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using ConsoleMonster.Serialization;

namespace ConsoleMonster.Sessions {
    /// <summary>Writes sanitized v2 events in arrival order, while keeping controls active after the byte cap.</summary>
    internal sealed class SessionCastWriter : IDisposable {
        internal const string ControlPrefix = "\u001b]777;ConsoleMonster;";
        private readonly SessionRecordingOptions _options;
        private readonly RecordingRedactor _redactor;
        private readonly FileStream _file;
        private readonly FileStream _metadata;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly StringBuilder _pending = new StringBuilder();
        private readonly StringBuilder _relay = new StringBuilder();
        private readonly List<Arrival> _arrivals = new List<Arrival>();
        private readonly List<BufferedEvent> _deferred = new List<BufferedEvent>();
        private readonly StringBuilder _control = new StringBuilder();
        private readonly List<Arrival> _controlArrivals = new List<Arrival>();
        private readonly List<object?> _markers = new List<object?>();
        private double _lastReal;
        private double _time;
        private long _sequence;
        private bool _disposed;
        private bool _suppressLongRecord;
        private bool _suppressLongControl;
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
            string header = "{\"version\":2,\"width\":" + options.Width + ",\"height\":" + options.Height + ",\"timestamp\":" + Started.ToUnixTimeSeconds() + ",\"title\":" + Json(_redactor.Apply(options.Name)) + ",\"idle_time_limit\":" + Seconds(options.IdleLimit.TotalSeconds) + ",\"env\":{\"TERM\":\"xterm-256color\"},\"consolemonster\":{\"recording\":true,\"mode\":" + Json(mode) + ",\"host\":" + Json(_redactor.Apply(Environment.MachineName)) + "}}\n";
            if (Encoding.UTF8.GetByteCount(header) > options.MaxBytes) { throw new ArgumentException("The cast header exceeds MaxBytes. Use a shorter name or a larger cap."); }
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            if (File.Exists(Path + ".json")) { throw new IOException("The recording sidecar already exists: " + Path + ".json"); }
            _file = new FileStream(Path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            try { _metadata = new FileStream(Path + ".json", FileMode.CreateNew, FileAccess.Write, FileShare.Read); }
            catch { _file.Dispose(); throw; }
            Write(header);
        }

        public string Append(string text) {
            if (_disposed || StopRequested) { return string.Empty; }
            _relay.Clear();
            Arrival arrival = Now();
            foreach (char c in text) {
                if (StopRequested) { break; }
                if (c == '\u001b') {
                    if (_control.ToString().StartsWith(ControlPrefix, StringComparison.Ordinal)) { ClearControl(); }
                    else { FlushNonPrivateControl(); }
                    _control.Append(c); _controlArrivals.Add(arrival);
                    _suppressLongControl = false;
                } else if (_control.Length > 0 || _suppressLongControl) {
                    if (!_suppressLongControl) { _control.Append(c); _controlArrivals.Add(arrival); }
                    string value = _control.ToString();
                    if (value.StartsWith(ControlPrefix, StringComparison.Ordinal)) {
                        if (c == '\a') {
                            string command = value.Substring(ControlPrefix.Length, value.Length - ControlPrefix.Length - 1);
                            ClearControl();
                            FlushPending();
                            if (command == "stop") { StopRequested = true; }
                            else if (command.StartsWith("marker;", StringComparison.Ordinal)) {
                                string label;
                                try { label = Encoding.UTF8.GetString(Convert.FromBase64String(command.Substring(7))); }
                                catch (FormatException) { label = "Chapter"; }
                                Marker(label, arrival);
                            }
                        } else if (_control.Length > 8192) { ClearControl(); _suppressLongControl = true; }
                    } else if (_suppressLongControl) {
                        if (c == '\a') { _suppressLongControl = false; }
                    } else if (!ControlPrefix.StartsWith(value, StringComparison.Ordinal)) { FlushNonPrivateControl(); }
                } else { AppendCharacter(c, arrival); }
            }
            return _relay.ToString();
        }

        private void AppendCharacter(char c, Arrival arrival) {
            _relay.Append(c);
            if (LimitReached) { return; }
            if (_suppressLongRecord) {
                if (c == '\r' || c == '\n') { _suppressLongRecord = false; }
                return;
            }
            _pending.Append(c); _arrivals.Add(arrival);
            if (c == '\r' || c == '\n') { FlushPending(); }
            else if (_pending.Length >= 65536) {
                Arrival first = _arrivals[0];
                _pending.Clear(); _arrivals.Clear();
                _deferred.Add(new BufferedEvent(first, "o", "[REDACTED: output record exceeds 64 KiB]\r\n"));
                FlushDeferred();
                _suppressLongRecord = true;
            }
        }
        private void FlushNonPrivateControl() {
            for (int i = 0; i < _control.Length; i++) { AppendCharacter(_control[i], _controlArrivals[i]); }
            ClearControl();
        }
        private void ClearControl() { _control.Clear(); _controlArrivals.Clear(); }
        private void FlushPending() {
            if (_pending.Length > 0 && !LimitReached) {
                RedactedOutput safe = _redactor.ApplyWithOffsets(_pending.ToString());
                int start = 0;
                while (start < safe.Text.Length) {
                    Arrival arrival = _arrivals[safe.Offsets[start]];
                    int end = start + 1;
                    while (end < safe.Text.Length && _arrivals[safe.Offsets[end]].Sequence == arrival.Sequence) { end++; }
                    _deferred.Add(new BufferedEvent(arrival, "o", safe.Text.Substring(start, end - start)));
                    start = end;
                }
            }
            _pending.Clear(); _arrivals.Clear();
            FlushDeferred();
        }
        private void FlushDeferred() {
            _deferred.Sort((a, b) => a.Arrival.Sequence.CompareTo(b.Arrival.Sequence));
            foreach (BufferedEvent item in _deferred) { Event(item); }
            _deferred.Clear();
        }
        private Arrival Now() => new Arrival(_clock.Elapsed.TotalSeconds, ++_sequence);
        private void Queue(BufferedEvent item) {
            if (_pending.Length > 0 || _control.Length > 0) { _deferred.Add(item); }
            else { Event(item); }
        }
        public void Resize(int width, int height) {
            if (!_disposed && !StopRequested && !LimitReached) { Queue(new BufferedEvent(Now(), "r", width + "x" + height)); }
        }
        public void Marker(string label) => Marker(label, Now());
        private void Marker(string label, Arrival arrival) {
            if (_disposed || LimitReached || StopRequested) { return; }
            Queue(new BufferedEvent(arrival, "m", _redactor.Apply(label.Replace('\r', ' ').Replace('\n', ' '))));
        }
        private void Event(BufferedEvent item) {
            if (item.Value.Length == 0 || LimitReached || StopRequested) { return; }
            double gap = Math.Max(0, item.Arrival.Time - _lastReal);
            _time += _options.IdleLimit > TimeSpan.Zero ? Math.Min(gap, _options.IdleLimit.TotalSeconds) : gap;
            _lastReal = Math.Max(_lastReal, item.Arrival.Time);
            Write("[" + Seconds(_time) + "," + Json(item.Kind) + "," + Json(item.Value) + "]\n");
            if (item.Kind == "m" && !LimitReached) { _markers.Add(new object?[] { (long)(_time * 1000), item.Value }); }
        }
        private void Write(string text) {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            if (_file.Length + bytes.Length > _options.MaxBytes) { LimitReached = true; return; }
            _file.Write(bytes, 0, bytes.Length); _file.Flush();
        }
        internal static string Json(string text) => MiniJson.Write(text);
        internal static string Seconds(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        public void Dispose() {
            if (_disposed) { return; }
            if (_control.Length > 0) {
                if (ControlPrefix.StartsWith(_control.ToString(), StringComparison.Ordinal) || _control.ToString().StartsWith(ControlPrefix, StringComparison.Ordinal)) { ClearControl(); }
                else { FlushNonPrivateControl(); }
            }
            FlushPending(); _disposed = true; _file.Dispose();
            string metadata = MiniJson.Write(new Dictionary<string, object?> {
                ["format"] = "consolemonster-session", ["version"] = 1, ["recording"] = true,
                ["started"] = Started.ToString("O", CultureInfo.InvariantCulture), ["host"] = _redactor.Apply(Environment.MachineName),
                ["mode"] = Mode, ["sizeLimitReached"] = LimitReached, ["markers"] = _markers
            });
            using (FileStream stream = _metadata) { byte[] bytes = Encoding.UTF8.GetBytes(metadata); stream.Write(bytes, 0, bytes.Length); }
        }
        private readonly struct Arrival {
            public Arrival(double time, long sequence) { Time = time; Sequence = sequence; }
            public double Time { get; } public long Sequence { get; }
        }
        private sealed class BufferedEvent {
            public BufferedEvent(Arrival arrival, string kind, string value) { Arrival = arrival; Kind = kind; Value = value; }
            public Arrival Arrival { get; } public string Kind { get; } public string Value { get; }
        }
    }
}
