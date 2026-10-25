using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Sessions {
    /// <summary>
    /// A small VT/ANSI terminal model: it reads the byte stream a program wrote to a pseudo console and keeps the screen as
    /// ConsoleMonster cells, so captured output goes through the same HTML converter as frames rendered in-process.
    /// It covers what console programs and ConPTY emit: SGR colours (16, 256, 24-bit) and decorations, cursor movement,
    /// erase, insert and delete, scroll regions, the alternate screen, autowrap and OSC 8 hyperlinks. Widths come from
    /// ConsoleMonster's own display-width tables, so wide glyphs take two cells exactly as the library laid them out.
    /// </summary>
    internal sealed partial class VtScreen {
        private readonly Decoder _decoder = new UTF8Encoding(false).GetDecoder();
        private readonly char[] _chars = new char[8192];
        private CellBuffer _main;
        private CellBuffer _alternate;
        private bool _onAlternate;
        private readonly List<Cell[]> _scrollback = new List<Cell[]>();
        private readonly bool _keepScrollback;
        private int _x;
        private int _y;
        private int _savedX;
        private int _savedY;
        private bool _savedWrap;
        private bool _savedOrigin;
        private int _mainX;
        private int _mainY;
        private bool _mainWrap;
        private bool _mainOrigin;
        private bool _origin;
        private bool _pendingWrap;
        private bool _autowrap = true;
        private int _top;
        private int _bottom;
        private Style _style = Style.Plain;
        private string? _link;
        private char _pendingHigh;

        // Parser state.
        private int _state;
        private readonly StringBuilder _params = new StringBuilder();
        private readonly StringBuilder _osc = new StringBuilder();
        private char _intermediate;

        public VtScreen(int width, int height, bool keepScrollback = true) {
            _keepScrollback = keepScrollback;
            Width = width;
            Height = height;
            _main = new CellBuffer(width, height);
            _alternate = new CellBuffer(width, height);
            _top = 0;
            _bottom = height - 1;
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        /// <summary>Resizes both screens while preserving their overlapping cells and the parser state.</summary>
        public void Resize(int width, int height) {
            CellBuffer main = new CellBuffer(width, height), alternate = new CellBuffer(width, height);
            for (int y = 0; y < Math.Min(height, Height); y++) {
                for (int x = 0; x < Math.Min(width, Width); x++) {
                    if (x + _main[x, y].Width <= width) { Put(main, x, y, _main[x, y]); }
                    if (x + _alternate[x, y].Width <= width) { Put(alternate, x, y, _alternate[x, y]); }
                }
            }
            _main = main; _alternate = alternate; Width = width; Height = height;
            _x = Math.Min(_x, width - 1); _y = Math.Min(_y, height - 1);
            _savedX = Math.Min(_savedX, width - 1); _savedY = Math.Min(_savedY, height - 1);
            _mainX = Math.Min(_mainX, width - 1); _mainY = Math.Min(_mainY, height - 1);
            _savedWrap = _mainWrap = false;
            _top = 0; _bottom = height - 1; _pendingWrap = false;
        }

        /// <summary>Whether the program is drawing on the alternate screen (a live dashboard).</summary>
        public bool OnAlternate => _onAlternate;

        /// <summary>Whether a synchronized update is still being painted.</summary>
        public bool SynchronizedOutput { get; private set; }

        /// <summary>Notifies replay when a complete synchronized repaint becomes visible.</summary>
        internal Action? SynchronizedFrameCompleted { get; set; }

        private CellBuffer Current => _onAlternate ? _alternate : _main;

        public void Feed(byte[] data, int count) {
            int offset = 0;
            while (offset < count) {
                int take = Math.Min(count - offset, 4096);
                int chars = _decoder.GetChars(data, offset, take, _chars, 0, false);
                for (int i = 0; i < chars; i++) {
                    Process(_chars[i]);
                }
                offset += take;
            }
        }

        public void Feed(string text) {
            foreach (char c in text) {
                Process(c);
            }
        }

        /// <summary>A copy of the visible screen.</summary>
        public CellBuffer Snapshot() {
            CellBuffer copy = new CellBuffer(Width, Height);
            copy.CopyFrom(Current);
            return copy;
        }

        /// <summary>The main screen with everything that scrolled off its top above it (print-once output).</summary>
        public CellBuffer Transcript() {
            int rows = _scrollback.Count + Height;
            int width = Width;
            foreach (Cell[] row in _scrollback) { width = Math.Max(width, row.Length); }
            CellBuffer all = new CellBuffer(width, rows);
            for (int y = 0; y < _scrollback.Count; y++) {
                Cell[] row = _scrollback[y];
                for (int x = 0; x < row.Length; x++) {
                    Put(all, x, y, row[x]);
                }
            }
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    Put(all, x, _scrollback.Count + y, _main[x, y]);
                }
            }
            return all;
        }

        private static void Put(CellBuffer buffer, int x, int y, Cell cell) {
            if (cell.IsContinuation) {
                return;
            }
            buffer.Set(x, y, string.IsNullOrEmpty(cell.Glyph) ? " " : cell.Glyph, cell.Style, Math.Max(1, (int)cell.Width));
        }

        private void Process(char c) {
            switch (_state) {
                case 0:
                    Ground(c);
                    return;
                case 1: // ESC
                    Escape(c);
                    return;
                case 2: // CSI
                    if (c >= 0x30 && c <= 0x3F) {
                        if (_params.Length < 1024) { _params.Append(c); }
                    } else if (c >= 0x20 && c <= 0x2F) {
                        _intermediate = c;
                    } else if (c >= 0x40 && c <= 0x7E) {
                        _state = 0;
                        Csi(c);
                    } else if (c == '\u001b') {
                        _state = 1;
                    }
                    return;
                case 3: // OSC
                    if (c == '\u0007') {
                        _state = 0;
                        Osc(_osc.ToString());
                    } else if (c == '\u001b') {
                        _state = 4;
                    } else {
                        if (_osc.Length < 8192) { _osc.Append(c); }
                    }
                    return;
                case 4: // ESC inside OSC: expect '\' (ST)
                    _state = 0;
                    Osc(_osc.ToString());
                    if (c != '\\') {
                        Process(c);
                    }
                    return;
                case 5: // charset designation: one more character
                    _state = 0;
                    return;
                case 6: // DCS / SOS / PM / APC: skip to ST
                    if (c == '\u001b') {
                        _state = 7;
                    }
                    return;
                case 7:
                    _state = c == '\\' ? 0 : 6;
                    return;
            }
        }

        private void Ground(char c) {
            if (_pendingHigh != '\0') {
                char high = _pendingHigh;
                _pendingHigh = '\0';
                if (char.IsLowSurrogate(c)) {
                    Print(new string(new[] { high, c }));
                    return;
                }
            }
            switch (c) {
                case '\u001b':
                    _state = 1;
                    return;
                case '\r':
                    _x = 0;
                    _pendingWrap = false;
                    return;
                case '\n':
                case '\u000b':
                case '\u000c':
                    LineFeed();
                    return;
                case '\b':
                    if (_x > 0) {
                        _x--;
                    }
                    _pendingWrap = false;
                    return;
                case '\t':
                    _x = Math.Min(Width - 1, ((_x / 8) + 1) * 8);
                    return;
                case '\u0007':
                case '\0':
                    return;
            }
            if (c < 0x20 || c == 0x7F) {
                return;
            }
            if (char.IsHighSurrogate(c)) {
                _pendingHigh = c;
                return;
            }
            Print(c.ToString());
        }

        private void Escape(char c) {
            _state = 0;
            switch (c) {
                case '[':
                    _params.Clear();
                    _intermediate = '\0';
                    _state = 2;
                    return;
                case ']':
                    _osc.Clear();
                    _state = 3;
                    return;
                case 'P':
                case 'X':
                case '^':
                case '_':
                    _state = 6;
                    return;
                case '(':
                case ')':
                case '*':
                case '+':
                    _state = 5;
                    return;
                case '7':
                    _savedX = _x;
                    _savedY = _y;
                    _savedWrap = _pendingWrap;
                    _savedOrigin = _origin;
                    return;
                case '8':
                    _x = _savedX;
                    _y = _savedY;
                    _pendingWrap = _savedWrap;
                    _origin = _savedOrigin;
                    return;
                case 'M':
                    if (_y == _top) {
                        ScrollDown(1);
                    } else if (_y > 0) {
                        _y--;
                    }
                    return;
                case 'D':
                    LineFeed();
                    return;
                case 'E':
                    _x = 0;
                    LineFeed();
                    return;
                case 'c':
                    Reset();
                    return;
            }
        }

        private void Reset() {
            _main = new CellBuffer(Width, Height);
            _alternate = new CellBuffer(Width, Height);
            _onAlternate = false;
            SynchronizedOutput = false;
            _x = _y = _savedX = _savedY = _mainX = _mainY = 0;
            _origin = false;
            _pendingWrap = false;
            _savedWrap = _mainWrap = false;
            _savedOrigin = _mainOrigin = false;
            _autowrap = true;
            _style = Style.Plain;
            _top = 0;
            _bottom = Height - 1;
        }

        private int[] Params(out bool privateMode) {
            string text = _params.ToString();
            privateMode = text.StartsWith("?", StringComparison.Ordinal);
            if (privateMode || text.StartsWith(">", StringComparison.Ordinal) || text.StartsWith("=", StringComparison.Ordinal)) {
                text = text.Substring(1);
            }
            if (text.Length == 0) {
                return Array.Empty<int>();
            }
            string[] parts = text.Split(';');
            int[] values = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++) {
                string part = parts[i];
                int colon = part.IndexOf(':');
                if (colon >= 0) {
                    part = part.Substring(0, colon);
                }
                values[i] = int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
            }
            return values;
        }

        private static int Arg(int[] p, int index, int fallback) => index < p.Length && p[index] > 0 ? p[index] : fallback;

        private void Csi(char final) {
            int[] p = Params(out bool priv);
            if (_intermediate != '\0') {
                return; // cursor style (SP q) and similar
            }
            CellBuffer screen = Current;
            switch (final) {
                case 'm':
                    if (!priv) {
                        Sgr(p);
                    }
                    return;
                case 'H':
                case 'f':
                    PositionRow(Arg(p, 0, 1));
                    _x = Clamp(Arg(p, 1, 1) - 1, 0, Width - 1);
                    _pendingWrap = false;
                    return;
                case 'A':
                    _y = Math.Max(_y < _top ? 0 : _top, _y - Arg(p, 0, 1));
                    _pendingWrap = false;
                    return;
                case 'B':
                    _y = Math.Min(_y > _bottom ? Height - 1 : _bottom, _y + Math.Min(Height, Arg(p, 0, 1)));
                    _pendingWrap = false;
                    return;
                case 'C':
                    _x = Math.Min(Width - 1, _x + Math.Min(Width, Arg(p, 0, 1)));
                    _pendingWrap = false;
                    return;
                case 'D':
                    _x = Math.Max(0, _x - Arg(p, 0, 1));
                    _pendingWrap = false;
                    return;
                case 'E':
                    _y = Math.Min(_origin ? _bottom : Height - 1, _y + Math.Min(Height, Arg(p, 0, 1)));
                    _x = 0;
                    _pendingWrap = false;
                    return;
                case 'F':
                    _y = Math.Max(_origin ? _top : 0, _y - Arg(p, 0, 1));
                    _x = 0;
                    _pendingWrap = false;
                    return;
                case 'G':
                case '`':
                    _x = Clamp(Arg(p, 0, 1) - 1, 0, Width - 1);
                    _pendingWrap = false;
                    return;
                case 'd':
                    PositionRow(Arg(p, 0, 1));
                    _pendingWrap = false;
                    return;
                case 'K': {
                    int mode = p.Length > 0 ? p[0] : 0;
                    if (mode == 0) {
                        ClearRow(screen, _y, _x, Width);
                    } else if (mode == 1) {
                        ClearRow(screen, _y, 0, _x + 1);
                    } else {
                        ClearRow(screen, _y, 0, Width);
                    }
                    return;
                }
                case 'J': {
                    int mode = p.Length > 0 ? p[0] : 0;
                    if (mode == 0) {
                        ClearRow(screen, _y, _x, Width);
                        for (int y = _y + 1; y < Height; y++) {
                            ClearRow(screen, y, 0, Width);
                        }
                    } else if (mode == 1) {
                        for (int y = 0; y < _y; y++) {
                            ClearRow(screen, y, 0, Width);
                        }
                        ClearRow(screen, _y, 0, _x + 1);
                    } else {
                        for (int y = 0; y < Height; y++) {
                            ClearRow(screen, y, 0, Width);
                        }
                        if (mode == 3) {
                            _scrollback.Clear();
                        }
                    }
                    return;
                }
                case 'X': {
                    int n = Math.Min(Width, Arg(p, 0, 1));
                    ClearRow(screen, _y, _x, _x + n);
                    return;
                }
                case '@': {
                    ShiftCells(Math.Min(Width - _x, Arg(p, 0, 1)));
                    return;
                }
                case 'P': {
                    ShiftCells(-Math.Min(Width - _x, Arg(p, 0, 1)));
                    return;
                }
                case 'L': {
                    if (_y < _top || _y > _bottom) {
                        return;
                    }
                    int saveTop = _top;
                    _top = _y;
                    ScrollDown(Arg(p, 0, 1));
                    _top = saveTop;
                    return;
                }
                case 'M': {
                    if (_y < _top || _y > _bottom) {
                        return;
                    }
                    int saveTop = _top;
                    _top = _y;
                    ScrollUp(Arg(p, 0, 1), false);
                    _top = saveTop;
                    return;
                }
                case 'S':
                    ScrollUp(Arg(p, 0, 1));
                    return;
                case 'T':
                    ScrollDown(Arg(p, 0, 1));
                    return;
                case 'r':
                    _top = Clamp(Arg(p, 0, 1) - 1, 0, Height - 1);
                    _bottom = Clamp(Arg(p, 1, Height) - 1, 0, Height - 1);
                    if (_bottom <= _top) {
                        _top = 0;
                        _bottom = Height - 1;
                    }
                    _x = 0;
                    _y = _origin ? _top : 0;
                    _pendingWrap = false;
                    return;
                case 's':
                    _savedX = _x;
                    _savedY = _y;
                    _savedWrap = _pendingWrap;
                    _savedOrigin = _origin;
                    return;
                case 'u':
                    _x = _savedX;
                    _y = _savedY;
                    _pendingWrap = _savedWrap;
                    _origin = _savedOrigin;
                    return;
                case 'h':
                case 'l':
                    if (priv) {
                        Mode(p, final == 'h');
                    }
                    return;
            }
        }

        private void PositionRow(int row) {
            _y = (_origin ? _top : 0) + Clamp(row - 1, 0, _origin ? _bottom - _top : Height - 1);
        }

        private void Mode(int[] p, bool set) {
            foreach (int mode in p) {
                switch (mode) {
                    case 6:
                        _origin = set;
                        _x = 0;
                        _y = set ? _top : 0;
                        _pendingWrap = false;
                        break;
                    case 2026:
                        bool completed = SynchronizedOutput && !set;
                        SynchronizedOutput = set;
                        if (completed) { SynchronizedFrameCompleted?.Invoke(); }
                        break;
                    case 7:
                        _autowrap = set;
                        break;
                    case 47:
                    case 1047:
                    case 1049:
                        if (set && !_onAlternate) {
                            if (mode == 1049) {
                                _mainX = _x;
                                _mainY = _y;
                                _mainWrap = _pendingWrap;
                                _mainOrigin = _origin;
                                _pendingWrap = false;
                            }
                            _onAlternate = true;
                            _alternate = new CellBuffer(Width, Height);
                        } else if (!set && _onAlternate) {
                            _onAlternate = false;
                            if (mode == 1049) {
                                _x = _mainX;
                                _y = _mainY;
                                _pendingWrap = _mainWrap;
                                _origin = _mainOrigin;
                            }
                        }
                        break;
                }
            }
        }

        private void Osc(string text) {
            // OSC 8 ; params ; URI
            if (text.StartsWith("8;", StringComparison.Ordinal)) {
                int second = text.IndexOf(';', 2);
                string uri = second >= 0 ? text.Substring(second + 1) : string.Empty;
                _link = uri.Length == 0 ? null : uri;
            }
        }

        private void Sgr(int[] p) {
            if (p.Length == 0) {
                _style = Style.Plain;
                return;
            }
            for (int i = 0; i < p.Length; i++) {
                int code = p[i];
                switch (code) {
                    case 0: _style = Style.Plain; break;
                    case 1: _style = _style.With(TextDecoration.Bold); break;
                    case 2: _style = _style.With(TextDecoration.Dim); break;
                    case 3: _style = _style.With(TextDecoration.Italic); break;
                    case 4: _style = _style.With(TextDecoration.Underline); break;
                    case 7: _style = _style.With(TextDecoration.Reverse); break;
                    case 9: _style = _style.With(TextDecoration.Strikethrough); break;
                    case 21:
                    case 22: _style = _style.Without(TextDecoration.Bold | TextDecoration.Dim); break;
                    case 23: _style = _style.Without(TextDecoration.Italic); break;
                    case 24: _style = _style.Without(TextDecoration.Underline); break;
                    case 27: _style = _style.Without(TextDecoration.Reverse); break;
                    case 29: _style = _style.Without(TextDecoration.Strikethrough); break;
                    case 39: _style = _style.WithColor(Color.Default); break;
                    case 49: _style = _style.WithBackgroundColor(Color.Default); break;
                    case 38:
                    case 48: {
                        if (Extended(p, ref i, out Color color)) {
                            _style = code == 38 ? _style.WithColor(color) : _style.WithBackgroundColor(color);
                        }
                        break;
                    }
                    default:
                        if (code >= 30 && code <= 37) {
                            _style = _style.WithColor(Color.FromAnsi16(code - 30));
                        } else if (code >= 40 && code <= 47) {
                            _style = _style.WithBackgroundColor(Color.FromAnsi16(code - 40));
                        } else if (code >= 90 && code <= 97) {
                            _style = _style.WithColor(Color.FromAnsi16(code - 90 + 8));
                        } else if (code >= 100 && code <= 107) {
                            _style = _style.WithBackgroundColor(Color.FromAnsi16(code - 100 + 8));
                        }
                        break;
                }
            }
        }

        private static bool Extended(int[] p, ref int i, out Color color) {
            color = Color.Default;
            if (i + 1 >= p.Length) {
                return false;
            }
            if (p[i + 1] == 5 && i + 2 < p.Length) {
                int index = p[i + 2];
                i += 2;
                if (index < 0 || index > 255) { return false; }
                color = index < 16 ? Color.FromAnsi16(index) : Color.FromAnsi256(index);
                return true;
            }
            if (p[i + 1] == 2 && i + 4 < p.Length) {
                byte r = (byte)Clamp(p[i + 2], 0, 255), g = (byte)Clamp(p[i + 3], 0, 255), b = (byte)Clamp(p[i + 4], 0, 255);
                i += 4;
                color = Color.FromRgb(r, g, b);
                return true;
            }
            i = p.Length;
            return false;
        }

        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    }
}
