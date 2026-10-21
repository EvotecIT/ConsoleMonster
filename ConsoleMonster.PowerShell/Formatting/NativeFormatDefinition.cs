using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.PowerShell.Binding;

namespace ConsoleMonster.PowerShell {
    // Native view metadata supplies the selection and layout. Capture changes only the expression's return path.
    internal sealed class NativeFormatDefinition {
        internal readonly List<IDictionary> Columns = new List<IDictionary>();
        internal object? GroupBy;
        internal bool Wrap;
        private TableControl? _table;
        private string[]? _labels;
        private bool _firstSelected;

        internal IReadOnlyList<IDictionary> ColumnsFor(PSObject item) {
            if (_table == null) { return Columns; }
            if (_firstSelected) { _firstSelected = false; return Columns; }
            TableControlRow? selected = Select(_table, item);
            return selected == null ? Columns : TableColumns(_table, selected, _labels!);
        }

        internal static NativeFormatDefinition? Resolve(PSCmdlet cmdlet, string kind, PSObject first, IDictionary parameters) {
            if (first.BaseObject is IEnumerable && !(first.BaseObject is string) && !(first.BaseObject is IDictionary)) { return null; }
            NativeFormatDefinition result = new NativeFormatDefinition();
            if (parameters.Contains("Property")) {
                foreach (object property in Sequence(parameters["Property"])) {
                    if (property is IDictionary dictionary) { result.Columns.Add(dictionary); }
                    else if (property is string name) {
                        foreach (string resolved in Names(first, name)) { result.Columns.Add(Column(resolved, resolved)); }
                    } else { return null; }
                }
                return result.Columns.Count > 0 ? result : null;
            }
            string? requested = parameters["View"] as string;
            foreach (string type in first.TypeNames) {
                var definitions = cmdlet.InvokeCommand.InvokeScript(
                    "param($type) Microsoft.PowerShell.Utility\\Get-FormatData -TypeName $type -PowerShellVersion $PSVersionTable.PSVersion -ErrorAction Ignore", true,
                    System.Management.Automation.Runspaces.PipelineResultTypes.None, null, type);
                foreach (PSObject item in definitions) {
                    if (!(item.BaseObject is ExtendedTypeDefinition definition)) { continue; }
                    foreach (FormatViewDefinition view in definition.FormatViewDefinition) {
                        if (requested != null && !string.Equals(requested, view.Name, StringComparison.OrdinalIgnoreCase)) { continue; }
                        if (kind == "Table" && view.Control is TableControl table) {
                            TableControlRow? row = Select(table, first);
                            if (row == null) { return null; }
                            result._table = table;
                            result._firstSelected = true;
                            result._labels = Enumerable.Range(0, row.Columns.Count).Select(c => c < table.Headers.Count && !string.IsNullOrEmpty(table.Headers[c].Label)
                                ? table.Headers[c].Label : Label(row.Columns[c].DisplayEntry)).ToArray();
                            result.Wrap = row.Wrap;
                            result.Columns.AddRange(TableColumns(table, row, result._labels));
                            foreach (TableControlRow other in table.Rows) {
                                if (other.Columns.Count != row.Columns.Count) { return null; }
                                var columns = TableColumns(table, other, result._labels);
                                for (int c = 0; c < columns.Count; c++) {
                                    if (!Equals(columns[c]["FormatString"], result.Columns[c]["FormatString"])) { return null; }
                                }
                            }
                        } else if (kind == "List" && view.Control is ListControl list) {
                            if (list.Entries.Count != 1 || !DefaultRow(list.Entries[0].EntrySelectedBy)) { return null; }
                            foreach (ListControlEntryItem value in list.Entries[0].Items) {
                                if (value.ItemSelectionCondition != null) { return null; }
                                IDictionary column = Column(string.IsNullOrEmpty(value.Label) ? Label(value.DisplayEntry) : value.Label, Expression(value.DisplayEntry));
                                if (!string.IsNullOrEmpty(value.FormatString)) { column["FormatString"] = value.FormatString; }
                                result.Columns.Add(column);
                            }
                        } else { continue; }
                        if (view.Control.GroupBy?.Expression != null) { result.GroupBy = Expression(view.Control.GroupBy.Expression); }
                        return result.Columns.Count > 0 ? result : null;
                    }
                }
            }
            if (requested != null) { return null; }
            IEnumerable<string> names = kind == "Table" && PropertyBinder.GetDefaultDisplayForSource(first) is PSPropertySet display
                ? display.ReferencedPropertyNames : first.Properties.Where(p => p.IsGettable && p.Name != "PSStandardMembers").Select(p => p.Name);
            foreach (string name in names) { result.Columns.Add(Column(name, name)); }
            return result.Columns.Count > 0 ? result : null;
        }

        private static IEnumerable<string> Names(PSObject first, string pattern) {
            if (!WildcardPattern.ContainsWildcardCharacters(pattern)) { return new[] { pattern }; }
            WildcardPattern match = new WildcardPattern(pattern, WildcardOptions.IgnoreCase);
            return first.Properties.Where(p => p.IsGettable && match.IsMatch(p.Name)).Select(p => p.Name);
        }
        private static IDictionary Column(string label, object expression) => new Hashtable(StringComparer.OrdinalIgnoreCase) { ["Label"] = label, ["Expression"] = expression };
        private static object Expression(DisplayEntry entry) => entry.ValueType == DisplayEntryValueType.ScriptBlock ? (object)ScriptBlock.Create(entry.Value) : entry.Value;
        private static string Label(DisplayEntry entry) => entry.Value;
        private static bool DefaultRow(EntrySelectedBy? selection) => selection == null || ((selection.TypeNames?.Count ?? 0) == 0 && (selection.SelectionCondition?.Count ?? 0) == 0);
        private static TableControlRow? Select(TableControl table, PSObject item) {
            TableControlRow? fallback = null;
            foreach (TableControlRow row in table.Rows) {
                if (DefaultRow(row.SelectedBy)) { fallback ??= row; continue; }
                foreach (string type in row.SelectedBy.TypeNames ?? new List<string>()) { if (item.TypeNames.Any(name => string.Equals(name, type, StringComparison.OrdinalIgnoreCase))) { return row; } }
                foreach (DisplayEntry condition in row.SelectedBy.SelectionCondition ?? new List<DisplayEntry>()) {
                    if (LanguagePrimitives.IsTrue(PropertyBinder.Evaluate(item, Expression(condition)))) { return row; }
                }
            }
            return fallback;
        }
        private static List<IDictionary> TableColumns(TableControl table, TableControlRow row, string[] labels) {
            List<IDictionary> columns = new List<IDictionary>();
            for (int c = 0; c < row.Columns.Count; c++) {
                TableControlColumn value = row.Columns[c];
                TableControlColumnHeader? header = c < table.Headers.Count ? table.Headers[c] : null;
                IDictionary column = Column(labels[c], Expression(value.DisplayEntry));
                Alignment alignment = value.Alignment != Alignment.Undefined ? value.Alignment : header?.Alignment ?? Alignment.Undefined;
                if (alignment != Alignment.Undefined) { column["Alignment"] = alignment.ToString(); }
                if ((header?.Width ?? 0) > 0) { column["Width"] = header!.Width; }
                string? format = PSObject.AsPSObject(value).Properties["FormatString"]?.Value as string;
                if (!string.IsNullOrEmpty(format)) { column["FormatString"] = format; }
                columns.Add(column);
            }
            return columns;
        }
        internal static IEnumerable Sequence(object? value) => value is string || value is IDictionary ? new[] { value } : value as IEnumerable ?? Array.Empty<object>();
    }
}
