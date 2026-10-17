using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a line or scatter chart (braille on modern fonts, half blocks on Consolas, characters in ASCII mode).</para>
    /// <para type="description">-Data maps series names to arrays of values; values are spaced evenly. Append values live with Add-ConsoleValue -Key series.</para>
    /// <example>
    ///   <para>Two series</para>
    ///   <code>New-ConsoleLineChart -Height 8 -Data ([ordered] @{ cpu = 12, 30, 45, 38, 52; memory = 40, 41, 43, 47, 50 }) | Write-ConsoleWidget</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleLineChart")]
    [OutputType(typeof(LineChart))]
    public sealed class CmdletNewConsoleLineChart : PSCmdlet {
        /// <summary><para type="description">Series names mapped to value arrays.</para></summary>
        [Parameter(Position = 0)]
        public IDictionary? Data { get; set; }

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

        /// <summary><para type="description">The live name (default chart).</para></summary>
        [Parameter]
        public string Name { get; set; } = "chart";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            LineChart chart = new LineChart(Name) { Height = Height, Scatter = Scatter.IsPresent, Unit = Unit };
            if (Series != null) {
                foreach (string series in Series) {
                    chart.AddSeries(series);
                }
            }
            if (Data != null) {
                foreach (DictionaryEntry entry in Data) {
                    string series = PSWidgets.ToText(entry.Key);
                    foreach (double value in LanguagePrimitives.ConvertTo<double[]>(entry.Value)) {
                        chart.Add(series, value);
                    }
                }
            }
            WriteObject(chart);
        }
    }
}
