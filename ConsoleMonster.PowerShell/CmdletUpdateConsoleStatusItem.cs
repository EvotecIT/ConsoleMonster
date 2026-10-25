using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Updates an item of a status list in a running dashboard.</para>
    /// <para type="description">Adds the item when it does not exist. -Detail shows a right-aligned muted note such as a duration.</para>
    /// </summary>
    /// <example>
    ///   <para>Mark a step as passed with its duration</para>
    ///   <code>Update-ConsoleStatusItem -Key restore -Status Pass -Detail '1.6s'</code>
    /// </example>
    [Cmdlet(VerbsData.Update, "ConsoleStatusItem")]
    public sealed class CmdletUpdateConsoleStatusItem : LiveCmdlet {
        /// <summary><para type="description">The item key.</para></summary>
        [Parameter(Position = 1, Mandatory = true)]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">The new status.</para></summary>
        [Parameter]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind Status { get; set; } = StatusKind.Running;

        /// <summary><para type="description">The item label (markup allowed).</para></summary>
        [Parameter]
        public object? Label { get; set; }

        /// <summary><para type="description">A right-aligned detail.</para></summary>
        [Parameter]
        public string? Detail { get; set; }

        /// <summary><para type="description">The status list name (default status).</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = "status";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Send(new LiveEvent(WidgetId, "set", Key, PSText.Markup(Label), Status.ToString()));
            if (Detail != null) {
                Send(new LiveEvent(WidgetId, "detail", Key, Detail));
            }
        }
    }
}
