using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a tree node with an optional status, a muted detail and child nodes, for New-ConsoleTree.</para>
    /// <para type="description">Use it inside the script block of New-ConsoleTree, or inside another New-ConsoleTreeNode to nest. The label accepts markup or rich text from New-ConsoleText. -Status shows a status marker before the label (Pass, Fail, Warn, Skip, Info, Running, Pending, or PowerShell's words such as Success, Warning and Error). -Detail is a muted note after the label, and -Value a value formatted by a presenter (sizes, dates, durations). The script block emits the children: more New-ConsoleTreeNode calls, or strings for leaves (shown literally). To add nodes to a tree made earlier, pipe the tree to Add-ConsoleItem.</para>
    /// </summary>
    /// <example>
    ///   <para>A mail domain with records and their state</para>
    ///   <code>
    /// New-ConsoleTree '[bold]evotec.xyz[/]' -Content {
    ///     New-ConsoleTreeNode 'MX' -Status Pass -Detail '2 records' -Content {
    ///         New-ConsoleTreeNode 'evotec-xyz.mail.protection.outlook.com' -Detail 'priority 0'
    ///     }
    ///     New-ConsoleTreeNode 'DMARC' -Status Fail -Detail 'p=none'
    /// } | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Leaves from a list of names</para>
    ///   <code>
    /// New-ConsoleTree 'Domain controllers' -Content {
    ///     New-ConsoleTreeNode 'ad.adatum.com' -Content { 'DC01', 'DC02' }
    ///     New-ConsoleTreeNode 'eu.ad.adatum.com' -Status Warn -Detail 'one offline' -Content { 'DC07' }
    /// } | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>A value shown through a presenter</para>
    ///   <code>
    /// New-ConsoleTree 'Reports share' -Content {
    ///     New-ConsoleTreeNode 'Weekly' -Value 734003200 -Presenter Bytes
    ///     New-ConsoleTreeNode 'Archive' -Value 9663676416 -Presenter Bytes -Status Warn
    /// } | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleTreeNode")]
    [OutputType(typeof(TreeNode))]
    public sealed class CmdletNewConsoleTreeNode : PSCmdlet {
        /// <summary><para type="description">The label (markup allowed; rich text from New-ConsoleText keeps its colours).</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public object Label { get; set; } = string.Empty;

        /// <summary><para type="description">A script block that emits the child nodes: New-ConsoleTreeNode calls, or strings for leaves (shown literally).</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">A status marker before the label: Pass, Fail, Warn, Skip, Info, Running or Pending (PowerShell's words such as Success, Warning and Error work too).</para></summary>
        [Parameter]
        [Binding.StatusKindTransformation]
        [ArgumentCompleter(typeof(Binding.StatusKindCompleter))]
        public StatusKind? Status { get; set; }

        /// <summary><para type="description">A muted note after the label, for example a count or a short reason (shown literally).</para></summary>
        [Parameter]
        public object? Detail { get; set; }

        /// <summary><para type="description">A value after the label, formatted by -Presenter or by the presenter its type suggests (dates, sizes, durations, booleans).</para></summary>
        [Parameter]
        public object? Value { get; set; }

        /// <summary><para type="description">How -Value is shown: Bytes, Duration, Date, Number, Boolean, Status, Progress, DataBar, Link, Markup or Text.</para></summary>
        [Parameter]
        [ArgumentCompleter(typeof(Binding.PresenterNameCompleter))]
        public string? Presenter { get; set; }

        /// <summary><para type="description">A unit for -Value with the Number presenter, for example ms.</para></summary>
        [Parameter]
        public string? Unit { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            TreeNode node = new TreeNode(PSText.Markup(Label) ?? string.Empty, Status) {
                Note = Detail == null ? null : PSWidgets.ToText(Detail)
            };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Value))) {
                object? value = PSText.Unwrap(Value);
                node.Value = TableCell.FromValue(value);
                node.Presenter = string.IsNullOrEmpty(Presenter) ? Presenters.CellPresenters.ForLabel(PSText.PlainText(Label), value) : Presenters.CellPresenters.FromName(Presenter!, Unit);
            }
            PSItems.AddAll(this, node, PSItems.Items(Content));
            WriteObject(node);
        }
    }
}
