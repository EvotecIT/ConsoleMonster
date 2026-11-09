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
public sealed class ViewFact {
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
    /// <summary>The value's colour, or null for a neutral muted fact.</summary>
    public ThemeToken? Role { get; }
}

/// <summary>
/// The line under a default view that sums it up: <c>22 folders · 10 files · 117 KB</c>. Values are presented like table
/// cells (sizes, durations, dates), a sparkline or bar value draws inline, and lines break only between facts.
/// </summary>
public sealed class ViewFacts : IWidget {
    private const int Indent = 1;
    private const int MaxSparkline = 40;

    /// <summary>Creates an empty facts line.</summary>
    public ViewFacts() { }

    /// <summary>Adds a complete fact descriptor.</summary>
    /// <param name="fact">The fact.</param><returns>This line.</returns>
    public ViewFacts Add(ViewFact fact) {
        Facts.Add(fact ?? throw new ArgumentNullException(nameof(fact)));
        return this;
    }

    /// <summary>Adds a value and its literal label, with an optional semantic colour.</summary>
    /// <param name="value">The value, presented like a table cell.</param>
    /// <param name="label">The literal word after the value.</param>
    /// <param name="role">The semantic colour; null draws a neutral fact.</param>
    /// <returns>This facts line.</returns>
    public ViewFacts Add(object? value, string? label = null, ThemeToken? role = null) {
        Facts.Add(new ViewFact(value, label, role: role));
        return this;
    }

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
        TextSpan separator = new TextSpan(context.Glyphs.Tier == Capabilities.GlyphTier.Ascii ? " | " : " · ", muted);
        List<TextSpan> current = new List<TextSpan>();
        int used = 0;
        PresenterContext presenter = new PresenterContext(context);
        foreach (ViewFact fact in Facts) {
            StyledLine part = Present(fact, presenter, muted, context);
            if (part.Width == 0) { continue; }
            if (used > 0 && (fact.NewLine || used + DisplayWidth.Measure(separator.Text) + part.Width > room)) { Flush(); }
            if (used > 0) { current.Add(separator); used += DisplayWidth.Measure(separator.Text); }
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
        Style tone = context.Style(fact.Role ?? ThemeToken.Muted);
        // A sparkline fact is its shape: the caption and label say what it is, so its last value is not repeated.
        foreach (TextSpan span in value.Visual == CellVisualKind.Sparkline ? StyledLine.Empty : value.Line) {
            spans.Add(new TextSpan(span.Text, span.Style.Combine(value.Number.HasValue || fact.Label != null ? tone.With(TextDecoration.Bold) : tone)));
        }
        string visual = Visual(value, context, (fact.Role ?? ThemeToken.Muted).ToString().ToLowerInvariant());
        if (visual.Length > 0) {
            if (value.Line.Width > 0 && value.Visual != CellVisualKind.Sparkline) { spans.Add(new TextSpan(" ", Style.Plain)); }
            spans.AddRange(context.Markup(visual).Lines[0]);
        }
        if (!string.IsNullOrEmpty(fact.Label)) { spans.Add(new TextSpan(" " + fact.Label, tone)); }
        return new StyledLine(spans);
    }

    // Inline cell visuals use the same comparison and part-of-whole bar language as their full widgets.
    private static string Visual(PresentedValue value, RenderContext context, string role) {
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
                return BarDrawer.Markup(cells, value.Fraction, role, context, value.Visual == CellVisualKind.Progress,
                    value.Visual == CellVisualKind.Progress ? BarShape.Line : BarShape.Block);
            default:
                return string.Empty;
        }
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
