using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Canvas {
    /// <summary>
    /// A sub-cell drawing surface: pixels are mapped onto cells using braille (2x4 per cell), half blocks (1x2 per cell)
    /// or plain characters (1x1 per cell). <see cref="Create"/> picks the densest canvas the glyph tier can show, so a
    /// chart drawn once looks right on every terminal.
    /// </summary>
    public abstract class PixelCanvas {
        /// <summary>Creates a canvas.</summary>
        /// <param name="columns">Width in cells.</param>
        /// <param name="rows">Height in cells.</param>
        /// <param name="pixelsPerColumn">Horizontal pixels per cell.</param>
        /// <param name="pixelsPerRow">Vertical pixels per cell.</param>
        protected PixelCanvas(int columns, int rows, int pixelsPerColumn, int pixelsPerRow) {
            Columns = Math.Max(0, columns);
            Rows = Math.Max(0, rows);
            PixelsPerColumn = pixelsPerColumn;
            PixelsPerRow = pixelsPerRow;
        }

        /// <summary>Width in cells.</summary>
        public int Columns { get; }

        /// <summary>Height in cells.</summary>
        public int Rows { get; }

        /// <summary>Horizontal pixels per cell.</summary>
        public int PixelsPerColumn { get; }

        /// <summary>Vertical pixels per cell.</summary>
        public int PixelsPerRow { get; }

        /// <summary>Width in pixels.</summary>
        public int PixelWidth => Columns * PixelsPerColumn;

        /// <summary>Height in pixels.</summary>
        public int PixelHeight => Rows * PixelsPerRow;

        /// <summary>Creates the densest canvas a glyph tier can show.</summary>
        /// <param name="tier">The glyph tier.</param>
        /// <param name="columns">Width in cells.</param>
        /// <param name="rows">Height in cells.</param>
        /// <returns>A braille canvas (Full), half-block canvas (Basic) or character canvas (Ascii).</returns>
        public static PixelCanvas Create(GlyphTier tier, int columns, int rows) {
            switch (tier) {
                case GlyphTier.Full:
                    return new BrailleCanvas(columns, rows);
                case GlyphTier.Basic:
                    return new HalfBlockCanvas(columns, rows);
                default:
                    return new CharacterCanvas(columns, rows);
            }
        }

        /// <summary>Sets a pixel. Out-of-range pixels are ignored.</summary>
        /// <param name="x">Pixel column (0 = left).</param>
        /// <param name="y">Pixel row (0 = top).</param>
        /// <param name="color">The colour; a cell shows the colour of the last pixel set in it.</param>
        public abstract void Set(int x, int y, Color color);

        /// <summary>Clears all pixels.</summary>
        public abstract void Clear();

        /// <summary>Renders the canvas into a region at a local position.</summary>
        /// <param name="region">The region.</param>
        /// <param name="x">Local column.</param>
        /// <param name="y">Local row.</param>
        /// <param name="context">The render context.</param>
        public abstract void Render(Region region, int x, int y, RenderContext context);

        /// <summary>Draws a line between two pixels (Bresenham).</summary>
        /// <param name="x0">Start column.</param>
        /// <param name="y0">Start row.</param>
        /// <param name="x1">End column.</param>
        /// <param name="y1">End row.</param>
        /// <param name="color">The colour.</param>
        public void Line(int x0, int y0, int x1, int y1, Color color) {
            int dx = Math.Abs(x1 - x0);
            int dy = -Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            int guard = dx - dy + 2;
            while (guard-- > 0) {
                Set(x0, y0, color);
                if (x0 == x1 && y0 == y1) {
                    break;
                }
                int e2 = 2 * err;
                if (e2 >= dy) {
                    err += dy;
                    x0 += sx;
                }
                if (e2 <= dx) {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        /// <summary>Fills a vertical span of pixels in one column (bars, areas).</summary>
        /// <param name="x">Pixel column.</param>
        /// <param name="yTop">Top pixel row.</param>
        /// <param name="yBottom">Bottom pixel row (inclusive).</param>
        /// <param name="color">The colour.</param>
        public void VerticalSpan(int x, int yTop, int yBottom, Color color) {
            if (yTop > yBottom) {
                int t = yTop;
                yTop = yBottom;
                yBottom = t;
            }
            for (int y = yTop; y <= yBottom; y++) {
                Set(x, y, color);
            }
        }
    }
}
