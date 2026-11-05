using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// One part of a document drawn at one width with its current folds: the cells, where each heading is, which section
    /// owns each row, the links and the rows as text. Headings are <see cref="Collapsible"/> header rows.
    /// </summary>
    internal sealed class DocumentLayout {
        private DocumentLayout(CellBuffer buffer, int[] headingRows, int[] rowSections, List<DocumentLink> links, List<string> lines) {
            Buffer = buffer;
            HeadingRows = headingRows;
            RowSections = rowSections;
            Links = links;
            Lines = lines;
        }

        internal CellBuffer Buffer { get; }

        /// <summary>The row of each section's heading, or -1 when the section has no heading or is inside a folded one.</summary>
        internal int[] HeadingRows { get; }

        /// <summary>The section each row belongs to.</summary>
        internal int[] RowSections { get; }

        internal List<DocumentLink> Links { get; }

        internal List<string> Lines { get; }

        internal int Height => Buffer.Height;

        /// <summary>The indent of a heading row: two columns per level below the part's shallowest heading.</summary>
        internal static int HeadingIndent(DocumentPart part, DocumentSection section) => Math.Max(0, section.Level - MinimumLevel(part)) * 2;

        internal static int MinimumLevel(DocumentPart part) {
            int minimum = int.MaxValue;
            foreach (DocumentSection section in part.Sections) { if (section.Level > 0) { minimum = Math.Min(minimum, section.Level); } }
            return minimum == int.MaxValue ? 1 : minimum;
        }

        /// <summary>Whether each section shows: a folded heading hides every deeper section up to the next one at its level.</summary>
        internal static bool[] Visible(DocumentPart part, bool[] folded) {
            bool[] visible = new bool[part.Sections.Count];
            int foldLevel = int.MaxValue;
            for (int i = 0; i < part.Sections.Count; i++) {
                DocumentSection section = part.Sections[i];
                if (foldLevel != int.MaxValue && section.Level > foldLevel) { continue; }
                foldLevel = int.MaxValue;
                visible[i] = true;
                if (section.Level > 0 && folded[i]) { foldLevel = section.Level; }
            }
            return visible;
        }

        /// <summary>The headings that contain a section, outermost first, ending with the section itself.</summary>
        internal static List<int> Path(DocumentPart part, int index) {
            List<int> path = new List<int>();
            if (index < 0 || index >= part.Sections.Count) { return path; }
            int level = part.Sections[index].Level;
            if (level > 0) { path.Add(index); }
            for (int i = index - 1; i >= 0 && level > 1; i--) {
                int candidate = part.Sections[i].Level;
                if (candidate > 0 && candidate < level) { path.Insert(0, i); level = candidate; }
            }
            return path;
        }

        internal static DocumentLayout Build(DocumentPart part, bool[] folded, IList<Collapsible> headings, int width, RenderContext context) {
            width = Math.Max(1, width);
            bool[] visible = Visible(part, folded);
            List<CellBuffer> pieces = new List<CellBuffer>();
            List<int> owners = new List<int>();
            int[] headingRows = new int[part.Sections.Count];
            int row = 0;
            bool first = true;
            bool headed = part.Sections.Exists(section => section.Level > 0);
            for (int i = 0; i < part.Sections.Count; i++) {
                headingRows[i] = -1;
                if (!visible[i]) { continue; }
                DocumentSection section = part.Sections[i];
                if (!first) { pieces.Add(new CellBuffer(width, 1)); owners.Add(i); row++; }
                first = false;
                int indent = section.Level > 0 ? HeadingIndent(part, section) : 0;
                if (section.Level > 0) {
                    Collapsible heading = headings[i];
                    heading.Expanded = !folded[i];
                    pieces.Add(FrameComposer.RenderBlock(new Padding(heading, indent, 0, 0, 0), width, context));
                    owners.Add(i);
                    headingRows[i] = row++;
                }
                if (section.Body != null && (section.Level == 0 || !folded[i])) {
                    // Bodies line up with their heading's title; text without a heading with the fold marks.
                    int bodyIndent = section.Level > 0 ? indent + 4 : headed ? 2 : 0;
                    CellBuffer body = FrameComposer.RenderBlock(new Padding(section.Body, Math.Min(bodyIndent, Math.Max(0, width - 1)), 0, 0, 0), width, context);
                    pieces.Add(body);
                    owners.Add(i);
                    row += body.Height;
                }
            }
            CellBuffer buffer = new CellBuffer(width, Math.Max(1, row));
            int[] rowSections = new int[buffer.Height];
            int y = 0;
            for (int p = 0; p < pieces.Count; p++) {
                CellBuffer piece = pieces[p];
                buffer.Blit(piece, 0, 0, Math.Min(width, piece.Width), piece.Height, 0, y);
                for (int r = 0; r < piece.Height; r++) { rowSections[y + r] = owners[p]; }
                y += piece.Height;
            }
            return new DocumentLayout(buffer, headingRows, rowSections, FindLinks(buffer), Text(buffer));
        }

        /// <summary>A section's body as text at a width, which find searches inside folded sections.</summary>
        internal static List<string> BodyText(DocumentSection section, int width, RenderContext context) {
            List<string> lines = new List<string> { section.Title };
            if (section.Body != null) { lines.AddRange(Text(FrameComposer.RenderBlock(section.Body, Math.Max(1, width), context))); }
            return lines;
        }

        private static List<string> Text(CellBuffer buffer) {
            List<string> lines = new List<string>(buffer.Height);
            StringBuilder row = new StringBuilder();
            for (int y = 0; y < buffer.Height; y++) {
                row.Clear();
                for (int x = 0; x < buffer.Width; x++) {
                    Cell cell = buffer[x, y];
                    if (!cell.IsContinuation) { row.Append(cell.Glyph.Length == 0 ? " " : cell.Glyph); }
                }
                lines.Add(row.ToString());
            }
            return lines;
        }

        // Every run of cells carrying the same link target on a row is one link.
        private static List<DocumentLink> FindLinks(CellBuffer buffer) {
            List<DocumentLink> links = new List<DocumentLink>();
            for (int y = 0; y < buffer.Height; y++) {
                int start = -1;
                string? target = null;
                for (int x = 0; x <= buffer.Width; x++) {
                    string? link = x < buffer.Width ? buffer[x, y].Style.Link : null;
                    if (x < buffer.Width && buffer[x, y].IsContinuation && target != null) { continue; }
                    if (link == target) { continue; }
                    if (target != null) { links.Add(new DocumentLink(y, start, x - start, target)); }
                    target = link;
                    start = x;
                }
            }
            return links;
        }
    }

    /// <summary>A link drawn in a document: its row, columns and target.</summary>
    internal sealed class DocumentLink {
        internal DocumentLink(int row, int column, int width, string target) { Row = row; Column = column; Width = width; Target = target; }
        internal int Row { get; }
        internal int Column { get; }
        internal int Width { get; }
        internal string Target { get; }
        /// <summary>Whether a link target is the viewer's to follow (a heading anchor or a command's help), not the terminal's.</summary>
        internal static bool IsInternal(string target) => target.StartsWith("#", StringComparison.Ordinal) || target.StartsWith(DocumentModel.HelpScheme, StringComparison.Ordinal);
    }
}
