using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A table with content-sized columns. Identifiers get the width of their longest value; a flexible text column takes
    /// the remaining width when the table expands; numeric columns (and their headers) are right-aligned; long values wrap
    /// before anything is truncated, and headers are never truncated while there is spare width.
    /// </summary>
    public sealed partial class Table : IWidget {
        private readonly List<TableColumn> _columns = new List<TableColumn>();
        private readonly List<TableCell[]> _rows = new List<TableCell[]>();
        private TableCell[]? _footer;

        /// <summary>The columns.</summary>
        public IReadOnlyList<TableColumn> Columns => _columns;

        /// <summary>The rows.</summary>
        public IReadOnlyList<TableCell[]> Rows => _rows;

        /// <summary>A title shown above the table (markup).</summary>
        public string? Title { get; set; }

        /// <summary>A caption shown below the table (markup, muted).</summary>
        public string? Caption { get; set; }

        /// <summary>The border style (default rounded).</summary>
        public TableBorder Border { get; set; } = TableBorder.Rounded;

        /// <summary>Whether the header row is shown (default true).</summary>
        public bool ShowHeader { get; set; } = true;

        /// <summary>
        /// Whether a separator row is drawn under the header (default true). When false the header is underlined instead,
        /// so data starts on the row directly below it.
        /// </summary>
        public bool HeaderSeparator { get; set; } = true;

        /// <summary>Whether the table fills the available width (spare width goes to the flexible column).</summary>
        public bool Expand { get; set; }

        /// <summary>Adds a column.</summary>
        /// <param name="header">The header (markup).</param>
        /// <param name="alignment">Alignment, or <c>null</c> for automatic.</param>
        /// <returns>This instance.</returns>
        public Table AddColumn(string header, HorizontalAlignment? alignment = null) {
            _columns.Add(new TableColumn(header) { Alignment = alignment });
            return this;
        }

        /// <summary>Adds a column definition.</summary>
        /// <param name="column">The column.</param>
        /// <returns>This instance.</returns>
        public Table AddColumn(TableColumn column) {
            _columns.Add(column ?? throw new ArgumentNullException(nameof(column)));
            return this;
        }

        /// <summary>Adds a row of cells (strings convert to markup cells). Missing cells are blank.</summary>
        /// <param name="cells">The cells.</param>
        /// <returns>This instance.</returns>
        public Table AddRow(params TableCell[] cells) {
            _rows.Add(Normalize(cells));
            return this;
        }

        /// <summary>Adds a row of plain text values (brackets are shown literally).</summary>
        /// <param name="values">The values.</param>
        /// <returns>This instance.</returns>
        public Table AddPlainRow(params string?[] values) {
            TableCell[] cells = new TableCell[values?.Length ?? 0];
            for (int i = 0; i < cells.Length; i++) {
                cells[i] = new TableCell(Markup.Escape(values![i] ?? string.Empty));
            }
            _rows.Add(Normalize(cells));
            return this;
        }

        /// <summary>Sets a footer row (totals), separated by a line.</summary>
        /// <param name="cells">The cells.</param>
        /// <returns>This instance.</returns>
        public Table SetFooter(params TableCell[] cells) {
            _footer = Normalize(cells);
            return this;
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            TableLayout layout = ComputeLayout(constraints.MaxWidth, context);
            int height = layout.TotalHeight;
            return new Size(Math.Min(layout.TotalWidth, constraints.MaxWidth), constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            TableLayout layout = ComputeLayout(region.Width, context);
            Style border = context.Style(ThemeToken.Border);
            bool boxed = IsBoxed;
            BoxGlyphs box = context.Glyphs.GetBox(BoxStyleFor(Border));
            int y = 0;
            if (!string.IsNullOrEmpty(Title)) {
                StyledLine title = TextWrapper.Truncate(context.Markup(Title!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Title)), region.Width, context.Ellipsis);
                region.Write(boxed ? 1 : 0, y++, title);
            }
            if (boxed) {
                DrawRule(region, y++, layout, box.TopLeft, box.TopJoint, box.TopRight, box.Horizontal, border);
            }
            if (ShowHeader) {
                y = DrawRow(region, y, layout, layout.HeaderLines, layout.HeaderHeight, true, box, border, context);
                if (!HeaderSeparator) {
                    // The underlined header carries the separation.
                } else if (boxed) {
                    DrawRule(region, y++, layout, box.LeftJoint, box.Cross, box.RightJoint, box.Horizontal, border);
                } else if (Border == TableBorder.Minimal) {
                    DrawRule(region, y++, layout, string.Empty, " ", string.Empty, context.Glyphs.Get(Glyph.RuleLine), border);
                }
            }
            for (int r = 0; r < layout.RowLines.Count; r++) {
                y = DrawRow(region, y, layout, layout.RowLines[r], layout.RowHeights[r], false, box, border, context);
            }
            if (layout.FooterLines != null) {
                if (boxed) {
                    DrawRule(region, y++, layout, box.LeftJoint, box.Cross, box.RightJoint, box.Horizontal, border);
                } else if (Border == TableBorder.Minimal) {
                    DrawRule(region, y++, layout, string.Empty, " ", string.Empty, context.Glyphs.Get(Glyph.RuleLine), border);
                }
                y = DrawRow(region, y, layout, layout.FooterLines, layout.FooterHeight, false, box, border, context);
            }
            if (boxed) {
                DrawRule(region, y++, layout, box.BottomLeft, box.BottomJoint, box.BottomRight, box.Horizontal, border);
            }
            if (!string.IsNullOrEmpty(Caption)) {
                StyledLine caption = TextWrapper.Truncate(context.Markup(Caption!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted)), region.Width, context.Ellipsis);
                region.Write(boxed ? 1 : 0, y, caption);
            }
        }

        private bool IsBoxed => Border == TableBorder.Rounded || Border == TableBorder.Square || Border == TableBorder.Double;

        private static BoxStyle BoxStyleFor(TableBorder border) {
            switch (border) {
                case TableBorder.Square:
                    return BoxStyle.Single;
                case TableBorder.Double:
                    return BoxStyle.Double;
                default:
                    return BoxStyle.Rounded;
            }
        }

        private TableCell[] Normalize(TableCell[]? cells) {
            int count = Math.Max(_columns.Count, cells?.Length ?? 0);
            TableCell[] result = new TableCell[count];
            for (int i = 0; i < count; i++) {
                result[i] = cells != null && i < cells.Length && cells[i] != null ? cells[i] : new TableCell(string.Empty);
            }
            return result;
        }

        private void DrawRule(Region region, int y, TableLayout layout, string left, string joint, string right, string line, Style style) {
            if (y >= region.Height) {
                return;
            }
            int x = 0;
            if (left.Length > 0) {
                region.Put(x++, y, left, style);
            }
            for (int c = 0; c < layout.Widths.Length; c++) {
                int span = layout.Widths[c] + (IsBoxed ? 2 : 0);
                region.HorizontalLine(x, y, span, line, style);
                x += span;
                if (c < layout.Widths.Length - 1) {
                    if (IsBoxed) {
                        region.Put(x++, y, joint, style);
                    } else {
                        x += layout.Gap;
                    }
                }
            }
            if (right.Length > 0) {
                region.Put(x, y, right, style);
            }
        }

        private int DrawRow(Region region, int y, TableLayout layout, List<StyledLine>[] cells, int height, bool header, BoxGlyphs box, Style border, RenderContext context) {
            for (int line = 0; line < height && y < region.Height; line++, y++) {
                int x = 0;
                if (IsBoxed) {
                    region.Put(x++, y, box.Vertical, border);
                }
                for (int c = 0; c < layout.Widths.Length; c++) {
                    int width = layout.Widths[c];
                    if (IsBoxed) {
                        x++;
                    }
                    if (line < cells[c].Count) {
                        StyledLine text = cells[c][line];
                        int offset = 0;
                        HorizontalAlignment alignment = layout.Alignments[c];
                        if (alignment == HorizontalAlignment.Right) {
                            offset = Math.Max(0, width - text.Width);
                        } else if (alignment == HorizontalAlignment.Center) {
                            offset = Math.Max(0, (width - text.Width) / 2);
                        }
                        region.Write(x + offset, y, text, width - offset);
                    }
                    x += width;
                    if (IsBoxed) {
                        x++;
                        region.Put(x++, y, box.Vertical, border);
                    } else if (c < layout.Widths.Length - 1) {
                        x += layout.Gap;
                    }
                }
            }
            return y;
        }
    }
}
