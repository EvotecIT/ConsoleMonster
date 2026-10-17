using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Table {
        private static readonly ICellPresenter CountPresenter = new NumberPresenter();

        private int DisplayedRowCount => MaxRows > 0 ? Math.Min(MaxRows, _rows.Count) : _rows.Count;

        private double?[] ColumnMaxima() {
            double?[] maxima = new double?[_columns.Count];
            foreach (TableCell[] row in _rows) {
                for (int c = 0; c < _columns.Count && c < row.Length; c++) {
                    if (row[c].HasValue && PresenterValues.TryGetNumber(row[c].Value, out double number)) {
                        maxima[c] = maxima[c].HasValue ? Math.Max(maxima[c]!.Value, number) : number;
                    }
                }
            }
            return maxima;
        }

        private PresenterContext[] ColumnContexts(PresenterContext presenter) {
            double?[] maxima = ColumnMaxima();
            PresenterContext[] contexts = new PresenterContext[_columns.Count];
            for (int c = 0; c < _columns.Count; c++) {
                contexts[c] = presenter.WithColumnMaximum(maxima[c]);
            }
            return contexts;
        }

        private CellContent[] Present(TableCell[] cells, PresenterContext[] contexts) {
            CellContent[] contents = new CellContent[_columns.Count];
            for (int c = 0; c < _columns.Count; c++) {
                TableColumn column = _columns[c];
                contents[c] = CellRendering.Content(cells[c], column.Presenter, (IReadOnlyList<ToneRule>)column.ToneRules, contexts[c]);
            }
            return contents;
        }

        private List<TableLayoutRow> BuildRows(PresenterContext presenter, StreamStats? stats = null) {
            PresenterContext[] contexts = ColumnContexts(presenter);
            List<TableLayoutRow> rows = new List<TableLayoutRow>();
            int shown = DisplayedRowCount;
            int group = int.MinValue;
            List<int> groupRows = new List<int>();
            for (int r = 0; r < shown; r++) {
                if (_rowGroups[r] != group) {
                    AddSubtotal(rows, groupRows, contexts);
                    group = _rowGroups[r];
                    groupRows.Clear();
                    if (group >= 0) {
                        rows.Add(GroupRow(group, presenter));
                    }
                }
                groupRows.Add(r);
                CellContent[] cells = Present(_rows[r], contexts);
                if (stats != null) {
                    stats.Add(cells);
                    rows.Add(new TableLayoutRow(TableRowKind.Data) { Source = r });
                } else {
                    rows.Add(new TableLayoutRow(TableRowKind.Data) { Cells = cells });
                }
            }
            AddSubtotal(rows, groupRows, contexts);
            return rows;
        }

        private TableLayoutRow GroupRow(int group, PresenterContext presenter) {
            int count = 0;
            for (int r = 0; r < _rowGroups.Count; r++) {
                if (_rowGroups[r] == group) {
                    count++;
                }
            }
            List<TextSpan> spans = new List<TextSpan>();
            foreach (TextSpan span in presenter.Render.Markup(_groups[group]).Lines[0].WithBaseStyle(presenter.Render.Style(ThemeToken.Header).With(TextDecoration.Bold))) {
                spans.Add(span);
            }
            spans.Add(new TextSpan("  " + count.ToString("N0", presenter.Culture), presenter.Render.Style(ThemeToken.Muted)));
            return new TableLayoutRow(TableRowKind.Group) { Spanning = new StyledLine(spans) };
        }

        private void AddSubtotal(List<TableLayoutRow> rows, List<int> groupRows, PresenterContext[] contexts) {
            if (!ShowSubtotals || groupRows.Count == 0 || _groups.Count == 0 || !HasAggregates) {
                return;
            }
            rows.Add(new TableLayoutRow(TableRowKind.Subtotal) { Cells = SummaryCells(groupRows, "Subtotal", contexts, contexts[0].Render.Style(ThemeToken.Muted), false) });
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
            return new TableLayoutRow(TableRowKind.Total) { Cells = SummaryCells(all, TotalLabel, contexts, presenter.Render.Style(ThemeToken.Text), true) };
        }

        private bool HasAggregates {
            get {
                foreach (TableColumn column in _columns) {
                    if (column.Aggregate != ColumnAggregate.None) {
                        return true;
                    }
                }
                return false;
            }
        }

        private CellContent[] SummaryCells(List<int> rows, string label, PresenterContext[] contexts, Style tone, bool bold) {
            CellContent[] cells = new CellContent[_columns.Count];
            int labelColumn = _columns[KeyColumn].Aggregate == ColumnAggregate.None ? KeyColumn : FirstPlainColumn();
            for (int c = 0; c < _columns.Count; c++) {
                TableColumn column = _columns[c];
                CellContent content;
                if (c == labelColumn) {
                    content = new CellContent(new List<StyledLine> { StyledLine.From(label, tone) }, null);
                } else if (column.Aggregate == ColumnAggregate.None) {
                    content = new CellContent(new List<StyledLine> { StyledLine.Empty }, null);
                } else {
                    object? value = AggregateOf(column, c, rows);
                    ICellPresenter? presenter = column.Aggregate == ColumnAggregate.Count ? CountPresenter : column.Presenter;
                    PresentedValue presented = CellPresenters.Present(value, presenter, (IReadOnlyList<ToneRule>)column.ToneRules, contexts[c]);
                    // Totals show the number only; a data bar of a sum would not share the rows' scale.
                    PresentedValue plain = new PresentedValue(presented.Line, presented.Alignment) { Number = presented.Number, Tone = presented.Tone };
                    content = new CellContent(new List<StyledLine> { Restyle(plain.Line, tone, bold) }, plain);
                }
                cells[c] = content;
            }
            return cells;
        }

        private int FirstPlainColumn() {
            for (int c = 0; c < _columns.Count; c++) {
                if (_columns[c].Aggregate == ColumnAggregate.None) {
                    return c;
                }
            }
            return -1;
        }

        private static StyledLine Restyle(StyledLine line, Style tone, bool bold) {
            List<TextSpan> spans = new List<TextSpan>();
            foreach (TextSpan span in line) {
                Style style = bold ? span.Style.With(TextDecoration.Bold) : span.Style.WithForeground(tone.Foreground);
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
            if (column.Aggregate == ColumnAggregate.Count) {
                return count;
            }
            if (numbers == 0) {
                return null;
            }
            double result;
            switch (column.Aggregate) {
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
