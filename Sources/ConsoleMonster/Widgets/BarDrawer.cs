using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Draws horizontal bars with sub-cell precision for the glyph tier: eighths on Full, halves on Basic, whole cells on
    /// Ascii. With 256 or more colours the empty track is a solid groove in the subtle colour; with 16 colours it is a
    /// light shade; without colour it is the tier's track glyph, so bars stay readable everywhere.
    /// </summary>
    internal static class BarDrawer {
        /// <summary>Draws one bar.</summary>
        public static void Draw(Region region, int x, int y, int width, double fraction, Style fill, RenderContext context, bool track = true, BarShape shape = BarShape.Block) {
            if (width <= 0) {
                return;
            }
            fraction = double.IsNaN(fraction) ? 0 : Math.Max(0, Math.Min(1, fraction));
            IGlyphSet glyphs = context.Glyphs;
            if (shape == BarShape.Line && glyphs.Tier == GlyphTier.Full) {
                DrawLine(region, x, y, width, fraction, fill, context, track);
                return;
            }
            if (shape == BarShape.Line && glyphs.Tier == GlyphTier.Basic) {
                DrawHalfHeight(region, x, y, width, fraction, fill, context, track);
                return;
            }
            int eighths = (int)Math.Round(fraction * width * 8);
            int full = eighths / 8;
            int remainder = eighths % 8;
            string block = glyphs.Get(Glyph.BlockFull);
            for (int i = 0; i < full && i < width; i++) {
                region.Put(x + i, y, block, fill);
            }
            int next = full;
            bool solidTrack = track && context.Depth >= ColorDepth.Ansi256;
            Style trackStyle = context.Style(ThemeToken.Subtle);
            if (remainder > 0 && next < width) {
                string partial = glyphs.HorizontalFill(remainder);
                if (partial.Length > 0) {
                    Style partialStyle = solidTrack ? fill.WithBackground(trackStyle.Foreground) : fill;
                    region.Put(x + next, y, partial, partialStyle);
                    next++;
                }
            }
            if (!track) {
                return;
            }
            string trackGlyph;
            if (solidTrack) {
                trackGlyph = block;
            } else if (context.HasColor && glyphs.Tier != GlyphTier.Ascii) {
                trackGlyph = glyphs.Get(Glyph.ShadeLight);
            } else {
                trackGlyph = glyphs.Get(Glyph.BarTrack);
            }
            for (int i = next; i < width; i++) {
                region.Put(x + i, y, trackGlyph, trackStyle);
            }
        }

        private static void DrawHalfHeight(Region region, int x, int y, int width, double fraction, Style fill, RenderContext context, bool track) {
            // Lower half blocks (WGL4) leave the upper half of each row empty, so stacked bars stay separate on Consolas.
            string lower = context.Glyphs.Get(Glyph.HalfLower);
            int full = (int)Math.Round(fraction * width);
            for (int i = 0; i < full && i < width; i++) {
                region.Put(x + i, y, lower, fill);
            }
            if (!track) {
                return;
            }
            string trackGlyph = context.HasColor ? lower : context.Glyphs.Get(Glyph.RuleLine);
            Style trackStyle = context.Style(ThemeToken.Subtle);
            for (int i = full; i < width; i++) {
                region.Put(x + i, y, trackGlyph, trackStyle);
            }
        }

        private static void DrawLine(Region region, int x, int y, int width, double fraction, Style fill, RenderContext context, bool track) {
            const string Heavy = "━";
            const string HeavyLeftHalf = "╸";
            const string HeavyRightHalf = "╺";
            const string Thin = "─";
            int halves = (int)Math.Round(fraction * width * 2);
            int full = halves / 2;
            bool half = halves % 2 == 1;
            for (int i = 0; i < full && i < width; i++) {
                region.Put(x + i, y, Heavy, fill);
            }
            int next = full;
            Style trackStyle = context.HasColor ? context.Style(ThemeToken.Subtle) : Style.Plain;
            string trackGlyph = context.HasColor ? Heavy : Thin;
            if (half && next < width) {
                region.Put(x + next, y, HeavyLeftHalf, fill);
                next++;
                if (track && context.HasColor && next < width) {
                    region.Put(x + next, y, HeavyRightHalf, trackStyle);
                    next++;
                }
            }
            if (!track) {
                return;
            }
            for (int i = next; i < width; i++) {
                region.Put(x + i, y, trackGlyph, trackStyle);
            }
        }
    }
}
