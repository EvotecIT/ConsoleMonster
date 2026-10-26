using System;
using System.Collections.Generic;
using System.Globalization;
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

        private static BoundColumn BindChartColumn(IReadOnlyList<PSObject> items, object property) {
            IReadOnlyList<BoundColumn> columns = PropertyBinder.Bind(items, new[] { property });
            if (columns.Count != 1) { throw new PSArgumentException("Chart property selections must resolve to exactly one column."); }
            return columns[0];
        }
    }
}
