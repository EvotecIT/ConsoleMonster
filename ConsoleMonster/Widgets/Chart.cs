using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Widgets {
    /// <summary>Creates numeric chart representations through the same validated factory as New-ConsoleChart.</summary>
    public static class Chart {
        /// <summary>Builds a chart and validates options against its selected kind before adding data.</summary>
        /// <param name="options">Chart kind, initial data and presentation options.</param>
        /// <returns>The concrete numeric chart widget.</returns>
        public static IWidget Create(ChartOptions? options = null) {
            ChartOptions o = options ?? new ChartOptions();
            if (!Enum.IsDefined(typeof(ChartType), o.Type)) { throw new ArgumentOutOfRangeException(nameof(o.Type)); }
            string common = o.Type == ChartType.Stacked ? "Type WidgetId" : "Type WidgetId Unit";
            string specific;
            switch (o.Type) {
                case ChartType.Bar: specific = "Role Shape Maximum Warning Critical ValueColumn"; break;
                case ChartType.Stacked: specific = "CountColumn PercentColumn LabelColumn"; break;
                case ChartType.Line: case ChartType.Scatter: specific = "Height Minimum Maximum"; break;
                case ChartType.Histogram: specific = "Height Role Edges Values"; break;
                default: specific = "Height Role Label Maximum ShowLatest Values"; break;
            }
            var allowed = new HashSet<string>((common + " " + specific).Split(' '), StringComparer.Ordinal);
            foreach (string field in o.Bound) {
                if (!allowed.Contains(field)) { throw new ArgumentException(o.Type + " does not support " + field + ". Supported options: " + common + " " + specific + "."); }
            }
            if (o.Height.HasValue && o.Height < 1) { throw new ArgumentOutOfRangeException(nameof(o.Height)); }
            if (o.Minimum.HasValue && o.Maximum.HasValue && o.Minimum >= o.Maximum) { throw new ArgumentException("Minimum must be less than Maximum."); }
            if (o.Items.Count > 0 && o.Type != ChartType.Bar && o.Type != ChartType.Stacked) { throw new ArgumentException("Items apply to Bar and Stacked charts."); }
            if (o.Series.Count > 0 && o.Type != ChartType.Line && o.Type != ChartType.Scatter) { throw new ArgumentException("Series apply to Line and Scatter charts."); }
            if (o.Role.HasValue && !Enum.IsDefined(typeof(Styling.ThemeToken), o.Role.Value)) { throw new ArgumentOutOfRangeException(nameof(o.Role)); }
            foreach (double? limit in new[] { o.Minimum, o.Maximum, o.Warning, o.Critical }) {
                if (limit.HasValue && !NumberFormat.IsFinite(limit.Value)) { throw new ArgumentException("Chart limits and thresholds must be finite.", nameof(options)); }
            }
            foreach (ChartItem item in o.Items) { if (item == null || !NumberFormat.IsFinite(item.Value) || item.Role.HasValue && !Enum.IsDefined(typeof(Styling.ThemeToken), item.Role.Value)) { throw new ArgumentException("Every chart item requires a finite Value.", nameof(options)); } }
            foreach (ChartSeries series in o.Series) {
                if (series == null || !Enum.IsDefined(typeof(Styling.ThemeToken), series.Role) || series.Values.Any(value => !NumberFormat.IsFinite(value))) { throw new ArgumentException("Every series requires finite Values.", nameof(options)); }
            }
            foreach (double value in o.Values ?? Array.Empty<double>()) { if (!NumberFormat.IsFinite(value)) { throw new ArgumentException("Chart Values must be finite.", nameof(options)); } }
            string id = o.WidgetId ?? DefaultId(o.Type);
            switch (o.Type) {
                case ChartType.Bar:
                    var bars = new BarChart(id) { Unit = o.Unit, Shape = o.Shape, Maximum = o.Maximum, Warning = o.Warning, Critical = o.Critical };
                    if (o.Role.HasValue) { bars.Role = o.Role.Value; }
                    if (o.ValueColumn != null) { bars.ValueColumn = o.ValueColumn; }
                    foreach (ChartItem item in o.Items) {
                        if (item.LabelText != null) { bars.Add(item.LabelText, item.Value, item.Role, item.Key); }
                        else { bars.Add(item.Label, item.Value, item.Role, item.Key); }
                    }
                    return bars;
                case ChartType.Stacked:
                    var stacked = new StackedBar(id);
                    if (o.CountColumn != null) { stacked.CountColumn = o.CountColumn; }
                    if (o.PercentColumn != null) { stacked.PercentColumn = o.PercentColumn; }
                    if (o.LabelColumn != null) { stacked.LabelColumn = o.LabelColumn; }
                    foreach (ChartItem item in o.Items) {
                        if (item.LabelText != null) { stacked.Add(item.LabelText, item.Value, item.Role, item.Key); }
                        else { stacked.Add(item.Label, item.Value, item.Role, item.Key); }
                    }
                    return stacked;
                case ChartType.Line: case ChartType.Scatter:
                    var line = new LineChart(id) { Unit = o.Unit, Height = o.Height ?? 8, Minimum = o.Minimum, Maximum = o.Maximum, Scatter = o.Type == ChartType.Scatter };
                    foreach (ChartSeries source in o.Series) {
                        ChartSeries series = line.AddSeries(source.Key, source.AutomaticRole ? (Styling.ThemeToken?)null : source.Role);
                        series.Label = source.Label;
                        series.LabelText = source.LabelText;
                        foreach (double value in source.Values) { line.Add(source.Key, value); }
                    }
                    return line;
                case ChartType.Histogram:
                    if (o.Edges == null) { throw new ArgumentException("Histogram requires Edges."); }
                    var histogram = new Histogram(o.Edges, id) { Unit = o.Unit, Height = o.Height ?? 6 };
                    if (o.Role.HasValue) { histogram.Role = o.Role.Value; }
                    foreach (double value in o.Values ?? Array.Empty<double>()) { histogram.Add(value); }
                    return histogram;
                default:
                    var sparkline = new Sparkline(id) { Unit = o.Unit, Label = o.Label, Height = o.Height ?? 1, Maximum = o.Maximum, ShowLatest = o.ShowLatest };
                    if (o.Role.HasValue) { sparkline.Role = o.Role.Value; }
                    sparkline.AddRange(o.Values ?? Array.Empty<double>());
                    return sparkline;
            }
        }

        private static string DefaultId(ChartType type) {
            switch (type) {
                case ChartType.Bar: return "bars";
                case ChartType.Stacked: return "stacked";
                case ChartType.Line: case ChartType.Scatter: return "chart";
                default: return type.ToString().ToLowerInvariant();
            }
        }
    }
}
