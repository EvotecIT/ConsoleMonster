using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Draws a prompt in the scrollback, below the cursor, without taking over the screen. The region is redrawn in place
    /// line by line (only changed lines are written), grows by scrolling when needed, and on completion is replaced by a
    /// one-line summary that stays in the scrollback like any other output. Lines are kept one cell narrower than the
    /// window so the terminal never wraps them; after a narrowing resize the reflowed height is accounted for.
    /// </summary>
    internal sealed class InlineSurface : IPromptSurface {
        private const string Csi = "\u001b[";
        private readonly ITerminalWriter _writer;
        private readonly bool _ownsWriter;
        private readonly TerminalProfile _profile;
        private readonly Func<int[]?>? _sizeSource;
        private readonly int? _maxHeight;
        private readonly List<string> _rows = new List<string>();
        private readonly List<int> _rowWidths = new List<int>();
        private readonly StringBuilder _sb = new StringBuilder(4096);
        private int _windowWidth;
        private int _windowHeight;
        private int _cursorRow;
        private int _cursorColumn;
        private int _moveUpOnFullDraw;
        private bool _full = true;
        private bool _drawn;
        private bool _completed;
        private bool _startOnNewLine;

        public InlineSurface(TerminalProfile profile, ITerminalWriter? writer = null, int? maxHeight = null, Func<int[]?>? sizeSource = null) {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _ownsWriter = writer == null;
            _writer = writer ?? TerminalWriterFactory.Create(profile);
            _maxHeight = maxHeight;
            _sizeSource = sizeSource;
            ReadSize(out _windowWidth, out _windowHeight);
            if (writer == null) {
                // On Unix the column is unknown (querying it would read the reply from the keyboard input): the area starts on
                // a new line, so text before it (Write-Host -NoNewline) is never overwritten.
                _startOnNewLine = StartOnNewLine(Output.ConsoleCursor.CanQuery, () => Console.CursorLeft, () => Console.CursorTop);
            }
        }

        /// <summary>
        /// Whether the area starts on a new line: when the cursor is past the start of a line, or when its column is unknown
        /// (never queried when <paramref name="canQuery"/> is false).
        /// </summary>
        internal static bool StartOnNewLine(bool canQuery, Func<int> readLeft, Func<int> readTop) =>
            !Output.ConsoleCursor.TryGetPosition(canQuery, readLeft, readTop, out int left, out _) || left > 0;

        public int Width => Math.Max(10, _windowWidth - 1);

        public int Height {
            get {
                int available = Math.Max(3, _windowHeight - 2);
                return Math.Max(3, Math.Min(available, _maxHeight ?? 24));
            }
        }

        public bool IsInline => true;

        private bool Hyperlinks => _profile.Kind == TerminalKind.WindowsTerminal || _profile.Kind == TerminalKind.VisualStudioCode;

        public bool Refresh() {
            ReadSize(out int width, out int height);
            if (width == _windowWidth && height == _windowHeight) {
                return false;
            }
            int newUsable = Math.Max(1, width);
            int up = _cursorRow;
            if (width < _windowWidth && _drawn) {
                // The terminal reflows our lines at the new width: count the rows they occupy now above the cursor.
                up = 0;
                for (int i = 0; i < _cursorRow && i < _rowWidths.Count; i++) {
                    up += Math.Max(1, (_rowWidths[i] + newUsable - 1) / newUsable);
                }
                up += _cursorColumn / newUsable;
            }
            _moveUpOnFullDraw = up;
            _windowWidth = width;
            _windowHeight = height;
            _full = true;
            return true;
        }

        public void Present(CellBuffer frame, Caret? caret) {
            if (frame == null) {
                throw new ArgumentNullException(nameof(frame));
            }
            int count = Math.Min(frame.Height, Height);
            List<string> rows = new List<string>(count);
            List<int> widths = new List<int>(count);
            for (int y = 0; y < count; y++) {
                rows.Add(EncodeRow(frame, y, out int width));
                widths.Add(width);
            }
            _sb.Clear();
            _sb.Append(Csi).Append("?25l");
            if (_full || !_drawn) {
                if (_drawn) {
                    MoveUp(_full ? _moveUpOnFullDraw : _cursorRow);
                } else if (_startOnNewLine) {
                    _sb.Append("\r\n");
                    _startOnNewLine = false;
                }
                _sb.Append('\r').Append(Csi).Append('J');
                for (int i = 0; i < rows.Count; i++) {
                    _sb.Append(rows[i]);
                    if (i < rows.Count - 1) {
                        _sb.Append("\r\n");
                    }
                }
                _cursorRow = Math.Max(0, rows.Count - 1);
                _full = false;
                _drawn = true;
            } else {
                int previous = _rows.Count;
                if (rows.Count > previous) {
                    MoveTo(Math.Max(0, previous - 1));
                    for (int k = previous; k < rows.Count; k++) {
                        _sb.Append("\r\n");
                    }
                    _cursorRow = rows.Count - 1;
                }
                for (int i = 0; i < rows.Count; i++) {
                    if (i >= previous || !string.Equals(rows[i], _rows[i], StringComparison.Ordinal)) {
                        MoveTo(i);
                        _sb.Append('\r').Append(rows[i]);
                    }
                }
                if (rows.Count < previous) {
                    MoveTo(rows.Count);
                    _sb.Append('\r').Append(Csi).Append('J');
                    MoveTo(Math.Max(0, rows.Count - 1));
                }
            }
            _rows.Clear();
            _rows.AddRange(rows);
            _rowWidths.Clear();
            _rowWidths.AddRange(widths);
            if (caret.HasValue && caret.Value.Y < rows.Count) {
                MoveTo(caret.Value.Y);
                _sb.Append('\r');
                if (caret.Value.X > 0) {
                    _sb.Append(Csi).Append(caret.Value.X.ToString(CultureInfo.InvariantCulture)).Append('C');
                }
                _cursorColumn = caret.Value.X;
                _sb.Append(Csi).Append("?25h");
            } else {
                _sb.Append('\r');
                _cursorColumn = 0;
            }
            Emit();
        }

        public void Complete(IReadOnlyList<StyledLine>? summary) {
            if (_completed) {
                return;
            }
            _completed = true;
            _sb.Clear();
            if (_drawn) {
                MoveTo(0);
                _sb.Append('\r').Append(Csi).Append('J');
            }
            if (summary != null) {
                foreach (StyledLine line in summary) {
                    AppendLine(line);
                    _sb.Append("\r\n");
                }
            }
            _sb.Append(Csi).Append("?25h");
            Emit();
            _rows.Clear();
            _drawn = false;
        }

        public void Dispose() {
            if (!_completed) {
                Complete(null);
            }
            if (_ownsWriter) {
                _writer.Dispose();
            }
        }

        private string EncodeRow(CellBuffer frame, int y, out int width) {
            StringBuilder row = new StringBuilder();
            ColorDepth depth = _profile.ColorDepth;
            int end = Math.Min(CellEncoder.ContentEnd(frame, y, depth), Width);
            Style? current = null;
            CellEncoder.AppendRun(row, frame, y, 0, end, depth, ref current, true, Hyperlinks);
            if (depth != ColorDepth.None && end > 0) {
                row.Append(SgrEncoder.Reset);
            }
            row.Append(Csi).Append('K');
            width = end;
            return row.ToString();
        }

        private void AppendLine(StyledLine line) {
            ColorDepth depth = _profile.ColorDepth;
            foreach (TextSpan span in line) {
                SgrEncoder.Append(_sb, span.Style, depth);
                TextSanitizer.Append(_sb, span.Text);
            }
            if (depth != ColorDepth.None) {
                _sb.Append(SgrEncoder.Reset);
            }
        }

        private void MoveTo(int row) {
            int delta = row - _cursorRow;
            if (delta < 0) {
                MoveUp(-delta);
            } else if (delta > 0) {
                _sb.Append(Csi).Append(delta.ToString(CultureInfo.InvariantCulture)).Append('B');
            }
            _cursorRow = row;
        }

        private void MoveUp(int rows) {
            if (rows > 0) {
                _sb.Append(Csi).Append(rows.ToString(CultureInfo.InvariantCulture)).Append('A');
            }
        }

        private void ReadSize(out int width, out int height) {
            if (_sizeSource != null) {
                int[]? size = _sizeSource();
                if (size != null && size.Length == 2 && size[0] > 0 && size[1] > 0) {
                    width = size[0];
                    height = size[1];
                    return;
                }
            } else if (TerminalSize.TryGet(out width, out height) && width > 0 && height > 0) {
                return;
            }
            width = _profile.Width;
            height = _profile.Height;
        }

        private void Emit() {
            _writer.Write(_sb.ToString());
            _writer.Flush();
            _sb.Clear();
        }
    }
}
