using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Glyphs {
    /// <summary>Status symbols and the running spinner, shared by every widget (see Docs/Design-System.md).</summary>
    public static class GlyphSetExtensions {
        /// <summary>
        /// The one-cell symbol of a status in the glyph set's tier: ✓ ✗ ▲ – ● ○ ◆ (Full), the same in Basic, + x ! - * . i
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
                case StatusKind.Unknown:
                case StatusKind.None:
                    return glyphs.StatusMarker(status);
                default:
                    return glyphs.Get(Glyph.Pending);
            }
        }

        // Whether status markers are already words (the ASCII tier, or the Words style), so a status word after one repeats it.
        internal static bool MarkersAreWords(this IGlyphSet glyphs) {
            string marker = glyphs.StatusMarker(StatusKind.Fail);
            return marker.Length > 1 && marker[0] == '[' && marker[marker.Length - 1] == ']';
        }

        /// <summary>
        /// The text of an icon catalog key in the glyph set's tier (see <see cref="Icons"/>). Custom glyph sets resolve by
        /// their <see cref="IGlyphSet.Tier"/>; the status icons (warning, done) use the set's own symbols. A key from another
        /// pack (<c>pack:name</c>) is looked up by its name; an unknown key draws the item mark.
        /// </summary>
        /// <param name="glyphs">The glyph set.</param>
        /// <param name="icon">The icon.</param>
        /// <returns>One cell of text.</returns>
        public static string Icon(this IGlyphSet glyphs, Icon icon) {
            int tier = glyphs.Tier == Capabilities.GlyphTier.Full ? 0 : glyphs.Tier == Capabilities.GlyphTier.Basic ? 1 : 2;
            IGlyphSet set = StyledGlyphSet.Unwrap(glyphs);
            bool consoleView = ReferenceEquals(set, GlyphSet.ConsoleViewFull) || ReferenceEquals(set, GlyphSet.ConsoleViewBasic) || ReferenceEquals(set, GlyphSet.ConsoleViewAscii);
            string key = icon.Key.Substring(icon.Key.IndexOf(':') + 1);
            switch (key) {
                case "warning":
                    return glyphs.Get(Glyph.Warning);
                case "done":
                    return glyphs.Get(Glyph.Check);
                case "targets":
                    return Pick(tier, "●", consoleView ? "○" : "●", "o");
                case "tasks":
                    return Pick(tier, "■", "■", "#");
                case "settings":
                    return Pick(tier, "◆", "♦", "*");
                case "favorite":
                    return consoleView ? "*" : Pick(tier, "★", "☼", "*");
                case "run":
                    return Pick(tier, "►", "►", ">");
                case "back":
                    return Pick(tier, "◄", "◄", "<");
                case "list":
                    return Pick(tier, "≡", "≡", "=");
                case "help":
                    return "?";
                case "exit":
                    return Pick(tier, "×", "×", "x");
                default:
                    return glyphs.Get(Glyph.Bullet);
            }
        }

        private static string Pick(int tier, string full, string basic, string ascii) => tier == 0 ? full : tier == 1 ? basic : ascii;

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
            return context.ReducedMotion || count == 0 ? context.Glyphs.Get(Glyph.Running) : context.Glyphs.SpinnerFrames[(int)((((context.Tick + offset) % count) + count) % count)];
        }
    }
}
