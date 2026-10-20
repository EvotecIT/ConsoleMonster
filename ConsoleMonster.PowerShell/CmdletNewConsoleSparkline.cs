using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a sparkline: a compact trend of recent values.</para>
    /// <para type="description">One row by default (▁▂▄▆█ on modern fonts, shades on Consolas, an ASCII ramp in ASCII mode); with -Height above 1 it becomes a small area chart. Values come from -Value or the pipeline. -Tone sets the colour, -Maximum the top of the scale and -NoLatest hides the latest value. To add values later, pipe it to Add-ConsoleItem; in a live dashboard, append values with Add-ConsoleValue.</para>
    /// </summary>
    /// <example>
    ///   <para>A labelled trend</para>
    ///   <code>New-ConsoleSparkline -Label 'req/s' -Value 3, 5, 8, 13, 9, 7, 12 | Write-ConsoleWidget</code>
    /// </example>
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

        /// <summary><para type="description">Values from the pipeline, oldest first.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public double[]? InputObject { get; set; }

        /// <summary><para type="description">The colour as a theme role: Accent (default), Accent2, Pass, Fail, Warn or Series1 to Series6.</para></summary>
        [Parameter]
        public Styling.ThemeToken? Tone { get; set; }

        /// <summary><para type="description">The value at the top of the scale (default: the largest value).</para></summary>
        [Parameter]
        public double? Maximum { get; set; }

        /// <summary><para type="description">Hide the latest value after the trend.</para></summary>
        [Parameter]
        public SwitchParameter NoLatest { get; set; }

        private readonly System.Collections.Generic.List<double> _values = new System.Collections.Generic.List<double>();

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _values.AddRange(InputObject);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Sparkline sparkline = new Sparkline(Name) { Label = PSText.Markup(Label), Height = Height, Unit = Unit, Max = Maximum, ShowLatest = !NoLatest.IsPresent };
            if (Tone.HasValue) {
                sparkline.Token = Tone.Value;
            }
            if (Value != null) {
                sparkline.AddRange(Value);
            }
            sparkline.AddRange(_values);
            WriteObject(sparkline);
        }
    }
}
