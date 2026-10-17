using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Widgets {
    /// <summary>Lays out and draws progress columns: fixed widths first, flexible columns share the rest, low priorities drop when narrow.</summary>
    internal static class ProgressLayout {
        internal static int[] Arrange(IList<ProgressColumn> columns, IReadOnlyList<ProgressRow> rows, int width, RenderContext context) {
            int count = columns.Count;
            int[] widths = new int[count];
            bool[] visible = new bool[count];
            for (int i = 0; i < count; i++) {
                ProgressColumn column = columns[i];
                widths[i] = column.Flexible ? column.MinWidth : Math.Max(0, column.Measure(rows, context));
                visible[i] = widths[i] > 0 || column.Flexible;
            }
            while (Required(columns, widths, visible) > width) {
                int drop = -1;
                int visibleCount = 0;
                for (int i = 0; i < count; i++) {
                    if (!visible[i]) {
                        continue;
                    }
                    visibleCount++;
                    if (drop < 0 || columns[i].Priority < columns[drop].Priority) {
                        drop = i;
                    }
                }
                if (visibleCount <= 1) {
                    widths[drop] = Math.Max(1, width);
                    break;
                }
                visible[drop] = false;
            }
            int left = width - Required(columns, widths, visible);
            // Share the rest among flexible columns, each up to its maximum; repeat while some width is unclaimed.
            while (left > 0) {
                List<int> open = new List<int>();
                for (int i = 0; i < count; i++) {
                    if (visible[i] && columns[i].Flexible && (columns[i].MaxWidth <= 0 || widths[i] < columns[i].MaxWidth)) {
                        open.Add(i);
                    }
                }
                if (open.Count == 0) {
                    break;
                }
                int share = Math.Max(1, left / open.Count);
                foreach (int i in open) {
                    int room = columns[i].MaxWidth > 0 ? columns[i].MaxWidth - widths[i] : int.MaxValue;
                    int add = Math.Min(Math.Min(share, room), left);
                    widths[i] += add;
                    left -= add;
                    if (left == 0) {
                        break;
                    }
                }
            }
            for (int i = 0; i < count; i++) {
                if (!visible[i]) {
                    widths[i] = 0;
                }
            }
            return widths;
        }

        internal static void Draw(Region region, int y, IList<ProgressColumn> columns, int[] widths, ProgressRow row, RenderContext context) {
            int x = 0;
            int previous = -1;
            for (int i = 0; i < columns.Count; i++) {
                if (widths[i] <= 0) {
                    continue;
                }
                if (previous >= 0) {
                    x += columns[previous].Name == "Spinner" ? 1 : 2;
                }
                columns[i].Draw(region, x, y, widths[i], row, context);
                x += widths[i];
                previous = i;
            }
        }

        /// <summary>The row as one compact line for stream output: the label and every column that summarizes.</summary>
        internal static string Summarize(IList<ProgressColumn> columns, ProgressRow row, RenderContext context) {
            StringBuilder sb = new StringBuilder(row.Label);
            foreach (ProgressColumn column in columns) {
                if (column.Name == "Description") {
                    continue;
                }
                string? text = column.Summarize(row, context);
                if (!string.IsNullOrEmpty(text)) {
                    sb.Append(' ').Append(text);
                }
            }
            return sb.ToString();
        }

        // One space after the spinner, two between the other columns.

        private static int Required(IList<ProgressColumn> columns, int[] widths, bool[] visible) {
            int total = 0;
            int shown = 0;
            int previous = -1;
            for (int i = 0; i < columns.Count; i++) {
                if (!visible[i]) {
                    continue;
                }
                if (shown > 0) {
                    total += columns[previous].Name == "Spinner" ? 1 : 2;
                }
                total += widths[i];
                previous = i;
                shown++;
            }
            return total;
        }
    }
}
