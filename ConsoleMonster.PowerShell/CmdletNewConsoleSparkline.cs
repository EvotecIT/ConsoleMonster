using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a sparkline: a compact trend of recent values.</para>
    /// <para type="description">One row by default (▁▂▄▆█ on modern fonts, shades on Consolas, an ASCII ramp in ASCII mode); with -Height above 1 it becomes a small area chart. Append values live with Add-ConsoleValue.</para>
    /// <example>
    ///   <para>A labelled trend</para>
    ///   <code>New-ConsoleSparkline -Label 'req/s' -Value 3, 5, 8, 13, 9, 7, 12 | Write-ConsoleWidget</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleSparkline")]
    [OutputType(typeof(Sparkline))]
    public sealed class CmdletNewConsoleSparkline : PSCmdlet {
        /// <summary><para type="description">The live name (default sparkline).</para></summary>
        [Parameter(Position = 0)]
        public string Name { get; set; } = "sparkline";

        /// <summary><para type="description">Initial values, oldest first.</para></summary>
        [Parameter]
        public double[]? Value { get; set; }

        /// <summary><para type="description">A muted label on the left.</para></summary>
        [Parameter]
        public object? Label { get; set; }

        /// <summary><para type="description">Rows (default 1).</para></summary>
        [Parameter]
        public int Height { get; set; } = 1;

        /// <summary><para type="description">A unit appended to the latest value.</para></summary>
        [Parameter]
        public string? Unit { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Sparkline sparkline = new Sparkline(Name) { Label = PSText.Markup(Label), Height = Height, Unit = Unit };
            if (Value != null) {
                sparkline.AddRange(Value);
            }
            WriteObject(sparkline);
        }
    }
}
