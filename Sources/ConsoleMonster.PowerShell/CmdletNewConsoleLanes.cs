using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a lanes widget: one row per worker, server or item of a parallel workload.</para>
    /// <para type="description">Each lane shows a status marker (a spinner while running), a progress bar, pass/fail/warning counts and what it is doing now. Lanes can be declared up front with -Lane or appear on their first Update-ConsoleLane.</para>
    /// <example>
    ///   <para>Lanes for three servers with 40 steps each</para>
    ///   <code>New-ConsoleLanes -Name servers -Lane 'DC01', 'DC02', 'DC03' -Total 40</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleLanes")]
    [OutputType(typeof(Lanes))]
    public sealed class CmdletNewConsoleLanes : PSCmdlet {
        /// <summary><para type="description">The live name (default lanes).</para></summary>
        [Parameter(Position = 0)]
        public string Name { get; set; } = "lanes";

        /// <summary><para type="description">Lanes to declare up front (key and label).</para></summary>
        [Parameter]
        public string[]? Lane { get; set; }

        /// <summary><para type="description">Planned steps per declared lane.</para></summary>
        [Parameter]
        public double Total { get; set; }

        /// <summary><para type="description">Bar shape: Line (thin stroke) or Block (solid blocks). Default Line.</para></summary>
        [Parameter]
        public BarShape Shape { get; set; } = BarShape.Line;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Lanes lanes = new Lanes(Name) { Shape = Shape };
            if (Lane != null) {
                foreach (string lane in Lane) {
                    lanes.Add(lane, lane, Total);
                }
            }
            WriteObject(lanes);
        }
    }
}
