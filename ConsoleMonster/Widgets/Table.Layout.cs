using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public partial class Table {
        private const int WordCap = 16;

        // With deferFit the body rows are not fitted to their column widths yet: a streaming render fits each block just before
        // drawing it and lets it go afterwards.
        private TableLayout ComputeLayout(int maxWidth, RenderContext context, bool deferFit = false) {
            Prepare();
            PresenterContext presenter = Presentation.CreateContext(context);
            if (AlwaysCards && _rows.Count > 0) {
                return new TableLayout(TableMode.Cards) { Record = BuildCards(presenter) };
            }
            if (RecordViewRows > 0 && _rows.Count > 0 && _rows.Count <= RecordViewRows && _columns.Count > 2) {
                return new TableLayout(TableMode.Record) { Record = BuildRecordView(presenter) };
            }
            int count = _columns.Count;
            int gap = IsBoxed ? 0 : 2;
            StyledLine[] headers = new StyledLine[count];
            int[] desired = new int[count];
            int[] minimum = new int[count];
            _freeText = new bool[count];
            for (int c = 0; c < count; c++) {
                Style headerStyle = Border == TableBorder.Rule ? context.Style(ThemeToken.Muted)
                    : HeaderSeparator ? context.Style(ThemeToken.Header) : context.Style(ThemeToken.Header).With(TextDecoration.Underline);
                headers[c] = context.Markup(_columns[c].Header).Lines[0].WithBaseStyle(headerStyle);
                desired[c] = Border == TableBorder.Rule ? 0 : ShowHeader ? headers[c].Width : 0;
                // Headers stay whole up to a word's length; very long headers may shorten.
                minimum[c] = Border == TableBorder.Rule ? 1 : ShowHeader ? Math.Min(headers[c].Width, WordCap) : 1;
            }
            // Streaming: every data row is presented and measured once here, then only its index is kept; its cells are
            // presented again when its block is drawn. Column widths still come from every row.
            StreamStats? stats = deferFit ? new StreamStats(this, count, desired, minimum) : null;
            List<TableLayoutRow> rows = BuildRows(presenter, stats);
            TableLayoutRow? total = BuildTotalRow(presenter);
            foreach (TableLayoutRow row in Enumerate(rows, total)) {
                if (row.Cells == null) {
                    continue;
                }
                for (int c = 0; c < count; c++) {
                    Measure(row.Cells[c], _columns[c], ref desired[c], ref minimum[c], ref _freeText[c]);
                }
            }
            _keyComfort = stats != null ? stats.KeyComfort() : KeyComfort(rows);
            for (int c = 0; c < count; c++) {
                if (desired[c] == 0 && Border == TableBorder.Rule) { desired[c] = ShowHeader ? headers[c].Width : 1; }
                ApplyColumnLimits(c, desired, minimum);
            }

            // Narrow, and even the minimum widths do not fit: cards show every column instead of hiding some.
            List<int> visible = new List<int>();
            for (int c = 0; c < count; c++) {
                visible.Add(c);
            }
            if (KeepAllColumns && count > 1 && SumOf(minimum, visible) + Overhead(visible, gap) > maxWidth) {
                List<Table> bands = BuildColumnBands(maxWidth, minimum, gap);
                Rows widgets = new Rows();
                foreach (Table band in bands) widgets.Add(band);
                return new TableLayout(TableMode.Grid) { Record = widgets, Bands = bands };
            }
            // A natural-width default view may be narrower than its terminal. Its tier still comes from that destination.
            int cardViewport = Border == TableBorder.Rule ? context.Profile.Width : maxWidth;
            if (_rows.Count > 0 && CardsBelow > 0 && cardViewport < CardsBelow && count > 1 && SumOf(minimum, visible) + Overhead(visible, gap) > maxWidth) {
                return new TableLayout(TableMode.Cards) { Record = BuildCards(presenter) };
            }
            // Hide columns by ascending priority (ties: rightmost first) until the minimums fit; the key column stays.
            TableLayout layout = new TableLayout(TableMode.Grid) { Gap = gap, AvailableWidth = maxWidth };
            List<int> hiddenColumns = new List<int>();
            for (int c = 0; c < count; c++) {
                if (c != KeyColumn && !_columns[c].IsKey && _columns[c].Placement == ColumnPlacement.Below && maxWidth <= _columns[c].BelowWidth) {
                    visible.Remove(c); layout.Below.Add(c);
                }
            }
            while (visible.Count > 1 && SumOf(minimum, visible) + Overhead(visible, gap) > maxWidth) {
                int victim = PickHidden(visible);
                if (victim < 0) {
                    break;
                }
                visible.Remove(victim);
                if (_columns[victim].Placement == ColumnPlacement.Below) { layout.Below.Add(victim); }
                else { hiddenColumns.Add(victim); }
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
            int available = Math.Max(visible.Count, maxWidth - Overhead(visible, gap));
            int[] widths = Distribute(visibleDesired, visibleMinimum, available, visible);
            WidenForGroupHeadings(widths, visible, rows, maxWidth, gap);
            layout.Visible = visible.ToArray();
            layout.Gaps = new int[visible.Count];
            for (int v = 1; v < visible.Count; v++) layout.Gaps[v] = Math.Max(0, _columns[visible[v]].Gap ?? gap);
            layout.Widths = widths;
            layout.Alignments = new HorizontalAlignment[visible.Count];
            layout.HeaderLines = new List<StyledLine>[visible.Count];
            for (int v = 0; v < visible.Count; v++) {
                int c = visible[v];
                if (_columns[c].ShortHeader != null && headers[c].Width > widths[v]) {
                    headers[c] = context.Markup(_columns[c].ShortHeader!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted));
                }
                layout.Alignments[v] = _columns[c].Alignment ?? (stats != null ? stats.Alignment(c) ?? AutomaticAlignment(c, rows) : AutomaticAlignment(c, rows));
                layout.HeaderLines[v] = ShowHeader ? Fit(new List<StyledLine> { headers[c] }, widths[v], WrapHeaders ? ColumnOverflow.Wrap : ColumnOverflow.Truncate, 0, context) : new List<StyledLine>();
                layout.HeaderHeight = Math.Max(layout.HeaderHeight, layout.HeaderLines[v].Count);
            }
            foreach (TableLayoutRow row in rows) {
                if (!deferFit) {
                    FitRow(row, layout, context);
                }
                layout.Rows.Add(row);
            }
            // A first group heading takes the place of the header separator, or of a boxed table's top rule without a header.
            layout.MergeFirstGroup = !GroupHeadersAsRows && layout.Rows.Count > 0 && layout.Rows[0].Kind == TableRowKind.Group
                && (ShowHeader ? HeaderSeparator && (IsBoxed || (Border == TableBorder.Minimal || Border == TableBorder.Rule)) : IsBoxed);
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

        private static void Measure(CellContent content, TableColumn column, ref int desired, ref int minimum, ref bool freeText) {
            int text = content.TextWidth;
            int want = content.HasVisual ? text + 1 + content.Presented!.VisualWidth : text;
            desired = Math.Max(desired, want);
            int least;
            if (content.Widget != null) {
                minimum = Math.Max(minimum, Math.Min(12, want));
                return;
            }
            if (content.Atomic || (content.Presented != null && content.Alignment == HorizontalAlignment.Right)) {
                // Short values never wrap or truncate: numbers, sizes and durations (a cut number is a wrong number),
                // booleans, dates, enums and status words. Free text gives way first. A data bar next to a number takes
                // only the space left over.
                minimum = Math.Max(minimum, text);
                return;
            }
            if (!content.IsEmpty) {
                freeText = true;
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
                // The key identifies the row: keep its typical values whole and let low-priority columns hide instead;
                // an unusually long key shortens (in the middle or at the end) rather than starving the other columns.
                minimum[c] = Math.Max(minimum[c], Math.Min(desired[c], Math.Max(column.MinWidth ?? 0, _keyComfort)));
            }
            minimum[c] = Math.Max(1, Math.Min(minimum[c], desired[c]));
        }

        private const int KeyComfortWidth = 32;
        private int _keyComfort = KeyComfortWidth;

        // The 80th-percentile width of the key column's values, between 16 and 32 cells.
        private int KeyComfort(List<TableLayoutRow> rows) {
            int key = KeyColumn;
            List<int> widths = new List<int>();
            foreach (TableLayoutRow row in rows) {
                if (row.Kind == TableRowKind.Data && row.Cells != null) {
                    widths.Add(row.Cells[key].TextWidth);
                }
            }
            if (widths.Count == 0) {
                return KeyComfortWidth;
            }
            widths.Sort();
            int index = Math.Min(widths.Count - 1, (int)Math.Ceiling(widths.Count * 0.8) - 1);
            return Math.Max(16, Math.Min(KeyComfortWidth, widths[Math.Max(0, index)]));
        }

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

        private int Overhead(IReadOnlyList<int> columns, int gap) {
            if (IsBoxed) return Overhead(columns.Count, gap);
            int total = 0;
            for (int v = 1; v < columns.Count; v++) total += Math.Max(0, _columns[columns[v]].Gap ?? gap);
            return total;
        }

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
                    Widen(widths, available - sum, visible);
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
                return _columns[column].Priority - (Border == TableBorder.Rule && _columns[column].Flexible ? 100000 : 0);
            }
            int highest = _columns[column].Priority;
            foreach (TableColumn other in _columns) {
                highest = Math.Max(highest, other.Priority);
            }
            return highest;
        }

        // Whether each column holds free text (values that may wrap or shorten), from the last layout's measure pass.
        private bool[] _freeText = Array.Empty<bool>();

        // Spare width when the table expands: columns with a Weight share it by weight; otherwise Flexible columns share it
        // evenly; otherwise the free-text columns share it in proportion to their widths, so numbers, dates and status words
        // keep their natural width. Fixed and maximum widths hold; what the chosen columns cannot take passes to the next
        // group, and only a table with no other room widens every column.
        private void Widen(int[] widths, int spare, List<int> visible) {
            Func<TableColumn, bool>[] groups = {
                column => column.Weight > 0,
                column => column.Flexible,
                column => IsFreeText(column),
                column => true
            };
            for (int g = 0; g < groups.Length && spare > 0; g++) {
                int[] shares = new int[visible.Count];
                for (int v = 0; v < visible.Count; v++) {
                    TableColumn column = _columns[visible[v]];
                    if (!column.Width.HasValue && groups[g](column)) {
                        shares[v] = g == 0 ? column.Weight : g == 1 ? 1 : Math.Max(1, widths[v]);
                    }
                }
                spare = Grow(widths, shares, spare, visible);
            }
        }

        // Shares spare width by the shares, largest remainders first; columns that reach their maximum drop out. Returns what is left.
        private int Grow(int[] widths, int[] shares, int spare, List<int> visible) {
            while (spare > 0) {
                long total = 0;
                for (int v = 0; v < visible.Count; v++) {
                    if (shares[v] > 0 && !AtMaximum(widths[v], visible[v])) { total += shares[v]; }
                }
                if (total == 0) {
                    return spare;
                }
                int given = 0;
                int[] extra = new int[visible.Count];
                long[] remainders = new long[visible.Count];
                for (int v = 0; v < visible.Count; v++) {
                    if (shares[v] <= 0 || AtMaximum(widths[v], visible[v])) { remainders[v] = -1; continue; }
                    long exact = (long)spare * shares[v];
                    extra[v] = (int)(exact / total);
                    remainders[v] = exact % total;
                    given += extra[v];
                }
                for (int left = spare - given; left > 0; left--) {
                    int best = -1;
                    for (int v = 0; v < visible.Count; v++) {
                        if (remainders[v] >= 0 && (best < 0 || remainders[v] > remainders[best])) { best = v; }
                    }
                    extra[best]++;
                    remainders[best] = -1;
                    given++;
                }
                int used = 0;
                for (int v = 0; v < visible.Count; v++) {
                    int room = _columns[visible[v]].MaxWidth is int max ? Math.Max(0, max - widths[v]) : int.MaxValue;
                    int add = Math.Min(extra[v], room);
                    widths[v] += add;
                    used += add;
                }
                if (used == 0) {
                    return spare;
                }
                spare -= used;
            }
            return spare;
        }

        private bool IsFreeText(TableColumn column) {
            int index = _columns.IndexOf(column);
            return index >= 0 && index < _freeText.Length && _freeText[index];
        }

        private bool AtMaximum(int width, int column) => _columns[column].MaxWidth is int max && width >= max;

        private void FitRow(TableLayoutRow row, TableLayout layout, RenderContext context) {
            if (row.Cells == null) {
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
                row.Lines[v] = content.Widget != null
                    ? content.LinesAtWidth(textWidth, context)
                    : Fit(content.Lines, textWidth, column.Overflow, column.MaxLines > 0 ? column.MaxLines : WrapLines, context);
                row.VisualCells[v] = visual;
                height = Math.Max(height, row.Lines[v].Count);
            }
            row.BodyHeight = height;
            row.Below = null;
            if (layout.Below.Count > 0) {
                int indent = layout.Visible.Length > 1 ? layout.Widths[0] + layout.Gaps[1] : 2;
                int lineWidth = Math.Max(1, layout.AvailableWidth - indent);
                row.Below = new List<StyledLine>();
                foreach (int c in layout.Below) {
                    foreach (StyledLine line in row.Cells[c].Lines) {
                        if (line.Width == 0) { continue; }
                        foreach (StyledLine wrapped in TextWrapper.Wrap(line.WithBaseStyle(context.Style(ThemeToken.Muted)), lineWidth)) {
                            List<TextSpan> spans = new List<TextSpan> { new TextSpan(new string(' ', indent)) };
                            spans.AddRange(wrapped); row.Below.Add(new StyledLine(spans));
                        }
                    }
                }
                height += row.Below.Count;
            }
            row.Height = height;
        }

        private void FinishLayout(TableLayout layout, int maxWidth, RenderContext context) {
            layout.TotalWidth = TrackAllocator.Sum(layout.Widths) + Overhead(layout.Visible, layout.Gap);
            foreach (TableLayoutRow row in layout.Rows) {
                if (row.Below != null) { foreach (StyledLine detail in row.Below) { layout.TotalWidth = Math.Max(layout.TotalWidth, detail.Width); } }
            }
            if (layout.Hidden.Count > 0 && ShowHiddenColumns) {
                string names = string.Join(", ", layout.Hidden.ToArray());
                string text = Localization.WidgetLabels.Format(layout.Hidden.Count == 1 ? context.Labels.OneHiddenColumn : context.Labels.ManyHiddenColumns, layout.Hidden.Count.ToString(CultureInfo.InvariantCulture), names);
                layout.HiddenMarker = TextWrapper.Truncate(StyledLine.From(text, context.Style(ThemeToken.Muted)), Math.Max(1, maxWidth - (IsBoxed ? 1 : 0)), context.Ellipsis);
            }
            int shown = DisplayedRowCount;
            int all = TotalRowCount ?? _rows.Count;
            if (shown < all) {
                CultureInfo culture = Presentation.Culture ?? context.Culture;
                string text = Localization.WidgetLabels.Format(context.Labels.Showing, shown.ToString("N0", culture), all.ToString("N0", culture));
                // With groups: how many of them the shown rows cover, "Showing 5 of 1 214 · 2 of 612 groups".
                int groups = GroupsWithRows(_rowGroups.Count);
                if (groups > 0) {
                    int visibleGroups = GroupsWithRows(shown);
                    string separator = context.Glyphs.Tier == Capabilities.GlyphTier.Ascii ? " - " : " · ";
                    text += separator + (visibleGroups < groups
                        ? Localization.WidgetLabels.Format(context.Labels.GroupsShowing, visibleGroups.ToString("N0", culture), groups.ToString("N0", culture))
                        : Localization.WidgetLabels.Format(groups == 1 ? context.Labels.OneGroup : context.Labels.ManyGroups, groups.ToString("N0", culture)));
                }
                layout.ShowingLine = TextWrapper.Truncate(StyledLine.From(text, context.Style(ThemeToken.Muted)), Math.Max(1, maxWidth - (IsBoxed ? 1 : 0)), context.Ellipsis);
            }
            int height = 0;
            if (Border == TableBorder.Rule && (layout.HiddenMarker != null || layout.ShowingLine != null)) {
                ViewHint hint = new ViewHint { MoreRows = Math.Max(0, all - shown) };
                if (ShowHiddenColumns) foreach (string column in layout.Hidden) hint.Columns.Add(column);
                layout.HintLines.AddRange(hint.Lines(maxWidth, context));
                layout.HiddenMarker = null; layout.ShowingLine = null;
                foreach (StyledLine line in layout.HintLines) layout.TotalWidth = Math.Max(layout.TotalWidth, line.Width);
                height += layout.HintLines.Count;
            }
            if (!string.IsNullOrEmpty(Title)) {
                height++;
            }
            if (IsBoxed) {
                height += layout.MergeFirstGroup && !ShowHeader ? 1 : 2;
            }
            if (ShowHeader) {
                height += layout.HeaderHeight + (HeaderSeparator && (IsBoxed || (Border == TableBorder.Minimal || Border == TableBorder.Rule)) && !layout.MergeFirstGroup ? 1 : 0);
            }
            foreach (TableLayoutRow row in layout.Rows) {
                height += row.Height;
            }
            if (layout.Total != null) {
                height += layout.Total.Height + (IsBoxed || (Border == TableBorder.Minimal || Border == TableBorder.Rule) ? 1 : 0);
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

        // A group heading is drawn into the rule over the first column ("├─ Domain controllers · 5 ─┼"), so the first column
        // is widened to hold the widest heading when the window has room and the growth is modest (at most its own width or
        // eight cells), and the column lines stay joined; the other columns keep their widths. A narrow first column such
        // as a process Id is not stretched to several times its width for a long heading: the table is then only made at
        // least as wide as the heading (the last columns take the width), so it is not cut short, and the heading crosses
        // into the next columns. A column with a fixed Width or MaxWidth is not widened past it.
        private void WidenForGroupHeadings(int[] widths, List<int> visible, List<TableLayoutRow> rows, int maxWidth, int gap) {
            int label = 0;
            foreach (TableLayoutRow row in rows) {
                if (row.Kind == TableRowKind.Group && row.Spanning != null) {
                    label = Math.Max(label, row.Spanning.Width);
                }
            }
            if (label == 0 || widths.Length == 0) {
                return;
            }
            int free = maxWidth - (TrackAllocator.Sum(widths) + Overhead(visible, gap));
            if (free <= 0) {
                return;
            }
            bool ruled = !GroupHeadersAsRows && (IsBoxed || (Border == TableBorder.Minimal || Border == TableBorder.Rule));
            if (ruled) {
                // Boxed "├─ label ─┼": the label, a space either side and a rule glyph either side; Minimal "─ label ─ ".
                int want = label + (IsBoxed ? 2 : 4);
                int grow = want - widths[0] <= Math.Max(8, widths[0]) ? Math.Min(Math.Min(want, CapOf(visible[0])) - widths[0], free) : 0;
                if (grow > 0) {
                    widths[0] += grow;
                    free -= grow;
                }
                if (widths[0] >= want) {
                    return;
                }
            }
            int needed = label + (IsBoxed ? GroupHeadersAsRows ? 4 : 6 : ruled ? 3 : 0);
            int extra = Math.Min(needed - (TrackAllocator.Sum(widths) + Overhead(visible, gap)), free);
            if (extra <= 0) {
                return;
            }
            // The last columns take it, past the heading, rather than stretching the first one.
            for (int v = widths.Length - 1; v >= 0; v--) {
                int room = CapOf(visible[v]) - widths[v];
                if (room > 0) {
                    int take = Math.Min(room, extra);
                    widths[v] += take;
                    extra -= take;
                    if (extra == 0) {
                        return;
                    }
                }
            }
        }

        // The widest a column may be made for a group heading: its fixed Width or MaxWidth, otherwise unbounded.
        private int CapOf(int column) => _columns[column].Width ?? _columns[column].MaxWidth ?? int.MaxValue;

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

        private static List<StyledLine> Fit(List<StyledLine> lines, int width, ColumnOverflow overflow, int maxLines, RenderContext context) =>
            CellRendering.FitLines(lines, width, overflow, maxLines, context);

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
