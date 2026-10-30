using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a lanes widget: one row per worker, server or item of a parallel workload.</para>
    /// <para type="description">Each lane shows a status marker (a spinner while running), a progress bar, pass/fail/warning counts and what it is doing now. Declare lanes with names or Key/Label/Total records emitted by Content or the pipeline. Records can include progress, status and counts. A lane can also appear on its first Update-Console call in a live dashboard.</para>
    /// </summary>
    /// <example>
    ///   <para>Lanes for three servers with 40 steps each</para>
    ///   <code>'DC01', 'DC02', 'DC03' | ForEach-Object { @{ Key = $_; Label = $_; Total = 40 } } | New-ConsoleLanes -WidgetId servers</code>
    /// </example>
    /// <example>
    ///   <para>Lanes at different points, printed once</para>
    ///   <code>
    /// New-ConsoleLanes -Content {
    ///     [pscustomobject]@{ Key = 'DC01'; Total = 40; Status = 'Pass'; Passed = 40; Value = 40 }
    ///     [pscustomobject]@{ Key = 'DC02'; Total = 40; Status = 'Running'; Passed = 21; Warned = 1; Detail = 'replication'; Value = 22 }
    ///     [pscustomobject]@{ Key = 'DC03'; Total = 40 }
    /// } | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleLanes")]
    [OutputType(typeof(Lanes))]
    public sealed class CmdletNewConsoleLanes : PSCmdlet {
        /// <summary><para type="description">The live name (default lanes). A script block here is taken as -Content.</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "lanes";

        /// <summary><para type="description">A script block that emits the lanes: Key/Label records calls, or names.</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Lanes from the pipeline: Key/Label records objects or names.</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        private readonly System.Collections.Generic.List<object> _piped = new System.Collections.Generic.List<object>();

        /// <summary><para type="description">Bar shape: Line (thin stroke) or Block (solid blocks). Default Line.</para></summary>
        [Parameter]
        public BarShape Shape { get; set; } = BarShape.Line;

        /// <summary><para type="description">Builtin names, Type/Alignment/Priority/MaxWidth records or typed columns, in order: Standard (spinner, description, bar, percent, count, elapsed, remaining time, status), or any of Spinner, Description, Bar, Percent, Count, Elapsed, Remaining, Rate, ByteRate, Results and Status. Without it the classic layout shows the label, bar, percentage, pass/fail/warning counts and detail.</para></summary>
        [Parameter]
        public object[]? Columns { get; set; }

        /// <summary><para type="description">Finished lanes leave the display and are counted in the summary line.</para></summary>
        [Parameter]
        public SwitchParameter HideCompleted { get; set; }

        /// <summary><para type="description">Collapses the display to one summary line once every lane has finished.</para></summary>
        [Parameter]
        public SwitchParameter CollapseWhenFinished { get; set; }

        /// <summary><para type="description">The display disappears once every lane has finished (with Invoke-ConsoleDashboard -Transient nothing is left).</para></summary>
        [Parameter]
        public SwitchParameter Transient { get; set; }

        /// <summary><para type="description">Shared CountColumn layout settings as a hashtable, for example @{ MinWidth = 6; Alignment = 'Right' }.</para></summary>
        [Parameter] public Layout.ColumnFormat? CountColumn { get; set; }
        /// <summary><para type="description">Shared PercentColumn layout settings as a hashtable, for example @{ MinWidth = 6; Alignment = 'Right' }.</para></summary>
        [Parameter] public Layout.ColumnFormat? PercentColumn { get; set; }

        /// <summary><para type="description">Maximum lane label width in cells; default 28.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)] public int MaxLabelWidth { get; set; } = 28;
        /// <summary><para type="description">Hides each lane's detail text.</para></summary>
        [Parameter] public SwitchParameter NoDetail { get; set; }
        /// <summary><para type="description">Includes skipped work in the completed count.</para></summary>
        [Parameter] public SwitchParameter CountSkipped { get; set; }
        /// <summary><para type="description">Uses normal text instead of dimming pending lanes.</para></summary>
        [Parameter] public SwitchParameter NoDimPending { get; set; }
        /// <summary><para type="description">Emphasizes active lane labels.</para></summary>
        [Parameter] public SwitchParameter BoldActive { get; set; }
        /// <summary><para type="description">Shows a separate count of running lanes.</para></summary>
        [Parameter] public SwitchParameter ShowRunningCount { get; set; }
        /// <summary><para type="description">Collapses lanes with this status into a summary.</para></summary>
        [Parameter] public Styling.StatusKind? CollapseWhen { get; set; }
        /// <summary><para type="description">Rate-estimation window in seconds; default 10.</para></summary>
        [Parameter, ValidateRange(0.001, 86400)] public double RateWindowSeconds { get; set; } = 10;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _piped.AddRange(PSItems.Items(InputObject));
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            ScriptBlock? content = Content;
            Lanes lanes = new Lanes(WidgetId) {
                Shape = Shape, MaxLabelWidth = MaxLabelWidth, ShowDetail = !NoDetail.IsPresent,
                CountSkipped = CountSkipped.IsPresent, DimPending = !NoDimPending.IsPresent,
                BoldActive = BoldActive.IsPresent, ShowRunningCount = ShowRunningCount.IsPresent,
                CollapseWhen = CollapseWhen, RateWindow = System.TimeSpan.FromSeconds(RateWindowSeconds),
                Columns = PSProgressColumns.Build(Columns),
                HideCompleted = HideCompleted.IsPresent,
                CollapseWhenFinished = CollapseWhenFinished.IsPresent,
                Transient = Transient.IsPresent
            };
            PSItems.AddAll(this, lanes, PSItems.Items(content));
            PSItems.AddAll(this, lanes, _piped);
            if (CountColumn != null) { lanes.CountColumn = CountColumn; }
            if (PercentColumn != null) { lanes.PercentColumn = PercentColumn; }
            WriteObject(lanes);
        }
    }
}
