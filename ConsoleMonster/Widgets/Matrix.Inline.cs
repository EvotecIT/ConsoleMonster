using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets;

public sealed partial class Matrix {
    /// <summary>The inline grid's fitted label form (0 full, 1 short, 2 numbered, 3 none fits) and its widths.</summary>
    private sealed class InlinePlan {
        internal int Mode;
        internal int NameWidth;
        internal int Lead;
        internal int BandWidth;
        internal bool ColumnBands;
        internal int[] Columns = Array.Empty<int>();
    }

    private InlinePlan PlanInline(int width, RenderContext context) {
        InlinePlan plan = new InlinePlan { Columns = new int[_columns.Count] };
        plan.ColumnBands = _columns.Any(column => !string.IsNullOrEmpty(column.Band));
        plan.BandWidth = _rows.Where(row => !string.IsNullOrEmpty(row.Band)).Select(row => DisplayWidth.Measure(row.Band!)).DefaultIfEmpty(0).Max();
        if (plan.BandWidth > 0) plan.BandWidth = Math.Max(8, plan.BandWidth);
        for (plan.Mode = 0; plan.Mode < 3; plan.Mode++) {
            int mode = plan.Mode;
            bool numbered = mode == 2;
            int natural = _rows.Select((row, index) => DisplayWidth.Measure(Name(row, index, true, mode))).DefaultIfEmpty(0).Max();
            plan.NameWidth = Math.Min(natural, Math.Max(8, width / 3));
            int bands = numbered ? 0 : plan.BandWidth;
            plan.Lead = plan.NameWidth + 2 + (bands > 0 ? bands + 2 : 0);
            if (!numbered && !plan.ColumnBands && !string.IsNullOrEmpty(RowAxisCaption)) plan.Lead = Math.Max(plan.Lead, DisplayWidth.Measure(RowAxisCaption + " " + context.Glyphs.Get(Glyph.ArrowDown)) + 1);
            if (numbered) plan.Lead = Math.Max(plan.Lead, DisplayWidth.Measure((ShortRowAxisCaption ?? RowAxisCaption ?? string.Empty) + " " + context.Glyphs.Get(Glyph.ArrowDown) + "  " + (ShortColumnAxisCaption ?? ColumnAxisCaption ?? string.Empty) + " " + context.Glyphs.Get(Glyph.ArrowRight)) + 2);
            for (int c = 0; c < plan.Columns.Length; c++) {
                int content = _rows.Select((_, r) => InlineValueWidth(GetCell(r, c), numbered, context)).DefaultIfEmpty(1).Max();
                plan.Columns[c] = numbered ? 5 : Math.Max(Math.Max(1, MinCellWidth), Math.Max(content, DisplayWidth.Measure(Name(_columns[c], c, false, mode))));
            }
            if (InlineTotalWidth(plan) <= width) break;
        }
        return plan;
    }

    private bool InlineList(InlinePlan plan, int width) => NonHealthyOnly || width < 40 || plan.Mode > 2 || _rows.Count > MaxGridSize || _columns.Count > MaxGridSize;

    private int InlineGap(int mode, int column) => mode == 2 || _columns[column - 1].Band == _columns[column].Band ? 2 : 3;

    private int InlineTotalWidth(InlinePlan plan) => plan.Lead + plan.Columns.Sum() + Enumerable.Range(1, Math.Max(0, plan.Columns.Length - 1)).Sum(column => InlineGap(plan.Mode, column));

    private static int InlineValueWidth(MatrixCell? cell, bool numbered, RenderContext context) {
        if (cell?.Diagonal == true) return DisplayWidth.Measure(context.Glyphs.Get(Glyph.Self));
        if (cell == null) return DisplayWidth.Measure(context.Glyphs.Get(Glyph.None));
        string text = numbered ? cell.ShortText ?? cell.Text ?? string.Empty : cell.Text ?? string.Empty;
        return DisplayWidth.Measure(CellMarker(cell, context)) + (text.Length > 0 ? 1 + DisplayWidth.Measure(text) : 0);
    }

