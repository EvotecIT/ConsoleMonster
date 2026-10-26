using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// A hierarchy with guide lines (<c>├──</c>, <c>└──</c>, <c>│</c> on Full/Basic; <c>|--</c> on Ascii). Guides use the
    /// border colour so the labels carry the attention. Long labels are truncated with an ellipsis.
    /// </summary>
    public sealed partial class Tree : IWidget {
        /// <summary>Creates a tree.</summary>
        /// <param name="title">The root label (markup), or <c>null</c> to show only the children as top-level nodes.</param>
        public Tree(string? title = null) {
            Root = new TreeNode(title ?? string.Empty);
            ShowRoot = title != null;
        }

        /// <summary>The root title (markup); null hides the root while preserving its children.</summary>
        public string? Title {
            get => ShowRoot ? Root.Label : null;
            set { Root.Label = value ?? string.Empty; ShowRoot = value != null; }
        }

        /// <summary>The root node.</summary>
        public TreeNode Root { get; }

        /// <summary>Whether the root label is shown.</summary>
        public bool ShowRoot { get; set; }

        /// <summary>Wrap labels, values and notes with a hanging indent instead of shortening them (default false).</summary>
        public bool WrapLabels { get; set; }

        /// <summary>Adds a top-level node.</summary>
        /// <param name="label">The label (markup).</param>
        /// <param name="status">An optional status.</param>
        /// <param name="detail">An optional muted detail.</param>
        /// <returns>The new node.</returns>
        public TreeNode Add(string label, StatusKind? status = null, string? detail = null) => Root.Add(label, status, detail);

        /// <summary>Adds a top-level node with a multicolour label.</summary>
        /// <param name="label">The label.</param>
        /// <param name="status">An optional status.</param>
        /// <param name="detail">An optional muted detail.</param>
        /// <returns>The new node.</returns>
        public TreeNode Add(Text.RichText label, StatusKind? status = null, string? detail = null) => Root.Add(label, status, detail);

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            if (WrapLabels) {
                int wrappedHeight = WrappedLines(constraints.MaxWidth, context).Count;
                return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? wrappedHeight : Math.Min(wrappedHeight, constraints.MaxHeight));
            }
            List<TreeRow> rows = Flatten(context);
            Presenters.PresenterContext presenter = Presentation.CreateContext(context);
            int width = 0;
            foreach (TreeRow row in rows) {
                width = Math.Max(width, row.Width(context) + ValueWidth(row.Node, presenter));
            }
            int height = constraints.IsHeightUnbounded ? rows.Count : Math.Min(rows.Count, constraints.MaxHeight);
            return new Size(Math.Min(width, constraints.MaxWidth), height);
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (WrapLabels) {
                List<StyledLine> wrapped = WrappedLines(region.Width, context);
                for (int y = 0; y < wrapped.Count && y < region.Height; y++) region.Write(0, y, wrapped[y]);
                return;
            }
            List<TreeRow> rows = Flatten(context);
            Presenters.PresenterContext presenter = Presentation.CreateContext(context);
            Style guide = context.Style(ThemeToken.Border);
            Style muted = context.Style(ThemeToken.Muted);
            for (int i = 0; i < rows.Count && i < region.Height; i++) {
                TreeRow row = rows[i];
                int x = region.Write(0, i, row.Prefix, guide);
                TreeNode node = row.Node;
                if (node.Status.HasValue) {
                    x = region.Write(x, i, context.Glyphs.StatusMarker(node.Status.Value), context.Style(node.Status.Value));
                    x++;
                }
                StyledLine label = TextWrapper.Truncate(context.Markup(node.LabelFor(context)).Lines[0], Math.Max(1, region.Width - x), context.Ellipsis);
                x = region.Write(x, i, label);
                if (node.Value != null) {
                    StyledLine value = CellRendering.Content(node.Value, node.Presenter, null, presenter).Lines[0];
                    if (region.Width - x - 2 >= 4) {
                        x = region.Write(x + 2, i, TextWrapper.Truncate(value, region.Width - x - 2, context.Ellipsis));
                    }
                }
                int noteSpace = region.Width - x - 2;
                string? note = node.NoteFor(context);
                if (!string.IsNullOrEmpty(note) && noteSpace >= 4) {
                    region.Write(x + 2, i, TextWrapper.Truncate(note!, noteSpace, context.Ellipsis), muted);
                }
            }
        }

        /// <summary>Culture, clock and time zone for node values.</summary>
        public Presenters.PresentationOptions Presentation { get; set; } = new Presenters.PresentationOptions();

        private static int ValueWidth(TreeNode node, Presenters.PresenterContext presenter) {
            return node.Value == null ? 0 : 2 + CellRendering.Content(node.Value, node.Presenter, null, presenter).TextWidth;
        }

        private List<TreeRow> Flatten(RenderContext context) {
            List<TreeRow> rows = new List<TreeRow>();
            if (ShowRoot) {
                rows.Add(new TreeRow(string.Empty, Root));
                Walk(Root, string.Empty, rows, context);
            } else {
                Walk(Root, string.Empty, rows, context);
            }
            return rows;
        }

        private static void Walk(TreeNode node, string indent, List<TreeRow> rows, RenderContext context) {
            IGlyphSet glyphs = context.Glyphs;
            for (int i = 0; i < node.Children.Count; i++) {
                bool last = i == node.Children.Count - 1;
                TreeNode child = node.Children[i];
                rows.Add(new TreeRow(indent + glyphs.Get(last ? Glyph.TreeLast : Glyph.TreeBranch), child));
                Walk(child, indent + glyphs.Get(last ? Glyph.TreeSpace : Glyph.TreeVertical), rows, context);
            }
        }

        private sealed class TreeRow {
            public TreeRow(string prefix, TreeNode node) {
                Prefix = prefix;
                Node = node;
            }

            public string Prefix { get; }

            public TreeNode Node { get; }

            public int Width(RenderContext context) {
                int width = DisplayWidth.Measure(Prefix) + context.Markup(Node.LabelFor(context)).Width;
                if (Node.Status.HasValue) {
                    width += DisplayWidth.Measure(context.Glyphs.StatusMarker(Node.Status.Value)) + 1;
                }
                string? note = Node.NoteFor(context);
                if (!string.IsNullOrEmpty(note)) {
                    width += DisplayWidth.Measure(note) + 2;
                }
                return width;
            }
        }
    }
}
