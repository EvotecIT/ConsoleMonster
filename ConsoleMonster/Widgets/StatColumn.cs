using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The width state of one stat column inside a widget: applies a <see cref="ColumnFormat"/> and remembers the widest
    /// width used, so live columns never shrink. Not thread-safe; widgets render on one thread.
    /// </summary>
    internal sealed class StatColumn {
        private int _highWater;

        internal StatColumn(ColumnFormat format) {
            Format = format ?? throw new ArgumentNullException(nameof(format));
        }

        /// <summary>The format (replaceable by the widget's public property).</summary>
        internal ColumnFormat Format { get; set; }

        /// <summary>
        /// Resolves the column width for this frame.
        /// </summary>
        /// <param name="widest">Widest current value in cells.</param>
        /// <param name="fixedPart">Cells that are not digits (symbol, unit, percent sign) added to reserved digits.</param>
        /// <param name="knownMaximum">Width of a known maximum value (for example a total), or 0.</param>
        /// <returns>The width in cells.</returns>
        internal int Resolve(int widest, int fixedPart = 0, int knownMaximum = 0) {
            int width = Math.Max(widest, Math.Max(Format.MinWidth, knownMaximum));
            if (Format.ReserveDigits > 0) {
                width = Math.Max(width, fixedPart + Format.ReserveDigits);
            }
            if (!Format.AllowShrink) {
                width = Math.Max(width, _highWater);
                _highWater = width;
            }
            return width;
        }

        /// <summary>Forgets the widest width used (for example when a widget is cleared).</summary>
        internal void Reset() {
            _highWater = 0;
        }

        /// <summary>Writes text aligned inside a column, truncated to the column width with the tier's ellipsis.</summary>
        internal static void Write(Region region, int x, int y, int width, string text, Style style, HorizontalAlignment alignment, string ellipsis = TextWrapper.Ellipsis) {
            if (width <= 0 || string.IsNullOrEmpty(text)) {
                return;
            }
            int textWidth = DisplayWidth.Measure(text);
            if (textWidth > width) {
                text = TextWrapper.Truncate(text, width, ellipsis);
                textWidth = DisplayWidth.Measure(text);
            }
            int offset = alignment == HorizontalAlignment.Right ? width - textWidth : alignment == HorizontalAlignment.Center ? (width - textWidth) / 2 : 0;
            region.Write(x + offset, y, text, style, width - offset);
        }

        /// <summary>Pads text to a column width with the given alignment (for line-based output such as legends).</summary>
        internal static string Pad(string text, int width, HorizontalAlignment alignment) {
            int gap = width - DisplayWidth.Measure(text);
            if (gap <= 0) {
                return text;
            }
            if (alignment == HorizontalAlignment.Right) {
                return new string(' ', gap) + text;
            }
            if (alignment == HorizontalAlignment.Center) {
                return new string(' ', gap / 2) + text + new string(' ', gap - (gap / 2));
            }
            return text + new string(' ', gap);
        }
    }
}
