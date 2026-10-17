using System;
using System.Collections.Generic;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Glyphs {
    /// <summary>
    /// The built-in glyph sets. <see cref="Full"/> needs a modern font (Cascadia, JetBrains Mono, DejaVu);
    /// <see cref="Basic"/> stays inside the WGL4 repertoire that Consolas and Lucida Console cover;
    /// <see cref="Ascii"/> is printable ASCII only.
    /// </summary>
    public sealed partial class GlyphSet : IGlyphSet {
        private readonly string[] _glyphs;
        private readonly Dictionary<BoxStyle, BoxGlyphs> _boxes;
        private readonly string[] _statusMarkers;
        private readonly string[] _horizontalFill;
        private readonly string[] _verticalFill;

        private GlyphSet(
            GlyphTier tier,
            string[] glyphs,
            Dictionary<BoxStyle, BoxGlyphs> boxes,
            string[] statusMarkers,
            string[] horizontalFill,
            string[] verticalFill,
            string[] verticalRamp,
            string[] spinner) {
            Tier = tier;
            _glyphs = glyphs;
            _boxes = boxes;
            _statusMarkers = statusMarkers;
            _horizontalFill = horizontalFill;
            _verticalFill = verticalFill;
            VerticalRamp = verticalRamp;
            SpinnerFrames = spinner;
        }

        /// <inheritdoc />
        public GlyphTier Tier { get; }

        /// <inheritdoc />
        public bool SupportsBraille => Tier == GlyphTier.Full;

        /// <inheritdoc />
        public bool SupportsHalfBlocks => Tier != GlyphTier.Ascii;

        /// <inheritdoc />
        public IReadOnlyList<string> SpinnerFrames { get; }

        /// <inheritdoc />
        public IReadOnlyList<string> VerticalRamp { get; }

        /// <summary>Returns the built-in set for a tier.</summary>
        /// <param name="tier">The tier.</param>
        /// <returns>The glyph set.</returns>
        public static GlyphSet For(GlyphTier tier) {
            switch (tier) {
                case GlyphTier.Full:
                    return Full;
                case GlyphTier.Basic:
                    return Basic;
                default:
                    return Ascii;
            }
        }

        /// <inheritdoc />
        public string Get(Glyph glyph) {
            int index = (int)glyph;
            if (index < 0 || index >= _glyphs.Length) {
                throw new ArgumentOutOfRangeException(nameof(glyph));
            }
            return _glyphs[index];
        }

        /// <inheritdoc />
        public BoxGlyphs GetBox(BoxStyle style) {
            if (_boxes.TryGetValue(style, out BoxGlyphs? box)) {
                return box;
            }
            return _boxes[BoxStyle.Single];
        }

        /// <inheritdoc />
        public string StatusMarker(StatusKind status) => _statusMarkers[(int)status];

        /// <inheritdoc />
        public string HorizontalFill(int eighths) {
            if (eighths <= 0) {
                return string.Empty;
            }
            return _horizontalFill[Math.Min(8, eighths) - 1];
        }

        /// <inheritdoc />
        public string VerticalFill(int eighths) {
            if (eighths <= 0) {
                return string.Empty;
            }
            return _verticalFill[Math.Min(8, eighths) - 1];
        }
    }
}