    private List<StyledLine> InlineLines(int width, RenderContext context) {
        List<StyledLine> result = new List<StyledLine>();
        InlinePlan plan = PlanInline(width, context);
        int mode = plan.Mode;
        bool columnBands = plan.ColumnBands;
        int bandWidth = plan.BandWidth;
        int[] columns = plan.Columns;
        int lead = plan.Lead;
        if (InlineList(plan, width)) {
            Attention(result, width, context);
        } else {
            bool numbered = mode == 2;
            if (!numbered) {
                string caption = string.IsNullOrEmpty(ColumnAxisCaption) ? string.Empty : ColumnAxisCaption + " " + context.Glyphs.Get(Glyph.ArrowRight);
                List<TextSpan> band = new List<TextSpan> { new TextSpan(new string(' ', Math.Max(0, lead - caption.Length - 2)) + caption + "  ", context.Style(ThemeToken.Muted)) };
                for (int c = 0; c < _columns.Count; c++) {
                    if (c > 0) band.Add(new TextSpan(new string(' ', Gap(c))));
                    string text = !columnBands ? Name(_columns[c], c, false, mode) : c == 0 || _columns[c - 1].Band != _columns[c].Band ? _columns[c].Band ?? string.Empty : string.Empty;
                    Pad(band, StyledLine.From(text, context.Style(ThemeToken.Muted).With(TextDecoration.Bold)), columns[c]);
                }
                if (band.Any(span => span.Text.Trim().Length > 0)) result.Add(new StyledLine(band));
            }
            string rowCaption = (numbered ? ShortRowAxisCaption ?? RowAxisCaption : RowAxisCaption) ?? string.Empty;
            if (rowCaption.Length > 0) rowCaption += " " + context.Glyphs.Get(Glyph.ArrowDown);
            if (numbered) rowCaption += "  " + (ShortColumnAxisCaption ?? ColumnAxisCaption ?? string.Empty) + " " + context.Glyphs.Get(Glyph.ArrowRight);
            List<TextSpan> header = new List<TextSpan>();
            Pad(header, StyledLine.From(rowCaption, context.Style(ThemeToken.Muted)), lead);
            for (int c = 0; c < _columns.Count && (numbered || columnBands); c++) {
                if (c > 0) header.Add(new TextSpan(new string(' ', Gap(c))));
                Pad(header, StyledLine.From(Name(_columns[c], c, false, mode), context.Style(numbered ? ThemeToken.Muted : ThemeToken.Text)), columns[c]);
            }
            if (numbered || columnBands || rowCaption.Length > 0) result.Add(new StyledLine(header));
            string? previous = null;
            for (int r = 0; r < _rows.Count; r++) {
                List<TextSpan> row = new List<TextSpan>();
                if (!numbered && bandWidth > 0) {
                    string band = _rows[r].Band == previous ? string.Empty : _rows[r].Band ?? string.Empty;
                    Pad(row, StyledLine.From(band, context.Style(ThemeToken.Muted).With(TextDecoration.Bold)), bandWidth);
                    row.Add(new TextSpan("  ")); previous = _rows[r].Band;
                }
                Pad(row, StyledLine.From(Name(_rows[r], r, true, mode), context.Style(ThemeToken.Text)), lead - (!numbered && bandWidth > 0 ? bandWidth + 2 : 0));
                for (int c = 0; c < _columns.Count; c++) {
                    if (c > 0) row.Add(new TextSpan(new string(' ', Gap(c))));
                    Pad(row, Value(GetCell(r, c), numbered), columns[c]);
                }
                result.Add(new StyledLine(row));
            }
            if (ShowLegend) {
                result.Add(StyledLine.Empty);
                result.AddRange(InlineLegend(context).Lines(width, context));
            }
            if (numbered) {
                string[] keys = _rows.Select((row, index) => Markup.Escape((index + 1).ToString(CultureInfo.InvariantCulture) + " " + (row.ShortText ?? row.Text))).ToArray();
                bool sameAxes = _rows.Count == _columns.Count && _rows.Select(row => row.ShortText ?? row.Text).SequenceEqual(_columns.Select(column => column.ShortText ?? column.Text), StringComparer.Ordinal);
                if (!sameAxes) result.Add(StyledLine.From(RowAxisCaption ?? "Rows", context.Style(ThemeToken.Muted)));
                result.AddRange(new Legend(keys).Lines(width, context));
                if (!sameAxes) {
                    result.Add(StyledLine.From(ColumnAxisCaption ?? "Columns", context.Style(ThemeToken.Muted)));
                    result.AddRange(new Legend(_columns.Select((column, index) => Markup.Escape((index + 1).ToString(CultureInfo.InvariantCulture) + " " + (column.ShortText ?? column.Text))).ToArray()).Lines(width, context));
                }
            }
        }
        if (!string.IsNullOrEmpty(CellNote)) WidgetLines.Wrap(result, StyledLine.From(CellNote!, context.Style(ThemeToken.Muted)), width);
        return result;

        int Gap(int column) => InlineGap(mode, column);
        void Pad(List<TextSpan> spans, StyledLine line, int cells) {
            StyledLine fitted = TextWrapper.Truncate(line, Math.Max(0, cells), context.Ellipsis);
            spans.AddRange(fitted); spans.Add(new TextSpan(new string(' ', Math.Max(0, cells - fitted.Width))));
        }
        StyledLine Value(MatrixCell? cell, bool numbered) {
            if (cell?.Diagonal == true) return StyledLine.From(context.Glyphs.Get(Glyph.Self), context.Style(ThemeToken.Muted));
            if (cell == null) return StyledLine.From(context.Glyphs.Get(Glyph.None), context.Style(ThemeToken.Subtle));
            string marker = CellMarker(cell, context);
            ThemeToken tone = cell.Tone ?? (cell.Status?.ToToken() ?? ThemeToken.Muted);
            List<TextSpan> spans = new List<TextSpan> { new TextSpan(marker, context.Style(tone)) };
            string text = numbered ? cell.ShortText ?? cell.Text ?? string.Empty : cell.Text ?? string.Empty;
            if (text.Length > 0) spans.Add(new TextSpan(" " + text, context.Style(cell.Status == StatusKind.Pass ? ThemeToken.Text : tone)));
            return new StyledLine(spans);
        }
    }
    private static string Name(MatrixLabel label, int index, bool row, int mode) => mode == 2
        ? (index + 1).ToString(CultureInfo.InvariantCulture) + (row ? " " + (label.ShortText ?? label.Text) : string.Empty)
        : mode == 1 ? label.ShortText ?? label.Text : label.Text;

