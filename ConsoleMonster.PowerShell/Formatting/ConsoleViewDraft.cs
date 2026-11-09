using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.PowerShell.Binding;
using ConsoleMonster.Presenters;
using ConsoleMonster.Values;

namespace ConsoleMonster.PowerShell {
    /// <summary>Samples PowerShell members and uses the table's own binding and inference to draft columns.</summary>
    internal sealed partial class ConsoleViewDraft {
        internal const int SampleLimit = 50;
        private readonly string _typeName;
        private readonly string? _statusProperty;
        internal object[] Property { get; }

        private ConsoleViewDraft(string typeName, object[] property, string? statusProperty) {
            _typeName = typeName;
            Property = property;
            _statusProperty = statusProperty;
        }

        internal static ConsoleViewDraft Create(string typeName, IReadOnlyList<PSObject> samples) {
            List<string> names = new List<string>();
            PSPropertySet? display = PropertyBinder.GetDefaultDisplayForSource(samples[0]);
            if (display != null && display.ReferencedPropertyNames.Count > 0) {
                names.AddRange(display.ReferencedPropertyNames);
            } else {
                foreach (PSObject sample in samples) {
                    foreach (PSPropertyInfo property in sample.Properties) {
                        if (property.IsGettable && !names.Contains(property.Name, StringComparer.OrdinalIgnoreCase)) {
                            names.Add(property.Name);
                        }
                    }
                }
            }
            names.RemoveAll(name => name.Equals("PSComputerName", StringComparison.OrdinalIgnoreCase)
                || name.Equals("RunspaceId", StringComparison.OrdinalIgnoreCase)
                || name.Equals("PSShowComputerName", StringComparison.OrdinalIgnoreCase));

            // Literal expressions preserve unusual property names rather than treating them as wildcard selectors.
            object[] specs = names.Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => (object)new Hashtable { ["Name"] = name, ["Expression"] = name }).ToArray();
            if (specs.Length == 0) { return new ConsoleViewDraft(typeName, Array.Empty<object>(), null); }
            List<BoundColumn> columns = PropertyBinder.Bind(samples, specs).ToList();
            List<object?[]> rows = samples.Select(sample => columns.Select(column => ReadProperty(sample, column.Name)).ToArray()).ToList();
            PSFormatting.ChoosePresenters(columns, rows, true);
            List<int> selected = Enumerable.Range(0, columns.Count)
                .Where(c => rows.Any(row => !Empty(row[c]))).ToList();
            int identity = ValueShaper.TitleIndex(selected.Select(c => columns[c].Name).ToList());
            if (identity > 0) {
                int column = selected[identity];
                selected.RemoveAt(identity);
                selected.Insert(0, column);
            }
            // Keep an Id and its CPU metric next to the record name, ahead of ancillary counters.
            int leading = selected.Count > 0 ? selected[0] : -1;
            selected = selected.OrderBy(c => c == leading ? 0
                : columns[c].Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ? 1
                : columns[c].Name.Equals("CPU", StringComparison.OrdinalIgnoreCase) ? 2 : 3).ToList();
            List<object> properties = new List<object>();
            string? status = null;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            foreach (int c in selected.Take(6)) {
                object[] values = rows.Select(row => Unwrap(row[c])).Where(value => !Empty(value)).Cast<object>().ToArray();
                if (values.Length == 0) { continue; }
                ICellPresenter? presenter = columns[c].Column.Presenter;
                if (values.All(value => value is string)) {
                    bool words = values.All(value => StatusEnumPresenter.TryRead((string)value, out _));
                    if (words) { presenter = CellPresenters.StatusWord; }
                    else if (presenter == CellPresenters.StatusWord) { presenter = null; }
                }
                string? presenterName = PresenterName(presenter);
                if (presenter == CellPresenters.Bytes && WideByteRange(values)) { presenterName = "BytesBar"; }
                if (presenter == CellPresenters.Date && values.All(value => RecentDate(value, now))) {
                    presenterName = "DateRelative";
                }
                Hashtable column = new Hashtable { ["Name"] = columns[c].Name, ["Expression"] = columns[c].Name };
                if (presenterName != null) { column["Presenter"] = presenterName; }
                if (presenter == CellPresenters.StatusWord && status == null) { status = columns[c].Name; }
                // A wildcard-bearing member still needs a literal expression instead of expanding a selection pattern.
                properties.Add(presenterName == null && columns[c].Name.IndexOfAny(new[] { '*', '?' }) < 0
                    ? (object)columns[c].Name : column);
            }
            return new ConsoleViewDraft(typeName, properties.ToArray(), status);
        }

        private static object? Unwrap(object? value) => ReferenceEquals(value, System.Management.Automation.Internal.AutomationNull.Value)
            ? null : value is PSObject ps ? ps.BaseObject : value;

        // Draft the supplied members, even when the session already has a registered shaper for this type.
        private static object? ReadProperty(PSObject sample, string name) {
            try { return Unwrap(sample.Properties[name]?.Value); }
            catch (Exception ex) when (!(ex is PipelineStoppedException) && !(ex is OperationCanceledException)) {
                return FieldError.From(ex);
            }
        }

        private static bool Empty(object? value) => PresenterValues.IsEmpty(value) || value is FieldError
            || (value is string text && string.IsNullOrWhiteSpace(text)) || (value is ICollection collection && collection.Count == 0);

        private static bool WideByteRange(object[] values) {
            List<double> numbers = new List<double>();
            foreach (object value in values) {
                if (!PresenterValues.TryGetNumber(value, out double number) || number < 0) { return false; }
                numbers.Add(number);
            }
            double min = numbers.Min(), max = numbers.Max();
            return max > 0 && (min == 0 || max / min > 10);
        }

        private static bool RecentDate(object value, DateTimeOffset now) {
            DateTimeOffset date;
            if (value is DateTimeOffset offset) { date = offset; }
            else if (value is DateTime time) { date = new DateTimeOffset(time.ToUniversalTime()); }
            else { return false; }
            return date <= now && date >= now.AddDays(-30);
        }

        private static string? PresenterName(ICellPresenter? presenter) {
            foreach (string name in new[] { "Identifier", "Bytes", "Date", "Duration", "Boolean", "StatusWord" }) {
                if (ReferenceEquals(presenter, CellPresenters.FromName(name))) { return name; }
            }
            return null;
        }
    }
}
