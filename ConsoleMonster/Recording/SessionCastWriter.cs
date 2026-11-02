using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ConsoleMonster.Serialization;
using ConsoleMonster.Export;

namespace ConsoleMonster.Sessions {
    /// <summary>Writes sanitized v2 events in arrival order, while keeping controls active after the byte cap.</summary>
    internal sealed class SessionCastWriter : IDisposable {
        // ConPTY flushes its pending repaint before forwarding OSC 1337 (DoITerm2Action). Unknown OSCs such as
        // 777 can overtake that repaint, placing a chapter or stop before the output it was meant to follow.
        // https://github.com/microsoft/terminal/blob/12c6b0a240c4cab02d471a445227d116df1df681/src/terminal/adapter/adaptDispatch.cpp#L3681-L3685
        internal const string ControlPrefix = "\u001b]1337;ConsoleMonster;";
        private const string LegacyControlPrefix = "\u001b]777;ConsoleMonster;";
        internal const int MaximumPendingCharacters = 1024 * 1024;
        private const int FlushThreshold = 65536;
        private const int OutputChunkCharacters = 16384;
        private readonly SessionRecordingOptions _options;
        private readonly RecordingRedactor _redactor;
        private readonly VtScreen _screen;
        private readonly FileStream _file;
        private readonly FileStream _metadata;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly StringBuilder _pending = new StringBuilder();
        private readonly StringBuilder _relay = new StringBuilder();
        private readonly List<Arrival> _arrivals = new List<Arrival>();
        // Source-aligned cursor observations survive the same streaming prefix/overlap cuts as output and arrivals.
        private readonly List<bool> _rowChanges = new List<bool>();
        private readonly List<BufferedEvent> _deferred = new List<BufferedEvent>();
        private readonly StringBuilder _control = new StringBuilder();
        private readonly List<Arrival> _controlArrivals = new List<Arrival>();
        private readonly List<object?> _markers = new List<object?>();
        private double _lastReal;
        private double _time;
        private long _sequence;
        private bool _disposed;
        private bool _suppressRecord;
        private bool _suppressLongControl;
        private RecordingRedactor.TerminalControlState _terminalState;
        private RecordingRedactor.TerminalControlState _standardTerminalState;
        private RecordingRedactor.TerminalControlState _replayTerminalState;
        private int _contextLength;
        private int _nextFlush = FlushThreshold;
        private int _redactionFallbacks;
        public string Path { get; }
        public bool LimitReached { get; private set; }
        public bool StopRequested { get; private set; }
        public string Mode { get; }
        public DateTimeOffset Started { get; } = DateTimeOffset.UtcNow;

