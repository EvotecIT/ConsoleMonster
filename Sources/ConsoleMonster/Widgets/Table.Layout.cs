using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Table {
        private const int WordCap = 16;

        private TableLayout ComputeLayout(int maxWidth, RenderContext context) {
            int count = _columns.Count;
            TableLayout layout = new TableLayout(count, IsBoxed ? 0 : 2);
            int overhead = IsBoxed ? (count * 3) + 1 : layout.Gap * Math.Max(0, count - 1);
            int available = Math.Max(count, maxWidth - overhead);

            StyledLine[] headers = new StyledLine[count];
            List<StyledLine>[][] raw = new List<StyledLine>[_rows.Count][];
            int[] desired = new int[count];
            int[] minimum = new int[count];
            for (int c = 0; c < count; c++) {
                Style headerStyle = HeaderSeparator ? context.Style(ThemeToken.Header) : context.Style(ThemeToken.Header).With(TextDecoration.Underline);
                headers[c] = context.Markup(_columns[c].Header).Lines[0].WithBaseStyle(headerStyle);
                desired[c] = headers[c].Width;
                minimum[c] = headers[c].Width;
                layout.Alignments[c] = _columns[c].Alignment ?? (IsNumericColumn(c) ? HorizontalAlignment.Right : HorizontalAlignment.Left);
            }
            for (int r = 0; r < _rows.Count; r++) {
                raw[r] = new List<StyledLine>[count];
                for (int c = 0; c < count; c++) {
                    List<StyledLine> lines = CellLines(_rows[r][c], context);
                    raw[r][c] = lines;
                    foreach (StyledLine line in lines) {
                        desired[c] = Math.Max(desired[c], line.Width);
                        minimum[c] = Math.Max(minimum[c], _columns[c].Wrap ? Math.Min(WordCap, LongestWord(line)) : line.Width);
                    }
                }
            }
            List<StyledLine>[]? footerRaw = null;
            if (_footer != null) {
                footerRaw = new List<StyledLine>[count];
                for (int c = 0; c < count; c++) {
                    footerRaw[c] = CellLines(_footer[c], context);
                    foreach (StyledLine line in footerRaw[c]) {
                        desired[c] = Math.Max(desired[c], line.Width);
                    }
                }
            }
            for (int c = 0; c < count; c++) {
                TableColumn column = _columns[c];
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
                minimum[c] = Math.Min(minimum[c], desired[c]);
            }

            int[] widths = Distribute(desired, minimum, available);
            layout.Widths = widths;
            for (int c = 0; c < count; c++) {
                layout.HeaderLines[c] = Fit(new List<StyledLine> { headers[c] }, widths[c], false, context);
                layout.HeaderHeight = Math.Max(layout.HeaderHeight, layout.HeaderLines[c].Count);
            }
            for (int r = 0; r < _rows.Count; r++) {
                List<StyledLine>[] fitted = new List<StyledLine>[count];
                int height = 1;
                for (int c = 0; c < count; c++) {
                    fitted[c] = Fit(raw[r][c], widths[c], _columns[c].Wrap, context);
                    height = Math.Max(height, fitted[c].Count);
                }
                layout.RowLines.Add(fitted);
                layout.RowHeights.Add(height);
            }
            if (footerRaw != null) {
                layout.FooterLines = new List<StyledLine>[count];
                layout.FooterHeight = 1;
                for (int c = 0; c < count; c++) {
                    layout.FooterLines[c] = Fit(footerRaw[c], widths[c], false, context);
                }
            }

            int contentWidth = TrackAllocator.Sum(widths);
            layout.TotalWidth = contentWidth + overhead;
            int total = 0;
            if (!string.IsNullOrEmpty(Title)) {
                total++;
            }
            if (IsBoxed) {
                total += 2;
            }
            if (ShowHeader) {
                total += layout.HeaderHeight + (HeaderSeparator && (IsBoxed || Border == TableBorder.Minimal) ? 1 : 0);
            }
            foreach (int h in layout.RowHeights) {
                total += h;
            }
            if (layout.FooterLines != null) {
                total += layout.FooterHeight + (IsBoxed || Border == TableBorder.Minimal ? 1 : 0);
            }
            if (!string.IsNullOrEmpty(Caption)) {
                total++;
            }
            layout.TotalHeight = total;
            return layout;
        }

        private int[] Distribute(int[] desired, int[] minimum, int available) {
            int count = desired.Length;
            int[] widths = (int[])desired.Clone();
            int sum = TrackAllocator.Sum(widths);
            if (sum <= available) {
                if (Expand && count > 0) {
                    int target = FlexibleColumn(desired);
                    widths[target] += available - sum;
                }
                return widths;
            }
            int need = sum - available;
            while (need > 0) {
                int best = -1;
                int bestSlack = 0;
                for (int c = 0; c < count; c++) {
                    int slack = widths[c] - minimum[c];
                    if (slack > bestSlack || (slack == bestSlack && slack > 0 && best >= 0 && widths[c] > widths[best])) {
                        best = c;
                        bestSlack = slack;
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
                for (int c = 1; c < count; c++) {
                    if (widths[c] > widths[widest]) {
                        widest = c;
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

        private int FlexibleColumn(int[] desired) {
            for (int c = 0; c < _columns.Count; c++) {
                if (_columns[c].Flexible) {
                    return c;
                }
            }
            int best = 0;
            for (int c = 0; c < _columns.Count; c++) {
                if (_columns[c].Wrap && !IsNumericColumn(c) && desired[c] > desired[best]) {
                    best = c;
                }
            }
            return best;
        }

        private static List<StyledLine> Fit(List<StyledLine> lines, int width, bool wrap, RenderContext context) {
            List<StyledLine> result = new List<StyledLine>();
            foreach (StyledLine line in lines) {
                if (line.Width <= width) {
                    result.Add(line);
                } else if (wrap) {
                    result.AddRange(TextWrapper.Wrap(line, width));
                } else {
                    result.Add(TextWrapper.Truncate(line, width, context.Ellipsis));
                }
            }
            if (result.Count == 0) {
                result.Add(StyledLine.Empty);
            }
            return result;
        }

        private static List<StyledLine> CellLines(TableCell cell, RenderContext context) {
            StyledText text = context.Markup(cell.Markup);
            List<StyledLine> lines = new List<StyledLine>(text.Lines);
            if (cell.Status.HasValue && lines.Count > 0) {
                Style style = context.Style(cell.Status.Value);
                List<TextSpan> spans = new List<TextSpan> {
                    new TextSpan(context.Glyphs.StatusMarker(cell.Status.Value), style)
                };
                if (context.Glyphs.Tier != Capabilities.GlyphTier.Ascii || lines[0].Width > 0) {
                    spans.Add(new TextSpan(" ", style));
                    foreach (TextSpan span in lines[0]) {
                        spans.Add(new TextSpan(span.Text, style.Combine(span.Style)));
                    }
                }
                lines[0] = new StyledLine(spans);
            }
            return lines;
        }

        private bool IsNumericColumn(int column) {
            bool any = false;
            foreach (TableCell[] row in _rows) {
                if (column >= row.Length) {
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
