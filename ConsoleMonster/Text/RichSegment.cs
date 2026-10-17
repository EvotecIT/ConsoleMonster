using System;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Text {
    /// <summary>One span of <see cref="RichText"/>: literal text or markup, with a style.</summary>
    public readonly struct RichSegment : IEquatable<RichSegment> {
        /// <summary>Creates a segment.</summary>
        /// <param name="text">The text.</param>
        /// <param name="style">The style (for markup, a style around the whole markup).</param>
        /// <param name="isMarkup">Whether the text is markup; literal text is never interpreted.</param>
        public RichSegment(string text, RichStyle style = default, bool isMarkup = false) {
            Text = text ?? string.Empty;
            Style = style;
            IsMarkup = isMarkup;
        }

        /// <summary>The text.</summary>
        public string Text { get; }

        /// <summary>The style.</summary>
        public RichStyle Style { get; }

        /// <summary>Whether <see cref="Text"/> is markup.</summary>
        public bool IsMarkup { get; }

        /// <inheritdoc />
        public bool Equals(RichSegment other) => string.Equals(Text, other.Text, StringComparison.Ordinal) && Style.Equals(other.Style) && IsMarkup == other.IsMarkup;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is RichSegment other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (StringComparer.Ordinal.GetHashCode(Text) * 397) ^ Style.GetHashCode() ^ (IsMarkup ? 1 : 0);

        /// <inheritdoc />
        public override string ToString() => Text;
    }
}
