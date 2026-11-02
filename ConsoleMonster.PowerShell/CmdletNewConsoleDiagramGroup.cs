using System;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary><para type="synopsis">Groups components and nested diagram groups.</para><para type="description">Content emits New-ConsoleDiagramNode and New-ConsoleDiagramGroup objects. The grouped tree keeps all children at narrow widths. Boundary controls the topology outline.</para></summary>
    /// <example><para>Office components.</para><code>New-ConsoleDiagramGroup 'Office LAN' { New-ConsoleDiagramNode '12 PCs' -Kind Workstation -To Firewall }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleDiagramGroup")][OutputType(typeof(DiagramGroup))]
    public sealed class CmdletNewConsoleDiagramGroup : PSCmdlet {
        /// <summary><para type="description">The group label.</para></summary>
        [Parameter(Mandatory = true, Position = 0)][ValidateNotNullOrEmpty] public string Label { get; set; } = "";
        /// <summary><para type="description">Emits nodes and nested groups.</para></summary>
        [Parameter(Position = 1)] public ScriptBlock? Content { get; set; }
        /// <summary><para type="description">Whether to draw the group outline.</para></summary>
        [Parameter] public bool Boundary { get; set; } = true;
        /// <inheritdoc />
        protected override void ProcessRecord() {
            var group = new DiagramGroup(Label) { Boundary = Boundary };
            foreach (object item in PSItems.Items(Content)) {
                if (item is DiagramNode node) group.Nodes.Add(node);
                else if (item is DiagramGroup child) group.Groups.Add(child);
                else throw new ArgumentException("A diagram group accepts nodes and nested groups.");
            }
            WriteObject(group);
        }
    }
}
