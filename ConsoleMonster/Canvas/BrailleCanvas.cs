using System;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Canvas {
    /// <summary>
    /// A canvas with 2x4 pixels per cell drawn with braille patterns (U+2800-U+28FF). Needs a Full-tier font.
    /// </summary>
    public sealed class BrailleCanvas : PixelCanvas {
        private static readonly int[,] DotBits = {
            { 0x01, 0x08 },
            { 0x02, 0x10 },
            { 0x04, 0x20 },
            { 0x40, 0x80 }
        };

        private readonly byte[] _masks;
        private readonly Color[] _colors;

        /// <summary>Creates a braille canvas.</summary>
        /// <param name="columns">Width in cells.</param>
        /// <param name="rows">Height in cells.</param>
        public BrailleCanvas(int columns, int rows) : base(columns, rows, 2, 4) {
            _masks = new byte[Columns * Rows];
            _colors = new Color[Columns * Rows];
        }

        /// <inheritdoc />
        public override void Set(int x, int y, Color color) {
            if (x < 0 || y < 0 || x >= PixelWidth || y >= PixelHeight) {
                return;
            }
            int index = ((y / 4) * Columns) + (x / 2);
            _masks[index] |= (byte)DotBits[y % 4, x % 2];
            _colors[index] = color;
        }

        /// <inheritdoc />
        public override void Clear() {
            Array.Clear(_masks, 0, _masks.Length);
            Array.Clear(_colors, 0, _colors.Length);
        }

        /// <inheritdoc />
        public override void Render(Region region, int x, int y, RenderContext context) {
            for (int row = 0; row < Rows; row++) {
                for (int col = 0; col < Columns; col++) {
                    int index = (row * Columns) + col;
                    byte mask = _masks[index];
                    if (mask == 0) {
                        continue;
                    }
                    string glyph = ((char)(0x2800 + mask)).ToString();
                    region.Put(x + col, y + row, glyph, new Style(_colors[index]).Downsample(context.Depth));
                }
            }
        }
    }
}
