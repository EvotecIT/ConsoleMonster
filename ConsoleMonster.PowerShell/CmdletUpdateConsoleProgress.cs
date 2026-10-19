using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Updates a progress bar in a running dashboard.</para>
    /// <para type="description">-Value sets the value, -Increment adds to it, -Label renames the bar and -Status sets a final state that recolours it.</para>
    /// <example>
    ///   <para>Advance by one</para>
    ///   <code>Update-ConsoleProgress -Increment 1</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsData.Update, "ConsoleProgress")]
    public sealed class CmdletUpdateConsoleProgress : LiveCmdlet {
        /// <summary><para type="description">The progress bar name (default progress).</para></summary>
        [Parameter(Position = 0)]
        public string Name { get; set; } = "progress";

        /// <summary><para type="description">The new value.</para></summary>
        [Parameter]
        public double? Value { get; set; }

        /// <summary><para type="description">A new total.</para></summary>
        [Parameter]
        public double Total { get; set; }

        /// <summary><para type="description">Amount to add to the value.</para></summary>
        [Parameter]
        public double Increment { get; set; }

        /// <summary><para type="description">A new label.</para></summary>
        [Parameter]
        public object? Label { get; set; }

        /// <summary><para type="description">A final status (Pass, Fail, Warn).</para></summary>
        [Parameter]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind? Status { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (Value.HasValue || Total > 0) {
                Send(new LiveEvent(Name, "set", value: Value ?? 0, total: Total));
            }
            if (Increment != 0) {
                Send(new LiveEvent(Name, "advance", value: Increment));
            }
            if (Label != null) {
                Send(new LiveEvent(Name, "label", text: PSText.Markup(Label)));
            }
            if (Status.HasValue) {
                Send(new LiveEvent(Name, "state", state: Status.Value.ToString()));
            }
        }
    }
}