        public SessionCastWriter(SessionRecordingOptions options, string mode) {
            options.Validate();
            _options = options;
            _redactor = new RecordingRedactor(options.Redact, options.RedactionText);
            _screen = new VtScreen(options.Width, options.Height, keepScrollback: false);
            Mode = mode;
            Path = System.IO.Path.GetFullPath(options.Path ?? System.IO.Path.Combine(options.Folder, "ConsoleRecording-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".cast"));
            string header = "{\"version\":2,\"width\":" + options.Width + ",\"height\":" + options.Height + ",\"timestamp\":" + Started.ToUnixTimeSeconds() + ",\"title\":" + Json(_redactor.Apply(options.Title)) + ",\"idle_time_limit\":" + Seconds(options.IdleLimit.TotalSeconds) + ",\"env\":{\"TERM\":\"xterm-256color\"},\"consolemonster\":{\"recording\":true,\"mode\":" + Json(mode) + ",\"host\":" + Json(_redactor.Apply(Environment.MachineName)) + "}}\n";
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
                    if (PrivatePrefix(_control.ToString()) != null) { ClearControl(); }
                    else { FlushNonPrivateControl(); }
                    _control.Append(c); _controlArrivals.Add(arrival);
                    _suppressLongControl = false;
                } else if (_control.Length > 0 || _suppressLongControl) {
                    if (!_suppressLongControl) { _control.Append(c); _controlArrivals.Add(arrival); }
                    string value = _control.ToString();
                    string? prefix = PrivatePrefix(value);
                    if (prefix != null) {
                        if (c == '\a') {
                            string command = value.Substring(prefix.Length, value.Length - prefix.Length - 1);
                            ClearControl();
                            if (command == "stop") { FlushPending(); StopRequested = true; }
                            else if (command.StartsWith("marker;", StringComparison.Ordinal)) {
                                string label;
                                try { label = Encoding.UTF8.GetString(Convert.FromBase64String(command.Substring(7))); }
                                catch (FormatException) { label = "Chapter"; }
                                Marker(label, new Arrival(arrival.Time, ++_sequence));
                                arrival = new Arrival(arrival.Time, ++_sequence);
                            } else if (command.StartsWith("timeline;", StringComparison.Ordinal)) {
                                try {
                                    RecordingMarker marker = SessionMarkerData.Read(TimeSpan.Zero, Encoding.UTF8.GetString(Convert.FromBase64String(command.Substring(9))));
                                    Marker(marker.Label, marker.Hold, marker.Pace, marker.IsChapter, new Arrival(arrival.Time, ++_sequence));
                                    arrival = new Arrival(arrival.Time, ++_sequence);
                                } catch (FormatException) { /* Malformed private controls are consumed, never exposed as output. */ }
                            }
                        } else if (_control.Length > 8192) { ClearControl(); _suppressLongControl = true; }
                    } else if (_suppressLongControl) {
                        if (c == '\a') { _suppressLongControl = false; }
                    } else if (!PendingPrivatePrefix(value)) { FlushNonPrivateControl(); }
                } else { AppendCharacter(c, arrival); }
            }
            return _relay.ToString();
        }

        private static string? PrivatePrefix(string value) => value.StartsWith(ControlPrefix, StringComparison.Ordinal) ? ControlPrefix
            : value.StartsWith(LegacyControlPrefix, StringComparison.Ordinal) ? LegacyControlPrefix : null;

        private static bool PendingPrivatePrefix(string value) => ControlPrefix.StartsWith(value, StringComparison.Ordinal)
            || LegacyControlPrefix.StartsWith(value, StringComparison.Ordinal);

        private void AppendCharacter(char c, Arrival arrival) {
            _relay.Append(c);
            if (LimitReached) { return; }
            int row = _screen.Cursor.Y;
            _screen.Feed(c);
            bool rowChanged = row != _screen.Cursor.Y;
            bool wasControl = _terminalState.IsPending || _standardTerminalState.IsPending || _replayTerminalState.IsPending;
            _terminalState.Append(c);
            _standardTerminalState.Append(c, RecordingRedactor.TerminalInterpretation.Standard);
            _replayTerminalState.Append(c, RecordingRedactor.TerminalInterpretation.Replay);
            bool lineBoundary = !wasControl && !_terminalState.IsPending && !_standardTerminalState.IsPending
                && !_replayTerminalState.IsPending && (c == '\r' || c == '\n');
            if (_suppressRecord) {
                if (lineBoundary) { _suppressRecord = false; }
                return;
            }
            _pending.Append(c); _arrivals.Add(arrival); _rowChanges.Add(rowChanged);
            if (lineBoundary) { FlushPending(); }
            else if (_pending.Length >= _nextFlush) {
                if (!_redactor.RequiresCompleteRecord) {
                    try { FlushPrefix(); }
                    catch (RegexMatchTimeoutException) { RedactionFallback("pattern timeout", suppress: true); }
                }
                if (_pending.Length >= MaximumPendingCharacters) {
                    RedactionFallback(_redactor.RequiresCompleteRecord
                        ? "complete-record regex buffer exceeds 1,048,576 characters; output omitted through next line break"
                        : "unfinished secret or terminal control exceeds 1,048,576 characters; output omitted through next line break", suppress: true);
                }
                _nextFlush = _redactor.RequiresCompleteRecord ? MaximumPendingCharacters : Math.Max(FlushThreshold, _pending.Length + FlushThreshold / 2);
            }
        }
        private void FlushNonPrivateControl() {
            for (int i = 0; i < _control.Length; i++) { AppendCharacter(_control[i], _controlArrivals[i]); }
            ClearControl();
            FlushDeferred();
        }
        private void ClearControl() { _control.Clear(); _controlArrivals.Clear(); }
        private void FlushPending() {
            if (_pending.Length > _contextLength && !LimitReached) {
                RedactedOutput safe = _redactor.ApplyPrefix(_pending.ToString(), _pending.Length, _contextLength, _rowChanges);
                if (safe.TimedOut) { RedactionFallback("pattern timeout", suppress: false); return; }
                QueueOutput(safe);
            }
            _pending.Clear(); _arrivals.Clear(); _rowChanges.Clear();
            _contextLength = 0; _nextFlush = FlushThreshold;
            FlushDeferred();
        }

