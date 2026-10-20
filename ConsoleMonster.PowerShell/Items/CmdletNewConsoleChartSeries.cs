using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates one series of a line or scatter chart: a name, values and an optional tone.</para>
    /// <para type="description">Use it inside the script block of New-ConsoleLineChart. Values come from -Value or from the pipeline, oldest first, and are spaced evenly. -Tone takes a theme role (Accent, Accent2, Pass, Fail, Warn, Series1 to Series6); without it the chart picks the next series colour, and a series named with a status word (failed, passed, warnings) takes that status colour. To add a series to a chart made earlier, pipe the chart to Add-ConsoleItem; in a running dashboard, append values with Add-ConsoleValue -Key.</para>
    /// </summary>
    /// <example>
    ///   <para>Two series computed in the script</para>
    ///   <code>
    /// $completed = foreach ($i in 0..47) { 20 + 9 * [math]::Sin($i / 7) + $i % 3 }
    /// $failed = foreach ($i in 0..47) { 3 + 2 * [math]::Cos($i / 5) }
    /// New-ConsoleLineChart -Height 7 {
    ///     New-ConsoleChartSeries 'completed' -Value $completed -Tone Accent
    ///     New-ConsoleChartSeries 'failed' -Value $failed -Tone Fail
    /// } | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Values from the pipeline</para>
    ///   <code>
    /// New-ConsoleLineChart -Scatter -Unit 'ms' {
    ///     Get-Content latency.txt | New-ConsoleChartSeries 'latency'
    /// } | Write-ConsoleWidget</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleChartSeries")]
    [OutputType(typeof(ChartSeries))]
    public sealed class CmdletNewConsoleChartSeries : PSCmdlet {
        private readonly List<double> _values = new List<double>();

        /// <summary><para type="description">The series name, shown in the legend (rich text from New-ConsoleText keeps its colours).</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public object Name { get; set; } = string.Empty;

        /// <summary><para type="description">The values, oldest first (pipeline input works too).</para></summary>
        [Parameter(Position = 1, ValueFromPipeline = true)]
        public double[]? Value { get; set; }

        /// <summary><para type="description">The colour as a theme role: Accent, Accent2, Pass, Fail, Warn, Info, Muted or Series1 to Series6.</para></summary>
        [Parameter]
        public ThemeToken? Tone { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Value != null) {
                _values.AddRange(Value);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Text.RichText? rich = PSText.Rich(Name);
            ChartSeries series = new ChartSeries(rich?.PlainText ?? PSText.PlainText(Name), Tone ?? ThemeToken.Series1) { NameText = rich };
            if (!Tone.HasValue) {
                PSItems.MarkAutomaticTone(series);
            }
            foreach (double value in _values) {
                series.Add(value, int.MaxValue);
            }
            WriteObject(series);
        }
    }
}
