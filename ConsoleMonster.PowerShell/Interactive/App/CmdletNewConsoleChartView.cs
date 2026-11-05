using System.Management.Automation;
using ConsoleMonster.Graphics;
using ConsoleMonster.Interactive;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Makes a line chart explorable inside Show-ConsoleApp.</para>
    /// <para type="description">Left/Right or a click moves a crosshair along X, and the row above the plot reads every shown series at that point; the app footer repeats it for screen readers. A number key or a click on a legend entry hides or shows a series. + and - or the wheel zoom along X, Shift+Left/Right or a drag pans, and Home shows everything. The state holds the index of the point under the crosshair. Give New-ConsoleDataGrid the same -LinkKey to link a table: selecting a row moves the crosshair and moving the crosshair selects the row. -Chart takes a Line or Scatter chart from New-ConsoleChart, or a ChartForgeX line chart, whose zoom uses ChartForgeX's prepared X levels.</para>
    /// </summary>
    /// <example>
    /// <para>Explore CPU and memory samples next to the table they came from</para>
    /// <code>$samples = 1..30 | ForEach-Object { [pscustomobject]@{ Minute = $_; Cpu = 40 + (Get-Random -Maximum 50); Memory = 50 + $_ } }
    /// $chart = New-ConsoleChart -Type Line -Unit '%' -Series @(
    ///     @{ Key = 'cpu'; Values = $samples.Cpu }, @{ Key = 'memory'; Values = $samples.Memory })
    /// Show-ConsoleApp -Content {
    ///     New-ConsoleChartView -Key Load -Chart $chart -PointLabels $samples.Minute -LinkKey Minute
    ///     New-ConsoleDataGrid -Key Samples -InputObject $samples -Height 8 -LinkKey Minute
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleChartView")]
    [OutputType(typeof(ChartView))]
    public sealed class CmdletNewConsoleChartView : AppInputCmdlet {
        /// <summary><para type="description">A Line or Scatter chart from New-ConsoleChart, a ChartForgeX line chart with numeric X, or a ChartPlot.</para></summary>
        [Parameter(Mandatory = true)] public object Chart { get; set; } = null!;
        /// <summary><para type="description">Rows for the readout and the plot, legend included.</para></summary>
        [Parameter, ValidateRange(4, 1000)] public int Height { get; set; } = 10;
        /// <summary><para type="description">Labels for the points along X, such as times, read out with their values.</para></summary>
        [Parameter] public string[]? PointLabels { get; set; }
        /// <summary><para type="description">The state key shared with linked controls (New-ConsoleDataGrid, New-ConsoleSelect, another chart): the index of the point under the crosshair. A position it cannot show is ignored.</para></summary>
        [Parameter] public string? LinkKey { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            object chart = Chart is PSObject wrapped ? wrapped.BaseObject : Chart;
            ChartPlot plot = chart is LineChart line ? line
                : chart is ChartForgeX.Core.Chart forge ? forge.ToChartPlot()
                : chart as ChartPlot ?? throw new PSArgumentException("Chart takes a Line or Scatter chart from New-ConsoleChart, a ChartForgeX line chart or a ChartPlot.", nameof(Chart));
            ChartView view = new ChartViewInputOptions { Key = Key, Chart = plot, Height = Height, PointLabels = PointLabels, LinkKey = LinkKey }.Build();
            Emit(view);
        }
    }
}
