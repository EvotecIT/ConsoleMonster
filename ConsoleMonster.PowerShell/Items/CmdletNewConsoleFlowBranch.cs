using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary><para type="synopsis">Creates the single side branch below a flow strip.</para></summary>
    /// <example><code>New-ConsoleFlowBranch -From issuer -Label 'expired 2026-09-30' -Status Fail</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleFlowBranch")]
    [OutputType(typeof(FlowBranch))]
    public sealed class CmdletNewConsoleFlowBranch : PSCmdlet {
        /// <summary><para type="description">Source node key.</para></summary>
        [Parameter(Mandatory = true)] public string From { get; set; } = string.Empty;
        /// <summary><para type="description">Literal branch label.</para></summary>
        [Parameter(Position = 0, Mandatory = true)] public string Label { get; set; } = string.Empty;
        /// <summary><para type="description">Optional later node to return to; omitted to end the branch.</para></summary>
        [Parameter] public string? To { get; set; }
        /// <summary><para type="description">Optional branch status.</para></summary>
        [Parameter, Binding.StatusKindTransformation, ArgumentCompleter(typeof(Binding.StatusKindCompleter))] public StatusKind? Status { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() => WriteObject(new FlowBranch(From, Label, To, Status));
    }
}
