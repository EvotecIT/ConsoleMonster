using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates a diagram component.</summary>
    /// <remarks><para>Label is the connection identity unless Id is set. Subtitle is the second line and Value is the third. To adds unlabelled outgoing connections; missing destination nodes are created by the diagram.</para></remarks>
    /// <example><para>A monitored gateway.</para><code>New-ConsoleDiagramNode Router -Kind Gateway -Subtitle '192.168.1.1' -Value '1 ms' -Status Pass -To Internet</code></example>
    /// <example>
    /// <para>Compose an inventory diagram node</para>
    /// <code>New-ConsoleDiagram -Title 'Request path' -Content {
    ///     New-ConsoleDiagramNode -Id api -Label 'Orders API' -Kind Service
    /// } | Show-Console
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleDiagramNode", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolediagramnode/")][OutputType(typeof(DiagramNode))]
    public sealed class CmdletNewConsoleDiagramNode : PSCmdlet {
        /// <summary><para type="description">The component's displayed name and default id.</para></summary>
        [Parameter(Mandatory = true, Position = 0)][ValidateNotNullOrEmpty] public string Label { get; set; } = "";
        /// <summary><para type="description">An explicit connection and live-update key.</para></summary>
        [Parameter][ValidateNotNullOrEmpty] public string? Id { get; set; }
        /// <summary><para type="description">The component kind and icon.</para></summary>
        [Parameter] [PSDefaultValue(Help = "Service")]
        public DiagramNodeKind Kind { get; set; }
        /// <summary><para type="description">Secondary text shown below the diagram node title.</para></summary>
        [Parameter] public string? Subtitle { get; set; }
        /// <summary><para type="description">The component health. Omit it to show no status marker.</para></summary>
        [Parameter] [PSDefaultValue(Help = "None")]
        public StatusKind Status { get; set; } = StatusKind.None;
        /// <summary><para type="description">The independent third line.</para></summary>
        [Parameter] public string? Value { get; set; }
        /// <summary><para type="description">Destination node ids for outgoing connections.</para></summary>
        [Parameter] public string[] To { get; set; } = System.Array.Empty<string>();
        /// <inheritdoc />
        protected override void ProcessRecord() {
            var node = new DiagramNode(Label) { Kind = Kind, Subtitle = Subtitle, Value = Value };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Status))) node.Status = Status;
            if (Id != null) node.Id = Id;
            node.To.AddRange(To);
            WriteObject(node);
        }
    }
}
