using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Rendering {
    /// <summary>
    /// Turns rendered cells back into markup (one line per row, styles as tags), so a picture of a widget can travel as
    /// text, for example in a recording, and be shown again by a <see cref="Widgets.TextBlock"/>.
    /// </summary>
    internal static class CellMarkup {
        public static string From(CellBuffer buffer) {
            StringBuilder markup = new StringBuilder();
            int last = buffer.Height - 1;
            while (last >= 0 && IsBlank(buffer, last)) {
                last--;
            }
            for (int y = 0; y <= last; y++) {
                if (y > 0) {
                    markup.Append('\n');
                }
                AppendRow(markup, buffer, y);
            }
            return markup.ToString();
        }

        private static void AppendRow(StringBuilder markup, CellBuffer buffer, int y) {
            int end = buffer.Width;
            while (end > 0 && IsBlankCell(buffer[end - 1, y])) {
                end--;
            }
            int x = 0;
            while (x < end) {
                Style style = buffer[x, y].Style;
                StringBuilder run = new StringBuilder();
                while (x < end && buffer[x, y].Style.Equals(style)) {
                    Cell cell = buffer[x, y];
                    if (!cell.IsContinuation) {
                        run.Append(cell.Glyph ?? " ");
                    }
                    x++;
                }
                string text = Text.Markup.Escape(run.ToString());
                string tag = style.IsPlain ? string.Empty : new RichStyle(RichColor.From(style.Color), RichColor.From(style.BackgroundColor), style.Decoration, style.Link).ToMarkup();
                if (tag.Length == 0) {
                    markup.Append(text);
                } else {
                    markup.Append('[').Append(tag).Append(']').Append(text).Append("[/]");
                }
            }
        }

        private static bool IsBlank(CellBuffer buffer, int y) {
            for (int x = 0; x < buffer.Width; x++) {
                if (!IsBlankCell(buffer[x, y])) {
                    return false;
                }
            }
            return true;
        }

        private static bool IsBlankCell(Cell cell) => !cell.IsContinuation && cell.Glyph == " " && cell.Style.BackgroundColor.IsDefault && (cell.Style.Decoration & (TextDecoration.Reverse | TextDecoration.Underline | TextDecoration.Strikethrough)) == 0;
    }
}
