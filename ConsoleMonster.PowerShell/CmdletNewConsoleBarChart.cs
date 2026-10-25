using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a horizontal bar chart, or a stacked bar with -Stacked.</para>
    /// <para type="description">Bars come from -InputObject (labels mapped to numbers), from a script block of New-ConsoleBarItem calls (when a bar needs its own tone) or from the pipeline. A stacked bar splits one bar into proportional segments with a legend showing values and shares. -Role sets the colour of every bar as a theme role (Fail, Warn, Pass, Accent, Series1 to Series6...); -Warning and -Critical colour bars by threshold instead. To add bars later, pipe the chart to Add-ConsoleItem; in a live dashboard, update values with Add-ConsoleValue or Set-ConsoleValue.</para>
    /// </summary>
    /// <example>
    ///   <para>Resolver latency</para>
    ///   <code>New-ConsoleBarChart -Unit ' ms' -InputObject ([ordered] @{ Cloudflare = 12; Google = 18; Quad9 = 24 }) | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Failures per area, drawn in the fail colour</para>
    ///   <code>New-ConsoleBarChart -Role Fail -InputObject ([ordered] @{ 'Domain controllers' = 14; 'Group Policy' = 9; 'DNS' = 4 }) | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Disk use with thresholds: 80 % and more is a warning, 90 % and more critical</para>
    ///   <code>New-ConsoleBarChart -Unit '%' -Warning 80 -Critical 90 -InputObject ([ordered] @{ 'C:' = 93; 'D:' = 34; 'E:' = 81 }) | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Results split, each segment in its status colour</para>
    ///   <code>
    /// New-ConsoleBarChart -Stacked -Content {
    ///     New-ConsoleBarItem 'Passed' 412 -Role Pass
    ///     New-ConsoleBarItem 'Warnings' 51 -Role Warn
    ///     New-ConsoleBarItem 'Failed' 23 -Role Fail
    /// } | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleBarChart")]
    [OutputType(typeof(BarChart), typeof(StackedBar))]
    public sealed class CmdletNewConsoleBarChart : PSCmdlet {
        private readonly List<object> _piped = new List<object>();
        private readonly List<IDictionary> _dictionaries = new List<IDictionary>();


        /// <summary><para type="description">A script block that emits the bars or segments: New-ConsoleBarItem calls or hashtables with Label, Value and Tone.</para></summary>
        [Parameter(Position = 2)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Bars from the pipeline: New-ConsoleBarItem objects or hashtables with Label, Value and Tone.</para></summary>
        [Parameter(ValueFromPipeline = true, Position = 1)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Draw one stacked bar with a legend instead of one bar per label.</para></summary>
        [Parameter]
        public SwitchParameter Stacked { get; set; }

        /// <summary><para type="description">A unit appended to values.</para></summary>
        [Parameter]
        public string? Unit { get; set; }

        /// <summary><para type="description">The live name (default bars, or stacked with -Stacked).</para></summary>
        [Parameter(Position = 0)]
        public string? WidgetId { get; set; }

        /// <summary><para type="description">Bar shape: Line (thin stroke) or Block (solid blocks). Default Block.</para></summary>
        [Parameter]
        public BarShape Shape { get; set; } = BarShape.Block;

        /// <summary><para type="description">The colour of the bars as a theme role: Accent, Pass, Fail, Warn, Info, Muted or Series1 to Series6 (default Series1). A New-ConsoleBarItem -Role overrides it for one bar.</para></summary>
        [Parameter]
        public ThemeToken? Role { get; set; }

        /// <summary><para type="description">The value that fills a whole bar (default: the largest value).</para></summary>
        [Parameter]
        public double? Maximum { get; set; }

        /// <summary><para type="description">The value from which a bar turns to the warning colour (with -Critical below it, lower values are worse, as for free space).</para></summary>
        [Parameter]
        public double? Warning { get; set; }

        /// <summary><para type="description">The value from which a bar turns to the fail colour.</para></summary>
        [Parameter]
        public double? Critical { get; set; }

        /// <summary><para type="description">Shared ValueColumn settings from New-ConsoleColumnFormat.</para></summary>
        [Parameter] public Layout.ColumnFormat? ValueColumn { get; set; }
        /// <summary><para type="description">Shared CountColumn settings from New-ConsoleColumnFormat.</para></summary>
        [Parameter] public Layout.ColumnFormat? CountColumn { get; set; }
        /// <summary><para type="description">Shared PercentColumn settings from New-ConsoleColumnFormat.</para></summary>
        [Parameter] public Layout.ColumnFormat? PercentColumn { get; set; }
        /// <summary><para type="description">Shared LabelColumn settings from New-ConsoleColumnFormat.</para></summary>
        [Parameter] public Layout.ColumnFormat? LabelColumn { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                object? raw = PSText.Unwrap(InputObject);
                if (raw is IDictionary dictionary && !dictionary.Contains("Label")) { _dictionaries.Add(dictionary); }
                else if (raw is ScriptBlock script) {
                    if (Content != null) { throw new PSArgumentException("Give the builder script through Content only once."); }
                    Content = script;
                } else { _piped.AddRange(PSItems.Items(InputObject)); }
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            object widget;
            ScriptBlock? content = Content;
            if (Stacked.IsPresent) {
                StackedBar stacked = new StackedBar(WidgetId ?? "stacked");
                foreach (IDictionary dictionary in _dictionaries) {
                    foreach (DictionaryEntry entry in dictionary) {
                        stacked.Add(PSWidgets.ToText(entry.Key), Number(entry));
                    }
                }
                widget = stacked;
            } else {
                BarChart chart = new BarChart(WidgetId ?? "bars") { Unit = Unit, Shape = Shape, Warning = Warning, Critical = Critical, Maximum = Maximum };
                if (Role.HasValue) {
                    chart.Role = Role.Value;
                }
                foreach (IDictionary dictionary in _dictionaries) {
                    foreach (DictionaryEntry entry in dictionary) {
                        chart.Add(PSWidgets.ToText(entry.Key), Number(entry));
                    }
                }
                widget = chart;
            }
            PSItems.AddAll(this, widget, PSItems.Items(content));
            PSItems.AddAll(this, widget, _piped);
            if (widget is BarChart bars) {
                if (CountColumn != null || PercentColumn != null || LabelColumn != null) { throw new PSArgumentException("CountColumn, PercentColumn and LabelColumn apply to Stacked bars."); }
                if (ValueColumn != null) { bars.ValueColumn = ValueColumn; }
            } else if (widget is StackedBar segments) {
                if (ValueColumn != null) { throw new PSArgumentException("ValueColumn applies to unstacked bars."); }
                if (CountColumn != null) { segments.CountColumn = CountColumn; }
                if (PercentColumn != null) { segments.PercentColumn = PercentColumn; }
                if (LabelColumn != null) { segments.LabelColumn = LabelColumn; }
            }
            WriteObject(widget);
        }

        // A -Data value as a number, or a terminating error that names the bar.
        private double Number(DictionaryEntry entry) {
            try {
                return LanguagePrimitives.ConvertTo<double>(entry.Value);
            } catch (PSInvalidCastException) {
                ThrowTerminatingError(new ErrorRecord(new PSArgumentException("The value of '" + PSWidgets.ToText(entry.Key) + "' is not a number: '" + PSWidgets.ToText(entry.Value) + "'."), "ConsoleMonsterBarValue", ErrorCategory.InvalidArgument, entry.Value));
                return 0;
            }
        }
    }
}
