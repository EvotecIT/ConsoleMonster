using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Maps item objects (from the New-Console*Item cmdlets, plain values and pipeline input) onto the existing add methods of
    /// a widget. Containers use it for their script block and pipeline input; Add-ConsoleItem uses it for a widget made
    /// earlier. No rendering or layout logic lives here: every item ends in one of the widget's own public methods.
    /// </summary>
    internal static class PSItems {
        // Series made by New-ConsoleChartSeries without -Tone: the chart picks their colour (status words take their tone).
        private static readonly ConditionalWeakTable<ChartSeries, object> AutomaticTone = new ConditionalWeakTable<ChartSeries, object>();

        /// <summary>Remembers that a series has no chosen tone.</summary>
        internal static void MarkAutomaticTone(ChartSeries series) => AutomaticTone.Add(series, true);

        /// <summary>What a widget accepts, for error messages and help.</summary>
        internal static string Accepted(object widget) {
            switch (widget) {
                case Tree _:
                case TreeNode _:
                    return "New-ConsoleTreeNode objects or strings";
                case KeyValueList _:
                    return "New-ConsoleKeyValueItem objects or hashtables";
                case StatusList _:
                    return "New-ConsoleStatusItem objects, hashtables or objects with Label, Status and Detail";
                case Phases _:
                    return "New-ConsoleStatusItem objects, hashtables or phase names";
                case BarChart _:
                case StackedBar _:
                    return "New-ConsoleBarItem objects or hashtables";
                case LineChart _:
                    return "New-ConsoleChartSeries objects";
                case LogRegion _:
                    return "New-ConsoleLogEntry objects or strings";
                case Lanes _:
                    return "New-ConsoleLane objects or strings";
                case CellMap _:
                    return "status words (Pass, Fail, Warn, Skip, Running, Pending)";
                case Sparkline _:
                case Histogram _:
                    return "numbers";
                case LiveTable _:
                    return "objects or hashtables with a property per column";
                default:
                    return "nothing (this widget has no items)";
            }
        }

        /// <summary>Runs a script block (or takes a value or an array) and returns the items it produced, unwrapped.</summary>
        internal static List<object> Items(object? content) {
            List<object> items = new List<object>();
            object? raw = PSText.Unwrap(content);
            if (raw == null) {
                return items;
            }
            if (raw is ScriptBlock script) {
                foreach (PSObject emitted in script.Invoke()) {
                    Flatten(items, emitted);
                }
            } else {
                Flatten(items, content);
            }
            return items;
        }

        private static void Flatten(List<object> items, object? value) {
            object? raw = PSText.Unwrap(value);
            if (raw == null) {
                return;
            }
            // Arrays and lists are their items (a double[] of samples, a List of nodes); strings, dictionaries and
            // widgets are one item each.
            if (raw is IEnumerable enumerable && !(raw is string) && !(raw is IDictionary) && !(raw is Layout.IWidget) && !(raw is PSCustomObject)) {
                foreach (object? element in enumerable) {
                    Flatten(items, element);
                }
                return;
            }
            items.Add(Item(value!));
        }

        // The item itself, except a [pscustomobject], whose properties live on its PowerShell wrapper.
        private static object Item(object value) {
            object? raw = PSText.Unwrap(value);
            return raw == null || raw is PSCustomObject ? value : raw;
        }

        /// <summary>Adds every item to the widget; throws a terminating error for items the widget does not take.</summary>
        internal static void AddAll(PSCmdlet cmdlet, object widget, IEnumerable<object> items, string? keyProperty = null) {
            foreach (object item in items) {
                if (!TryAdd(widget, item, keyProperty, out string? problem)) {
                    cmdlet.ThrowTerminatingError(new ErrorRecord(
                        new PSArgumentException(problem ?? (TypeLabel(item) + " cannot be added to a " + widget.GetType().Name + ". It takes " + Accepted(widget) + ".")),
                        "ConsoleMonsterItemNotAccepted", ErrorCategory.InvalidArgument, item));
                }
            }
        }

        private static string TypeLabel(object item) => item is string ? "A string" : "A " + item.GetType().Name;

        /// <summary>Adds one item through the widget's own methods.</summary>
        internal static bool TryAdd(object widget, object item, string? keyProperty, out string? problem) {
            problem = null;
            item = Item(item);
            switch (widget) {
                case Tree tree:
                    return TryAddNode(tree.Root, item);
                case TreeNode node:
                    return TryAddNode(node, item);
                case KeyValueList list:
                    return TryAddPair(list, item);
                case StatusList status:
                    if (StatusItemFrom(item, ref problem) is StatusItem statusItem) {
                        status.Set(statusItem.Key, statusItem.Label, statusItem.Status, statusItem.Detail);
                        return true;
                    }
                    return false;
                case Phases phases:
                    if (item is string phaseName) {
                        phases.Set(phaseName);
                        return true;
                    }
                    if (StatusItemFrom(item, ref problem) is StatusItem phase) {
                        phases.Set(phase.Key, phase.Status, phase.Label, phase.Detail);
                        return true;
                    }
                    return false;
                case BarChart bars:
                    if (BarItemFrom(item, ref problem) is ChartItem bar) {
                        if (bar.LabelText != null) {
                            bars.Add(bar.LabelText, bar.Value, bar.Token);
                        } else {
                            bars.Add(bar.Label, bar.Value, bar.Token);
                        }
                        return true;
                    }
                    return false;
                case StackedBar stacked:
                    if (BarItemFrom(item, ref problem) is ChartItem segment) {
                        if (segment.LabelText != null) {
                            stacked.Add(segment.LabelText, segment.Value, segment.Token);
                        } else {
                            stacked.Add(segment.Label, segment.Value, segment.Token);
                        }
                        return true;
                    }
                    return false;
                case LineChart chart:
                    if (item is ChartSeries series) {
                        ThemeToken? token = AutomaticTone.TryGetValue(series, out _) ? (ThemeToken?)null : series.Token;
                        if (series.NameText != null) {
                            chart.AddSeries(series.NameText, token);
                        } else {
                            chart.AddSeries(series.Name, token);
                        }
                        foreach (double value in series.Values) {
                            chart.Add(series.Name, value);
                        }
                        return true;
                    }
                    return false;
                case LogRegion log:
                    if (item is LogEntry entry) {
                        log.Write(entry);
                        return true;
                    }
                    if (item is string line) {
                        log.Write(line);
                        return true;
                    }
                    return false;
                case Lanes lanes:
                    return TryAddLane(lanes, item);
                case CellMap cells:
                    if (item is StatusKind cellState) {
                        cells.Add(cellState);
                        return true;
                    }
                    if (item is string word && StatusKinds.TryParse(word, out StatusKind parsed)) {
                        cells.Add(parsed);
                        return true;
                    }
                    problem = "'" + item + "' is not a status. Use one of: " + string.Join(", ", StatusKinds.Words) + ".";
                    return false;
                case Sparkline sparkline:
                    if (TryNumber(item, out double sample)) {
                        sparkline.Add(sample);
                        return true;
                    }
                    return false;
                case Histogram histogram:
                    if (TryNumber(item, out double counted)) {
                        histogram.Add(counted);
                        return true;
                    }
                    return false;
                case LiveTable table:
                    return TryAddRow(table, item, keyProperty, out problem);
                default:
                    return false;
            }
        }

        private static bool TryAddNode(TreeNode parent, object item) {
            if (item is TreeNode node) {
                parent.Add(node);
                return true;
            }
            if (item is Text.RichText rich) {
                parent.Add(rich);
                return true;
            }
            if (item is string label) {
                // Strings are data (member names, paths): shown literally. New-ConsoleTreeNode takes markup.
                parent.Add(Text.Markup.Escape(label));
                return true;
            }
            return false;
        }

        private static bool TryAddPair(KeyValueList list, object item) {
            if (item is KeyValueItem pair) {
                list.Add(pair);
                return true;
            }
            if (item is IDictionary dictionary) {
                foreach (DictionaryEntry entry in dictionary) {
                    list.Add(PSWidgets.ToText(entry.Key), Text.Markup.Escape(PSWidgets.ToText(entry.Value)));
                }
                return true;
            }
            if (item is PSObject record) {
                foreach (PSPropertyInfo property in record.Properties) {
                    if (property.IsGettable) {
                        list.Add(property.Name, Text.Markup.Escape(PSWidgets.ToText(Property(record, record, property.Name))));
                    }
                }
                return true;
            }
            return false;
        }

        private static bool TryAddLane(Lanes lanes, object item) {
            if (item is Lane spec) {
                lanes.Add(spec);
                return true;
            }
            if (item is Text.RichText rich) {
                lanes.Add(rich.PlainText, rich);
                return true;
            }
            if (item is string key) {
                lanes.Add(key, key);
                return true;
            }
            return false;
        }

        private static StatusItem? StatusItemFrom(object item, ref string? problem) {
            if (item is StatusItem statusItem) {
                return statusItem;
            }
            if (IsRecord(item)) {
                // @{ Label = 'Restore'; Status = 'Pass'; Detail = '1.6 s' } (or such an object) reads like the item cmdlet's parameters.
                string label = PSWidgets.ToText(Field(item, "Label") ?? Field(item, "Name") ?? Field(item, "Key"));
                if (label.Length == 0) {
                    problem = "A status item needs a Label (or Name) value.";
                    return null;
                }
                StatusKind status = StatusKind.Pending;
                object? state = Field(item, "Status");
                if (state is StatusKind kind) {
                    status = kind;
                } else if (state != null && !StatusKinds.TryParse(PSWidgets.ToText(state), out status)) {
                    problem = "'" + PSWidgets.ToText(state) + "' is not a status. Use one of: " + string.Join(", ", StatusKinds.Words) + ".";
                    return null;
                }
                object? detail = Field(item, "Detail");
                string key = PSWidgets.ToText(Field(item, "Key") ?? label);
                return new StatusItem(key, Text.Markup.Escape(label), status, detail == null ? null : PSWidgets.ToText(detail));
            }
            return null;
        }

        private static ChartItem? BarItemFrom(object item, ref string? problem) {
            if (item is ChartItem chartItem) {
                return chartItem;
            }
            if (IsRecord(item)) {
                object? label = Field(item, "Label") ?? Field(item, "Name");
                object? value = Field(item, "Value");
                if (label == null || value == null || !TryNumber(value, out double number)) {
                    problem = "A bar needs a Label (or Name) and a numeric Value.";
                    return null;
                }
                ThemeToken? token = null;
                object? tone = Field(item, "Tone");
                if (tone != null) {
                    string toneText = PSWidgets.ToText(tone);
                    if (int.TryParse(toneText, out _) || !Enum.TryParse(toneText, true, out ThemeToken parsed) || !Enum.IsDefined(typeof(ThemeToken), parsed)) {
                        problem = "'" + toneText + "' is not a tone. Use a theme role such as Pass, Fail, Warn, Accent or Series1 to Series6.";
                        return null;
                    }
                    token = parsed;
                }
                return new ChartItem(PSWidgets.ToText(label), number, token);
            }
            return null;
        }

        private static bool TryAddRow(LiveTable table, object item, string? keyProperty, out string? problem) {
            problem = null;
            PSObject row = PSObject.AsPSObject(item);
            List<object?> values = new List<object?>();
            foreach (TableColumn column in table.Table.Columns) {
                values.Add(Property(row, item, Text.Markup.Strip(column.Header)));
            }
            string keyName = keyProperty ?? (table.Table.Columns.Count > 0 ? Text.Markup.Strip(table.Table.Columns[0].Header) : string.Empty);
            object? key = Property(row, item, keyName);
            if (key == null) {
                problem = "The row has no '" + keyName + "' value to use as its key. Give -KeyProperty or a value for the first column.";
                return false;
            }
            table.Set(PSWidgets.ToText(key), values.ToArray());
            return true;
        }

        private static object? Property(PSObject row, object item, string name) {
            if (item is IDictionary dictionary) {
                return Lookup(dictionary, name);
            }
            PSPropertyInfo? property = row.Properties[name];
            if (property == null) {
                return null;
            }
            try {
                return PSText.Unwrap(property.Value);
            } catch (GetValueException) {
                return null;
            }
        }

        // A hashtable or an object with properties (not a string, number or other plain value).
        private static bool IsRecord(object item) => item is IDictionary || item is PSObject || !(item is string || item is IConvertible || item is Layout.IWidget);

        // A value from a hashtable key or an object property, case-insensitively.
        private static object? Field(object item, string name) {
            if (item is IDictionary dictionary) {
                return Lookup(dictionary, name);
            }
            PSObject record = item as PSObject ?? PSObject.AsPSObject(item);
            return Property(record, item, name);
        }

        private static object? Lookup(IDictionary dictionary, string name) {
            foreach (DictionaryEntry entry in dictionary) {
                if (string.Equals(PSWidgets.ToText(entry.Key), name, StringComparison.OrdinalIgnoreCase)) {
                    return PSText.Unwrap(entry.Value);
                }
            }
            return null;
        }

        private static bool TryNumber(object item, out double value) {
            value = 0;
            if (item is string text) {
                return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }
            if (item is IConvertible) {
                try {
                    value = LanguagePrimitives.ConvertTo<double>(item);
                    return true;
                } catch (PSInvalidCastException) {
                    return false;
                }
            }
            return false;
        }
    }
}
