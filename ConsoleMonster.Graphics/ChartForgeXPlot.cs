using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using CmColor = ConsoleMonster.Styling.Color;

namespace ConsoleMonster.Graphics {
    // A ChartForgeX line chart explored in the terminal. Zoom and pan select scenes from the X zoom bank ChartForgeX
    // prepares once per size and theme, so navigating never runs the source chart, its formatters or its layout again.
    // Hiding a series prepares that visibility over the current window. The image fills the plot rows as half blocks
    // (background-coloured cells on the ASCII tier); the legend row below it is drawn as text so it can be clicked.
    internal sealed class ChartForgeXPlot : ChartPlot {
        // Logical units per cell: the scene is laid out at this size and sampled down to two pixels per cell.
        private const int CellWidth = 8, CellHeight = 16;
        private readonly Chart _chart;
        private readonly ChartXZoomLevels _levels;
        private readonly double[] _x;
        private PreparedChartXZoom? _bank;
        private string? _bankKey;
        private string? _sceneKey;
        private Scene? _scene;
        private Dictionary<double, double>?[]? _values;
        private bool? _hideable;

        internal ChartForgeXPlot(Chart chart, ChartXZoomLevels levels) {
            _chart = chart ?? throw new ArgumentNullException(nameof(chart));
            _levels = levels ?? throw new ArgumentNullException(nameof(levels));
            ChartSeries? first = chart.Series.FirstOrDefault();
            _x = first == null ? Array.Empty<double>() : first.Points.Select(point => point.X).ToArray();
        }


        public override int Count => _x.Length;
        public override int SeriesCount => _chart.Series.Count;
        public override string SeriesName(int series) => _chart.Series[series].Name;
        public override Style SeriesStyle(int series, RenderContext context) {
            ChartColor? color = _chart.Series[series].Color;
            return color.HasValue ? Style.Plain.WithColor(CmColor.FromRgb(color.Value.R, color.Value.G, color.Value.B)).Downsample(context.Depth)
                : context.Style((ThemeToken)((int)ThemeToken.Series1 + (series % 6)));
        }
        // The first series supplies the points, so it reads its own point; every other series is read at that point's X
        // (its first point there), and a series without a point there has no value.
        public override double? Value(int series, int index) {
            if (index < 0 || index >= _x.Length || series < 0 || series >= _chart.Series.Count) { return null; }
            if (series == 0) { return _chart.Series[0].Points[index].Y; }
            _values ??= new Dictionary<double, double>?[_chart.Series.Count];
            Dictionary<double, double> byX = _values[series] ??= ByX(_chart.Series[series].Points);
            return byX.TryGetValue(_x[index], out double value) ? value : (double?)null;
        }

        private static Dictionary<double, double> ByX(IList<ChartPoint> points) {
            var byX = new Dictionary<double, double>();
            foreach (ChartPoint point in points) { if (!byX.ContainsKey(point.X)) { byX[point.X] = point.Y; } }
            return byX;
        }
        // Hiding is offered for the whole chart or not at all, as ChartForgeX itself decides: the first time it is asked,
        // the plot prepares a small visibility view and keeps the answer. A view that still fails while drawing turns
        // hiding off for good and shows every series.
        public override bool CanHide(int series) => series >= 0 && series < _chart.Series.Count && Hideable;

