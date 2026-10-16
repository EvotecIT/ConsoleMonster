using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Rendering {
    /// <summary>Shared encoding of cell runs into text with SGR changes, used by every output.</summary>
    internal static class CellEncoder {
        /// <summary>
        /// Appends cells <paramref name="start"/>..<paramref name="endExclusive"/> of a row, emitting SGR only when the
        /// style changes. <paramref name="current"/> tracks the active style across calls; <c>null</c> means unknown.
        /// </summary>
        public static void AppendRun(StringBuilder sb, CellBuffer buffer, int y, int start, int endExclusive, ColorDepth depth, ref Style? current) {
            for (int x = start; x < endExclusive; x++) {
                Cell cell = buffer[x, y];
                if (cell.IsContinuation) {
                    continue;
                }
                if (depth != ColorDepth.None && (!current.HasValue || !current.Value.Equals(cell.Style))) {
                    SgrEncoder.Append(sb, cell.Style, depth);
                    current = cell.Style;
                }
                if (cell.Width == 2 && x + 1 >= endExclusive) {
                    sb.Append(' ');
                } else {
                    sb.Append(cell.Glyph);
                }
            }
        }

        /// <summary>Returns the exclusive end of the meaningful content in a row (trailing plain blanks trimmed).</summary>
        public static int ContentEnd(CellBuffer buffer, int y, ColorDepth depth) {
            for (int x = buffer.Width - 1; x >= 0; x--) {
                Cell cell = buffer[x, y];
                if (cell.IsContinuation) {
                    return x + 1;
                }
                if (cell.Glyph != " ") {
                    return x + 1;
                }
                if (depth != ColorDepth.None && (!cell.Style.Background.IsDefault || (cell.Style.Decoration & (TextDecoration.Underline | TextDecoration.Reverse | TextDecoration.Strikethrough)) != 0)) {
                    return x + 1;
                }
            }
            return 0;
        }
    }
}
