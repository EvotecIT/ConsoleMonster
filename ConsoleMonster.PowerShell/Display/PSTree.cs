using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    internal sealed class PSTreeOptions {
        internal object? Title { get; set; }
        internal string? ChildrenProperty { get; set; }
        internal ScriptBlock? Children { get; set; }
        internal string? LabelProperty { get; set; }
        internal string? StatusProperty { get; set; }
        internal string? DetailProperty { get; set; }
        internal int Depth { get; set; } = 3;
        internal int MaxItems { get; set; } = 100;
        internal int MaxNodes { get; set; } = 1000;
    }

    /// <summary>PowerShell property/child adaptation into the bounded core hierarchy walker.</summary>
    internal static partial class PSTree {
        internal static Tree Build(IReadOnlyList<object> objects, PSTreeOptions options) {
            if (options.ChildrenProperty != null && options.Children != null) {
                throw new PSArgumentException("Give children with ChildrenProperty or Children, not both.");
            }
            Tree tree = new Tree(PSText.Markup(options.Title));
            if (objects.Count == 1 && PSText.Unwrap(objects[0]) is IDictionary dictionary && options.ChildrenProperty == null && options.Children == null && options.LabelProperty == null && options.StatusProperty == null && options.DetailProperty == null) {
                Fill(tree.Root, dictionary, options);
                return tree;
            }
            if (options.ChildrenProperty == null && options.Children == null && options.LabelProperty == null && options.StatusProperty == null && options.DetailProperty == null
                && System.Linq.Enumerable.All(objects, item => PSText.Unwrap(item) is TreeNode || PSItems.Field(item, "Label") != null)) {
                foreach (object item in objects) {
                    if (!PSItems.TryAdd(tree, item, null, out string? problem)) { throw new PSArgumentException(problem ?? "A tree record needs Label and optional Children."); }
                }
                return tree;
            }
            Values.ShapeOptions shape = Binding.PSShaping.Options();
            TreeHierarchy.Add(tree.Root, objects, Node, ChildrenOf, new TreeHierarchyOptions {
                Depth = System.Math.Max(0, options.Depth),
                MaxItems = options.MaxItems,
                MaxNodes = options.MaxNodes,
                Identity = Identity
            });
            return tree;

            TreeNode Node(object item) {
                // Nodes made by Label/Children records (and strings) piped in go in as they are.
                switch (PSText.Unwrap(item)) {
                    case TreeNode made:
                        return made;
                    case Text.RichText richLabel:
                        return new TreeNode(richLabel.ToMarkup());
                }
                PSObject ps = PSObject.AsPSObject(item);
                object? label = options.LabelProperty != null ? Read(ps, options.LabelProperty) : null;
                string text = label != null ? PSWidgets.ToText(label) : Binding.PSShaping.Title(ps, shape);
                Text.RichText? rich = PSText.Rich(label);
                StatusKind? status = null;
                if (options.StatusProperty != null) {
                    object? state = Read(ps, options.StatusProperty);
                    if (state is StatusKind kind) {
                        status = kind;
                    } else if (state != null && StatusKinds.TryParse(PSWidgets.ToText(state), out StatusKind parsed)) {
                        status = parsed;
                    }
                }
                object? detail = options.DetailProperty != null ? Read(ps, options.DetailProperty) : null;
                return new TreeNode(rich != null ? rich.ToMarkup() : Markup.Escape(text), status) { Detail = detail == null ? null : PSWidgets.ToText(detail) };
            }

            IEnumerable<object>? ChildrenOf(object item) {
                if (PSText.Unwrap(item) is TreeNode || PSText.Unwrap(item) is Text.RichText) {
                    return null;
                }
                object? children;
                if (options.Children != null) {
                    List<PSVariable> variables = new List<PSVariable> { new PSVariable("_", item), new PSVariable("PSItem", item) };
                    children = options.Children.InvokeWithContext(null, variables);
                } else if (options.ChildrenProperty != null) {
                    children = Read(PSObject.AsPSObject(item), options.ChildrenProperty, raw: true);
                } else {
                    return null;
                }
                List<object> list = new List<object>();
                Collect(list, children);
                return list.Count == 0 ? null : list;
            }
        }


        // Children keep their PowerShell wrapper: the properties of a [pscustomobject] live on it, not on its base object.
        internal static void Collect(List<object> list, object? value, List<object>? path = null) {
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
            PSCollectionPath.Enter(ref path, raw);
            try {
                foreach (object? element in enumerable) { Collect(list, element, path); }
            } finally { path!.RemoveAt(path.Count - 1); }
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

    }
}
