using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates one status item, a step with a status and a detail, for New-ConsoleStatusList and New-ConsolePhases.</para>
    /// <para type="description">Use it inside the script block of New-ConsoleStatusList (a checklist) or New-ConsolePhases (a strip of stages). -Status is the shared status vocabulary: Pass, Fail, Warn, Skip, Info, Running or Pending, and PowerShell's words such as Success, Warning and Error. -Detail is a muted note on the right, such as a duration. The label is shown literally; rich text from New-ConsoleText keeps its colours. -Key names the item for live updates (Update-ConsoleStatus, Update-ConsolePhase) when it differs from the label. To add items to a list made earlier, pipe the list to Add-ConsoleItem.</para>
    /// </summary>
    /// <example>
    ///   <para>A build checklist</para>
    ///   <code>
    /// New-ConsoleStatusList {
    ///     New-ConsoleStatusItem 'Restore NuGet packages' -Status Pass -Detail '1.6 s'
    ///     New-ConsoleStatusItem 'Run Pester on PowerShell 7' -Status Warn -Detail '1 skipped'
    ///     New-ConsoleStatusItem 'Publish to PowerShell Gallery' -Status Pending
    /// } | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Phases of a run</para>
    ///   <code>
    /// New-ConsolePhases {
    ///     New-ConsoleStatusItem 'Discover' -Status Pass -Detail '2.1s'
    ///     New-ConsoleStatusItem 'Evaluate' -Status Running -Detail '64%'
    ///     New-ConsoleStatusItem 'Report'
    /// } | Write-ConsoleWidget</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleStatusItem")]
    [OutputType(typeof(StatusItem))]
    public sealed class CmdletNewConsoleStatusItem : PSCmdlet {
        /// <summary><para type="description">The label (shown literally; rich text from New-ConsoleText keeps its colours).</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public object Label { get; set; } = string.Empty;

        /// <summary><para type="description">The status (default Pending): Pass, Fail, Warn, Skip, Info, Running or Pending, or PowerShell's words such as Success, Warning and Error.</para></summary>
        [Parameter(Position = 1)]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind Status { get; set; } = StatusKind.Pending;

        /// <summary><para type="description">A muted detail on the right, for example a duration or a count.</para></summary>
        [Parameter]
        public object? Detail { get; set; }

        /// <summary><para type="description">The key for live updates (Update-ConsoleStatus -Key, Update-ConsolePhase -Key); defaults to the label's text.</para></summary>
        [Parameter]
        public string? Key { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            string key = string.IsNullOrEmpty(Key) ? PSText.PlainText(Label) : Key!;
            WriteObject(new StatusItem(key, PSText.LiteralMarkup(Label) ?? string.Empty, Status, Detail == null ? null : PSWidgets.ToText(Detail)));
        }
    }
}
