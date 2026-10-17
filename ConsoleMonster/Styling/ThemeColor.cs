using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// A theme colour with explicit fallbacks per colour tier. Automatic nearest-colour downsampling often turns
    /// carefully chosen RGB tones into muddy greys on a 16-colour console, so themes state the 16-colour (and
    /// optionally 256-colour) choice deliberately.
    /// </summary>
    public readonly struct ThemeColor {
        /// <summary>Creates a theme colour.</summary>
        /// <param name="trueColor">The 24-bit colour.</param>
        /// <param name="ansi16">The 16-colour index (0-15) to use on 16-colour terminals.</param>
        /// <param name="ansi256">The 256-colour index, or -1 to downsample <paramref name="trueColor"/> automatically.</param>
        public ThemeColor(Color trueColor, int ansi16, int ansi256 = -1) {
            TrueColor = trueColor;
            Ansi16 = ansi16;
            Ansi256 = ansi256;
        }

        /// <summary>The 24-bit colour.</summary>
        public Color TrueColor { get; }

        /// <summary>The 16-colour index; -1 means the terminal default.</summary>
        public int Ansi16 { get; }

        /// <summary>The 256-colour index; -1 means automatic.</summary>
        public int Ansi256 { get; }

        /// <summary>Creates a theme colour from a hex string and a 16-colour index.</summary>
        /// <param name="hex">The 24-bit colour as <c>#rrggbb</c>.</param>
        /// <param name="ansi16">The 16-colour index.</param>
        /// <param name="ansi256">The 256-colour index or -1.</param>
        /// <returns>The theme colour.</returns>
        public static ThemeColor Hex(string hex, int ansi16, int ansi256 = -1) => new ThemeColor(Color.FromHex(hex), ansi16, ansi256);

        /// <summary>Returns the concrete colour for a colour depth.</summary>
        /// <param name="depth">The depth.</param>
        /// <returns>The colour to emit.</returns>
        public Color Resolve(ColorDepth depth) {
            switch (depth) {
                case ColorDepth.TrueColor:
                    return TrueColor;
                case ColorDepth.Ansi256:
                    return Ansi256 >= 0 ? Color.FromAnsi256(Ansi256) : TrueColor.Downsample(ColorDepth.Ansi256);
                case ColorDepth.Ansi16:
                    return Ansi16 >= 0 && Ansi16 <= 15 ? Color.FromAnsi16(Ansi16) : Color.Default;
                default:
                    return Color.Default;
            }
        }
    }
}
