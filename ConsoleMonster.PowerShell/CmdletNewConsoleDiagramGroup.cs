using System;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Groups components and nested diagram groups.</summary>
    /// <remarks><para>Content emits New-ConsoleDiagramNode and New-ConsoleDiagramGroup objects. The grouped tree keeps all children at narrow widths. Boundary controls the topology outline.</para></remarks>
    /// <example><para>Office components.</para><code>New-ConsoleDiagramGroup 'Office LAN' { New-ConsoleDiagramNode '12 PCs' -Kind Workstation -To Firewall }</code></example>
    /// <example>
    /// <para>Compose an inventory diagram group</para>
    /// <code>New-ConsoleDiagram -Title 'Production services' -Content {
    ///     New-ConsoleDiagramGroup -Label Production -Content { New-ConsoleDiagramNode -Id api -Label 'Orders API' }
    /// } | Show-Console
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleDiagramGroup", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolediagramgroup/")][OutputType(typeof(DiagramGroup))]
    public sealed class CmdletNewConsoleDiagramGroup : PSCmdlet {
        /// <summary><para type="description">Visible heading for the diagram group that contains related nodes.</para></summary>
        [Parameter(Mandatory = true, Position = 0)][ValidateNotNullOrEmpty] public string Label { get; set; } = "";
        /// <summary><para type="description">Emits nodes and nested groups.</para></summary>
        [Parameter(Position = 1)] public ScriptBlock? Content { get; set; }
        /// <summary><para type="description">Whether to draw the group outline.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$true")]
        public bool Boundary { get; set; } = true;
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
