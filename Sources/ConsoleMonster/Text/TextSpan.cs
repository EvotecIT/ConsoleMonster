using System;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Text {
    /// <summary>A run of text with one style. Spans never contain line breaks once inside a <see cref="StyledLine"/>.</summary>
    public readonly struct TextSpan : IEquatable<TextSpan> {
        /// <summary>Creates a span.</summary>
        /// <param name="text">The text.</param>
        /// <param name="style">The style.</param>
        public TextSpan(string text, Style style = default) {
            Text = text ?? string.Empty;
            Style = style;
        }

        /// <summary>The text.</summary>
        public string Text { get; }

        /// <summary>The style.</summary>
        public Style Style { get; }

        /// <summary>Display width in terminal cells.</summary>
        public int Width => DisplayWidth.Measure(Text);

        /// <inheritdoc />
        public bool Equals(TextSpan other) => string.Equals(Text, other.Text, StringComparison.Ordinal) && Style.Equals(other.Style);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is TextSpan other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() {
            unchecked {
                return ((Text?.GetHashCode() ?? 0) * 397) ^ Style.GetHashCode();
            }
        }

        /// <inheritdoc />
        public override string ToString() => Text;
    }
}
