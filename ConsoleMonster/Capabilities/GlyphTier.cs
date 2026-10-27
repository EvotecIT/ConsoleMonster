namespace ConsoleMonster.Capabilities {

    /// <summary>
    /// Which glyphs the terminal font can show reliably. Widgets ask the glyph set for semantic glyphs
    /// and never hard-code characters, so every widget degrades through these tiers automatically.
    /// </summary>
    public enum GlyphTier {

        /// <summary>Printable ASCII only: <c>+-|#*.</c>, <c>[OK]</c>/<c>[FAIL]</c> words.</summary>
        Ascii = 0,

        /// <summary>Box drawing, shades and half blocks covered by Consolas and Lucida Console.</summary>
        Basic = 1,

        /// <summary>Braille, eighth blocks and rounded corners (Cascadia, JetBrains Mono, DejaVu and similar).</summary>
        Full = 2
    }
}
