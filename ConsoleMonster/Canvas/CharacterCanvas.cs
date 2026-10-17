using System;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Canvas {
    /// <summary>
    /// A canvas with one pixel per cell drawn with a plain character (<c>*</c> by default). The ASCII fallback for charts.
    /// </summary>
    public sealed class CharacterCanvas : PixelCanvas {
        private readonly Color[] _pixels;
        private readonly bool[] _set;

        /// <summary>Creates a character canvas.</summary>
        /// <param name="columns">Width in cells.</param>
        /// <param name="rows">Height in cells.</param>
        public CharacterCanvas(int columns, int rows) : base(columns, rows, 1, 1) {
            _pixels = new Color[Columns * Rows];
            _set = new bool[Columns * Rows];
        }

        /// <summary>The character drawn for a set pixel.</summary>
        public string Mark { get; set; } = "*";

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
            for (int row = 0; row < Rows; row++) {
                for (int col = 0; col < Columns; col++) {
                    int index = (row * Columns) + col;
                    if (_set[index]) {
                        region.Put(x + col, y + row, Mark, new Style(_pixels[index]).Downsample(context.Depth));
                    }
                }
            }
        }
    }
}
