using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets;

/// <summary>What a <see cref="Matrix"/> shows when its grid does not fit.</summary>
public enum MatrixOverflow {
    /// <summary>The attention list of non-healthy cells (default).</summary>
    List,
    /// <summary>
    /// One single-cell status symbol per column under the column bands; when that is still too wide, one cell per column
    /// band showing its worst status; the attention list only when neither fits. Suits estates with many targets, such as
    /// checks × domain controllers grouped by site.
    /// </summary>
    Dense
}

public sealed partial class Matrix {
    /// <summary>What to show when the grid does not fit or an axis exceeds <see cref="MaxGridSize"/> (default <see cref="MatrixOverflow.List"/>).</summary>
    public MatrixOverflow Overflow { get; set; }

    /// <summary>The dense form that fits the width: per column, per band, or the list when neither fits.</summary>
    private LayoutKind DenseFit(int width, RenderContext context) {
        if (Overflow != MatrixOverflow.Dense || NonHealthyOnly || _columns.Count == 0 || _rows.Count == 0) return LayoutKind.List;
        int lead = DenseLabelWidth(width) + 1, tail = 2 + DenseSummaryWidth(context);
        if (lead + _columns.Count + tail <= width) return LayoutKind.DensePerColumn;
        List<(string? Band, int Start, int Count)> bands = DenseBands();
        if (bands.Count < _columns.Count && !bands.Exists(band => band.Band == null) && lead + bands.Count + tail <= width) return LayoutKind.DensePerBand;
        return LayoutKind.List;
    }

    private int DenseLabelWidth(int width) {
        int raw = 0;
        foreach (MatrixLabel row in _rows) raw = Math.Max(raw, DisplayWidth.Measure(row.ShortText ?? row.Text));
        return Math.Min(raw, Math.Max(8, width / 4));
    }

    /// <summary>
    /// The row summary's widest form: room for failure and warning counts as large as the column count, so the dense form
    /// chosen for a width does not flip as a live run's counts grow.
    /// </summary>
    private int DenseSummaryWidth(RenderContext context) {
        int digits = _columns.Count.ToString(CultureInfo.InvariantCulture).Length;
        int widest = 2 * (1 + digits) + 1;
        for (int r = 0; r < _rows.Count; r++) widest = Math.Max(widest, DenseSummary(r, context).Width);
        return widest;
    }

    private List<(string? Band, int Start, int Count)> DenseBands() {
        List<(string? Band, int Start, int Count)> bands = new List<(string?, int, int)>();
        for (int c = 0; c < _columns.Count;) {
            int end = c + 1;
            while (end < _columns.Count && _columns[end].Band == _columns[c].Band) end++;
            bands.Add((string.IsNullOrEmpty(_columns[c].Band) ? null : _columns[c].Band, c, end - c));
            c = end;
        }
        return bands;
    }

    private List<StyledLine> DenseLines(int width, bool perBand, RenderContext context) {
        List<StyledLine> lines = new List<StyledLine>();
        if (!string.IsNullOrEmpty(Title)) WidgetLines.Wrap(lines, context.Markup(Title!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Accent).With(TextDecoration.Bold)), width);
        int labelWidth = DenseLabelWidth(width);
        List<(string? Band, int Start, int Count)> bands = DenseBands();
        bool hasBands = bands.Exists(band => band.Band != null);
        string lead = new string(' ', labelWidth + 1);

