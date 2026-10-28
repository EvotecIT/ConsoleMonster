using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates one numeric chart representation from records or samples.</para>
    /// <para type="description">Type selects Bar, Stacked, Line, Scatter, Histogram or Sparkline. The shared C# Chart factory rejects options that do not apply to the selected kind. InputObject accepts chart records (Label and Value; Key, Label, Values and Role for series); Content emits the same records. For Bar, Histogram and Sparkline, -Property picks the number from ordinary objects and -Label (Bar only, default Name) their label, as Show-Console -View Chart does. Use Update-Console for live changes.</para>
    /// </summary>
    /// <example><para>Chart a property of ordinary objects inside a report</para><code>$servers = [pscustomobject]@{ Name = 'web01'; Cpu = 42 }, [pscustomobject]@{ Name = 'web02'; Cpu = 27 }; $servers | New-ConsoleChart -Property Cpu -Label Name -Unit '%'</code></example>
    /// <example><para>Show resolver latency</para><code>[pscustomobject] @{ Label = 'Cloudflare'; Value = 12 } | New-ConsoleChart -Type Bar -Unit ' ms' | Show-Console</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleChart")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleChart : PSCmdlet {
        private readonly List<object> _items = new List<object>();
        private int _nullInputs;
        /// <summary><para type="description">The numeric chart kind: Bar, Stacked, Line, Scatter, Histogram or Sparkline.</para></summary>
        [Parameter]
        public ChartType Type { get; set; } = ChartType.Bar;
        /// <summary><para type="description">The live identifier; omitted uses the selected kind default.</para></summary>
        [Parameter(Position = 0)]
        public string? WidgetId { get; set; }
        /// <summary><para type="description">The unit on numeric labels.</para></summary>
        [Parameter]
        public string? Unit { get; set; }
        /// <summary><para type="description">The Sparkline caption shown before the trend.</para></summary>
        [Parameter]
        public object? Title { get; set; }
        /// <summary><para type="description">For Bar, Histogram or Sparkline: the numeric property of ordinary input objects (a name, pattern or calculated property).</para></summary>
        [Parameter]
        public object[]? Property { get; set; }
        /// <summary><para type="description">For Bar with -Property: the label property or expression; default Name.</para></summary>
        [Parameter]
        public object Label { get; set; } = "Name";
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
            else if (MyInvocation.BoundParameters.ContainsKey(nameof(InputObject))) { _nullInputs++; }
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
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Title))) { options.Title = PSText.Markup(Title); }
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
            int skipped = _nullInputs;
            if (Property != null && Property.Length > 0) { skipped += AddSelected(options); }
            else {
                if (MyInvocation.BoundParameters.ContainsKey(nameof(Label))) { throw new PSArgumentException("Label picks the bar label of ordinary objects; use it with -Property."); }
                PSItems.ChartData(options, _items);
            }
            if (skipped > 0) { WriteWarning(skipped.ToString(System.Globalization.CultureInfo.InvariantCulture) + " rows without chartable values skipped."); }
            WriteObject(Chart.Create(options));
        }

        // Ordinary objects: the same property binding as Show-Console -View Chart, ending in the typed AddItem/Values inputs.
        private int AddSelected(ChartOptions options) {
            if (Type != ChartType.Bar && Type != ChartType.Histogram && Type != ChartType.Sparkline) { throw new PSArgumentException("Property applies to Bar, Histogram and Sparkline charts; Line, Stacked and Scatter take series records."); }
            if (Property!.Length != 1) { throw new PSArgumentException("Chart requires exactly one Property."); }
            if (Type != ChartType.Bar && MyInvocation.BoundParameters.ContainsKey(nameof(Label))) { throw new PSArgumentException("Label applies to Bar charts."); }
            var input = new List<PSObject>();
            foreach (object item in _items) { input.Add(PSObject.AsPSObject(item)); }
            IReadOnlyList<PSObject> rows = PSFormatting.Expand(input, Binding.PSShaping.Options());
            Binding.BoundColumn value = Column(rows, Property[0]);
            Binding.BoundColumn? label = Type == ChartType.Bar ? Column(rows, Label) : null;
            var samples = new List<double>(options.Values ?? Array.Empty<double>());
            var labels = new HashSet<string>(StringComparer.Ordinal);
            int row = 0;
            int skipped = 0;
            foreach (PSObject item in rows) {
                row++;
                object? raw = PSText.Unwrap(value.GetValue(item));
                object? name = label == null ? null : PSText.Unwrap(label.GetValue(item));
                if (!PipelineChart.TryNumber(raw, out double number) || name is Values.FieldError) {
                    skipped++;
                    continue;
                }
                if (label == null) { samples.Add(number); continue; }
                string text = Convert.ToString(name, System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
                options.AddItem(PipelineChart.UniqueLabel(text.Length == 0 ? row.ToString(System.Globalization.CultureInfo.InvariantCulture) : text, labels), number);
            }
            if (Type != ChartType.Bar) { options.Values = samples.ToArray(); }
            return skipped;
        }

        private static Binding.BoundColumn Column(IReadOnlyList<PSObject> rows, object property) {
            IReadOnlyList<Binding.BoundColumn> columns = Binding.PropertyBinder.Bind(rows, new[] { property });
            if (columns.Count != 1) { throw new PSArgumentException("Chart property selections must resolve to exactly one column."); }
            columns[0].FormatString = null;
            return columns[0];
        }
    }
}
