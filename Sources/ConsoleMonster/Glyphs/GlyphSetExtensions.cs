using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Glyphs {
    /// <summary>Status symbols and the running spinner, shared by every widget (see Docs/Design-System.md).</summary>
    public static class GlyphSetExtensions {
        /// <summary>
        /// The one-cell symbol of a status in the glyph set's tier: ✓ ✗ ▴ – ● ○ ◆ (Full), the same in Basic, + x ! - * . i
        /// (ASCII). <see cref="IGlyphSet.StatusMarker"/> is the wider marker for status columns (bracketed words in ASCII).
        /// </summary>
        /// <param name="glyphs">The glyph set.</param>
        /// <param name="status">The status.</param>
        /// <returns>The symbol.</returns>
        public static string StatusSymbol(this IGlyphSet glyphs, StatusKind status) {
            switch (status) {
                case StatusKind.Pass:
                    return glyphs.Get(Glyph.Check);
                case StatusKind.Fail:
                    return glyphs.Get(Glyph.Cross);
                case StatusKind.Warn:
                    return glyphs.Get(Glyph.Warning);
                case StatusKind.Skip:
                    return glyphs.Get(Glyph.Skip);
                case StatusKind.Running:
                    return glyphs.Get(Glyph.Running);
                case StatusKind.Info:
                    return glyphs.Get(Glyph.Info);
                default:
                    return glyphs.Get(Glyph.Pending);
            }
        }

        /// <summary>
        /// The running symbol for this frame: the tier's spinner, advanced by the session's tick (offset so neighbouring
        /// rows do not spin in step), or the static running glyph when the tier has no spinner. Every widget that shows
        /// running work (lanes, phases, status lists, activities, spinners) uses this one.
        /// </summary>
        /// <param name="context">The render context.</param>
        /// <param name="offset">A per-row offset.</param>
        /// <returns>The symbol.</returns>
        public static string Spinner(this RenderContext context, long offset = 0) {
            int count = context.Glyphs.SpinnerFrames.Count;
            return count == 0 ? context.Glyphs.Get(Glyph.Running) : context.Glyphs.SpinnerFrames[(int)((((context.Tick + offset) % count) + count) % count)];
        }
    }
}