        if (hasBands) {
            // Band names start at their first cell when they fit before the next name; the guide line alternates tone per band.
            int cells = perBand ? bands.Count : _columns.Count;
            StringBuilder names = new StringBuilder();
            int position = 0, cursor = 0;
            List<TextSpan> guide = new List<TextSpan> { new TextSpan(lead) };
            string even = GuideMark(context, true), odd = GuideMark(context, false);
            for (int b = 0; b < bands.Count; b++) {
                int span = perBand ? 1 : bands[b].Count;
                string name = bands[b].Band ?? string.Empty;
                // A name starts at its band's first cell when the previous name has ended, and never runs past the field.
                int room = Math.Min(Math.Max(span, 3), cells - position);
                if (position >= cursor && room > 0 && name.Length > 0) {
                    string shown = DisplayWidth.Measure(name) <= room ? name : TextWrapper.Truncate(StyledLine.From(name), room, string.Empty).PlainText;
                    names.Append(' ', position - cursor).Append(shown);
                    cursor = position + DisplayWidth.Measure(shown) + 1;
                    if (cursor - 1 < cells) names.Append(' ');
                    else cursor = cells;
                }
                guide.Add(new TextSpan(Repeat(b % 2 == 0 ? even : odd, span), context.Style(b % 2 == 0 ? ThemeToken.Muted : ThemeToken.Subtle)));
                position += span;
            }
            string caption = string.IsNullOrEmpty(ColumnAxisCaption) ? string.Empty : ColumnAxisCaption + " " + context.Glyphs.Get(Glyph.ArrowRight);
            List<TextSpan> header = new List<TextSpan> { new TextSpan(Fit(caption, labelWidth, context.Ellipsis) + " ", context.Style(ThemeToken.Muted)), new TextSpan(names.ToString().TrimEnd(), context.Style(ThemeToken.Muted).With(TextDecoration.Bold)) };
            lines.Add(new StyledLine(header));
            lines.Add(new StyledLine(guide));
        }

        string? previous = null;
        for (int r = 0; r < _rows.Count; r++) {
            string? band = _rows[r].Band;
            if (!string.IsNullOrEmpty(band) && band != previous) WidgetLines.Wrap(lines, StyledLine.From(band!, context.Style(ThemeToken.Muted).With(TextDecoration.Bold)), width);
            previous = band;
            List<TextSpan> row = new List<TextSpan> { new TextSpan(Fit(_rows[r].ShortText ?? _rows[r].Text, labelWidth, context.Ellipsis) + " ", context.Style(ThemeToken.Text)) };
            if (perBand) {
                foreach (var group in bands) {
                    MatrixCell? worst = null;
                    for (int c = group.Start; c < group.Start + group.Count; c++) worst = Worse(worst, GetCell(r, c));
                    row.Add(DenseCell(worst, context));
                }
            } else {
                for (int c = 0; c < _columns.Count; c++) row.Add(DenseCell(GetCell(r, c), context));
            }
            row.Add(new TextSpan("  "));
            row.AddRange(DenseSummary(r, context));
            lines.Add(new StyledLine(row));
        }

