using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Connects two diagram components.</summary>
    /// <remarks><para>From and To identify nodes by their label or explicit Id. The containing diagram creates undeclared endpoints. Labels are literal, Status uses the shared ConsoleMonster health states, and Both adds an arrow at each end.</para></remarks>
    /// <example><para>A failing backup connection.</para><code>New-ConsoleDiagramLink 'File server' Backup -Label nightly -Status Fail -Style Dashed</code></example>
    /// <example>
    /// <para>Compose an inventory diagram link</para>
    /// <code>New-ConsoleDiagram -Title 'Service dependencies' -Content {
    ///     New-ConsoleDiagramNode -Id api -Label API
    ///     New-ConsoleDiagramNode -Id db -Label Database
    ///     New-ConsoleDiagramLink -From api -To db -Label SQL
    /// } | Show-Console
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleDiagramLink", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolediagramlink/")][OutputType(typeof(DiagramLink))]
    public sealed class CmdletNewConsoleDiagramLink : PSCmdlet {
        /// <summary><para type="description">The source node id.</para></summary>
        [Parameter(Mandatory = true, Position = 0)][ValidateNotNullOrEmpty] public string From { get; set; } = "";
        /// <summary><para type="description">The destination node id.</para></summary>
        [Parameter(Mandatory = true, Position = 1)][ValidateNotNullOrEmpty] public string To { get; set; } = "";
        /// <summary><para type="description">Label displayed beside the connection between the two diagram nodes.</para></summary>
        [Parameter] public string? Label { get; set; }
        /// <summary><para type="description">The connection health. Omit it to show no status marker.</para></summary>
        [Parameter] [PSDefaultValue(Help = "None")]
        public StatusKind Status { get; set; } = StatusKind.None;
        /// <summary><para type="description">Solid, dashed or dotted stroke.</para></summary>
        [Parameter] [PSDefaultValue(Help = "Solid")]
        public DiagramLinkStyle Style { get; set; }
        /// <summary><para type="description">Draws arrows at both ends.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$false")]
        public SwitchParameter Both { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() {
            var link = new DiagramLink(From, To) { Label = Label, Style = Style, Both = Both };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Status))) link.Status = Status;
            WriteObject(link);
        }
    }
}
