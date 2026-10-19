using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a horizontal bar chart, or a stacked bar with -Stacked.</para>
    /// <para type="description">-Data maps labels to numbers. A stacked bar splits one bar into proportional segments with a legend showing values and shares. Update values live with Add-ConsoleValue or Set-ConsoleValue.</para>
    /// <example>
    ///   <para>Resolver latency</para>
    ///   <code>New-ConsoleBarChart -Unit ' ms' -Data ([ordered] @{ Cloudflare = 12; Google = 18; Quad9 = 24 }) | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Disk use with thresholds: 80 % and more is a warning, 90 % and more critical</para>
    ///   <code>New-ConsoleBarChart -Unit '%' -Warning 80 -Critical 90 -Data ([ordered] @{ 'C:' = 93; 'D:' = 34; 'E:' = 81 }) | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Results split</para>
    ///   <code>New-ConsoleBarChart -Stacked -Data ([ordered] @{ Passed = 412; Warnings = 51; Failed = 23 }) | Write-ConsoleWidget</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleBarChart")]
    [OutputType(typeof(BarChart), typeof(StackedBar))]
    public sealed class CmdletNewConsoleBarChart : PSCmdlet {
        /// <summary><para type="description">Labels mapped to values.</para></summary>
        [Parameter(Position = 0)]
        public IDictionary? Data { get; set; }

        /// <summary><para type="description">Draw one stacked bar with a legend instead of one bar per label.</para></summary>
        [Parameter]
        public SwitchParameter Stacked { get; set; }

        /// <summary><para type="description">A unit appended to values.</para></summary>
        [Parameter]
        public string? Unit { get; set; }

        /// <summary><para type="description">The live name (default bars, or stacked with -Stacked).</para></summary>
        [Parameter]
        public string? Name { get; set; }

        /// <summary><para type="description">Bar shape: Line (thin stroke) or Block (solid blocks). Default Block.</para></summary>
        [Parameter]
        public BarShape Shape { get; set; } = BarShape.Block;

        /// <summary><para type="description">The value from which a bar turns to the warning colour (with -Critical below it, lower values are worse, as for free space).</para></summary>
        [Parameter]
        public double? Warning { get; set; }

        /// <summary><para type="description">The value from which a bar turns to the fail colour.</para></summary>
        [Parameter]
        public double? Critical { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Stacked.IsPresent) {
                StackedBar stacked = new StackedBar(Name ?? "stacked");
                if (Data != null) {
                    foreach (DictionaryEntry entry in Data) {
                        stacked.Add(PSWidgets.ToText(entry.Key), LanguagePrimitives.ConvertTo<double>(entry.Value));
                    }
                }
                WriteObject(stacked);
                return;
            }
            BarChart chart = new BarChart(Name ?? "bars") { Unit = Unit, Shape = Shape, Warning = Warning, Critical = Critical };
            if (Data != null) {
                foreach (DictionaryEntry entry in Data) {
                    chart.Add(PSWidgets.ToText(entry.Key), LanguagePrimitives.ConvertTo<double>(entry.Value));
                }
            }
            WriteObject(chart);
        }
    }
}
