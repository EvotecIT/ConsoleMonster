using System;
using System.Globalization;
using ConsoleMonster.Data;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Builds a <see cref="Tree"/> from an inspection: objects and collections become branches (with their type or item
    /// count), scalar members become leaves showing the presented value. Used by <c>Format-ConsoleTree</c>.
    /// </summary>
    public static class InspectorTree {
        /// <summary>Builds a tree for an inspection root.</summary>
        /// <param name="root">The root.</param>
        /// <param name="title">The root label, or null for the type name.</param>
        /// <returns>The tree.</returns>
        public static Tree Build(InspectNode root, string? title = null) {
            if (root == null) {
                throw new ArgumentNullException(nameof(root));
            }
            Tree tree = new Tree("[bold]" + Markup.Escape(title ?? root.TypeName) + "[/]") { ShowRoot = true };
            Fill(tree.Root, root);
            return tree;
        }

        private static void Fill(TreeNode parent, InspectNode node) {
            switch (node) {
                case InspectObject obj:
                    foreach (InspectNode member in obj.Members) {
                        AddNode(parent, member, member.Name ?? string.Empty);
                    }
                    break;
                case InspectCollection collection:
                    int index = 0;
                    foreach (InspectNode item in collection.Items) {
                        AddNode(parent, item, "[" + index.ToString(CultureInfo.InvariantCulture) + "]");
                        index++;
                    }
                    if (collection.Omitted > 0) {
                        parent.Add("[muted]" + Markup.Escape(Localization.WidgetLabels.Format(Localization.WidgetLabels.Current.MoreCount, collection.Omitted.ToString("N0", CultureInfo.InvariantCulture))) + "[/]");
                    } else if (collection.Omitted < 0) {
                        parent.Add("[muted]" + Markup.Escape(Localization.WidgetLabels.Current.MoreUnknown) + "[/]");
                    }
                    break;
            }
        }

        private static void AddNode(TreeNode parent, InspectNode node, string name) {
            string label = Markup.Escape(name);
            switch (node) {
                case InspectObject obj:
                    Fill(parent.Add(label, null, obj.TypeName), obj);
                    break;
                case InspectCollection collection:
                    int count = collection.Count ?? collection.Items.Count;
                    Fill(parent.Add(label, null, Localization.WidgetLabels.Format(count == 1 ? Localization.WidgetLabels.Current.OneItem : Localization.WidgetLabels.Current.ManyItems, count.ToString("N0", CultureInfo.InvariantCulture))), collection);
                    break;
                case InspectScalar scalar:
                    parent.Add(new TreeNode(label) { Value = TableCell.FromValue(scalar.Value is byte[] bytes ? bytes.Length + " bytes" : scalar.Value) });
                    break;
                case InspectCycle cycle:
                    parent.Add(label, null, "(cycle: " + cycle.TargetPath + ")");
                    break;
                case InspectLimit limit:
                    parent.Add(label, null, limit.Summary + " (depth limit)");
                    break;
                case InspectError error:
                    parent.Add(label, Styling.StatusKind.Warn, "error: " + error.Message);
                    break;
            }
        }
    }
}
