using System;
using System.Globalization;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Export {
    /// <summary>
    /// Turns cell colours into CSS colours: the terminal's default foreground and background for the chosen theme, the
    /// Windows Terminal "Campbell" values for the 16 ANSI colours (what Windows users see), the xterm cube for 256 colours.
    /// With <see cref="UseVariables"/> the defaults become CSS variables, so one rendering follows the viewer's light or
    /// dark preference (used for frames that were rendered once, with fixed colours).
    /// </summary>
    internal sealed class ExportPalette {
        private ExportPalette(string name, string foreground, string background, bool variables) {
            Name = name;
            Foreground = foreground;
            Background = background;
            UseVariables = variables;
        }

        public static ExportPalette Dark { get; } = new ExportPalette("dark", "#cccccc", "#0c0c0c", false);

        public static ExportPalette Light { get; } = new ExportPalette("light", "#24292f", "#ffffff", false);
        public static ExportPalette HighContrast { get; } = new ExportPalette("high-contrast", "#ffffff", "#000000", false);

        /// <summary>Defaults as CSS variables (<c>--cm-fg</c>, <c>--cm-bg</c>) that switch with prefers-color-scheme.</summary>
        public static ExportPalette Adaptive { get; } = new ExportPalette("auto", "var(--cm-fg)", "var(--cm-bg)", true);

        public string Name { get; }

        /// <summary>The default foreground (a hex colour, or a variable reference).</summary>
        public string Foreground { get; }

        /// <summary>The default background (a hex colour, or a variable reference).</summary>
        public string Background { get; }

        public bool UseVariables { get; }

        /// <summary>The 16 ANSI colours as hex, for asciicast headers.</summary>
        public static string[] Ansi16Hex() {
            string[] colors = new string[16];
            for (int i = 0; i < 16; i++) {
                AnsiPalette.Get16(i, out byte r, out byte g, out byte b);
                colors[i] = Hex(r, g, b);
            }
            return colors;
        }

        /// <summary>The resolved foreground and background of a cell style (reverse applied), as CSS colours.</summary>
        public void Resolve(Style style, out string foreground, out string background, out bool defaultBackground) {
            string? fg = ToCss(style.Foreground);
            string? bg = ToCss(style.Background);
            if ((style.Decoration & TextDecoration.Reverse) != 0) {
                string? swap = fg;
                fg = bg ?? Background;
                bg = swap ?? Foreground;
            }
            defaultBackground = bg == null;
            foreground = fg ?? Foreground;
            background = bg ?? Background;
            if ((style.Decoration & TextDecoration.Dim) != 0) {
                foreground = Dim(foreground, background);
            }
        }

        /// <summary>Dim uses alpha when a resolved foreground is a CSS variable; concrete colours are already mixed.</summary>
        public static double ForegroundOpacity(Style style, string foreground) =>
            (style.Decoration & TextDecoration.Dim) != 0 && foreground.StartsWith("var(", StringComparison.Ordinal) ? 0.65 : 1;

        /// <summary>A colour as #rrggbb, or null for the terminal default.</summary>
        public static string? ToCss(Color color) {
            if (color.IsDefault) {
                return null;
            }
            color.ToRgb(out byte r, out byte g, out byte b);
            return Hex(r, g, b);
        }

        public static string Hex(byte r, byte g, byte b) {
            return "#" + r.ToString("x2", CultureInfo.InvariantCulture) + g.ToString("x2", CultureInfo.InvariantCulture) + b.ToString("x2", CultureInfo.InvariantCulture);
        }

        // Dim text: 60% of the way from the background to the foreground, when both are concrete colours.
        private static string Dim(string foreground, string background) {
            if (!TryParse(foreground, out int fr, out int fg, out int fb) || !TryParse(background, out int br, out int bg, out int bb)) {
                return foreground;
            }
            return Hex(Mix(br, fr), Mix(bg, fg), Mix(bb, fb));
        }

        private static byte Mix(int from, int to) => (byte)Math.Round(from + ((to - from) * 0.6));

        private static bool TryParse(string css, out int r, out int g, out int b) {
            r = g = b = 0;
            if (css.Length != 7 || css[0] != '#') {
                return false;
            }
            return int.TryParse(css.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
                && int.TryParse(css.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
                && int.TryParse(css.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
        }
    }
}
