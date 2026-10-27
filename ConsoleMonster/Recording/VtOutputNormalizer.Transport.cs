using System;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Sessions {
    public sealed partial class VtOutputNormalizer {
        private void Emit(StringBuilder result, string text) { result.Append(text); _written.Feed(text); }

        private void EmitStyle(StringBuilder result, Style style) {
            style = style.WithLink(null);
            if (_written.ActiveStyle.WithLink(null) == style) { return; }
            var encoded = new StringBuilder();
            SgrEncoder.Append(encoded, style, ColorDepth.TrueColor);
            Emit(result, encoded.ToString());
        }

        private void FlushCursor(StringBuilder result, VtScreen.CursorState cursor) {
            VtScreen.CursorState current = _written.Cursor;
            if (current.X == cursor.X && current.Y == cursor.Y && current.PendingWrap == cursor.PendingWrap) { return; }
            if (cursor.PendingWrap) {
                Cell cell = cursor.EdgeCell;
                if (PlainBlank(cell)) { cell = new Cell(" ", cell.Style.WithColor(default)); }
                WriteCell(result, cursor.Edge, cursor.Y, cursor.OriginTop, cell);
            } else { Position(result, cursor.X, cursor.Y, cursor.OriginTop); }
        }

        private void Position(StringBuilder result, int x, int y, int originTop) => Emit(result,
            "\u001b[" + (y - originTop + 1).ToString(CultureInfo.InvariantCulture) + ";" + (x + 1).ToString(CultureInfo.InvariantCulture) + "H");

        private void WriteCell(StringBuilder result, int x, int y, int originTop, Cell cell) {
            string? link = _written.ActiveStyle.Link;
            Position(result, x, y, originTop);
            if (link != cell.Style.Link) { Emit(result, "\u001b]8;;" + cell.Style.Link + "\u001b\\"); }
            EmitStyle(result, cell.Style);
            Emit(result, cell.Glyph);
            if (link != cell.Style.Link) { Emit(result, "\u001b]8;;" + link + "\u001b\\"); }
        }

        private static bool Housekeeping(string control) => control.StartsWith("\u001b]0;", StringComparison.Ordinal) || control.StartsWith("\u001b]2;", StringComparison.Ordinal)
            || control is "\u001b[6n" or "\u001b[?25h" or "\u001b[?25l" or "\u001b[?9001h" or "\u001b[?9001l" or "\u001b[?1004h" or "\u001b[?1004l";

        private static bool CursorTransport(string control) {
            if (!control.StartsWith("\u001b[", StringComparison.Ordinal) || "HfABCDEFGd`".IndexOf(control[control.Length - 1]) < 0) { return false; }
            for (int i = 2; i < control.Length - 1; i++) { if (control[i] != ';' && (control[i] < '0' || control[i] > '9')) { return false; } }
            return true;
        }

        private static bool MayErase(string text, int start) {
            if (start + 2 >= text.Length || text[start + 1] != '[') { return false; }
            int end = start + 2;
            while (end < text.Length && char.IsDigit(text[end])) { end++; }
            if (end == text.Length || "JKX".IndexOf(text[end]) < 0) { return false; }
            // Erasing scrollback has an effect outside the viewport and must always survive.
            return text[end] != 'J' || !int.TryParse(text.Substring(start + 2, end - start - 2), out int mode) || mode != 3;
        }

        private bool SameVisibleCells(CellBuffer before) {
            for (int y = 0; y < before.Height; y++) {
                for (int x = 0; x < before.Width; x++) { if (!SameVisibleCell(before[x, y], _styles.CellAt(x, y))) { return false; } }
            }
            return true;
        }

        private static bool PlainBlank(Cell cell) => cell.Glyph == " " && cell.Width == 1 && cell.Style.Decoration == TextDecoration.None && cell.Style.Link == null;

        private static bool SameVisibleCell(Cell left, Cell right) {
            if (PlainBlank(left) && PlainBlank(right)) { return left.Style.BackgroundColor == right.Style.BackgroundColor; }
            return left == right;
        }
    }
}