        private void FlushPrefix() {
            string text = _pending.ToString();
            int prefix = _redactor.PrefixLength(text, _rowChanges);
            if (prefix <= _contextLength) { return; }
            RedactedOutput safe = _redactor.ApplyPrefix(text, prefix, _contextLength, _rowChanges);
            if (safe.TimedOut) { RedactionFallback("pattern timeout", suppress: true); return; }
            if (safe.SourceLength <= _contextLength) { return; }
            QueueOutput(safe);
            int retain = RecordingRedactor.ContextStart(text, safe.SourceLength);
            _pending.Remove(0, retain); _arrivals.RemoveRange(0, retain); _rowChanges.RemoveRange(0, retain);
            _contextLength = safe.SourceLength - retain;
            FlushDeferred();
        }

        private void QueueOutput(RedactedOutput safe) {
            int start = 0;
            while (start < safe.Text.Length && safe.Offsets[start] < _contextLength) { start++; }
            while (start < safe.Text.Length && safe.Offsets[start] < safe.SourceLength) {
                Arrival arrival = _arrivals[safe.Offsets[start]];
                int end = start + 1;
                while (end < safe.Text.Length && end - start < OutputChunkCharacters && safe.Offsets[end] < safe.SourceLength
                    && _arrivals[safe.Offsets[end]].Sequence == arrival.Sequence) { end++; }
                if (end < safe.Text.Length && char.IsHighSurrogate(safe.Text[end - 1]) && char.IsLowSurrogate(safe.Text[end])) {
                    end += end == start + 1 ? 1 : -1;
                }
                _deferred.Add(new BufferedEvent(arrival, "o", safe.Text.Substring(start, end - start)));
                start = end;
            }
        }

