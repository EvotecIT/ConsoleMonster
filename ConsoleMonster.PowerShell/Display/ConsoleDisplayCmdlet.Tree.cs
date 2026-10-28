using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    public abstract partial class ConsoleDisplayCmdlet {
        private IWidget BuildInspectorView(IReadOnlyList<PSObject> items) {
            var views = new List<IWidget>();
            foreach (PSObject item in items) {
                InspectorOptions options = PSFormatting.InspectorOptions(Depth, MaxItems, Include, Exclude, MaxNodes, item);
#pragma warning disable IL2026
                views.Add(new InspectorView(ObjectInspector.Inspect(item, options), PSText.Markup(Title)));
#pragma warning restore IL2026
            }
            return PSFormatting.Combine(views);
        }
        private IWidget BuildTreeView(IReadOnlyList<PSObject> items) {
            string[]? include = Include;
            if (Property != null) {
                if (Include != null || Property.Any(value => !(PSText.Unwrap(value) is string))) {
                    throw new PSArgumentException("Tree -Property accepts member names or patterns and cannot be combined with -Include.");
                }
                include = Property.Select(value => (string)PSText.Unwrap(value)!).ToArray();
            }
            bool hierarchy = ChildrenProperty != null || Children != null || LabelProperty != null || StatusProperty != null || DetailProperty != null;
            if (hierarchy || items.All(item => PSText.Unwrap(item) is TreeNode)) {
                return ConsoleHostViews.Tree(items.Cast<object>().ToList(), new ConsoleTreeOptions {
                    Title = Title, ChildrenProperty = ChildrenProperty, Children = Children, LabelProperty = LabelProperty,
                    StatusProperty = StatusProperty, DetailProperty = DetailProperty, Depth = Depth, MaxItems = MaxItems, MaxNodes = MaxNodes
                });
            }
            // Content explicitly contains node records; ordinary pipeline objects remain bounded object graphs.
            if (Content != null) {
                var tree = new Tree(PSText.Markup(Title));
                PSItems.AddAll(this, tree, items.Cast<object>());
                return tree;
            }
            var trees = new List<IWidget>();
            var shape = Binding.PSShaping.Options();
            foreach (PSObject item in items) {
                InspectorOptions options = PSFormatting.InspectorOptions(Depth, MaxItems, include, Exclude, MaxNodes, item, rootDefaultDisplay: true);
#pragma warning disable IL2026
                InspectNode node = ObjectInspector.Inspect(item, options);
                string? label = PSText.Markup(Title);
                if (label == null && (Exclude == null || Exclude.Length == 0) && (include == null || include.Length == 0 || (include.Length == 1 && include[0] == "*"))) { label = Binding.PSShaping.Title(item, shape); }
                trees.Add(InspectorTree.Build(node, label));
#pragma warning restore IL2026
            }
            return PSFormatting.Combine(trees);
        }
    }
}
