namespace ConsoleMonster.Capabilities {
    /// <summary>
    /// The colour palette of the built-in theme. <see cref="ThemeMode"/> picks light or dark; the palette picks the colours
    /// within that mode, so <c>ColorBlind</c> in <c>Light</c> mode is the light colour-blind-safe theme.
    /// </summary>
    public enum ThemePalette {
        /// <summary>The standard palette.</summary>
        Default,
        /// <summary>The high contrast palette (one dark rendering).</summary>
        HighContrast,
        /// <summary>The colour-blind-safe palette, light or dark by the mode or the detected background.</summary>
        ColorBlind,
        /// <summary>The terminal's own sixteen palette colours.</summary>
        Terminal
    }
}
