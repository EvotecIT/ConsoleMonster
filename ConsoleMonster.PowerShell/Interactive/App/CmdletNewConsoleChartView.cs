using System.Management.Automation;
using ConsoleMonster.Graphics;
using ConsoleMonster.Interactive;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Makes a line chart explorable inside Show-ConsoleApp.</summary>
    /// <remarks><para>Left/Right or a click moves a crosshair along X, and the row above the plot reads every shown series at that point; the app footer repeats it for screen readers. A number key or a click on a legend entry hides or shows a series. + and - or the wheel zoom along X, Shift+Left/Right or a drag pans, and Home shows everything. The state holds the index of the point under the crosshair. Give New-ConsoleDataGrid the same -LinkKey to link a table: selecting a row moves the crosshair and moving the crosshair selects the row. -Chart takes a Line or Scatter chart from New-ConsoleChart, or a ChartForgeX line chart, whose zoom uses ChartForgeX's prepared X levels.</para></remarks>
    /// <example>
    /// <para>Explore CPU and memory together, as in the chart-table gallery demo. Both series and PointLabels use the order of the samples array. At each point, the label and both values describe the same minute. Giving the chart and grid the same LinkKey synchronizes the selected grid row and chart crosshair.</para>
    /// <code>$samples = 1..30 | ForEach-Object { [pscustomobject]@{ Minute = $_; Cpu = 40 + (Get-Random -Maximum 50); Memory = 50 + $_ } }
    /// $chartOptions = @{
    ///     Type = 'Line'
    ///     Unit = '%'
    ///     Series = @(
    ///         @{ Key = 'cpu'; Values = $samples.Cpu }, @{ Key = 'memory'; Values = $samples.Memory })
    /// }
    /// $chart = New-ConsoleChart @chartOptions
    /// Show-ConsoleApp -Content {
    ///     $chartViewOptions = @{
    ///         Key = 'Load'
    ///         Chart = $chart
    ///         PointLabels = @($samples.Minute)
    ///         LinkKey = 'Minute'
    ///     }
    ///     New-ConsoleChartView @chartViewOptions
    ///     $dataGridOptions = @{
    ///         Key = 'Samples'
    ///         InputObject = $samples
    ///         Property = @('Minute', 'Cpu', 'Memory')
    ///         Height = 8
    ///         LinkKey = 'Minute'
    ///     }
    ///     New-ConsoleDataGrid @dataGridOptions
    /// }</code>
    /// </example>
    /// <example>
    /// <para>Compose an app with one CPU series. Series is a one-element array containing the series descriptor. Its Values array supplies four successive points. The chart view uses the resulting line chart as its data.</para>
    /// <code>$chartOptions = @{
    ///     Type = 'Line'
    ///     Series = @(
    ///         @{ Key = 'cpu'; Values = @(12, 25, 18, 31) }
    ///     )
    /// }
    /// $chart = New-ConsoleChart @chartOptions
    /// Show-ConsoleApp -Content {
    ///     $chartViewOptions = @{
    ///         Key = 'cpu'
    ///         Chart = $chart
    ///     }
    ///     New-ConsoleChartView @chartViewOptions
    /// }</code>
    /// </example>
    /// <example>
    /// <para>Read any of these gallery scripts to see New-ConsoleChartView in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>Get-ConsoleDemo -Slug 'chart-table' | Select-Object -ExpandProperty Source
    /// Get-ConsoleDemo -Slug 'incident-workbench' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleChartView", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolechartview/")]
    [OutputType(typeof(ChartView))]
    public sealed class CmdletNewConsoleChartView : AppInputCmdlet {
        /// <summary><para type="description">A Line or Scatter chart from New-ConsoleChart, a ChartForgeX line chart with numeric X, or a ChartPlot.</para></summary>
        [Parameter(Mandatory = true)] public object Chart { get; set; } = null!;
        /// <summary><para type="description">Rows for the readout and the plot, legend included.</para></summary>
        [Parameter, ValidateRange(4, 1000)] [PSDefaultValue(Help = "10")]
        public int Height { get; set; } = 10;
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
