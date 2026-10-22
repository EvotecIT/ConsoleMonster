using System;
using System.Globalization;
using System.Text;
using ConsoleMonster.Output;

namespace ConsoleMonster.Interactive {
    /// <summary>Owns xterm button reporting for the lifetime of one interactive app, never through Console.Out capture.</summary>
    internal sealed class MouseReporting : IDisposable {
        private readonly ITerminalWriter _writer;
        private readonly bool _ownsWriter;
        private bool _active;

        internal MouseReporting(ITerminalWriter writer, bool ownsWriter) {
            _writer = writer;
            _ownsWriter = ownsWriter;
            try { Set(true); _active = true; }
            catch {
                try { Set(false); } finally { if (_ownsWriter) { _writer.Dispose(); } }
                throw;
            }
        }

        private void Set(bool enabled) {
            StringBuilder sequence = new StringBuilder();
            foreach (int mode in new[] { 1000, 1006 }) {
                sequence.Append((char)27).Append('[').Append('?').Append(mode.ToString(CultureInfo.InvariantCulture)).Append(enabled ? 'h' : 'l');
            }
            _writer.Write(sequence.ToString());
            _writer.Flush();
        }

        public void Dispose() {
            if (!_active) { return; }
            _active = false;
            try { Set(false); } finally { if (_ownsWriter) { _writer.Dispose(); } }
        }
    }
}
