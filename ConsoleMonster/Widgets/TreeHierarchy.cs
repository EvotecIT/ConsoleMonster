using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>Limits for <see cref="TreeHierarchy.Add{T}"/>: how deep, how wide and how large a tree built from objects may grow.</summary>
    public sealed class TreeHierarchyOptions {
        /// <summary>How many levels of children are followed below each top-level item (default 5; 0 shows only the items).</summary>
        public int Depth { get; set; } = 5;

        /// <summary>The most children listed per node, and top-level items listed, before a "+N more" note (default 25; 0 or less lists all).</summary>
        public int MaxItems { get; set; } = 25;

        /// <summary>The most nodes the whole hierarchy may add (default 5000; 0 or less for no limit); beyond it a "node limit reached" note ends the walk.</summary>
        public int MaxNodes { get; set; } = 5000;

        /// <summary>
        /// The identity of an item for cycle detection (for example the object a host wrapper wraps), or null to use the item
        /// itself. An item that is already on the path from the top is shown once more with a "(cycle)" note and not followed.
        /// </summary>
        public Func<object, object>? Identity { get; set; }

        /// <summary>Suppress identities already shown on another branch, using explicit repeat markers (default false).</summary>
        public bool DetectRepeatedNodes { get; set; }

        /// <summary>An optional identity comparer. Null preserves reference identity, including for custom identities.</summary>
        public IEqualityComparer<object>? IdentityComparer { get; set; }

        /// <summary>Produces a plain text aggregate when descendants are omitted at the depth limit.</summary>
        public Func<object, string>? CollapsedSummary { get; set; }

        /// <summary>Reports incomplete source data independently of configured depth or size limits.</summary>
        public Func<object, bool>? IsIncomplete { get; set; }
    }

    /// <summary>
    /// Builds tree nodes from hierarchical objects (groups and members, folders and files, an org chart): a function gives
    /// each item's node and another its children. Depth, width and size limits keep a large or cyclic hierarchy readable
    /// and bounded; what is left out is marked in the tree instead of being dropped silently.
    /// </summary>
    public static class TreeHierarchy {
        /// <summary>Adds the items and their descendants under a node.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="parent">The node to add to (for example <see cref="Tree.Root"/>).</param>
        /// <param name="items">The top-level items.</param>
        /// <param name="node">Creates the node for an item (its label, status and note); children are added by the walk.</param>
        /// <param name="children">The children of an item, or null when it has none.</param>
        /// <param name="options">The limits, or null for the defaults.</param>
        /// <returns>The number of item nodes added (limit notes not counted).</returns>
        public static int Add<T>(TreeNode parent, IEnumerable<T> items, Func<T, TreeNode> node, Func<T, IEnumerable<T>?> children, TreeHierarchyOptions? options = null) {
            if (parent == null) {
                throw new ArgumentNullException(nameof(parent));
            }
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }
            if (children == null) {
                throw new ArgumentNullException(nameof(children));
            }
            if (Stops(parent.Reference)) return 0;
            Walk<T> walk = new Walk<T>(node, children, options ?? new TreeHierarchyOptions());
            walk.Level(parent, items, 0);
            return walk.Added;
        }

        private static bool Stops(TreeReferenceKind reference) => (reference & (TreeReferenceKind.Cycle | TreeReferenceKind.Repeat | TreeReferenceKind.Collapsed)) != 0;

        private sealed class Walk<T> {
            private readonly Func<T, TreeNode> _node;
            private readonly Func<T, IEnumerable<T>?> _children;
            private readonly TreeHierarchyOptions _options;
            private readonly HashSet<object> _path;
            private readonly HashSet<object> _seen;
            private bool _limitNoted;

            public Walk(Func<T, TreeNode> node, Func<T, IEnumerable<T>?> children, TreeHierarchyOptions options) {
                _node = node;
                _children = children;
                _options = options;
                IEqualityComparer<object> comparer = options.IdentityComparer ?? Values.ValueShaper.ReferenceIdentity.Instance;
                _path = new HashSet<object>(comparer);
                _seen = new HashSet<object>(comparer);
            }

            public int Added { get; private set; }

            public void Level(TreeNode parent, IEnumerable<T> items, int depth) {
                int listed = 0;
                int omitted = 0;
                foreach (T item in items) {
                    if (item == null) {
                        continue;
                    }
                    if (_options.MaxItems > 0 && listed >= _options.MaxItems) {
                        omitted++;
                        continue;
                    }
                    if (_options.MaxNodes > 0 && Added >= _options.MaxNodes) {
                        NoteLimit(parent);
                        return;
                    }
                    listed++;
                    Added++;
                    TreeNode child = _node(item) ?? new TreeNode(string.Empty);
                    parent.Add(child);
                    if (_options.IsIncomplete?.Invoke(item!) == true) child.Reference |= TreeReferenceKind.Incomplete;
                    object identity = _options.Identity?.Invoke(item!) ?? item!;
                    if (_path.Contains(identity)) {
                        child.Reference |= TreeReferenceKind.Cycle;
                        Func<Localization.WidgetLabels, string?>? localizedNote = child.LocalizedNote;
                        string? note = child.Detail;
                        child.LocalizedNote = labels => {
                            string? detail = localizedNote == null ? note : localizedNote(labels);
                            return string.IsNullOrEmpty(detail) ? labels.TreeCycle : detail + " " + labels.TreeCycle;
                        };
                        continue;
                    }
                    if (_options.DetectRepeatedNodes && !_seen.Add(identity)) {
                        child.Reference |= TreeReferenceKind.Repeat;
                        continue;
                    }
                    // A terminal reference is still an identity already shown to the user.
                    // Record it before stopping so a later occurrence cannot build an invisible repeated subtree.
                    if (Stops(child.Reference)) continue;
                    IEnumerable<T>? next = _children(item);
                    if (next == null) {
                        continue;
                    }
                    if (depth >= _options.Depth) {
                        if (HasAny(next)) {
                            if (_options.CollapsedSummary != null) {
                                child.Reference |= TreeReferenceKind.Collapsed;
                                child.ReferenceSummary = _options.CollapsedSummary(item!);
                            } else child.Add(string.Empty).LocalizedLabel = labels => "[muted]" + Markup.Escape(labels.TreeDepthLimit) + "[/]";
                        }
                        continue;
                    }
                    _path.Add(identity);
                    try {
                        Level(child, next, depth + 1);
                    } finally {
                        _path.Remove(identity);
                    }
                    if (_limitNoted) {
                        return;
                    }
                }
                if (omitted > 0) {
                    int count = omitted;
                    parent.Add(string.Empty).LocalizedLabel = labels => "[muted]" + Markup.Escape(Localization.WidgetLabels.Format(labels.MoreCount, count.ToString("N0", CultureInfo.InvariantCulture))) + "[/]";
                }
            }

            private void NoteLimit(TreeNode parent) {
                if (!_limitNoted) {
                    _limitNoted = true;
                    parent.Add(string.Empty).LocalizedLabel = labels => "[muted]" + Markup.Escape(labels.TreeNodeLimit) + "[/]";
                }
            }

            private static bool HasAny(IEnumerable<T> items) {
                foreach (T item in items) {
                    if (item != null) {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
