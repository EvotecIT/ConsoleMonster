using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Frames;

/// <summary>
/// Inline bars as markup for the destination's glyph tier, so they can sit in table cells and lists. Line bars are a
/// heavy stroke on a light track with half-cell precision; on Basic they are lower half blocks, on ASCII <c>#</c> on <c>.</c>.
/// </summary>
public static class FrameBars {
    /// <summary>A line data bar.</summary>
    /// <param name="fraction">Filled share, 0 to 1.</param><param name="width">Cells.</param>
    /// <param name="state">The fill's state; null draws a neutral fill.</param><param name="context">The destination.</param><returns>Markup exactly <paramref name="width"/> cells wide.</returns>
    public static string Line(double fraction, int width, HealthState? state, RenderContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (width <= 0) return string.Empty;
        fraction = double.IsNaN(fraction) ? 0 : Math.Max(0, Math.Min(1, fraction));
        string fill = state.HasValue && state.Value != HealthState.Healthy ? FrameText.Tag(state.Value) : "muted";
        GlyphTier tier = context.Glyphs.Tier;
        if (tier == GlyphTier.Full) {
            int halves = (int)Math.Round(fraction * width * 2, MidpointRounding.AwayFromZero);
            if (halves == 0 && fraction > 0) halves = 1;
            int full = halves / 2;
            bool half = halves % 2 == 1;
            int rest = width - full - (half ? 1 : 0);
            return Run(fill, "━", full) + (half ? "[" + fill + "]╸[/]" : string.Empty) + Run("subtle", "─", rest);
        }
        int cells = (int)Math.Round(fraction * width, MidpointRounding.AwayFromZero);
        if (cells == 0 && fraction > 0) cells = 1;
        // Basic uses lower half blocks (WGL4): full blocks or shades merge into one slab across stacked rows.
        if (tier == GlyphTier.Basic) return Run(fill, "▄", cells) + (context.HasColor ? Run("subtle", "▄", width - cells) : Run("subtle", "─", width - cells));
        return Run(fill, "#", cells) + Run("subtle", ".", width - cells);
    }

    /// <summary>A range on a shared axis: a track, a segment from <paramref name="from"/> to <paramref name="to"/> and a dot at <paramref name="to"/>.</summary>
    /// <param name="from">Start of the range, 0 to 1 on the axis.</param><param name="to">End of the range, 0 to 1 on the axis.</param>
    /// <param name="width">Cells.</param><param name="state">The range's state; null or healthy draws it neutral.</param>
    /// <param name="threshold">An optional threshold position, 0 to 1, supplied by the data owner.</param><param name="context">The destination.</param>
    /// <returns>Markup exactly <paramref name="width"/> cells wide.</returns>
    public static string Range(double from, double to, int width, HealthState? state, double? threshold, RenderContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (width <= 0) return string.Empty;
        GlyphTier tier = context.Glyphs.Tier;
        string track = tier == GlyphTier.Ascii ? "-" : "─";
        string segment = tier == GlyphTier.Full ? "━" : tier == GlyphTier.Basic ? "═" : "=";
        string dot = tier == GlyphTier.Full ? "●" : tier == GlyphTier.Basic ? "■" : "o";
        string mark = tier == GlyphTier.Full ? "┊" : "|";
        string role = state.HasValue && state.Value != HealthState.Healthy ? FrameText.Tag(state.Value) : "muted";
        int start = Cell(Math.Min(from, to), width);
        int end = Cell(Math.Max(from, to), width);
        int? marker = threshold.HasValue ? Cell(threshold.Value, width) : (int?)null;
        StringBuilder text = new StringBuilder();
        string? open = null;
        for (int x = 0; x < width; x++) {
            string glyph; string style;
            if (x == end) { glyph = !context.HasColor && state.HasValue && state.Value != HealthState.Healthy ? FrameText.Glyph(state.Value, context) : dot; style = role; }
            else if (x >= start && x < end) { glyph = segment; style = role; }
            else if (marker.HasValue && x == marker.Value) { glyph = mark; style = "subtle"; }
            else { glyph = track; style = "subtle"; }
            if (open != style) { if (open != null) text.Append("[/]"); text.Append('[').Append(style).Append(']'); open = style; }
            text.Append(glyph);
        }
        if (open != null) text.Append("[/]");
        return text.ToString();
    }

