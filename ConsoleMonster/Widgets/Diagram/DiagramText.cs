using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>Applies Diagram's explicit ASCII contract without changing the shared Tree's literal-text contract.</summary>
    internal static class DiagramText {
        internal static string ForTier(string text, RenderContext context) {
            if (context.Profile.Glyphs != GlyphTier.Ascii) return text;
            text = SymbolFallback.Apply(text, GlyphTier.Ascii, context.Profile.WidthProfile);
            return string.Concat(DisplayWidth.Clusters(text, context.Profile.WidthProfile).Select(cluster => {
                string glyph = text.Substring(cluster.Start, cluster.Length);
                return glyph.All(ch => ch < 128) ? glyph : SymbolFallback.GetGlyph(glyph, GlyphTier.Ascii, context.Profile.WidthProfile);
            }));
        }
    }
}
