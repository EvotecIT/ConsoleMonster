using System.Management.Automation;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Sends a raw live event to any widget of a running dashboard.</para>
    /// <para type="description">The general form behind the Update-* cmdlets, for custom widgets and less common operations (for example the dashboard's title or status). Operations are defined by each widget.</para>
    /// </summary>
    /// <example>
    ///   <para>Change the dashboard status in the header</para>
    ///   <code>Send-ConsoleEvent -Target dashboard -Operation status -Text 'deploying wave 2' -State running</code>
    /// </example>
    [Cmdlet(VerbsCommunications.Send, "ConsoleEvent")]
    public sealed class CmdletSendConsoleEvent : LiveCmdlet {
        /// <summary><para type="description">The widget name.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Target { get; set; } = string.Empty;

        /// <summary><para type="description">The operation (advance, state, set, add, write, title, status...).</para></summary>
        [Parameter(Position = 1, Mandatory = true)]
        public string Operation { get; set; } = string.Empty;

        /// <summary><para type="description">The item key.</para></summary>
        [Parameter]
        public string? Key { get; set; }

        /// <summary><para type="description">Text.</para></summary>
        [Parameter]
        public string? Text { get; set; }

        /// <summary><para type="description">A state word.</para></summary>
        [Parameter]
        public string? State { get; set; }

        /// <summary><para type="description">A numeric value.</para></summary>
        [Parameter]
        public double Value { get; set; }

        /// <summary><para type="description">A second numeric value (total, maximum).</para></summary>
        [Parameter]
        public double Total { get; set; }

        /// <summary><para type="description">Milliseconds since the run began. Zero lets the session stamp the event; supply the original time when forwarding timestamped updates.</para></summary>
        [Parameter]
        [ValidateRange(0, long.MaxValue)]
        public long TimeMilliseconds { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Send(new LiveEvent(Target, Operation, Key, Text, State, Value, Total, TimeMilliseconds));
        }
    }
}
