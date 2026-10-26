using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates one numeric chart representation from records or samples.</para>
    /// <para type="description">Type selects Bar, Stacked, Line, Scatter, Histogram or Sparkline. The shared C# Chart factory rejects options that do not apply to the selected kind. InputObject accepts ordinary records; Content emits the same records. Use Update-Console for live changes.</para>
    /// </summary>
    /// <example><para>Show resolver latency</para><code>[pscustomobject] @{ Label = 'Cloudflare'; Value = 12 } | New-ConsoleChart -Type Bar -Unit ' ms' | Show-Console</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleChart")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleChart : PSCmdlet {
        private readonly List<object> _items = new List<object>();
        /// <summary><para type="description">The numeric chart kind: Bar, Stacked, Line, Scatter, Histogram or Sparkline.</para></summary>
        [Parameter(Mandatory = true)]
        public ChartType Type { get; set; }
        /// <summary><para type="description">The live identifier; omitted uses the selected kind default.</para></summary>
        [Parameter(Position = 0)]
        public string? WidgetId { get; set; }
        /// <summary><para type="description">The unit on numeric labels.</para></summary>
        [Parameter]
        public string? Unit { get; set; }
        /// <summary><para type="description">The sparkline label.</para></summary>
        [Parameter]
        public object? Label { get; set; }
        /// <summary><para type="description">Plot rows; omitted uses the selected kind default.</para></summary>
        [Parameter]
        public int? Height { get; set; }
        /// <summary><para type="description">The lower numeric limit.</para></summary>
        [Parameter]
        public double? Minimum { get; set; }
        /// <summary><para type="description">The upper numeric limit.</para></summary>
        [Parameter]
        public double? Maximum { get; set; }
        /// <summary><para type="description">The chart theme role.</para></summary>
        [Parameter]
        public Styling.ThemeToken? Role { get; set; }
        /// <summary><para type="description">The bar shape, Block by default.</para></summary>
        [Parameter]
        public BarShape Shape { get; set; } = BarShape.Block;
        /// <summary><para type="description">The warning threshold for Bar.</para></summary>
        [Parameter]
        public double? Warning { get; set; }
        /// <summary><para type="description">The critical threshold for Bar.</para></summary>
        [Parameter]
        public double? Critical { get; set; }
        /// <summary><para type="description">Hides the latest Sparkline value.</para></summary>
        [Parameter]
        public SwitchParameter NoLatest { get; set; }
        /// <summary><para type="description">Strictly ascending Histogram bucket edges.</para></summary>
        [Parameter]
        public double[]? Edges { get; set; }
        /// <summary><para type="description">Initial numeric samples for Histogram or Sparkline.</para></summary>
        [Parameter]
        public double[]? Values { get; set; }
        /// <summary><para type="description">Bar numeric-column settings.</para></summary>
        [Parameter]
        public Layout.ColumnFormat? ValueColumn { get; set; }
        /// <summary><para type="description">Stacked count-column settings.</para></summary>
        [Parameter]
        public Layout.ColumnFormat? CountColumn { get; set; }
        /// <summary><para type="description">Stacked percent-column settings.</para></summary>
        [Parameter]
        public Layout.ColumnFormat? PercentColumn { get; set; }
        /// <summary><para type="description">Stacked legend-label settings.</para></summary>
        [Parameter]
        public Layout.ColumnFormat? LabelColumn { get; set; }
        /// <summary><para type="description">Records, numeric samples or a dictionary of labels to values/series.</para></summary>
        [Parameter(ValueFromPipeline = true, Position = 1)] public PSObject? InputObject { get; set; }
        /// <summary><para type="description">A script block emitting the same records accepted by InputObject.</para></summary>
        [Parameter] public ScriptBlock? Content { get; set; }
        /// <summary><para type="description">Initial Line/Scatter series names, or records with Key, Label, Role and Values.</para></summary>
        [Parameter] public object[]? Series { get; set; }

        /// <summary><para type="description">Hides Bar-chart numeric value labels.</para></summary>
        [Parameter] public SwitchParameter NoValues { get; set; }
        /// <summary><para type="description">Shows signed Bar values on both sides of zero.</para></summary>
        [Parameter] public SwitchParameter Signed { get; set; }
        /// <summary><para type="description">Hides the Stacked-chart legend.</para></summary>
        [Parameter] public SwitchParameter NoLegend { get; set; }
        /// <summary><para type="description">Maximum Stacked legend columns; zero fits the available width.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] public int MaxLegendColumns { get; set; }
        /// <summary><para type="description">Retained Line, Scatter or Sparkline samples; omitted uses the selected kind default.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)] public int? Capacity { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) { _items.AddRange(PSItems.Items(InputObject)); }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            var options = new ChartOptions { Type = Type };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(NoValues))) { options.ShowValues = !NoValues.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Signed))) { options.Signed = Signed.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(NoLegend))) { options.ShowLegend = !NoLegend.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(MaxLegendColumns))) { options.MaxLegendColumns = MaxLegendColumns; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Capacity))) { options.Capacity = Capacity; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(WidgetId))) { options.WidgetId = WidgetId; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Unit))) { options.Unit = Unit; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Label))) { options.Label = PSText.Markup(Label); }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Height))) { options.Height = Height; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Minimum))) { options.Minimum = Minimum; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Maximum))) { options.Maximum = Maximum; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Role))) { options.Role = Role; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Shape))) { options.Shape = Shape; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Warning))) { options.Warning = Warning; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Critical))) { options.Critical = Critical; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(NoLatest))) { options.ShowLatest = !NoLatest.IsPresent; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Edges))) { options.Edges = Edges; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Values))) { options.Values = Values; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(ValueColumn))) { options.ValueColumn = ValueColumn; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(CountColumn))) { options.CountColumn = CountColumn; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(PercentColumn))) { options.PercentColumn = PercentColumn; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(LabelColumn))) { options.LabelColumn = LabelColumn; }
            if (Series != null) {
                if (Type != ChartType.Line && Type != ChartType.Scatter) { throw new PSArgumentException("Series applies to Line and Scatter charts."); }
                PSItems.ChartData(options, PSItems.Items(Series));
            }
            _items.AddRange(PSItems.Items(Content));
            PSItems.ChartData(options, _items);
            WriteObject(Chart.Create(options));
        }
    }
}
