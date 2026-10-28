using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.PowerShell.Binding;
using ConsoleMonster.Values;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    public abstract partial class ConsoleDisplayCmdlet {
        private IWidget? BuildChartView(IReadOnlyList<PSObject> input) {
            IReadOnlyList<PSObject> items = PSFormatting.Expand(input, PSShaping.Options());
            object property = Property == null || Property.Length == 0 ? "Value" : Property.Length == 1 ? Property[0] : throw new PSArgumentException("Chart requires exactly one Property.");
            BoundColumn value = BindChartColumn(items, property);
            value.FormatString = null;
            BoundColumn? label = Type == ChartType.Histogram ? null : BindChartColumn(items, Label);
            if ((Property == null || Property.Length == 0) && items.Count > 0 && !items.Any(HasValueMember) && !HasNumber(items, value)) {
                // Without -Property the chart reads Value; one hint naming the numeric columns beats an error per row.
                var numeric = new List<string>();
                foreach (PSPropertyInfo candidate in items[0].Properties) {
                    if (candidate.IsGettable && PipelineChart.TryNumber(PSText.Unwrap(SafeValue(candidate)), out _)) { numeric.Add(candidate.Name); }
                }
                throw new PSArgumentException("Chart reads the Value property by default and these objects have none. Choose the numeric property with -Property"
                    + (numeric.Count == 0 ? "." : " (for example -Property " + numeric[0] + "; numeric properties: " + string.Join(", ", numeric) + ")."));
            }
            var chart = new PipelineChart { Type = Type, Sort = Sort, MaxRows = MaxRows, Bins = Bins, Unit = Unit };
            int row = 0;
            foreach (PSObject item in items) {
                row++;
                object? raw = PSText.Unwrap(value.GetValue(item));
                object? name = label == null ? null : PSText.Unwrap(label.GetValue(item));
                if (!PipelineChart.TryNumber(raw, out double number) || name is FieldError) {
                    WriteError(new ErrorRecord(new ArgumentException("Row " + row.ToString(CultureInfo.InvariantCulture) + " must have a finite numeric value and a readable label."), "ConsoleMonsterChartValue", ErrorCategory.InvalidData, item));
                    continue;
                }
                string text = Convert.ToString(name, CultureInfo.InvariantCulture) ?? string.Empty;
                if (text.Length == 0 && Type == ChartType.Bar) { text = row.ToString(CultureInfo.InvariantCulture); }
                chart.Add(text, number);
            }
            return chart.Samples.Count == 0 ? null : chart;
        }

        private static bool HasNumber(IReadOnlyList<PSObject> items, BoundColumn value) {
            foreach (PSObject item in items) { if (PipelineChart.TryNumber(PSText.Unwrap(value.GetValue(item)), out _)) { return true; } }
            return false;
        }

        private static bool HasValueMember(PSObject item) => PSText.Unwrap(item) is System.Collections.IDictionary map ? map.Contains("Value") : item.Properties["Value"] != null;

        private static object? SafeValue(PSPropertyInfo property) {
            try { return property.Value; } catch (GetValueException) { return null; }
        }

        private static BoundColumn BindChartColumn(IReadOnlyList<PSObject> items, object property) {
            IReadOnlyList<BoundColumn> columns = PropertyBinder.Bind(items, new[] { property });
            if (columns.Count != 1) { throw new PSArgumentException("Chart property selections must resolve to exactly one column."); }
            return columns[0];
        }
    }
}
