using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public partial class Table {
        // A single row reads better as a sheet of labelled values than as a one-line table.
        private RecordView BuildRecordView(PresenterContext presenter) {
            if (_groups.Count > 0) {
                // A grouped row keeps its group heading (the key column may be hidden because the heading names it).
                RecordView grouped = BuildCards(presenter, false);
                if (!string.IsNullOrEmpty(Caption)) {
                    grouped.Summary = Markup.Strip(Caption!);
                }
                return grouped;
            }
            PresenterContext[] contexts = ColumnContexts(presenter);
            RecordView view = new RecordView(Title) { Presentation = Presentation };
            int key = KeyColumn;
            bool sections = _rows.Count > 1 || string.IsNullOrEmpty(Title);
            for (int index = 0; index < _rows.Count; index++) {
                TableCell[] row = _rows[index];
                if (sections) {
                    view.AddSection(Markup.Escape(RowTitle(index, key, contexts)), RowTone(row, contexts));
                }
                AddFields(view, row, key, contexts);
            }
            if (!string.IsNullOrEmpty(Caption)) {
                view.Summary = Markup.Strip(Caption!);
            }
            return view;
        }

        // Narrow widths: every row becomes a card titled by the key column, with all other columns as labelled fields,
        // so nothing is hidden; groups become sections and totals a final card. Without a summary it is the record view of
        // a grouped table's few rows.
        private RecordView BuildCards(PresenterContext presenter, bool summary = true) {
            PresenterContext[] contexts = ColumnContexts(presenter);
            RecordView view = new RecordView(Title) { Presentation = Presentation };
            int key = KeyColumn;
            int shown = DisplayedRowCount;
            int all = TotalRowCount ?? _rows.Count;
            CultureInfo culture = presenter.Culture;
            if (summary) {
                view.Summary = shown < all
                    ? Localization.WidgetLabels.Format(presenter.Render.Labels.Showing, shown.ToString("N0", culture), all.ToString("N0", culture))
                    : Localization.WidgetLabels.Format(all == 1 ? presenter.Render.Labels.OneRecord : presenter.Render.Labels.ManyRecords, all.ToString("N0", culture));
            }
            int group = int.MinValue;
            int level = 0;
            Dictionary<int, int> visible = VisibleGroupRows(shown);
            for (int r = 0; r < shown; r++) {
                if (_rowGroups[r] != group) {
                    group = _rowGroups[r];
                    if (group >= 0) {
                        // The same count as the table's group headings: "5", or "2 of 3" when -Top cut the group.
                        int count = GroupRowCount(group);
                        int seen = visible.TryGetValue(group, out int v) ? v : 0;
                        string note = seen < count
                            ? Localization.WidgetLabels.Format(presenter.Render.Labels.GroupOf, seen.ToString("N0", culture), count.ToString("N0", culture))
                            : count.ToString("N0", culture);
                        view.AddSection(_groups[group], null, note);
                        level = 1;
                    }
                }
                view.AddHeading(Markup.Escape(RowTitle(r, key, contexts)), level, RowTone(_rows[r], contexts));
                AddFields(view, _rows[r], key, contexts);
            }
            TableLayoutRow? total = BuildTotalRow(presenter);
            if (total?.Cells != null) {
                view.AddHeading(Markup.Escape(TotalLabel ?? presenter.Render.Labels.Total), 0);
                for (int c = 0; c < _columns.Count; c++) {
                    if (c == key || total.Cells[c].IsEmpty) {
                        continue;
                    }
                    view.AddField(Markup.Strip(_columns[c].Header), new TableCell(Markup.Escape(total.Cells[c].Lines[0].PlainText)));
                }
            }
            return view;
        }

        private void AddFields(RecordView view, TableCell[] row, int key, PresenterContext[] contexts) {
            for (int c = 0; c < _columns.Count; c++) {
                if (c == key) {
                    continue;
                }
                TableColumn column = _columns[c];
                view.AddField(Markup.Strip(column.Header), row[c], column.Presenter, (IReadOnlyList<ToneRule>)column.ToneRules);
            }
        }

        private string RowTitle(int index, int key, PresenterContext[] contexts) =>
            RowTitles != null && index < RowTitles.Count && !string.IsNullOrEmpty(RowTitles[index]) ? RowTitles[index]! : KeyText(_rows[index], key, contexts);

        private string KeyText(TableCell[] row, int key, PresenterContext[] contexts) {
            CellContent content = CellRendering.Content(row[key], _columns[key].Presenter, null, contexts[key]);
            return content.Lines.Count > 0 ? content.Lines[0].PlainText : string.Empty;
        }

        // The row's tone (from a status cell or a status presenter) colours its card title.
        private ThemeToken? RowTone(TableCell[] row, PresenterContext[] contexts) {
            for (int c = 0; c < _columns.Count; c++) {
                if (row[c].Status.HasValue) {
                    return row[c].Status!.Value.ToToken();
                }
                if (row[c].HasValue && (_columns[c].Presenter is StatusPresenter || row[c].Value is StatusKind)) {
                    PresentedValue presented = CellPresenters.Present(row[c].Value, _columns[c].Presenter, null, contexts[c]);
                    if (presented.Tone.HasValue) {
                        return presented.Tone;
                    }
                }
            }
            return null;
        }
    }
}
