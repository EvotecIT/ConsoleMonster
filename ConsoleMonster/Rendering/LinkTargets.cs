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
        internal static string? Suffix(string label, string link) {
            string address = TextSanitizer.Clean(link);
            string text = label.Trim();
            return string.Equals(text, address, StringComparison.Ordinal) || string.Equals("mailto:" + text, address, StringComparison.Ordinal)
                ? null : " (" + address + ")";
        }

        /// <summary>Adds each link's target after its label when <see cref="AsText"/> holds for the context.</summary>
        internal static StyledText Show(StyledText text, RenderContext context) {
            if (!AsText(context.Profile)) {
                return text;
            }
            bool changed = false;
            List<StyledLine> lines = new List<StyledLine>(text.Lines.Count);
            StringBuilder label = new StringBuilder();
            foreach (StyledLine line in text.Lines) {
                List<TextSpan>? spans = null;
                for (int i = 0; i < line.Count; i++) {
                    TextSpan span = line[i];
                    spans?.Add(span);
                    string? link = span.Style.Link;
                    if (link == null) {
                        continue;
                    }
                    label.Append(span.Text);
                    if (i + 1 < line.Count && string.Equals(line[i + 1].Style.Link, link, StringComparison.Ordinal)) {
                        continue;
                    }
                    string? suffix = Suffix(label.ToString(), link);
                    label.Clear();
                    if (suffix == null) {
                        continue;
                    }
                    if (spans == null) {
                        spans = new List<TextSpan>(line.Count + 1);
                        for (int j = 0; j <= i; j++) {
                            spans.Add(line[j]);
                        }
                    }
                    spans.Add(new TextSpan(suffix, context.Style(ThemeToken.Muted)));
                }
                changed |= spans != null;
                lines.Add(spans == null ? line : new StyledLine(spans));
            }
            return changed ? new StyledText(lines) : text;
        }
    }
}
