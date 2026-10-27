using System;
using System.Management.Automation;
using System.Management.Automation.Host;

namespace ConsoleMonster.PowerShell.Display;

// Runspace-thread only. Never runs a timer callback against a host or moves over intervening stream output.
internal sealed class CollectingStatusLine : IDisposable {
    private readonly PSCmdlet _sink;
    private Coordinates? _start;
    private Coordinates? _end;
    private int _length;
    internal CollectingStatusLine(PSCmdlet sink) { _sink = sink; }
    internal void Update(int count, int width) {
        try {
            if (!DefaultDisplayHostGate.IsTranscriptInactive(_sink.Host.UI)) { Dispose(); return; }
            if (_end.HasValue && !Same(_sink.Host.UI.RawUI.CursorPosition, _end.Value)) { _start = null; _end = null; return; }
            Dispose();
            _start = _sink.Host.UI.RawUI.CursorPosition;
            string text = "collecting " + count.ToString("N0", System.Globalization.CultureInfo.CurrentCulture) + " rows...";
            int available = Math.Max(0, width - _start.Value.X - 1);
            if (text.Length > available) { text = text.Substring(0, available); }
            _length = text.Length;
            _sink.Host.UI.Write(ConsoleColor.DarkGray, _sink.Host.UI.RawUI.BackgroundColor, text);
            _end = _sink.Host.UI.RawUI.CursorPosition;
        } catch (Exception) { _start = null; _end = null; }
    }
    public void Dispose() {
        try {
            if (_start.HasValue && _end.HasValue && Same(_sink.Host.UI.RawUI.CursorPosition, _end.Value)
                && DefaultDisplayHostGate.IsTranscriptInactive(_sink.Host.UI)) {
                _sink.Host.UI.RawUI.CursorPosition = _start.Value;
                _sink.Host.UI.Write(new string(' ', _length));
                _sink.Host.UI.RawUI.CursorPosition = _start.Value;
            }
        } catch (Exception) { } finally { _start = null; _end = null; }
    }
    private static bool Same(Coordinates left, Coordinates right) => left.X == right.X && left.Y == right.Y;
}
