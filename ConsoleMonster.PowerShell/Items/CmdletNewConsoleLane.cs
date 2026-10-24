using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates one lane (a worker, server or item of a parallel workload) for New-ConsoleLanes.</para>
    /// <para type="description">Use it inside the script block of New-ConsoleLanes when lanes differ: each has its own total, progress, status, result counts and detail. -Key names the lane for Update-ConsoleLane; the label defaults to the key and accepts rich text from New-ConsoleText. To add lanes to a widget made earlier, pipe it to Add-ConsoleItem.</para>
    /// </summary>
    /// <example>
    ///   <para>Three servers at different points</para>
    ///   <code>
    /// New-ConsoleLanes -Content {
    ///     New-ConsoleLane 'DC01' -Total 40 -Done 40 -Status Pass -Passed 40
    ///     New-ConsoleLane 'DC02' -Total 40 -Done 22 -Status Running -Passed 21 -Warned 1 -Detail 'replication'
    ///     New-ConsoleLane 'DC03' -Total 40
    /// } | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleLane")]
    [OutputType(typeof(Lane))]
    public sealed class CmdletNewConsoleLane : PSCmdlet {
        /// <summary><para type="description">The lane key, used by Update-ConsoleLane -Key.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">The label (defaults to the key; rich text from New-ConsoleText keeps its colours).</para></summary>
        [Parameter]
        public object? Label { get; set; }

        /// <summary><para type="description">Planned steps.</para></summary>
        [Parameter]
        public double Total { get; set; }

        /// <summary><para type="description">Steps done.</para></summary>
        [Parameter]
        public double Done { get; set; }

        /// <summary><para type="description">The status (default Pending): Pass, Fail, Warn, Skip, Running or Pending, or PowerShell's words.</para></summary>
        [Parameter]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind Status { get; set; } = StatusKind.Pending;

        /// <summary><para type="description">What the lane is doing now (shown literally; rich text keeps its colours).</para></summary>
        [Parameter]
        public object? Detail { get; set; }

        /// <summary><para type="description">Results that passed.</para></summary>
        [Parameter]
        public int Passed { get; set; }

        /// <summary><para type="description">Results that failed.</para></summary>
        [Parameter]
        public int Failed { get; set; }

        /// <summary><para type="description">Results with warnings.</para></summary>
        [Parameter]
        public int Warned { get; set; }

        /// <summary><para type="description">Results skipped.</para></summary>
        [Parameter]
        public int Skipped { get; set; }

        /// <summary><para type="description">When the lane started, measured from the start of the session. Preserved when added to New-ConsoleLanes or Add-ConsoleItem.</para></summary>
        [Parameter]
        public double? StartedSeconds { get; set; }
        private System.TimeSpan? Started => StartedSeconds.HasValue ? System.TimeSpan.FromSeconds(StartedSeconds.Value) : (System.TimeSpan?)null;

        /// <summary><para type="description">When the lane finished, measured from the start of the session. Together with -StartedSeconds, makes elapsed-time columns deterministic.</para></summary>
        [Parameter]
        public double? FinishedSeconds { get; set; }
        private System.TimeSpan? Finished => FinishedSeconds.HasValue ? System.TimeSpan.FromSeconds(FinishedSeconds.Value) : (System.TimeSpan?)null;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Text.RichText? rich = PSText.Rich(Label);
            Lane lane = new Lane(Key, Label == null ? Key : PSText.PlainText(Label)) {
                Total = Total,
                Done = Done,
                Status = Status,
                Passed = Passed,
                Failed = Failed,
                Warned = Warned,
                Skipped = Skipped,
                Started = Started,
                Finished = Finished
            };
            if (rich != null) {
                lane.LabelText = rich;
            }
            Text.RichText? detail = PSText.Rich(Detail);
            if (detail != null) {
                lane.DetailText = detail;
            } else if (Detail != null) {
                lane.Detail = PSWidgets.ToText(Detail);
            }
            WriteObject(lane);
        }
    }
}
