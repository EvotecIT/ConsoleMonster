using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a line or scatter chart (braille on modern fonts, half blocks on Consolas, characters in ASCII mode).</para>
    /// <para type="description">Series come from a script block of New-ConsoleChartSeries calls (each with its values and an optional tone), from the pipeline, or from -Data (series names mapped to arrays of values). Values are spaced evenly. -Series declares empty series for a live chart. To add a series later, pipe the chart to Add-ConsoleItem; in a live dashboard, append values with Add-ConsoleValue -Key series.</para>
    /// </summary>
    /// <example>
    ///   <para>Two series with their own tones</para>
    ///   <code>
    /// $completed = foreach ($i in 0..47) { 20 + 9 * [math]::Sin($i / 7) + $i % 3 }
    /// $failed = foreach ($i in 0..47) { 3 + 2 * [math]::Cos($i / 5) }
    /// New-ConsoleLineChart -Height 7 {
    ///     New-ConsoleChartSeries 'completed' -Value $completed -Tone Accent
    ///     New-ConsoleChartSeries 'failed' -Value $failed -Tone Fail
    /// } | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Series from a hashtable</para>
    ///   <code>New-ConsoleLineChart -Height 8 -Data ([ordered] @{ cpu = 12, 30, 45, 38, 52; memory = 40, 41, 43, 47, 50 }) | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>A scatter of latency samples</para>
    ///   <code>
    /// $samples = foreach ($i in 0..59) { 40 + ($i * 37) % 23 }
    /// New-ConsoleLineChart -Scatter -Height 7 -Unit 'ms' { New-ConsoleChartSeries 'latency' $samples -Tone Accent2 } | Write-ConsoleWidget</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleLineChart")]
    [OutputType(typeof(LineChart))]
    public sealed class CmdletNewConsoleLineChart : PSCmdlet {
        private readonly List<object> _piped = new List<object>();

        /// <summary><para type="description">Series names mapped to value arrays (a script block here is taken as -Content).</para></summary>
        [Parameter(Position = 0)]
        public object? Data { get; set; }

        /// <summary><para type="description">A script block that emits the series: New-ConsoleChartSeries calls.</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Series from the pipeline: New-ConsoleChartSeries objects.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Series to declare without values (for live charts).</para></summary>
        [Parameter]
        public string[]? Series { get; set; }

        /// <summary><para type="description">Rows including the axis (default 8).</para></summary>
        [Parameter]
        public int Height { get; set; } = 8;

        /// <summary><para type="description">Draw points instead of lines.</para></summary>
        [Parameter]
        public SwitchParameter Scatter { get; set; }

        /// <summary><para type="description">A unit for the axis labels.</para></summary>
        [Parameter]
        public string? Unit { get; set; }

        /// <summary><para type="description">The lowest value on the axis (default: the smallest value).</para></summary>
        [Parameter]
        public double? Minimum { get; set; }

        /// <summary><para type="description">The highest value on the axis (default: the largest value).</para></summary>
        [Parameter]
        public double? Maximum { get; set; }

        /// <summary><para type="description">The live name (default chart).</para></summary>
        [Parameter]
        public string Name { get; set; } = "chart";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _piped.AddRange(PSItems.Items(InputObject));
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            LineChart chart = new LineChart(Name) { Height = Height, Scatter = Scatter.IsPresent, Unit = Unit, Min = Minimum, Max = Maximum };
            if (Series != null) {
                foreach (string series in Series) {
                    chart.AddSeries(series);
                }
            }
            object? data = PSText.Unwrap(Data);
            ScriptBlock? content = Content;
            if (data is ScriptBlock script) {
                content = script;
            } else if (data is IDictionary dictionary) {
                foreach (DictionaryEntry entry in dictionary) {
                    string series = PSWidgets.ToText(entry.Key);
                    foreach (double value in LanguagePrimitives.ConvertTo<double[]>(entry.Value)) {
                        chart.Add(series, value);
                    }
                }
            } else if (data != null) {
                ThrowTerminatingError(new ErrorRecord(new PSArgumentException("-Data takes a hashtable of series names and values; give series with a script block of New-ConsoleChartSeries calls."), "ConsoleMonsterChartData", ErrorCategory.InvalidArgument, data));
            }
            PSItems.AddAll(this, chart, PSItems.Items(content));
            PSItems.AddAll(this, chart, _piped);
            WriteObject(chart);
        }
    }
}
