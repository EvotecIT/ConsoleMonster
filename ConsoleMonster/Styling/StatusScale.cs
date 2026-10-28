using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Text;

namespace ConsoleMonster.Styling;

/// <summary>Assessment severity glyphs and roles. Health statuses keep their separate scale.</summary>
public static class StatusScale {
    /// <summary>Returns the semantic role for an assessment outcome.</summary>
    public static ThemeToken Role(Severity severity) => severity switch {
        Severity.Critical => ThemeToken.Critical,
        Severity.High => ThemeToken.Fail,
        Severity.Medium => ThemeToken.Warn,
        Severity.Low => ThemeToken.Low,
        Severity.Info => ThemeToken.Info,
        Severity.Passed => ThemeToken.Pass,
        Severity.CouldNotEvaluate => ThemeToken.Gap,
        _ => ThemeToken.Muted
    };

    /// <summary>Returns the glyph before measurement for the destination tier.</summary>
    public static string Marker(Severity severity, GlyphTier tier) {
        int index = (int)severity;
        if (index < 0 || index > (int)Severity.Unknown) { throw new ArgumentOutOfRangeException(nameof(severity)); }
        string[] glyphs = tier == GlyphTier.Full
            ? new[] { "◆", "▲", "▴", "▵", "•", "✓", "?", "–", "◌" }
            : tier == GlyphTier.Basic
                ? new[] { "♦", "▲", "!", "◊", "•", "√", "?", "–", "○" }
                : new[] { "X", "x", "!", "~", "i", "+", "?", "-", "o" };
        return glyphs[index];
    }

    /// <summary>Returns a lower-case default word for an assessment outcome.</summary>
    public static string Word(Severity severity) => severity switch {
        Severity.CouldNotEvaluate => "could not evaluate",
        Severity.NotEvaluated => "not evaluated",
        _ => severity.ToString().ToLowerInvariant()
    };

    /// <summary>Formats a literal caller word with its severity glyph and semantic style.</summary>
    public static string Format(Severity severity, GlyphTier tier, string? word = null) =>
        "[" + Role(severity).ToString().ToLowerInvariant() + "]" + Marker(severity, tier) + (severity == Severity.Passed ? "[/] [text]" : " ") + Markup.Escape(word ?? Word(severity)) + "[/]";
}
