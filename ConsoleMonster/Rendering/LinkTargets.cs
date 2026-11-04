using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Rendering {
    /// <summary>Keeps link targets readable where the output cannot make links clickable.</summary>
    internal static class LinkTargets {
        /// <summary>
        /// Whether links show their target as text: plain, stream, redirected and CI output, and detected terminals without
        /// OSC 8 (the classic console). Fixed profiles (exports, tests) keep the link on the cells for the HTML and SVG writers.
        /// </summary>
        internal static bool AsText(TerminalProfile profile) => !profile.SupportsHyperlinks
            && (profile.Mode != OutputMode.Interactive || profile.IsOutputRedirected || profile.IsContinuousIntegration
                || profile.Kind != TerminalKind.Unknown);

        /// <summary>The text that follows a link's label, <c> (url)</c>, or null when the label already is the target.</summary>
        internal static string? Suffix(string label, string link, GlyphTier tier = GlyphTier.Full) {
            string original = TextSanitizer.Clean(link);
            string address = HyperlinkTarget.ForText(link, tier);
            string text = label.Trim();
            return string.Equals(text, original, StringComparison.Ordinal) || string.Equals(text, address, StringComparison.Ordinal)
                || string.Equals("mailto:" + text, original, StringComparison.Ordinal)
                ? null : " (" + address + ")";
        }

        // A label that is itself the resource address must be escaped too; a human caption remains prose.
        internal static string? PrintedLabel(string label, string link, GlyphTier tier) {
            if (tier == GlyphTier.Full) return null;
            string original = TextSanitizer.Clean(link);
            string text = label.Trim();
            if (!string.Equals(text, original, StringComparison.Ordinal) && !string.Equals("mailto:" + text, original, StringComparison.Ordinal)) return null;
            string encoded = HyperlinkTarget.ForText(link, tier);
            if (encoded == original) return null;
            int start = label.IndexOf(text, StringComparison.Ordinal);
            return label.Substring(0, start) + encoded + label.Substring(start + text.Length);
        }

        /// <summary>
        /// Adds each link's target after its label when <see cref="AsText"/> holds for the context. A terminal with OSC 8
        /// also gets the target as text for a link it cannot open, such as a bare relative path (<c>docs/report.pdf</c>).
        /// </summary>
        internal static StyledText Show(StyledText text, RenderContext context) {
            bool asText = AsText(context.Profile);
            if (!asText && !context.Profile.SupportsHyperlinks) {
                return text;
            }
            bool changed = false;
            List<StyledLine> lines = new List<StyledLine>(text.Lines.Count);
            StringBuilder label = new StringBuilder();
            foreach (StyledLine line in text.Lines) {
                List<TextSpan>? spans = null;
                int runSource = 0;
                int runOutput = 0;
                for (int i = 0; i < line.Count; i++) {
                    TextSpan span = line[i];
                    if (i == 0 || !string.Equals(line[i - 1].Style.Link, span.Style.Link, StringComparison.Ordinal)) {
                        runSource = i;
                        runOutput = spans?.Count ?? i;
                    }
                    spans?.Add(span);
                    string? link = span.Style.Link;
                    if (link == null) {
                        continue;
                    }
                    label.Append(span.Text);
                    if (i + 1 < line.Count && string.Equals(line[i + 1].Style.Link, link, StringComparison.Ordinal)) {
                        continue;
                    }
                    // The same decision the terminal encoder makes: a target it can emit stays a clickable link, and a label
                    // that already shows the target (a bare link split across lines) needs nothing more.
                    string shown = label.ToString();
                    string? suffix = asText || (!(HyperlinkTarget.IsSafe(link) || HyperlinkTarget.From(link) != null)
                        && !(link.IndexOf('\n') >= 0 && link.IndexOf(shown.Trim(), StringComparison.Ordinal) >= 0)) ? Suffix(shown, link, context.Glyphs.Tier) : null;
                    string? printed = PrintedLabel(shown, link, context.Glyphs.Tier);
                    label.Clear();
                    if (suffix == null && printed == null) {
                        continue;
                    }
                    if (spans == null) {
                        spans = new List<TextSpan>(line.Count + 1);
                        for (int j = 0; j <= i; j++) {
                            spans.Add(line[j]);
                        }
                    }
                    if (printed != null) {
                        spans.RemoveRange(runOutput, spans.Count - runOutput);
                        spans.Add(new TextSpan(printed, line[runSource].Style));
                    }
                    if (suffix != null) spans.Add(new TextSpan(suffix, context.Style(ThemeToken.Muted)));
                }
                changed |= spans != null;
                lines.Add(spans == null ? line : new StyledLine(spans));
            }
            return changed ? new StyledText(lines) : text;
        }
    }
}
