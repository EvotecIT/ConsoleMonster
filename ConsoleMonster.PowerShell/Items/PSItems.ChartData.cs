using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    internal static partial class PSItems {
        // Adapt records once; the shared factory validates the complete typed definition before building a chart.
        internal static void ChartData(ChartOptions options, IEnumerable<object> input) {
            var samples = new List<double>(options.Values ?? Array.Empty<double>());
            foreach (object item in input) {
                object? raw = PSText.Unwrap(item);
                if (raw == null) { throw new PSArgumentException("Chart data cannot contain null."); }
                object record = Item(item);
                if (options.Type == ChartType.Histogram || options.Type == ChartType.Sparkline) {
                    samples.Add(ChartNumber(raw));
                } else if (options.Type == ChartType.Line || options.Type == ChartType.Scatter) {
                    if (raw is ChartSeries series) { options.Series.Add(series); }
                    else if (raw is string key) { options.Series.Add(new ChartSeries(key)); }
                    else if (raw is IDictionary dictionary && !HasField(dictionary, "Key")) {
                        foreach (DictionaryEntry entry in dictionary) { options.Series.Add(Series(PSWidgets.ToText(entry.Key), entry.Value)); }
                    } else {
                        RecordFields(record, "Key Label Role Values");
                        object? keyValue = Field(record, "Key");
                        if (keyValue == null) { throw new PSArgumentException("A chart series needs Key, with optional Label, Role and Values."); }
                        object? role = Field(record, "Role");
                        var parsed = Series(PSText.PlainText(keyValue), Field(record, "Values"), role == null ? (ThemeToken?)null : LanguagePrimitives.ConvertTo<ThemeToken>(role));
                        if (Field(record, "Label") is object label) { parsed.Label = PSText.PlainText(label); parsed.LabelText = label as Text.RichText; }
                        options.Series.Add(parsed);
                    }
                } else {
                    if (raw is ChartItem bar) { options.Items.Add(bar); }
                    else if (raw is IDictionary dictionary && !HasField(dictionary, "Label")) {
                        foreach (DictionaryEntry entry in dictionary) { options.Items.Add(new ChartItem(PSWidgets.ToText(entry.Key), ChartNumber(entry.Value))); }
                    } else {
                        string? problem = null;
                        ChartItem? parsed = BarItemFrom(record, ref problem);
                        if (parsed == null) { throw new PSArgumentException(problem ?? "A bar needs Label and a finite numeric Value."); }
                        options.Items.Add(parsed);
                    }
                }
            }
            if (options.Type == ChartType.Histogram || options.Type == ChartType.Sparkline) { options.Values = samples.ToArray(); }
        }

        private static ChartSeries Series(string key, object? values, ThemeToken? role = null) {
            var series = new ChartSeries(key, role);
            if (values != null) {
                foreach (object value in Items(values)) { series.Add(ChartNumber(value), 0); }
            }
            return series;
        }

        private static double ChartNumber(object? value) {
            if (!PipelineChart.TryNumber(PSText.Unwrap(value), out double number)) { throw new PSArgumentException("A chart requires a finite numeric Value."); }
            return number;
        }

        private static bool HasField(IDictionary dictionary, string name) {
            foreach (object key in dictionary.Keys) { if (string.Equals(PSWidgets.ToText(key), name, StringComparison.OrdinalIgnoreCase)) { return true; } }
            return false;
        }
    }
}
