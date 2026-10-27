using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    public abstract partial class ConsoleDisplayCmdlet {
        private IWidget BuildTreeView(IReadOnlyList<PSObject> items) {
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
            foreach (PSObject item in items) {
                InspectorOptions options = PSFormatting.InspectorOptions(Depth, MaxItems, Include, Exclude, MaxNodes, item);
#pragma warning disable IL2026
                trees.Add(InspectorTree.Build(ObjectInspector.Inspect(item, options), PSText.Markup(Title)));
#pragma warning restore IL2026
            }
            return PSFormatting.Combine(trees);
        }
    }
}
