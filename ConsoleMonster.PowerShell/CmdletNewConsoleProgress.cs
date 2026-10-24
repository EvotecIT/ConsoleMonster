using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a labelled progress bar.</para>
    /// <para type="description">Shows a bar with sub-cell precision for the glyph tier, the percentage and value/total. -Status marks a finished bar (Pass, Fail, Warn, Skip) with its status colour. Update it live with Update-ConsoleProgress.</para>
    /// </summary>
    /// <example>
    ///   <para>A static progress bar</para>
    ///   <code>New-ConsoleProgress -Label 'Upload' -Value 64 -Total 100 | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Several bars, one of them finished</para>
    ///   <code>
    /// New-ConsoleRows -Content {
    ///     New-ConsoleProgress -Label 'Build' -Value 64 -Total 100
    ///     New-ConsoleProgress -Label 'Docs' -Value 100 -Total 100 -Status Pass
    ///     New-ConsoleProgress -Label 'Upload' -Value 3 -Total 8 -Shape Block
    /// } | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleProgress")]
    [OutputType(typeof(ProgressBar))]
    public sealed class CmdletNewConsoleProgress : PSCmdlet {
        /// <summary><para type="description">The label (markup allowed).</para></summary>
        [Parameter(Position = 1)]
        public object Label { get; set; } = string.Empty;

        /// <summary><para type="description">The current value.</para></summary>
        [Parameter]
        public double Value { get; set; }

        /// <summary><para type="description">The total (default 0: terminal width, falling back to 100).</para></summary>
        [Parameter]
        public double Total { get; set; } = 100;

        /// <summary><para type="description">The status (default Running): Pass, Fail, Warn or Skip mark a finished bar in that colour (PowerShell's words such as Success and Error work too).</para></summary>
        [Parameter]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public Styling.StatusKind Status { get; set; } = Styling.StatusKind.Running;

        /// <summary><para type="description">The live name (default progress).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "progress";

        /// <summary><para type="description">Bar shape: Line (thin stroke) or Block (solid blocks). Default Line.</para></summary>
        [Parameter]
        public BarShape Shape { get; set; } = BarShape.Line;

        /// <summary><para type="description">The columns of the row, in order: Standard, or any of Spinner, Description, Bar, Percent, Count, Elapsed, Remaining, Rate, ByteRate, Results and Status. Rate and Remaining are smoothed over the last 10 seconds. Without it the classic label, bar, percentage and count show.</para></summary>
        [Parameter]
        [ValidateSet("Standard", "Spinner", "Description", "Bar", "Percent", "Count", "Elapsed", "Remaining", "Rate", "ByteRate", "Results", "Status", IgnoreCase = true)]
        public string[]? Column { get; set; }

        /// <summary><para type="description">Shared count-column settings from New-ConsoleColumnFormat.</para></summary>
        [Parameter] public Layout.ColumnFormat? CountColumn { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            ProgressBar progress = new ProgressBar(PSText.Markup(Label) ?? string.Empty, WidgetId) { Value = Value, Total = Total, Status = Status, Shape = Shape, Columns = Column == null ? null : ProgressColumns.FromNames(Column) };
            if (CountColumn != null) { progress.CountColumn = CountColumn; }
            WriteObject(progress);
        }
    }
}
