using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Frames;

/// <summary>The health and state scale for inventory frames: one glyph and one role per state in every tier.</summary>
public enum HealthState {
    /// <summary>Healthy or current (<c>✓</c>).</summary>
    Healthy,
    /// <summary>A value that was not reported (<c>◌</c>); never shown as zero.</summary>
    Unknown,
    /// <summary>Could not be evaluated, for example a target that did not answer (<c>?</c>, violet).</summary>
    NotEvaluated,
    /// <summary>Needs attention soon (<c>▴</c>).</summary>
    Warning,
    /// <summary>Failing (<c>×</c>).</summary>
    Failing,
    /// <summary>Failing with the highest urgency (<c>◆</c>).</summary>
    Critical
}

/// <summary>One state fact on a title line: glyph, count and word, coloured by its state.</summary>
public readonly struct FrameState {
    private FrameState(HealthState state, long? count, string word) { State = state; Count = count; Word = word ?? string.Empty; }

    /// <summary>A counted state, such as <c>× 2 failing</c>. Zero counts are dropped by the frame.</summary>
    /// <param name="state">The state.</param><param name="count">The count.</param><param name="word">The literal word.</param><returns>The state fact.</returns>
    public static FrameState Of(HealthState state, long count, string word) => new FrameState(state, count, word);

    /// <summary>A single healthy verdict, such as <c>✓ all healthy</c>.</summary>
    /// <param name="word">The literal verdict.</param><returns>The state fact.</returns>
    public static FrameState Healthy(string word) => new FrameState(HealthState.Healthy, null, word);

    /// <summary>The state.</summary>
    public HealthState State { get; }
    /// <summary>The count, or null for a verdict.</summary>
    public long? Count { get; }
    /// <summary>The literal word.</summary>
    public string Word { get; }

    internal bool IsEmpty => Count.HasValue && Count.Value == 0;

    internal string ToMarkup(RenderContext context) {
        string glyph = FrameText.Glyph(State, context);
        string role = FrameText.Tag(State);
        string text = ((Count.HasValue ? Count.Value.ToString("N0", context.Culture) + " " : string.Empty) + Word).Trim();
        return State == HealthState.Healthy
            ? "[" + role + "]" + glyph + "[/] " + Markup.Escape(text)
            : "[" + role + "]" + glyph + " " + Markup.Escape(text) + "[/]";
    }

    /// <summary>Joins states worst first, dropping zero counts.</summary>
    internal static string Join(IEnumerable<FrameState> states, RenderContext context) {
        List<FrameState> list = new List<FrameState>();
        foreach (FrameState state in states) { if (!state.IsEmpty) list.Add(state); }
        list.Sort((a, b) => FrameText.Rank(b.State).CompareTo(FrameText.Rank(a.State)));
        List<string> parts = new List<string>();
        foreach (FrameState state in list) parts.Add(state.ToMarkup(context));
        return string.Join(" [muted]" + FrameText.Dot(context) + "[/] ", parts);
    }
}

internal static class FrameText {
    internal static int Rank(HealthState state) {
        switch (state) {
            case HealthState.Critical: return 5;
            case HealthState.Failing: return 4;
            case HealthState.Warning: return 3;
            case HealthState.NotEvaluated: return 2;
            case HealthState.Unknown: return 1;
            default: return 0;
        }
    }

    internal static ThemeToken Role(HealthState state) {
        switch (state) {
            case HealthState.Critical: return ThemeToken.Critical;
            case HealthState.Failing: return ThemeToken.Fail;
            case HealthState.Warning: return ThemeToken.Warn;
            case HealthState.NotEvaluated: return ThemeToken.Gap;
            case HealthState.Unknown: return ThemeToken.Muted;
            default: return ThemeToken.Pass;
        }
    }

    internal static string Tag(HealthState state) => Role(state).ToString().ToLowerInvariant();

    internal static string Glyph(HealthState state, RenderContext context) {
        GlyphTier tier = context.Glyphs.Tier;
        switch (state) {
            case HealthState.Critical: return tier == GlyphTier.Full ? "◆" : tier == GlyphTier.Basic ? "♦" : "*";
            case HealthState.Failing: return tier == GlyphTier.Ascii ? "x" : "×";
            case HealthState.Warning: return tier == GlyphTier.Full ? "▴" : "!";
            case HealthState.NotEvaluated: return "?";
            case HealthState.Unknown: return tier == GlyphTier.Full ? "◌" : tier == GlyphTier.Basic ? "○" : "o";
            default: return tier == GlyphTier.Full ? "✓" : tier == GlyphTier.Basic ? "√" : "+";
        }
    }

    internal static string Dot(RenderContext context) => context.Glyphs.Tier == GlyphTier.Ascii ? "-" : "·";

    internal static string Arrow(RenderContext context) => context.Glyphs.Tier == GlyphTier.Ascii ? "<-" : "←";

    internal static string Escape(string? text) => Markup.Escape(text ?? string.Empty);

    internal static StyledLine Line(string markup, RenderContext context) {
        IReadOnlyList<StyledLine> lines = context.Markup(markup).Lines;
        return lines.Count == 0 ? StyledLine.From(string.Empty) : lines[0];
    }

    internal static int Width(string markup, RenderContext context) => Line(markup, context).Width;

    /// <summary>Fits a line to an exact width: truncated with the tier ellipsis, or padded on the given side.</summary>
    internal static List<TextSpan> Fit(StyledLine line, int width, bool right, RenderContext context) {
        List<TextSpan> spans = new List<TextSpan>();
        if (width <= 0) return spans;
        if (line.Width > width) line = TextWrapper.Truncate(line, width, context.Ellipsis);
        int pad = width - line.Width;
        if (right && pad > 0) spans.Add(new TextSpan(new string(' ', pad)));
        foreach (TextSpan span in line) spans.Add(span);
        if (!right && pad > 0) spans.Add(new TextSpan(new string(' ', pad)));
        return spans;
    }

    internal static string Number(long value, RenderContext context) => value.ToString("N0", context.Culture ?? CultureInfo.CurrentCulture);
}
