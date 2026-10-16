using System;
using System.Text;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Canvas {
    /// <summary>
    /// A grid of <see cref="Cell"/> values: the frame every widget renders into. Writing keeps wide graphemes
    /// consistent (a wide grapheme that would be split by the right edge or overwritten by half is replaced by blanks).
    /// Not thread-safe: a buffer belongs to one render pass.
    /// </summary>
    public sealed class CellBuffer {
        private Cell[] _cells;

        /// <summary>Creates a blank buffer.</summary>
        /// <param name="width">Width in cells (at least 0).</param>
        /// <param name="height">Height in cells (at least 0).</param>
        public CellBuffer(int width, int height) {
            Width = Math.Max(0, width);
            Height = Math.Max(0, height);
            _cells = new Cell[Width * Height];
            Clear();
        }

        /// <summary>Width in cells.</summary>
        public int Width { get; private set; }

        /// <summary>Height in cells.</summary>
        public int Height { get; private set; }

        /// <summary>Gets the cell at a position (blank when out of range).</summary>
        /// <param name="x">Column.</param>
        /// <param name="y">Row.</param>
        public Cell this[int x, int y] {
            get {
                if (x < 0 || y < 0 || x >= Width || y >= Height) {
                    return Cell.Blank;
                }
                return _cells[(y * Width) + x];
            }
        }

        /// <summary>Resets every cell to a blank with the given style.</summary>
        /// <param name="style">The fill style.</param>
        public void Clear(Style style = default) {
            Cell blank = style.IsPlain ? Cell.Blank : new Cell(" ", style, 1);
            for (int i = 0; i < _cells.Length; i++) {
                _cells[i] = blank;
            }
        }

        /// <summary>Resizes the buffer, clearing its content.</summary>
        /// <param name="width">New width.</param>
        /// <param name="height">New height.</param>
        public void Resize(int width, int height) {
            Width = Math.Max(0, width);
            Height = Math.Max(0, height);
            _cells = new Cell[Width * Height];
            Clear();
        }

        /// <summary>Places one grapheme. Wide graphemes that do not fit at the right edge become a blank.</summary>
        /// <param name="x">Column.</param>
        /// <param name="y">Row.</param>
        /// <param name="glyph">The grapheme.</param>
        /// <param name="style">The style.</param>
        /// <param name="width">Grapheme width (1 or 2); 0 or less measures the glyph.</param>
        public void Set(int x, int y, string glyph, Style style, int width = 1) {
            if (x < 0 || y < 0 || x >= Width || y >= Height) {
                return;
            }
            if (string.IsNullOrEmpty(glyph)) {
                glyph = " ";
            }
            if (width <= 0) {
                width = Math.Max(1, DisplayWidth.Measure(glyph));
            }
            if (width > 2) {
                width = 2;
            }
            if (width == 2 && x + 1 >= Width) {
                glyph = " ";
                width = 1;
            }
            int row = y * Width;
            ReleaseWide(x, row);
            if (width == 2) {
                ReleaseWide(x + 1, row);
                _cells[row + x] = new Cell(glyph, style, 2);
                _cells[row + x + 1] = new Cell(string.Empty, style, 0);
            } else {
                _cells[row + x] = new Cell(glyph, style, 1);
            }
        }

        /// <summary>Changes only the style of a cell, keeping its glyph.</summary>
        /// <param name="x">Column.</param>
        /// <param name="y">Row.</param>
        /// <param name="style">The new style.</param>
        public void SetStyle(int x, int y, Style style) {
            if (x < 0 || y < 0 || x >= Width || y >= Height) {
                return;
            }
            int index = (y * Width) + x;
            Cell cell = _cells[index];
            _cells[index] = new Cell(cell.Glyph, style, cell.Width);
        }

        /// <summary>Writes text from a position, clipped at <paramref name="maxX"/> (exclusive).</summary>
        /// <param name="x">Start column.</param>
        /// <param name="y">Row.</param>
        /// <param name="text">The text (single line).</param>
        /// <param name="style">The style.</param>
        /// <param name="maxX">Exclusive right clip edge; defaults to the buffer width.</param>
        /// <returns>The column after the last written cell.</returns>
        public int Write(int x, int y, string text, Style style, int maxX = int.MaxValue) {
            int limit = Math.Min(maxX, Width);
            int column = x;
            foreach (TextCell cell in DisplayWidth.Cells(text)) {
                if (column + cell.Width > limit) {
                    break;
                }
                if (column >= 0) {
                    Set(column, y, cell.Glyph, style, cell.Width);
                }
                column += cell.Width;
            }
            return column;
        }

        /// <summary>Copies a region of another buffer into this one.</summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="sourceX">Source column.</param>
        /// <param name="sourceY">Source row.</param>
        /// <param name="width">Columns to copy.</param>
        /// <param name="height">Rows to copy.</param>
        /// <param name="targetX">Target column.</param>
        /// <param name="targetY">Target row.</param>
        public void Blit(CellBuffer source, int sourceX, int sourceY, int width, int height, int targetX, int targetY) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }
            for (int row = 0; row < height; row++) {
                for (int col = 0; col < width; col++) {
                    Cell cell = source[sourceX + col, sourceY + row];
                    if (cell.IsContinuation) {
                        continue;
                    }
                    Set(targetX + col, targetY + row, cell.Glyph, cell.Style, cell.Width);
                }
            }
        }

        /// <summary>Copies another buffer of the same size into this one.</summary>
        /// <param name="source">The source buffer.</param>
        public void CopyFrom(CellBuffer source) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.Width != Width || source.Height != Height) {
                Width = source.Width;
                Height = source.Height;
                _cells = new Cell[source._cells.Length];
            }
            Array.Copy(source._cells, _cells, _cells.Length);
        }

        /// <summary>Returns a row as plain text (continuation cells skipped, trailing spaces kept).</summary>
        /// <param name="y">Row.</param>
        /// <returns>The row text.</returns>
        public string GetRowText(int y) {
            StringBuilder sb = new StringBuilder(Width);
            for (int x = 0; x < Width; x++) {
                Cell cell = this[x, y];
                if (!cell.IsContinuation) {
                    sb.Append(cell.Glyph);
                }
            }
            return sb.ToString();
        }

        /// <summary>Returns the whole buffer as plain text lines with trailing spaces trimmed (used by snapshot tests).</summary>
        /// <returns>The text, rows separated by <c>\n</c>.</returns>
        public string ToPlainText() {
            StringBuilder sb = new StringBuilder();
            for (int y = 0; y < Height; y++) {
                if (y > 0) {
                    sb.Append('\n');
                }
                sb.Append(GetRowText(y).TrimEnd(' '));
            }
            return sb.ToString();
        }

        private void ReleaseWide(int x, int row) {
            if (x < 0 || x >= Width) {
                return;
            }
            Cell existing = _cells[row + x];
            if (existing.Width == 0 && x > 0) {
                Cell head = _cells[row + x - 1];
                _cells[row + x - 1] = new Cell(" ", head.Style, 1);
            } else if (existing.Width == 2 && x + 1 < Width) {
                Cell tail = _cells[row + x + 1];
                _cells[row + x + 1] = new Cell(" ", tail.Style, 1);
            }
        }
    }
}
