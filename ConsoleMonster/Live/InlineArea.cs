using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interop;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Live {
    /// <summary>
    /// A few lines below the cursor, drawn on demand on the calling thread (no render thread), on a console that other code
    /// writes to at the same time, such as PowerShell's warnings or Write-Host. Before each draw the area checks the
    /// console buffer for its own lines: when nothing was written after them they are redrawn in place; when other output
    /// came after them, the old lines are deleted (the output below moves up) and the area is drawn again under that
    /// output, so nothing is overwritten and no stale copy stays behind. <see cref="Clear"/> removes the area the same way.
    /// It needs a Windows console whose buffer can be read (Windows Terminal, conhost); see <see cref="IsSupported"/>.
    /// Enable-ConsoleFormatting -Feature Progress draws Write-Progress with it.
    /// </summary>
    public sealed class InlineArea : IDisposable {
        private const string Esc = "\u001b[";
        private readonly TerminalProfile _profile;
        private readonly ITerminalWriter _writer;
        private readonly IConsoleScreen _screen;
        private readonly bool _ownsWriter;
        private readonly List<string> _signatures = new List<string>();

        /// <summary>Creates an area on the current console.</summary>
        /// <param name="profile">The terminal profile, or null for the current one.</param>
        public InlineArea(TerminalProfile? profile = null) : this(profile ?? Terminal.GetCurrentProfile(), null, new WindowsConsoleScreen()) {
        }

        internal InlineArea(TerminalProfile profile, ITerminalWriter? writer, IConsoleScreen screen) {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _ownsWriter = writer == null;
            _writer = writer ?? TerminalWriterFactory.Create(profile);
        }

        /// <summary>The rows the area occupies now (0 when it is not shown).</summary>
        public int Height => _signatures.Count;

        /// <summary>Whether an area can be drawn on the current console: an interactive Windows console whose buffer can be read.</summary>
        /// <param name="profile">The terminal profile, or null for the current one.</param>
        /// <returns>True when <see cref="Draw(IWidget, RenderContext)"/> works here.</returns>
        public static bool IsSupported(TerminalProfile? profile = null) {
            TerminalProfile p = profile ?? Terminal.GetCurrentProfile();
            return WindowsConsole.IsWindows && p.Mode == OutputMode.Interactive && p.IsConsoleHandle
                && new WindowsConsoleScreen().TryGetCursor(out _, out _, out _, out _);
        }

        /// <summary>The widest the content is drawn (0, the default, for the console's width), so short rows keep their right-aligned parts close.</summary>
        public int MaxWidth { get; set; }

        /// <summary>The width content is drawn at: the console's width less one column, so a full row never wraps (at most <see cref="MaxWidth"/>).</summary>
        public int Width {
            get {
                int width = _profile.Width;
                if (_screen.TryGetCursor(out _, out _, out int bufferWidth, out _) && bufferWidth > 0) {
                    width = width > 0 ? Math.Min(width, bufferWidth) : bufferWidth;
                }
                width = Math.Max(1, width - 1);
                return MaxWidth > 0 ? Math.Min(width, MaxWidth) : width;
            }
        }

        /// <summary>Draws a widget as the area's new content, as tall as it needs (at most the window height minus one).</summary>
        /// <param name="widget">The content.</param>
        /// <param name="context">The render context (for example with a clock for elapsed times), or null for the terminal's.</param>
        public void Draw(IWidget widget, RenderContext? context = null) {
            if (widget == null) {
                throw new ArgumentNullException(nameof(widget));
            }
            context ??= Terminal.CreateContext(_profile);
            int width = Width;
            int maxHeight = Math.Max(1, _profile.Height - 1);
            Size size = widget.Measure(new Constraints(width, maxHeight), context);
            CellBuffer frame = new CellBuffer(width, Math.Max(1, Math.Min(maxHeight, size.Height)));
            widget.Render(new Region(frame), context);
            Draw(frame);
        }

        /// <summary>Draws a rendered frame as the area's new content.</summary>
        /// <param name="frame">The frame; one row per line.</param>
        public void Draw(CellBuffer frame) {
            if (frame == null) {
                throw new ArgumentNullException(nameof(frame));
            }
            StringBuilder sb = new StringBuilder(4096);
            sb.Append(Esc).Append("?2026h");
            Place place = Locate();
            if (place.Found && !place.Foreign) {
                // Nothing was written after the area: back to its first row and draw over it.
                MoveUp(sb, Height);
            } else {
                if (place.Found) {
                    Remove(sb, place);
                }
                if (place.CursorX != 0) {
                    sb.Append("\r\n");
                }
            }
            int previous = place.Found && !place.Foreign ? Height : 0;
            _signatures.Clear();
            Style? current = null;
            for (int y = 0; y < frame.Height; y++) {
                sb.Append('\r');
                CellEncoder.AppendRun(sb, frame, y, 0, CellEncoder.ContentEnd(frame, y, _profile.ColorDepth), _profile.ColorDepth, ref current);
                if (current.HasValue) {
                    sb.Append(Esc).Append("0m");
                    current = null;
                }
                sb.Append(Esc).Append('K').Append("\r\n");
                _signatures.Add(Signature(frame.GetRowText(y)));
            }
            if (frame.Height < previous) {
                // The area shrank: erase the old rows below the new last row.
                sb.Append(Esc).Append('J');
            }
            sb.Append(Esc).Append("?2026l");
            _writer.Write(sb.ToString());
            _writer.Flush();
        }

        /// <summary>Removes the area; later output starts where it was (or, after other output, where that output ended).</summary>
        public void Clear() {
            if (Height == 0) {
                return;
            }
            StringBuilder sb = new StringBuilder();
            Place place = Locate();
            if (place.Found && !place.Foreign) {
                MoveUp(sb, Height);
                sb.Append(Esc).Append('J');
            } else if (place.Found) {
                Remove(sb, place);
            }
            _signatures.Clear();
            if (sb.Length > 0) {
                _writer.Write(sb.ToString());
                _writer.Flush();
            }
        }

        /// <summary>Forgets the area without touching the screen (it stays as printed text).</summary>
        public void Abandon() => _signatures.Clear();

        /// <inheritdoc />
        public void Dispose() {
            Clear();
            if (_ownsWriter) {
                _writer.Dispose();
            }
        }

        private static void MoveUp(StringBuilder sb, int rows) {
            sb.Append('\r');
            if (rows > 0) {
                sb.Append(Esc).Append(rows.ToString(CultureInfo.InvariantCulture)).Append('A');
            }
        }

        // Deletes the area's rows (the output after them moves up) and puts the cursor back after that output.
        private void Remove(StringBuilder sb, Place place) {
            int up = place.CursorY - place.Top;
            MoveUp(sb, up);
            sb.Append(Esc).Append(Height.ToString(CultureInfo.InvariantCulture)).Append('M');
            int down = up - Height;
            if (down > 0) {
                sb.Append(Esc).Append(down.ToString(CultureInfo.InvariantCulture)).Append('B');
            }
            if (place.CursorX > 0) {
                sb.Append(Esc).Append((place.CursorX + 1).ToString(CultureInfo.InvariantCulture)).Append('G');
            }
        }

        // Where the area's rows are now: right above the cursor (nothing written since), further up (output came after
        // them), or not found (scrolled out of the window, overwritten, or the buffer cannot be read).
        private Place Locate() {
            if (!_screen.TryGetCursor(out int x, out int y, out int width, out int windowTop)) {
                return new Place(false, false, 0, 0, 0);
            }
            int height = Height;
            if (height == 0 || !HasText()) {
                return new Place(false, false, 0, x, y);
            }
            Dictionary<int, string> rows = new Dictionary<int, string>();
            for (int top = y - height; top >= windowTop && top >= 0; top--) {
                if (Matches(top, width, rows)) {
                    return new Place(true, top != y - height || x != 0, top, x, y);
                }
            }
            return new Place(false, false, 0, x, y);
        }

        private bool Matches(int top, int width, Dictionary<int, string> rows) {
            for (int i = 0; i < _signatures.Count; i++) {
                if (!rows.TryGetValue(top + i, out string? row)) {
                    row = Signature(_screen.ReadRow(top + i, width));
                    rows[top + i] = row;
                }
                if (!string.Equals(row, _signatures[i], StringComparison.Ordinal)) {
                    return false;
                }
            }
            return true;
        }

        private bool HasText() {
            foreach (string signature in _signatures) {
                if (signature.Length > 0) {
                    return true;
                }
            }
            return false;
        }

        // What can be compared between our rows and the console buffer: printable ASCII only (glyphs, wide characters and
        // spaces are read back differently by different consoles).
        internal static string Signature(string? text) {
            if (string.IsNullOrEmpty(text)) {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder(text!.Length);
            foreach (char c in text) {
                if (c > ' ' && c < 0x7F) {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private readonly struct Place {
            internal Place(bool found, bool foreign, int top, int cursorX, int cursorY) {
                Found = found;
                Foreign = foreign;
                Top = top;
                CursorX = cursorX;
                CursorY = cursorY;
            }

            internal bool Found { get; }

            internal bool Foreign { get; }

            internal int Top { get; }

            internal int CursorX { get; }

            internal int CursorY { get; }
        }
    }

    /// <summary>Reads the console's cursor and buffer rows (the Windows console; tests use a fake).</summary>
    internal interface IConsoleScreen {
        bool TryGetCursor(out int x, out int y, out int width, out int windowTop);

        string? ReadRow(int y, int width);
    }

    internal sealed class WindowsConsoleScreen : IConsoleScreen {
        public bool TryGetCursor(out int x, out int y, out int width, out int windowTop) =>
            WindowsConsole.TryGetCursor(WindowsConsole.StandardOutput, out x, out y, out width, out windowTop);

        public string? ReadRow(int y, int width) => WindowsConsole.TryReadRow(WindowsConsole.StandardOutput, y, width);
    }
}
