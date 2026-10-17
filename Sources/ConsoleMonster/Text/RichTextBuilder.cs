using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Text {
    /// <summary>
    /// Builds <see cref="RichText"/> fluently:
    /// <c>new RichTextBuilder().Append("SPF ", ConsoleColor.Gray).Append("PASS", ThemeToken.Pass, TextDecoration.Bold).Build()</c>.
    /// </summary>
    public sealed class RichTextBuilder {
        private readonly List<RichSegment> _segments = new List<RichSegment>();

        /// <summary>Appends literal text.</summary>
        /// <param name="text">The text (shown as written).</param>
        /// <param name="style">The style.</param>
        /// <returns>This builder.</returns>
        public RichTextBuilder Append(string? text, RichStyle style = default) {
            _segments.Add(new RichSegment(text ?? string.Empty, style, false));
            return this;
        }

        /// <summary>Appends literal text with a foreground colour, decorations and an optional background.</summary>
        /// <param name="text">The text.</param>
        /// <param name="foreground">The foreground (a ConsoleColor, theme role or colour converts implicitly).</param>
        /// <param name="decoration">Decorations.</param>
        /// <param name="background">The background.</param>
        /// <returns>This builder.</returns>
        public RichTextBuilder Append(string? text, RichColor foreground, TextDecoration decoration = TextDecoration.None, RichColor background = default) {
            return Append(text, new RichStyle(foreground, background, decoration));
        }

        /// <summary>Appends a hyperlink (OSC 8 where the terminal supports it, otherwise the text).</summary>
        /// <param name="text">The text.</param>
        /// <param name="url">The target.</param>
        /// <param name="style">The style.</param>
        /// <returns>This builder.</returns>
        public RichTextBuilder AppendLink(string? text, string url, RichStyle style = default) => Append(text, style.WithLink(url));

        /// <summary>Appends markup.</summary>
        /// <param name="markup">The markup.</param>
        /// <returns>This builder.</returns>
        public RichTextBuilder AppendMarkup(string? markup) {
            _segments.AddRange(RichText.FromMarkup(markup).Segments);
            return this;
        }

        /// <summary>Appends other rich text.</summary>
        /// <param name="text">The text.</param>
        /// <returns>This builder.</returns>
        public RichTextBuilder Append(RichText? text) {
            if (text != null) {
                _segments.AddRange(text.Segments);
            }
            return this;
        }

        /// <summary>Appends a line break.</summary>
        /// <returns>This builder.</returns>
        public RichTextBuilder AppendLine() => Append("\n");

        /// <summary>Creates the rich text.</summary>
        /// <returns>The rich text.</returns>
        public RichText Build() => new RichText(_segments);
    }
}
