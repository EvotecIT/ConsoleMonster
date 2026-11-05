using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// What a <see cref="ChartView"/> explores, so any chart can take its crosshair, legend toggles, zoom and pan: points
    /// along X (index 0 to <see cref="Count"/> - 1), each series' value at a point, and a drawing at a zoom level and pan
    /// position. A level shows a fixed fraction of the X domain; the pan fraction places that window, 0 at the start and 1
    /// at the end. <see cref="LineChart"/> computes any level; ConsoleMonster.Graphics draws a ChartForgeX chart from the
    /// levels ChartForgeX prepared.
    /// </summary>
    public abstract class ChartPlot {
        /// <summary>The number of points along X.</summary>
        public abstract int Count { get; }
        /// <summary>The number of series.</summary>
        public abstract int SeriesCount { get; }
        /// <summary>A series' name, read out with its value.</summary>
        /// <param name="series">The series position, from 0.</param>
        /// <returns>The name.</returns>
        public abstract string SeriesName(int series);
        /// <summary>The style of a series' number in the readout and legend.</summary>
        /// <param name="series">The series position, from 0.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The style.</returns>
        public abstract Style SeriesStyle(int series, RenderContext context);
        /// <summary>A series' value at a point, or null where it has none.</summary>
        /// <param name="series">The series position, from 0.</param>
        /// <param name="index">The point index.</param>
        /// <returns>The value.</returns>
        public abstract double? Value(int series, int index);
        /// <summary>Whether a series can be hidden (default: every series).</summary>
        /// <param name="series">The series position, from 0.</param>
        /// <returns>True when it can be hidden.</returns>
        public virtual bool CanHide(int series) => series >= 0 && series < SeriesCount;
        /// <summary>A value as the readout shows it, with its unit.</summary>
        /// <param name="value">The value.</param>
        /// <param name="context">The render context, for its culture.</param>
        /// <returns>The text.</returns>
        public abstract string FormatValue(double value, RenderContext context);
        /// <summary>A label for a point when the view has none of its own, or null to show its number.</summary>
        /// <param name="index">The point index.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The label.</returns>
        public abstract string? XLabel(int index, RenderContext context);
        /// <summary>The number of zoom levels, at least one; level 0 shows the whole X domain.</summary>
        public abstract int Levels { get; }
        /// <summary>The fraction of the X domain a level shows (1 at level 0).</summary>
        /// <param name="level">The level.</param>
        /// <returns>The fraction.</returns>
        public abstract double WindowFraction(int level);
        /// <summary>The magnification a level is read out as (1 at level 0).</summary>
        /// <param name="level">The level.</param>
        /// <returns>The magnification.</returns>
        public abstract double Magnification(int level);
        /// <summary>Where a point sits in the X domain, from 0 to 1.</summary>
        /// <param name="index">The point index.</param>
        /// <returns>The position.</returns>
        public abstract double Position(int index);
        /// <summary>The natural size of the drawing.</summary>
        /// <param name="constraints">The available space.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The size.</returns>
        public abstract Size Measure(Constraints constraints, RenderContext context);
        /// <summary>Draws the window a level and pan show, without the hidden series, and reports where it drew.</summary>
        /// <param name="region">The area below the readout row.</param>
        /// <param name="context">The render context.</param>
        /// <param name="level">The zoom level.</param>
        /// <param name="pan">The pan fraction, from 0 to 1.</param>
        /// <param name="hidden">The positions of the hidden series.</param>
        /// <returns>The plot area, the column of each point and the legend entries.</returns>
        public abstract ChartPlotLayout Render(Region region, RenderContext context, int level, double pan, ICollection<int> hidden);

        /// <summary>Explores a native line chart: its points are value indices, aligned from each series' first value.</summary>
        /// <param name="chart">The chart.</param>
        public static implicit operator ChartPlot(LineChart chart) => new LineChartPlot(chart);

        // The window start, as a fraction of the domain, for a level and pan.
        internal double WindowStart(int level, double pan) => Math.Max(0, Math.Min(1, pan)) * (1 - WindowFraction(level));
    }

    /// <summary>Where a <see cref="ChartPlot"/> drew: its plot area, which column shows which point, and its legend entries, in buffer coordinates.</summary>
    public abstract class ChartPlotLayout {
        /// <summary>The plot area; the crosshair is drawn and clicks land inside it.</summary>
        public Rect Plot { get; set; }
        /// <summary>Each legend entry's series position and area; a click on one hides or shows that series.</summary>
        public IList<KeyValuePair<int, Rect>> Legend { get; } = new List<KeyValuePair<int, Rect>>();
        /// <summary>The column that shows a point, or null when it is outside the window.</summary>
        /// <param name="index">The point index.</param>
        /// <returns>The column.</returns>
        public abstract int? ColumnOf(int index);
        /// <summary>The point a column shows, or null outside the plot.</summary>
        /// <param name="column">The column.</param>
        /// <returns>The point index.</returns>
        public abstract int? IndexAt(int column);
    }
    // The native line chart: X positions are value indices, aligned from each series' first value.
    internal sealed class LineChartPlot : ChartPlot {
        private readonly LineChart _chart;

        internal LineChartPlot(LineChart chart) { _chart = chart ?? throw new ArgumentNullException(nameof(chart)); }

        public override int Count => _chart.Longest;
        public override int SeriesCount => _chart.Series.Count;
        public override string SeriesName(int series) => _chart.Series[series].Label;
        public override Style SeriesStyle(int series, RenderContext context) => context.Style(_chart.Series[series].Role);
        public override double? Value(int series, int index) {
            IReadOnlyList<double> values = _chart.Series[series].Values;
            return index >= 0 && index < values.Count ? values[index] : (double?)null;
        }
        public override string FormatValue(double value, RenderContext context) => value.ToString("G", context.Culture) + (_chart.Unit ?? string.Empty);
        public override string? XLabel(int index, RenderContext context) => null;

        // Each level halves the window, down to two points.
        public override int Levels {
            get {
                int levels = 1;
                while (levels < 16 && Length(levels) < Length(levels - 1)) { levels++; }
                return levels;
            }
        }
        public override double WindowFraction(int level) => Count <= 1 ? 1 : (double)(Length(level) - 1) / (Count - 1);
        public override double Magnification(int level) => Count <= 1 ? 1 : (double)Count / Length(level);
        public override double Position(int index) => Count <= 1 ? 0 : (double)index / (Count - 1);
        private int Length(int level) => Math.Min(Count, Math.Max(2, (int)Math.Ceiling(Count / Math.Pow(2, level))));
        private int Start(int level, double pan) => (int)Math.Round(Math.Max(0, Math.Min(1, pan)) * (Count - Length(level)));

        public override Size Measure(Constraints constraints, RenderContext context) => _chart.Measure(constraints, context);

        public override ChartPlotLayout Render(Region region, RenderContext context, int level, double pan, ICollection<int> hidden) {
            LineChartView view = new LineChartView { Start = Start(level, pan), Length = Math.Max(1, Length(level)) };
            foreach (int series in hidden) { view.Hidden.Add(series); }
            _chart.Render(region, context, view);
            Layout layout = new Layout(view, region.Bounds);
            if (view.LegendRow >= 0) {
                foreach (LineChartView.Entry entry in view.Legend) {
                    layout.Legend.Add(new KeyValuePair<int, Rect>(entry.Series, new Rect(region.Bounds.X + entry.X, region.Bounds.Y + view.LegendRow, entry.Width, 1)));
                }
            }
            return layout;
        }

        private sealed class Layout : ChartPlotLayout {
            private readonly LineChartView _view;
            internal Layout(LineChartView view, Rect bounds) {
                _view = view;
                Plot = view.PlotColumns <= 0 ? new Rect(bounds.X, bounds.Y, 0, 0) : new Rect(bounds.X + view.AxisWidth, bounds.Y, view.PlotColumns, view.PlotRows);
            }

            // The chart places point i of the window at pixel round(i / (length - 1) * (pixels - 1)).
            public override int? ColumnOf(int index) {
                int offset = index - _view.Start;
                if (Plot.Width <= 0 || offset < 0 || offset >= _view.Length) { return null; }
                int pixel = _view.Length == 1 ? _view.PixelWidth - 1 : (int)Math.Round((double)offset / (_view.Length - 1) * (_view.PixelWidth - 1));
                return Plot.X + pixel * Plot.Width / Math.Max(1, _view.PixelWidth);
            }

            public override int? IndexAt(int column) {
                if (Plot.Width <= 0 || column < Plot.X || column >= Plot.Right) { return null; }
                if (_view.Length <= 1) { return _view.Start; }
                int perColumn = Math.Max(1, _view.PixelWidth / Plot.Width);
                double pixel = (column - Plot.X) * perColumn + (perColumn - 1) / 2.0;
                return _view.Start + (int)Math.Round(pixel / Math.Max(1, _view.PixelWidth - 1) * (_view.Length - 1));
            }
        }
    }
}
