using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a histogram of values counted into buckets.</para>
    /// <para type="description">-Edge defines ascending bucket edges. Bars grow with eighth-block precision on modern fonts. Add values live with Add-ConsoleValue.</para>
    /// </summary>
    /// <example>
    ///   <para>Request durations in seconds</para>
    ///   <code>New-ConsoleHistogram -Edge 0, 0.25, 0.5, 1, 2, 4 -Unit s -Value (1..200 | ForEach-Object { Get-Random -Maximum 4.0 }) | Write-ConsoleWidget</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleHistogram")]
    [OutputType(typeof(Histogram))]
    public sealed class CmdletNewConsoleHistogram : PSCmdlet {
        /// <summary><para type="description">Ascending bucket edges (at least two).</para></summary>
        [Parameter(Mandatory = true)]
        public double[] Edge { get; set; } = new double[0];

        /// <summary><para type="description">Values to count.</para></summary>
        [Parameter]
        public double[]? Value { get; set; }

        /// <summary><para type="description">Rows for the bars (default 6).</para></summary>
        [Parameter]
        public int Height { get; set; } = 6;

        /// <summary><para type="description">A unit for bucket labels.</para></summary>
        [Parameter]
        public string? Unit { get; set; }

        /// <summary><para type="description">The colour as a theme role: Series1 (default), Accent, Pass, Fail, Warn and the other series colours.</para></summary>
        [Parameter]
        public Styling.ThemeToken? Tone { get; set; }

        /// <summary><para type="description">The live name (default histogram).</para></summary>
        [Parameter]
        public string Name { get; set; } = "histogram";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Histogram histogram = new Histogram(Edge, Name) { Height = Height, Unit = Unit };
            if (Tone.HasValue) {
                histogram.Token = Tone.Value;
            }
            if (Value != null) {
                foreach (double value in Value) {
                    histogram.Add(value);
                }
            }
            WriteObject(histogram);
        }
    }
}
