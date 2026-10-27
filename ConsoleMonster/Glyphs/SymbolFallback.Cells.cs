using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Text;

namespace ConsoleMonster.Glyphs {
    public static partial class SymbolFallback {
        /// <summary>
        /// Returns a single-cell decorative fallback in Basic/ASCII, or the original symbol in Full.
        /// Unlike prose substitution, this never returns badges such as [OK] or [doc] and never truncates a token.
        /// Unknown reduced-tier symbols use *. Ordinary prose should use <see cref="Apply(string, GlyphTier)"/>.
        /// </summary>
        /// <param name="symbol">The emoji or symbol used as a cell glyph.</param>
        /// <param name="tier">The destination glyph tier.</param>
        /// <param name="widthProfile">The destination width model, or the current model.</param>
        /// <returns>A complete glyph; reduced-tier glyphs occupy exactly one destination cell.</returns>
        public static string GetGlyph(string symbol, GlyphTier tier, TerminalWidthProfile? widthProfile = null) {
            if (symbol == null) throw new ArgumentNullException(nameof(symbol));
            if (tier == GlyphTier.Full) return symbol;
            TerminalWidthProfile profile = widthProfile ?? DisplayWidth.Profile;
            if (_map.TryGetValue(Strip(symbol), out string[]? entry)) {
                string value = tier == GlyphTier.Basic ? entry[0] : entry.Length > 2 ? entry[2] : entry[1];
                value = Safe(value, tier);
                if (DisplayWidth.Measure(value, profile) == 1) return value;
                string plain = Safe(entry.Length > 2 ? entry[2] : entry[1], GlyphTier.Ascii);
                if (tier == GlyphTier.Basic && DisplayWidth.Measure(plain, profile) == 1) return plain;
                return "*";
            }
            if (symbol.Length > 0 && !IsEmoji(symbol) && !IsPictographic(symbol)
                && Safe(symbol, tier) == symbol && DisplayWidth.Measure(symbol, profile) == 1) return symbol;
            return "*";
        }

        /// <summary>
        /// Returns a fixed-cell prefix with a separate trailing space. Basic/ASCII prefixes occupy two cells;
        /// Full prefixes reserve the emoji width plus one separator (at least three cells). No glyph is truncated.
        /// </summary>
        /// <param name="symbol">The emoji or symbol used as a prefix.</param>
        /// <param name="tier">The destination glyph tier.</param>
        /// <param name="widthProfile">The destination width model, or the current model.</param>
        /// <returns>The complete glyph padded to the prefix width.</returns>
        public static string GetPrefix(string symbol, GlyphTier tier, TerminalWidthProfile? widthProfile = null) {
            TerminalWidthProfile profile = widthProfile ?? DisplayWidth.Profile;
            string glyph = GetGlyph(symbol, tier, profile);
            int width = tier == GlyphTier.Full ? Math.Max(3, profile.EmojiWidth + 1) : 2;
            return glyph + new string(' ', Math.Max(1, width - DisplayWidth.Measure(glyph, profile)));
        }
    }
}
