using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// Lays children out in equal-width columns, row by row. Cells in a row share the row height. When columns would
    /// become narrower than <see cref="MinColumnWidth"/>, the grid uses fewer columns. In a live frame with spare height,
    /// rows grow equally so the grid fills its region.
    /// </summary>
    public sealed class Grid : IContainer {
        private readonly List<IWidget> _children = new List<IWidget>();

        /// <summary>Creates a grid.</summary>
        /// <param name="columns">Number of columns (at least 1).</param>
        /// <param name="children">Initial children.</param>
        public Grid(int columns, params IWidget[] children) {
            ColumnCount = Math.Max(1, columns);
            if (children != null) {
                foreach (IWidget child in children) {
                    Add(child);
                }
            }
        }

        /// <summary>The preferred number of columns.</summary>
        public int ColumnCount { get; }

        /// <summary>Blank columns between cells (default 1).</summary>
        public int ColumnGap { get; set; } = 1;

        /// <summary>Blank rows between grid rows (default 0).</summary>
        public int RowGap { get; set; }

        /// <summary>The narrowest column before the grid reduces its column count (default 24).</summary>
        public int MinColumnWidth { get; set; } = 24;

        /// <summary>Whether rows grow to fill spare height in a bounded region (default true).</summary>
        public bool FillHeight { get; set; } = true;

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => _children;

        /// <summary>Adds a cell.</summary>
        /// <param name="child">The child.</param>
        /// <returns>This instance.</returns>
        public Grid Add(IWidget child) {
            _children.Add(child ?? throw new ArgumentNullException(nameof(child)));
            return this;
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int columns = EffectiveColumns(constraints.MaxWidth);
            int[] widths = ColumnWidths(constraints.MaxWidth, columns);
            int[] heights = RowHeights(widths, columns, constraints.MaxHeight, context);
            int height = TrackAllocator.Sum(heights) + (RowGap * Math.Max(0, heights.Length - 1));
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            int columns = EffectiveColumns(region.Width);
            int[] widths = ColumnWidths(region.Width, columns);
            int[] heights = RowHeights(widths, columns, region.Height, context);
            int used = TrackAllocator.Sum(heights) + (RowGap * Math.Max(0, heights.Length - 1));
            if (FillHeight && heights.Length > 0 && used < region.Height) {
                int extra = region.Height - used;
                for (int r = 0; r < heights.Length; r++) {
                    heights[r] += extra / heights.Length + (r < extra % heights.Length ? 1 : 0);
                }
            }
            int y = 0;
            for (int r = 0; r < heights.Length; r++) {
                int x = 0;
                for (int c = 0; c < columns; c++) {
                    int index = (r * columns) + c;
                    if (index >= _children.Count) {
                        break;
                    }
                    _children[index].Render(region.Sub(x, y, widths[c], heights[r]), context);
                    x += widths[c] + ColumnGap;
                }
                y += heights[r] + RowGap;
            }
        }

        private int EffectiveColumns(int width) {
            int columns = Math.Min(ColumnCount, Math.Max(1, _children.Count));
            while (columns > 1 && (width - (ColumnGap * (columns - 1))) / columns < MinColumnWidth) {
                columns--;
            }
            return columns;
        }

        private int[] ColumnWidths(int width, int columns) {
            int[] widths = new int[columns];
            int space = Math.Max(0, width - (ColumnGap * (columns - 1)));
            for (int c = 0; c < columns; c++) {
                widths[c] = space / columns + (c < space % columns ? 1 : 0);
            }
            return widths;
        }

        private int[] RowHeights(int[] widths, int columns, int maxHeight, RenderContext context) {
            int rows = (_children.Count + columns - 1) / columns;
            int[] heights = new int[rows];
            for (int r = 0; r < rows; r++) {
                for (int c = 0; c < columns; c++) {
                    int index = (r * columns) + c;
                    if (index >= _children.Count) {
                        break;
                    }
                    heights[r] = Math.Max(heights[r], _children[index].Measure(new Constraints(widths[c], maxHeight), context).Height);
                }
            }
            return heights;
        }
    }
}
