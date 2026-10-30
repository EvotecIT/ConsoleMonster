using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    public abstract partial class ConsoleDisplayCmdlet {
        private IWidget BuildTreeView(IReadOnlyList<PSObject> items) {
            string[]? include = Property?.Select(PSText.Unwrap).OfType<string>().ToArray();
            var trees = new List<IWidget>();
            var inspected = new List<InspectNode>();
            var titles = new List<string?>();
            bool excludedCalculated = false;
            var shape = Binding.PSShaping.Options();
            foreach (PSObject item in items) {
                ConsoleViewRegistration? registration = Property == null ? ConsoleFormatting.Find(item) : null;
                InspectorOptions options = PSFormatting.InspectorOptions(Depth, MaxItems, include, Exclude, MaxNodes, item, rootDefaultDisplay: true);
                if (Property != null && Property.Any(property => !(PSText.Unwrap(property) is string))) {
                    Binding.PSObjectMemberSource? source = options.Sources.OfType<Binding.PSObjectMemberSource>().FirstOrDefault();
                    if (source != null) {
                        source.RootColumns = Binding.PropertyBinder.Bind(new[] { item }, Property, shape, this);
                        foreach (Binding.BoundColumn column in source.RootColumns) {
                            if (options.Include.Count > 0 && !options.Include.Contains(column.Name)) { options.Include.Add(column.Name); }
                        }
                        options.Unwrap = value => ReferenceEquals(value, item) ? value : Binding.PSObjectMemberSource.UnwrapPSObject(value);
                    }
                }
                else if (registration?.Property != null && registration.Property.Length > 0 && registration.Property.All(property => PSText.Unwrap(property) is string)) {
                    Binding.PSObjectMemberSource? source = options.Sources.OfType<Binding.PSObjectMemberSource>().FirstOrDefault();
                    if (source != null) { source.RootProperties = registration.Property.Select(property => (string)PSText.Unwrap(property)!).ToArray(); }
                }
                else if (registration?.Property != null && registration.Property.Length > 0 && (Exclude == null || Exclude.Length == 0)) {
                    Binding.PSObjectMemberSource? source = options.Sources.OfType<Binding.PSObjectMemberSource>().FirstOrDefault();
                    if (source != null) {
                        source.RootColumns = Binding.PropertyBinder.Bind(new[] { item }, registration.Property, shape, this);
                        options.Unwrap = value => ReferenceEquals(value, item) ? value : Binding.PSObjectMemberSource.UnwrapPSObject(value);
                    }
                }
                else if (registration?.Property != null && registration.Property.Length > 0) {
                    Binding.PSObjectMemberSource? source = options.Sources.OfType<Binding.PSObjectMemberSource>().FirstOrDefault();
                    if (source != null) {
                        source.RootProperties = registration.Property.Where(property => PSText.Unwrap(property) is string).Select(property => (string)PSText.Unwrap(property)!).ToArray();
                        options.Unwrap = value => ReferenceEquals(value, item) ? value : Binding.PSObjectMemberSource.UnwrapPSObject(value);
                        excludedCalculated = true;
                    }
                }
                options.Budget = new ConsoleMonster.Values.ShapeBudget(options.TimeBudget, options.MaxNodes);
                string? label = PSText.Markup(Title);
                var identityProperties = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                if (label == null && (Exclude == null || Exclude.Length == 0) && (Property == null || (Property.Length == 1 && PSText.Unwrap(Property[0]) is string selector && selector == "*"))) { label = Binding.PSShaping.IdentityTitle(item, shape, options.Budget, identityProperties); }
#pragma warning disable IL2026
                InspectNode node = ObjectInspector.Inspect(item, options);
                if (node is InspectLimit limit && limit.TypeName == "Limit") {
                    node = new InspectObject(null, node.TypeName, node.Path, new[] { limit });
                }
                identityProperties.UnionWith(identityProperties.Select(name => Binding.PropertyBinder.SourceProperty(name, item) ?? name).ToArray());
                inspected.Add(InspectorTree.OmitIdentity(node, identityProperties));
                titles.Add(label);
#pragma warning restore IL2026
            }
            bool hideEmpty = Property == null;
            IReadOnlyList<InspectNode> summarized = InspectionSummary.Prepare(inspected, hideEmpty, out IReadOnlyList<string> unavailable);
            for (int index = 0; index < summarized.Count; index++) {
                Tree tree = InspectorTree.Build(summarized[index], titles[index]);
                MuteInspectionNotes(tree.Root, summarized[index]);
                trees.Add(tree);
            }
            foreach (string note in unavailable) {
                trees.Add(TextBlock.Plain(note, ThemeToken.Muted));
            }
            if (excludedCalculated) { WriteWarning("Calculated registered-view properties were not evaluated because -Exclude is in use."); }
            return PSFormatting.Combine(trees);
        }

        private static void MuteInspectionNotes(TreeNode target, InspectNode node) {
            if (node is InspectScalar scalar) {
                if (scalar.TypeName == "InspectionUnavailable" && target.Value != null) { target.Value = target.Value.WithTone(ThemeToken.Muted); }
                if (scalar.TypeName == "InspectionEmptySummary") { target.Label = "[muted]" + target.Label + "[/]"; }
            }
            IReadOnlyList<InspectNode>? children = node is InspectObject record ? record.Members : node is InspectCollection collection ? collection.Items : null;
            if (children == null) { return; }
            for (int index = 0; index < children.Count && index < target.Children.Count; index++) { MuteInspectionNotes(target.Children[index], children[index]); }
        }
    }
}
