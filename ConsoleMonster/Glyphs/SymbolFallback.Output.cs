using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Text;

namespace ConsoleMonster.Glyphs {
    public static partial class SymbolFallback {
        /// <summary>Enforces the destination repertoire even for caller text and custom drawing code.</summary>
        internal static string Constrain(string text, GlyphTier tier) {
            if (tier == GlyphTier.Full || string.IsNullOrEmpty(text)) return text;
            bool safe = true;
            foreach (char c in text) {
                if (!Allowed(c, tier)) { safe = false; break; }
            }
            if (safe) return text;
            var result = new StringBuilder(text.Length);
            foreach (TextCluster cluster in DisplayWidth.Clusters(text)) {
                string glyph = text.Substring(cluster.Start, cluster.Length);
                bool allowed = true;
                foreach (char c in glyph) if (!Allowed(c, tier)) { allowed = false; break; }
                if (allowed) { result.Append(glyph); continue; }
                if (TryGet(glyph, tier, out string fallback)) { result.Append(Safe(fallback, tier)); continue; }
                // Invisible format characters (a byte order mark, zero-width spaces and joiners) take no cell: drop them
                // rather than show a '?' for something the source never displayed.
                string visible = WithoutFormatCharacters(glyph);
                if (visible.Length == 0) continue;
                allowed = true;
                foreach (char c in visible) if (!Allowed(c, tier)) { allowed = false; break; }
                result.Append(allowed ? visible : tier == GlyphTier.Ascii ? Transliterate(visible) : "?");
            }
            return result.ToString();
        }

        private static string WithoutFormatCharacters(string glyph) {
            StringBuilder? kept = null;
            for (int i = 0; i < glyph.Length; i++) {
                bool format = glyph[i] != '\u00AD' && CharUnicodeInfo.GetUnicodeCategory(glyph[i]) == UnicodeCategory.Format; // a soft hyphen keeps its cell
                if (format && kept == null) kept = new StringBuilder(glyph, 0, i, glyph.Length);
                else if (!format) kept?.Append(glyph[i]);
            }
            return kept?.ToString() ?? glyph;
        }

        private static string Transliterate(string glyph) {
            string decomposed;
            try { decomposed = glyph.Normalize(NormalizationForm.FormD); }
            catch (System.ArgumentException) { return "?"; }
            var result = new StringBuilder(decomposed.Length);
            for (int i = 0; i < decomposed.Length; i++) {
                char c = decomposed[i];
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(decomposed, i);
                if (category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark || category == UnicodeCategory.EnclosingMark) continue;
                string? latin = c switch {
                    'ł' => "l", 'Ł' => "L", 'đ' => "d", 'Đ' => "D", 'ø' => "o", 'Ø' => "O", 'ß' => "ss",
                    'æ' => "ae", 'Æ' => "AE", 'œ' => "oe", 'Œ' => "OE", 'þ' => "th", 'Þ' => "TH", 'ð' => "d", 'Ð' => "D", _ => null
                };
                result.Append(latin ?? (Allowed(c, GlyphTier.Ascii) ? c.ToString() : "?"));
                if (char.IsHighSurrogate(c) && i + 1 < decomposed.Length && char.IsLowSurrogate(decomposed[i + 1])) i++;
            }
            return result.ToString();
        }

        private static bool Allowed(char c, GlyphTier tier) => c == '\n' || c == '\r' || c == '\t'
            || (tier == GlyphTier.Ascii ? c >= ' ' && c <= '~' : Wgl4Repertoire.Contains(c));

        internal static StyledText ForOutput(StyledText text, GlyphTier tier) {
            if (tier == GlyphTier.Full) return text;
            StyledText fallback = Apply(text, tier);
            var lines = new List<StyledLine>(fallback.Lines.Count);
            foreach (StyledLine line in fallback.Lines) {
                var spans = new List<TextSpan>(line.Count);
                foreach (TextSpan span in line) spans.Add(new TextSpan(Constrain(span.Text, tier), span.Style));
                lines.Add(new StyledLine(spans));
            }
            return new StyledText(lines);
        }
    }
}
