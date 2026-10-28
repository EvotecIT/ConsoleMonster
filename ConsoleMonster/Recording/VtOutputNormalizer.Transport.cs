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

        private void FlushCursor(StringBuilder result, VtScreen.CursorState cursor, bool preserveLatent = false) {
            VtScreen.CursorState current = _written.Cursor;
            if (current.X == cursor.X && current.Y == cursor.Y && current.PendingWrap == cursor.PendingWrap) { return; }
            if (cursor.PendingWrap) {
                Cell cell = cursor.EdgeCell;
                if (!preserveLatent && PlainBlank(cell)) { cell = new Cell(" ", cell.Style.WithColor(default)); }
                WriteCell(result, cursor.Edge, cursor.Y, cursor.OriginTop, cell);
            } else { Position(result, cursor.X, cursor.Y, cursor.OriginTop); }
        }

        private void Position(StringBuilder result, int x, int y, int originTop) {
            if (_written.Cursor.Y == y) {
                Emit(result, "\u001b[" + (x + 1).ToString(CultureInfo.InvariantCulture) + "G");
                return;
            }
            if (_written.Cursor.Origin && (y < originTop || y > _written.Cursor.Bottom)) {
                throw new FormatException("Cannot safely reconstruct a cursor outside its current origin margins.");
            }
            Emit(result, "\u001b[" + (y - originTop + 1).ToString(CultureInfo.InvariantCulture) + ";" + (x + 1).ToString(CultureInfo.InvariantCulture) + "H");
        }

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

        private static bool ModeledControl(string control) {
            if (Housekeeping(control) || CursorTransport(control) || control is "\u001b7" or "\u001b8" or "\u001bM" or "\u001bD" or "\u001bE" or "\u001bc"
                || control.StartsWith("\u001b]8;", StringComparison.Ordinal)) { return true; }
            if (!control.StartsWith("\u001b[", StringComparison.Ordinal)) { return false; }
            char final = control[control.Length - 1];
            string parameters = control.Substring(2, control.Length - 3);
            if (final is 'h' or 'l' && parameters.StartsWith("?", StringComparison.Ordinal)) {
                foreach (string value in parameters.Substring(1).Split(';')) {
                    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int mode) || mode is not (6 or 7 or 47 or 1047 or 1048 or 1049 or 2026)) { return false; }
                }
                return true;
            }
            if ("JKX@PLMSTrsu".IndexOf(final) < 0) { return false; }
            foreach (char value in parameters) { if (value != ';' && (value < '0' || value > '9')) { return false; } }
            return true;
        }

        private void PreparePassthrough(StringBuilder result, VtScreen.CursorState before, Style active) {
            // Unmodeled state can change the meaning of later tabs, spaces and glyphs. Materialize
            // latent source styles now, then keep subsequent transport verbatim until a real reset.
            MaterializeLatentStyles(result, before, active);
            _passthrough = true;
        }

        private void MaterializeLatentStyles(StringBuilder result, VtScreen.CursorState before, Style active) {
            for (int y = 0; y < _styles.Height; y++) {
                for (int x = 0; x < _styles.Width; x++) {
                    Cell source = _styles.CellAt(x, y);
                    if (source != _written.CellAt(x, y)) { WriteCell(result, x, y, before.OriginTop, source); }
                }
            }
            FlushCursor(result, before, preserveLatent: true);
            EmitStyle(result, active);
        }

        private static bool SwitchesScreen(string control) {
            if (!control.StartsWith("\u001b[?", StringComparison.Ordinal) || control[control.Length - 1] is not ('h' or 'l')) { return false; }
            foreach (string value in control.Substring(3, control.Length - 4).Split(';')) {
                if (int.TryParse(value, out int mode) && mode is 47 or 1047 or 1049) { return true; }
            }
            return false;
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
