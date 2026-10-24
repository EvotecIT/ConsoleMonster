using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Shapes numeric samples into existing chart widgets. Bars retain individual observations; sparklines group by
    /// label, preserving arrival order. No-colour output lists values, bucket counts or every trend sample as text.
    /// </summary>
    public sealed class PipelineChart : IWidget {
        private readonly List<ChartItem> _samples = new List<ChartItem>();
        private PipelineChartType _type;
        private PipelineChartSort _sort;
        private int _top;
        private int _bins = 10;

        /// <summary>Valid samples in arrival order. Adding a repeated label does not replace an earlier observation.</summary>
        public IReadOnlyList<ChartItem> Samples => _samples.AsReadOnly();

        /// <summary>The chart presentation; defaults to horizontal bars.</summary>
        public PipelineChartType Type {
            get => _type;
            set {
                if (!Enum.IsDefined(typeof(PipelineChartType), value)) { throw new ArgumentOutOfRangeException(nameof(value)); }
                _type = value;
            }
        }

        /// <summary>Value order before Top; ties retain arrival order. Histogram observations are never sorted or limited.</summary>
        public PipelineChartSort Sort {
            get => _sort;
            set {
                if (!Enum.IsDefined(typeof(PipelineChartSort), value)) { throw new ArgumentOutOfRangeException(nameof(value)); }
                _sort = value;
            }
        }

        /// <summary>Maximum bars or trend groups to show; zero shows all. Histogram always uses every valid sample.</summary>
        public int Top {
            get => _top;
            set {
                if (value < 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                _top = value;
            }
        }

        /// <summary>Requested histogram bucket count, from 1 through 128. Integral samples use whole-number edges.</summary>
        public int Bins {
            get => _bins;
            set {
                if (value < 1 || value > 128) { throw new ArgumentOutOfRangeException(nameof(value)); }
                _bins = value;
            }
        }

        /// <summary>A unit appended to values and bucket edges.</summary>
        public string? Unit { get; set; }

        /// <summary>Adds a finite sample. Labels are literal text; a null label joins the unnamed trend.</summary>
        /// <param name="label">The sample or group label.</param>
        /// <param name="value">A finite number; NaN and infinity are rejected.</param>
        /// <returns>This chart.</returns>
        public PipelineChart Add(string? label, double value) {
            if (!NumberFormat.IsFinite(value)) { throw new ArgumentOutOfRangeException(nameof(value), "Chart samples must be finite."); }
            _samples.Add(new ChartItem(label ?? string.Empty, value));
            return this;
        }

        /// <summary>Parses a scalar number without treating booleans, dates, null or whitespace as zero.</summary>
        /// <param name="value">A numeric primitive, decimal or invariant numeric string.</param>
        /// <param name="number">The finite value when successful; zero otherwise.</param>
        /// <returns>Whether the input is a finite numeric scalar.</returns>
        public static bool TryNumber(object? value, out double number) {
            number = 0;
            if (value == null || value.GetType().IsEnum) { return false; }
            TypeCode code = System.Type.GetTypeCode(value.GetType());
            bool numeric = code >= TypeCode.SByte && code <= TypeCode.Decimal;
            double parsed;
            if (numeric) {
                parsed = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            } else if (code != TypeCode.String ||
                !double.TryParse((string)value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)) { return false; }
            if (!NumberFormat.IsFinite(parsed)) { return false; }
            number = parsed;
            return true;
        }

        /// <summary>Builds a snapshot using the existing renderers, or literal text when colour is unavailable.</summary>
        /// <param name="textOnly">Whether to return numerical text instead of chart glyphs.</param>
        /// <returns>A widget for the selected samples; an empty chart has no rows.</returns>
        public IWidget Build(bool textOnly = false) {
            if (_samples.Count == 0) { return new Rows(); }
            if (Type == PipelineChartType.Histogram) { return Distribution(textOnly); }
            if (Type == PipelineChartType.Sparkline) { return Trends(textOnly); }
            BarChart bars = new BarChart() { Signed = true, Unit = Unit };
            Rows text = new Rows();
            HashSet<string> labels = new HashSet<string>(StringComparer.Ordinal);
            int index = 0;
            foreach (ChartItem item in Select(_samples, s => s.Value)) {
                string label = item.Label;
                int duplicate = 1;
                while (!labels.Add(label)) { label = item.Label + " (" + (++duplicate).ToString(CultureInfo.InvariantCulture) + ")"; }
                bars.Add(label, item.Value, Palette(index++));
                text.Add(TextBlock.Plain(label + ": " + Value(item.Value)));
            }
            return textOnly ? (IWidget)text : bars;
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Build(!context.HasColor).Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Build(!context.HasColor).Render(region, context);

        private IEnumerable<T> Select<T>(IEnumerable<T> source, Func<T, double> value) {
            IEnumerable<T> ordered = Sort == PipelineChartSort.Ascending ? source.OrderBy(value) :
                Sort == PipelineChartSort.Descending ? source.OrderByDescending(value) : source;
            return Top > 0 ? ordered.Take(Top) : ordered;
        }

        private IWidget Trends(bool textOnly) {
            List<ChartSeries> groups = new List<ChartSeries>();
            Dictionary<string, ChartSeries> lookup = new Dictionary<string, ChartSeries>(StringComparer.Ordinal);
            foreach (ChartItem sample in _samples) {
                if (!lookup.TryGetValue(sample.Label, out ChartSeries? series)) {
                    series = new ChartSeries(sample.Label, Palette(groups.Count));
                    lookup.Add(sample.Label, series);
                    groups.Add(series);
                }
                series.Add(sample.Value, 0);
            }
            Rows rows = new Rows();
            ChartSeries[] selected = Select(groups, g => g.Values[g.Values.Count - 1]).ToArray();
            int labelWidth = selected.Max(g => DisplayWidth.Measure(g.Key));
            int latestWidth = selected.Max(g => DisplayWidth.Measure(NumberFormat.Compact(g.Values[g.Values.Count - 1]) + (Unit ?? string.Empty)));
            int desiredWidth = selected.Max(g => g.Values.Count) + latestWidth + 1;
            foreach (ChartSeries series in selected) {
                // The existing sparkline scales from zero. Signed trends use exact text rather than flattening negatives.
                if (textOnly || series.Values.Any(v => v < 0)) {
                    rows.Add(TextBlock.Plain((series.Key.Length == 0 ? string.Empty : series.Key + ": ") + string.Join(", ", series.Values.Select(Value))));
                } else {
                    Sparkline sparkline = new Sparkline() { Role = series.Role, Unit = Unit, Capacity = series.Values.Count }.AddRange(series.Values);
                    int valueWidth = DisplayWidth.Measure(NumberFormat.Compact(series.Values[series.Values.Count - 1]) + (Unit ?? string.Empty));
                    rows.Add(new TrendRow(series.Key, labelWidth, sparkline, valueWidth, desiredWidth, latestWidth + 2));
                }
            }
            return rows;
        }

        // Resolve label space at layout time so both direct charts and Build() snapshots adapt to narrow regions.
        private sealed class TrendRow : IWidget {
            private readonly string _label;
            private readonly int _labelWidth;
            private readonly Sparkline _sparkline;
            private readonly int _latestWidth;
            private readonly int _desiredWidth;
            private readonly int _minimumWidth;

            public TrendRow(string label, int labelWidth, Sparkline sparkline, int latestWidth, int desiredWidth, int minimumWidth) {
                _label = label;
                _labelWidth = labelWidth;
                _sparkline = sparkline;
                _latestWidth = latestWidth;
                _desiredWidth = desiredWidth;
                _minimumWidth = minimumWidth;
            }

            private Columns Layout(int width) {
                int chartWidth = _sparkline.Values.Count + _latestWidth + 1;
                int reserved = Math.Min(_desiredWidth, Math.Max(_minimumWidth, width / 2));
                int labelWidth = Math.Min(_labelWidth, Math.Max(0, width - reserved - 1));
                Columns columns = new Columns() { CollapseBelow = 0 };
                // An omitted label must not reserve a track or its inter-column gap.
                if (labelWidth > 0) {
                    columns.Add(new TextBlock(_label) { IsMarkup = false, Role = ThemeToken.Muted, Overflow = TextOverflow.Ellipsis }, LayoutSize.Fixed(labelWidth));
                }
                return columns.Add(_sparkline, LayoutSize.Fixed(chartWidth));
            }

            public Size Measure(Constraints constraints, RenderContext context) => Layout(constraints.MaxWidth).Measure(constraints, context);

            public void Render(Region region, RenderContext context) => Layout(region.Width).Render(region, context);
        }

        private IWidget Distribution(bool textOnly) {
            double[] edges = Edges();
            Histogram histogram = new Histogram(edges) { Unit = Unit, EdgeFormat = Value };
            foreach (ChartItem sample in _samples) { histogram.Add(sample.Value); }
            if (!textOnly) { return histogram; }
            Rows rows = new Rows();
            for (int i = 0; i < histogram.Counts.Count; i++) {
                rows.Add(TextBlock.Plain("[" + Value(edges[i]) + ", " + Value(edges[i + 1]) +
                    (i == histogram.Counts.Count - 1 ? "]: " : "): ") + histogram.Counts[i].ToString(CultureInfo.InvariantCulture)));
            }
            return rows;
        }

        private double[] Edges() {
            double min = _samples.Min(s => s.Value);
            double max = _samples.Max(s => s.Value);
            bool integral = _samples.All(s => s.Value == Math.Truncate(s.Value));
            if (min == max) {
                // Avoid an overflowing or unrepresentable value + 1 at the ends of the double range.
                double delta = Math.Max(1, Math.Abs(min) * 0.5);
                if (integral) { delta = Math.Ceiling(delta); }
                return min > 0 ? new[] { min - delta, min } : new[] { min, min + delta };
            }
            List<double> edges = new List<double>() { min };
            double step = integral ? Math.Ceiling(max / Bins - min / Bins) : 0;
            for (int i = 1; i < Bins; i++) {
                double fraction = (double)i / Bins;
                double edge = integral && NumberFormat.IsFinite(step) ? min + step * i : min * (1 - fraction) + max * fraction;
                if (!NumberFormat.IsFinite(edge)) { edge = min * (1 - fraction) + max * fraction; }
                if (NumberFormat.IsFinite(edge) && edge > edges[edges.Count - 1] && edge < max) { edges.Add(edge); }
            }
            edges.Add(max);
            return edges.ToArray();
        }

        private string Value(double value) => value.ToString("R", CultureInfo.InvariantCulture) + (Unit ?? string.Empty);

        private static ThemeToken Palette(int index) => (ThemeToken)((int)ThemeToken.Series1 + index % 6);
    }
}
