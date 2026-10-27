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

        /// <summary>Use a middle ellipsis for shortened identity labels, preserving both ends (default false).</summary>
        public bool TruncateLabelsInMiddle { get; set; }

        /// <summary>Use the rendering context's middle ellipsis for shortened notes and value notes,
        /// preserving both ends (default false). Labels keep priority; wrapped notes continue to wrap.</summary>
        public bool TruncateNotesInMiddle { get; set; }

        /// <summary>Mark identities seen on earlier branches as shown above instead of expanding them again.</summary>
        public bool DetectRepeatedNodes { get; set; }

        /// <summary>Produces a plain text aggregate for explicitly collapsed nodes; never enumerates descendants itself.</summary>
        public Func<TreeNode, string>? CollapsedSummary { get; set; }

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
            if (ValueColumns.Count > 0) return WidgetLines.Measure(ColumnLines(constraints.MaxWidth, context), constraints);
            if (WrapLabels) {
                int wrappedHeight = WrappedLines(constraints.MaxWidth, context).Count;
                return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? wrappedHeight : Math.Min(wrappedHeight, constraints.MaxHeight));
            }
            List<TreeRow> rows = Flatten(context);
            if (rows.Exists(row => row.Reference != TreeReferenceKind.None)) return WidgetLines.Measure(ColumnLines(constraints.MaxWidth, context), constraints);
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
            if (ValueColumns.Count > 0) {
                WidgetLines.Render(ColumnLines(region.Width, context), region);
                return;
            }
            if (WrapLabels) {
                List<StyledLine> wrapped = WrappedLines(region.Width, context);
                for (int y = 0; y < wrapped.Count && y < region.Height; y++) region.Write(0, y, wrapped[y]);
                return;
            }
            List<TreeRow> rows = Flatten(context);
            if (rows.Exists(row => row.Reference != TreeReferenceKind.None)) {
                WidgetLines.Render(ColumnLines(region.Width, context), region);
                return;
            }
            Presenters.PresenterContext presenter = Presentation.CreateContext(context);
            Style guide = context.Style(ThemeToken.Border);
            Style muted = context.Style(ThemeToken.Muted);
            for (int i = 0; i < rows.Count && i < region.Height; i++) {
                TreeRow row = rows[i];
                int x = region.Write(0, i, row.Prefix, guide);
                TreeNode node = row.Node;
                string? marker = node.MarkerFor(context);
                if (!string.IsNullOrEmpty(marker)) {
                    x = region.Write(x, i, marker!, node.MarkerStyle(context));
                    x++;
                }
                StyledLine label = FitLabel(context.Markup(node.LabelFor(context)).Lines[0], Math.Max(1, region.Width - x), context);
                x = region.Write(x, i, label);
                if (node.Value != null) {
                    StyledLine value = CellRendering.Content(node.Value, node.Presenter, null, presenter).Lines[0];
                    if (region.Width - x - 2 >= 4) {
                        x = region.Write(x + 2, i, TextWrapper.Truncate(value, region.Width - x - 2, context.Ellipsis));
                    }
                }
                int noteSpace = region.Width - x - 2;
                string? note = row.NoteFor(context);
                if (!string.IsNullOrEmpty(note) && noteSpace >= 4) {
                    region.Write(x + 2, i, FitNote(StyledLine.From(note!, muted), noteSpace, context));
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
            HashSet<object> path = new HashSet<object>();
            HashSet<object> seen = new HashSet<object>();
            object identity = Root.Identity ?? Root;
            path.Add(identity);
            seen.Add(identity);
            if (ShowRoot) {
                rows.Add(Row(string.Empty, Root, Root.Reference));
            }
            if (!Stops(Root.Reference)) Walk(Root, string.Empty, rows, context, path, seen);
            return rows;
        }

        private void Walk(TreeNode node, string indent, List<TreeRow> rows, RenderContext context, HashSet<object> path, HashSet<object> seen) {
            IGlyphSet glyphs = context.Glyphs;
            for (int i = 0; i < node.Children.Count; i++) {
                bool last = i == node.Children.Count - 1;
                TreeNode child = node.Children[i];
                object identity = child.Identity ?? child;
                TreeReferenceKind reference = child.Reference;
                if (path.Contains(identity)) reference |= TreeReferenceKind.Cycle;
                else if (DetectRepeatedNodes && seen.Contains(identity)) reference |= TreeReferenceKind.Repeat;
                rows.Add(Row(indent + glyphs.Get(last ? Glyph.TreeLast : Glyph.TreeBranch), child, reference));
                seen.Add(identity);
                if (Stops(reference)) continue;
                path.Add(identity);
                Walk(child, indent + glyphs.Get(last ? Glyph.TreeSpace : Glyph.TreeVertical), rows, context, path, seen);
                path.Remove(identity);
            }
        }

        private TreeRow Row(string prefix, TreeNode node, TreeReferenceKind reference) => new TreeRow(prefix, node, reference,
            (reference & TreeReferenceKind.Collapsed) != 0 ? CollapsedSummary?.Invoke(node) ?? node.ReferenceSummary : node.ReferenceSummary);

        private static bool Stops(TreeReferenceKind reference) => (reference & (TreeReferenceKind.Cycle | TreeReferenceKind.Repeat | TreeReferenceKind.Collapsed)) != 0;

        private sealed class TreeRow {
            public TreeRow(string prefix, TreeNode node, TreeReferenceKind reference, string? summary) {
                Prefix = prefix;
                Node = node;
                Reference = reference;
                Summary = summary;
            }

            public string Prefix { get; }

            public TreeNode Node { get; }
            public TreeReferenceKind Reference { get; }
            private string? Summary { get; }

            public string? NoteFor(RenderContext context) {
                string? note = Node.NoteFor(context);
                List<string> parts = new List<string>();
                if (!string.IsNullOrEmpty(note)) parts.Add(note!);
                string markers = MarkersFor(context);
                if (markers.Length > 0) parts.Add(markers);
                if (!string.IsNullOrEmpty(Summary)) parts.Add(Summary!);
                return parts.Count == 0 ? null : string.Join(" ", parts);
            }

            public string? DetailFor(RenderContext context) => string.IsNullOrEmpty(Summary) ? Node.NoteFor(context) :
                (Node.NoteFor(context) is string note ? note + " " : string.Empty) + Summary;

            public string MarkersFor(RenderContext context) {
                bool ascii = context.Glyphs.Tier != Capabilities.GlyphTier.Full;
                List<string> parts = new List<string>();
                if ((Reference & TreeReferenceKind.Cycle) != 0) parts.Add((ascii ? "@" : "↺") + (Node.HasLocalizedCycleNote ? string.Empty : " cycle"));
                else if ((Reference & TreeReferenceKind.Repeat) != 0) parts.Add((ascii ? "@" : "↪") + " shown above");
                if ((Reference & TreeReferenceKind.Collapsed) != 0) parts.Add((ascii ? ">" : "▸") + " collapsed");
                if ((Reference & TreeReferenceKind.Incomplete) != 0) parts.Add("? incomplete");
                return string.Join(" ", parts);
            }

            public int Width(RenderContext context) {
                int width = DisplayWidth.Measure(Prefix) + context.Markup(Node.LabelFor(context)).Width;
                string? marker = Node.MarkerFor(context);
                if (!string.IsNullOrEmpty(marker)) {
                    width += DisplayWidth.Measure(marker!) + 1;
                }
                string? note = NoteFor(context);
                if (!string.IsNullOrEmpty(note)) {
                    width += DisplayWidth.Measure(note) + 2;
                }
                return width;
            }
        }
    }
}
