using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Updates a phase of a running dashboard's phase strip.</para>
    /// <para type="description">Set -Status Running when a phase starts; its duration is measured until it gets a final status (Pass, Fail, Warn, Skip). -Detail replaces the shown detail (for example a percentage).</para>
    /// </summary>
    /// <example>
    ///   <para>Start and finish a phase</para>
    ///   <code>Update-ConsolePhase -Key Build -Status Running; dotnet build; Update-ConsolePhase -Key Build -Status Pass</code>
    /// </example>
    [Cmdlet(VerbsData.Update, "ConsolePhase")]
    public sealed class CmdletUpdateConsolePhase : LiveCmdlet {
        /// <summary><para type="description">The phase key.</para></summary>
        [Parameter(Position = 1, Mandatory = true)]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">The new status.</para></summary>
        [Parameter]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind? Status { get; set; }

        /// <summary><para type="description">A detail such as a percentage or duration.</para></summary>
        [Parameter]
        public string? Detail { get; set; }

        /// <summary><para type="description">The phases widget name (default phases).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "phases";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Status.HasValue) {
                Send(new LiveEvent(WidgetId, "state", Key, Detail, Status.Value.ToString()));
            } else if (Detail != null) {
                Send(new LiveEvent(WidgetId, "detail", Key, Detail));
            }
        }
    }
}
