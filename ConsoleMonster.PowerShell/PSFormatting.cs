using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.PowerShell.Binding;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Shared building and output for the Format-Console* cmdlets (the cmdlets only bind parameters).</summary>
    internal static class PSFormatting {
        /// <summary>Builds a table from pipeline objects, with optional groups and totals.</summary>
        internal static Table BuildTable(IReadOnlyList<PSObject> items, object[]? property, object? groupBy, bool total, bool autoPresenters = false) {
            IReadOnlyList<BoundColumn> columns = PropertyBinder.Bind(items, property);
            if (autoPresenters) {
                // Presenters by member name and .NET type (bytes for Length, dates, durations, status-like enums).
                foreach (BoundColumn column in columns) {
                    if (column.Column.Presenter != null) {
                        continue;
                    }
                    foreach (PSObject item in items) {
                        object? value = PropertyBinder.Unwrap(column.GetValue(item));
                        if (value != null) {
                            column.Column.Presenter = CellPresenters.ForMember(column.Name, value.GetType());
                            break;
                        }
                    }
                }
            }
            Table table = new Table();
            foreach (BoundColumn column in columns) {
                table.AddColumn(column.Column);
            }
            if (total) {
                MarkNumericTotals(columns, items);
                table.ShowTotals = true;
            }
            if (groupBy != null) {
                table.ShowSubtotals = total;
                foreach (KeyValuePair<string, IReadOnlyList<PSObject>> group in GroupBinder.Group(items, groupBy)) {
                    table.AddGroup(Text.Markup.Escape(group.Key));
                    foreach (PSObject item in group.Value) {
                        table.AddValues(Values(columns, item));
                    }
                }
            } else {
                foreach (PSObject item in items) {
                    table.AddValues(Values(columns, item));
                }
            }
            return table;
        }

        /// <summary>Builds a record view: one section per object (titled by the key column), groups as top-level sections.</summary>
        internal static RecordView BuildList(IReadOnlyList<PSObject> items, object[]? property, object? groupBy, bool showEmpty, int depth, string? title, System.Globalization.CultureInfo? culture = null) {
            System.Globalization.CultureInfo resolved = ResolveCulture(culture);
            IReadOnlyList<BoundColumn> columns = PropertyBinder.Bind(items, property);
            RecordView view = new RecordView(title) { HideEmpty = !showEmpty };
            int key = KeyIndex(columns);
            if (items.Count > 1 || groupBy != null) {
                view.Summary = items.Count.ToString("N0", resolved) + (items.Count == 1 ? " record" : " records");
            }
            if (groupBy != null) {
                foreach (KeyValuePair<string, IReadOnlyList<PSObject>> group in GroupBinder.Group(items, groupBy)) {
                    view.AddSection(Text.Markup.Escape(group.Key));
                    foreach (PSObject item in group.Value) {
                        AddRecord(view, columns, key, item, 1, depth, true, resolved);
                    }
                }
            } else {
                foreach (PSObject item in items) {
                    AddRecord(view, columns, key, item, 0, depth, items.Count > 1 || title == null, resolved);
                }
            }
            return view;
        }

        /// <summary>Inspector options for PowerShell objects (PSObject members first, then dictionaries and reflection).</summary>
        internal static InspectorOptions InspectorOptions(int depth, int maxItems, string[]? include, string[]? exclude, int maxNodes = 5000) {
            // PSObjects wrapping dictionaries, collections or scalars inspect as those; PSCustomObjects keep their note properties.
            InspectorOptions options = new InspectorOptions { MaxDepth = depth, MaxItems = maxItems, MaxNodes = maxNodes, Unwrap = PSObjectMemberSource.UnwrapPSObject };
            options.Sources.Add(new PSObjectMemberSource());
            if (include != null) {
                foreach (string name in include) {
                    options.Include.Add(name);
                }
            }
            if (exclude != null) {
                foreach (string name in exclude) {
                    options.Exclude.Add(name);
                }
            }
            return options;
        }

        /// <summary>The culture a Format cmdlet renders with: -Culture, else the profile's (CONSOLEMONSTER_CULTURE), else the current culture.</summary>
        internal static System.Globalization.CultureInfo ResolveCulture(System.Globalization.CultureInfo? culture) {
            return culture ?? Terminal.Profile.Culture ?? System.Globalization.CultureInfo.CurrentCulture;
        }

        /// <summary>Writes a widget to the host, laid out for an explicit width and culture when given.</summary>
        internal static void Write(PSCmdlet cmdlet, IWidget widget, int width, System.Globalization.CultureInfo? culture = null) {
            TerminalProfile profile = Terminal.GetCurrentProfile();
            if (culture != null) {
                profile = profile.WithCulture(culture);
            }
            if (width > 0) {
                profile = profile.WithSize(width, profile.Height);
            }
            // A blank line after each block, as Format-Table does, so consecutive outputs stay apart.
            HostOutput.Write(cmdlet, new Rows(widget, new TextBlock(string.Empty)), profile, width > 0);
        }

        private static void AddRecord(RecordView view, IReadOnlyList<BoundColumn> columns, int key, PSObject item, int level, int depth, bool heading, System.Globalization.CultureInfo culture) {
            int fieldLevel = level;
            if (heading) {
                object? title = columns[key].GetValue(item);
                view.AddHeading(title is ConsoleMonster.Text.RichText richTitle ? richTitle.ToMarkup() : Text.Markup.Escape(PresenterValues.ToText(title, culture)), level);
                fieldLevel = level + 1;
            }
            for (int c = 0; c < columns.Count; c++) {
                if (heading && c == key) {
                    continue;
                }
                object? value = columns[c].GetValue(item);
                TableColumn column = columns[c].Column;
                if (depth > 0 && value != null && !ColumnInference.IsScalar(value) && columns[c].Column.Presenter == null) {
                    view.AddHeading(Text.Markup.Escape(Text.Markup.Strip(column.Header)), fieldLevel);
                    view.AddWidgetAt(fieldLevel + 1, InspectorBody(value, depth));
                    continue;
                }
                view.AddFieldAt(fieldLevel, Text.Markup.Strip(column.Header), TableCell.FromValue(value), column.Presenter);
            }
        }

        // A nested inspector without its own title line (the property name is the heading).
#pragma warning disable IL2026
        private static IWidget InspectorBody(object value, int depth) {
            InspectNode node = ObjectInspector.Inspect(value, InspectorOptions(depth, 25, null, null));
            return new InspectorView(node) { ShowHeader = false };
        }
#pragma warning restore IL2026

        private static object?[] Values(IReadOnlyList<BoundColumn> columns, PSObject item) {
            object?[] values = new object?[columns.Count];
            for (int c = 0; c < columns.Count; c++) {
                values[c] = columns[c].GetValue(item);
            }
            return values;
        }

        private static int KeyIndex(IReadOnlyList<BoundColumn> columns) {
            for (int c = 0; c < columns.Count; c++) {
                if (columns[c].Column.IsKey) {
                    return c;
                }
            }
            return 0;
        }

        // -Total sums numeric columns that have no explicit aggregate (the key column labels the row).
        private static void MarkNumericTotals(IReadOnlyList<BoundColumn> columns, IReadOnlyList<PSObject> items) {
            for (int c = 0; c < columns.Count; c++) {
                TableColumn column = columns[c].Column;
                if (column.IsKey || column.Aggregate != ColumnAggregate.None) {
                    continue;
                }
                bool numeric = false;
                bool other = false;
                foreach (PSObject item in items) {
                    object? value = columns[c].GetValue(item);
                    if (PresenterValues.IsEmpty(value)) {
                        continue;
                    }
                    if (PresenterValues.TryGetNumber(value, out _) && !(value is string)) {
                        numeric = true;
                    } else {
                        other = true;
                        break;
                    }
                }
                if (numeric && !other) {
                    column.Aggregate = ColumnAggregate.Sum;
                }
            }
        }
    }
}
