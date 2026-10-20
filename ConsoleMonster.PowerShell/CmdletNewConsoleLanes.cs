using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a lanes widget: one row per worker, server or item of a parallel workload.</para>
    /// <para type="description">Each lane shows a status marker (a spinner while running), a progress bar, pass/fail/warning counts and what it is doing now. Lanes can be declared up front with -Lane (names sharing one -Total), with a script block of New-ConsoleLane calls (each with its own total, progress, status and counts), from the pipeline, or appear on their first Update-ConsoleLane in a live dashboard. To add lanes later, pipe the widget to Add-ConsoleItem.</para>
    /// </summary>
    /// <example>
    ///   <para>Lanes for three servers with 40 steps each</para>
    ///   <code>New-ConsoleLanes -Name servers -Lane 'DC01', 'DC02', 'DC03' -Total 40</code>
    /// </example>
    /// <example>
    ///   <para>Lanes at different points, printed once</para>
    ///   <code>
    /// New-ConsoleLanes {
    ///     New-ConsoleLane 'DC01' -Total 40 -Done 40 -Status Pass -Passed 40
    ///     New-ConsoleLane 'DC02' -Total 40 -Done 22 -Status Running -Passed 21 -Warned 1 -Detail 'replication'
    ///     New-ConsoleLane 'DC03' -Total 40
    /// } | Write-ConsoleWidget</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleLanes")]
    [OutputType(typeof(Lanes))]
    public sealed class CmdletNewConsoleLanes : PSCmdlet {
        /// <summary><para type="description">The live name (default lanes). A script block here is taken as -Content.</para></summary>
        [Parameter(Position = 0)]
        public object Name { get; set; } = "lanes";

        /// <summary><para type="description">A script block that emits the lanes: New-ConsoleLane calls, or names.</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Lanes from the pipeline: New-ConsoleLane objects or names.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        private readonly System.Collections.Generic.List<object> _piped = new System.Collections.Generic.List<object>();

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
            if (InputObject != null) {
                _piped.AddRange(PSItems.Items(InputObject));
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            object? name = PSText.Unwrap(Name);
            ScriptBlock? content = name is ScriptBlock script ? script : Content;
            Lanes lanes = new Lanes(name is ScriptBlock || name == null ? "lanes" : PSWidgets.ToText(name)) {
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
            PSItems.AddAll(this, lanes, PSItems.Items(content));
            PSItems.AddAll(this, lanes, _piped);
            WriteObject(lanes);
        }
    }
}
