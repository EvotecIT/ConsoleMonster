using System;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Canvas {
    /// <summary>
    /// The rectangle a widget renders into: a clipped view onto a <see cref="CellBuffer"/> with local coordinates.
    /// Writes outside the region are discarded, so a widget can never draw over its neighbours.
    /// </summary>
    public sealed class Region {
        /// <summary>Creates a region covering part of a buffer.</summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="bounds">The absolute rectangle (clipped to the buffer).</param>
        public Region(CellBuffer buffer, Rect bounds) {
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            Bounds = bounds.Intersect(new Rect(0, 0, buffer.Width, buffer.Height));
        }

        /// <summary>Creates a region covering a whole buffer.</summary>
        /// <param name="buffer">The buffer.</param>
        public Region(CellBuffer buffer) : this(buffer, new Rect(0, 0, buffer?.Width ?? 0, buffer?.Height ?? 0)) {
        }

        /// <summary>The underlying buffer.</summary>
        public CellBuffer Buffer { get; }

        /// <summary>The absolute rectangle within the buffer.</summary>
        public Rect Bounds { get; }

        /// <summary>Width in cells.</summary>
        public int Width => Bounds.Width;

        /// <summary>Height in cells.</summary>
        public int Height => Bounds.Height;

        /// <summary>Returns a child region in local coordinates, clipped to this region.</summary>
        /// <param name="x">Local left column.</param>
        /// <param name="y">Local top row.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <returns>The child region.</returns>
        public Region Sub(int x, int y, int width, int height) {
            Rect rect = new Rect(Bounds.X + x, Bounds.Y + y, width, height).Intersect(Bounds);
            return new Region(Buffer, rect);
        }

        /// <summary>Places one grapheme at a local position.</summary>
        /// <param name="x">Local column.</param>
        /// <param name="y">Local row.</param>
        /// <param name="glyph">The grapheme.</param>
        /// <param name="style">The style.</param>
        /// <param name="width">Grapheme width (1 or 2), or 0 to measure.</param>
        public void Put(int x, int y, string glyph, Style style, int width = 1) {
            if (x < 0 || y < 0 || y >= Height) {
                return;
            }
            if (width <= 0) {
                width = Math.Max(1, DisplayWidth.Measure(glyph));
            }
            if (x + width > Width) {
                return;
            }
            Buffer.Set(Bounds.X + x, Bounds.Y + y, glyph, style, width);
        }

        /// <summary>Writes single-line text at a local position, clipped to the region (and to <paramref name="maxWidth"/>).</summary>
        /// <param name="x">Local column.</param>
        /// <param name="y">Local row.</param>
        /// <param name="text">The text.</param>
        /// <param name="style">The style.</param>
        /// <param name="maxWidth">Maximum cells to use, or -1 for the rest of the row.</param>
        /// <returns>The local column after the text.</returns>
        public int Write(int x, int y, string text, Style style, int maxWidth = -1) {
            if (y < 0 || y >= Height || string.IsNullOrEmpty(text)) {
                return x;
            }
            int limit = maxWidth < 0 ? Width : Math.Min(Width, x + maxWidth);
            int end = Buffer.Write(Bounds.X + x, Bounds.Y + y, text, style, Bounds.X + limit);
            return end - Bounds.X;
        }

        /// <summary>Writes a styled line at a local position, clipped to the region.</summary>
        /// <param name="x">Local column.</param>
        /// <param name="y">Local row.</param>
        /// <param name="line">The line.</param>
        /// <param name="maxWidth">Maximum cells to use, or -1 for the rest of the row.</param>
        /// <param name="baseStyle">A style layered underneath every span.</param>
        /// <returns>The local column after the text.</returns>
        public int Write(int x, int y, StyledLine line, int maxWidth = -1, Style baseStyle = default) {
            if (line == null) {
                return x;
            }
            int limit = maxWidth < 0 ? Width - x : maxWidth;
            if (line.Count == 1) {
                return Write(x, y, line[0].Text, baseStyle.Combine(line[0].Style), limit);
            }
            // Segment the whole line once so a grapheme whose parts carry different styles stays one cell.
            StyledLineMap map = new StyledLineMap(line);
            int column = x;
            for (int i = 0; i < map.Clusters.Count; i++) {
                TextCluster cluster = map.Clusters[i];
                if (cluster.Width <= 0) {
                    continue;
                }
                if (column + cluster.Width - x > limit) {
                    break;
                }
                Put(column, y, map.Text.Substring(cluster.Start, cluster.Length), baseStyle.Combine(map.StyleAt(i)), cluster.Width);
                column += cluster.Width;
            }
            return column;
        }

        /// <summary>Fills a local rectangle with a glyph.</summary>
        /// <param name="x">Local column.</param>
        /// <param name="y">Local row.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="glyph">The glyph.</param>
        /// <param name="style">The style.</param>
        public void Fill(int x, int y, int width, int height, string glyph, Style style) {
            int glyphWidth = Math.Max(1, DisplayWidth.Measure(glyph));
            for (int row = y; row < y + height; row++) {
                for (int col = x; col + glyphWidth <= x + width; col += glyphWidth) {
                    Put(col, row, glyph, style, glyphWidth);
                }
            }
        }

        /// <summary>Fills the whole region with blanks in a style (for example a header bar background).</summary>
        /// <param name="style">The style.</param>
        public void Fill(Style style) => Fill(0, 0, Width, Height, " ", style);

        /// <summary>Repeats a glyph horizontally.</summary>
        /// <param name="x">Local column.</param>
        /// <param name="y">Local row.</param>
        /// <param name="count">Number of cells.</param>
        /// <param name="glyph">The glyph.</param>
        /// <param name="style">The style.</param>
        public void HorizontalLine(int x, int y, int count, string glyph, Style style) => Fill(x, y, count, 1, glyph, style);

        /// <summary>Repeats a glyph vertically.</summary>
        /// <param name="x">Local column.</param>
        /// <param name="y">Local row.</param>
        /// <param name="count">Number of cells.</param>
        /// <param name="glyph">The glyph.</param>
        /// <param name="style">The style.</param>
        public void VerticalLine(int x, int y, int count, string glyph, Style style) => Fill(x, y, 1, count, glyph, style);

        /// <summary>Changes the style of a cell, keeping its glyph.</summary>
        /// <param name="x">Local column.</param>
        /// <param name="y">Local row.</param>
        /// <param name="style">The style.</param>
        public void SetStyle(int x, int y, Style style) {
            if (x < 0 || y < 0 || x >= Width || y >= Height) {
                return;
            }
            Buffer.SetStyle(Bounds.X + x, Bounds.Y + y, style);
        }

        /// <summary>Reads a cell at a local position.</summary>
        /// <param name="x">Local column.</param>
        /// <param name="y">Local row.</param>
        /// <returns>The cell (blank when outside).</returns>
        public Cell Get(int x, int y) {
            if (x < 0 || y < 0 || x >= Width || y >= Height) {
                return Cell.Blank;
            }
            return Buffer[Bounds.X + x, Bounds.Y + y];
        }
    }
}
