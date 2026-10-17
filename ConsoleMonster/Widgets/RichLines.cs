using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>Shared helpers for widgets whose text is literal by default but may carry <see cref="RichText"/>.</summary>
    internal static class RichLines {
        /// <summary>
        /// One styled line: the rich text over <paramref name="baseStyle"/> (its colours win, line breaks become spaces), or
        /// the literal fallback in the base style.
        /// </summary>
        internal static StyledLine Line(RichText? rich, string? fallback, Style baseStyle, RenderContext context) {
            if (rich == null) {
                return StyledLine.From(fallback ?? string.Empty, baseStyle);
            }
            StyledText text = rich.ToStyledText(context);
            if (text.Lines.Count == 1) {
                return text.Lines[0].WithBaseStyle(baseStyle);
            }
            List<TextSpan> spans = new List<TextSpan>();
            for (int i = 0; i < text.Lines.Count; i++) {
                if (i > 0) {
                    spans.Add(new TextSpan(" ", baseStyle));
                }
                spans.AddRange(text.Lines[i].WithBaseStyle(baseStyle));
            }
            return new StyledLine(spans);
        }

        /// <summary>Rich text from live-event text: markup when flagged and valid, otherwise literal.</summary>
        internal static RichText FromEventText(string text, bool isMarkup) {
            return isMarkup && Markup.Validate(text, out _) ? RichText.FromMarkup(text) : RichText.Plain(text);
        }
    }
}
