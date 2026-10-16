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
    public sealed class Tree : IWidget {
        /// <summary>Creates a tree.</summary>
        /// <param name="rootLabel">The root label (markup), or <c>null</c> to show only the children as top-level nodes.</param>
        public Tree(string? rootLabel = null) {
            Root = new TreeNode(rootLabel ?? string.Empty);
            ShowRoot = rootLabel != null;
        }

        /// <summary>The root node.</summary>
        public TreeNode Root { get; }

        /// <summary>Whether the root label is shown.</summary>
        public bool ShowRoot { get; set; }

        /// <summary>Adds a top-level node.</summary>
        /// <param name="label">The label (markup).</param>
        /// <param name="status">An optional status.</param>
        /// <param name="note">An optional muted note.</param>
        /// <returns>The new node.</returns>
        public TreeNode Add(string label, StatusKind? status = null, string? note = null) => Root.Add(label, status, note);

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            List<TreeRow> rows = Flatten(context);
            int width = 0;
            foreach (TreeRow row in rows) {
                width = Math.Max(width, row.Width(context));
            }
            int height = constraints.IsHeightUnbounded ? rows.Count : Math.Min(rows.Count, constraints.MaxHeight);
            return new Size(Math.Min(width, constraints.MaxWidth), height);
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            List<TreeRow> rows = Flatten(context);
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
                StyledLine label = TextWrapper.Truncate(context.Markup(node.Label).Lines[0], Math.Max(1, region.Width - x), context.Ellipsis);
                x = region.Write(x, i, label);
                int noteSpace = region.Width - x - 2;
                if (!string.IsNullOrEmpty(node.Note) && noteSpace >= 4) {
                    region.Write(x + 2, i, TextWrapper.Truncate(node.Note!, noteSpace, context.Ellipsis), muted);
                }
            }
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
                int width = DisplayWidth.Measure(Prefix) + context.Markup(Node.Label).Width;
                if (Node.Status.HasValue) {
                    width += DisplayWidth.Measure(context.Glyphs.StatusMarker(Node.Status.Value)) + 1;
                }
                if (!string.IsNullOrEmpty(Node.Note)) {
                    width += DisplayWidth.Measure(Node.Note) + 2;
                }
                return width;
            }
        }
    }
}
