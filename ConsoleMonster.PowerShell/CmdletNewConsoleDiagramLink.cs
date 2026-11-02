using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary><para type="synopsis">Connects two diagram components.</para><para type="description">From and To identify nodes by their label or explicit Id. The containing diagram creates undeclared endpoints. Labels are literal, Status uses the shared ConsoleMonster health states, and Both adds an arrow at each end.</para></summary>
    /// <example><para>A failing backup connection.</para><code>New-ConsoleDiagramLink 'File server' Backup -Label nightly -Status Fail -Style Dashed</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleDiagramLink")][OutputType(typeof(DiagramLink))]
    public sealed class CmdletNewConsoleDiagramLink : PSCmdlet {
        /// <summary><para type="description">The source node id.</para></summary>
        [Parameter(Mandatory = true, Position = 0)][ValidateNotNullOrEmpty] public string From { get; set; } = "";
        /// <summary><para type="description">The destination node id.</para></summary>
        [Parameter(Mandatory = true, Position = 1)][ValidateNotNullOrEmpty] public string To { get; set; } = "";
        /// <summary><para type="description">The connection text.</para></summary>
        [Parameter] public string? Label { get; set; }
        /// <summary><para type="description">The connection health.</para></summary>
        [Parameter] public StatusKind Status { get; set; } = StatusKind.Pass;
        /// <summary><para type="description">Solid, dashed or dotted stroke.</para></summary>
        [Parameter] public DiagramLinkStyle Style { get; set; }
        /// <summary><para type="description">Draws arrows at both ends.</para></summary>
        [Parameter] public SwitchParameter Both { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() => WriteObject(new DiagramLink(From, To) { Label = Label, Status = Status, Style = Style, Both = Both });
    }
}
