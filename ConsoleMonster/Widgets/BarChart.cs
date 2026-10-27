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
    /// sets its own role or a value crosses <see cref="Warning"/> or <see cref="Critical"/>, so the chart does not shout. Live operations (Key = label): <c>set</c> (Value), <c>add</c> (Value).
    /// </summary>
    public sealed class BarChart : LiveWidget {
        private readonly List<ChartItem> _items = new List<ChartItem>();

        /// <summary>Creates a bar chart.</summary>
        /// <param name="widgetId">The live name (default <c>bars</c>).</param>
        public BarChart(string widgetId = "bars") : base(widgetId) {
        }

        /// <summary>The items.</summary>
        public IReadOnlyList<ChartItem> Items => _items;

        /// <summary>
        /// The value of a full bar, or <c>null</c> for the largest item. Non-finite maxima use the largest finite item;
        /// a negative maximum is treated as zero, so signed bars always include their zero baseline.
        /// </summary>
        public double? Maximum { get; set; }

        /// <summary>The default bar colour (default <see cref="ThemeToken.Series1"/>).</summary>
        public ThemeToken Role { get; set; } = ThemeToken.Series1;

        /// <summary>Whether values are shown after the bars (default true).</summary>
        public bool ShowValues { get; set; } = true;

        /// <summary>Draws negative values to the left of a shared zero baseline; default false preserves zero-based bars.</summary>
        public bool Signed { get; set; }

        /// <summary>The bar shape: <see cref="BarShape.Block"/> (default, solid three-quarter-height bars that keep a gap between rows) or <see cref="BarShape.Line"/>.</summary>
        public BarShape Shape { get; set; } = BarShape.Block;

        /// <summary>
        /// Layout of the value column on the right (default right-aligned). Its numeric width never shrinks during a
        /// session, so the bars keep their scale when a value grows from 9 to 10. Missing explanations use only the
        /// current frame's available space and do not reserve width after a measurement arrives.
        /// </summary>
        public ColumnFormat ValueColumn {
            get => _valueColumn.Format;
            set => _valueColumn.Format = value ?? throw new ArgumentNullException(nameof(value));
        }

        private readonly StatColumn _valueColumn = new StatColumn(new ColumnFormat());

        /// <summary>A unit appended to values (for example <c>ms</c>).</summary>
        public string? Unit { get; set; }

        /// <summary>A shared plain text label below the bars, for example elapsed time since an observation.</summary>
        public string? AxisLabel { get; set; }

        /// <summary>The plain text shown for non-finite values; null retains the tier-appropriate dash.</summary>
        public string? MissingText { get; set; }

        /// <summary>Adds an item without a measurement, preserving its status and explanatory text.</summary>
        /// <param name="label">The plain text label.</param>
        /// <param name="text">The missing-measurement explanation, or null for the chart default.</param>
        /// <param name="status">An optional status.</param>
        /// <param name="key">Stable identity, or null to use the label.</param>
        /// <returns>This chart.</returns>
        public BarChart AddMissing(string label, string? text = null, StatusKind? status = null, string? key = null) {
            Add(label, double.NaN, null, key);
            ChartItem item = Find(key ?? label)!;
            item.MissingText = text;
            item.Status = status;
            return this;
        }

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
        /// <param name="role">An optional colour role.</param>
        /// <param name="key">Stable item identity, or null to use the label.</param>
        /// <returns>This instance.</returns>
        public BarChart Add(Text.RichText label, double value, ThemeToken? role = null, string? key = null) {
            if (label == null) {
                throw new ArgumentNullException(nameof(label));
            }
            Add(label.PlainText, value, role, key);
            Find(key ?? label.PlainText)!.LabelText = label;
            return this;
        }

        /// <summary>Adds or updates an item.</summary>
        /// <param name="label">The label.</param>
        /// <param name="value">The value.</param>
        /// <param name="role">An optional colour role.</param>
        /// <param name="key">Stable item identity, or null to use the label.</param>
        /// <returns>This instance.</returns>
        public BarChart Add(string label, double value, ThemeToken? role = null, string? key = null) {
            ChartItem? existing = Find(key ?? label);
            if (existing != null) {
                existing.Value = value;
                if (role.HasValue) {
                    existing.Role = role;
                }
            } else {
                _items.Add(new ChartItem(label, value, role, key));
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
            int rows = _items.Count + (!string.IsNullOrEmpty(AxisLabel) ? 1 : 0);
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? rows : Math.Min(rows, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (_items.Count == 0) {
                return;
            }
            int labelWidth = 0;
            int valueWidth = 0;
            int numericWidth = 0;
            bool fixedMax = Maximum.HasValue && NumberFormat.IsFinite(Maximum.Value);
            double max = fixedMax ? Math.Max(0, Maximum!.Value) : 0;
            double min = 0;
            foreach (ChartItem item in _items) {
                labelWidth = Math.Max(labelWidth, DisplayWidth.Measure(item.Label));
                valueWidth = Math.Max(valueWidth, DisplayWidth.Measure(ValueText(item, context)));
                if (NumberFormat.IsFinite(item.Value)) numericWidth = Math.Max(numericWidth, DisplayWidth.Measure(ValueText(item, context)));
                if (!fixedMax && NumberFormat.IsFinite(item.Value)) {
                    max = Math.Max(max, item.Value);
                }
                if (Signed && NumberFormat.IsFinite(item.Value)) { min = Math.Min(min, item.Value); }
            }
            int barReserve = 4;
            if (ShowValues) {
                // Missing explanations are transient text, not part of the remembered numeric column width.
                valueWidth = Math.Max(valueWidth, _valueColumn.Resolve(numericWidth, DisplayWidth.Measure(Unit ?? string.Empty), fixedMax ? DisplayWidth.Measure(FormatValue(Maximum!.Value)) : 0));
                int labelReserve = Math.Min(labelWidth, Math.Max(1, region.Width / 3));
                int valueReserve = Math.Min(valueWidth, DisplayWidth.Measure(context.Ellipsis) + 2);
                barReserve = Math.Min(barReserve, Math.Max(0, region.Width - labelReserve - valueReserve - 2));
                valueWidth = Math.Min(valueWidth, Math.Max(0, region.Width - labelReserve - barReserve - 2));
            }
            // Leave a small useful bar, rather than reserving two thirds of every row for empty space.
            int suffix = ShowValues ? valueWidth + 2 : 1;
            labelWidth = Math.Min(labelWidth, Math.Max(0, region.Width - suffix - barReserve));
            int barX = labelWidth + 1;
            int barWidth = Math.Max(0, region.Width - barX - (ShowValues ? valueWidth + 1 : 0));
            Style muted = context.Style(ThemeToken.Muted);
            for (int i = 0; i < _items.Count && i < region.Height; i++) {
                ChartItem item = _items[i];
                region.Write(0, i, TextWrapper.Truncate(RichLines.Line(item.LabelText, item.Label, context.Style(ThemeToken.Text), context), labelWidth, context.Ellipsis));
                double fraction = max <= 0 || !NumberFormat.IsFinite(item.Value) ? 0 : item.Value / max;
                StatusKind? reached = item.Status ?? (item.Role.HasValue ? null : Threshold(item.Value));
                ThemeToken role = item.Role ?? (reached.HasValue ? reached.Value.ToToken() : Role);
                if (min < 0 && barWidth > 1) {
                    double scale = Math.Max(Math.Abs(min), Math.Abs(max));
                    double span = max / scale - min / scale;
                    int zero = (int)Math.Round((-min / scale) / span * (barWidth - 1));
                    int length = NumberFormat.IsFinite(item.Value) ? Math.Max(0, (int)Math.Round(Math.Min(1, Math.Abs(item.Value / scale) / span) * (barWidth - 1))) : 0;
                    length = Math.Min(length, item.Value < 0 ? zero : barWidth - zero - 1);
                    int start = item.Value < 0 ? barX + zero - length : barX + zero + 1;
                    BarDrawer.Draw(region, start, i, length, 1, context.Style(role), context, false, Shape);
                    region.Write(barX + zero, i, "|", context.Style(ThemeToken.Border));
                } else {
                    BarDrawer.Draw(region, barX, i, barWidth, fraction, context.Style(role), context, false, Shape);
                }
                if (ShowValues) {
                    StatColumn.Write(region, region.Width - valueWidth, i, valueWidth, ValueText(item, context), reached.HasValue ? context.Style(reached.Value) : muted, ValueColumn.Alignment, context.Ellipsis);
                }
            }
            if (!string.IsNullOrEmpty(AxisLabel)) region.Write(0, _items.Count, TextWrapper.Truncate(AxisLabel!, region.Width, context.Ellipsis), muted);
        }

        private string FormatValue(double value) => NumberFormat.Compact(value) + (Unit ?? string.Empty);

        // The value with a status symbol in front when it crossed a threshold and colour cannot show it (never colour alone).
        private string ValueText(ChartItem item, RenderContext context) {
            string text = NumberFormat.IsFinite(item.Value) ? FormatValue(item.Value, context) : item.MissingText ?? MissingText ?? NumberFormat.Missing(context.Glyphs.Tier);
            StatusKind? reached = item.Status ?? (item.Role.HasValue ? null : Threshold(item.Value));
            return reached.HasValue && (item.Status.HasValue || !context.HasColor) ? context.Glyphs.StatusSymbol(reached.Value) + " " + text : text;
        }

        // NaN and infinite values draw no bar and show a dash instead of "InfinityM".
        private string FormatValue(double value, RenderContext context) => NumberFormat.IsFinite(value) ? FormatValue(value) : NumberFormat.Missing(context.Glyphs.Tier);

        private ChartItem? Find(string label) {
            foreach (ChartItem item in _items) {
                if (string.Equals(item.Key, label, StringComparison.Ordinal)) {
                    return item;
                }
            }
            return null;
        }
    }
}
