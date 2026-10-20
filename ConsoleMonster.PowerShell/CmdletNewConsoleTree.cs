using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a console tree from nested New-ConsoleTreeNode calls, hierarchical objects or nested hashtables.</para>
    /// <para type="description">Three ways in, one tree out. Nodes: a script block of New-ConsoleTreeNode calls, each with its own status, detail and children (strings become leaves shown literally); nodes can also be piped in. Objects: pipe hierarchical objects (groups and members, folders and files) and name their children with -ChildrenProperty or a -Children script block ($_ is the parent); -NameProperty, -StatusProperty and -DetailProperty pick what each node shows, and -Depth, -MaxItems and -MaxNodes keep a large or cyclic hierarchy bounded and mark what was left out (a repeated item is shown once with "(cycle)"). Hashtables: -Data maps keys to nested hashtables (child nodes), arrays (leaves) or other values (a muted note); use [ordered] to keep order. The title (markup allowed) is the root; without it the top-level nodes are the roots.</para>
    /// </summary>
    /// <example>
    ///   <para>A mail domain with records and their state</para>
    ///   <code>
    /// New-ConsoleTree '[bold]evotec.xyz[/]' {
    ///     New-ConsoleTreeNode 'MX' -Status Pass -Detail '2 records' {
    ///         New-ConsoleTreeNode 'evotec-xyz.mail.protection.outlook.com' -Detail 'priority 0'
    ///     }
    ///     New-ConsoleTreeNode 'SPF' -Status Warn -Detail '9 of 10 DNS lookups' {
    ///         'include:spf.protection.outlook.com'
    ///         'include:_spf.salesforce.com'
    ///     }
    ///     New-ConsoleTreeNode 'DMARC' -Status Fail -Detail 'p=none'
    /// } | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>Folders and files from the pipeline, three levels deep</para>
    ///   <code>Get-Item C:\Support\Reports | New-ConsoleTree -Children { if ($_.PSIsContainer) { Get-ChildItem $_.FullName } } -Depth 3 | Write-ConsoleWidget</code>
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
    /// $groups | New-ConsoleTree 'Privileged groups' -ChildrenProperty Members -StatusProperty Health -DetailProperty Note | Write-ConsoleWidget</code>
    /// </example>
    /// <example>
    ///   <para>A DNS delegation from nested hashtables</para>
    ///   <code>New-ConsoleTree -Title 'evotec.xyz' -Data ([ordered] @{ NS = @('dana.ns.cloudflare.com', 'kirk.ns.cloudflare.com'); MX = [ordered] @{ 'mail.protection.outlook.com' = '52.101.68.17' } }) | Write-ConsoleWidget</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleTree", DefaultParameterSetName = NodesSet)]
    [OutputType(typeof(Tree))]
    public class CmdletNewConsoleTree : PSCmdlet {
        private const string NodesSet = "Nodes";
        private const string DataSet = "Data";
        private const string ObjectsSet = "Objects";
        private readonly List<object> _objects = new List<object>();

        /// <summary><para type="description">The root label (markup allowed; rich text from New-ConsoleText keeps its colours). Without it the top-level nodes are the roots.</para></summary>
        [Parameter(ParameterSetName = NodesSet, Position = 0)]
        [Parameter(ParameterSetName = DataSet)]
        [Parameter(ParameterSetName = ObjectsSet, Position = 0)]
        public object? Title { get; set; }

        /// <summary><para type="description">A script block that emits the nodes: New-ConsoleTreeNode calls, or strings for leaves (shown literally).</para></summary>
        [Parameter(ParameterSetName = NodesSet, Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Nested hashtables describing the tree: a nested hashtable becomes child nodes, an array leaf nodes, any other value a muted note after the key.</para></summary>
        [Parameter(ParameterSetName = DataSet, Position = 0, Mandatory = true)]
        public IDictionary Data { get; set; } = null!;

        /// <summary><para type="description">Hierarchical objects (pipeline input); each becomes a top-level node. Nodes from New-ConsoleTreeNode go in as they are.</para></summary>
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
        public string? NameProperty { get; set; }

        /// <summary><para type="description">The property whose value is the node status (pass, fail, warn, skip... or PowerShell's words).</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public string? StatusProperty { get; set; }

        /// <summary><para type="description">The property shown as a muted note after the label.</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public string? DetailProperty { get; set; }

        /// <summary><para type="description">How many levels of children are followed below each object (default 5).</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public int Depth { get; set; } = 5;

        /// <summary><para type="description">The most children listed per node, and objects listed at the top, before "+N more" (default 25; 0 lists all).</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public int MaxItems { get; set; } = 25;

        /// <summary><para type="description">The most nodes the tree may grow to (default 5000; 0 for no limit); beyond it a "node limit reached" note ends the walk.</para></summary>
        [Parameter(ParameterSetName = ObjectsSet)]
        public int MaxNodes { get; set; } = 5000;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (ParameterSetName == ObjectsSet) {
                if (InputObject != null) {
                    _objects.Add(InputObject);
                }
                return;
            }
            Tree tree = new Tree(PSText.Markup(LegacyData == null ? Title : null));
            if (ParameterSetName == DataSet || LegacyData != null) {
                Fill(tree.Root, LegacyData ?? Data);
            } else {
                PSItems.AddAll(this, tree, PSItems.Items(Content));
            }
            Emit(tree);
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (ParameterSetName != ObjectsSet) {
                return;
            }
            if (ChildrenProperty != null && Children != null) {
                ThrowTerminatingError(new ErrorRecord(new PSArgumentException("Give the children with -ChildrenProperty or -Children, not both."), "ConsoleMonsterTreeChildren", ErrorCategory.InvalidArgument, null));
            }
            Tree tree = new Tree(PSText.Markup(Title));
            Values.ShapeOptions shape = Binding.PSShaping.Options();
            TreeHierarchy.Add(tree.Root, _objects, Node, ChildrenOf, new TreeHierarchyOptions {
                Depth = System.Math.Max(0, Depth),
                MaxItems = MaxItems,
                MaxNodes = MaxNodes,
                Identity = Identity
            });
            Emit(tree);

            TreeNode Node(object item) {
                // Nodes made by New-ConsoleTreeNode (and strings) piped in go in as they are.
                switch (PSText.Unwrap(item)) {
                    case TreeNode made:
                        return made;
                    case Text.RichText richLabel:
                        return new TreeNode(richLabel.ToMarkup());
                }
                PSObject ps = PSObject.AsPSObject(item);
                object? label = NameProperty != null ? Read(ps, NameProperty) : null;
                string text = label != null ? PSWidgets.ToText(label) : Binding.PSShaping.Title(ps, shape);
                Text.RichText? rich = PSText.Rich(label);
                StatusKind? status = null;
                if (StatusProperty != null) {
                    object? state = Read(ps, StatusProperty);
                    if (state is StatusKind kind) {
                        status = kind;
                    } else if (state != null && StatusKinds.TryParse(PSWidgets.ToText(state), out StatusKind parsed)) {
                        status = parsed;
                    }
                }
                object? detail = DetailProperty != null ? Read(ps, DetailProperty) : null;
                return new TreeNode(rich != null ? rich.ToMarkup() : Markup.Escape(text), status) { Note = detail == null ? null : PSWidgets.ToText(detail) };
            }

            IEnumerable<object>? ChildrenOf(object item) {
                if (PSText.Unwrap(item) is TreeNode || PSText.Unwrap(item) is Text.RichText) {
                    return null;
                }
                object? children;
                if (Children != null) {
                    List<PSVariable> variables = new List<PSVariable> { new PSVariable("_", item), new PSVariable("PSItem", item) };
                    children = Children.InvokeWithContext(null, variables);
                } else if (ChildrenProperty != null) {
                    children = Read(PSObject.AsPSObject(item), ChildrenProperty, raw: true);
                } else {
                    return null;
                }
                List<object> list = new List<object>();
                Collect(list, children);
                return list.Count == 0 ? null : list;
            }
        }

        // A hashtable given by position (New-ConsoleTree @{ ... }), as the command took before it had a title position.
        private IDictionary? LegacyData => ParameterSetName == NodesSet && Content == null && PSText.Unwrap(Title) is IDictionary dictionary ? dictionary : null;

        // Children keep their PowerShell wrapper: the properties of a [pscustomobject] live on it, not on its base object.
        private static void Collect(List<object> list, object? value) {
            if (value == null) {
                return;
            }
            object? raw = PSText.Unwrap(value);
            if (raw == null) {
                return;
            }
            if (raw is string || raw is IDictionary || !(raw is IEnumerable enumerable)) {
                list.Add(PSObject.AsPSObject(value));
                return;
            }
            foreach (object? element in enumerable) {
                Collect(list, element);
            }
        }

        // The identity for cycle detection: the wrapped object, except for [pscustomobject], whose base object is shared.
        private static object Identity(object item) {
            object? raw = PSText.Unwrap(item);
            return raw == null || raw is PSCustomObject ? item : raw;
        }

        // A property (or dictionary key) of an item. With raw, the value keeps its PowerShell wrapper: a single
        // [pscustomobject] child has its properties on the wrapper, not on its base object.
        private static object? Read(PSObject item, string name, bool raw = false) {
            object? value = null;
            object? baseObject = PSText.Unwrap(item);
            if (baseObject is IDictionary dictionary) {
                foreach (DictionaryEntry entry in dictionary) {
                    if (string.Equals(PSWidgets.ToText(entry.Key), name, System.StringComparison.OrdinalIgnoreCase)) {
                        value = entry.Value;
                        break;
                    }
                }
            } else {
                PSPropertyInfo? property = item.Properties[name];
                if (property == null) {
                    return null;
                }
                try {
                    value = property.Value;
                } catch (GetValueException) {
                    return null;
                }
            }
            return raw ? value : PSText.Unwrap(value);
        }

        /// <summary>Emits the tree (Write-ConsoleTree overrides this to write it).</summary>
        /// <param name="tree">The tree.</param>
        protected virtual void Emit(Tree tree) => WriteObject(tree);

        private static void Fill(TreeNode parent, IDictionary data) {
            foreach (DictionaryEntry entry in data) {
                string key = Markup.Escape(PSWidgets.ToText(entry.Key));
                object? value = entry.Value is PSObject ps ? ps.BaseObject : entry.Value;
                if (value is IDictionary nested) {
                    Fill(parent.Add(key), nested);
                } else if (value is IEnumerable list && !(value is string)) {
                    TreeNode node = parent.Add(key);
                    foreach (object? item in list) {
                        object? raw = item is PSObject itemPs ? itemPs.BaseObject : item;
                        if (raw is IDictionary itemDictionary) {
                            Fill(node, itemDictionary);
                        } else {
                            node.Add(Markup.Escape(PSWidgets.ToText(raw)));
                        }
                    }
                } else {
                    parent.Add(key, null, value == null ? null : PSWidgets.ToText(value));
                }
            }
        }
    }
}
