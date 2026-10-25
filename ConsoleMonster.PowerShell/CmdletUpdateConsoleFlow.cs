using System.Management.Automation;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Updates a node or edge in a live dashboard's flow strip.</para>
    /// <para type="description">Posts a status change to an existing flow in a running dashboard. WidgetId selects the flow; NodeKey or EdgeKey selects its item. Cut severs an edge, Cut:$false reconnects it, and omitting Cut preserves its connection state.</para>
    /// </summary>
    /// <example><para>Mark an issuer failure and sever its incoming connection</para><code>Update-ConsoleFlow -WidgetId chain -NodeKey issuer -Status Fail
    /// Update-ConsoleFlow -WidgetId chain -EdgeKey signature -Status Fail -Cut</code></example>
    [Cmdlet(VerbsData.Update, "ConsoleFlow")]
    public sealed class CmdletUpdateConsoleFlow : LiveCmdlet {
        /// <summary><para type="description">Flow widget identifier.</para></summary>
        [Parameter] public string WidgetId { get; set; } = "flow";
        /// <summary><para type="description">Node to update.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Node")] public string NodeKey { get; set; } = string.Empty;
        /// <summary><para type="description">Edge to update.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Edge")] public string EdgeKey { get; set; } = string.Empty;
        /// <summary><para type="description">New status.</para></summary>
        [Parameter(Mandatory = true), Binding.StatusKindTransformation, ArgumentCompleter(typeof(Binding.StatusKindCompleter))] public StatusKind Status { get; set; }
        /// <summary><para type="description">Cuts the edge; -Cut:$false reconnects it. Omitted preserves connectivity.</para></summary>
        [Parameter(ParameterSetName = "Edge")] public SwitchParameter Cut { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() {
            string operation = ParameterSetName == "Node" ? "node" : MyInvocation.BoundParameters.ContainsKey(nameof(Cut)) ? (Cut.IsPresent ? "cut" : "connect") : "edge";
            Send(new LiveEvent(WidgetId, operation, ParameterSetName == "Node" ? NodeKey : EdgeKey, state: Status.ToString()));
        }
    }
}
