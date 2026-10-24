using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Updates a spinner in a running dashboard.</para>
    /// <para type="description">-Label renames it, -Detail changes the muted detail, -Restart restarts its clock and returns it to running, and -Status sets a final state that replaces the animation with the status marker.</para>
    /// </summary>
    /// <example>
    ///   <para>Report progress, then finish</para>
    ///   <code>
    /// Update-ConsoleSpinner -Detail 'zone 4'
    /// Update-ConsoleSpinner -Status Fail -Detail 'zone 7 timed out'</code>
    /// </example>
    [Cmdlet(VerbsData.Update, "ConsoleSpinner")]
    public sealed class CmdletUpdateConsoleSpinner : LiveCmdlet {
        /// <summary><para type="description">The spinner name (default spinner).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "spinner";

        /// <summary><para type="description">A new label.</para></summary>
        [Parameter]
        public object? Label { get; set; }

        /// <summary><para type="description">A new detail.</para></summary>
        [Parameter]
        public object? Detail { get; set; }

        /// <summary><para type="description">Restart the clock and return to running.</para></summary>
        [Parameter]
        public SwitchParameter Restart { get; set; }

        /// <summary><para type="description">A final status (Pass, Fail, Warn, Skip).</para></summary>
        [Parameter]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind? Status { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Restart.IsPresent) {
                Send(new LiveEvent(WidgetId, "start", text: PSText.Markup(Label)));
            } else if (Label != null) {
                Send(new LiveEvent(WidgetId, "label", text: PSText.Markup(Label)));
            }
            if (Status.HasValue) {
                Send(new LiveEvent(WidgetId, "state", text: PSText.Markup(Detail), status: Status.Value.ToString()));
            } else if (Detail != null) {
                Send(new LiveEvent(WidgetId, "detail", text: PSText.Markup(Detail)));
            }
        }
    }
}
