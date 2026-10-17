using ConsoleMonster.Text;

namespace ConsoleMonster.Rendering {
    /// <summary>
    /// Decides whether every terminal draws a glyph exactly as wide as measured. Printable ASCII always qualifies; box
    /// drawing, block elements, shades and braille qualify while the active width profile treats East Asian Ambiguous
    /// characters as one cell. Everything else (emoji, ZWJ sequences, flags, CJK, ambiguous symbols such as ▲ or ●) is
    /// treated as uncertain, and VT output re-anchors the cursor after it.
    /// </summary>
    internal static class GlyphTrust {
        internal static bool IsTrusted(string glyph) {
            if (string.IsNullOrEmpty(glyph) || glyph.Length != 1) {
                return false;
            }
            char c = glyph[0];
            if (c >= 0x20 && c < 0x7F) {
                return true;
            }
            if (c >= 0x2800 && c <= 0x28FF) {
                return true;
            }
            return c >= 0x2500 && c <= 0x259F && AmbiguousIsNarrow();
        }

        private static bool AmbiguousIsNarrow() => DisplayWidth.Profile.AmbiguousWidth == 1;
    }
}
