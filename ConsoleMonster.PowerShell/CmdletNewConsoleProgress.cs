using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a labelled progress bar.</para>
    /// <para type="description">Shows a bar with sub-cell precision for the glyph tier, the percentage and value/total. Update it live with Update-ConsoleProgress.</para>
    /// <example>
    ///   <para>A static progress bar</para>
    ///   <code>New-ConsoleProgress -Label 'Upload' -Value 64 -Total 100 | Write-ConsoleWidget</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleProgress")]
    [OutputType(typeof(ProgressBar))]
    public sealed class CmdletNewConsoleProgress : PSCmdlet {
        /// <summary><para type="description">The label (markup allowed).</para></summary>
        [Parameter(Position = 0)]
        public object Label { get; set; } = string.Empty;

        /// <summary><para type="description">The current value.</para></summary>
        [Parameter]
        public double Value { get; set; }

        /// <summary><para type="description">The total (default 100).</para></summary>
        [Parameter]
        public double Total { get; set; } = 100;

        /// <summary><para type="description">The live name (default progress).</para></summary>
        [Parameter]
        public string Name { get; set; } = "progress";

        /// <summary><para type="description">Bar shape: Line (thin stroke) or Block (solid blocks). Default Line.</para></summary>
        [Parameter]
        public BarShape Shape { get; set; } = BarShape.Line;

        /// <summary><para type="description">The columns of the row, in order: Standard, or any of Spinner, Description, Bar, Percent, Count, Elapsed, Remaining, Rate, ByteRate, Results and Status. Rate and Remaining are smoothed over the last 10 seconds. Without it the classic label, bar, percentage and count show.</para></summary>
        [Parameter]
        [ValidateSet("Standard", "Spinner", "Description", "Bar", "Percent", "Count", "Elapsed", "Remaining", "Rate", "ByteRate", "Results", "Status", IgnoreCase = true)]
        public string[]? Column { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            WriteObject(new ProgressBar(PSText.Markup(Label) ?? string.Empty, Name) { Value = Value, Total = Total, Shape = Shape, Columns = Column == null ? null : ProgressColumns.FromNames(Column) });
        }
    }
}
