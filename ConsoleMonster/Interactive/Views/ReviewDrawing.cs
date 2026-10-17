using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The review page's parts, shared by forms and review items: label/value rows, problems, the equivalent command in a
    /// titled box (wrapped between arguments), and the button row at the bottom right.
    /// </summary>
    internal static class ReviewDrawing {
        public static int LabelWidth(IEnumerable<KeyValuePair<string, string>> rows, int width) {
            int widest = 0;
            foreach (KeyValuePair<string, string> row in rows) {
                widest = Math.Max(widest, DisplayWidth.Measure(row.Key));
            }
            return Math.Max(10, Math.Min(Math.Min(28, width / 3), widest + 3));
        }

        /// <summary>
        /// Draws the review into a region: rows from the top, the command box under them, problems directly above the button
        /// row at the bottom right (so they sit next to the blocked Run button they explain).
        /// </summary>
        public static void Draw(Region region, RenderContext context, IList<KeyValuePair<string, string>> rows, IList<string> problems, string? command, string commandTitle, IReadOnlyList<string> buttons, int focus, bool active, int blocked = -1) {
            int labelWidth = LabelWidth(rows, region.Width);
            int valueX = 2 + labelWidth;
            List<string> commandLines = new List<string>();
            if (!string.IsNullOrEmpty(command)) {
                commandLines.AddRange(CommandLine.Wrap(command!, Math.Max(10, region.Width - 6)));
            }
            int commandHeight = commandLines.Count == 0 ? 0 : Math.Min(commandLines.Count, 6) + 2;
            int problemRows = problems.Count == 0 ? 0 : Math.Min(problems.Count, Math.Max(1, region.Height / 4));
            int reserved = 2 + (commandHeight > 0 ? commandHeight + 1 : 0) + (problemRows > 0 ? problemRows + 1 : 0);
            int limit = Math.Max(1, region.Height - reserved);
            int y = 0;
            for (int i = 0; i < rows.Count && y < limit; i++, y++) {
                if (y == limit - 1 && i < rows.Count - 1) {
                    region.Write(2, y, PromptLabels.Format(PromptLabels.Default.More, rows.Count - i), context.Style(ThemeToken.Muted));
                    y++;
                    break;
                }
                region.Write(2, y, TextWrapper.Truncate(rows[i].Key, labelWidth - 2, context.Ellipsis), context.Style(ThemeToken.Muted));
                bool empty = string.IsNullOrEmpty(rows[i].Value);
                region.Write(valueX, y, TextWrapper.Truncate(empty ? "—" : rows[i].Value, Math.Max(1, region.Width - valueX - 1), context.Ellipsis), context.Style(empty ? ThemeToken.Subtle : ThemeToken.Text));
            }
            int problemTop = region.Height - 2 - problemRows;
            if (commandHeight > 0) {
                y++;
                int bottom = problemRows > 0 ? problemTop - 1 : region.Height - 2;
                Region box = region.Sub(0, y, region.Width, Math.Min(commandHeight, Math.Max(0, bottom - y)));
                new Panel(new Lines(commandLines, context.Style(ThemeToken.Accent)), Markup.Escape(commandTitle)) { BorderToken = ThemeToken.Border }.Render(box, context);
            }
            for (int i = 0; i < problemRows; i++) {
                string text = i == problemRows - 1 && problems.Count > problemRows ? PromptLabels.Format(PromptLabels.Default.More, problems.Count - i) : problems[i];
                region.Write(2, problemTop + i, TextWrapper.Truncate(ChoicePane.Problem(text, context), Math.Max(1, region.Width - 3), context.Ellipsis));
            }
            StyledLine buttonRow = new StyledLine(Buttons.Spans(buttons, focus, context, active, null, blocked));
            region.Write(Math.Max(0, region.Width - buttonRow.Width - 1), region.Height - 1, buttonRow);
        }
        private sealed class Lines : IWidget {
            private readonly List<string> _lines;
            private readonly Style _style;

            public Lines(List<string> lines, Style style) {
                _lines = lines;
                _style = style;
            }

            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(_lines.Count, constraints.MaxHeight));

            public void Render(Region region, RenderContext context) {
                for (int i = 0; i < _lines.Count && i < region.Height; i++) {
                    region.Write(0, i, TextWrapper.Truncate(_lines[i], region.Width, context.Ellipsis), _style);
                }
            }
        }
    }
}
