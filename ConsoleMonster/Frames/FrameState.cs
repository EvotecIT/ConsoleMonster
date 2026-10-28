using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
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

    // The frame's states reuse the library vocabulary: the health scale for healthy, warning, failing and unknown,
    // and the assessment scale for critical and could-not-evaluate, so every tier keeps one glyph per meaning.
    internal static ThemeToken Role(HealthState state) {
        switch (state) {
            case HealthState.Critical: return StatusScale.Role(Severity.Critical);
            case HealthState.Failing: return StatusKind.Fail.ToToken();
            case HealthState.Warning: return StatusKind.Warn.ToToken();
            case HealthState.NotEvaluated: return StatusScale.Role(Severity.CouldNotEvaluate);
            case HealthState.Unknown: return ThemeToken.Muted;
            default: return StatusKind.Pass.ToToken();
        }
    }

    internal static string Tag(HealthState state) => Role(state).ToString().ToLowerInvariant();

    internal static string Glyph(HealthState state, RenderContext context) {
        switch (state) {
            case HealthState.Critical: return StatusScale.Marker(Severity.Critical, context.Glyphs.Tier);
            case HealthState.Failing: return context.Glyphs.StatusSymbol(StatusKind.Fail);
            case HealthState.Warning: return context.Glyphs.StatusSymbol(StatusKind.Warn);
            case HealthState.NotEvaluated: return StatusScale.Marker(Severity.CouldNotEvaluate, context.Glyphs.Tier);
            case HealthState.Unknown: return context.Glyphs.StatusMarker(StatusKind.Unknown);
            default: return context.Glyphs.StatusSymbol(StatusKind.Pass);
        }
    }
    internal static string Dot(RenderContext context) => context.Glyphs.Get(Glyphs.Glyph.Dot);

    internal static string Arrow(RenderContext context) => context.Glyphs.Get(Glyphs.Glyph.ArrowLeft);

    internal static string Escape(string? text) => Markup.Escape(text ?? string.Empty);

    private static readonly KeyValuePair<string, string>[] AsciiFolds = {
        new KeyValuePair<string, string>("←", "<-"), new KeyValuePair<string, string>("→", "->"), new KeyValuePair<string, string>("↔", "<>"),
        new KeyValuePair<string, string>("·", "-"), new KeyValuePair<string, string>("—", "-"), new KeyValuePair<string, string>("–", "-"),
        new KeyValuePair<string, string>("…", "..."), new KeyValuePair<string, string>("\u00A0", " "), new KeyValuePair<string, string>("\u202F", " "),
        new KeyValuePair<string, string>("\u2019", "'"), new KeyValuePair<string, string>("\u2018", "'"), new KeyValuePair<string, string>("\u201C", "\""),
        new KeyValuePair<string, string>("\u201D", "\""), new KeyValuePair<string, string>("\u2009", " ")
    };

    /// <summary>
    /// Folds the common typographic characters of literal product text for the ASCII tier, before measuring, so an
    /// author's <c>DC01 ← DC02</c> or <c>a · b</c> never puts a non-ASCII character into ASCII output.
    /// </summary>
    internal static string Fold(string text, RenderContext context) {
        if (context.Glyphs.Tier != Capabilities.GlyphTier.Ascii || string.IsNullOrEmpty(text)) return text;
        foreach (KeyValuePair<string, string> fold in AsciiFolds) text = text.Replace(fold.Key, fold.Value);
        // Anything left outside printable ASCII (culture separators, letters with accents) becomes '?', never a raw byte.
        System.Text.StringBuilder? ascii = null;
        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            if (c < 127 && (c >= ' ' || c == '\n')) { ascii?.Append(c); continue; }
            if (ascii == null) ascii = new System.Text.StringBuilder(text, 0, i, text.Length);
            if (char.IsHighSurrogate(c) && i + 1 < text.Length) i++;
            ascii.Append('?');
        }
        return ascii?.ToString() ?? text;
    }

    internal static StyledLine Line(string markup, RenderContext context) {
        IReadOnlyList<StyledLine> lines = context.Markup(Fold(markup, context)).Lines;
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

    internal static string Count(long value, string singular, string? plural, RenderContext context) => Number(value, context) + " " + (value == 1 ? singular : plural ?? singular + "s");

    internal static string Number(long value, RenderContext context) => value.ToString("N0", context.Culture ?? CultureInfo.CurrentCulture);
}
