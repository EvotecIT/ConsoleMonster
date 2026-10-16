using System.Globalization;
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
        /// <remarks>
        /// With <paramref name="anchor"/> (VT outputs), every cell that follows a glyph whose terminal width cannot be
        /// trusted (emoji, ZWJ sequences, flags, CJK, East Asian Ambiguous symbols) is placed with an absolute column move
        /// (<c>CHA</c>), and an untrusted wide glyph is written over two blanks. If the terminal draws such a glyph wider or
        /// narrower than measured, only that glyph's own cells are affected; the rest of the row stays in its columns.
        /// </remarks>
        public static void AppendRun(StringBuilder sb, CellBuffer buffer, int y, int start, int endExclusive, ColorDepth depth, ref Style? current, bool anchor = false) {
            bool drifting = false;
            for (int x = start; x < endExclusive; x++) {
                Cell cell = buffer[x, y];
                if (cell.IsContinuation) {
                    continue;
                }
                if (depth != ColorDepth.None && (!current.HasValue || !current.Value.Equals(cell.Style))) {
                    SgrEncoder.Append(sb, cell.Style, depth);
                    current = cell.Style;
                }
                bool trusted = !anchor || GlyphTrust.IsTrusted(cell.Glyph);
                if (drifting) {
                    AppendColumn(sb, x);
                    drifting = false;
                }
                if (cell.Width == 2 && x + 1 >= endExclusive) {
                    sb.Append(' ');
                    continue;
                }
                if (!trusted && cell.Width == 2) {
                    // Blank both cells first: a terminal that draws the glyph one cell wide leaves a blank, not stale text.
                    sb.Append("  ");
                    AppendColumn(sb, x);
                }
                sb.Append(cell.Glyph);
                drifting = !trusted;
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

        // CHA: cursor to an absolute column on the current row (1-based). Works for live frames and print-once lines alike.
        private static void AppendColumn(StringBuilder sb, int x) {
            sb.Append('\u001b').Append('[').Append((x + 1).ToString(CultureInfo.InvariantCulture)).Append('G');
        }
    }
}
