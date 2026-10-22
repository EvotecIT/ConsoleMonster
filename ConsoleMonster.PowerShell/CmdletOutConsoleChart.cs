using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.PowerShell.Binding;
using ConsoleMonster.Values;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Draws horizontal bars, a histogram or grouped sparklines from pipeline objects.</para>
    /// <para type="description">Select one numeric property, including a calculated property, or pipe numbers directly.
    /// Bars show value labels in theme colours. Sorting is stable and happens before Top. Sparkline labels identify groups;
    /// samples within each group retain arrival order, so sort timestamped input before piping it. Histogram uses every
    /// valid sample regardless of Sort or Top, and integral data has whole-number bucket edges. Null, malformed, boolean
    /// and non-finite values produce a non-terminating error and are skipped; an empty input produces no output.
    /// No-colour output lists numeric values, bucket counts or all trend samples. Signed trends also use numerical text
    /// because the existing sparkline renderer scales from zero. Output uses the normal ConsoleMonster formatting route.</para>
    /// </summary>
    /// <example>
    /// <para>The ten busiest processes, highest CPU first</para>
    /// <code>Get-Process | Out-ConsoleChart -Property CPU -Label ProcessName -Sort Descending -Top 10</code>
    /// </example>
    /// <example>
    /// <para>Distribution of file sizes with a calculated property</para>
    /// <code>Get-ChildItem -File | Out-ConsoleChart -Property @{ Name = 'KB'; Expression = { $_.Length / 1KB } } -Type Histogram -Bins 8</code>
    /// </example>
    /// <example>
    /// <para>One trend per server, in timestamp order</para>
    /// <code>$samples | Sort-Object Time | Out-ConsoleChart -Property Latency -Label Server -Type Sparkline</code>
    /// </example>
    /// <example>
    /// <para>Capture a chart as plain text on Windows PowerShell 5.1 or PowerShell 7</para>
    /// <code>1, 3, 2, 5 | Out-ConsoleChart -Type Histogram -Bins 4 -AsString</code>
    /// </example>
    [Cmdlet(VerbsData.Out, "ConsoleChart")]
    [OutputType(typeof(string), typeof(PipelineChart))]
    public sealed class CmdletOutConsoleChart : PSCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();

        /// <summary><para type="description">Numbers or objects containing the numeric property, supplied directly or through the pipeline. Explicit null samples produce a non-terminating invalid-data error; no input produces no output.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">One numeric property name, wildcard matching one property, or calculated-property hashtable. Uses the raw numeric expression result regardless of FormatString. Defaults to Value for scalar input.</para></summary>
        [Parameter(Position = 0)]
        [ValidateNotNull]
        public object Property { get; set; } = "Value";

        /// <summary><para type="description">A property name or calculated-property hashtable for bar labels and sparkline group names. Defaults to Name, then row number for bars or one unnamed trend for scalars. Labels are literal text.</para></summary>
        [Parameter]
        [ValidateNotNull]
        public object Label { get; set; } = "Name";

        /// <summary><para type="description">Bar (default), Histogram or Sparkline. Each sparkline group is one line and preserves sample arrival order.</para></summary>
        [Parameter]
        public PipelineChartType Type { get; set; }

        /// <summary><para type="description">Show this many bars or sparkline groups after sorting. Zero (default) shows all. Histogram always uses every valid sample.</para></summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public int Top { get; set; }

        /// <summary><para type="description">None (default), Ascending or Descending by numeric value; trends use the latest sample. Ties retain input order. Ignored for Histogram.</para></summary>
        [Parameter]
        public PipelineChartSort Sort { get; set; }

        /// <summary><para type="description">Requested histogram bucket count, 1 through 128 (default 10). Small integral ranges and identical values use fewer buckets to avoid fractional or repeated edges.</para></summary>
        [Parameter]
        [ValidateRange(1, 128)]
        public int Bins { get; set; } = 10;

        /// <summary><para type="description">Unit suffix appended to numerical values and histogram edges, for example ' ms'.</para></summary>
        [Parameter]
        public string? Unit { get; set; }

        /// <summary><para type="description">Layout width in terminal cells; zero uses the current terminal width.</para></summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public int Width { get; set; }

        /// <summary><para type="description">Return one plain-text string for reliable assignment and redirection on every supported PowerShell version. Cannot be combined with AsWidget.</para></summary>
        [Parameter]
        public SwitchParameter AsString { get; set; }

        /// <summary><para type="description">Return the PipelineChart widget for layout composition or frame export. Cannot be combined with AsString.</para></summary>
        [Parameter]
        public SwitchParameter AsWidget { get; set; }

        /// <inheritdoc />
        protected override void BeginProcessing() {
            if (AsWidget.IsPresent && AsString.IsPresent) {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException("-AsWidget and -AsString cannot be combined."), "ConsoleMonsterChartOutput", ErrorCategory.InvalidArgument, null));
            }
        }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) { _items.Add(InputObject); }
            else if (MyInvocation.BoundParameters.ContainsKey(nameof(InputObject))) {
                WriteError(new ErrorRecord(new ArgumentException("Chart samples must have a finite numeric value; null is not a valid sample."), "ConsoleMonsterChartValue", ErrorCategory.InvalidData, null));
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_items.Count == 0) { return; }
            IReadOnlyList<PSObject> items = PSFormatting.Expand(_items, PSShaping.Options());
            if (items.Count == 0) { return; }
            BoundColumn value = Bind(items, Property);
            // Chart calculations consume numbers, not the culture-specific text used by table formatting.
            value.FormatString = null;
            BoundColumn? label = Type == PipelineChartType.Histogram ? null : Bind(items, Label);
            PipelineChart chart = new PipelineChart().WithType(Type).WithSort(Sort).WithTop(Top).WithBins(Bins).WithUnit(Unit);
            int row = 0;
            foreach (PSObject item in items) {
                row++;
                object? raw = PSText.Unwrap(value.GetValue(item));
                object? name = label == null ? null : PSText.Unwrap(label.GetValue(item));
                if (!PipelineChart.TryNumber(raw, out double number) || name is FieldError) {
                    WriteError(new ErrorRecord(new ArgumentException("Row " + row.ToString(CultureInfo.InvariantCulture) + " must have a finite numeric value and a readable label."), "ConsoleMonsterChartValue", ErrorCategory.InvalidData, item));
                    continue;
                }
                string text = Convert.ToString(name, CultureInfo.InvariantCulture) ?? string.Empty;
                if (text.Length == 0 && Type == PipelineChartType.Bar) { text = row.ToString(CultureInfo.InvariantCulture); }
                chart.Add(text, number);
            }
            if (chart.Samples.Count == 0) { return; }
            if (AsWidget.IsPresent) { WriteObject(chart); return; }
            PSFormatting.Emit(this, chart, Width, null, AsString.IsPresent);
        }

        private BoundColumn Bind(IReadOnlyList<PSObject> items, object property) {
            IReadOnlyList<BoundColumn> columns = PropertyBinder.Bind(items, new[] { property });
            if (columns.Count != 1) {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException("Chart property selections must resolve to exactly one column."), "ConsoleMonsterChartProperty", ErrorCategory.InvalidArgument, property));
            }
            return columns[0];
        }
    }
}
