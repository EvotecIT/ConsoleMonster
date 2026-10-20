using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Updates an item of a status list in a running dashboard.</para>
    /// <para type="description">Adds the item when it does not exist. -Detail shows a right-aligned muted note such as a duration.</para>
    /// <example>
    ///   <para>Mark a step as passed with its duration</para>
    ///   <code>Update-ConsoleStatus -Key restore -Status Pass -Detail '1.6s'</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsData.Update, "ConsoleStatus")]
    public sealed class CmdletUpdateConsoleStatus : LiveCmdlet {
        /// <summary><para type="description">The item key.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
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
        [Parameter]
        public string Name { get; set; } = "status";

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Send(new LiveEvent(Name, "set", Key, PSText.Markup(Label), Status.ToString()));
            if (Detail != null) {
                Send(new LiveEvent(Name, "detail", Key, Detail));
            }
        }
    }
}
