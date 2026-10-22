using System;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interop;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Rendering {
    /// <summary>
    /// A live area of a few lines anchored below the cursor, in the normal screen (no alternate screen): the frame is
    /// redrawn in place, lines written during the session are inserted above it (so output and logs scroll by while the
    /// area stays at the bottom), and when the session ends the area is cleared so the final state can be printed into the
    /// scrollback (or left out for a transient area). Frames are as tall as the content, not the window.
    /// </summary>
    public sealed class InlineOutput : ITerminalOutput {
        private const string Esc = "\u001b[";
        private readonly ITerminalWriter _writer;
        private readonly bool _ownsWriter;
        private readonly StringBuilder _sb = new StringBuilder(8192);
        private CellBuffer? _previous;
        private int _drawn;
        private int _drawnWidth;
        private bool _invalidated = true;
        private bool _live;
        private WindowsOutputModeScope? _outputMode;

        /// <summary>Creates an inline output.</summary>
        /// <param name="writer">The terminal writer.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="ownsWriter">Whether disposing the output disposes the writer.</param>
        public InlineOutput(ITerminalWriter writer, TerminalProfile profile, bool ownsWriter = true) {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _ownsWriter = ownsWriter;
        }

        /// <inheritdoc />
        public TerminalProfile Profile { get; }

        /// <inheritdoc />
        public bool SupportsLiveFrames => true;

        /// <summary>The number of rows the live area occupies now.</summary>
        public int Height => _drawn;

        private bool Hyperlinks => Profile.SupportsHyperlinks;

        /// <inheritdoc />
        public void WriteBlock(CellBuffer buffer) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }
            _sb.Clear();
            ClearArea();
            for (int y = 0; y < buffer.Height; y++) {
                AppendRow(buffer, y, CellEncoder.ContentEnd(buffer, y, Profile.ColorDepth));
                _sb.Append("\r\n");
            }
            Redraw();
            Emit();
        }

        /// <inheritdoc />
        public void WriteLine(StyledLine line) {
            if (line == null) {
                throw new ArgumentNullException(nameof(line));
            }
            _sb.Clear();
            ClearArea();
            AppendStyled(line);
            _sb.Append("\r\n");
            Redraw();
            Emit();
        }

        /// <inheritdoc />
        public void WriteInline(StyledLine line) => WriteLine(line);

        /// <inheritdoc />
        public void BeginLive() {
            if (_live) {
                return;
            }
            _live = true;
            _invalidated = true;
            _outputMode = WindowsOutputModeScope.Activate(_writer);
            try {
                _writer.Write(Esc + "?25l");
                _writer.Flush();
            } catch {
                // A partial cursor update still needs cleanup; preserve the original writer failure.
                try { EndLive(); } catch (Exception) { }
                throw;
            }
        }

        /// <inheritdoc />
        public void RenderFrame(CellBuffer frame) {
            if (frame == null) {
                throw new ArgumentNullException(nameof(frame));
            }
            _sb.Clear();
            _sb.Append(Esc).Append("?2026h");
            bool full = _invalidated || _previous == null || _previous.Width != frame.Width;
            if (_previous != null && _previous.Width != frame.Width) {
                // The terminal may have reflowed the old rows to the new width; clear what they occupy now.
                ClearArea();
            }
            MoveToTop();
            int height = frame.Height;
            for (int y = 0; y < height; y++) {
                bool changed = full || y >= _drawn || !RowEquals(_previous!, frame, y);
                if (changed) {
                    _sb.Append('\r');
                    AppendRow(frame, y, CellEncoder.ContentEnd(frame, y, Profile.ColorDepth));
                    _sb.Append(Esc).Append('K');
                }
                if (y < height - 1) {
                    if (y < _drawn - 1) {
                        _sb.Append(Esc).Append("1B");
                    } else {
                        _sb.Append("\r\n");
                    }
                }
            }
            if (height < _drawn) {
                // The area shrank: erase the rows below the new last row.
                _sb.Append("\r\n").Append(Esc).Append('J').Append(Esc).Append("1A");
            }
            _sb.Append(Esc).Append("?2026l");
            if (_previous == null || _previous.Width != frame.Width || _previous.Height != frame.Height) {
                _previous = new CellBuffer(frame.Width, frame.Height);
            }
            _previous.CopyFrom(frame);
            _drawn = height;
            _drawnWidth = frame.Width;
            _invalidated = false;
            Emit();
        }

        /// <inheritdoc />
        public void Invalidate() {
            _invalidated = true;
        }

        /// <inheritdoc />
        public void EndLive() {
            if (!_live) {
                return;
            }
            _live = false;
            _sb.Clear();
            ClearArea();
            _sb.Append(Esc).Append("0m").Append(Esc).Append("?25h");
            _previous = null;
            try { Emit(); }
            finally {
                _outputMode?.Dispose();
                _outputMode = null;
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            try { EndLive(); }
            finally {
                if (_ownsWriter) { _writer.Dispose(); }
            }
        }

        // Moves to the first column of the area's first row and erases everything from there down; the area is empty after.
        private void ClearArea() {
            if (_drawn == 0) {
                return;
            }
            MoveToTop();
            _sb.Append('\r').Append(Esc).Append('J');
            _drawn = 0;
        }

        // The cursor rests on the area's last row; move up to its first row. After a width change the old rows may have
        // been reflowed into more lines, so the distance is estimated from their content at the current width.
        private void MoveToTop() {
            if (_drawn <= 1) {
                _sb.Append('\r');
                return;
            }
            int width = TerminalWidth();
            int up = _drawn - 1;
            if (_previous != null && width > 0 && width < _drawnWidth) {
                up = 0;
                for (int y = 0; y < _drawn && y < _previous.Height; y++) {
                    int length = Math.Max(1, CellEncoder.ContentEnd(_previous, y, Profile.ColorDepth));
                    up += (length + width - 1) / width;
                }
                up -= 1;
            }
            _sb.Append('\r').Append(Esc).Append(up.ToString(CultureInfo.InvariantCulture)).Append('A');
        }

        private static int TerminalWidth() {
            return TerminalSize.TryGetWindow(out _, out _, out int width, out _) ? width : 0;
        }

        // After inserting output above, the last frame is drawn again below it.
        private void Redraw() {
            if (!_live || _previous == null) {
                return;
            }
            for (int y = 0; y < _previous.Height; y++) {
                _sb.Append('\r');
                AppendRow(_previous, y, CellEncoder.ContentEnd(_previous, y, Profile.ColorDepth));
                _sb.Append(Esc).Append('K');
                if (y < _previous.Height - 1) {
                    _sb.Append("\r\n");
                }
            }
            _drawn = _previous.Height;
            _drawnWidth = _previous.Width;
        }

        private void AppendRow(CellBuffer buffer, int y, int end) {
            ColorDepth depth = Profile.ColorDepth;
            Style? current = null;
            CellEncoder.AppendRun(_sb, buffer, y, 0, Math.Max(0, end), depth, ref current, true, Hyperlinks);
            if (depth != ColorDepth.None) {
                _sb.Append(SgrEncoder.Reset);
            }
        }

        private void AppendStyled(StyledLine line) {
            ColorDepth depth = Profile.ColorDepth;
            CellEncoder.AppendStyled(_sb, line, depth, Hyperlinks);
            if (depth != ColorDepth.None) {
                _sb.Append(SgrEncoder.Reset);
            }
            _sb.Append(Esc).Append('K');
        }

        private static bool RowEquals(CellBuffer a, CellBuffer b, int y) {
            if (a.Width != b.Width || y >= a.Height) {
                return false;
            }
            for (int x = 0; x < a.Width; x++) {
                if (!a[x, y].Equals(b[x, y])) {
                    return false;
                }
            }
            return true;
        }

        private void Emit() {
            if (_sb.Length == 0) {
                return;
            }
            using (WindowsOutputModeScope? mode = WindowsOutputModeScope.Activate(_writer)) {
                _writer.Write(_sb.ToString());
                _writer.Flush();
                _sb.Clear();
            }
        }
    }
}
