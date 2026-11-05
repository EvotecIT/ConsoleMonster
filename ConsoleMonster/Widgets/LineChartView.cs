using System.Collections.Generic;

namespace ConsoleMonster.Widgets {
    // What an explorable view asks a line chart to draw (a window of x positions, without some series) and what the chart
    // reports back: where the plot landed and where each legend entry was written.
    internal sealed class LineChartView {
        internal int Start { get; set; }
        internal int Length { get; set; }
        internal HashSet<int> Hidden { get; } = new HashSet<int>();

        internal int AxisWidth { get; set; }
        internal int PlotColumns { get; set; }
        internal int PlotRows { get; set; }
        internal int PixelWidth { get; set; }
        internal int LegendRow { get; set; } = -1;
        internal List<Entry> Legend { get; } = new List<Entry>();

        internal readonly struct Entry {
            internal Entry(int series, int x, int width) { Series = series; X = x; Width = width; }
            internal int Series { get; }
            internal int X { get; }
            internal int Width { get; }
        }
    }
}