    private Legend InlineLegend(RenderContext context, IDictionary<StatusKind, string>? markers = null) {
        Dictionary<StatusKind, int> counts = new Dictionary<StatusKind, int>();
        int self = 0;
        foreach (MatrixCell cell in _cells.Values) {
            if (cell.Diagonal) self++;
            else if (cell.Status.HasValue) counts[cell.Status.Value] = counts.TryGetValue(cell.Status.Value, out int count) ? count + 1 : 1;
        }
        List<string> entries = new List<string>();
        foreach (StatusKind status in new[] { StatusKind.Pass, StatusKind.Warn, StatusKind.Fail, StatusKind.Info, StatusKind.Unknown, StatusKind.Pending, StatusKind.Running, StatusKind.Skip }) {
            if (!counts.TryGetValue(status, out int count)) continue;
            string marker = LegendMarkers.TryGetValue(status, out string? custom) ? custom : markers != null && markers.TryGetValue(status, out string? mode) ? mode : context.Glyphs.StatusSymbol(status);
            string label = LegendLabels.TryGetValue(status, out string? supplied) ? supplied : status.ToWord(context).ToLowerInvariant();
            ThemeToken role = LegendRoles.TryGetValue(status, out ThemeToken declared) ? declared : status.ToToken();
            entries.Add("[" + role.ToString().ToLowerInvariant() + "]" + Markup.Escape(marker) + "[/] [" + (status == StatusKind.Pass && ConsoleViewStyle.IsApplied(context) ? "text" : role.ToString().ToLowerInvariant()) + "]"
                + Markup.Escape(label + (ShowLegendCounts ? " " + count.ToString(context.Culture) : string.Empty)) + "[/]");
        }
        long absent = (long)_rows.Count * _columns.Count - self - _cells.Values.Count(cell => !cell.Diagonal);
        if (absent > 0) entries.Add("[subtle]" + context.Glyphs.Get(Glyph.None) + "[/][muted] " + Markup.Escape(MissingLegendLabel ?? "none") + "[/]");
        if (self > 0) entries.Add("[muted]" + context.Glyphs.Get(Glyph.Self) + " " + Markup.Escape(SelfLegendLabel ?? "self") + "[/]");
        foreach (string entry in AdditionalLegendEntries) entries.Add(entry);
        return new Legend(entries.ToArray());
    }
}
