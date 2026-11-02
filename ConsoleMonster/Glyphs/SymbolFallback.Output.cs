using System.Collections.Generic;
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
                string replacement = allowed ? glyph : TryGet(glyph, tier, out string fallback) ? Safe(fallback, tier) : "?";
                result.Append(replacement);
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
