using System;
using ChartForgeX.Core;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.Graphics {
    /// <summary>Explores ChartForgeX line charts inside a ConsoleMonster app.</summary>
    public static class ChartPlotExtensions {
        /// <summary>
        /// A ChartForgeX Line or StepLine chart with numeric X as the <see cref="ChartPlot"/> a <see cref="ChartView"/>
        /// explores: <c>new ChartView("key", chart.ToChartPlot())</c> or <c>Ui.ChartView().Chart(chart.ToChartPlot())</c>.
        /// Zoom and pan select scenes from the X zoom levels (1, 2, 4, 8 and 16) ChartForgeX prepares once per size and
        /// theme, so navigating never prepares the source chart again. Points along X come from the first series. Series
        /// can be hidden when ChartForgeX can prepare a visibility view for the chart, which needs every series added with
        /// its own <see cref="ChartSeriesId"/>; otherwise every series stays shown.
        /// </summary>
        /// <param name="chart">The chart.</param>
        /// <returns>The plot.</returns>
        public static ChartPlot ToChartPlot(this Chart chart) {
            if (chart == null) { throw new ArgumentNullException(nameof(chart)); }
            return new ChartForgeXPlot(chart, new ChartXZoomLevels(1, 2, 4, 8, 16));
        }
    }
}