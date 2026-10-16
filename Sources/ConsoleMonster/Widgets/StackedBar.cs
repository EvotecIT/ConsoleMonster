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
    /// (Passed, Failed, Warnings, Skipped...) take that status colour unless a token is given. Without colour, segments use distinct
    /// fill glyphs so the split stays readable. Live operations (Key = segment): <c>set</c> (Value), <c>add</c> (Value).
    /// </summary>
    public sealed class StackedBar : LiveWidget {
        private static readonly string[] AsciiFills = { "#", "=", "+", "-", ".", ":" };
        private readonly List<ChartItem> _segments = new List<ChartItem>();

        /// <summary>Creates a stacked bar.</summary>
        /// <param name="name">The live name (default <c>stacked</c>).</param>
        public StackedBar(string name = "stacked") : base(name) {
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

        /// <summary>Adds or updates a segment.</summary>
        /// <param name="label">The label.</param>
        /// <param name="value">The value.</param>
        /// <param name="token">The colour token, or <c>null</c> for the next series colour.</param>
        /// <returns>This instance.</returns>
        public StackedBar Add(string label, double value, ThemeToken? token = null) {
            foreach (ChartItem segment in _segments) {
                if (string.Equals(segment.Label, label, StringComparison.Ordinal)) {
                    segment.Value = value;
                    if (token.HasValue) {
                        segment.Token = token;
                    }
                    return this;
                }
            }
            ThemeToken automatic = StatusKinds.TryParse(label, out StatusKind status) ? status.ToToken() : (ThemeToken)((int)ThemeToken.Series1 + (_segments.Count % 6));
            _segments.Add(new ChartItem(label, value, token ?? automatic));
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
                    if (segment.Label == liveEvent.Key) {
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
                string fill = FillGlyph(i, context);
                region.HorizontalLine(x, 0, widths[i], fill, context.Style(_segments[i].Token ?? ThemeToken.Series1));
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

        private string FillGlyph(int index, RenderContext context) {
            IGlyphSet glyphs = context.Glyphs;
            if (glyphs.Tier == GlyphTier.Ascii) {
                return AsciiFills[index % AsciiFills.Length];
            }
            if (context.HasColor) {
                return glyphs.Get(Glyph.BlockFull);
            }
            Glyph[] shades = { Glyph.BlockFull, Glyph.ShadeDark, Glyph.ShadeMedium, Glyph.ShadeLight };
            return glyphs.Get(shades[index % shades.Length]);
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
                markerWidth = Math.Max(markerWidth, DisplayWidth.Measure(FillGlyph(i, context)));
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
                string label = TextWrapper.Truncate(segment.Label, labelWidth, context.Ellipsis);
                current.Add(new TextSpan(StatColumn.Pad(FillGlyph(i, context), markerWidth, HorizontalAlignment.Left), context.Style(segment.Token ?? ThemeToken.Series1)));
                current.Add(new TextSpan(" " + StatColumn.Pad(label, labelWidth, LabelColumn.Alignment), context.Style(ThemeToken.Text)));
                current.Add(new TextSpan(" " + StatColumn.Pad(Count(segment.Value, context), countWidth, CountColumn.Alignment), context.Style(ThemeToken.Text)));
                if (percentWidth > 0) {
                    string percent = NumberFormat.IsFinite(segment.Value) ? NumberFormat.Percent(NumberFormat.PositiveOrZero(segment.Value) / total) : NumberFormat.Missing(context.Glyphs.Tier);
                    current.Add(new TextSpan(" " + StatColumn.Pad(percent, percentWidth, PercentColumn.Alignment), context.Style(ThemeToken.Muted)));
                }
            }
            if (current.Count > 0) {
                lines.Add(new StyledLine(current));
            }
            return lines;
        }

        // NaN or infinite segments (a rate over zero elapsed time) take no width and show a dash.
        private static string Count(double value, RenderContext context) {
            return NumberFormat.IsFinite(value) ? NumberFormat.Compact(value) : NumberFormat.Missing(context.Glyphs.Tier);
        }
    }
}
