using System;
using System.Collections.Generic;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>An explorable chart shared by Ui.ChartView and New-ConsoleChartView.</summary>
    public sealed class ChartViewInputOptions : AppInputOptions {
        /// <summary>The chart to explore: a <see cref="LineChart"/> (converted implicitly), or any other <see cref="ChartPlot"/>.</summary>
        public ChartPlot? Chart { get; set; }
        /// <summary>Rows for the readout and the plot, legend included.</summary>
        public int Height { get; set; } = 10;
        /// <summary>Labels for the points along X, read out with their values.</summary>
        public IList<string>? Labels { get; set; }
        /// <summary>The app-state key shared with linked controls (see <see cref="ILinkedSelection"/>): the point under the crosshair.</summary>
        public string? SelectionKey { get; set; }
        /// <summary>Creates the persistent input after its key and chart are configured.</summary>
        public ChartView Build() {
            if (Chart == null) { throw new InvalidOperationException("A chart view needs a Chart."); }
            var control = new ChartView(Key, Chart) { Label = Label, Height = Height, SelectionKey = SelectionKey, Disabled = Disabled, TabIndex = TabIndex, ValidationMessage = ValidationMessage };
            if (Labels != null) { foreach (string label in Labels) { control.Labels.Add(label); } }
            if (OnChange != null) { control.Changed += OnChange; }
            return control;
        }
    }
}
