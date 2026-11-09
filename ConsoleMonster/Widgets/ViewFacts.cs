using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets;

/// <summary>One fact under a view: an optional caption, the value (presented like a table cell) and an optional label.</summary>
internal sealed class ViewFact {
    /// <summary>Creates a fact.</summary>
    public ViewFact(object? value, string? label = null, string? caption = null, ICellPresenter? presenter = null, ThemeToken? role = null, bool newLine = false) {
        Value = value; Label = label; Caption = caption; Presenter = presenter; Role = role; NewLine = newLine;
    }
    /// <summary>Starts a new line (one line per destination, for example).</summary>
    public bool NewLine { get; }
    /// <summary>Literal text before the value ("largest").</summary>
    public string? Caption { get; }
    /// <summary>The value, presented by <see cref="Presenter"/> or automatically.</summary>
    public object? Value { get; }
    /// <summary>Literal text after the value ("files").</summary>
    public string? Label { get; }
    /// <summary>The presenter, or null for automatic.</summary>
    public ICellPresenter? Presenter { get; }
    /// <summary>The value's colour, or null for the text colour.</summary>
    public ThemeToken? Role { get; }
}

/// <summary>
/// The line under a default view that sums it up: <c>22 folders · 10 files · 117 KB</c>. Values are presented like table
/// cells (sizes, durations, dates), a sparkline or bar value draws inline, and lines break only between facts.
/// </summary>
internal sealed class ViewFacts : IWidget {
    private const int Indent = 1;
    private const int MaxSparkline = 40;

    /// <summary>Creates the line from facts.</summary>
    public ViewFacts(IEnumerable<ViewFact> facts) {
        if (facts == null) { throw new ArgumentNullException(nameof(facts)); }
        Facts.AddRange(facts);
    }

    /// <summary>The facts in display order.</summary>
    public List<ViewFact> Facts { get; } = new List<ViewFact>();

    internal List<StyledLine> Lines(int width, RenderContext context) {
        List<StyledLine> lines = new List<StyledLine>();
        int room = Math.Max(1, width - Indent);
        Style muted = context.Style(ThemeToken.Muted);
        TextSpan separator = new TextSpan(" " + context.Glyphs.Get(Glyph.Dot) + " ", muted);
        List<TextSpan> current = new List<TextSpan>();
        int used = 0;
        PresenterContext presenter = new PresenterContext(context);
        foreach (ViewFact fact in Facts) {
            StyledLine part = Present(fact, presenter, muted, context);
            if (part.Width == 0) { continue; }
            if (used > 0 && (fact.NewLine || used + separator.Text.Length + part.Width > room)) { Flush(); }
            if (used > 0) { current.Add(separator); used += separator.Text.Length; }
            current.AddRange(part);
            used += part.Width;
        }
        Flush();
        return lines;

        void Flush() {
            if (current.Count == 0) { return; }
            foreach (StyledLine line in TextWrapper.Wrap(new StyledLine(current), room)) {
                List<TextSpan> indented = new List<TextSpan> { new TextSpan(new string(' ', Indent), Style.Plain) };
                indented.AddRange(line);
                lines.Add(new StyledLine(indented));
            }
            current = new List<TextSpan>();
            used = 0;
        }
    }

    private static StyledLine Present(ViewFact fact, PresenterContext presenter, Style muted, RenderContext context) {
        List<TextSpan> spans = new List<TextSpan>();
        if (!string.IsNullOrEmpty(fact.Caption)) { spans.Add(new TextSpan(fact.Caption + " ", muted)); }
        PresentedValue value = CellPresenters.Present(fact.Value, fact.Presenter, null, presenter);
        Style? tone = fact.Role.HasValue ? context.Style(fact.Role.Value) : (Style?)null;
        // A sparkline fact is its shape: the caption and label say what it is, so its last value is not repeated.
        foreach (TextSpan span in value.Visual == CellVisualKind.Sparkline ? StyledLine.Empty : value.Line) {
            spans.Add(tone.HasValue ? new TextSpan(span.Text, span.Style.WithColor(tone.Value.Color).With(TextDecoration.Bold)) : span);
        }
        string visual = Visual(value, context);
        if (visual.Length > 0) {
            if (value.Line.Width > 0 && value.Visual != CellVisualKind.Sparkline) { spans.Add(new TextSpan(" ", Style.Plain)); }
            spans.Add(new TextSpan(visual, tone ?? context.Style(ThemeToken.Accent)));
        }
        if (!string.IsNullOrEmpty(fact.Label)) { spans.Add(new TextSpan(" " + fact.Label, muted)); }
        return new StyledLine(spans);
    }

    // The cell visuals as text: a sparkline in the tier's vertical ramp, a bar in eighth blocks over a light shade.
    private static string Visual(PresentedValue value, RenderContext context) {
        switch (value.Visual) {
            case CellVisualKind.Sparkline:
                IReadOnlyList<double>? series = value.Series;
                if (series == null || series.Count < 2) { return string.Empty; }
                int start = Math.Max(0, series.Count - MaxSparkline);
                double min = double.MaxValue, max = double.MinValue;
                for (int i = start; i < series.Count; i++) { min = Math.Min(min, series[i]); max = Math.Max(max, series[i]); }
                IReadOnlyList<string> ramp = context.Glyphs.VerticalRamp;
                System.Text.StringBuilder text = new System.Text.StringBuilder();
                for (int i = start; i < series.Count; i++) {
                    double fraction = max > min ? (series[i] - min) / (max - min) : 0.5;
                    int index = 1 + (int)Math.Round(fraction * (ramp.Count - 2));
                    text.Append(ramp[Math.Max(1, Math.Min(ramp.Count - 1, index))]);
                }
                return text.ToString();
            case CellVisualKind.Bar:
            case CellVisualKind.Progress:
                int cells = Math.Max(4, Math.Min(12, value.VisualWidth));
                double amount = cells * (double.IsNaN(value.Fraction) ? 0 : Math.Max(0, Math.Min(1, value.Fraction)));
                int eighths = (int)Math.Round(amount * 8, MidpointRounding.AwayFromZero);
                int whole = Math.Min(cells, eighths / 8);
                string partial = whole < cells ? context.Glyphs.HorizontalFill(eighths % 8) : string.Empty;
                int rest = cells - whole - (partial.Length > 0 ? 1 : 0);
                return Repeat(context.Glyphs.Get(Glyph.BlockFull), whole) + partial + Repeat(context.Glyphs.Get(Glyph.ShadeLight), Math.Max(0, rest));
            default:
                return string.Empty;
        }
    }

    private static string Repeat(string glyph, int count) {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++) { text.Append(glyph); }
        return text.ToString();
    }

    /// <inheritdoc />
    public Size Measure(Constraints constraints, RenderContext context) {
        List<StyledLine> lines = Lines(constraints.MaxWidth, context);
        int width = 0;
        foreach (StyledLine line in lines) { width = Math.Max(width, line.Width); }
        return new Size(width, Math.Min(lines.Count, constraints.MaxHeight));
    }

    /// <inheritdoc />
    public void Render(Region region, RenderContext context) {
        List<StyledLine> lines = Lines(region.Width, context);
        for (int y = 0; y < lines.Count && y < region.Height; y++) { region.Write(0, y, lines[y]); }
    }
}
