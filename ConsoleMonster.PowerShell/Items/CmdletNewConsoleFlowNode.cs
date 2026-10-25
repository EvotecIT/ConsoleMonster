using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a labelled flow node with an optional status glyph and colour.</para>
    /// <para type="description">Builds a node for New-ConsoleFlow or Add-ConsoleItem. Key is its stable identity for live updates; Label is literal text. Status uses the shared vocabulary and remains readable through a glyph when colour is disabled.</para>
    /// </summary>
    /// <example><para>A client hop that has completed</para><code>New-ConsoleFlowNode -Key client -Label 'Client' -Status Pass</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleFlowNode")]
    [OutputType(typeof(FlowNode))]
    public sealed class CmdletNewConsoleFlowNode : PSCmdlet {
        /// <summary><para type="description">Stable node key.</para></summary>
        [Parameter(Mandatory = true)] public string Key { get; set; } = string.Empty;
        /// <summary><para type="description">Literal node label.</para></summary>
        [Parameter(Position = 0, Mandatory = true)] public string Label { get; set; } = string.Empty;
        /// <summary><para type="description">Optional node status.</para></summary>
        [Parameter, Binding.StatusKindTransformation, ArgumentCompleter(typeof(Binding.StatusKindCompleter))] public StatusKind? Status { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() => WriteObject(new FlowNode(Key, Label, Status));
    }
}
