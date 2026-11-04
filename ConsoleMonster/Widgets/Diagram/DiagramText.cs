using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;

namespace ConsoleMonster.Widgets {
    /// <summary>Uses the shared caller-text repertoire policy before Diagram measures and wraps text.</summary>
    internal static class DiagramText {
        internal static string ForTier(string text, RenderContext context) {
            if (context.Profile.Glyphs != GlyphTier.Ascii) return text;
            return SymbolFallback.Constrain(SymbolFallback.Apply(text, GlyphTier.Ascii, context.Profile.WidthProfile), GlyphTier.Ascii);
        }
    }
}
