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
    private List<StyledLine> InlineLines(int width, RenderContext context) {
        List<StyledLine> result = new List<StyledLine>();
        int mode = 0;
        bool columnBands = _columns.Any(column => !string.IsNullOrEmpty(column.Band));
        int bandWidth = _rows.Where(row => !string.IsNullOrEmpty(row.Band)).Select(row => DisplayWidth.Measure(row.Band!)).DefaultIfEmpty(0).Max();
        if (bandWidth > 0) bandWidth = Math.Max(8, bandWidth);
        int nameWidth = 0;
        int[] columns = new int[_columns.Count];
        int lead = 0;
        for (; mode < 3; mode++) {
            bool numbered = mode == 2;
            nameWidth = _rows.Select((row, index) => DisplayWidth.Measure(Name(row, index, true, mode))).DefaultIfEmpty(0).Max();
            nameWidth = Math.Min(nameWidth, Math.Max(8, width / 3));
            int bands = numbered ? 0 : bandWidth;
            lead = nameWidth + 2 + (bands > 0 ? bands + 2 : 0);
            if (!numbered && !columnBands && !string.IsNullOrEmpty(RowAxisCaption)) lead = Math.Max(lead, DisplayWidth.Measure(RowAxisCaption + " " + context.Glyphs.Get(Glyph.ArrowDown)) + 1);
            if (numbered) lead = Math.Max(lead, DisplayWidth.Measure((ShortRowAxisCaption ?? RowAxisCaption ?? string.Empty) + " " + context.Glyphs.Get(Glyph.ArrowDown) + "  " + (ShortColumnAxisCaption ?? ColumnAxisCaption ?? string.Empty) + " " + context.Glyphs.Get(Glyph.ArrowRight)) + 2);
            for (int c = 0; c < columns.Length; c++) {
                int content = _rows.Select((_, r) => Value(GetCell(r, c), numbered).Width).DefaultIfEmpty(1).Max();
                columns[c] = numbered ? 5 : Math.Max(Math.Max(1, MinCellWidth), Math.Max(content, DisplayWidth.Measure(Name(_columns[c], c, false, mode))));
            }
            if (TotalWidth() <= width) break;
        }
        if (NonHealthyOnly || width < 40 || mode > 2 || _rows.Count > MaxGridSize || _columns.Count > MaxGridSize) {
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

        int Gap(int column) => mode == 2 || _columns[column - 1].Band == _columns[column].Band ? 2 : 3;
        int TotalWidth() => lead + columns.Sum() + Enumerable.Range(1, Math.Max(0, columns.Length - 1)).Sum(Gap);
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

    private Legend InlineLegend(RenderContext context) {
        Dictionary<StatusKind, int> counts = new Dictionary<StatusKind, int>();
        int self = 0;
        foreach (MatrixCell cell in _cells.Values) {
            if (cell.Diagonal) self++;
            else if (cell.Status.HasValue) counts[cell.Status.Value] = counts.TryGetValue(cell.Status.Value, out int count) ? count + 1 : 1;
        }
        List<string> entries = new List<string>();
        foreach (StatusKind status in new[] { StatusKind.Pass, StatusKind.Warn, StatusKind.Fail, StatusKind.Info, StatusKind.Unknown, StatusKind.Pending, StatusKind.Running, StatusKind.Skip }) {
            if (!counts.TryGetValue(status, out int count)) continue;
            string marker = LegendMarkers.TryGetValue(status, out string? custom) ? custom : context.Glyphs.StatusSymbol(status);
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
