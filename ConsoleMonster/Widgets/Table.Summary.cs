using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public partial class Table {
        private static readonly ICellPresenter CountPresenter = new NumberPresenter();

        private int DisplayedRowCount => MaxRows > 0 ? Math.Min(MaxRows, _rows.Count) : _rows.Count;

        // Per column: the largest number (data bars scale to it) and the decimals its numbers share (the most any value
        // needs, at most two), so a column of 7 / 11.7 / 344.4 reads 7,0 / 11,7 / 344,4 with the separators in line.
        private double?[] ColumnMaxima(out int?[] decimals) {
            double?[] maxima = new double?[_columns.Count];
            decimals = new int?[_columns.Count];
            foreach (TableCell[] row in _rows) {
                for (int c = 0; c < _columns.Count && c < row.Length; c++) {
                    if (row[c].HasValue && PresenterValues.TryGetNumber(row[c].Value, out double number)) {
                        maxima[c] = maxima[c].HasValue ? Math.Max(maxima[c]!.Value, number) : number;
                        if (!(row[c].Value is TimeSpan)) {
                            int needed = PresenterContext.DecimalsNeeded(row[c].Value);
                            decimals[c] = decimals[c].HasValue ? Math.Max(decimals[c]!.Value, needed) : needed;
                        }
                    }
                }
            }
            return maxima;
        }

        private PresenterContext[] ColumnContexts(PresenterContext presenter) {
            double?[] maxima = ColumnMaxima(out int?[] decimals);
            PresenterContext[] contexts = new PresenterContext[_columns.Count];
            for (int c = 0; c < _columns.Count; c++) {
                contexts[c] = presenter.WithColumn(maxima[c], decimals[c]);
            }
            return contexts;
        }

        private CellContent[] Present(TableCell[] cells, PresenterContext[] contexts) {
            CellContent[] contents = new CellContent[_columns.Count];
            for (int c = 0; c < _columns.Count; c++) {
                TableColumn column = _columns[c];
                TableCell cell = cells[c];
                if (cell.HasValue && cell.Value == null && column.UnknownText != null) {
                    cell = new TableCell("[muted]" + Markup.Escape(column.UnknownText) + "[/]");
                }
                contents[c] = CellRendering.Content(cell, column.Presenter, (IReadOnlyList<RoleRule>)column.RoleRules, contexts[c]);
            }
            return contents;
        }

        private List<TableLayoutRow> BuildRows(PresenterContext presenter, StreamStats? stats = null) {
            PresenterContext[] contexts = ColumnContexts(presenter);
            List<TableLayoutRow> rows = new List<TableLayoutRow>();
            int shown = DisplayedRowCount;
            int group = int.MinValue;
            List<int> groupRows = new List<int>();
            Dictionary<int, int> visible = VisibleGroupRows(shown);
            for (int r = 0; r < shown; r++) {
                if (_rowGroups[r] != group) {
                    AddSubtotal(rows, groupRows, contexts);
                    group = _rowGroups[r];
                    groupRows.Clear();
                    if (group >= 0) {
                        TableLayoutRow heading = GroupRow(group, presenter, visible.TryGetValue(group, out int count) ? count : 0);
                        if (rows.Count > 0) heading.Height += Math.Max(0, GroupGap);
                        rows.Add(heading);
                    }
                }
                groupRows.Add(r);
                CellContent[] cells = Present(_rows[r], contexts);
                for (int c = 0; c < _columns.Count; c++) {
                    if (_columns[c].DimRepeats && r > 0 && _rowGroups[r] == _rowGroups[r - 1]) {
                        TableCell previous = _rows[r - 1][c];
                        TableCell current = _rows[r][c];
                        if (current.HasValue == previous.HasValue && (current.HasValue ? Equals(current.Value, previous.Value) : current.Markup == previous.Markup)) {
                            for (int line = 0; line < cells[c].Lines.Count; line++) { cells[c].Lines[line] = cells[c].Lines[line].WithBaseStyle(presenter.Render.Style(ThemeToken.Muted)); }
                        }
                    }
                }
                if (stats != null) {
                    stats.Add(cells);
                    rows.Add(new TableLayoutRow(TableRowKind.Data) { Source = r });
                } else {
                    rows.Add(new TableLayoutRow(TableRowKind.Data) { Cells = cells, Source = r });
                }
            }
            AddSubtotal(rows, groupRows, contexts);
            return rows;
        }

        // How many groups have at least one of the first rowCount rows.
        private int GroupsWithRows(int rowCount) {
            HashSet<int> groups = new HashSet<int>();
            for (int r = 0; r < rowCount && r < _rowGroups.Count; r++) {
                if (_rowGroups[r] >= 0) {
                    groups.Add(_rowGroups[r]);
                }
            }
            return groups.Count;
        }

        // Rows shown per group (the first DisplayedRowCount rows, after -Top).
        private Dictionary<int, int> VisibleGroupRows(int shown) {
            Dictionary<int, int> visible = new Dictionary<int, int>();
            for (int r = 0; r < shown && r < _rowGroups.Count; r++) {
                visible[_rowGroups[r]] = visible.TryGetValue(_rowGroups[r], out int count) ? count + 1 : 1;
            }
            return visible;
        }

        private int GroupRowCount(int group) {
            int count = 0;
            for (int r = 0; r < _rowGroups.Count; r++) {
                if (_rowGroups[r] == group) {
                    count++;
                }
            }
            return count;
        }

        // A group heading: the key in bold and its row count, "1Password · 5", or "1Password · 2 of 3" when -Top cut it.
        internal StyledLine GroupLabel(int group, PresenterContext presenter, int shown) {
            int total = GroupRowCount(group);
            List<TextSpan> spans = new List<TextSpan>();
            foreach (TextSpan span in presenter.Render.Markup(_groups[group]).Lines[0].WithBaseStyle(presenter.Render.Style(Border == TableBorder.Rule ? ThemeToken.Strong : GroupHeadersAsRows ? ThemeToken.Accent : ThemeToken.Header).With(TextDecoration.Bold))) {
                spans.Add(span);
            }
            if (!ShowGroupCounts) { return new StyledLine(spans); }
            string separator = presenter.Render.Glyphs.Tier == Capabilities.GlyphTier.Ascii ? " - " : " · ";
            string count = shown < total
                ? Localization.WidgetLabels.Format(presenter.Render.Labels.GroupOf, shown.ToString("N0", presenter.Culture), total.ToString("N0", presenter.Culture))
                : total.ToString("N0", presenter.Culture);
            spans.Add(new TextSpan(separator + count, presenter.Render.Style(ThemeToken.Muted)));
            return new StyledLine(spans);
        }

        private TableLayoutRow GroupRow(int group, PresenterContext presenter, int shown) =>
            new TableLayoutRow(TableRowKind.Group) { Spanning = GroupLabel(group, presenter, shown) };

        private void AddSubtotal(List<TableLayoutRow> rows, List<int> groupRows, PresenterContext[] contexts) {
            if (!ShowSubtotals || groupRows.Count == 0 || _groups.Count == 0 || !HasAggregates) {
                return;
            }
            rows.Add(new TableLayoutRow(TableRowKind.Subtotal) { Cells = SummaryCells(groupRows, contexts[0].Render.Labels.Subtotal, contexts, contexts[0].Render.Style(ThemeToken.Muted), false) });
        }

        private TableLayoutRow? BuildTotalRow(PresenterContext presenter) {
            PresenterContext[] contexts = ColumnContexts(presenter);
            if (_footer != null) {
                return new TableLayoutRow(TableRowKind.Total) { Cells = Present(_footer, contexts) };
            }
            if (!ShowTotals || !HasAggregates || _rows.Count == 0) {
                return null;
            }
            List<int> all = new List<int>();
            for (int r = 0; r < _rows.Count; r++) {
                all.Add(r);
            }
            return new TableLayoutRow(TableRowKind.Total) { Cells = SummaryCells(all, TotalLabel ?? presenter.Render.Labels.Total, contexts, presenter.Render.Style(ThemeToken.Text), true) };
        }

        private bool HasAggregates {
            get {
                foreach (TableColumn column in _columns) {
                    if (EffectiveAggregate(column) != ColumnAggregate.None) {
                        return true;
                    }
                }
                return false;
            }
        }

        private CellContent[] SummaryCells(List<int> rows, string label, PresenterContext[] contexts, Style tone, bool bold) {
            CellContent[] cells = new CellContent[_columns.Count];
            int labelColumn = EffectiveAggregate(_columns[KeyColumn]) == ColumnAggregate.None ? KeyColumn : FirstPlainColumn();
            for (int c = 0; c < _columns.Count; c++) {
                TableColumn column = _columns[c];
                CellContent content;
                if (c == labelColumn) {
                    content = new CellContent(new List<StyledLine> { StyledLine.From(label, tone) }, null);
                } else if (EffectiveAggregate(column) == ColumnAggregate.None) {
                    content = new CellContent(new List<StyledLine> { StyledLine.Empty }, null);
                } else {
                    object? value = AggregateOf(column, c, rows);
                    ICellPresenter? presenter = EffectiveAggregate(column) == ColumnAggregate.Count ? CountPresenter : column.Presenter;
                    // An average of whole numbers keeps its own decimals (71.5 %), never fewer than the column's.
                    PresenterContext context = contexts[c].ColumnDecimals.HasValue
                        ? contexts[c].WithColumn(contexts[c].ColumnMaximum, Math.Max(contexts[c].ColumnDecimals!.Value, PresenterContext.DecimalsNeeded(value)))
                        : contexts[c];
                    PresentedValue presented = CellPresenters.Present(value, presenter, (IReadOnlyList<RoleRule>)column.RoleRules, context);
                    // Totals show the number only; a data bar of a sum would not share the rows' scale.
                    PresentedValue plain = new PresentedValue(presented.Line, presented.Alignment) { Number = presented.Number, Role = presented.Role };
                    content = new CellContent(new List<StyledLine> { Restyle(plain.Line, tone, bold) }, plain);
                }
                cells[c] = content;
            }
            return cells;
        }

        private int FirstPlainColumn() {
            for (int c = 0; c < _columns.Count; c++) {
                if (EffectiveAggregate(_columns[c]) == ColumnAggregate.None) {
                    return c;
                }
            }
            return -1;
        }

        private static StyledLine Restyle(StyledLine line, Style tone, bool bold) {
            List<TextSpan> spans = new List<TextSpan>();
            foreach (TextSpan span in line) {
                Style style = bold ? span.Style.With(TextDecoration.Bold) : span.Style.WithColor(tone.Color);
                spans.Add(new TextSpan(span.Text, style));
            }
            return new StyledLine(spans);
        }

        private object? AggregateOf(TableColumn column, int c, List<int> rows) {
            int count = 0;
            int numbers = 0;
            bool allSpans = true;
            double sum = 0;
            double min = double.MaxValue;
            double max = double.MinValue;
            foreach (int r in rows) {
                TableCell cell = _rows[r][c];
                object? value = cell.HasValue ? cell.Value : Markup.Strip(cell.Markup);
                if (PresenterValues.IsEmpty(value)) {
                    continue;
                }
                count++;
                if (!PresenterValues.TryGetNumber(value, out double number)) {
                    continue;
                }
                allSpans &= value is TimeSpan;
                numbers++;
                sum += number;
                min = Math.Min(min, number);
                max = Math.Max(max, number);
            }
            ColumnAggregate aggregate = EffectiveAggregate(column);
            if (aggregate == ColumnAggregate.Count) {
                return count;
            }
            if (numbers == 0) {
                return null;
            }
            double result;
            switch (aggregate) {
                case ColumnAggregate.Average:
                    result = sum / numbers;
                    break;
                case ColumnAggregate.Min:
                    result = min;
                    break;
                case ColumnAggregate.Max:
                    result = max;
                    break;
                default:
                    result = sum;
                    break;
            }
            if (allSpans) {
                // A total beyond the TimeSpan range (a column holding TimeSpan.MaxValue) shows as unbounded, never throws.
                bool representable = !double.IsNaN(result) && Math.Abs(result) < TimeSpan.MaxValue.TotalSeconds;
                return representable ? TimeSpan.FromSeconds(result) : TimeSpan.MaxValue;
            }
            return Math.Abs(result - Math.Round(result)) < 1e-9 && Math.Abs(result) < long.MaxValue ? (object)Convert.ToInt64(Math.Round(result), CultureInfo.InvariantCulture) : result;
        }
    }
}
