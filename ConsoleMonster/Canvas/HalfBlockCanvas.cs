using System;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Canvas {
    /// <summary>
    /// A canvas with 1x2 pixels per cell drawn with half blocks (▀ ▄ █). Each pixel keeps its own colour: when the two
    /// pixels of a cell differ, the upper one is the foreground and the lower one the background. Basic-tier fonts
    /// (Consolas, Lucida Console) can show it; it is also the renderer for pixel-art logos.
    /// </summary>
    public sealed class HalfBlockCanvas : PixelCanvas {
        private readonly Color[] _pixels;
        private readonly bool[] _set;

        /// <summary>Creates a half-block canvas.</summary>
        /// <param name="columns">Width in cells.</param>
        /// <param name="rows">Height in cells.</param>
        public HalfBlockCanvas(int columns, int rows) : base(columns, rows, 1, 2) {
            _pixels = new Color[Columns * Rows * 2];
            _set = new bool[Columns * Rows * 2];
        }

        /// <inheritdoc />
        public override void Set(int x, int y, Color color) {
            if (x < 0 || y < 0 || x >= PixelWidth || y >= PixelHeight) {
                return;
            }
            int index = (y * Columns) + x;
            _pixels[index] = color;
            _set[index] = true;
        }

        /// <inheritdoc />
        public override void Clear() {
            Array.Clear(_pixels, 0, _pixels.Length);
            Array.Clear(_set, 0, _set.Length);
        }

        /// <inheritdoc />
        public override void Render(Region region, int x, int y, RenderContext context) {
            string full = context.Glyphs.Get(Glyph.BlockFull);
            string upper = context.Glyphs.Get(Glyph.HalfUpper);
            string lower = context.Glyphs.Get(Glyph.HalfLower);
            for (int row = 0; row < Rows; row++) {
                for (int col = 0; col < Columns; col++) {
                    int top = ((row * 2) * Columns) + col;
                    int bottom = (((row * 2) + 1) * Columns) + col;
                    bool hasTop = _set[top];
                    bool hasBottom = _set[bottom];
                    if (!hasTop && !hasBottom) {
                        continue;
                    }
                    Style style;
                    string glyph;
                    if (hasTop && hasBottom) {
                        if (_pixels[top].Equals(_pixels[bottom]) || !context.HasColor) {
                            glyph = full;
                            style = new Style(_pixels[top]);
                        } else {
                            glyph = upper;
                            style = new Style(_pixels[top], _pixels[bottom]);
                        }
                    } else if (hasTop) {
                        glyph = upper;
                        style = new Style(_pixels[top]);
                    } else {
                        glyph = lower;
                        style = new Style(_pixels[bottom]);
                    }
                    region.Put(x + col, y + row, glyph, style.Downsample(context.Depth));
                }
            }
        }
    }
}
