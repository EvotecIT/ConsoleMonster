using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Frames;

/// <summary>
/// A headline tile under the title line: a label, then a value with a bar or a short fact. Meters keep their declared
/// order; when they do not fit one line they wrap, so a meter is never dropped.
/// </summary>
public sealed class Meter {
    internal const int TileWidth = 28;
    private Meter(string label) { Label = label ?? string.Empty; }

    /// <summary>A composition: healthy out of the total, with the problem shares in their roles. A fully healthy meter is quiet.</summary>
    /// <param name="label">The literal label.</param><param name="healthy">Healthy items.</param><param name="warning">Warnings.</param>
    /// <param name="failing">Failing items.</param><param name="unknown">Items without a value.</param><param name="notEvaluated">Items that could not be evaluated.</param>
    /// <returns>The meter.</returns>
    public static Meter Composition(string label, long healthy, long warning = 0, long failing = 0, long unknown = 0, long notEvaluated = 0) =>
        new Meter(label) { Kind = MeterKind.Composition, Healthy = healthy, Warning = warning, Failing = failing, Unknown = unknown, NotEvaluated = notEvaluated };

    /// <summary>A healthy part of a whole; the rest is drawn in the state the data owner gives it.</summary>
    /// <param name="label">The literal label.</param><param name="healthy">The healthy part.</param><param name="whole">The whole.</param>
    /// <param name="rest">The state of the remainder, for example <see cref="HealthState.NotEvaluated"/> for targets that did not answer.</param>
    /// <returns>The meter.</returns>
    public static Meter Ratio(string label, long healthy, long whole, HealthState rest = HealthState.Failing) {
        Meter meter = new Meter(label) { Kind = MeterKind.Composition, Healthy = healthy };
        meter.Assign(rest, Math.Max(0, whole - healthy));
        return meter;
    }

    /// <summary>A count with an optional Muted fact (<c>12  all on CORP-DC03</c>).</summary>
    /// <param name="label">The literal label.</param><param name="value">The count.</param><param name="fact">A literal fact.</param><param name="state">The value's state.</param>
    /// <returns>The meter.</returns>
    public static Meter Count(string label, long value, string? fact = null, HealthState state = HealthState.Healthy) =>
        new Meter(label) { Kind = MeterKind.Value, CountValue = value, Note = fact, State = state };

    /// <summary>A literal value with an optional Muted fact (<c>2d 4h  CORP-DC03 ← CORP-DC05</c>).</summary>
    /// <param name="label">The literal label.</param><param name="value">The literal value.</param><param name="fact">A literal fact.</param><param name="state">The value's state.</param>
    /// <returns>The meter.</returns>
    public static Meter Fact(string label, string value, string? fact = null, HealthState state = HealthState.Healthy) =>
        new Meter(label) { Kind = MeterKind.Value, ValueText = value ?? string.Empty, Note = fact, State = state };

    /// <summary>A meter whose value was not reported. It shows <c>◌ unknown</c>, never zero.</summary>
    /// <param name="label">The literal label.</param><param name="reason">An optional literal reason.</param><returns>The meter.</returns>
    public static Meter Missing(string label, string? reason = null) =>
        new Meter(label) { Kind = MeterKind.Value, Note = reason, State = HealthState.Unknown, ValueText = null };

    /// <summary>The literal label.</summary>
    public string Label { get; }
    private MeterKind Kind { get; set; }
    private long Healthy { get; set; }
    private long Warning { get; set; }
    private long Failing { get; set; }
    private long Unknown { get; set; }
    private long NotEvaluated { get; set; }
    private long? CountValue { get; set; }
    private string? ValueText { get; set; }
    private string? Note { get; set; }
    private HealthState State { get; set; }

    private void Assign(HealthState state, long value) {
        switch (state) {
            case HealthState.Warning: Warning = value; break;
            case HealthState.Unknown: Unknown = value; break;
            case HealthState.NotEvaluated: NotEvaluated = value; break;
            case HealthState.Healthy: Healthy += value; break;
            default: Failing = value; break;
        }
    }

    internal string CountsText(RenderContext context) => FrameText.Escape(Label) + " " + Markup.Strip(ValueMarkup(context));

    private string ValueMarkup(RenderContext context) {
        if (Kind == MeterKind.Composition) {
            long total = Healthy + Warning + Failing + Unknown + NotEvaluated;
            long shown = Healthy;
            return "[strong]" + FrameText.Number(shown, context) + "[/][muted]/" + FrameText.Number(total, context) + "[/]";
        }
        if (State == HealthState.Unknown && ValueText == null && !CountValue.HasValue) return "[muted]" + FrameText.Glyph(HealthState.Unknown, context) + " unknown[/]";
        string value = CountValue.HasValue ? FrameText.Number(CountValue.Value, context) : FrameText.Escape(ValueText);
        return State == HealthState.Healthy ? "[strong]" + value + "[/]" : "[" + FrameText.Tag(State) + " bold]" + value + "[/]";
    }

