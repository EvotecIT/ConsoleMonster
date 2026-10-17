using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Updates a lane of a running dashboard: progress, step results, status and detail.</para>
    /// <para type="description">With -Result, counts one completed step (or -Steps steps) as passed, failed, warning or skipped and advances the bar. With -Status, sets the lane state (Running, Pass, Fail, Warn, Skip). -Complete finishes the lane (Fail if any step failed, Warn if any warned, otherwise Pass). Safe to call from any runspace or thread.</para>
    /// <example>
    ///   <para>Record a passed step and say what comes next</para>
    ///   <code>Update-ConsoleLane -Key DC01 -Result Pass -Detail 'Checking replication'</code>
    /// </example>
    /// <example>
    ///   <para>Finish a lane</para>
    ///   <code>Update-ConsoleLane -Key DC01 -Complete</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsData.Update, "ConsoleLane")]
    public sealed class CmdletUpdateConsoleLane : LiveCmdlet {
        /// <summary><para type="description">The lane key (created on first use).</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">The lanes widget name (default lanes).</para></summary>
        [Parameter]
        public string Name { get; set; } = "lanes";

        /// <summary><para type="description">Result of completed steps.</para></summary>
        [Parameter]
        public StatusKind? Result { get; set; }

        /// <summary><para type="description">Steps completed with this result (default 1).</para></summary>
        [Parameter]
        public double Steps { get; set; } = 1;

        /// <summary><para type="description">Set the lane status.</para></summary>
        [Parameter]
        public StatusKind? Status { get; set; }

        /// <summary><para type="description">What the lane is doing now.</para></summary>
        [Parameter]
        public object? Detail { get; set; }

        /// <summary><para type="description">The lane label (when adding or renaming).</para></summary>
        [Parameter]
        public object? Label { get; set; }

        /// <summary><para type="description">Planned steps for the lane.</para></summary>
        [Parameter]
        public double Total { get; set; }

        /// <summary><para type="description">Finish the lane.</para></summary>
        [Parameter]
        public SwitchParameter Complete { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            string? detail = PSText.EventText(Detail, out bool detailMarkup);
            if (Label != null || Total > 0) {
                string? label = PSText.EventText(Label, out bool labelMarkup);
                Send(new LiveEvent(Name, "add", Key, label, total: Total, isMarkup: labelMarkup));
            }
            if (Status.HasValue) {
                Send(new LiveEvent(Name, "state", Key, detail, Status.Value.ToString(), isMarkup: detailMarkup));
            } else if (Result.HasValue) {
                Send(new LiveEvent(Name, "advance", Key, detail, Result.Value.ToString(), Steps, isMarkup: detailMarkup));
            } else if (Detail != null) {
                Send(new LiveEvent(Name, "detail", Key, detail, isMarkup: detailMarkup));
            }
            if (Complete.IsPresent) {
                Send(new LiveEvent(Name, "complete", Key));
            }
        }
    }
}
