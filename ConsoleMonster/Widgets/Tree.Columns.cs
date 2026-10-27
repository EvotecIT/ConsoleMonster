using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Tree {
        /// <summary>
        /// Aligned value columns, keyed by <see cref="TableColumn.Id"/> in <see cref="TreeNode.Values"/>.
        /// Default alignment is right. Below 120 cells values become one muted note. Lower priorities drop first;
        /// below 60 cells all values drop.
        /// With columns the tree uses single-line labels, regardless of <see cref="WrapLabels"/>.
        /// </summary>
        public IList<TableColumn> ValueColumns { get; } = new List<TableColumn>();

        /// <summary>Show headings above aligned value columns (default true).</summary>
        public bool ShowValueHeaders { get; set; } = true;

        private List<StyledLine> ColumnLines(int width, RenderContext context) {
            List<TreeRow> rows = Flatten(context);
            PresenterContext presenter = Presentation.CreateContext(context);
            int[] widths = new int[ValueColumns.Count];
            int labelDesired = 0;
            List<int> visible = new List<int>();
            for (int c = 0; c < ValueColumns.Count; c++) {
                TableColumn column = ValueColumns[c];
                int desired = ShowValueHeaders ? context.Markup(column.Header).Width : 0;
                foreach (TreeRow row in rows) desired = Math.Max(desired, ColumnValue(row.Node, column, presenter).Width);
                widths[c] = Math.Max(1, column.Width ?? Math.Max(column.MinWidth ?? 1, Math.Min(column.MaxWidth ?? int.MaxValue, desired)));
                visible.Add(c);
            }
            foreach (TreeRow row in rows) labelDesired = Math.Max(labelDesired, row.Width(context) + ValueWidth(row.Node, presenter));
            while (visible.Count > 0 && (width < 60 || ColumnsWidth(visible, widths) + Math.Min(labelDesired, 28) > width)) {
                int victim = visible[0];
                foreach (int c in visible) if (ValueColumns[c].Priority <= ValueColumns[victim].Priority) victim = c;
                visible.Remove(victim);
            }
            int labelWidth = Math.Max(0, width - ColumnsWidth(visible, widths));
            bool notes = width < 120;
            List<StyledLine> lines = new List<StyledLine>();
            if (ShowValueHeaders && visible.Count > 0 && !notes) {
                List<TextSpan> header = new List<TextSpan> { new TextSpan(new string(' ', labelWidth)) };
                foreach (int c in visible) AppendColumn(header, context.Markup(ValueColumns[c].Header).Lines[0].WithBaseStyle(context.Style(ThemeToken.Header)), widths[c], ValueColumns[c].Alignment ?? HorizontalAlignment.Right, context);
                lines.Add(new StyledLine(header));
            }
            foreach (TreeRow row in rows) {
                TreeNode node = row.Node;
                List<TextSpan> body = new List<TextSpan>();
                // Keep reference information next to the outcome, ahead of a potentially long label.
                string? note = width >= 60 ? row.DetailFor(context) : null;
                List<TextSpan> tail = new List<TextSpan>();
                if (!string.IsNullOrEmpty(note)) tail.Add(new TextSpan("  " + note, context.Style(ThemeToken.Muted)));
                if (node.Value != null && width >= 60) {
                    tail.Add(new TextSpan("  "));
                    tail.AddRange(CellRendering.Content(node.Value, node.Presenter, null, presenter).Lines[0]);
                }
                if (notes) foreach (int c in visible) {
                    tail.Add(new TextSpan("  " + ColumnValue(node, ValueColumns[c], presenter).PlainText, context.Style(ThemeToken.Muted)));
                }
                StyledLine suffix = TextWrapper.Truncate(new StyledLine(tail), Math.Max(0, notes ? width - Math.Min(labelDesired, 28) : labelWidth / 2), context.Ellipsis);
                List<TextSpan> lead = new List<TextSpan> { new TextSpan(row.Prefix, context.Style(ThemeToken.Border)) };
                if (node.Status.HasValue) lead.Add(new TextSpan(context.Glyphs.StatusMarker(node.Status.Value) + " ", context.Style(node.Status.Value)));
                string markers = row.MarkersFor(context);
                if (markers.Length > 0) lead.Add(new TextSpan(markers + " ", context.Style(ThemeToken.Muted)));
                lead.AddRange(context.Markup(node.LabelFor(context)).Lines[0]);
                body.AddRange(TextWrapper.Truncate(new StyledLine(lead), Math.Max(0, (notes ? width : labelWidth) - suffix.Width), context.Ellipsis));
                body.AddRange(suffix);
                if (!notes) {
                    int used = new StyledLine(body).Width;
                    body.Add(new TextSpan(new string(' ', Math.Max(0, labelWidth - used))));
                    foreach (int c in visible) AppendColumn(body, ColumnValue(node, ValueColumns[c], presenter), widths[c], ValueColumns[c].Alignment ?? HorizontalAlignment.Right, context);
                }
                lines.Add(new StyledLine(body));
            }
            if (visible.Count < ValueColumns.Count) {
                List<string> hidden = new List<string>();
                for (int c = 0; c < ValueColumns.Count; c++) if (!visible.Contains(c)) hidden.Add(Markup.Strip(ValueColumns[c].Header));
                WidgetLines.Wrap(lines, StyledLine.From("+" + hidden.Count + " columns (" + string.Join(", ", hidden) + ")", context.Style(ThemeToken.Muted)), width);
            }
            return lines;
        }

        private static int ColumnsWidth(List<int> visible, int[] widths) {
            int sum = 0;
            foreach (int c in visible) sum += widths[c] + 2;
            return sum;
        }

        private static StyledLine ColumnValue(TreeNode node, TableColumn column, PresenterContext presenter) =>
            node.Values.TryGetValue(column.Id, out TableCell? value)
                ? CellRendering.Content(value, column.Presenter, (IReadOnlyList<RoleRule>)column.RoleRules, presenter).Lines[0]
                : StyledLine.Empty;

        private static void AppendColumn(List<TextSpan> spans, StyledLine value, int width, HorizontalAlignment alignment, RenderContext context) {
            StyledLine fitted = TextWrapper.Truncate(value, width, context.Ellipsis);
            int gap = Math.Max(0, width - fitted.Width);
            spans.Add(new TextSpan("  " + (alignment == HorizontalAlignment.Right ? new string(' ', gap) : string.Empty)));
            spans.AddRange(fitted);
            if (alignment != HorizontalAlignment.Right) spans.Add(new TextSpan(new string(' ', gap)));
        }
    }
}
