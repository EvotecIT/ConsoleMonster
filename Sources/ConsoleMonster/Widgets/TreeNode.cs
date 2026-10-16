using System;
using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A node of a <see cref="Tree"/>: a label (markup), an optional status and children.</summary>
    public sealed class TreeNode {
        private readonly List<TreeNode> _children = new List<TreeNode>();

        /// <summary>Creates a node.</summary>
        /// <param name="label">The label (markup).</param>
        /// <param name="status">An optional status marker.</param>
        public TreeNode(string label, StatusKind? status = null) {
            Label = label ?? string.Empty;
            Status = status;
        }

        /// <summary>The label (markup).</summary>
        public string Label { get; set; }

        /// <summary>An optional status marker shown before the label.</summary>
        public StatusKind? Status { get; set; }

        /// <summary>A muted note shown after the label (counts, values), or <c>null</c>.</summary>
        public string? Note { get; set; }

        /// <summary>The children.</summary>
        public IReadOnlyList<TreeNode> Children => _children;

        /// <summary>Adds a child and returns it.</summary>
        /// <param name="label">The label (markup).</param>
        /// <param name="status">An optional status.</param>
        /// <param name="note">An optional muted note.</param>
        /// <returns>The new child.</returns>
        public TreeNode Add(string label, StatusKind? status = null, string? note = null) {
            TreeNode node = new TreeNode(label, status) { Note = note };
            _children.Add(node);
            return node;
        }

        /// <summary>Adds an existing node as a child.</summary>
        /// <param name="node">The node.</param>
        /// <returns>This node.</returns>
        public TreeNode Add(TreeNode node) {
            _children.Add(node ?? throw new ArgumentNullException(nameof(node)));
            return this;
        }
    }
}