    internal StyledLine[] Lines(int width, RenderContext context) {
        StyledLine label = FrameText.Line("[muted]" + FrameText.Escape(Label) + "[/]", context);
        if (label.Width > width) label = TextWrapper.Truncate(label, width, context.Ellipsis);
        StyledLine value = FrameText.Line(ValueMarkup(context), context);
        List<TextSpan> spans = new List<TextSpan>(value);
        int column = Math.Max(value.Width + 2, 8);
        spans.Add(new TextSpan(new string(' ', column - value.Width)));
        if (Kind == MeterKind.Composition) {
            int cells = Math.Max(4, width - column);
            foreach (TextSpan span in FrameText.Line(Bar(cells, context), context)) spans.Add(span);
        } else if (!string.IsNullOrEmpty(Note)) {
            foreach (TextSpan span in FrameText.Line("[muted]" + FrameText.Escape(Note) + "[/]", context)) spans.Add(span);
        }
        StyledLine second = new StyledLine(spans);
        if (second.Width > width) second = TextWrapper.Truncate(second, width, context.Ellipsis);
        return new[] { label, second };
    }

    private string Bar(int cells, RenderContext context) {
        long total = Healthy + Warning + Failing + Unknown + NotEvaluated;
        if (total <= 0) return "[subtle]" + new string(Shade(context), cells) + "[/]";
        // Problem shares keep at least one cell each, so one failure out of thousands is still visible.
        (HealthState state, long value)[] shares = {
            (HealthState.Failing, Failing), (HealthState.Warning, Warning), (HealthState.NotEvaluated, NotEvaluated), (HealthState.Unknown, Unknown)
        };
        int[] widths = new int[shares.Length];
        int used = 0;
        for (int i = 0; i < shares.Length; i++) {
            if (shares[i].value <= 0) continue;
            widths[i] = Math.Max(1, (int)Math.Round(cells * (double)shares[i].value / total, MidpointRounding.AwayFromZero));
            used += widths[i];
        }
        int healthy = Math.Max(0, cells - used);
        if (Healthy > 0 && healthy == 0 && used > 1) {
            int largest = 0;
            for (int i = 1; i < widths.Length; i++) { if (widths[i] > widths[largest]) largest = i; }
            widths[largest]--; healthy = 1;
        }
        if (Healthy == 0 && healthy > 0) {
            int largest = 0;
            for (int i = 1; i < widths.Length; i++) { if (widths[i] > widths[largest]) largest = i; }
            widths[largest] += healthy; healthy = 0;
        }
        bool color = context.HasColor;
        char block = context.Glyphs.Get(Glyphs.Glyph.BlockFull)[0];
        if (context.Glyphs.Tier == GlyphTier.Ascii) block = '#';
        string result = healthy > 0 ? "[subtle]" + new string(block, healthy) + "[/]" : string.Empty;
        if (!color) result = healthy > 0 ? new string(block, healthy) : string.Empty;
        for (int i = 0; i < shares.Length; i++) {
            if (widths[i] == 0) continue;
            HealthState state = shares[i].state;
            if (state == HealthState.Unknown) { result += "[subtle]" + new string(Shade(context), widths[i]) + "[/]"; continue; }
            // Without colour each problem share repeats its state glyph, so the bar still says what is wrong.
            string fill = color ? new string(block, widths[i]) : Repeat(FrameText.Glyph(state, context), widths[i]);
            result += "[" + FrameText.Tag(state) + "]" + Markup.Escape(fill) + "[/]";
        }
        return result;
    }

    private static char Shade(RenderContext context) => context.Glyphs.Tier == GlyphTier.Ascii ? '.' : context.Glyphs.Get(Glyphs.Glyph.ShadeLight)[0];

    private static string Repeat(string glyph, int count) {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++) text.Append(glyph);
        return text.ToString();
    }

    private enum MeterKind { Composition, Value }
}

/// <summary>
/// Lays meters out in fixed tiles: one line of as many tiles as the width allows, in declared priority order; below
/// 100 columns, up to four meters two per line.
/// </summary>
internal sealed class MeterRow : IWidget {
    private readonly IList<Meter> _all;
    private readonly FrameScale _scale;
    private IList<Meter> _meters;
    internal MeterRow(IList<Meter> meters, FrameScale scale) { _all = meters; _scale = scale; _meters = meters; }

    private int PerLine(int width) {
        int fit = Math.Max(1, (width + 2) / (Meter.TileWidth + 2));
        int per = _scale.Stacked ? Math.Min(2, fit) : Math.Min(_scale.MeterTiles, fit);
        int visible = _scale.Stacked ? Math.Min(_all.Count, 2 * per) : per;
        _meters = _all.Count <= visible ? _all : new List<Meter>(System.Linq.Enumerable.Take(_all, visible));
        return per;
    }

    public Size Measure(Constraints constraints, RenderContext context) {
        int per = PerLine(constraints.MaxWidth);
        if (_meters.Count == 0) return new Size(0, 0);
        int lines = (_meters.Count + per - 1) / per * 2;
        int tiles = Math.Min(per, _meters.Count);
        return new Size(Math.Min(constraints.MaxWidth, tiles * (Meter.TileWidth + 2) - 2), Math.Min(lines, constraints.MaxHeight));
    }

    public void Render(Region region, RenderContext context) {
        int per = PerLine(region.Width);
        for (int i = 0; i < _meters.Count; i++) {
            int x = (i % per) * (Meter.TileWidth + 2);
            int y = (i / per) * 2;
            int width = Math.Min(Meter.TileWidth, region.Width - x);
            if (width <= 0 || y + 1 >= region.Height) continue;
            StyledLine[] lines = _meters[i].Lines(width, context);
            region.Write(x, y, lines[0], width);
            region.Write(x, y + 1, lines[1], width);
        }
    }
}
