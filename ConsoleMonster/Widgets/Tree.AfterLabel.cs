using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets;

public sealed partial class Tree {
    private List<StyledLine> AfterLabelLines(int width, RenderContext context) {
        List<TreeRow> rows = Flatten(context);
        PresenterContext presenter = Presentation.CreateContext(context);
        List<int> labelWidths = new List<int>();
        int desired = 0;
        foreach (TreeRow row in rows) {
            if (row.Node.Bare || row.Node.Values.Count == 0) { continue; }
            int length = Label(row).Width;
            labelWidths.Add(length); desired = Math.Max(desired, length);
        }
        labelWidths.Sort();
        int p90 = labelWidths.Count == 0 ? 0 : labelWidths[Math.Min(labelWidths.Count - 1, (int)Math.Ceiling(labelWidths.Count * .9) - 1)];
        int labelWidth = Math.Min(desired, Math.Max(p90 + 8, (int)Math.Ceiling(width * .4)));
        int[] columnWidths = new int[ValueColumns.Count];
        List<int> visible = new List<int>();
        for (int c = 0; c < ValueColumns.Count; c++) {
            TableColumn column = ValueColumns[c];
            int want = ShowValueHeaders ? context.Markup(column.Header).Width : 0;
            foreach (TreeRow row in rows) { want = Math.Max(want, Value(row, c).Width); }
            columnWidths[c] = column.Width ?? Math.Max(column.MinWidth ?? 0, Math.Min(column.MaxWidth ?? int.MaxValue, want));
            if (context.Profile.Width >= 80 || column.Priority >= 8) { visible.Add(c); }
        }
        int identityMinimum = Math.Min(desired, Math.Max(16, width * 2 / 5));
        while (visible.Count > 1 && ColumnsWidth(visible, columnWidths) > width - identityMinimum) {
            int remove = visible[0];
            foreach (int c in visible) { if (ValueColumns[c].Priority < ValueColumns[remove].Priority) remove = c; }
            visible.Remove(remove);
        }
        if (visible.Count == 1) columnWidths[visible[0]] = Math.Min(columnWidths[visible[0]], Math.Max(1, width - identityMinimum - 2));
        int valuesWidth = ColumnsWidth(visible, columnWidths);
        labelWidth = Math.Max(1, Math.Min(labelWidth, width - valuesWidth));
        if (context.Profile.Width < 80 && visible.Count > 1) {
            int best = visible[0];
            foreach (int c in visible) { if (ValueColumns[c].Priority > ValueColumns[best].Priority) { best = c; } }
            visible.Clear(); visible.Add(best);
            labelWidth = Math.Max(1, Math.Min(desired, width - ColumnsWidth(visible, columnWidths)));
        }
        List<StyledLine> result = new List<StyledLine>();
        if (ShowValueHeaders && visible.Count > 0 && context.Profile.Width >= 80) {
            List<TextSpan> header = new List<TextSpan> { new TextSpan(new string(' ', labelWidth)) };
            foreach (int c in visible) { AppendColumn(header, context.Markup(ValueColumns[c].Header).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted)), columnWidths[c], ValueColumns[c].Alignment ?? HorizontalAlignment.Left, context); }
            result.Add(new StyledLine(header));
        }
        foreach (TreeRow row in rows) {
            List<StyledLine> continuations = new List<StyledLine>();
            StyledLine label = Label(row);
            if (row.Node.Values.Count == 0 && row.Reference == TreeReferenceKind.None) {
                List<TextSpan> standalone = new List<TextSpan>(FitLabel(label, width, context));
                AppendEvidence(row, standalone, continuations, false);
                result.Add(new StyledLine(standalone)); result.AddRange(continuations); continue;
            }
            label = row.Node.Bare ? FitLabel(label, width, context) : row.Node.TruncateLabelInMiddle ? TextWrapper.TruncateMiddle(label, labelWidth, context.Ellipsis) : TextWrapper.Truncate(label, labelWidth, context.Ellipsis);
            List<TextSpan> spans = new List<TextSpan>(label);
            spans.Add(new TextSpan(new string(' ', Math.Max(0, labelWidth - label.Width))));
            foreach (int c in visible) {
                StyledLine value = Value(row, c);
                int desiredValue = context.Profile.Width < 80 ? value.Width : columnWidths[c];
                AppendColumn(spans, value, Math.Min(desiredValue, Math.Max(0, width - new StyledLine(spans).Width - 2)), ValueColumns[c].Alignment ?? HorizontalAlignment.Left, context);
            }
            if (visible.Count < ValueColumns.Count) {
                foreach (int c in System.Linq.Enumerable.Range(0, ValueColumns.Count)) {
                    if (visible.Contains(c) || context.Profile.Width < 80 && ValueColumns[c].Priority < 2) { continue; }
                    StyledLine note = Value(row, c);
                    if (note.Width == 0) { continue; }
                    AppendEvidenceLine(note, spans, continuations);
                }
            }
            bool referenceInValue = visible.Exists(c => IsState(ValueColumns[c])) && !row.Node.HasLocalizedCycleNote;
            AppendEvidence(row, spans, continuations, referenceInValue);
            result.Add(new StyledLine(spans));
            result.AddRange(continuations);
        }
        return result;

        StyledLine Label(TreeRow row) => Primary(row, int.MaxValue, true, context);
        bool IsState(TableColumn column) => string.Equals(column.Id, "State", StringComparison.OrdinalIgnoreCase) || string.Equals(column.Id, "Relation", StringComparison.OrdinalIgnoreCase);
        void AppendEvidence(TreeRow row, List<TextSpan> spans, List<StyledLine> continuations, bool referenceInValue) {
            string words = ReferenceWords(row, !referenceInValue);
            if (words.Length > 0) AppendEvidenceLine(StyledLine.From(words, context.Style(ThemeToken.Muted)), spans, continuations);
            string? detail = row.DetailFor(context);
            if (!string.IsNullOrEmpty(detail)) AppendEvidenceLine(StyledLine.From(detail!, context.Style(ThemeToken.Muted)), spans, continuations);
            if (row.Node.Value != null) {
                foreach (StyledLine line in CellRendering.Content(row.Node.Value, row.Node.Presenter, null, presenter).Lines) AppendEvidenceLine(line, spans, continuations);
            }
        }
        void AppendEvidenceLine(StyledLine note, List<TextSpan> spans, List<StyledLine> continuations) {
            if (note.Width == 0) return;
            string separator = " " + context.Glyphs.Get(Glyph.Dot) + " ";
            int remaining = width - new StyledLine(spans).Width - separator.Length;
            if (continuations.Count == 0 && remaining >= note.Width) {
                spans.Add(new TextSpan(separator, context.Style(ThemeToken.Muted))); spans.AddRange(note);
            } else {
                int indent = Math.Min(4, Math.Max(0, width - 1));
                foreach (StyledLine wrapped in TextWrapper.Wrap(note, Math.Max(1, width - indent))) {
                    List<TextSpan> detail = new List<TextSpan> { new TextSpan(new string(' ', indent)) };
                    detail.AddRange(wrapped); continuations.Add(new StyledLine(detail));
                }
            }
        }
        StyledLine Value(TreeRow row, int c) {
            TableColumn column = ValueColumns[c];
            bool state = IsState(column);
            if (state && (row.Reference & TreeReferenceKind.Cycle) != 0) { return StyledLine.From(context.Glyphs.Get(Glyph.Cycle) + (row.Node.HasLocalizedCycleNote ? string.Empty : " cycle"), context.Style(ThemeToken.Muted)); }
            if (state && (row.Reference & TreeReferenceKind.Repeat) != 0) { return StyledLine.From(context.Glyphs.Get(Glyph.Repeat) + " shown above", context.Style(ThemeToken.Muted)); }
            return ColumnValue(row.Node, column, presenter);
        }
    }
}
