using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Data;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Builds a <see cref="Tree"/> from an inspection: objects and collections become branches (with their type or item
    /// count), scalar members become leaves showing the presented value. Used by <c>Show-Console -View Tree</c>.
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
            if (root is InspectScalar scalar) {
                // A single value: its type and the value itself, not just the type name.
                // Inspecting a date with an offset keeps the offset (often the reason to look); elsewhere dates read in local time.
                string text = scalar.Value is DateTimeOffset offset
                    ? offset.ToString("yyyy-MM-dd HH:mm:ss zzz", System.Globalization.CultureInfo.InvariantCulture)
                    : Values.ValueShaper.ScalarText(scalar.Value);
                return new Tree("[bold]" + Markup.Escape(title ?? root.TypeName) + "[/]" + (text.Length > 0 ? "  " + Markup.Escape(text) : string.Empty)) { ShowRoot = true };
            }
            Tree tree = new Tree("[bold]" + Markup.Escape(title ?? root.TypeName) + "[/]") { ShowRoot = true };
            Fill(tree.Root, root);
            return tree;
        }

        // Only the host's identity helper knows which fields made an automatic header.
        internal static InspectNode OmitIdentity(InspectNode node, ISet<string> identityProperties) {
            if (!(node is InspectObject obj) || identityProperties.Count == 0) { return node; }
            return new InspectObject(obj.Name, obj.TypeName, obj.Path,
                obj.Members.Where(member => !(member is InspectScalar) || member.Name == null || !identityProperties.Contains(member.Name)).ToArray());
        }

        private static void Fill(TreeNode parent, InspectNode node) {
            switch (node) {
                case InspectObject obj:
                    int keyWidth = Math.Min(16, obj.Members.Where(member => member.TypeName != "InspectionEmptySummary")
                        .Select(member => DisplayWidth.Measure(member.Name ?? string.Empty)).DefaultIfEmpty().Max());
                    foreach (InspectNode member in obj.Members) {
                        AddNode(parent, member, member.Name ?? string.Empty, keyWidth: member.TypeName == "InspectionEmptySummary" ? 0 : keyWidth);
                    }
                    break;
                case InspectCollection collection:
                    int index = 0;
                    foreach (InspectNode item in collection.Items) {
                        AddNode(parent, item, "[" + index.ToString(CultureInfo.InvariantCulture) + "]", true);
                        index++;
                    }
                    if (collection.Omitted > 0) {
                        parent.Add(string.Empty).LocalizedLabel = labels => "[muted]" + Markup.Escape(Localization.WidgetLabels.Format(labels.MoreCount, collection.Omitted.ToString("N0", CultureInfo.InvariantCulture))) + "[/]";
                    } else if (collection.Omitted < 0) {
                        parent.Add(string.Empty).LocalizedLabel = labels => "[muted]" + Markup.Escape(labels.MoreUnknown) + "[/]";
                    }
                    break;
            }
        }

        private static void AddNode(TreeNode parent, InspectNode node, string name, bool collectionItem = false, int keyWidth = 0) {
            string label = Markup.Escape(name);
            TreeNode Add(Styling.StatusKind? status = null, string? detail = null) {
                TreeNode child = parent.Add(label, status, detail);
                child.LabelPadding = Math.Max(0, keyWidth - DisplayWidth.Measure(name));
                return child;
            }
            switch (node) {
                case InspectObject obj:
                    Fill(Add(detail: obj.TypeName), obj);
                    break;
                case InspectCollection collection:
                    int count = collection.Count ?? collection.Items.Count;
                    TreeNode branch = Add();
                    branch.LocalizedNote = labels => Localization.WidgetLabels.Format(count == 1 ? labels.OneItem : labels.ManyItems, count.ToString("N0", CultureInfo.InvariantCulture));
                    Fill(branch, collection);
                    break;
                case InspectScalar scalar:
                    TreeNode leaf = Add();
                    leaf.Value = TableCell.FromValue(scalar.Value is byte[] || scalar.Value is System.Security.SecureString ? Values.ValueShaper.ScalarText(scalar.Value) : scalar.Value);
                    leaf.Presenter = Presenters.CellPresenters.ForLabel(name, scalar.Value);
                    leaf.IsScalarCollectionItem = collectionItem;
                    break;
                case InspectCycle cycle:
                    Add(detail: "(cycle: " + cycle.TargetPath + ")");
                    break;
                case InspectLimit limit:
                    Add(detail: limit.TypeName == "Limit" ? limit.Summary : limit.Summary + " (depth limit)");
                    break;
                case InspectError error:
                    Add(Styling.StatusKind.Warn, "error: " + error.Message);
                    break;
            }
        }
    }
}
