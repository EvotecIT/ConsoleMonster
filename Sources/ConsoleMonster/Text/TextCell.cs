namespace ConsoleMonster.Text {
    /// <summary>
    /// One user-perceived character (a base code point plus any zero-width combining marks) and the number
    /// of terminal cells it occupies (1 or 2).
    /// </summary>
    public readonly struct TextCell {
        /// <summary>Creates a text cell.</summary>
        /// <param name="glyph">The grapheme text.</param>
        /// <param name="width">Cell width, 1 or 2.</param>
        public TextCell(string glyph, int width) {
            Glyph = glyph;
            Width = width;
        }

        /// <summary>The grapheme text.</summary>
        public string Glyph { get; }

        /// <summary>Cell width, 1 or 2.</summary>
        public int Width { get; }
    }
}
