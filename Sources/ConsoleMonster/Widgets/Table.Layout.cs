using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Table {
        private const int WordCap = 16;

        private TableLayout ComputeLayout(int maxWidth, RenderContext context) {
            PresenterContext presenter = Presentation.CreateContext(context);
            if (RecordViewRows > 0 && _rows.Count > 0 && _rows.Count <= RecordViewRows && _columns.Count > 2) {
                return new TableLayout(TableMode.Record) { Record = BuildRecordView(presenter) };
            }
            int count = _columns.Count;
            int gap = IsBoxed ? 0 : 2;
            List<TableLayoutRow> rows = BuildRows(presenter);
            TableLayoutRow? total = BuildTotalRow(presenter);

            StyledLine[] headers = new StyledLine[count];
            int[] desired = new int[count];
            int[] minimum = new int[count];
            for (int c = 0; c < count; c++) {
                Style headerStyle = HeaderSeparator ? context.Style(ThemeToken.Header) : context.Style(ThemeToken.Header).With(TextDecoration.Underline);
                headers[c] = context.Markup(_columns[c].Header).Lines[0].WithBaseStyle(headerStyle);
                desired[c] = ShowHeader ? headers[c].Width : 0;
                // Headers stay whole up to a word's length; very long headers may shorten.
                minimum[c] = ShowHeader ? Math.Min(headers[c].Width, WordCap) : 1;
            }
            foreach (TableLayoutRow row in Enumerate(rows, total)) {
                if (row.Cells == null) {
                    continue;
                }
                for (int c = 0; c < count; c++) {
                    Measure(row.Cells[c], _columns[c], ref desired[c], ref minimum[c]);
                }
            }
            for (int c = 0; c < count; c++) {
                ApplyColumnLimits(c, desired, minimum);
            }

            // Narrow, and even the minimum widths do not fit: cards show every column instead of hiding some.
            List<int> visible = new List<int>();
            for (int c = 0; c < count; c++) {
                visible.Add(c);
            }
            if (CardsBelow > 0 && maxWidth < CardsBelow && _rows.Count > 0 && count > 1 && SumOf(minimum, visible) + Overhead(count, gap) > maxWidth) {
                return new TableLayout(TableMode.Cards) { Record = BuildCards(presenter) };
            }
            // Hide columns by ascending priority (ties: rightmost first) until the minimums fit; the key column stays.
            TableLayout layout = new TableLayout(TableMode.Grid) { Gap = gap };
            List<int> hiddenColumns = new List<int>();
            while (visible.Count > 1 && SumOf(minimum, visible) + Overhead(visible.Count, gap) > maxWidth) {
                int victim = PickHidden(visible);
                if (victim < 0) {
                    break;
                }
                visible.Remove(victim);
                hiddenColumns.Add(victim);
            }
            hiddenColumns.Sort();
            foreach (int hidden in hiddenColumns) {
                layout.Hidden.Add(Markup.Strip(_columns[hidden].Header));
            }
            if (layout.Hidden.Count > 0 && visible.Count == 1 && count > 1 && CardsBelow > 0) {
                return new TableLayout(TableMode.Cards) { Record = BuildCards(presenter) };
            }

            int[] visibleDesired = new int[visible.Count];
            int[] visibleMinimum = new int[visible.Count];
            for (int v = 0; v < visible.Count; v++) {
                visibleDesired[v] = desired[visible[v]];
                visibleMinimum[v] = minimum[visible[v]];
            }
            int available = Math.Max(visible.Count, maxWidth - Overhead(visible.Count, gap));
            int[] widths = Distribute(visibleDesired, visibleMinimum, available, visible);
            layout.Visible = visible.ToArray();
            layout.Widths = widths;
            layout.Alignments = new HorizontalAlignment[visible.Count];
            layout.HeaderLines = new List<StyledLine>[visible.Count];
            for (int v = 0; v < visible.Count; v++) {
                int c = visible[v];
                layout.Alignments[v] = _columns[c].Alignment ?? AutomaticAlignment(c, rows);
                layout.HeaderLines[v] = ShowHeader ? Fit(new List<StyledLine> { headers[c] }, widths[v], ColumnOverflow.Truncate, 0, context) : new List<StyledLine>();
                layout.HeaderHeight = Math.Max(layout.HeaderHeight, layout.HeaderLines[v].Count);
            }
            foreach (TableLayoutRow row in rows) {
                FitRow(row, layout, context);
                layout.Rows.Add(row);
            }
            if (total != null) {
                FitRow(total, layout, context);
                layout.Total = total;
            }
            FinishLayout(layout, maxWidth, context);
            return layout;
        }

        private static IEnumerable<TableLayoutRow> Enumerate(List<TableLayoutRow> rows, TableLayoutRow? total) {
            foreach (TableLayoutRow row in rows) {
                yield return row;
            }
            if (total != null) {
                yield return total;
            }
        }

        private static void Measure(CellContent content, TableColumn column, ref int desired, ref int minimum) {
            int text = content.TextWidth;
            int want = content.HasVisual ? text + 1 + content.Presented!.VisualWidth : text;
            desired = Math.Max(desired, want);
            int least;
            if (content.Presented != null && content.Alignment == HorizontalAlignment.Right) {
                // Numbers, sizes and durations never wrap or truncate: a cut number is a wrong number. A data bar
                // next to the number takes only the space left over.
                minimum = Math.Max(minimum, text);
                return;
            }
            switch (column.Overflow) {
                case ColumnOverflow.Never:
                    least = text;
                    break;
                case ColumnOverflow.Wrap:
                    least = 0;
                    foreach (StyledLine line in content.Lines) {
                        least = Math.Max(least, Math.Min(WordCap, LongestWord(line)));
                    }
                    break;
                default:
                    // Truncated values stay long enough to recognise (a path's drive and file name, a hash prefix).
                    least = Math.Min(text, Math.Max(column.MinWidth ?? 0, 12));
                    break;
            }
            minimum = Math.Max(minimum, least);
        }

        private void ApplyColumnLimits(int c, int[] desired, int[] minimum) {
            TableColumn column = _columns[c];
            if (StableWidths && _stableWidths.TryGetValue(column.Id, out int seen)) {
                desired[c] = Math.Max(desired[c], seen);
            }
            if (StableWidths) {
                _stableWidths[column.Id] = desired[c];
            }
            if (column.Width.HasValue) {
                desired[c] = column.Width.Value;
                minimum[c] = column.Width.Value;
            }
            if (column.MaxWidth.HasValue) {
                desired[c] = Math.Min(desired[c], column.MaxWidth.Value);
                minimum[c] = Math.Min(minimum[c], column.MaxWidth.Value);
            }
            if (column.MinWidth.HasValue) {
                desired[c] = Math.Max(desired[c], column.MinWidth.Value);
                minimum[c] = Math.Max(minimum[c], column.MinWidth.Value);
            }
            if (c == KeyColumn || column.IsKey) {
                // The key identifies the row: keep it readable and let low-priority columns hide instead.
                minimum[c] = Math.Max(minimum[c], Math.Min(desired[c], Math.Max(column.MinWidth ?? 0, KeyComfortWidth)));
            }
            minimum[c] = Math.Max(1, Math.Min(minimum[c], desired[c]));
        }

        private const int KeyComfortWidth = 24;

        private int PickHidden(List<int> visible) {
            int key = KeyColumn;
            int victim = -1;
            foreach (int c in visible) {
                if (c == key || _columns[c].IsKey) {
                    continue;
                }
                if (victim < 0 || _columns[c].Priority < _columns[victim].Priority || (_columns[c].Priority == _columns[victim].Priority && c > victim)) {
                    victim = c;
                }
            }
            return victim;
        }

        private int Overhead(int columns, int gap) => IsBoxed ? (columns * 3) + 1 : gap * Math.Max(0, columns - 1);

        private static int SumOf(int[] values, List<int> indexes) {
            int sum = 0;
            foreach (int i in indexes) {
                sum += values[i];
            }
            return sum;
        }

        private int[] Distribute(int[] desired, int[] minimum, int available, List<int> visible) {
            int count = desired.Length;
            int[] widths = (int[])desired.Clone();
            int sum = TrackAllocator.Sum(widths);
            if (sum <= available) {
                if (Expand && count > 0) {
                    widths[FlexibleColumn(desired, visible)] += available - sum;
                }
                return widths;
            }
            int need = sum - available;
            // Low-priority columns give way first, down to their minimum; within a priority, the column with the most slack.
            while (need > 0) {
                int best = -1;
                for (int v = 0; v < count; v++) {
                    if (widths[v] <= minimum[v]) {
                        continue;
                    }
                    if (best < 0) {
                        best = v;
                        continue;
                    }
                    int priority = ShrinkPriority(visible[v]);
                    int bestPriority = ShrinkPriority(visible[best]);
                    if (priority < bestPriority || (priority == bestPriority && widths[v] - minimum[v] > widths[best] - minimum[best])) {
                        best = v;
                    }
                }
                if (best < 0) {
                    break;
                }
                widths[best]--;
                need--;
            }
            while (need > 0) {
                int widest = 0;
                for (int v = 1; v < count; v++) {
                    if (widths[v] > widths[widest]) {
                        widest = v;
                    }
                }
                if (widths[widest] <= 3) {
                    break;
                }
                widths[widest]--;
                need--;
            }
            return widths;
        }

        // Key columns shrink only with the most important columns (down to their comfortable minimum); others by priority.
        private int ShrinkPriority(int column) {
            if (column != KeyColumn && !_columns[column].IsKey) {
                return _columns[column].Priority;
            }
            int highest = _columns[column].Priority;
            foreach (TableColumn other in _columns) {
                highest = Math.Max(highest, other.Priority);
            }
            return highest;
        }

        private int FlexibleColumn(int[] desired, List<int> visible) {
            for (int v = 0; v < visible.Count; v++) {
                if (_columns[visible[v]].Flexible) {
                    return v;
                }
            }
            int best = 0;
            for (int v = 0; v < visible.Count; v++) {
                if (_columns[visible[v]].Overflow == ColumnOverflow.Wrap && desired[v] > desired[best]) {
                    best = v;
                }
            }
            return best;
        }

        private void FitRow(TableLayoutRow row, TableLayout layout, RenderContext context) {
            if (row.Cells == null) {
                row.Height = 1;
                return;
            }
            int visibleCount = layout.Visible.Length;
            row.Lines = new List<StyledLine>[visibleCount];
            row.VisualCells = new int[visibleCount];
            int height = 1;
            for (int v = 0; v < visibleCount; v++) {
                int c = layout.Visible[v];
                CellContent content = row.Cells[c];
                int width = layout.Widths[v];
                int visual = CellRendering.VisualCells(content, width, content.TextWidth);
                int textWidth = Math.Max(1, width - visual - (visual > 0 ? 1 : 0));
                TableColumn column = _columns[c];
                row.Lines[v] = Fit(content.Lines, textWidth, column.Overflow, column.MaxLines, context);
                row.VisualCells[v] = visual;
                height = Math.Max(height, row.Lines[v].Count);
            }
            row.Height = height;
        }

        private void FinishLayout(TableLayout layout, int maxWidth, RenderContext context) {
            layout.TotalWidth = TrackAllocator.Sum(layout.Widths) + Overhead(layout.Visible.Length, layout.Gap);
            if (layout.Hidden.Count > 0 && ShowHiddenColumns) {
                string names = string.Join(", ", layout.Hidden.ToArray());
                string text = "+" + layout.Hidden.Count.ToString(CultureInfo.InvariantCulture) + (layout.Hidden.Count == 1 ? " column (" : " columns (") + names + ")";
                layout.HiddenMarker = TextWrapper.Truncate(StyledLine.From(text, context.Style(ThemeToken.Muted)), Math.Max(1, maxWidth - (IsBoxed ? 1 : 0)), context.Ellipsis);
            }
            int shown = DisplayedRowCount;
            int all = TotalRowCount ?? _rows.Count;
            if (shown < all) {
                string text = "Showing " + shown.ToString("N0", Presentation.Culture ?? CultureInfo.CurrentCulture) + " of " + all.ToString("N0", Presentation.Culture ?? CultureInfo.CurrentCulture);
                layout.ShowingLine = TextWrapper.Truncate(StyledLine.From(text, context.Style(ThemeToken.Muted)), Math.Max(1, maxWidth - (IsBoxed ? 1 : 0)), context.Ellipsis);
            }
            int height = 0;
            if (!string.IsNullOrEmpty(Title)) {
                height++;
            }
            if (IsBoxed) {
                height += 2;
            }
            if (ShowHeader) {
                height += layout.HeaderHeight + (HeaderSeparator && (IsBoxed || Border == TableBorder.Minimal) ? 1 : 0);
            }
            foreach (TableLayoutRow row in layout.Rows) {
                height += row.Height;
            }
            if (layout.Total != null) {
                height += layout.Total.Height + (IsBoxed || Border == TableBorder.Minimal ? 1 : 0);
            }
            if (layout.HiddenMarker != null) {
                height++;
            }
            if (layout.ShowingLine != null) {
                height++;
            }
            if (!string.IsNullOrEmpty(Caption)) {
                height++;
            }
            layout.TotalHeight = height;
        }

        private HorizontalAlignment AutomaticAlignment(int column, List<TableLayoutRow> rows) {
            int right = 0;
            int other = 0;
            foreach (TableLayoutRow row in rows) {
                if (row.Kind != TableRowKind.Data || row.Cells == null) {
                    continue;
                }
                CellContent content = row.Cells[column];
                if (content.Presented == null || content.IsEmpty) {
                    continue;
                }
                if (content.Alignment == HorizontalAlignment.Right) {
                    right++;
                } else {
                    other++;
                }
            }
            if (right + other > 0) {
                return right > other ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            return IsNumericColumn(column) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        private static List<StyledLine> Fit(List<StyledLine> lines, int width, ColumnOverflow overflow, int maxLines, RenderContext context) {
            List<StyledLine> result = new List<StyledLine>();
            foreach (StyledLine line in lines) {
                if (line.Width <= width) {
                    result.Add(line);
                } else if (overflow == ColumnOverflow.Wrap) {
                    result.AddRange(TextWrapper.Wrap(line, width));
                } else if (overflow == ColumnOverflow.TruncateMiddle) {
                    result.Add(TruncateMiddle(line, width, context));
                } else {
                    result.Add(TextWrapper.Truncate(line, width, context.Ellipsis));
                }
            }
            if (maxLines > 0 && result.Count > maxLines) {
                // Keep MaxLines lines; the last one ends with an ellipsis so the cut is visible.
                StyledLine last = result[maxLines - 1];
                int room = Math.Max(0, width - DisplayWidth.Measure(context.Ellipsis));
                StyledLine kept = last.Width <= room ? last : TextWrapper.Truncate(last, room, string.Empty);
                List<TextSpan> spans = new List<TextSpan>(kept);
                spans.Add(new TextSpan(context.Ellipsis, context.Style(ThemeToken.Muted)));
                result.RemoveRange(maxLines - 1, result.Count - maxLines + 1);
                result.Add(new StyledLine(spans));
            }            if (result.Count == 0) {
                result.Add(StyledLine.Empty);
            }
            return result;
        }

        private static StyledLine TruncateMiddle(StyledLine line, int width, RenderContext context) {
            Style style = Style.Plain;
            foreach (TextSpan span in line) {
                style = span.Style;
                break;
            }
            return StyledLine.From(TextWrapper.TruncateMiddle(line.PlainText, width, context.Ellipsis), style);
        }

        private bool IsNumericColumn(int column) {
            bool any = false;
            foreach (TableCell[] row in _rows) {
                if (column >= row.Length || row[column].HasValue) {
                    continue;
                }
                string text = Markup.Strip(row[column].Markup).Trim();
                if (text.Length == 0 || text == "-") {
                    continue;
                }
                if (row[column].Status.HasValue || !LooksNumeric(text)) {
                    return false;
                }
                any = true;
            }
            return any;
        }

        private static bool LooksNumeric(string text) {
            string value = text.Replace(",", string.Empty).Replace(" ", string.Empty);
            string[] suffixes = { "ms", "us", "%", "s", "m", "h", "d", "k", "K", "M", "B", "KB", "MB", "GB", "TB", "x" };
            foreach (string suffix in suffixes) {
                if (value.Length > suffix.Length && value.EndsWith(suffix, StringComparison.Ordinal)) {
                    value = value.Substring(0, value.Length - suffix.Length);
                    break;
                }
            }
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        private static int LongestWord(StyledLine line) {
            int longest = 0;
            foreach (string word in line.PlainText.Split(' ')) {
                longest = Math.Max(longest, DisplayWidth.Measure(word));
            }
            return longest;
        }
    }
}
