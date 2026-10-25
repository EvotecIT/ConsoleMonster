using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Builds a native flow strip of nodes, labelled edges and one side branch.</para>
    /// <para type="description">Emit nodes first, then edges and an optional branch. Pipeline rows use Kind (Node, Edge or Branch) and the item command's property names. Labels are literal. The strip becomes vertical in a narrow terminal.</para>
    /// </summary>
    /// <example><para>A certificate path with a broken connection</para><code>
    /// New-ConsoleFlow {
    ///     New-ConsoleFlowNode -Key leaf -Label 'Leaf' -Status Pass
    ///     New-ConsoleFlowNode -Key issuer -Label 'Intermediate' -Status Fail
    ///     New-ConsoleFlowEdge -Key chain -From leaf -To issuer -Label 'expired' -Status Fail -Cut
    /// }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleFlow")]
    [OutputType(typeof(Flow))]
    public sealed class CmdletNewConsoleFlow : PSCmdlet {
        private readonly List<object> _items = new List<object>();
        /// <summary><para type="description">Emits New-ConsoleFlowNode, New-ConsoleFlowEdge and New-ConsoleFlowBranch items.</para></summary>
        [Parameter(Position = 0)] public ScriptBlock? Content { get; set; }
        /// <summary><para type="description">Items or plain rows with Kind and the corresponding item properties.</para></summary>
        [Parameter(ValueFromPipeline = true)] public PSObject? InputObject { get; set; }
        /// <summary><para type="description">Identifier used by Update-ConsoleFlow (default flow).</para></summary>
        [Parameter] public string WidgetId { get; set; } = "flow";
        /// <inheritdoc />
        protected override void ProcessRecord() { if (InputObject != null) _items.AddRange(PSItems.Items(InputObject)); }
        /// <inheritdoc />
        protected override void EndProcessing() {
            var flow = new Flow(WidgetId);
            PSItems.AddAll(this, flow, PSItems.Items(Content));
            PSItems.AddAll(this, flow, _items);
            WriteObject(flow);
        }
    }
}
