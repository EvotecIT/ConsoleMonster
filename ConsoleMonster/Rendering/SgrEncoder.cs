using System.Globalization;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Rendering {
    /// <summary>
    /// Encodes styles as SGR (Select Graphic Rendition) sequences for a colour depth. Every style change emits a full
    /// reset plus the new attributes, which is robust on every VT implementation (conhost, Windows Terminal, xterm.js)
    /// and keeps frames independent of any previous terminal state.
    /// </summary>
    public static class SgrEncoder {
        /// <summary>The reset sequence.</summary>
        public const string Reset = "\u001b[0m";

        /// <summary>Appends the SGR sequence for a style (including the reset prefix).</summary>
        /// <param name="builder">The target.</param>
        /// <param name="style">The style.</param>
        /// <param name="depth">The colour depth; colours are downsampled to it.</param>
        public static void Append(StringBuilder builder, Style style, ColorDepth depth) {
            if (depth == ColorDepth.None) {
                return;
            }
            Style s = style.Downsample(depth);
            builder.Append("\u001b[0");
            TextDecoration d = s.Decoration;
            if ((d & TextDecoration.Bold) != 0) {
                builder.Append(";1");
            }
            if ((d & TextDecoration.Dim) != 0) {
                builder.Append(";2");
            }
            if ((d & TextDecoration.Italic) != 0) {
                builder.Append(";3");
            }
            if ((d & TextDecoration.Underline) != 0) {
                builder.Append(";4");
            }
            if ((d & TextDecoration.Reverse) != 0) {
                builder.Append(";7");
            }
            if ((d & TextDecoration.Strikethrough) != 0) {
                builder.Append(";9");
            }
            AppendColor(builder, s.Color, false);
            AppendColor(builder, s.BackgroundColor, true);
            builder.Append('m');
        }

        /// <summary>Returns the SGR sequence for a style.</summary>
        /// <param name="style">The style.</param>
        /// <param name="depth">The colour depth.</param>
        /// <returns>The sequence, or an empty string for <see cref="ColorDepth.None"/>.</returns>
        public static string Encode(Style style, ColorDepth depth) {
            StringBuilder sb = new StringBuilder(24);
            Append(sb, style, depth);
            return sb.ToString();
        }

        private static void AppendColor(StringBuilder builder, Color color, bool background) {
            switch (color.Kind) {
                case ColorKind.Ansi16:
                    int index = color.Index;
                    int code = index < 8 ? (background ? 40 : 30) + index : (background ? 100 : 90) + (index - 8);
                    builder.Append(';').Append(code.ToString(CultureInfo.InvariantCulture));
                    break;
                case ColorKind.Ansi256:
                    builder.Append(background ? ";48;5;" : ";38;5;").Append(color.Index.ToString(CultureInfo.InvariantCulture));
                    break;
                case ColorKind.Rgb:
                    builder.Append(background ? ";48;2;" : ";38;2;")
                        .Append(color.R.ToString(CultureInfo.InvariantCulture)).Append(';')
                        .Append(color.G.ToString(CultureInfo.InvariantCulture)).Append(';')
                        .Append(color.B.ToString(CultureInfo.InvariantCulture));
                    break;
            }
        }
    }
}
