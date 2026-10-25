using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Vertical bars for a distribution: values are counted into buckets defined by edges, bars grow with eighth-block
    /// precision, the largest count labels the top and bucket labels sit under the bars. Live operation: <c>add</c> (Value).
    /// </summary>
    public sealed class Histogram : LiveWidget {
        private readonly double[] _edges;
        private readonly int[] _counts;

        /// <summary>Creates a histogram.</summary>
        /// <param name="edges">Ascending bucket edges; values below the first edge go to the first bucket, values at or above the last edge to the last.</param>
        /// <param name="widgetId">The live name (default <c>histogram</c>).</param>
        public Histogram(IReadOnlyList<double> edges, string widgetId = "histogram") : base(widgetId) {
            if (edges == null || edges.Count < 2) {
                throw new ArgumentException("At least two edges are required.", nameof(edges));
            }
            _edges = new double[edges.Count];
            for (int i = 0; i < edges.Count; i++) {
                if (!NumberFormat.IsFinite(edges[i]) || i > 0 && edges[i] <= edges[i - 1]) {
                    throw new ArgumentException("Edges must be finite and strictly increasing.", nameof(edges));
                }
                _edges[i] = edges[i];
            }
            _counts = new int[edges.Count - 1];
        }

        /// <summary>Bucket counts.</summary>
        public IReadOnlyList<int> Counts => _counts;

        /// <summary>Rows for the bars (default 6).</summary>
        public int Height { get; set; } = 6;

        /// <summary>The bar colour role (default <see cref="ThemeToken.Series1"/>).</summary>
        public ThemeToken Role { get; set; } = ThemeToken.Series1;

        /// <summary>A unit for bucket labels (for example <c>s</c>).</summary>
        public string? Unit { get; set; }

        /// <summary>Formats the bucket edge labels (for example milliseconds as 250ms and 1s), or null for the compact number and <see cref="Unit"/>.</summary>
        public Func<double, string>? EdgeFormat { get; set; }

        /// <summary>Counts a value.</summary>
        /// <param name="value">The value.</param>
        /// <returns>This instance.</returns>
        public Histogram Add(double value) {
            if (!NumberFormat.IsFinite(value)) {
                return this;
            }
            int bucket = _counts.Length - 1;
            for (int i = 1; i < _edges.Length; i++) {
                if (value < _edges[i]) {
                    bucket = i - 1;
                    break;
                }
            }
            _counts[bucket]++;
            return this;
        }

        /// <summary>Returns a handle that adds values through a dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public SparklineHandle Handle(ILiveDispatcher dispatcher) => new SparklineHandle(dispatcher, WidgetId);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation == "add") {
                Add(liveEvent.Value);
            }
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int height = Math.Max(2, Height) + 1;
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Height < 2 || region.Width < 4) {
                return;
            }
            int barRows = region.Height - 1;
            int max = 0;
            foreach (int count in _counts) {
                max = Math.Max(max, count);
            }
            string maxLabel = max.ToString(CultureInfo.InvariantCulture);
            int axisWidth = DisplayWidth.Measure(maxLabel) + 1;
            Style muted = context.Style(ThemeToken.Muted);
            Style axis = context.Style(ThemeToken.Border);
            region.Write(0, 0, maxLabel.PadLeft(axisWidth - 1), muted);
            region.Write(0, barRows - 1, "0".PadLeft(axisWidth - 1), muted);
            region.VerticalLine(axisWidth - 1, 0, barRows, context.Glyphs.Get(Glyph.Separator), axis);
            int plotX = axisWidth;
            int plotWidth = region.Width - plotX;
            int buckets = _counts.Length;
            int minimumSlot = Math.Max(1, plotWidth / buckets);
            Style bar = context.Style(Role);
            IGlyphSet glyphs = context.Glyphs;
            for (int b = 0; b < buckets; b++) {
                // Spread the plot width over the buckets so the bars fill the panel.
                int x = plotX + (int)((long)b * plotWidth / buckets);
                int next = plotX + (int)((long)(b + 1) * plotWidth / buckets);
                int barWidth = Math.Max(1, next - x - (minimumSlot > 2 ? 1 : 0));
                if (x + barWidth > region.Width) {
                    break;
                }
                int eighths = max == 0 ? 0 : (int)Math.Round((double)_counts[b] / max * barRows * 8);
                if (eighths == 0 && _counts[b] > 0) {
                    eighths = 1;
                }
                for (int r = 0; r < barRows; r++) {
                    int cell = Math.Max(0, Math.Min(8, eighths - (r * 8)));
                    if (cell == 0) {
                        break;
                    }
                    string glyph = cell == 8 ? glyphs.Get(Glyph.BlockFull) : glyphs.VerticalFill(cell);
                    if (glyph.Length == 0) {
                        continue;
                    }
                    region.HorizontalLine(x, barRows - 1 - r, barWidth, glyph, bar);
                }
            }
            DrawEdgeLabels(region, plotX, plotWidth, barRows, muted);
        }

        /// <summary>
        /// Labels the bucket edges: the left edge of every bar plus the right end of the last one (buckets + 1 labels).
        /// The first and last edges are always shown (the last when it does not run into the first). When the bars are too
        /// narrow for every label, every Nth inner edge is labelled, with the smallest N that keeps a space between labels,
        /// so the labels thin out evenly; an inner label that would touch the last one is left out.
        /// </summary>
        private void DrawEdgeLabels(Region region, int plotX, int plotWidth, int row, Style style) {
            int buckets = _counts.Length;
            string[] labels = new string[buckets];
            int[] starts = new int[buckets];
            int[] widths = new int[buckets];
            for (int b = 0; b < buckets; b++) {
                labels[b] = EdgeLabel(_edges[b]);
                starts[b] = plotX + (int)((long)b * plotWidth / buckets);
                widths[b] = DisplayWidth.Measure(labels[b]);
            }
            string last = EdgeLabel(_edges[buckets]);
            int lastStart = Math.Max(plotX, region.Width - DisplayWidth.Measure(last));
            int step = LabelStep(starts, widths);
            int labelEnd = 0;
            for (int b = 0; b < buckets; b += step) {
                if (b > 0 && starts[b] + widths[b] + 1 > lastStart) {
                    break;
                }
                labelEnd = region.Write(starts[b], row, labels[b], style) + 1;
            }
            if (lastStart >= labelEnd) {
                region.Write(lastStart, row, last, style);
            }
        }

        /// <summary>The smallest step between labelled edges at which no label touches the next one (one free cell between).</summary>
        /// <param name="starts">Where each edge label starts.</param>
        /// <param name="widths">The width of each edge label.</param>
        /// <returns>The step, at least 1.</returns>
        internal static int LabelStep(IReadOnlyList<int> starts, IReadOnlyList<int> widths) {
            int count = starts.Count;
            for (int step = 1; step < count; step++) {
                bool fits = true;
                for (int b = step; b < count && fits; b += step) {
                    fits = starts[b] >= starts[b - step] + widths[b - step] + 1;
                }
                if (fits) {
                    return step;
                }
            }
            return Math.Max(1, count);
        }

        private string EdgeLabel(double value) => EdgeFormat != null ? EdgeFormat(value) : NumberFormat.Compact(value) + (Unit ?? string.Empty);
    }
}