        private bool Hideable {
            get {
                if (!_hideable.HasValue) {
                    try {
                        var probe = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 160), 8), frame: new VisualFrame(showLegend: false, transparentBackground: true));
                        _chart.PrepareVisibility(probe, new ChartVisibilityView(new[] { _chart.Series[0].Id }));
                        _hideable = true;
                    } catch (Exception error) when (Unsupported(error)) {
                        _hideable = false;
                    }
                }
                return _hideable.Value;
            }
        }

        private static bool Unsupported(Exception error) => error is NotSupportedException || error is ArgumentException || error is InvalidOperationException;
        public override string FormatValue(double value, RenderContext context) => value.ToString("0.##", context.Culture);
        public override string? XLabel(int index, RenderContext context) => index >= 0 && index < _x.Length ? _x[index].ToString("G", context.Culture) : null;
        public override int Levels => _levels.Count;
        public override double WindowFraction(int level) => 1 / _levels[Math.Max(0, Math.Min(_levels.Count - 1, level))];
        public override double Magnification(int level) => _levels[Math.Max(0, Math.Min(_levels.Count - 1, level))];

        public override double Position(int index) {
            if (index < 0 || index >= _x.Length) { return 0; }
            double min, max;
            if (_bank != null) { min = _bank.FullViewport.Minimum; max = _bank.FullViewport.Maximum; }
            else { min = _x.Min(); max = _x.Max(); }
            return max > min ? Math.Max(0, Math.Min(1, (_x[index] - min) / (max - min))) : 0;
        }

        public override Size Measure(Constraints constraints, RenderContext context) =>
            new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? 12 : Math.Min(12, constraints.MaxHeight));

        public override ChartPlotLayout Render(Region region, RenderContext context, int level, double pan, ICollection<int> hidden) {
            bool legend = _chart.Series.Count > 1 && region.Height >= 3;
            int rows = region.Height - (legend ? 1 : 0);
            Rect bounds = new Rect(region.Bounds.X, region.Bounds.Y, region.Width, Math.Max(0, rows));
            Layout layout = new Layout(this, bounds);
            if (rows < 2 || region.Width < 8 || _x.Length == 0) { return layout; }
            Scene scene = SceneFor(region.Width, rows, context, level, pan, hidden);
            layout.Scene = scene;
            Draw(region, scene.Pixels, region.Width, rows, context);
            ChartRect plot = scene.Visual.XProjection?.PlotBounds ?? new ChartRect(0, 0, region.Width * CellWidth, rows * CellHeight);
            int left = (int)Math.Floor(plot.Left / CellWidth), right = (int)Math.Ceiling(plot.Right / CellWidth);
            int top = (int)Math.Floor(plot.Top / CellHeight), bottom = (int)Math.Ceiling(plot.Bottom / CellHeight);
            layout.Plot = new Rect(bounds.X + left, bounds.Y + top, Math.Max(0, Math.Min(region.Width, right) - left), Math.Max(0, Math.Min(rows, bottom) - top));
            if (legend) { DrawLegend(region, rows, context, hidden, layout); }
            return layout;
        }

        private Scene SceneFor(int columns, int rows, RenderContext context, int level, double pan, ICollection<int> hidden) {
            bool dark = Luminance(context.Style(ThemeToken.Text).Color) > 0.5;
            string bankKey = columns.ToString(CultureInfo.InvariantCulture) + "x" + rows.ToString(CultureInfo.InvariantCulture) + (dark ? "d" : "l");
            VisualRenderContext visual = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(columns * CellWidth, rows * CellHeight), 8),
                themeMode: dark ? VisualThemeMode.Dark : VisualThemeMode.Light, frame: new VisualFrame(showLegend: false, transparentBackground: true));
            if (_bank == null || _bankKey != bankKey) {
                _bank = _chart.PrepareXZoom(visual, _levels);
                _bankKey = bankKey;
                _scene = null;
            }
            level = Math.Max(0, Math.Min(_levels.Count - 1, level));
            pan = Math.Max(0, Math.Min(1, pan));
            string sceneKey = bankKey + "|" + level.ToString(CultureInfo.InvariantCulture) + "|" + pan.ToString("R", CultureInfo.InvariantCulture) + "|" + string.Join(",", hidden.OrderBy(i => i));
            if (_scene != null && _sceneKey == sceneKey) { return _scene; }
            PreparedVisual prepared;
            if (hidden.Count == 0) {
                prepared = _bank.At(level, pan);
            } else {
                // Visibility is not part of the zoom bank: the chart prepares the shown series over the current window.
                ChartAxisWindow full = _bank.FullViewport;
                double span = full.Maximum - full.Minimum, width = span / _levels[level];
                double start = full.Minimum + pan * (span - width);
                ChartVisibilityView visibility = new ChartVisibilityView(hidden.Where(i => i >= 0 && i < _chart.Series.Count).Select(i => _chart.Series[i].Id));
                ChartCartesianViewport? viewport = level == 0 ? null : new ChartCartesianViewport(new ChartAxisWindow(start, start + width));
                try {
                    prepared = _chart.PrepareVisibility(visual, visibility, viewport);
                } catch (Exception error) when (Unsupported(error)) {
                    _hideable = false;
                    prepared = _bank.At(level, pan);
                }
            }
            RgbaImage image = ImageSampler.Resize(prepared.ToRgba(), columns, rows * 2, keepDetail: true);
            _scene = new Scene(prepared, image);
            _sceneKey = sceneKey;
            return _scene;
        }

        // Half blocks on Basic and Full; one background-coloured space per cell on ASCII; without colour, a mark where a
        // cell differs from what lies behind the chart.
        private static void Draw(Region region, RgbaImage image, int columns, int rows, RenderContext context) {
            byte[] px = image.Pixels;
            if (!context.HasColor) {
                for (int y = 0; y < rows; y++) {
                    for (int x = 0; x < columns; x++) {
                        int top = ((y * 2 * columns) + x) * 4, bottom = top + (columns * 4);
                        if (px[top + 3] >= 128 || px[bottom + 3] >= 128) { region.Put(x, y, context.Glyphs.Tier == GlyphTier.Ascii ? "*" : "•", Style.Plain); }
                    }
                }
                return;
            }
            if (context.Glyphs.Tier == GlyphTier.Ascii) {
                for (int y = 0; y < rows; y++) {
                    for (int x = 0; x < columns; x++) {
                        int top = ((y * 2 * columns) + x) * 4, bottom = top + (columns * 4);
                        int alpha = px[top + 3] + px[bottom + 3];
                        if (alpha < 256) { continue; }
                        CmColor color = CmColor.FromRgb(Mix(px, top, bottom, 0, alpha), Mix(px, top, bottom, 1, alpha), Mix(px, top, bottom, 2, alpha));
                        region.Put(x, y, " ", Style.Plain.WithBackgroundColor(color).Downsample(context.Depth));
                    }
                }
                return;
            }
            HalfBlockCanvas canvas = new HalfBlockCanvas(columns, rows);
            for (int y = 0; y < image.Height; y++) {
                for (int x = 0; x < image.Width; x++) {
                    int i = ((y * image.Width) + x) * 4;
                    if (px[i + 3] >= 128) { canvas.Set(x, y, CmColor.FromRgb(px[i], px[i + 1], px[i + 2])); }
                }
            }
            canvas.Render(region, 0, 0, context);
        }

        private void DrawLegend(Region region, int row, RenderContext context, ICollection<int> hidden, Layout layout) {
            Style muted = context.Style(ThemeToken.Muted);
            int x = 1;
            for (int series = 0; series < _chart.Series.Count && x < region.Width; series++) {
                string marker = (series + 1).ToString(CultureInfo.InvariantCulture);
                string name = Glyphs.SymbolFallback.Constrain(_chart.Series[series].Name, context.Glyphs.Tier);
                int start = x;
                if (hidden.Contains(series)) {
                    string text = marker + " " + (context.HasColor ? name : "(" + name + ")");
                    x = region.Write(x, row, TextWrapper.Truncate(text, region.Width - x, context.Ellipsis), muted.With(TextDecoration.Strikethrough));
                } else {
                    x = region.Write(x, row, marker, SeriesStyle(series, context));
                    x = region.Write(x + 1, row, TextWrapper.Truncate(name, Math.Max(0, region.Width - x - 1), context.Ellipsis), muted);
                }
                layout.Legend.Add(new KeyValuePair<int, Rect>(series, new Rect(region.Bounds.X + start, region.Bounds.Y + row, x - start, 1)));
                x += 3;
            }
        }

        private static double Luminance(CmColor color) {
            if (color.IsDefault) { return 1; }
            color.ToRgb(out byte r, out byte g, out byte b);
            return ((0.2126 * r) + (0.7152 * g) + (0.0722 * b)) / 255;
        }

        private static byte Mix(byte[] px, int top, int bottom, int channel, int alpha) =>
            (byte)(((px[top + channel] * px[top + 3]) + (px[bottom + channel] * px[bottom + 3]) + (alpha / 2)) / alpha);

        private sealed class Scene {
            internal Scene(PreparedVisual visual, RgbaImage pixels) { Visual = visual; Pixels = pixels; }
            internal PreparedVisual Visual { get; }
            internal RgbaImage Pixels { get; }
        }

        // Columns map to source X through the scene's own projection, so a click lands on the point drawn under it.
        private sealed class Layout : ChartPlotLayout {
            private readonly ChartForgeXPlot _plot;
            private readonly Rect _bounds;
            internal Layout(ChartForgeXPlot plot, Rect bounds) { _plot = plot; _bounds = bounds; Plot = new Rect(bounds.X, bounds.Y, 0, 0); }
            internal Scene? Scene { get; set; }

            public override int? ColumnOf(int index) {
                ChartAxisProjection? x = Scene?.Visual.XProjection;
                if (x == null || index < 0 || index >= _plot._x.Length) { return null; }
                double source = _plot._x[index];
                double low = Math.Min(x.Minimum, x.Maximum), high = Math.Max(x.Minimum, x.Maximum);
                if (source < low || source > high) { return null; }
                int column = _bounds.X + (int)Math.Floor(x.Project(source) / CellWidth);
                return column >= Plot.X && column < Plot.Right ? column : (int?)null;
            }

            public override int? IndexAt(int column) {
                ChartAxisProjection? x = Scene?.Visual.XProjection;
                if (x == null || column < Plot.X || column >= Plot.Right || _plot._x.Length == 0) { return null; }
                double source = x.Unproject(((column - _bounds.X) + 0.5) * CellWidth);
                double low = Math.Min(x.Minimum, x.Maximum), high = Math.Max(x.Minimum, x.Maximum);
                int best = -1;
                double distance = double.MaxValue;
                for (int i = 0; i < _plot._x.Length; i++) {
                    double at = _plot._x[i];
                    if (at < low || at > high) { continue; }
                    double d = Math.Abs(at - source);
                    if (d < distance) { distance = d; best = i; }
                }
                return best < 0 ? (int?)null : best;
            }
        }
    }
}
