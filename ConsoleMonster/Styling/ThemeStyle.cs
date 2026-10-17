using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// The definition of one theme token: optional foreground and background theme colours plus decorations.
    /// </summary>
    public readonly struct ThemeStyle {
        /// <summary>Creates a theme style.</summary>
        /// <param name="foreground">Foreground, or <c>null</c> for the terminal default.</param>
        /// <param name="background">Background, or <c>null</c> for the terminal default.</param>
        /// <param name="decoration">Decorations.</param>
        public ThemeStyle(ThemeColor? foreground, ThemeColor? background = null, TextDecoration decoration = TextDecoration.None) {
            Foreground = foreground;
            Background = background;
            Decoration = decoration;
        }

        /// <summary>Foreground, or <c>null</c> for the terminal default.</summary>
        public ThemeColor? Foreground { get; }

        /// <summary>Background, or <c>null</c> for the terminal default.</summary>
        public ThemeColor? Background { get; }

        /// <summary>Decorations.</summary>
        public TextDecoration Decoration { get; }

        /// <summary>Resolves to a concrete style for a colour depth.</summary>
        /// <param name="depth">The depth.</param>
        /// <returns>The style.</returns>
        public Style Resolve(ColorDepth depth) {
            if (depth == ColorDepth.None) {
                return Style.Plain;
            }
            Color fg = Foreground.HasValue ? Foreground.Value.Resolve(depth) : Color.Default;
            Color bg = Background.HasValue ? Background.Value.Resolve(depth) : Color.Default;
            return new Style(fg, bg, Decoration);
        }
    }
}
