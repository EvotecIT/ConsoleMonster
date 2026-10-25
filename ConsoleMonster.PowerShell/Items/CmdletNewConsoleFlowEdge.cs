using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a labelled connection between adjacent flow nodes.</para>
    /// <para type="description">Builds an edge after its source and destination nodes have been added to a flow. From and To identify adjacent nodes in strip order. Label appears above the arrow; Cut severs the line with a cross, or x in ASCII mode.</para>
    /// </summary>
    /// <example><para>A successful TLS connection</para><code>New-ConsoleFlowEdge -Key tls -From client -To host -Label 'TLS 1.3' -Status Pass</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleFlowEdge")]
    [OutputType(typeof(FlowEdge))]
    public sealed class CmdletNewConsoleFlowEdge : PSCmdlet {
        /// <summary><para type="description">Stable edge key.</para></summary>
        [Parameter(Mandatory = true)] public string Key { get; set; } = string.Empty;
        /// <summary><para type="description">Source node key.</para></summary>
        [Parameter(Mandatory = true)] public string From { get; set; } = string.Empty;
        /// <summary><para type="description">Next node key.</para></summary>
        [Parameter(Mandatory = true)] public string To { get; set; } = string.Empty;
        /// <summary><para type="description">Literal label above the arrow.</para></summary>
        [Parameter] public string? Label { get; set; }
        /// <summary><para type="description">Optional edge status.</para></summary>
        [Parameter, Binding.StatusKindTransformation, ArgumentCompleter(typeof(Binding.StatusKindCompleter))] public StatusKind? Status { get; set; }
        /// <summary><para type="description">Draws a severed connection, using x in ASCII mode.</para></summary>
        [Parameter] public SwitchParameter Cut { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() => WriteObject(new FlowEdge(Key, From, To, Label, Status, Cut.IsPresent));
    }
}
