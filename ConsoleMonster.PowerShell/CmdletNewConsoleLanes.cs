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
        public object[]? Lane { get; set; }

        /// <summary><para type="description">Planned steps per declared lane.</para></summary>
        [Parameter]
        public double Total { get; set; }

        /// <summary><para type="description">Bar shape: Line (thin stroke) or Block (solid blocks). Default Line.</para></summary>
        [Parameter]
        public BarShape Shape { get; set; } = BarShape.Line;

        /// <summary><para type="description">The columns of each row, in order: Standard (spinner, description, bar, percent, count, elapsed, remaining time, status), or any of Spinner, Description, Bar, Percent, Count, Elapsed, Remaining, Rate, ByteRate, Results and Status. Without it the classic layout shows the label, bar, percentage, pass/fail/warning counts and detail.</para></summary>
        [Parameter]
        [ValidateSet("Standard", "Spinner", "Description", "Bar", "Percent", "Count", "Elapsed", "Remaining", "Rate", "ByteRate", "Results", "Status", IgnoreCase = true)]
        public string[]? Column { get; set; }

        /// <summary><para type="description">Finished lanes leave the display and are counted in the summary line.</para></summary>
        [Parameter]
        public SwitchParameter HideCompleted { get; set; }

        /// <summary><para type="description">Collapses the display to one summary line once every lane has finished.</para></summary>
        [Parameter]
        public SwitchParameter CollapseWhenFinished { get; set; }

        /// <summary><para type="description">The display disappears once every lane has finished (with Start-ConsoleDashboard -Transient nothing is left).</para></summary>
        [Parameter]
        public SwitchParameter Transient { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Lanes lanes = new Lanes(Name) {
                Shape = Shape,
                Columns = Column == null ? null : ProgressColumns.FromNames(Column),
                HideCompleted = HideCompleted.IsPresent,
                CollapseWhenFinished = CollapseWhenFinished.IsPresent,
                Transient = Transient.IsPresent
            };
            if (Lane != null) {
                foreach (object lane in Lane) {
                    Text.RichText? rich = PSText.Rich(lane);
                    if (rich != null) {
                        lanes.Add(rich.PlainText, rich, Total);
                    } else {
                        string label = PSText.PlainText(lane);
                        lanes.Add(label, label, Total);
                    }
                }
            }
            WriteObject(lanes);
        }
    }
}
