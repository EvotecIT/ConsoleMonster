using System;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Canvas {
    /// <summary>
    /// One terminal cell: a grapheme, its style and its width. A wide grapheme occupies two cells; the second
    /// is a continuation cell with <see cref="Width"/> 0 and an empty glyph.
    /// </summary>
    public readonly struct Cell : IEquatable<Cell> {
        /// <summary>Creates a cell.</summary>
        /// <param name="glyph">The grapheme (a single space for blank).</param>
        /// <param name="style">The style.</param>
        /// <param name="width">1 for normal, 2 for the first half of a wide grapheme, 0 for a continuation.</param>
        public Cell(string glyph, Style style, byte width = 1) {
            Glyph = glyph;
            Style = style;
            Width = width;
        }

        /// <summary>A blank cell with the plain style.</summary>
        public static Cell Blank { get; } = new Cell(" ", Style.Plain, 1);

        /// <summary>The grapheme; never null for cells read from a buffer.</summary>
        public string Glyph { get; }

        /// <summary>The style.</summary>
        public Style Style { get; }

        /// <summary>1 for normal, 2 for the first half of a wide grapheme, 0 for a continuation cell.</summary>
        public byte Width { get; }

        /// <summary>Whether this is the second half of a wide grapheme.</summary>
        public bool IsContinuation => Width == 0;

        /// <inheritdoc />
        public bool Equals(Cell other) {
            return Width == other.Width && Style.Equals(other.Style) && string.Equals(Glyph, other.Glyph, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Cell other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() {
            unchecked {
                return ((Glyph?.GetHashCode() ?? 0) * 397) ^ Style.GetHashCode() ^ Width;
            }
        }

        /// <summary>Equality operator.</summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <returns>Whether equal.</returns>
        public static bool operator ==(Cell left, Cell right) => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <returns>Whether different.</returns>
        public static bool operator !=(Cell left, Cell right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => Glyph;
    }
}
