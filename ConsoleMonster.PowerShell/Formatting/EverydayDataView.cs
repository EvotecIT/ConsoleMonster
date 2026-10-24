using System;
using System.Collections;
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
    internal static class EverydayDataView {
        internal const int RowLimit = 50;
        private const int ColumnLimit = 24;

        /// <summary>Builds a bounded table or single-record view using the shared adaptive table renderer.</summary>
        internal static IWidget Build(IReadOnlyList<PSObject> items, int omittedRows) {
            List<string> names = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PSObject item in items) {
                foreach (PSPropertyInfo property in item.Properties) {
                    if (property.IsGettable && seen.Add(property.Name)) { names.Add(property.Name); }
                }
            }
            int omittedColumns = Math.Max(0, names.Count - ColumnLimit);
            names = names.Take(ColumnLimit).ToList();
            List<object?[]> rows = new List<object?[]>(items.Count);
            foreach (PSObject item in items) {
                object?[] row = new object?[names.Count];
                for (int c = 0; c < names.Count; c++) {
                    try { row[c] = Summary(item.Properties[names[c]]?.Value); }
                    catch (Exception ex) when (!(ex is PipelineStoppedException) && !(ex is OperationCanceledException)) { row[c] = "<unavailable>"; }
                }
                rows.Add(row);
            }
            Table table = new Table();
            for (int c = 0; c < names.Count; c++) {
                int index = c;
                object?[] values = rows.Select(row => row[index]).Where(value => value != null && !(value is string text && string.IsNullOrWhiteSpace(text))).ToArray();
                bool align = values.Length > 0 && values.All(IsNumberOrDate);
                table.AddColumn(new TableColumn(Markup.Escape(names[c])) {
                    Alignment = align ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    IsKey = c == 0,
                    Priority = names.Count - c,
                    MaxLines = 2
                });
            }
            foreach (object?[] row in rows) { table.AddValues(row); }
            List<string> hints = new List<string>();
            if (omittedRows > 0) { hints.Add("+" + omittedRows.ToString("N0", CultureInfo.CurrentCulture) + " more rows"); }
            if (omittedColumns > 0) { hints.Add("+" + omittedColumns.ToString("N0", CultureInfo.CurrentCulture) + " more fields"); }
            if (hints.Count > 0) { table.Caption = string.Join("; ", hints); }
            return table;
        }

        // Only scalar data is expanded. Nested records never invoke their getters or ToString, and arrays use Count.
        private static object? Summary(object? value) {
            object? raw = value is PSObject wrapped ? wrapped.BaseObject : value;
            if (raw == null || ValueShaper.IsScalar(raw)) { return raw; }
            if (raw is IDictionary || raw is PSCustomObject) { return "{…}"; }
            if (raw is ICollection collection) {
                return collection.Count.ToString("N0", CultureInfo.CurrentCulture) + (collection.Count == 1 ? " item" : " items");
            }
            return "{…}";
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
