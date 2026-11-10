using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A compact trend of recent values: <c>▁▂▄▆█▇▅</c> on Full, shades on Basic, an ASCII ramp on Ascii. With
    /// <see cref="Height"/> above 1 it becomes a small area chart. The newest value is on the right and can be shown as a
    /// number. Live operation: <c>add</c> (Value).
    /// </summary>
    public sealed class Sparkline : LiveWidget {
        private readonly List<double> _values = new List<double>();

        /// <summary>Creates a sparkline.</summary>
        /// <param name="widgetId">The live name (default <c>sparkline</c>).</param>
        public Sparkline(string widgetId = "sparkline") : base(widgetId) {
        }

        /// <summary>The values, oldest first.</summary>
        public IReadOnlyList<double> Values => _values;

        /// <summary>A caption shown on the left (markup), or <c>null</c>.</summary>
        public string? Title { get; set; }

        /// <summary>Optional point labels in value order; labelled trends show an axis and their peak.</summary>
        public IList<string> Labels { get; } = new List<string>();

        /// <summary>Rows (default 1).</summary>
        public int Height { get; set; } = 1;

        /// <summary>How many values are kept (default 1024).</summary>
        public int Capacity { get; set; } = 1024;

        /// <summary>The colour role (default <see cref="ThemeToken.Accent"/>).</summary>
        public ThemeToken Role { get; set; } = ThemeToken.Accent;

        /// <summary>Whether the latest value is shown on the right (default true).</summary>
        public bool ShowLatest { get; set; } = true;

        /// <summary>A unit appended to the latest value.</summary>
        public string? Unit { get; set; }

        /// <summary>A fixed maximum, or <c>null</c> to scale to the visible values.</summary>
        public double? Maximum { get; set; }

        /// <summary>Appends a value.</summary>
        /// <param name="value">The value.</param>
        /// <returns>This instance.</returns>
        public Sparkline Add(double value) {
            if (!NumberFormat.IsFinite(value)) {
                // NaN and infinities (a rate over zero elapsed time) are skipped rather than drawn.
                return this;
            }
            _values.Add(value);
            int capacity = Math.Max(1, Capacity);
            if (_values.Count > capacity) {
                int removed = _values.Count - capacity;
                _values.RemoveRange(0, removed);
                for (int i = 0; i < removed && Labels.Count > 0; i++) Labels.RemoveAt(0);
            }
            return this;
        }

        /// <summary>Appends several values.</summary>
        /// <param name="values">The values.</param>
        /// <returns>This instance.</returns>
        public Sparkline AddRange(IEnumerable<double> values) {
            foreach (double value in values) {
                Add(value);
            }
            return this;
        }

        /// <summary>Returns a handle that appends values through a dispatcher.</summary>
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
            int height = Math.Max(1, Height) + (Labels.Count > 0 ? 1 : 0);
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) => Render(region, context, true);

        internal void Render(Region region, RenderContext context, bool showTitle) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            int rows = Math.Min(Math.Max(1, Height), Math.Max(1, region.Height - (Labels.Count > 0 ? 1 : 0)));
            int x = 0;
            if (showTitle && !string.IsNullOrEmpty(Title)) {
                StyledLine label = context.Markup(Title!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted));
                x = region.Write(0, rows - 1, label) + 1;
            }
            double peak = _values.Count > 0 ? _values[0] : 0;
            foreach (double value in _values) peak = Math.Max(peak, value);
            string latest = Labels.Count > 0 && _values.Count > 0 ? "peak " + NumberFormat.Compact(peak) + (Unit ?? string.Empty)
                : ShowLatest && _values.Count > 0 ? NumberFormat.Compact(_values[_values.Count - 1]) + (Unit ?? string.Empty) : string.Empty;
            int latestWidth = DisplayWidth.Measure(latest);
            int width = region.Width - x - (latestWidth > 0 ? latestWidth + 1 : 0);
            if (width <= 0) {
                return;
            }
            int start = Labels.Count > 0 ? 0 : Math.Max(0, _values.Count - width);
            // A non-finite fixed maximum would make every fraction NaN; use the data maximum instead.
            bool fixedMax = Maximum.HasValue && NumberFormat.IsFinite(Maximum.Value);
            double max = fixedMax ? Maximum!.Value : 0;
            if (!fixedMax) {
                for (int i = start; i < _values.Count; i++) {
                    max = Math.Max(max, _values[i]);
                }
            }
            Style style = context.Style(Role);
            IGlyphSet glyphs = context.Glyphs;
            int offset = Labels.Count > 0 ? 0 : width - (_values.Count - start);
            int points = Labels.Count > 0 && _values.Count > 0 ? width : _values.Count - start;
            for (int at = 0; at < points; at++) {
                int i = Labels.Count > 0 ? (int)((long)at * (_values.Count - 1) / Math.Max(1, width - 1)) : start + at;
                int column = x + offset + at;
                double fraction = max <= 0 ? 0 : Math.Max(0, Math.Min(1, _values[i] / max));
                if (rows == 1) {
                    IReadOnlyList<string> ramp = glyphs.VerticalRamp;
                    int level = (int)Math.Round(fraction * (ramp.Count - 1));
                    if (level == 0 && _values[i] > 0) {
                        level = 1;
                    }
                    region.Put(column, 0, ramp[level], style);
                } else {
                    int eighths = (int)Math.Round(fraction * rows * 8);
                    if (eighths == 0 && _values[i] > 0) {
                        eighths = 1;
                    }
                    for (int r = 0; r < rows; r++) {
                        int cellEighths = Math.Max(0, Math.Min(8, eighths - (r * 8)));
                        if (cellEighths == 0) {
                            break;
                        }
                        string glyph = cellEighths == 8 ? glyphs.Get(Glyph.BlockFull) : glyphs.VerticalFill(cellEighths);

                        if (glyph.Length > 0) {
                            region.Put(column, rows - 1 - r, glyph, style);
                        }
                    }
                }
            }
            if (Labels.Count > 0 && rows < region.Height && _values.Count > start) {
                ChartAxis.Draw(region, rows, x, width, Labels, _values.Count, context);
            }
            if (latestWidth > 0) {
                region.Write(region.Width - latestWidth, rows - 1, latest, context.Style(ThemeToken.Highlight));
            }
        }
    }
}
