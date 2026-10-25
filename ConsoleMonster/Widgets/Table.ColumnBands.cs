using System.Collections.Generic;

namespace ConsoleMonster.Widgets {
    public partial class Table {
        private List<Table> BuildColumnBands(int width, int[] minimum, int gap) {
            List<Table> bands = new List<Table>();
            int key = KeyColumn;
            List<int> columns = new List<int> { key };
            for (int c = 0; c < _columns.Count; c++) {
                if (c == key) continue;
                columns.Add(c);
                if (columns.Count > 2 && SumOf(minimum, columns) + Overhead(columns.Count, gap) > width) {
                    columns.RemoveAt(columns.Count - 1);
                    bands.Add(ColumnBand(columns, bands.Count == 0, width, minimum, gap));
                    columns = new List<int> { key, c };
                }
            }
            if (columns.Count > 1) bands.Add(ColumnBand(columns, bands.Count == 0, width, minimum, gap));
            if (bands.Count > 0) bands[bands.Count - 1].Caption = Caption;
            return bands;
        }

        private Table ColumnBand(List<int> columns, bool first, int width, int[] minimum, int gap) {
            Table band = new Table {
                Title = first ? Title : null, TitleIndent = TitleIndent,
                RecordViewRows = 0, CardsBelow = 0, WrapHeaders = WrapHeaders, ShowKeyField = ShowKeyField,
                Border = Border, ShowHeader = ShowHeader, HeaderSeparator = HeaderSeparator,
                GroupHeadersAsRows = GroupHeadersAsRows,
                WrapLines = WrapLines, Expand = Expand, Presentation = Presentation,
                ShowTotals = ShowTotals, ShowSubtotals = ShowSubtotals, AutoTotals = AutoTotals,
                MaxRows = MaxRows, TotalRowCount = TotalRowCount, TotalLabel = TotalLabel
            };
            bool oversized = SumOf(minimum, columns) + Overhead(columns.Count, gap) > width;
            int room = width - Overhead(columns.Count, gap);
            for (int i = 0; i < columns.Count; i++) {
                int c = columns[i];
                // An oversized key/value pair still needs both fields. Let the key wrap more tightly, rather
                // than letting the ordinary grid hide its only value column. At physically impossible widths
                // cards remain the lossless fallback.
                if (oversized && room >= 2) {
                    int valueWidth = System.Math.Min(minimum[columns[1]], room - 1);
                    band.AddColumn(WrappingColumn(_columns[c], i == 0 ? room - valueWidth : valueWidth));
                } else {
                    band.AddColumn(_columns[c]);
                }
            }
            band.AlwaysCards = oversized && room < 2;
            int group = -1;
            for (int r = 0; r < _rows.Count; r++) {
                if (_rowGroups[r] != group) {
                    group = _rowGroups[r];
                    if (group >= 0) band.AddGroup(_groups[group]);
                }
                TableCell[] cells = new TableCell[columns.Count];
                for (int c = 0; c < columns.Count; c++) cells[c] = _rows[r][columns[c]];
                band.AddRow(cells);
            }
            if (_footer != null) {
                TableCell[] footer = new TableCell[columns.Count];
                for (int c = 0; c < columns.Count; c++) footer[c] = _footer[columns[c]];
                band.SetFooter(footer);
            }
            return band;
        }

        private static TableColumn WrappingColumn(TableColumn source, int width) {
            TableColumn column = new TableColumn(source.Header) {
                Key = source.Key, Alignment = source.Alignment, Wrap = true, MaxLines = TableColumn.Unlimited,
                Priority = source.Priority, IsKey = source.IsKey, Flexible = source.Flexible,
                MaxWidth = width, Presenter = source.Presenter, Aggregate = source.Aggregate
            };
            foreach (var rule in source.RoleRules) column.RoleRules.Add(rule);
            return column;
        }
    }
}
