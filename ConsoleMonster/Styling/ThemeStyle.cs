using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// The definition of one theme token: optional foreground and background theme colours plus decorations.
    /// </summary>
    public readonly struct ThemeStyle {
        /// <summary>Creates a theme style.</summary>
        /// <param name="color">Foreground, or <c>null</c> for the terminal default.</param>
        /// <param name="backgroundColor">Background, or <c>null</c> for the terminal default.</param>
        /// <param name="decoration">Decorations.</param>
        public ThemeStyle(ThemeColor? color, ThemeColor? backgroundColor = null, TextDecoration decoration = TextDecoration.None) {
            Color = color;
            BackgroundColor = backgroundColor;
            Decoration = decoration;
        }

        /// <summary>Foreground, or <c>null</c> for the terminal default.</summary>
        public ThemeColor? Color { get; }

        /// <summary>Background, or <c>null</c> for the terminal default.</summary>
        public ThemeColor? BackgroundColor { get; }

        /// <summary>Decorations.</summary>
        public TextDecoration Decoration { get; }

        /// <summary>Resolves to a concrete style for a colour depth.</summary>
        /// <param name="depth">The depth.</param>
        /// <returns>The style.</returns>
        public Style Resolve(ColorDepth depth) {
            if (depth == ColorDepth.None) {
                return Style.Plain;
            }
            Color fg = Color.HasValue ? Color.Value.Resolve(depth) : global::ConsoleMonster.Styling.Color.Default;
            Color bg = BackgroundColor.HasValue ? BackgroundColor.Value.Resolve(depth) : global::ConsoleMonster.Styling.Color.Default;
            return new Style(fg, bg, Decoration);
        }
    }
}
