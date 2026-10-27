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
    private string _text = string.Empty;
    private bool _disabled;
    internal CollectingStatusLine(PSCmdlet sink) { _sink = sink; }
    internal void Update(int count, int width, bool plain) {
        try {
            if (_disabled) { return; }
            if (!DefaultDisplayHostGate.IsTranscriptInactive(_sink.Host.UI)) { Dispose(); return; }
            Dispose();
            if (_disabled) { return; }
            if (_sink.Host.UI.RawUI.CursorPosition.X != 0) { _sink.Host.UI.WriteLine(); }
            if (_sink.Host.UI.RawUI.CursorPosition.Y >= _sink.Host.UI.RawUI.BufferSize.Height - 1) { _disabled = true; return; }
            _start = _sink.Host.UI.RawUI.CursorPosition;
            string text = "collecting " + count.ToString("N0", System.Globalization.CultureInfo.CurrentCulture) + " rows...";
            int available = Math.Max(0, width - _start.Value.X - 1);
            if (text.Length > available) { text = text.Substring(0, available); }
            _length = text.Length;
            _text = text;
            if (plain) { _sink.Host.UI.WriteLine(text); }
            else { _sink.Host.UI.WriteLine(ConsoleColor.DarkGray, _sink.Host.UI.RawUI.BackgroundColor, text); }
            _end = _sink.Host.UI.RawUI.CursorPosition;
            // Qualify the exact owned-region eraser before the producer can emit another native stream.
            if (_length > 0) {
                BufferCell[,] own = _sink.Host.UI.RawUI.GetBufferContents(Area());
                _sink.Host.UI.RawUI.SetBufferContents(_start.Value, own);
            }
        } catch (Exception) {
            _disabled = true;
            try {
                if (_start.HasValue && _end.HasValue && Same(_sink.Host.UI.RawUI.CursorPosition, _end.Value)) {
                    _sink.Host.UI.RawUI.CursorPosition = _start.Value;
                    _sink.Host.UI.Write(new string(' ', _length));
                    _sink.Host.UI.RawUI.CursorPosition = _start.Value;
                }
            } catch (Exception) { }
            _start = null; _end = null;
        }
    }
    public void Dispose() {
        try {
            if (_start.HasValue && _end.HasValue && _length > 0 && DefaultDisplayHostGate.IsTranscriptInactive(_sink.Host.UI)) {
                PSHostRawUserInterface raw = _sink.Host.UI.RawUI;
                // Cursor equality is insufficient when a buffer scrolls. Every erasure verifies the owned characters.
                Rectangle area = Area();
                BufferCell[,] cells = raw.GetBufferContents(area);
                for (int x = 0; x < _length; x++) { if (cells[0, x].Character != _text[x]) { _disabled = true; return; } }
                bool unchanged = Same(raw.CursorPosition, _end.Value) && _end.Value.Y == _start.Value.Y + 1;
                raw.SetBufferContents(area, new BufferCell(' ', raw.ForegroundColor, raw.BackgroundColor, BufferCellType.Complete));
                if (unchanged) { raw.CursorPosition = _start.Value; }
            }
        } catch (Exception) { _disabled = true; } finally { _start = null; _end = null; }
    }
    private Rectangle Area() => new Rectangle(_start!.Value.X, _start.Value.Y, _start.Value.X + _length - 1, _start.Value.Y);
    private static bool Same(Coordinates left, Coordinates right) => left.X == right.X && left.Y == right.Y;
}
