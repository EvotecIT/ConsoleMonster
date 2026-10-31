using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;
using ConsoleMonster.Values;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Host-only presentation of JSON/REST records and CSV rows. PowerShell gives them the same PSCustomObject type,
    /// so alignment is inferred from the displayed values without converting or annotating the original objects.
    /// </summary>
    internal static partial class EverydayDataView {
        internal const int RowLimit = 50;
        private const int ColumnLimit = 24;

        /// <summary>Builds a bounded table or single-record view using the shared adaptive table renderer.</summary>
        internal static IWidget Build(IReadOnlyList<PSObject> items, int omittedRows) {
            ExpansionBudget budget = new ExpansionBudget();
            List<DataRecord> records = items.Select(item => ReadRecord(item, false)).ToList();
            if (records.Any(record => record.Fields.Any(field => IsRecordArray(field.Value)))
                || (records.Count == 1 && (records[0].Count == 0 || records[0].Fields.Any(field => IsNested(field.Value))))) {
                List<IWidget> parts = records.Select(record => (IWidget)BuildRecord(record, 0, budget, true)).ToList();
                if (omittedRows > 0) { parts.Add(new TextBlock("[muted]" + More(omittedRows, "rows") + "[/]")); }
                return PSFormatting.Combine(parts);
            }
            return BuildTable(records, omittedRows, 0, budget, false);
        }

        private static Table BuildTable(IReadOnlyList<DataRecord> items, int omittedRows, int depth, ExpansionBudget budget, bool nested) {
            List<string> names = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRecord item in items) {
                foreach (string name in item.Names()) {
                    if (seen.Add(name)) { names.Add(name); }
                }
            }
            int omittedColumns = Math.Max(0, names.Count - ColumnLimit);
            names = names.Take(ColumnLimit).ToList();
            if (names.Count == 0) { names.Add("Value"); }
            int title = ValueShaper.TitleIndex(names);
            int key = title >= 0 ? title : 0;
            List<object?[]> rows = new List<object?[]>(items.Count);
            foreach (DataRecord item in items) {
                object?[] row = new object?[names.Count];
                for (int c = 0; c < names.Count; c++) {
                    row[c] = item.Count == 0 && c == key ? "(none)" : FormatValue(item.GetValue(names[c]), depth + 1, budget);
                }
                rows.Add(row);
            }
            Table table = new Table { ShowKeyField = title < 0 };
            if (nested) { table.RecordViewRows = 0; table.ShowKeyField = true; }
            if (items.Count == 1 && title < 0) { table.Title = "Record"; }
            for (int c = 0; c < names.Count; c++) {
                int index = c;
                object?[] values = rows.Select(row => row[index]).Where(value => value != null && !(value is string text && string.IsNullOrWhiteSpace(text))).ToArray();
                bool align = values.Length > 0 && values.All(IsNumberOrDate);
                table.AddColumn(new TableColumn(Markup.Escape(BoundedText(names[c], 48))) {
                    // Status words and identifiers read as they do in Show-ConsoleTable (a marker before Pass, no separators in an Id).
                    Presenter = Presenters.CellPresenters.ForColumn(names[c], values.FirstOrDefault(value => !Equals(value, "—"))),
                    Alignment = align ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    IsKey = c == key,
                    Priority = names.Count - c,
                    MaxLines = 2
                });
            }
            foreach (object?[] row in rows) { table.AddValues(row); }
            List<string> hints = new List<string>();
            if (omittedRows > 0) { hints.Add(More(omittedRows, "rows")); }
            if (omittedColumns > 0) { hints.Add(More(omittedColumns, "fields")); }
            if (hints.Count > 0) { table.Caption = string.Join("; ", hints); }
            return table;
        }

        private static bool IsNumberOrDate(object? value) {
            if (!(value is string text)) {
                return value is DateTime || value is DateTimeOffset || Presenters.PresenterValues.TryGetNumber(value, out _);
            }
            // Parsing is for alignment only: leading zeroes, decimal spelling and date text stay exactly as imported.
            return decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.CurrentCulture, out _)
                || decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                || (text.IndexOfAny(new[] { '-', '/', ':', '.' }) >= 0
                    && DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out _));
        }
    }
}