        private void RedactionFallback(string reason, bool suppress) {
            Arrival first = _arrivals.Count > _contextLength ? _arrivals[_contextLength] : Now();
            _pending.Clear(); _arrivals.Clear(); _rowChanges.Clear(); _contextLength = 0;
            _redactionFallbacks++; _suppressRecord = suppress; _nextFlush = FlushThreshold;
            _deferred.Add(new BufferedEvent(first, "o", "[REDACTED: " + reason + "]\r\n"));
            FlushDeferred();
        }
        private void FlushDeferred() {
            // List.Sort is unstable: multiple chunks from one read must retain source order.
            for (int i = 0; i < _deferred.Count; i++) { _deferred[i].Order = i; }
            _deferred.Sort((a, b) => {
                int sequence = a.Arrival.Sequence.CompareTo(b.Arrival.Sequence);
                return sequence != 0 ? sequence : a.Order.CompareTo(b.Order);
            });
            long before = _pending.Length > _contextLength ? _arrivals[_contextLength].Sequence : long.MaxValue;
            if (_controlArrivals.Count > 0) { before = Math.Min(before, _controlArrivals[0].Sequence); }
            int count = 0;
            while (count < _deferred.Count && _deferred[count].Arrival.Sequence <= before) { Event(_deferred[count++]); }
            _deferred.RemoveRange(0, count);
            if (LimitReached) { _deferred.Clear(); }
        }
        private Arrival Now() => new Arrival(_clock.Elapsed.TotalSeconds, ++_sequence);
        // Hidden demo setup consumes real time, but must not become a blank opening or an idle playback gap.
        internal void ResumeCapture() { _lastReal = _clock.Elapsed.TotalSeconds; }
        private void Queue(BufferedEvent item) {
            if (_pending.Length > _contextLength || _control.Length > 0) { _deferred.Add(item); }
            else { Event(item); }
        }
        public void Resize(int width, int height) {
            if (!_disposed && !StopRequested && !LimitReached) {
                _screen.Resize(width, height);
                Queue(new BufferedEvent(Now(), "r", width + "x" + height));
            }
        }
        public void Marker(string label) => Marker(label, Now());
        public void Marker(string label, TimeSpan hold, RecordingPace? pace, bool isChapter) => Marker(label, hold, pace, isChapter, Now());
        private void Marker(string label, TimeSpan hold, RecordingPace? pace, bool isChapter, Arrival arrival) {
            string data = SessionMarkerData.Write(_redactor.Apply(label.Replace('\r', ' ').Replace('\n', ' ')), hold, pace, isChapter);
            if (_disposed || LimitReached || StopRequested) { return; }
            // Metadata shares the same ordered queue as output and resize events.
            Queue(new BufferedEvent(arrival, "cm", data));
        }
        private void Marker(string label, Arrival arrival) {
            if (_disposed || LimitReached || StopRequested) { return; }
            Queue(new BufferedEvent(arrival, "m", _redactor.Apply(label.Replace('\r', ' ').Replace('\n', ' '))));
        }
        private void Event(BufferedEvent item) {
            if (item.Value.Length == 0 || LimitReached || StopRequested) { return; }
            double gap = Math.Max(0, item.Arrival.Time - _lastReal);
            // Preserve source timestamps. The header idle limit is a player hint; export compression is reversible.
            _time += gap;
            _lastReal = Math.Max(_lastReal, item.Arrival.Time);
            Write("[" + Seconds(_time) + "," + Json(item.Kind) + "," + Json(item.Value) + "]\n");
            if (!LimitReached) {
                if (item.Kind == "m") { _markers.Add(new object?[] { (long)(_time * 1000), item.Value }); }
                else if (item.Kind == "cm") {
                    RecordingMarker marker = SessionMarkerData.Read(TimeSpan.Zero, item.Value);
                    if (marker.IsChapter) { _markers.Add(new object?[] { (long)(_time * 1000), marker.Label }); }
                }
            }
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
                if (PendingPrivatePrefix(_control.ToString()) || PrivatePrefix(_control.ToString()) != null) { ClearControl(); }
                else { FlushNonPrivateControl(); }
            }
            FlushPending(); _disposed = true; _file.Dispose();
            string metadata = MiniJson.Write(new Dictionary<string, object?> {
                ["format"] = "consolemonster-session", ["version"] = 1, ["recording"] = true,
                ["started"] = Started.ToString("O", CultureInfo.InvariantCulture), ["host"] = _redactor.Apply(Environment.MachineName),
                ["mode"] = Mode, ["sizeLimitReached"] = LimitReached, ["markers"] = _markers,
                ["redactionFallbacks"] = _redactionFallbacks
            });
            using (FileStream stream = _metadata) { byte[] bytes = Encoding.UTF8.GetBytes(metadata); stream.Write(bytes, 0, bytes.Length); }
        }
        private readonly struct Arrival {
            public Arrival(double time, long sequence) { Time = time; Sequence = sequence; }
            public double Time { get; } public long Sequence { get; }
        }
        private sealed class BufferedEvent {
            public BufferedEvent(Arrival arrival, string kind, string value) { Arrival = arrival; Kind = kind; Value = value; }
            public int Order { get; set; }
            public Arrival Arrival { get; } public string Kind { get; } public string Value { get; }
        }
    }
}
