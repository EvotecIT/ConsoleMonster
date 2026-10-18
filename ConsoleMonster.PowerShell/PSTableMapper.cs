using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Maps PowerShell objects to table rows (property selection and value formatting only).</summary>
    internal static class PSTableMapper {
        public static Table Build(IReadOnlyList<PSObject> items, string[]? properties, string? statusProperty) {
            Table table = new Table();
            List<string> names = new List<string>();
            if (properties != null && properties.Length > 0) {
                names.AddRange(properties);
            } else if (items.Count > 0) {
                PSObject first = items[0];
                if (first.BaseObject is IDictionary dictionary) {
                    foreach (object key in dictionary.Keys) {
                        names.Add(PSWidgets.ToText(key));
                    }
                } else {
                    foreach (PSPropertyInfo property in first.Properties) {
                        if (property.IsGettable) {
                            names.Add(property.Name);
                        }
                    }
                }
            }
            foreach (string name in names) {
                table.AddColumn(new TableColumn(Markup.Escape(name)));
            }
            foreach (PSObject item in items) {
                TableCell[] cells = new TableCell[names.Count];
                for (int i = 0; i < names.Count; i++) {
                    string text = PSWidgets.ToText(GetValue(item, names[i]));
                    bool isStatus = statusProperty != null && string.Equals(names[i], statusProperty, StringComparison.OrdinalIgnoreCase);
                    if (isStatus && StatusKinds.TryParse(text, out StatusKind status)) {
                        cells[i] = TableCell.ForStatus(status, text);
                    } else {
                        cells[i] = new TableCell(Markup.Escape(text));
                    }
                }
                table.AddRow(cells);
            }
            return table;
        }

        private static object? GetValue(PSObject item, string name) {
            if (item.BaseObject is IDictionary dictionary) {
                foreach (DictionaryEntry entry in dictionary) {
                    if (string.Equals(PSWidgets.ToText(entry.Key), name, StringComparison.OrdinalIgnoreCase)) {
                        return entry.Value;
                    }
                }
                return null;
            }
            PSPropertyInfo? property = item.Properties[name];
            if (property == null) {
                return null;
            }
            try {
                return property.Value;
            } catch (GetValueException) {
                return null;
            }
        }
    }
}
