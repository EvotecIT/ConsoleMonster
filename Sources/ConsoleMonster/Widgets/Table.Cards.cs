using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Table {
        // A single row reads better as a sheet of labelled values than as a one-line table.
        private RecordView BuildRecordView(PresenterContext presenter) {
            PresenterContext[] contexts = ColumnContexts(presenter);
            RecordView view = new RecordView(Title) { Presentation = Presentation };
            int key = KeyColumn;
            bool sections = _rows.Count > 1 || string.IsNullOrEmpty(Title);
            foreach (TableCell[] row in _rows) {
                if (sections) {
                    view.AddSection(Markup.Escape(KeyText(row, key, contexts)), RowTone(row, contexts));
                }
                AddFields(view, row, key, contexts);
            }
            if (!string.IsNullOrEmpty(Caption)) {
                view.Summary = Markup.Strip(Caption!);
            }
            return view;
        }

        // Narrow widths: every row becomes a card titled by the key column, with all other columns as labelled fields,
        // so nothing is hidden; groups become sections and totals a final card.
        private RecordView BuildCards(PresenterContext presenter) {
            PresenterContext[] contexts = ColumnContexts(presenter);
            RecordView view = new RecordView(Title) { Presentation = Presentation };
            int key = KeyColumn;
            int shown = DisplayedRowCount;
            int all = TotalRowCount ?? _rows.Count;
            CultureInfo culture = presenter.Culture;
            view.Summary = shown < all
                ? "Showing " + shown.ToString("N0", culture) + " of " + all.ToString("N0", culture)
                : all.ToString("N0", culture) + (all == 1 ? " record" : " records");
            int group = int.MinValue;
            int level = 0;
            for (int r = 0; r < shown; r++) {
                if (_rowGroups[r] != group) {
                    group = _rowGroups[r];
                    if (group >= 0) {
                        view.AddSection(_groups[group]);
                        level = 1;
                    }
                }
                view.AddHeading(Markup.Escape(KeyText(_rows[r], key, contexts)), level, RowTone(_rows[r], contexts));
                AddFields(view, _rows[r], key, contexts);
            }
            TableLayoutRow? total = BuildTotalRow(presenter);
            if (total?.Cells != null) {
                view.AddHeading(Markup.Escape(TotalLabel), 0);
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
