using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>
    /// PowerShell's own table views (format.ps1xml, read through Get-FormatData) as column definitions: the columns people
    /// know from Format-Table (ifIndex, IPAddress, PrefixLength for Get-NetIPAddress; DriveLetter, Size for Get-Volume;
    /// CommandType, Name, Version, Source for Get-Command). Definitions are cached per type name for the session; a type
    /// without a view is looked up again after a minute, in case a module adds one later.
    /// </summary>
    internal static class FormatViews {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, KeyValuePair<ColumnSpec[]?, DateTime>> Cache = new Dictionary<string, KeyValuePair<ColumnSpec[]?, DateTime>>(StringComparer.OrdinalIgnoreCase);

        // A type without a table view is asked again after this long: a module imported later (or Update-FormatData) may
        // add one. Found views stay for the session.
        private static readonly TimeSpan MissingViewLifetime = TimeSpan.FromMinutes(1);
        private static readonly Regex Escapes = new Regex("\u001b\\[[0-9;?]*[ -/]*[@-~]|\u001b\\][^\u0007\u001b]*(\u0007|\u001b\\\\)", RegexOptions.Compiled);

        private const string Script = "param($TypeName) $parameters = @{ TypeName = $TypeName; ErrorAction = 'Ignore' }; if ((Get-Command -Name Get-FormatData).Parameters.ContainsKey('PowerShellVersion')) { $parameters.PowerShellVersion = $PSVersionTable.PSVersion }; Get-FormatData @parameters";

        /// <summary>The table view columns for an object (as calculated-property hashtables), or null when its types have none.</summary>
        /// <param name="item">The object.</param>
        /// <param name="cmdlet">The calling cmdlet (Get-FormatData runs in its session), or null to skip the lookup.</param>
        /// <returns>The column definitions, or null.</returns>
        internal static object[]? TableColumns(PSObject item, PSCmdlet? cmdlet) {
            if (item == null || cmdlet == null) {
                return null;
            }
            foreach (string typeName in item.TypeNames) {
                ColumnSpec[]? specs = null;
                bool cached;
                lock (Gate) {
                    cached = Cache.TryGetValue(typeName, out KeyValuePair<ColumnSpec[]?, DateTime> entry)
                        && (entry.Key != null || DateTime.UtcNow - entry.Value < MissingViewLifetime);
                    if (cached) {
                        specs = entry.Key;
                    }
                }
                if (!cached) {
                    specs = Load(typeName, cmdlet);
                    lock (Gate) {
                        Cache[typeName] = new KeyValuePair<ColumnSpec[]?, DateTime>(specs, DateTime.UtcNow);
                    }
                }
                if (specs != null) {
                    return Build(specs);
                }
            }
            return null;
        }

        /// <summary>Removes VT escape sequences (format views in PowerShell 7.2+ colour some values through $PSStyle).</summary>
        /// <param name="value">The value.</param>
        /// <returns>The value without escape sequences.</returns>
        internal static object? StripEscapes(object? value) => value is string text && text.IndexOf('\u001b') >= 0 ? Escapes.Replace(text, string.Empty) : value;

        private static object[] Build(ColumnSpec[] specs) {
            object[] columns = new object[specs.Length];
            for (int i = 0; i < specs.Length; i++) {
                ColumnSpec spec = specs[i];
                Hashtable column = new Hashtable(StringComparer.OrdinalIgnoreCase) {
                    ["Name"] = spec.Label,
                    ["Expression"] = spec.IsScript ? (object)ScriptBlock.Create(spec.Value) : spec.Value
                };
                if (spec.Alignment != null) {
                    column["Alignment"] = spec.Alignment;
                }
                if (!string.IsNullOrEmpty(spec.FormatString)) {
                    column["FormatString"] = spec.FormatString;
                }
                columns[i] = column;
            }
            return columns;
        }

        private static ColumnSpec[]? Load(string typeName, PSCmdlet cmdlet) {
            Collection<PSObject> data;
            try {
                data = cmdlet.InvokeCommand.InvokeScript(Script, true, System.Management.Automation.Runspaces.PipelineResultTypes.None, null, typeName);
            } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                return null;
            }
            foreach (PSObject entry in data) {
                if (!(entry?.BaseObject is ExtendedTypeDefinition definition)) {
                    continue;
                }
                foreach (FormatViewDefinition view in definition.FormatViewDefinition) {
                    if (view.Control is TableControl table && table.Rows.Count > 0 && table.Rows[0].Columns.Count > 0) {
                        return Specs(table);
                    }
                }
            }
            return null;
        }

        private static ColumnSpec[] Specs(TableControl table) {
            TableControlRow row = table.Rows.FirstOrDefault(candidate => DefaultRow(candidate)) ?? table.Rows[0];
            List<ColumnSpec> specs = new List<ColumnSpec>();
            for (int i = 0; i < row.Columns.Count; i++) {
                TableControlColumn column = row.Columns[i];
                TableControlColumnHeader? header = i < table.Headers.Count ? table.Headers[i] : null;
                DisplayEntry entry = column.DisplayEntry;
                bool script = entry.ValueType == DisplayEntryValueType.ScriptBlock;
                string label = !string.IsNullOrEmpty(header?.Label) ? header!.Label : (script ? "Value" + (i + 1) : entry.Value);
                string? alignment = AlignmentName(column.Alignment) ?? AlignmentName(header?.Alignment ?? Alignment.Undefined);
                string? format = PSObject.AsPSObject(column).Properties["FormatString"]?.Value as string;
                string expression = table.Rows.Count == 1 && DefaultRow(row) ? entry.Value : SelectedExpression(table, i);
                specs.Add(new ColumnSpec(label, expression, script || table.Rows.Count != 1 || !DefaultRow(row), alignment, format));
            }
            return specs.ToArray();
        }

        private static bool DefaultRow(TableControlRow row) => row.SelectedBy == null || ((row.SelectedBy.TypeNames?.Count ?? 0) == 0 && (row.SelectedBy.SelectionCondition?.Count ?? 0) == 0);

        // Keep row selection inside each column expression: rows of the same type can choose different values.
        private static string SelectedExpression(TableControl table, int column) {
            StringBuilder expression = new StringBuilder();
            TableControlRow? fallback = null;
            foreach (TableControlRow row in table.Rows) {
                if (DefaultRow(row)) {
                    fallback = row;
                    continue;
                }
                List<string> conditions = new List<string>();
                foreach (string type in row.SelectedBy.TypeNames ?? new List<string>()) {
                    conditions.Add("($_.PSTypeNames -contains '" + type.Replace("'", "''") + "')");
                }
                foreach (DisplayEntry condition in row.SelectedBy.SelectionCondition ?? new List<DisplayEntry>()) {
                    conditions.Add("(& { " + EntryExpression(condition) + " })");
                }
                expression.Append(expression.Length == 0 ? "if (" : " elseif (").Append(string.Join(" -or ", conditions)).Append(") { ");
                if (column < row.Columns.Count) {
                    expression.Append(EntryExpression(row.Columns[column].DisplayEntry));
                }
                expression.Append(" }");
            }
            if (fallback != null && column < fallback.Columns.Count) {
                string value = EntryExpression(fallback.Columns[column].DisplayEntry);
                if (expression.Length == 0) {
                    return value;
                }
                expression.Append(" else { ").Append(value).Append(" }");
            }
            return expression.ToString();
        }

        private static string EntryExpression(DisplayEntry entry) => entry.ValueType == DisplayEntryValueType.ScriptBlock
            ? "& { " + entry.Value + " }"
            : "$_.PSObject.Properties['" + entry.Value.Replace("'", "''") + "'].Value";

        private static string? AlignmentName(Alignment alignment) {
            switch (alignment) {
                case Alignment.Left:
                    return "Left";
                case Alignment.Center:
                    return "Center";
                case Alignment.Right:
                    return "Right";
                default:
                    return null;
            }
        }

        private sealed class ColumnSpec {
            public ColumnSpec(string label, string value, bool isScript, string? alignment, string? formatString) {
                Label = label;
                Value = value;
                IsScript = isScript;
                Alignment = alignment;
                FormatString = formatString;
            }

            public string Label { get; }

            public string Value { get; }

            public bool IsScript { get; }

            public string? Alignment { get; }

            public string? FormatString { get; }
        }
    }
}
