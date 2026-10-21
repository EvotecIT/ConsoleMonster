using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Adapts already evaluated PowerShell format records to existing widgets. Expressions and native view selection
    /// run exactly once, in PowerShell's formatter. Unknown shapes retain the native Out-Default route.
    /// </summary>
    internal static class NativeFormatView {
        internal static bool TryBuild(IReadOnlyList<PSObject> records, out IWidget? result) {
            List<IWidget> widgets = new List<IWidget>();
            object? shape = null;
            Table? table = null;
            List<string> wide = new List<string>();
            string? title = null;
            void Flush() {
                if (table != null && table.Rows.Count > 0) { widgets.Add(table); }
                if (wide.Count > 0) { widgets.Add(Wide(wide, shape, title)); wide.Clear(); }
                table = null;
            }
            result = null;
            foreach (PSObject record in records) {
                string type = record.BaseObject.GetType().Name;
                switch (type) {
                    case "FormatStartData":
                        Flush();
                        shape = Get(record, "shapeInfo");
                        string shapeType = Raw(shape)?.GetType().Name ?? string.Empty;
                        if (shapeType != "TableHeaderInfo" && shapeType != "ListViewHeaderInfo" && shapeType != "WideViewHeaderInfo") { return false; }
                        // Page decorations and custom controls are deliberately left to the native renderer.
                        if (Get(record, "pageHeaderEntry") != null || Get(record, "pageFooterEntry") != null) { return false; }
                        title = null;
                        break;
                    case "GroupStartData":
                        Flush();
                        title = TextOf(Get(record, "groupingEntry")).Trim();
                        break;
                    case "GroupEndData":
                    case "FormatEndData":
                        Flush();
                        break;
                    case "FormatEntryData":
                        object? entry = Get(record, "formatEntryInfo");
                        string entryType = Raw(entry)?.GetType().Name ?? string.Empty;
                        if (Bool(record, "outOfBand")) {
                            string text = TextOf(entry);
                            if (!string.IsNullOrWhiteSpace(text)) {
                                Flush();
                                widgets.Add(new TextBlock(text) { IsMarkup = false });
                            }
                        } else if (entryType == "TableRowEntry" && Get(shape, "tableColumnInfoList") != null) {
                            table ??= TableFor(shape, title, Bool(entry, "multiLine"));
                            List<TableCell> cells = new List<TableCell>();
                            foreach (object field in Items(Get(entry, "formatPropertyFieldList"))) {
                                cells.Add(new TableCell(Markup.Escape(String(Get(field, "propertyValue")))));
                            }
                            if (cells.Count != table.Columns.Count) { return false; }
                            table.AddRow(cells.ToArray());
                        } else if (entryType == "ListViewEntry") {
                            RecordView view = new RecordView(Markup.Escape(title ?? string.Empty)) { HideEmpty = false };
                            foreach (object field in Items(Get(entry, "listViewFieldList"))) {
                                string label = String(Get(field, "label"));
                                if (label.Length == 0) { label = String(Get(field, "propertyName")); }
                                view.AddField(label, new TableCell(Markup.Escape(String(Get(Get(field, "formatPropertyField"), "propertyValue")))));
                            }
                            widgets.Add(view);
                        } else if (entryType == "WideViewEntry") {
                            wide.Add(String(Get(Get(entry, "formatPropertyField"), "propertyValue")));
                        } else { return false; }
                        break;
                    default:
                        return false;
                }
            }
            Flush();
            result = PSFormatting.Combine(widgets);
            return true;
        }

        private static Table TableFor(object? shape, string? title, bool wrap) {
            Table table = new Table { Title = Markup.Escape(title ?? string.Empty), ShowHeader = !Bool(shape, "hideHeader"), RecordViewRows = 0, CardsBelow = 0, WrapLines = wrap ? 0 : 1 };
            foreach (object column in Items(Get(shape, "tableColumnInfoList"))) {
                string label = String(Get(column, "label"));
                if (label.Length == 0) { label = String(Get(column, "propertyName")); }
                int alignment = Number(Get(column, "alignment"));
                int width = Number(Get(column, "width"));
                table.AddColumn(new TableColumn(Markup.Escape(label)) {
                    Alignment = alignment == (int)Alignment.Center ? HorizontalAlignment.Center : alignment == (int)Alignment.Right ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    Width = width > 0 ? width : (int?)null,
                    Overflow = wrap ? ColumnOverflow.Wrap : ColumnOverflow.Truncate
                });
            }
            return table;
        }

        private static IWidget Wide(List<string> values, object? shape, string? title) {
            int columns = Number(Get(shape, "columns"));
            if (columns <= 0) {
                int longest = 1;
                foreach (string value in values) { longest = Math.Max(longest, DisplayWidth.Measure(value)); }
                columns = Math.Max(1, Math.Min(values.Count, (Terminal.GetCurrentProfile().Width - 4) / (longest + 3)));
            }
            columns = Math.Min(columns, Math.Max(1, values.Count));
            Table table = new Table { Title = Markup.Escape(title ?? string.Empty), ShowHeader = false, Border = TableBorder.None, RecordViewRows = 0, CardsBelow = 0, WrapLines = 1 };
            for (int i = 0; i < columns; i++) { table.AddColumn(new TableColumn(string.Empty) { Overflow = ColumnOverflow.Truncate }); }
            for (int i = 0; i < values.Count; i += columns) {
                TableCell[] cells = new TableCell[columns];
                for (int c = 0; c < columns; c++) { cells[c] = new TableCell(i + c < values.Count ? Markup.Escape(values[i + c]) : string.Empty); }
                table.AddRow(cells);
            }
            return table;
        }

        private static object? Raw(object? value) => value is PSObject ps ? ps.BaseObject : value;
        private static object? Get(object? value, string name) => value == null ? null : PSObject.AsPSObject(value).Properties[name]?.Value;
        private static string String(object? value) => Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
        private static int Number(object? value) => value == null ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        private static bool Bool(object? value, string name) => Get(value, name) is bool yes && yes;
        private static IEnumerable Items(object? value) => Raw(value) as IEnumerable ?? Array.Empty<object>();
        private static string TextOf(object? value) {
            if (value == null) { return string.Empty; }
            object? text = Get(value, "text") ?? Get(value, "propertyValue");
            if (text != null) { return String(text); }
            StringBuilder result = new StringBuilder();
            foreach (object child in Items(Get(value, "formatValueList"))) { result.Append(TextOf(child)); }
            return result.ToString();
        }
    }
}