        if (ShowLegend) {
            lines.Add(StyledLine.Empty);
            Dictionary<StatusKind, string>? markers = context.Glyphs.Tier == GlyphTier.Ascii ? null : new Dictionary<StatusKind, string> {
                [StatusKind.Pass] = OneCell("■", context.Glyphs.StatusSymbol(StatusKind.Pass)), [StatusKind.Pending] = OneCell(context.Glyphs.Get(Glyph.None), ".") };
            lines.AddRange(InlineLegend(context, markers).Lines(width, context));
        }
        string unit = string.IsNullOrEmpty(ColumnAxisCaption) ? "column" : ColumnAxisCaption!;
        string note = CellNote ?? (perBand
            ? "one cell per group of " + unit + "s, showing the worst · widen for one cell per " + unit
            : "one cell per " + unit + " · " + _columns.Count.ToString(CultureInfo.InvariantCulture) + " in " + bands.Count.ToString(CultureInfo.InvariantCulture) + (bands.Count == 1 ? " group" : " groups"));
        WidgetLines.Wrap(lines, StyledLine.From(note, context.Style(ThemeToken.Muted)), width);
        return lines;
    }

    private StyledLine DenseSummary(int row, RenderContext context) {
        Dictionary<StatusKind, int> counts = new Dictionary<StatusKind, int>();
        for (int c = 0; c < _columns.Count; c++) {
            MatrixCell? cell = GetCell(row, c);
            if (cell == null || cell.Diagonal || !cell.Status.HasValue) continue;
            bool healthy = IsHealthy?.Invoke(cell.Status) ?? (cell.Status != StatusKind.Fail && cell.Status != StatusKind.Warn);
            if (!healthy) counts[cell.Status.Value] = counts.TryGetValue(cell.Status.Value, out int n) ? n + 1 : 1;
        }
        List<TextSpan> spans = new List<TextSpan>();
        foreach (StatusKind status in new[] { StatusKind.Fail, StatusKind.Warn, StatusKind.Unknown, StatusKind.Info, StatusKind.Running, StatusKind.Pending, StatusKind.Skip, StatusKind.Pass, StatusKind.None }) {
            if (!counts.TryGetValue(status, out int count)) continue;
            spans.Add(new TextSpan((spans.Count > 0 ? " " : string.Empty) + context.Glyphs.StatusSymbol(status) + count.ToString(context.Culture), context.Style(status)));
        }
        return new StyledLine(spans);
    }
    private static TextSpan DenseCell(MatrixCell? cell, RenderContext context) {
        if (cell?.Diagonal == true) return new TextSpan(OneCell(context.Glyphs.Get(Glyph.Self), "-"), context.Style(ThemeToken.Muted));
        if (cell == null) return new TextSpan(" ");
        ThemeToken tone = cell.Tone ?? (cell.Status?.ToToken() ?? ThemeToken.Muted);
        // Healthy and queued cells form a quiet field; problems keep their own shapes, so they stand out without colour.
        if (cell.Marker == null && context.Glyphs.Tier != GlyphTier.Ascii) {
            if (cell.Status == StatusKind.Pass) return new TextSpan(OneCell("■", context.Glyphs.StatusSymbol(StatusKind.Pass)), context.Style(tone));
            if (cell.Status == StatusKind.Pending) return new TextSpan(OneCell(context.Glyphs.Get(Glyph.None), "."), context.Style(ThemeToken.Subtle));
        }
        return new TextSpan(OneCell(CellMarker(cell, context), "?"), context.Style(tone));
    }

    private static string OneCell(string glyph, string fallback) => DisplayWidth.Measure(glyph) == 1 ? glyph : fallback;

    /// <summary>The band guide mark: blocks where the font has them (full tier), box drawing otherwise, always one cell.</summary>
    private static string GuideMark(RenderContext context, bool even) => context.Glyphs.Tier switch {
        GlyphTier.Ascii => even ? "=" : "-",
        GlyphTier.Basic => OneCell(even ? "━" : "─", even ? "=" : "-"),
        _ => OneCell(even ? "▀" : "▔", even ? "=" : "-") };

    private static string Repeat(string text, int count) {
        StringBuilder builder = new StringBuilder(text.Length * Math.Max(0, count));
        for (int i = 0; i < count; i++) builder.Append(text);
        return builder.ToString();
    }

    private static MatrixCell? Worse(MatrixCell? current, MatrixCell? candidate) {
        if (candidate == null || candidate.Diagonal) return current;
        if (current == null) return candidate;
        return Severity(candidate.Status) > Severity(current.Status) ? candidate : current;
    }

    private static int Severity(StatusKind? status) => status switch {
        StatusKind.Fail => 6, StatusKind.Warn => 5, StatusKind.Running => 4, StatusKind.Pending => 3,
        StatusKind.Info => 2, StatusKind.Pass => 1, _ => 0 };

    private static string Fit(string text, int width, string ellipsis) {
        string fitted = TextWrapper.Truncate(StyledLine.From(text), width, ellipsis).PlainText;
        return fitted + new string(' ', Math.Max(0, width - DisplayWidth.Measure(fitted)));
    }
}
