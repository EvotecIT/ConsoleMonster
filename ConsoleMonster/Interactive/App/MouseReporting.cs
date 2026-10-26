using System;
using System.Globalization;
using System.Text;
using ConsoleMonster.Output;

namespace ConsoleMonster.Interactive {
    /// <summary>Owns xterm button reporting for the lifetime of one interactive app, never through Console.Out capture.</summary>
    internal sealed class MouseReporting : IDisposable {
        [ThreadStatic] private static MouseReporting? _current;
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

        internal static MouseReporting? Open(bool enabled, InteractiveOptions interactive, Capabilities.TerminalProfile profile, Input.IInputSource input, bool customOutput = false) {
            if (!enabled || !(input is Input.ConsoleKeyInput) || customOutput || interactive.Capture != null
                || !profile.SupportsVirtualTerminal || profile.IsContinuousIntegration || profile.IsOutputRedirected || profile.Mode != Capabilities.OutputMode.Interactive) { return null; }
            if (_current != null && _current._active) { return null; }
            ITerminalWriter? writer = interactive.ResolveTarget().Writer;
            bool owns = writer == null;
            writer ??= TerminalWriterFactory.Create(profile);
            // Unix TTY output uses StreamTerminalWriter, whose IsConsole flag describes a Windows console handle.
            // The detected profile already established a real TTY; only custom borrowed stream targets are excluded.
            if (!owns && !writer.IsConsole) { return null; }
            MouseReporting scope = new MouseReporting(writer, owns);
            _current = scope;
            return scope;
        }

        private void Set(bool enabled) {
            StringBuilder sequence = new StringBuilder();
            foreach (int mode in new[] { 1002, 1006 }) {
                sequence.Append((char)27).Append('[').Append('?').Append(mode.ToString(CultureInfo.InvariantCulture)).Append(enabled ? 'h' : 'l');
            }
            _writer.Write(sequence.ToString());
            _writer.Flush();
        }

        public void Dispose() {
            if (!_active) { return; }
            _active = false;
            if (ReferenceEquals(_current, this)) { _current = null; }
            try { Set(false); } finally { if (_ownsWriter) { _writer.Dispose(); } }
        }
    }
}