    /// <summary>A state glyph and a note in place of a bar, for values that were not reported.</summary>
    /// <param name="text">The literal note.</param><param name="state">The note's state.</param><param name="width">Cells.</param><param name="context">The destination.</param>
    /// <returns>Markup at most <paramref name="width"/> cells wide.</returns>
    public static string Note(string text, HealthState state, int width, RenderContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        string glyph = FrameText.Glyph(state, context);
        string note = Text.TextWrapper.Truncate(FrameText.Fold(text ?? string.Empty, context), Math.Max(0, width - glyph.Length - 1), context.Ellipsis);
        return "[" + FrameText.Tag(state) + "]" + glyph + (note.Length > 0 ? " " + FrameText.Escape(note) : string.Empty) + "[/]";
    }

    private static int Cell(double fraction, int width) {
        fraction = double.IsNaN(fraction) ? 0 : Math.Max(0, Math.Min(1, fraction));
        return Math.Min(width - 1, (int)Math.Round(fraction * (width - 1), MidpointRounding.AwayFromZero));
    }

    private static string Run(string style, string glyph, int count) {
        if (count <= 0) return string.Empty;
        StringBuilder text = new StringBuilder("[").Append(style).Append(']');
        for (int i = 0; i < count; i++) text.Append(glyph);
        return text.Append("[/]").ToString();
    }
}

/// <summary>A logarithmic axis shared by every row of a section, with labels placed on the cells they describe.</summary>
public sealed class LogScale {
    /// <summary>Creates an axis.</summary>
    /// <param name="minimum">The smallest value, above zero.</param><param name="maximum">The largest value.</param>
    public LogScale(double minimum, double maximum) {
        if (minimum <= 0 || maximum <= minimum || double.IsInfinity(maximum) || double.IsNaN(minimum) || double.IsNaN(maximum)) throw new ArgumentOutOfRangeException(nameof(minimum), "The axis needs finite 0 < minimum < maximum.");
        Minimum = minimum; Maximum = maximum;
    }
    /// <summary>The smallest value.</summary>
    public double Minimum { get; }
    /// <summary>The largest value.</summary>
    public double Maximum { get; }
    /// <summary>The position of a value, 0 to 1; values outside the axis clamp to its ends.</summary>
    /// <param name="value">The value.</param><returns>The position.</returns>
    public double Fraction(double value) {
        if (double.IsNaN(value) || value <= Minimum) return 0;
        if (value >= Maximum) return 1;
        return (Math.Log(value) - Math.Log(Minimum)) / (Math.Log(Maximum) - Math.Log(Minimum));
    }
    /// <summary>Axis labels for a bar of the given width; a label that would overlap the previous one is skipped.</summary>
    /// <param name="width">Cells.</param><param name="ticks">Values and literal labels, in increasing order.</param><returns>Plain text, at most <paramref name="width"/> cells.</returns>
    public string Labels(int width, IEnumerable<KeyValuePair<double, string>> ticks) {
        char[] line = new string(' ', Math.Max(0, width)).ToCharArray();
        int next = 0;
        if (ticks == null) throw new ArgumentNullException(nameof(ticks));
        foreach (KeyValuePair<double, string> tick in ticks) {
            if (string.IsNullOrEmpty(tick.Value)) continue;
            int x = Math.Min(width - 1, (int)Math.Round(Fraction(tick.Key) * (width - 1), MidpointRounding.AwayFromZero));
            if (x + tick.Value.Length > width) x = width - tick.Value.Length;
            if (x < next || x < 0) continue;
            for (int i = 0; i < tick.Value.Length; i++) line[x + i] = tick.Value[i];
            next = x + tick.Value.Length + 1;
        }
        return new string(line).TrimEnd();
    }
}
