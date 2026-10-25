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
        public string Label {
            get => LocalizedLabel?.Invoke(Localization.WidgetLabels.Current) ?? _label;
            set {
                _label = value;
                LocalizedLabel = null;
            }
        }
        private string _label = string.Empty;

        /// <summary>An optional status marker shown before the label.</summary>
        public StatusKind? Status { get; set; }

        /// <summary>A muted detail shown after the label (counts, values), or <c>null</c>.</summary>
        public string? Detail {
            get => LocalizedNote?.Invoke(Localization.WidgetLabels.Current) ?? _note;
            set {
                _note = value;
                LocalizedNote = null;
            }
        }
        private string? _note;

        internal Func<Localization.WidgetLabels, string>? LocalizedLabel { get; set; }
        internal Func<Localization.WidgetLabels, string?>? LocalizedNote { get; set; }
        // Inspection indexes remain useful in the console; scalar items need only their value in semantic exports.
        internal bool IsScalarCollectionItem { get; set; }
        internal string LabelFor(Canvas.RenderContext context) => LocalizedLabel?.Invoke(context.Labels) ?? _label;
        internal string? NoteFor(Canvas.RenderContext context) => LocalizedNote?.Invoke(context.Labels) ?? _note;

        /// <summary>A value shown after the label through <see cref="Presenter"/> (dates, sizes, statuses...), or null.</summary>
        public TableCell? Value { get; set; }

        /// <summary>The presenter for <see cref="Value"/>, or null for automatic.</summary>
        public Presenters.ICellPresenter? Presenter { get; set; }

        /// <summary>The children.</summary>
        public IReadOnlyList<TreeNode> Children => _children;

        /// <summary>Adds a child and returns it.</summary>
        /// <param name="label">The label (markup).</param>
        /// <param name="status">An optional status.</param>
        /// <param name="detail">An optional muted detail.</param>
        /// <returns>The new child.</returns>
        public TreeNode Add(string label, StatusKind? status = null, string? detail = null) {
            TreeNode node = new TreeNode(label, status) { Detail = detail };
            _children.Add(node);
            return node;
        }

        /// <summary>Adds a child with a multicolour label.</summary>
        /// <param name="label">The label.</param>
        /// <param name="status">An optional status.</param>
        /// <param name="detail">An optional muted detail.</param>
        /// <returns>The new child.</returns>
        public TreeNode Add(Text.RichText label, StatusKind? status = null, string? detail = null) {
            return Add((label ?? throw new ArgumentNullException(nameof(label))).ToMarkup(), status, detail);
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
