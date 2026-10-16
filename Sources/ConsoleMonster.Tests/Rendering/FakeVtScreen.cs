using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Text;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>
    /// A minimal VT screen for tests: interprets CUP, CHA, ED 2 and CR/LF, ignores SGR and private modes, and advances the
    /// cursor by its own width function. Giving it widths that disagree with the library's measurement simulates a
    /// terminal that draws emoji or ambiguous symbols differently.
    /// </summary>
    internal sealed class FakeVtScreen {
        private readonly string[,] _cells;
        private readonly Func<string, int> _width;
        private int _x;
        private int _y;

        internal FakeVtScreen(int width, int height, Func<string, int> glyphWidth) {
            Width = width;
            Height = height;
            _cells = new string[width, height];
            _width = glyphWidth;
            Clear();
        }

        internal int Width { get; }

        internal int Height { get; }

        internal void Feed(string stream) {
            int i = 0;
            while (i < stream.Length) {
                char c = stream[i];
                if (c == '\u001b' && i + 1 < stream.Length && stream[i + 1] == '[') {
                    int end = i + 2;
                    while (end < stream.Length && (stream[end] < '@' || stream[end] > '~')) {
                        end++;
                    }
                    Csi(stream.Substring(i + 2, end - i - 2), stream[end]);
                    i = end + 1;
                    continue;
                }
                if (c == '\r') {
                    _x = 0;
                    i++;
                    continue;
                }
                if (c == '\n') {
                    _y = Math.Min(Height - 1, _y + 1);
                    i++;
                    continue;
                }
                // Take one grapheme as the library segments it, then draw it with this screen's own width.
                string rest = stream.Substring(i);
                int next = rest.IndexOf('\u001b');
                string text = next < 0 ? rest : rest.Substring(0, next);
                int crlf = text.IndexOfAny(new[] { '\r', '\n' });
                if (crlf >= 0) {
                    text = text.Substring(0, crlf);
                }
                foreach (TextCell cell in DisplayWidth.Cells(text)) {
                    Put(cell.Glyph);
                }
                i += text.Length;
            }
        }

        internal string CellAt(int x, int y) => _cells[x, y] ?? string.Empty;

        /// <summary>
        /// One character per screen cell, so golden files keep columns honest in any viewer: ASCII and box drawing as
        /// drawn, any other glyph as <c>E</c> followed by <c>~</c> for each extra cell the screen gave it.
        /// </summary>
        internal string Grid(int y) {
            StringBuilder row = new StringBuilder();
            for (int x = 0; x < Width; x++) {
                string cell = _cells[x, y] ?? " ";
                if (cell.Length == 0) {
                    row.Append('~');
                } else if (cell.Length == 1 && (cell[0] < 0x7F || (cell[0] >= 0x2500 && cell[0] <= 0x257F))) {
                    row.Append(cell);
                } else {
                    row.Append('E');
                }
            }
            return row.ToString().TrimEnd();
        }

        internal string Row(int y) {
            StringBuilder row = new StringBuilder();
            for (int x = 0; x < Width; x++) {
                if (_cells[x, y] != null) {
                    row.Append(_cells[x, y]);
                }
            }
            return row.ToString().TrimEnd();
        }

        /// <summary>The screen column where <paramref name="text"/> starts on a row, counting cells, or -1.</summary>
        internal int ColumnOf(int y, string text) {
            for (int x = 0; x < Width; x++) {
                StringBuilder probe = new StringBuilder();
                for (int k = x; k < Width && probe.Length < text.Length; k++) {
                    if (_cells[k, y] != null) {
                        probe.Append(_cells[k, y]);
                    }
                }
                if (probe.ToString().StartsWith(text, StringComparison.Ordinal) && _cells[x, y] != null && _cells[x, y]!.Length > 0) {
                    return x;
                }
            }
            return -1;
        }

        private void Put(string glyph) {
            int w = Math.Max(1, _width(glyph));
            for (int k = 0; k < w; k++) {
                ClearWide(_x + k);
            }
            if (_x < Width) {
                _cells[_x, _y] = glyph;
            }
            for (int k = 1; k < w && _x + k < Width; k++) {
                _cells[_x + k, _y] = string.Empty;
            }
            _x = Math.Min(Width, _x + w);
        }

        // Writing into either half of a wide glyph erases the whole glyph, as real terminals do.
        private void ClearWide(int x) {
            if (x >= Width) {
                return;
            }
            if (_cells[x, _y] == string.Empty) {
                int start = x;
                while (start > 0 && _cells[start, _y] == string.Empty) {
                    start--;
                }
                for (int k = start; k < Width && (k == start || _cells[k, _y] == string.Empty); k++) {
                    _cells[k, _y] = " ";
                }
            } else if (_cells[x, _y] != null && x + 1 < Width && _cells[x + 1, _y] == string.Empty) {
                for (int k = x; k < Width && (k == x || _cells[k, _y] == string.Empty); k++) {
                    _cells[k, _y] = " ";
                }
            }
        }

        private void Csi(string parameters, char final) {
            if (parameters.StartsWith("?", StringComparison.Ordinal)) {
                return;
            }
            string[] parts = parameters.Split(';');
            int First(int fallback) => parts.Length > 0 && int.TryParse(parts[0], out int v) && v > 0 ? v : fallback;
            int Second(int fallback) => parts.Length > 1 && int.TryParse(parts[1], out int v) && v > 0 ? v : fallback;
            switch (final) {
                case 'H':
                    _y = Math.Min(Height - 1, First(1) - 1);
                    _x = Math.Min(Width - 1, Second(1) - 1);
                    break;
                case 'G':
                    _x = Math.Min(Width - 1, First(1) - 1);
                    break;
                case 'J':
                    if (First(0) == 2) {
                        Clear();
                    }
                    break;
            }
        }

        private void Clear() {
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    _cells[x, y] = " ";
                }
            }
        }
    }
}
