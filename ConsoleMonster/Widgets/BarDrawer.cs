using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Draws horizontal bars for the glyph tier. Every shape leaves visible space between stacked rows, so neighbouring
    /// bars of similar length never merge into one block: <see cref="BarShape.Block"/> uses three-quarter-height blocks
    /// (<c>▆</c>) on Full and lower half blocks (<c>▄</c>) on Basic, whole cells either way; <see cref="BarShape.Line"/>
    /// uses a heavy stroke with half-cell precision on Full. Ascii uses <c>#</c>, whose glyph already separates rows. The
    /// empty track has the same height as the bar: a groove in the subtle colour, or the tier's track glyph without colour.
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
            if (glyphs.Tier == GlyphTier.Basic) {
                DrawHalfHeight(region, x, y, width, fraction, fill, context, track);
                return;
            }
            if (glyphs.Tier == GlyphTier.Full) {
                DrawRowBlock(region, x, y, width, fraction, fill, context, track);
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

        private static void DrawRowBlock(Region region, int x, int y, int width, double fraction, Style fill, RenderContext context, bool track) {
            // Three-quarter blocks leave the top quarter of each row empty: a clear gap between stacked bars. No glyph
            // has both a partial width and this height, so bars round to whole cells (a non-zero value keeps one cell).
            string block = context.Glyphs.VerticalFill(6);
            int full = WholeCells(fraction, width);
            for (int i = 0; i < full; i++) {
                region.Put(x + i, y, block, fill);
            }
            if (!track) {
                return;
            }
            string trackGlyph = context.HasColor ? block : context.Glyphs.Get(Glyph.BarTrack);
            Style trackStyle = context.Style(ThemeToken.Subtle);
            for (int i = full; i < width; i++) {
                region.Put(x + i, y, trackGlyph, trackStyle);
            }
        }

        private static int WholeCells(double fraction, int width) {
            int cells = (int)Math.Round(fraction * width);
            if (cells == 0 && fraction > 0) {
                cells = 1;
            }
            return Math.Min(width, cells);
        }

        private static void DrawHalfHeight(Region region, int x, int y, int width, double fraction, Style fill, RenderContext context, bool track) {
            // Lower half blocks (WGL4) leave the upper half of each row empty, so stacked bars stay separate on Consolas.
            string lower = context.Glyphs.Get(Glyph.HalfLower);
            int full = WholeCells(fraction, width);
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
            // Heavy fill, light track: a finished bar, a running bar and a separator rule all read differently.
            const string Heavy = "━";
            const string HeavyLeftHalf = "╸";
            const string HeavyLeftLightRight = "╾";
            const string Light = "─";
            int halves = (int)Math.Round(fraction * width * 2);
            int full = halves / 2;
            bool half = halves % 2 == 1;
            for (int i = 0; i < full && i < width; i++) {
                region.Put(x + i, y, Heavy, fill);
            }
            int next = full;
            if (half && next < width) {
                region.Put(x + next, y, track ? HeavyLeftLightRight : HeavyLeftHalf, fill);
                next++;
            }
            if (!track) {
                return;
            }
            Style trackStyle = context.Style(ThemeToken.Subtle);
            for (int i = next; i < width; i++) {
                region.Put(x + i, y, Light, trackStyle);
            }
        }
    }
}
