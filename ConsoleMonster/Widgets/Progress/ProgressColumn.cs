using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// One column of a progress display (<see cref="Lanes.Columns"/>, <see cref="ProgressBar.Columns"/>). Built-ins come
    /// from <see cref="ProgressColumns"/>; derive from this class, or use <see cref="ProgressColumns.Custom(string, Func{ProgressRow, RenderContext, StyledLine}, int, int)"/>,
    /// for your own (an icon, result counts, a phase). The same columns render in full-screen and inline sessions and
    /// feed the compact stream summary.
    /// </summary>
    public abstract class ProgressColumn {
        /// <summary>Creates a column.</summary>
        /// <param name="name">A name for diagnostics and PowerShell (<c>Percent</c>, <c>Rate</c>...).</param>
        protected ProgressColumn(string name) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>The column name.</summary>
        public string Name { get; }

        /// <summary>The alignment of the text within the column.</summary>
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <summary>When the region is too narrow, columns with the lowest priority are dropped first.</summary>
        public int Priority { get; set; } = 50;

        /// <summary>The widest the column gets, or 0 for no limit.</summary>
        public int MaxWidth { get; set; }

        /// <summary>Whether the column takes a share of the width left over by the fixed columns (the bar, the status text).</summary>
        public virtual bool Flexible => false;

        /// <summary>The narrowest a flexible column may be before it is dropped.</summary>
        public virtual int MinWidth => 1;

        /// <summary>The width of a fixed column: the widest row (capped by <see cref="MaxWidth"/>).</summary>
        /// <param name="rows">Every row, so the width stays stable while values change.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The width in cells.</returns>
        public virtual int Measure(IReadOnlyList<ProgressRow> rows, RenderContext context) {
            int width = 0;
            foreach (ProgressRow row in rows) {
                width = Math.Max(width, Render(row, context).Width);
            }
            return MaxWidth > 0 ? Math.Min(width, MaxWidth) : width;
        }

        /// <summary>The content of one row.</summary>
        /// <param name="row">The row.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The styled text.</returns>
        public abstract StyledLine Render(ProgressRow row, RenderContext context);

        /// <summary>Draws one row into its cell range (truncated and aligned by default).</summary>
        /// <param name="region">The region.</param>
        /// <param name="x">The first column.</param>
        /// <param name="y">The row.</param>
        /// <param name="width">The width in cells.</param>
        /// <param name="row">The row.</param>
        /// <param name="context">The render context.</param>
        public virtual void Draw(Region region, int x, int y, int width, ProgressRow row, RenderContext context) {
            if (width <= 0) {
                return;
            }
            StyledLine line = TextWrapper.Truncate(Render(row, context), width, context.Ellipsis);
            int offset = Alignment == HorizontalAlignment.Right ? width - line.Width : Alignment == HorizontalAlignment.Center ? (width - line.Width) / 2 : 0;
            region.Write(x + Math.Max(0, offset), y, line);
        }

        /// <summary>The column in a compact stream summary line, or null to leave it out (the bar).</summary>
        /// <param name="row">The row.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The text, or null.</returns>
        public virtual string? Summarize(ProgressRow row, RenderContext context) {
            string text = Render(row, context).PlainText;
            return text.Length == 0 ? null : text;
        }
    }
}
