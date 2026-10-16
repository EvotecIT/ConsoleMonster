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
        /// <param name="name">The live name (default <c>sparkline</c>).</param>
        public Sparkline(string name = "sparkline") : base(name) {
        }

        /// <summary>The values, oldest first.</summary>
        public IReadOnlyList<double> Values => _values;

        /// <summary>A label shown on the left (markup), or <c>null</c>.</summary>
        public string? Label { get; set; }

        /// <summary>Rows (default 1).</summary>
        public int Height { get; set; } = 1;

        /// <summary>How many values are kept (default 1024).</summary>
        public int Capacity { get; set; } = 1024;

        /// <summary>The colour token (default <see cref="ThemeToken.Accent"/>).</summary>
        public ThemeToken Token { get; set; } = ThemeToken.Accent;

        /// <summary>Whether the latest value is shown on the right (default true).</summary>
        public bool ShowLatest { get; set; } = true;

        /// <summary>A unit appended to the latest value.</summary>
        public string? Unit { get; set; }

        /// <summary>A fixed maximum, or <c>null</c> to scale to the visible values.</summary>
        public double? Max { get; set; }

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
                _values.RemoveRange(0, _values.Count - capacity);
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
        public SparklineHandle Handle(ILiveDispatcher dispatcher) => new SparklineHandle(dispatcher, Name);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation == "add") {
                Add(liveEvent.Value);
            }
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int height = Math.Max(1, Height);
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            int rows = Math.Min(Math.Max(1, Height), region.Height);
            int x = 0;
            if (!string.IsNullOrEmpty(Label)) {
                StyledLine label = context.Markup(Label!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted));
                x = region.Write(0, rows - 1, label) + 1;
            }
            string latest = ShowLatest && _values.Count > 0 ? NumberFormat.Compact(_values[_values.Count - 1]) + (Unit ?? string.Empty) : string.Empty;
            int latestWidth = DisplayWidth.Measure(latest);
            int width = region.Width - x - (latestWidth > 0 ? latestWidth + 1 : 0);
            if (width <= 0) {
                return;
            }
            int start = Math.Max(0, _values.Count - width);
            // A non-finite fixed maximum would make every fraction NaN; use the data maximum instead.
            bool fixedMax = Max.HasValue && NumberFormat.IsFinite(Max.Value);
            double max = fixedMax ? Max!.Value : 0;
            if (!fixedMax) {
                for (int i = start; i < _values.Count; i++) {
                    max = Math.Max(max, _values[i]);
                }
            }
            Style style = context.Style(Token);
            IGlyphSet glyphs = context.Glyphs;
            int offset = width - (_values.Count - start);
            for (int i = start; i < _values.Count; i++) {
                int column = x + offset + (i - start);
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
            if (latestWidth > 0) {
                region.Write(region.Width - latestWidth, rows - 1, latest, context.Style(ThemeToken.Highlight));
            }
        }
    }
}
