using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Glyphs {
    /// <summary>
    /// A glyph set drawn in a theme's style: default (rounded) frames use the style's frame, and status markers are words
    /// when the style asks for them. Every other glyph comes from the set it wraps.
    /// </summary>
    internal sealed class StyledGlyphSet : IGlyphSet {
        private static readonly object Gate = new object();
        private static readonly Dictionary<(IGlyphSet, BoxStyle, StatusMarkerStyle), StyledGlyphSet> Cache = new Dictionary<(IGlyphSet, BoxStyle, StatusMarkerStyle), StyledGlyphSet>();

        private readonly BoxStyle _border;
        private readonly StatusMarkerStyle _markers;

        private StyledGlyphSet(IGlyphSet inner, BoxStyle border, StatusMarkerStyle markers) {
            Inner = inner;
            _border = border;
            _markers = markers;
        }

        internal IGlyphSet Inner { get; }

        internal bool Words => _markers == StatusMarkerStyle.Words;

        /// <summary>The set in this style; the same instance for the same inputs, so caches keyed on the glyph set still hit.</summary>
        internal static IGlyphSet For(IGlyphSet glyphs, BoxStyle border, StatusMarkerStyle markers) {
            IGlyphSet inner = Unwrap(glyphs);
            if (border == BoxStyle.Rounded && markers == StatusMarkerStyle.Symbols) { return inner; }
            lock (Gate) {
                if (!Cache.TryGetValue((inner, border, markers), out StyledGlyphSet? styled)) {
                    styled = new StyledGlyphSet(inner, border, markers);
                    // Custom glyph sets are few; the cache only grows with the sets a program uses.
                    Cache[(inner, border, markers)] = styled;
                }
                return styled;
            }
        }

        internal static IGlyphSet Unwrap(IGlyphSet glyphs) => glyphs is StyledGlyphSet styled ? styled.Inner : glyphs;

        public GlyphTier Tier => Inner.Tier;
        public bool SupportsBraille => Inner.SupportsBraille;
        public bool SupportsHalfBlocks => Inner.SupportsHalfBlocks;
        public IReadOnlyList<string> SpinnerFrames => Inner.SpinnerFrames;
        public IReadOnlyList<string> VerticalRamp => Inner.VerticalRamp;
        public string Get(Glyph glyph) => Inner.Get(glyph);
        public string HorizontalFill(int eighths) => Inner.HorizontalFill(eighths);
        public string VerticalFill(int eighths) => Inner.VerticalFill(eighths);

        // Rounded is the default frame, so it takes the style's; a widget that names another frame keeps it.
        public BoxGlyphs GetBox(BoxStyle style) => Inner.GetBox(style == BoxStyle.Rounded ? _border : style);

        // Words are the ASCII tier's bracketed markers, which every terminal can show.
        public string StatusMarker(StatusKind status) =>
            _markers == StatusMarkerStyle.Words ? GlyphSet.Ascii.StatusMarker(status) : Inner.StatusMarker(status);
    }
}
