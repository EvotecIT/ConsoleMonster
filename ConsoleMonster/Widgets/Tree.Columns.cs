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
        /// Default alignment is right. Wide layouts reserve at least 40% for labels and shrink value columns before
        /// dropping them. Below 120 cells fitting detail precedes value notes. Lower priorities drop first;
        /// labels always receive space first.
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
            int labelReserve = width >= 120 ? (int)Math.Ceiling(width * 0.4) : Math.Min(labelDesired, 28);
            if (width >= 120) ShrinkColumns(visible, widths, Math.Max(0, width - labelReserve));
            while (width >= 60 && visible.Count > 0 && ColumnsWidth(visible, widths) + labelReserve > width) {
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
                int primaryWidth = notes ? width : labelWidth;
                body.AddRange(Primary(row, primaryWidth, notes, context));
                if (notes) {
                    AppendNote(body, ReferenceWords(row), primaryWidth, context);
                    AppendNote(body, row.DetailFor(context), primaryWidth, context);
                    List<int> ordered = new List<int>(visible);
                    ordered.Sort((a, b) => {
                        int priority = ValueColumns[b].Priority.CompareTo(ValueColumns[a].Priority);
                        return priority != 0 ? priority : a.CompareTo(b);
                    });
                    foreach (int c in ordered) {
                        StyledLine value = ColumnValue(node, ValueColumns[c], presenter);
                        if (ValueColumns[c].MaxWidth is int maximum) value = FitNote(value, Math.Max(0, maximum), context);
                        AppendNote(body, value.PlainText, primaryWidth, context);
                    }
                }
                if (node.Value != null && width >= 60) AppendNote(body, CellRendering.Content(node.Value, node.Presenter, null, presenter).Lines[0].PlainText, primaryWidth, context);
                if (!notes) AppendNote(body, row.DetailFor(context), primaryWidth, context);
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

        private void ShrinkColumns(List<int> visible, int[] widths, int budget) {
            while (ColumnsWidth(visible, widths) > budget) {
                int victim = -1;
                int slack = 0;
                foreach (int c in visible) {
                    int minimum = Math.Max(1, ValueColumns[c].Width ?? ValueColumns[c].MinWidth ?? 1);
                    if (widths[c] - minimum > slack) { victim = c; slack = widths[c] - minimum; }
                }
                if (victim < 0) break;
                widths[victim] -= Math.Min(slack, ColumnsWidth(visible, widths) - budget);
            }
        }

        private StyledLine Primary(TreeRow row, int width, bool narrow, RenderContext context) {
            TreeNode node = row.Node;
            List<TextSpan> markers = new List<TextSpan>();
            string? marker = node.MarkerFor(context, compact: narrow);
            if (!string.IsNullOrEmpty(marker)) markers.Add(new TextSpan(marker + " ", node.MarkerStyle(context)));
            string reference = narrow ? ReferenceGlyphs(row.Reference, context) : row.MarkersFor(context);
            if (reference.Length > 0) markers.Add(new TextSpan(reference + " ", context.Style(ThemeToken.Muted)));
            StyledLine symbols = new StyledLine(markers);
            StyledLine label = context.Markup(node.LabelFor(context)).Lines[0];
            int guideWidth = Math.Max(0, width - symbols.Width - Math.Min(label.Width, Math.Max(1, width / 2)));
            StyledLine guide = TextWrapper.Truncate(StyledLine.From(row.Prefix, context.Style(ThemeToken.Border)), guideWidth, string.Empty);
            List<TextSpan> spans = new List<TextSpan>(guide);
            spans.AddRange(symbols);
            spans.AddRange(FitLabel(label, Math.Max(0, width - guide.Width - symbols.Width), context));
            return new StyledLine(spans);
        }

        private StyledLine FitLabel(StyledLine label, int width, RenderContext context) => TruncateLabelsInMiddle
            ? TextWrapper.TruncateMiddle(label, width, context.Ellipsis) : TextWrapper.Truncate(label, width, context.Ellipsis);

        private StyledLine FitNote(StyledLine note, int width, RenderContext context) => TruncateNotesInMiddle
            ? TextWrapper.TruncateMiddle(note, width, context.Ellipsis) : TextWrapper.Truncate(note, width, context.Ellipsis);

        private void AppendNote(List<TextSpan> spans, string? text, int width, RenderContext context) {
            if (string.IsNullOrEmpty(text)) return;
            int remaining = width - new StyledLine(spans).Width - 2;
            if (remaining < 1) return;
            spans.Add(new TextSpan("  "));
            spans.AddRange(FitNote(StyledLine.From(text!, context.Style(ThemeToken.Muted)), remaining, context));
        }

        private static string ReferenceGlyphs(TreeReferenceKind reference, RenderContext context) {
            bool ascii = context.Glyphs.Tier != Capabilities.GlyphTier.Full;
            List<string> glyphs = new List<string>();
            if ((reference & TreeReferenceKind.Cycle) != 0) glyphs.Add(ascii ? "@" : "↺");
            else if ((reference & TreeReferenceKind.Repeat) != 0) glyphs.Add(ascii ? "@" : "↪");
            if ((reference & TreeReferenceKind.Collapsed) != 0) glyphs.Add(ascii ? ">" : "▸");
            if ((reference & TreeReferenceKind.Incomplete) != 0) glyphs.Add("?");
            return string.Join(" ", glyphs);
        }

        private static string ReferenceWords(TreeRow row) {
            TreeReferenceKind reference = row.Reference;
            List<string> words = new List<string>();
            if ((reference & TreeReferenceKind.Cycle) != 0) {
                if (!row.Node.HasLocalizedCycleNote) words.Add("cycle");
            }
            else if ((reference & TreeReferenceKind.Repeat) != 0) words.Add("shown above");
            if ((reference & TreeReferenceKind.Collapsed) != 0) words.Add("collapsed");
            if ((reference & TreeReferenceKind.Incomplete) != 0) words.Add("incomplete");
            return string.Join(" ", words);
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
