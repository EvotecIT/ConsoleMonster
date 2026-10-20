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
    /// Horizontal bars with labels and values, sub-cell precise per glyph tier. All bars share one colour unless an item
    /// sets its own token or a value crosses <see cref="Warning"/> or <see cref="Critical"/>, so the chart does not shout. Live operations (Key = label): <c>set</c> (Value), <c>add</c> (Value).
    /// </summary>
    public sealed class BarChart : LiveWidget {
        private readonly List<ChartItem> _items = new List<ChartItem>();

        /// <summary>Creates a bar chart.</summary>
        /// <param name="name">The live name (default <c>bars</c>).</param>
        public BarChart(string name = "bars") : base(name) {
        }

        /// <summary>The items.</summary>
        public IReadOnlyList<ChartItem> Items => _items;

        /// <summary>The value of a full bar, or <c>null</c> for the largest item.</summary>
        public double? Max { get; set; }

        /// <summary>The default bar colour (default <see cref="ThemeToken.Series1"/>).</summary>
        public ThemeToken BarToken { get; set; } = ThemeToken.Series1;

        /// <summary>Whether values are shown after the bars (default true).</summary>
        public bool ShowValues { get; set; } = true;

        /// <summary>The bar shape: <see cref="BarShape.Block"/> (default, solid three-quarter-height bars that keep a gap between rows) or <see cref="BarShape.Line"/>.</summary>
        public BarShape Shape { get; set; } = BarShape.Block;

        /// <summary>
        /// Layout of the value column on the right (default right-aligned). Its width never shrinks during a session, so
        /// the bars keep their scale when a value grows from 9 to 10.
        /// </summary>
        public ColumnFormat ValueColumn {
            get => _valueColumn.Format;
            set => _valueColumn.Format = value ?? throw new ArgumentNullException(nameof(value));
        }

        private readonly StatColumn _valueColumn = new StatColumn(new ColumnFormat());

        /// <summary>A unit appended to values (for example <c>ms</c>).</summary>
        public string? Unit { get; set; }

        /// <summary>
        /// The value from which a bar is a warning (warning colour), or null. With <see cref="Critical"/> below it, lower
        /// values are worse (free space); otherwise higher values are (disk use). An item's own colour wins.
        /// </summary>
        public double? Warning { get; set; }

        /// <summary>The value from which a bar is critical (fail colour), or null. See <see cref="Warning"/>.</summary>
        public double? Critical { get; set; }

        /// <summary>The status a value reaches against <see cref="Warning"/> and <see cref="Critical"/>, or null below both.</summary>
        /// <param name="value">The value.</param>
        /// <returns>Fail, Warn or null.</returns>
        public StatusKind? Threshold(double value) {
            if (!NumberFormat.IsFinite(value)) {
                return null;
            }
            bool lowerIsWorse = Warning.HasValue && Critical.HasValue && Critical.Value < Warning.Value;
            if (Critical.HasValue && (lowerIsWorse ? value <= Critical.Value : value >= Critical.Value)) {
                return StatusKind.Fail;
            }
            if (Warning.HasValue && (lowerIsWorse ? value <= Warning.Value : value >= Warning.Value)) {
                return StatusKind.Warn;
            }
            return null;
        }

        /// <summary>Adds or updates an item with a multicolour label (its plain text is the key).</summary>
        /// <param name="label">The rich label.</param>
        /// <param name="value">The value.</param>
        /// <param name="token">An optional colour token.</param>
        /// <returns>This instance.</returns>
        public BarChart Add(Text.RichText label, double value, ThemeToken? token = null) {
            if (label == null) {
                throw new ArgumentNullException(nameof(label));
            }
            Add(label.PlainText, value, token);
            Find(label.PlainText)!.LabelText = label;
            return this;
        }

        /// <summary>Adds or updates an item.</summary>
        /// <param name="label">The label.</param>
        /// <param name="value">The value.</param>
        /// <param name="token">An optional colour token.</param>
        /// <returns>This instance.</returns>
        public BarChart Add(string label, double value, ThemeToken? token = null) {
            ChartItem? existing = Find(label);
            if (existing != null) {
                existing.Value = value;
                if (token.HasValue) {
                    existing.Token = token;
                }
            } else {
                _items.Add(new ChartItem(label, value, token));
            }
            return this;
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Key == null) {
                return;
            }
            ChartItem? item = Find(liveEvent.Key);
            if (liveEvent.Operation == "set") {
                Add(liveEvent.Key, liveEvent.Value);
            } else if (liveEvent.Operation == "add") {
                Add(liveEvent.Key, (item?.Value ?? 0) + (liveEvent.Value == 0 ? 1 : liveEvent.Value));
            }
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int rows = _items.Count;
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? rows : Math.Min(rows, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (_items.Count == 0) {
                return;
            }
            int labelWidth = 0;
            int valueWidth = 0;
            double max = Max ?? 0;
            foreach (ChartItem item in _items) {
                labelWidth = Math.Max(labelWidth, DisplayWidth.Measure(item.Label));
                valueWidth = Math.Max(valueWidth, DisplayWidth.Measure(ValueText(item, context)));
                if (!Max.HasValue && NumberFormat.IsFinite(item.Value)) {
                    max = Math.Max(max, item.Value);
                }
            }
            labelWidth = Math.Min(labelWidth, Math.Max(4, region.Width / 3));
            if (ShowValues) {
                valueWidth = _valueColumn.Resolve(valueWidth, DisplayWidth.Measure(Unit ?? string.Empty), Max.HasValue ? DisplayWidth.Measure(FormatValue(Max.Value)) : 0);
            }
            int barX = labelWidth + 1;
            int barWidth = region.Width - barX - (ShowValues ? valueWidth + 1 : 0);
            Style muted = context.Style(ThemeToken.Muted);
            for (int i = 0; i < _items.Count && i < region.Height; i++) {
                ChartItem item = _items[i];
                region.Write(0, i, TextWrapper.Truncate(RichLines.Line(item.LabelText, item.Label, context.Style(ThemeToken.Text), context), labelWidth, context.Ellipsis));
                double fraction = max <= 0 || !NumberFormat.IsFinite(item.Value) ? 0 : item.Value / max;
                StatusKind? reached = item.Token.HasValue ? null : Threshold(item.Value);
                ThemeToken token = item.Token ?? (reached.HasValue ? reached.Value.ToToken() : BarToken);
                BarDrawer.Draw(region, barX, i, barWidth, fraction, context.Style(token), context, false, Shape);
                if (ShowValues) {
                    StatColumn.Write(region, region.Width - valueWidth, i, valueWidth, ValueText(item, context), reached.HasValue ? context.Style(reached.Value) : muted, ValueColumn.Alignment, context.Ellipsis);
                }
            }
        }

        private string FormatValue(double value) => NumberFormat.Compact(value) + (Unit ?? string.Empty);

        // The value with a status symbol in front when it crossed a threshold and colour cannot show it (never colour alone).
        private string ValueText(ChartItem item, RenderContext context) {
            string text = FormatValue(item.Value, context);
            StatusKind? reached = item.Token.HasValue ? null : Threshold(item.Value);
            return reached.HasValue && !context.HasColor ? context.Glyphs.StatusSymbol(reached.Value) + " " + text : text;
        }

        // NaN and infinite values draw no bar and show a dash instead of "InfinityM".
        private string FormatValue(double value, RenderContext context) => NumberFormat.IsFinite(value) ? FormatValue(value) : NumberFormat.Missing(context.Glyphs.Tier);

        private ChartItem? Find(string label) {
            foreach (ChartItem item in _items) {
                if (string.Equals(item.Label, label, StringComparison.Ordinal)) {
                    return item;
                }
            }
            return null;
        }
    }
}
