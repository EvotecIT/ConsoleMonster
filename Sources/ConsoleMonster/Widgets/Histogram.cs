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
        /// <param name="name">The live name (default <c>histogram</c>).</param>
        public Histogram(IReadOnlyList<double> edges, string name = "histogram") : base(name) {
            if (edges == null || edges.Count < 2) {
                throw new ArgumentException("At least two edges are required.", nameof(edges));
            }
            _edges = new double[edges.Count];
            for (int i = 0; i < edges.Count; i++) {
                _edges[i] = edges[i];
            }
            _counts = new int[edges.Count - 1];
        }

        /// <summary>Bucket counts.</summary>
        public IReadOnlyList<int> Counts => _counts;

        /// <summary>Rows for the bars (default 6).</summary>
        public int Height { get; set; } = 6;

        /// <summary>The bar colour token (default <see cref="ThemeToken.Series1"/>).</summary>
        public ThemeToken Token { get; set; } = ThemeToken.Series1;

        /// <summary>A unit for bucket labels (for example <c>s</c>).</summary>
        public string? Unit { get; set; }

        /// <summary>Counts a value.</summary>
        /// <param name="value">The value.</param>
        /// <returns>This instance.</returns>
        public Histogram Add(double value) {
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
        public SparklineHandle Handle(ILiveDispatcher dispatcher) => new SparklineHandle(dispatcher, Name);

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
            Style bar = context.Style(Token);
            IGlyphSet glyphs = context.Glyphs;
            int labelEnd = 0;
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
                string label = EdgeLabel(_edges[b]);
                if (x >= labelEnd) {
                    labelEnd = region.Write(x, barRows, label, muted) + 1;
                }
            }
        }

        private string EdgeLabel(double value) => NumberFormat.Compact(value) + (Unit ?? string.Empty);
    }
}
