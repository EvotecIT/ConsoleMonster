using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a horizontal bar chart, or a stacked bar with -Stacked.</para>
    /// <para type="description">Bars come from -Data (labels mapped to numbers), from a script block of New-ConsoleBarItem calls (when a bar needs its own tone) or from the pipeline. A stacked bar splits one bar into proportional segments with a legend showing values and shares. -Tone sets the colour of every bar as a theme role (Fail, Warn, Pass, Accent, Series1 to Series6...); -Warning and -Critical colour bars by threshold instead. To add bars later, pipe the chart to Add-ConsoleItem; in a live dashboard, update values with Add-ConsoleValue or Set-ConsoleValue.</para>
    /// </summary>
    /// <example>
    ///   <para>Resolver latency</para>
    ///   <code>New-ConsoleBarChart -Unit ' ms' -Data ([ordered] @{ Cloudflare = 12; Google = 18; Quad9 = 24 }) | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Failures per area, drawn in the fail colour</para>
    ///   <code>New-ConsoleBarChart -Tone Fail ([ordered] @{ 'Domain controllers' = 14; 'Group Policy' = 9; 'DNS' = 4 }) | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Disk use with thresholds: 80 % and more is a warning, 90 % and more critical</para>
    ///   <code>New-ConsoleBarChart -Unit '%' -Warning 80 -Critical 90 -Data ([ordered] @{ 'C:' = 93; 'D:' = 34; 'E:' = 81 }) | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Results split, each segment in its status colour</para>
    ///   <code>
    /// New-ConsoleBarChart -Stacked {
    ///     New-ConsoleBarItem 'Passed' 412 -Tone Pass
    ///     New-ConsoleBarItem 'Warnings' 51 -Tone Warn
    ///     New-ConsoleBarItem 'Failed' 23 -Tone Fail
    /// } | Write-ConsoleWidget</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleBarChart")]
    [OutputType(typeof(BarChart), typeof(StackedBar))]
    public sealed class CmdletNewConsoleBarChart : PSCmdlet {
        private readonly List<object> _piped = new List<object>();

        /// <summary><para type="description">Labels mapped to values (a script block here is taken as -Content).</para></summary>
        [Parameter(Position = 0)]
        public object? Data { get; set; }

        /// <summary><para type="description">A script block that emits the bars or segments: New-ConsoleBarItem calls or hashtables with Label, Value and Tone.</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Bars from the pipeline: New-ConsoleBarItem objects or hashtables with Label, Value and Tone.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

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

        /// <summary><para type="description">The colour of the bars as a theme role: Accent, Pass, Fail, Warn, Info, Muted or Series1 to Series6 (default Series1). A New-ConsoleBarItem -Tone overrides it for one bar.</para></summary>
        [Parameter]
        public ThemeToken? Tone { get; set; }

        /// <summary><para type="description">The value that fills a whole bar (default: the largest value).</para></summary>
        [Parameter]
        public double? Maximum { get; set; }

        /// <summary><para type="description">The value from which a bar turns to the warning colour (with -Critical below it, lower values are worse, as for free space).</para></summary>
        [Parameter]
        public double? Warning { get; set; }

        /// <summary><para type="description">The value from which a bar turns to the fail colour.</para></summary>
        [Parameter]
        public double? Critical { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _piped.AddRange(PSItems.Items(InputObject));
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            object widget;
            object? data = PSText.Unwrap(Data);
            ScriptBlock? content = data is ScriptBlock script ? script : Content;
            IDictionary? dictionary = data as IDictionary;
            if (data != null && dictionary == null && !(data is ScriptBlock)) {
                ThrowTerminatingError(new ErrorRecord(new PSArgumentException("-Data takes a hashtable of labels and values; give bars with a script block of New-ConsoleBarItem calls."), "ConsoleMonsterBarData", ErrorCategory.InvalidArgument, data));
            }
            if (Stacked.IsPresent) {
                StackedBar stacked = new StackedBar(Name ?? "stacked");
                if (dictionary != null) {
                    foreach (DictionaryEntry entry in dictionary) {
                        stacked.Add(PSWidgets.ToText(entry.Key), LanguagePrimitives.ConvertTo<double>(entry.Value));
                    }
                }
                widget = stacked;
            } else {
                BarChart chart = new BarChart(Name ?? "bars") { Unit = Unit, Shape = Shape, Warning = Warning, Critical = Critical, Max = Maximum };
                if (Tone.HasValue) {
                    chart.BarToken = Tone.Value;
                }
                if (dictionary != null) {
                    foreach (DictionaryEntry entry in dictionary) {
                        chart.Add(PSWidgets.ToText(entry.Key), LanguagePrimitives.ConvertTo<double>(entry.Value));
                    }
                }
                widget = chart;
            }
            PSItems.AddAll(this, widget, PSItems.Items(content));
            PSItems.AddAll(this, widget, _piped);
            WriteObject(widget);
        }
    }
}
