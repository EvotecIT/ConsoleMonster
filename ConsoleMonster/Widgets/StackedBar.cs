using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// One bar split into proportional segments with a legend (label, value, share). Segments labelled with a status word
    /// (Passed, Failed, Warnings, Skipped...) take that status colour unless a role is given. Without colour, segments use distinct
    /// fill glyphs so the split stays readable. Live operations (Key = segment): <c>set</c> (Value), <c>add</c> (Value).
    /// </summary>
    public sealed class StackedBar : LiveWidget {
        private static readonly string[] AsciiFills = { "#", "=", "+", "-", ".", ":" };
        private const string AdditionalFills = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!?%&*/\\|~_<>";
        private readonly List<ChartItem> _segments = new List<ChartItem>();

        /// <summary>Creates a stacked bar.</summary>
        /// <param name="widgetId">The live name (default <c>stacked</c>).</param>
        public StackedBar(string widgetId = "stacked") : base(widgetId) {
        }

        /// <summary>The segments.</summary>
        public IReadOnlyList<ChartItem> Segments => _segments;

        /// <summary>Whether the legend line is shown (default true).</summary>
        public bool ShowLegend { get; set; } = true;

        /// <summary>
        /// The most legend columns per row, or 0 (default) to fit as many as the width allows. Entries fill rows left to
        /// right and every row shares one grid, so labels, counts and percentages line up.
        /// </summary>
        public int MaxLegendColumns { get; set; }

        /// <summary>Layout of the legend label column (default left-aligned).</summary>
        public ColumnFormat LabelColumn {
            get => _labelColumn.Format;
            set => _labelColumn.Format = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Layout of the legend count column (default right-aligned; reserves the digits of the overall total).</summary>
        public ColumnFormat CountColumn {
            get => _countColumn.Format;
            set => _countColumn.Format = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Layout of the legend percentage column (default right-aligned, four cells).</summary>
        public ColumnFormat PercentColumn {
            get => _percentColumn.Format;
            set => _percentColumn.Format = value ?? throw new ArgumentNullException(nameof(value));
        }

        private readonly StatColumn _labelColumn = new StatColumn(new ColumnFormat(HorizontalAlignment.Left));
        private readonly StatColumn _countColumn = new StatColumn(new ColumnFormat());
        private readonly StatColumn _percentColumn = new StatColumn(new ColumnFormat(HorizontalAlignment.Right, 4));

        /// <summary>Adds or updates a segment with a multicolour legend label (its plain text is the key).</summary>
        /// <param name="label">The rich label.</param>
        /// <param name="value">The value.</param>
        /// <param name="role">The colour role, or <c>null</c> for the next series colour.</param>
        /// <param name="key">Stable item identity, or null to use the label.</param>
        /// <returns>This instance.</returns>
        public StackedBar Add(Text.RichText label, double value, ThemeToken? role = null, string? key = null) {
            if (label == null) {
                throw new ArgumentNullException(nameof(label));
            }
            Add(label.PlainText, value, role, key);
            foreach (ChartItem segment in _segments) {
                if (string.Equals(segment.Key, key ?? label.PlainText, StringComparison.Ordinal)) {
                    segment.LabelText = label;
                }
            }
            return this;
        }

        /// <summary>Adds or updates a segment.</summary>
        /// <param name="label">The label.</param>
        /// <param name="value">The value.</param>
        /// <param name="role">The colour role, or <c>null</c> for the next series colour.</param>
        /// <param name="key">Stable item identity, or null to use the label.</param>
        /// <returns>This instance.</returns>
        public StackedBar Add(string label, double value, ThemeToken? role = null, string? key = null) {
            foreach (ChartItem segment in _segments) {
                if (string.Equals(segment.Key, key ?? label, StringComparison.Ordinal)) {
                    segment.Value = value;
                    if (role.HasValue) {
                        segment.Role = role;
                    }
                    return this;
                }
            }
            ThemeToken automatic = StatusKinds.TryParse(label, out StatusKind status) ? status.ToToken() : (ThemeToken)((int)ThemeToken.Series1 + (_segments.Count % 6));
            _segments.Add(new ChartItem(label, value, role ?? automatic, key));
            return this;
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Key == null) {
                return;
            }
            if (liveEvent.Operation == "set") {
                Add(liveEvent.Key, liveEvent.Value);
            } else if (liveEvent.Operation == "add") {
                double current = 0;
                foreach (ChartItem segment in _segments) {
                    if (segment.Key == liveEvent.Key) {
                        current = segment.Value;
                    }
                }
                Add(liveEvent.Key, current + (liveEvent.Value == 0 ? 1 : liveEvent.Value));
            }
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int lines = 1 + (ShowLegend ? LegendLines(constraints.MaxWidth, context).Count : 0);
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? lines : Math.Min(lines, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Height <= 0 || region.Width <= 0) {
                return;
            }
            int[] widths = SegmentWidths(region.Width);
            int x = 0;
            for (int i = 0; i < _segments.Count; i++) {
                string fill = GetFillMarker(i, context);
                region.HorizontalLine(x, 0, widths[i], fill, context.Style(_segments[i].Role ?? ThemeToken.Series1));
                x += widths[i];
            }
            if (x < region.Width) {
                region.HorizontalLine(x, 0, region.Width - x, context.Glyphs.Get(Glyph.BarTrack), context.Style(ThemeToken.Subtle));
            }
            if (!ShowLegend) {
                return;
            }
            List<StyledLine> legend = LegendLines(region.Width, context);
            for (int i = 0; i < legend.Count && i + 1 < region.Height; i++) {
                region.Write(0, i + 1, legend[i]);
            }
        }

        /// <summary>Gets the exact unpadded fill glyph used for a segment in the supplied rendering context.
        /// Custom legends can use this marker when <see cref="RenderContext.HasColor"/> is false to identify
        /// the corresponding bar fill. The built-in bar and legend use this same mapping.</summary>
        /// <param name="segmentIndex">Zero-based index in <see cref="Segments"/>, including zero-valued segments.</param>
        /// <param name="context">The rendering context, including its glyph set and color capability.</param>
        /// <returns>The segment's fill glyph, with no surrounding spaces.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index does not identify a segment.</exception>
        /// <exception cref="ArgumentNullException">The context is null.</exception>
        public string GetFillMarker(int segmentIndex, RenderContext context) {
            if (segmentIndex < 0 || segmentIndex >= _segments.Count) throw new ArgumentOutOfRangeException(nameof(segmentIndex));
            if (context == null) throw new ArgumentNullException(nameof(context));
            IGlyphSet glyphs = context.Glyphs;
            if (glyphs.Tier == GlyphTier.Ascii) {
                return segmentIndex < AsciiFills.Length ? AsciiFills[segmentIndex] : AdditionalFills[(segmentIndex - AsciiFills.Length) % AdditionalFills.Length].ToString();
            }
            if (context.HasColor) {
                return glyphs.Get(Glyph.BlockFull);
            }
            Glyph[] shades = { Glyph.BlockFull, Glyph.ShadeDark, Glyph.ShadeMedium, Glyph.ShadeLight };
            return segmentIndex < shades.Length ? glyphs.Get(shades[segmentIndex]) : AdditionalFills[(segmentIndex - shades.Length) % AdditionalFills.Length].ToString();
        }

        private int[] SegmentWidths(int width) {
            double total = 0;
            foreach (ChartItem segment in _segments) {
                total += NumberFormat.PositiveOrZero(segment.Value);
            }
            int[] widths = new int[_segments.Count];
            if (total <= 0) {
                return widths;
            }
            double[] remainders = new double[_segments.Count];
            int used = 0;
            for (int i = 0; i < _segments.Count; i++) {
                double exact = NumberFormat.PositiveOrZero(_segments[i].Value) / total * width;
                widths[i] = (int)Math.Floor(exact);
                if (widths[i] == 0 && NumberFormat.PositiveOrZero(_segments[i].Value) > 0) {
                    widths[i] = 1;
                }
                remainders[i] = exact - Math.Floor(exact);
                used += widths[i];
            }
            while (used < width) {
                int best = -1;
                for (int i = 0; i < remainders.Length; i++) {
                    if (NumberFormat.PositiveOrZero(_segments[i].Value) > 0 && (best < 0 || remainders[i] > remainders[best])) {
                        best = i;
                    }
                }
                if (best < 0) {
                    break;
                }
                widths[best]++;
                remainders[best] = -1;
                used++;
            }
            while (used > width) {
                int widest = 0;
                for (int i = 1; i < widths.Length; i++) {
                    if (widths[i] > widths[widest]) {
                        widest = i;
                    }
                }
                widths[widest]--;
                used--;
            }
            return widths;
        }

        private List<StyledLine> LegendLines(int width, RenderContext context) {
            double total = 0;
            foreach (ChartItem segment in _segments) {
                total += NumberFormat.PositiveOrZero(segment.Value);
            }
            List<StyledLine> lines = new List<StyledLine>();
            if (_segments.Count == 0) {
                return lines;
            }
            // One grid for every entry: marker | label (left) | count (right) | percent (right). Counts reserve the digits
            // of the overall total, which no segment can exceed, so the grid holds still as the numbers grow.
            int markerWidth = 0;
            int labelWidth = 0;
            int countWidth = 0;
            foreach (ChartItem segment in _segments) {
                labelWidth = Math.Max(labelWidth, DisplayWidth.Measure(segment.Label));
                countWidth = Math.Max(countWidth, DisplayWidth.Measure(Count(segment.Value, context)));
            }
            for (int i = 0; i < _segments.Count; i++) {
                markerWidth = Math.Max(markerWidth, DisplayWidth.Measure(GetFillMarker(i, context)));
            }
            labelWidth = _labelColumn.Resolve(labelWidth);
            countWidth = _countColumn.Resolve(countWidth, 0, DisplayWidth.Measure(NumberFormat.Compact(total)));
            int percentWidth = total > 0 ? _percentColumn.Resolve(DisplayWidth.Measure(NumberFormat.Percent(1))) : 0;
            int entryWidth = markerWidth + 1 + labelWidth + 1 + countWidth + (percentWidth > 0 ? 1 + percentWidth : 0);
            const int Gap = 3;
            int columns = Math.Max(1, Math.Min(_segments.Count, (width + Gap) / (entryWidth + Gap)));
            if (MaxLegendColumns > 0) {
                columns = Math.Min(columns, MaxLegendColumns);
            }
            if (entryWidth > width) {
                // Too narrow for the grid: shrink the label column so counts and percents keep their columns.
                labelWidth = Math.Max(1, labelWidth - (entryWidth - width));
            }
            int[] percents = WholePercents(total);
            List<TextSpan> current = new List<TextSpan>();
            for (int i = 0; i < _segments.Count; i++) {
                if (i > 0 && i % columns == 0) {
                    lines.Add(new StyledLine(current));
                    current = new List<TextSpan>();
                }
                if (i % columns != 0) {
                    current.Add(new TextSpan(new string(' ', Gap)));
                }
                ChartItem segment = _segments[i];
                Style textStyle = context.Style(ThemeToken.Text);
                StyledLine label = TextWrapper.Truncate(RichLines.Line(segment.LabelText, segment.Label, textStyle, context), labelWidth, context.Ellipsis);
                current.Add(new TextSpan(StatColumn.Pad(GetFillMarker(i, context), markerWidth, HorizontalAlignment.Left), context.Style(segment.Role ?? ThemeToken.Series1)));
                // The label keeps its own colours; padding fills the column per its alignment.
                int padding = Math.Max(0, labelWidth - label.Width);
                int before = LabelColumn.Alignment == HorizontalAlignment.Right ? padding : LabelColumn.Alignment == HorizontalAlignment.Center ? padding / 2 : 0;
                current.Add(new TextSpan(" " + new string(' ', before), textStyle));
                current.AddRange(label);
                current.Add(new TextSpan(new string(' ', padding - before), textStyle));
                current.Add(new TextSpan(" " + StatColumn.Pad(Count(segment.Value, context), countWidth, CountColumn.Alignment), context.Style(ThemeToken.Text)));
                if (percentWidth > 0) {
                    string percent = NumberFormat.IsFinite(segment.Value) ? percents[i].ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%" : NumberFormat.Missing(context.Glyphs.Tier);
                    current.Add(new TextSpan(" " + StatColumn.Pad(percent, percentWidth, PercentColumn.Alignment), context.Style(ThemeToken.Muted)));
                }
            }
            if (current.Count > 0) {
                lines.Add(new StyledLine(current));
            }
            return lines;
        }

        // Whole legend percents that add up to 100 (largest remainder): 7, 3 and 2 of 12 are 58%, 25% and 17%, not 58/25/16.
        internal int[] WholePercents(double total) {
            int[] result = new int[_segments.Count];
            if (!(total > 0)) { return result; }
            double[] remainders = new double[_segments.Count];
            int assigned = 0;
            for (int i = 0; i < _segments.Count; i++) {
                if (!NumberFormat.IsFinite(_segments[i].Value)) { continue; }
                double exact = NumberFormat.PositiveOrZero(_segments[i].Value) / total * 100;
                result[i] = (int)Math.Floor(exact + 1e-9);
                remainders[i] = exact - result[i];
                assigned += result[i];
            }
            for (int left = 100 - assigned; left > 0; left--) {
                int best = -1;
                for (int i = 0; i < _segments.Count; i++) {
                    if (remainders[i] > 1e-9 && (best < 0 || remainders[i] > remainders[best])) { best = i; }
                }
                if (best < 0) { break; }
                result[best]++;
                remainders[best] = 0;
            }
            return result;
        }

        // NaN or infinite segments (a rate over zero elapsed time) take no width and show a dash.
        private static string Count(double value, RenderContext context) {
            return NumberFormat.IsFinite(value) ? NumberFormat.Compact(value) : NumberFormat.Missing(context.Glyphs.Tier);
        }
    }
}
