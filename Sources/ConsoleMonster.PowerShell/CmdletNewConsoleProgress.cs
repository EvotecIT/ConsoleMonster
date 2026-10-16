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
        public string Label { get; set; } = string.Empty;

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

        /// <inheritdoc />
        protected override void ProcessRecord() {
            WriteObject(new ProgressBar(Label, Name) { Value = Value, Total = Total, Shape = Shape });
        }
    }
}
