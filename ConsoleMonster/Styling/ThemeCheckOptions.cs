using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    /// <summary>The known drawing conditions for a theme check. Palette colours remain unknown until a terminal palette is available.</summary>
    public sealed class ThemeCheckOptions {
        /// <summary>The page colour. Null uses #0c0c0c for a dark Theme or #ffffff for a light Theme; other ITheme implementations need an explicit background.</summary>
        public Color? Background { get; set; }
        /// <summary>The renderable glyph tier; null means markers cannot be assessed. Defaults to Full.</summary>
        public GlyphTier? GlyphTier { get; set; } = Capabilities.GlyphTier.Full;
        /// <summary>The profile's status-marker override; null uses the theme's appearance, or Symbols for another ITheme implementation.</summary>
        public StatusMarkerStyle? StatusMarkers { get; set; }
    }
}
