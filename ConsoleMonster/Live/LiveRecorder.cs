using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ConsoleMonster.Live {
    /// <summary>Writes dependency-free JSON-lines recordings. Calls must be serialized by the owner.</summary>
    public sealed class LiveRecorder : IDisposable {
        private readonly TextWriter _writer;
        private readonly bool _leaveOpen;
        private bool _disposed;
        private long _written;

        // Retain only failures at the physical writer boundary, not invalid event/serialization diagnostics.
        internal Exception? WriteFailure { get; private set; }

        /// <summary>The most characters written before further events are dropped (0: no limit). Used by automatic recording.</summary>
        internal long Limit { get; set; }

        /// <summary>Whether events were dropped because <see cref="Limit"/> was reached.</summary>
        internal bool Truncated { get; private set; }

        /// <summary>Creates a recorder and writes its version header.</summary>
        /// <param name="writer">Destination.</param>
        /// <param name="leaveOpen">Whether disposal leaves the destination open.</param>
        public LiveRecorder(TextWriter writer, bool leaveOpen = true) {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _leaveOpen = leaveOpen;
            _writer.WriteLine("{\"consolemonster\":\"recording\",\"version\":1}");
        }

        /// <summary>Records an applied event with invariant, round-trip numeric values.</summary>
        /// <param name="liveEvent">The event.</param>
        public void Write(LiveEvent liveEvent) {
            if (_disposed) { throw new ObjectDisposedException(nameof(LiveRecorder)); }
            if (liveEvent == null) { throw new ArgumentNullException(nameof(liveEvent)); }
            if (!Finite(liveEvent.Value) || !Finite(liveEvent.Total)) {
                throw new ArgumentException("JSON numbers must be finite.", nameof(liveEvent));
            }
            StringBuilder json = new StringBuilder("{");
            if (liveEvent.Time != 0) { json.Append("\"t\":").Append(liveEvent.Time.ToString(CultureInfo.InvariantCulture)).Append(','); }
            Field(json, "target", liveEvent.Target);
            Field(json, "op", liveEvent.Operation);
            Field(json, "key", liveEvent.Key);
            Field(json, "text", liveEvent.Text);
            Field(json, "state", liveEvent.State);
            if (liveEvent.Value != 0) { json.Append("\"value\":").Append(liveEvent.Value.ToString("R", CultureInfo.InvariantCulture)).Append(','); }
            if (liveEvent.Total != 0) { json.Append("\"total\":").Append(liveEvent.Total.ToString("R", CultureInfo.InvariantCulture)).Append(','); }
            if (liveEvent.IsMarkup) { json.Append("\"markup\":1,"); }
            json.Length--;
            json.Append('}');
            if (Limit > 0) {
                if (Truncated || _written + json.Length > Limit) {
                    Truncated = true;
                    return;
                }
                _written += json.Length + 2;
            }
            try { _writer.WriteLine(json.ToString()); }
            catch (Exception ex) { WriteFailure ??= ex; throw; }
        }

        /// <summary>Flushes the recording.</summary>
        public void Flush() => _writer.Flush();

        /// <inheritdoc />
        public void Dispose() {
            if (_disposed) { return; }
            _disposed = true;
            try { _writer.Flush(); }
            finally { if (!_leaveOpen) { _writer.Dispose(); } }
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static void Field(StringBuilder json, string name, string? value) {
            if (value == null) { return; }
            json.Append('"').Append(name).Append("\":\"");
            foreach (char c in value) {
                if (c == '"' || c == '\\') { json.Append('\\').Append(c); }
                else if (c < ' ' || char.IsSurrogate(c)) { json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture)); }
                else { json.Append(c); }
            }
            json.Append("\",");
        }
    }
}
