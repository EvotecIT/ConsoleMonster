using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a console tree from nested Label/Children records calls, hierarchical objects or nested hashtables.</para>
    /// <para type="description">Three ways in, one tree out. Nodes: a script block of Label/Children records calls, each with its own status, detail and children (strings become leaves shown literally); nodes can also be piped in. Objects: pipe hierarchical objects (groups and members, folders and files) and name their children with -ChildrenProperty or a -Children script block ($_ is the parent); -LabelProperty, -StatusProperty and -DetailProperty pick what each node shows, and -Depth, -MaxItems and -MaxNodes keep a large or cyclic hierarchy bounded and mark what was left out (a repeated item is shown once with "(cycle)"). Hashtables: -InputObject maps keys to nested hashtables (child nodes), arrays (leaves) or other values (a muted note); use [ordered] to keep order. The title (markup allowed) is the root; without it the top-level nodes are the roots.</para>
    /// </summary>
    /// <example>
    ///   <para>A mail domain with records and their state</para>
    ///   <code>
    /// New-ConsoleTree '[bold]evotec.xyz[/]' -Content {
    ///     [pscustomobject]@{ Label = 'MX'; Status = 'Pass'; Detail = '2 records'; Children = @(&amp; {
    ///         [pscustomobject]@{ Label = 'evotec-xyz.mail.protection.outlook.com'; Detail = 'priority 0' }
    ///     }) }
    ///     [pscustomobject]@{ Label = 'SPF'; Status = 'Warn'; Detail = '9 of 10 DNS lookups'; Children = @(&amp; {
    ///         'include:spf.protection.outlook.com'
    ///         'include:_spf.salesforce.com'
    ///     }) }
    ///     [pscustomobject]@{ Label = 'DMARC'; Status = 'Fail'; Detail = 'p=none' }
    /// } | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Folders and files from the pipeline, three levels deep</para>
    ///   <code>Get-Item . | New-ConsoleTree -Children { if ($_.PSIsContainer) { Get-ChildItem $_.FullName } } -Depth 3 | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>Hierarchical objects with a status per node</para>
    ///   <code>
    /// $groups = @(
    ///     [pscustomobject] @{ Name = 'Domain Admins'; Health = 'Warn'; Note = '6 members'; Members = @(
    ///         [pscustomobject] @{ Name = 'Tier 0 operators'; Health = 'Pass'; Members = @([pscustomobject] @{ Name = 'anna.k' }) }
    ///         [pscustomobject] @{ Name = 'svc-backup'; Health = 'Fail'; Note = 'password never expires' }
    ///     ) }
    /// )
    /// $groups | New-ConsoleTree 'Privileged groups' -ChildrenProperty Members -StatusProperty Health -DetailProperty Note | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>A DNS delegation from nested hashtables</para>
    ///   <code>New-ConsoleTree -Title 'evotec.xyz' -InputObject ([ordered] @{ NS = @('dana.ns.cloudflare.com', 'kirk.ns.cloudflare.com'); MX = [ordered] @{ 'mail.protection.outlook.com' = '52.101.68.17' } }) | Show-Console</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleTree", DefaultParameterSetName = NodesSet)]
    [OutputType(typeof(Tree))]
    public partial class CmdletNewConsoleTree : PSCmdlet {
        private const string NodesSet = "Nodes";
        private const string ObjectsSet = "Objects";
        private readonly List<object> _objects = new List<object>();

        /// <summary><para type="description">The root label (markup allowed; rich text from New-ConsoleText keeps its colours). Without it the top-level nodes are the roots.</para></summary>
        [Parameter(ParameterSetName = NodesSet, Position = 0)]
        [Parameter(ParameterSetName = ObjectsSet, Position = 0)]
        public object? Title { get; set; }

        /// <summary><para type="description">A script block that emits the nodes: Label/Children records calls, or strings for leaves (shown literally).</para></summary>
        [Parameter(ParameterSetName = NodesSet, Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Hierarchical objects (pipeline input); each becomes a top-level node. Nodes from Label/Children records go in as they are.</para></summary>
        [Parameter(ParameterSetName = ObjectsSet, Mandatory = true, ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">The property that holds an object's children (for example Members or Children).</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public string? ChildrenProperty { get; set; }

        /// <summary><para type="description">A script block that returns an object's children; $_ is the object. Use it when children have to be looked up (Get-ChildItem, Get-ADGroupMember).</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public ScriptBlock? Children { get; set; }

        /// <summary><para type="description">The property shown as the node label (default: the object's name, as tables title a record: Name, DisplayName, Title...).</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public string? LabelProperty { get; set; }

        /// <summary><para type="description">The property whose value is the node status (pass, fail, warn, skip... or PowerShell's words).</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public string? StatusProperty { get; set; }

        /// <summary><para type="description">The property shown as a muted note after the label.</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public string? DetailProperty { get; set; }

        /// <summary><para type="description">Levels of children to follow: New-ConsoleTree defaults to 5. Show-Console -View Tree defaults to 3 for object graphs, or one for wide .NET objects when Depth is omitted.</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public int Depth { get; set; } = 5;

        /// <summary><para type="description">The most children listed per node, and objects listed at the top, before "+N more" (default 25; 0 lists all).</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public int MaxItems { get; set; } = 25;

        /// <summary><para type="description">The most nodes the tree may grow to (default 5000; 0 for no limit); beyond it a "node limit reached" note ends the walk.</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public int MaxNodes { get; set; } = 5000;

        /// <summary><para type="description">Wraps node labels and values with hanging indentation.</para></summary>
        [Parameter] public SwitchParameter WrapLabels { get; set; }
        /// <summary><para type="description">Hides the default-on root when a title was supplied.</para></summary>
        [Parameter] public SwitchParameter NoRoot { get; set; }
        /// <summary><para type="description">Typed culture, time-zone and value presentation configuration.</para></summary>
        [Parameter] public Presenters.PresentationOptions? Presentation { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (ParameterSetName == ObjectsSet) {
                if (InputObject != null) {
                    PSTree.Collect(_objects, InputObject);
                }
                return;
            }
            Tree tree = new Tree(PSText.Markup(Title));
            PSItems.AddAll(this, tree, PSItems.Items(Content));
            Emit(tree);
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (ParameterSetName != ObjectsSet) {
                return;
            }
            Tree tree = ConsoleHostViews.Tree(_objects, new ConsoleTreeOptions {
                Title = Title, ChildrenProperty = ChildrenProperty, Children = Children, LabelProperty = LabelProperty,
                StatusProperty = StatusProperty, DetailProperty = DetailProperty, Depth = Depth, MaxItems = MaxItems, MaxNodes = MaxNodes
            });
            Emit(tree);
        }

        /// <summary>Emits the tree (Show-Console -View Tree overrides this to write it).</summary>
        /// <param name="tree">The tree.</param>
        protected virtual void Emit(Tree tree) {
            tree.WrapLabels = WrapLabels.IsPresent;
            if (MyInvocation.BoundParameters.ContainsKey(nameof(NoRoot))) { tree.ShowRoot = !NoRoot.IsPresent; }
            if (Presentation != null) { tree.Presentation = Presentation; }
            WriteObject(tree);
        }

    }
}
