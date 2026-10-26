using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Builds a native flow strip of nodes, labelled edges and one side branch.</para>
    /// <para type="description">Emit nodes first, then edges and an optional branch. Pipeline rows use Kind (Node, Edge or Branch), Key, Label, Status, and the edge/branch From and To fields. Labels are literal. The strip becomes vertical in a narrow terminal.</para>
    /// </summary>
    /// <example><para>A certificate path with a broken connection</para><code>
    /// New-ConsoleFlow -Content {
    ///     [pscustomobject] @{ Kind = 'Node'; Key = 'leaf'; Label = 'Leaf'; Status = 'Pass' }
    ///     [pscustomobject] @{ Kind = 'Node'; Key = 'issuer'; Label = 'Intermediate'; Status = 'Fail' }
    ///     [pscustomobject] @{ Kind = 'Edge'; Key = 'chain'; From = 'leaf'; To = 'issuer'; Label = 'expired'; Status = 'Fail'; Cut = $true }
    /// }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleFlow")]
    [OutputType(typeof(Flow))]
    public sealed class CmdletNewConsoleFlow : PSCmdlet {
        private readonly List<object> _items = new List<object>();
        /// <summary><para type="description">Emits Node, Edge and Branch records with the documented Kind and fields.</para></summary>
        [Parameter(Position = 1)] public ScriptBlock? Content { get; set; }
        /// <summary><para type="description">Items or plain rows with Kind and the corresponding item properties.</para></summary>
        [Parameter(ValueFromPipeline = true)] public PSObject? InputObject { get; set; }
        /// <summary><para type="description">Identifier used by Update-Console (default flow).</para></summary>
        [Parameter(Position = 0)] public string WidgetId { get; set; } = "flow";
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
