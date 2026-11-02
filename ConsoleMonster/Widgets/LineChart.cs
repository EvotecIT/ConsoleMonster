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
    /// A line or scatter chart. With colour, the canvas uses braille (2x4 dots per cell) on Full, half blocks on Basic
    /// and characters on Ascii. Without colour, ASCII marks identify each series in the plot and legend.
    /// A y axis with min/max labels sits on the left, a legend below when there are several series or a semantic status to identify.
    /// Live operation: <c>add</c> (Key = series, Value).
    /// </summary>
    public sealed class LineChart : LiveWidget {
        private readonly List<ChartSeries> _series = new List<ChartSeries>();
        private const string SeriesMarks = "*+ox#%@&ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnpqrstuvwyz0123456789";

        private static string SeriesMark(int index) => SeriesMarks[index % SeriesMarks.Length].ToString();

        /// <summary>Creates a chart.</summary>
        /// <param name="widgetId">The live name (default <c>chart</c>).</param>
        public LineChart(string widgetId = "chart") : base(widgetId) {
        }

        /// <summary>The series.</summary>
        public IReadOnlyList<ChartSeries> Series => _series;

        /// <summary>Rows for the plot including the x axis (default 8).</summary>
        public int Height { get; set; } = 8;

        /// <summary>Whether points are joined by lines (default) or drawn as a scatter.</summary>
        public bool Scatter { get; set; }

        /// <summary>How many values each series keeps (default 512).</summary>
        public int Capacity { get; set; } = 512;

        /// <summary>A fixed minimum for the y axis, or <c>null</c> (0 when all values are positive).</summary>
        public double? Minimum { get; set; }

        /// <summary>A fixed maximum for the y axis, or <c>null</c>.</summary>
        public double? Maximum { get; set; }

        /// <summary>A unit appended to axis labels.</summary>
        public string? Unit { get; set; }

        /// <summary>Adds a series with a multicolour legend name (its plain text is the key), or returns the existing one.</summary>
        /// <param name="key">The rich name.</param>
        /// <param name="role">The colour role, or <c>null</c> for the next series colour.</param>
        /// <returns>The series.</returns>
        public ChartSeries AddSeries(Text.RichText key, ThemeToken? role = null) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }
            ChartSeries series = AddSeries(key.PlainText, role);
            series.LabelText = key;
            return series;
        }

        /// <summary>Adds a series, or returns the existing one with that name.</summary>
        /// <param name="key">The name.</param>
        /// <param name="role">The colour role, or <c>null</c> for the next series colour.</param>
        /// <returns>The series.</returns>
        public ChartSeries AddSeries(string key, ThemeToken? role = null) {
            foreach (ChartSeries existing in _series) {
                if (string.Equals(existing.Key, key, StringComparison.Ordinal)) {
                    return existing;
                }
            }
            // A series named with a status word (failed, passed, warnings...) takes that status tone unless a role is given.
            ThemeToken automatic = StatusKinds.TryParse(key, out StatusKind status) ? status.ToToken() : (ThemeToken)((int)ThemeToken.Series1 + (_series.Count % 6));
            ChartSeries series = new ChartSeries(key, role ?? automatic);
            _series.Add(series);
            return series;
        }

        /// <summary>Appends a value to a series (created when missing).</summary>
        /// <param name="series">The series name.</param>
        /// <param name="value">The value.</param>
        /// <returns>This instance.</returns>
        public LineChart Add(string series, double value) {
            AddSeries(series).Add(value, Capacity);
            return this;
        }

        /// <summary>Returns a handle that appends values through a dispatcher.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <returns>The handle.</returns>
        public SparklineHandle Handle(ILiveDispatcher dispatcher) => new SparklineHandle(dispatcher, WidgetId);

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Operation == "add") {
                Add(liveEvent.Key ?? "series", liveEvent.Value);
            }
        }

        private IWidget SeriesText(RenderContext context) {
            var rows = new Rows();
            foreach (ChartSeries series in _series) {
                var values = new List<string>();
                foreach (double value in series.Values) values.Add(value.ToString("G", context.Culture) + (Unit ?? string.Empty));
                rows.Add(new TextBlock(Markup.Escape((StatusKinds.TryFromToken(series.Role, out StatusKind state) ? context.Glyphs.StatusSymbol(state) + " " : string.Empty) + series.Label + ": " + (values.Count == 0 ? NumberFormat.Missing(context.Glyphs.Tier) : string.Join(", ", values.ToArray())))));
            }
            return rows;
        }

        private bool NeedsLegend => _series.Count > 1 || (_series.Count == 1 && StatusKinds.TryFromToken(_series[0].Role, out _));

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            if (context.Profile.IsAccessible) return SeriesText(context).Measure(constraints, context);
            int height = Math.Max(3, Height) + (NeedsLegend ? 1 : 0);
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (context.Profile.IsAccessible) { SeriesText(context).Render(region, context); return; }
            bool legend = NeedsLegend && region.Height >= 3;
            int plotRows = region.Height - 1 - (legend ? 1 : 0);
            if (plotRows < 1 || region.Width < 8) {
                return;
            }
            double min = Minimum ?? double.MaxValue;
            double max = Maximum ?? double.MinValue;
            int longest = 0;
            foreach (ChartSeries series in _series) {
                longest = Math.Max(longest, series.Values.Count);
                foreach (double v in series.Values) {
                    if (!Minimum.HasValue) {
                        min = Math.Min(min, v);
                    }
                    if (!Maximum.HasValue) {
                        max = Math.Max(max, v);
                    }
                }
            }
            if (longest == 0) {
                min = 0;
                max = 1;
            }
            if (!Minimum.HasValue && min > 0) {
                min = 0;
            }
            // A non-finite fixed bound would turn every point into NaN; fall back to the data range.
            if (!NumberFormat.IsFinite(min)) {
                min = 0;
            }
            if (!NumberFormat.IsFinite(max) || max <= min) {
                max = min + 1;
            }
            string top = NumberFormat.Compact(max) + (Unit ?? string.Empty);
            string bottom = NumberFormat.Compact(min) + (Unit ?? string.Empty);
            int axisWidth = Math.Max(DisplayWidth.Measure(top), DisplayWidth.Measure(bottom)) + 1;
            Style muted = context.Style(ThemeToken.Muted);
            Style axis = context.Style(ThemeToken.Border);
            region.Write(axisWidth - 1 - DisplayWidth.Measure(top), 0, top, muted);
            region.Write(axisWidth - 1 - DisplayWidth.Measure(bottom), plotRows - 1, bottom, muted);
            BoxGlyphs box = context.Glyphs.GetBox(BoxStyle.Single);
            region.VerticalLine(axisWidth - 1, 0, plotRows, box.Vertical, axis);
            region.Put(axisWidth - 1, plotRows, box.BottomLeft, axis);
            region.HorizontalLine(axisWidth, plotRows, region.Width - axisWidth, box.Horizontal, axis);

            int plotColumns = region.Width - axisWidth;
            bool monochrome = !context.HasColor;
            PixelCanvas canvas = monochrome ? new CharacterCanvas(plotColumns, plotRows) : PixelCanvas.Create(context.Profile.Glyphs, plotColumns, plotRows);
            int pw = canvas.PixelWidth;
            int ph = canvas.PixelHeight;
            var traceLabels = new Dictionary<int, List<TextSpan>>();
            int seriesIndex = 0;
            foreach (ChartSeries series in _series) {
                if (monochrome) {
                    canvas.Clear();
                    ((CharacterCanvas)canvas).Mark = SeriesMark(seriesIndex);
                }
                seriesIndex++;
                Color color = context.Style(series.Role).Color;
                IReadOnlyList<double> values = series.Values;
                int count = Math.Min(values.Count, pw);
                int start = values.Count - count;
                int prevX = -1;
                int prevY = -1;
                for (int i = 0; i < count; i++) {
                    int px = count == 1 ? pw - 1 : (int)Math.Round((double)i / (count - 1) * (pw - 1));
                    double norm = (values[start + i] - min) / (max - min);
                    if (double.IsNaN(norm)) {
                        // A range wider than double can represent (max - min overflowed).
                        norm = 0;
                    }
                    int py = (ph - 1) - (int)Math.Round(Math.Max(0, Math.Min(1, norm)) * (ph - 1));
                    if (Scatter || prevX < 0) {
                        canvas.Set(px, py, color);
                    } else {
                        canvas.Line(prevX, prevY, px, py, color);
                    }
                    prevX = px;
                    prevY = py;
                }
                if (monochrome) canvas.Render(region, axisWidth, 0, context);
                if (_series.Count > 1 && prevX >= 0) {
                    int row = prevY / canvas.PixelsPerRow;
                    if (!traceLabels.TryGetValue(row, out List<TextSpan>? labels)) traceLabels[row] = labels = new List<TextSpan>();
                    if (labels.Count > 0) labels.Add(new TextSpan("/", muted));
                    labels.Add(new TextSpan(seriesIndex.ToString(CultureInfo.InvariantCulture), context.Style(series.Role)));
                }
            }
            if (!monochrome) canvas.Render(region, axisWidth, 0, context);

            foreach (var label in traceLabels) {
                // Traces that converge in one terminal cell share its label; none overwrites another's identity.
                var text = new StyledLine(label.Value);
                region.Write(Math.Max(axisWidth, region.Width - text.Width), label.Key, text);
            }
            if (legend) {
                List<TextSpan> spans = new List<TextSpan>();
                int index = 0;
                foreach (ChartSeries series in _series) {
                    string marker = _series.Count > 1 ? (index + 1).ToString(CultureInfo.InvariantCulture) : string.Empty;
                    if (monochrome) marker += (marker.Length > 0 ? " " : string.Empty) + SeriesMark(index);
                    index++;
                    if (StatusKinds.TryFromToken(series.Role, out StatusKind status)) marker += (marker.Length > 0 ? " " : string.Empty) + context.Glyphs.StatusSymbol(status);
                    if (spans.Count > 0) {
                        spans.Add(new TextSpan("   "));
                    }
                    spans.Add(new TextSpan(marker, context.Style(series.Role)));
                    spans.Add(new TextSpan(" ", muted));
                    spans.AddRange(RichLines.Line(series.LabelText, series.Label, muted, context));
                }
                region.Write(axisWidth, region.Height - 1, new StyledLine(spans));
            }
        }
    }
}
