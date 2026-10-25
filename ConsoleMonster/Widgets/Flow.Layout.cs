using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Flow {
        private sealed class Plan {
            internal int Width, Height;
            internal readonly List<Action<Region>> Draw = new List<Action<Region>>();
            internal void Text(int x, int y, string text, Style style) => Draw.Add(r => r.Write(x, y, text, style));
            internal void Horizontal(int x, int y, int count, string glyph, Style style) => Draw.Add(r => r.HorizontalLine(x, y, count, glyph, style));
            internal void Vertical(int x, int y, int count, string glyph, Style style) => Draw.Add(r => r.VerticalLine(x, y, count, glyph, style));
        }

        private Plan Layout(int width, RenderContext context) {
            var plan = new Plan();
            if (_nodes.Count == 0 || width <= 0) return plan;
            bool ascii = context.Glyphs.Tier == Capabilities.GlyphTier.Ascii;
            BoxGlyphs box = context.Glyphs.GetBox(BoxStyle.Single);
            string right = ascii ? ">" : "→", down = ascii ? "v" : "▼", cut = ascii ? "x" : "✗";
            string[] labels = _nodes.Select(n => Marker(n.Status, context) + Literal(n.Label)).ToArray();
            int[] widths = labels.Select(l => DisplayWidth.Measure(l) + 4).ToArray();
            int[] gaps = Enumerable.Range(0, Math.Max(0, _nodes.Count - 1)).Select(i => Math.Max(5, DisplayWidth.Measure(EdgeText(Edge(i), context)) + 2)).ToArray();
            int natural = widths.Sum() + gaps.Sum();
            bool horizontal = natural <= width;
            var xs = new int[_nodes.Count];
            var ys = new int[_nodes.Count];
            var heights = new int[_nodes.Count];
            int x = 0, y = 0;
            for (int i = 0; i < _nodes.Count; i++) {
                int nodeWidth = horizontal ? widths[i] : Math.Min(width - (Branch?.To != null ? 3 : 0), widths.Max());
                nodeWidth = Math.Max(1, nodeWidth);
                xs[i] = x; ys[i] = y;
                heights[i] = DrawNode(plan, x, y, nodeWidth, labels[i], _nodes[i].Status, box, context);
                widths[i] = nodeWidth;
                if (!horizontal && i > 0 && nodeWidth >= 5) {
                    plan.Text(2, y, down, context.Style(Edge(i - 1).Status?.ToToken() ?? ThemeToken.Muted));
                }
                if (i < _nodes.Count - 1) {
                    FlowEdge edge = Edge(i);
                    Style style = context.Style(edge.Status?.ToToken() ?? ThemeToken.Muted);
                    string edgeLabel = EdgeText(edge, context);
                    if (horizontal) {
                        int edgeX = x + nodeWidth;
                        plan.Text(edgeX + 1, y, edgeLabel, style);
                        plan.Horizontal(edgeX, y + 1, gaps[i] - 1, box.Horizontal, style);
                        if (edge.Cut) plan.Text(edgeX + gaps[i] / 2 - 1, y + 1, " " + cut + " ", context.Style(edge.Status?.ToToken() ?? ThemeToken.Fail));
                        plan.Text(edgeX + gaps[i] - 1, y + 1, right, style);
                        x += nodeWidth + gaps[i];
                    } else {
                        int bottom = y + heights[i] - 1;
                        if (nodeWidth >= 5) plan.Text(2, bottom, ascii ? box.Vertical : box.TopJoint, style);
                        y = bottom + 1;
                        int connectorTop = y;
                        if (Branch?.From == _nodes[i].Key) {
                            var branchLines = Words(Marker(Branch.Status, context) + Literal(Branch.Label), Math.Max(1, nodeWidth - 6));
                            foreach (string line in branchLines) plan.Text(6, y++, line, context.Style(Branch.Status?.ToToken() ?? ThemeToken.Muted));
                        }
                        List<string> edgeLines = Words(edgeLabel, Math.Max(1, nodeWidth - 4));
                        int edgeHeight = Math.Max(edge.Cut ? 3 : 2, edgeLines.Count);
                        plan.Vertical(2, connectorTop, y - connectorTop + edgeHeight, box.Vertical, style);
                        if (edge.Cut) plan.Text(2, y + 1, cut, context.Style(edge.Status?.ToToken() ?? ThemeToken.Fail));
                        for (int j = 0; j < edgeLines.Count; j++) plan.Text(4, y + j, edgeLines[j], style);
                        y += edgeHeight;
                    }
                }
            }
            plan.Width = horizontal ? natural : Math.Min(width, widths.Max() + (Branch?.To != null ? 3 : 0));
            plan.Height = horizontal ? 3 : y + heights[heights.Length - 1];
            if (Branch != null) {
                int source = Index(Branch.From);
                Style style = context.Style(Branch.Status?.ToToken() ?? ThemeToken.Muted);
                if (horizontal) {
                    int from = xs[source] + widths[source] / 2;
                    string branchText = Marker(Branch.Status, context) + Literal(Branch.Label);
                    int labelX = Math.Min(from, Math.Max(0, width - DisplayWidth.Measure(branchText)));
                    List<string> lines = Words(branchText, Math.Max(1, width - labelX));
                    const int bottom = 4;
                    plan.Vertical(from, 3, 1, box.Vertical, style);
                    int to = Branch.To == null ? Math.Min(width - 1, from + 3) : xs[Index(Branch.To)] + widths[Index(Branch.To)] / 2;
                    plan.Text(from, bottom, box.BottomLeft, style);
                    plan.Horizontal(from + 1, bottom, Math.Max(0, to - from - 1), box.Horizontal, style);
                    plan.Text(to, bottom, Branch.To == null ? right : box.BottomRight, style);
                    if (Branch.To != null) {
                        plan.Vertical(to, 3, 1, box.Vertical, style);
                        plan.Text(to, 3, ascii ? "^" : "↑", style);
                    }
                    for (int j = 0; j < lines.Count; j++) plan.Text(labelX, 5 + j, lines[j], style);
                    plan.Width = Math.Max(plan.Width, Math.Min(width, labelX + lines.Max(DisplayWidth.Measure)));
                    plan.Height = 5 + lines.Count;
                } else if (Branch.To != null) {
                    int destination = Index(Branch.To), gutter = plan.Width - 1;
                    int top = ys[source] + 1, bottom = ys[destination] + 1;
                    plan.Horizontal(widths[source], top, gutter - widths[source], box.Horizontal, style);
                    plan.Text(gutter, top, box.TopRight, style);
                    plan.Vertical(gutter, top + 1, bottom - top - 1, box.Vertical, style);
                    plan.Text(gutter, bottom, box.BottomRight, style);
                    plan.Horizontal(widths[destination], bottom, gutter - widths[destination], box.Horizontal, style);
                    plan.Text(widths[destination], bottom, ascii ? "<" : "←", style);
                } else {
                    // The branch label has its own row below its source, before the main outgoing arrow.
                    int top = ys[source] + heights[source];
                    if (source == _nodes.Count - 1) {
                        List<string> lines = Words(Marker(Branch.Status, context) + Literal(Branch.Label), Math.Max(1, widths[source] - 6));
                        for (int j = 0; j < lines.Count; j++) plan.Text(6, top + j, lines[j], style);
                        plan.Height += lines.Count;
                    }
                    if (source == _nodes.Count - 1 && widths[source] >= 5)
                        plan.Text(2, top - 1, ascii ? box.Vertical : box.TopJoint, style);
                    plan.Text(2, top, (source == _nodes.Count - 1 ? box.BottomLeft : box.LeftJoint) + box.Horizontal + right, style);
                }
            }
            return plan;
        }

        private static int DrawNode(Plan plan, int x, int y, int width, string label, StatusKind? status, BoxGlyphs box, RenderContext context) {
            List<string> lines = Words(label, Math.Max(1, width - 4));
            Style border = context.Style(status?.ToToken() ?? ThemeToken.Border);
            Style text = context.Style(status?.ToToken() ?? ThemeToken.Text);
            if (width < 5) {
                for (int j = 0; j < lines.Count; j++) plan.Text(x, y + j, lines[j], text);
                return lines.Count;
            }
            plan.Text(x, y, box.TopLeft, border);
            plan.Horizontal(x + 1, y, width - 2, box.Horizontal, border);
            plan.Text(x + width - 1, y, box.TopRight, border);
            for (int j = 0; j < lines.Count; j++) {
                plan.Text(x, y + j + 1, box.Vertical, border);
                plan.Text(x + 2, y + j + 1, lines[j], text);
                plan.Text(x + width - 1, y + j + 1, box.Vertical, border);
            }
            int bottom = y + lines.Count + 1;
            plan.Text(x, bottom, box.BottomLeft, border);
            plan.Horizontal(x + 1, bottom, width - 2, box.Horizontal, border);
            plan.Text(x + width - 1, bottom, box.BottomRight, border);
            return lines.Count + 2;
        }

        private static string Marker(StatusKind? status, RenderContext context) => status.HasValue ? context.Glyphs.StatusMarker(status.Value) + " " : string.Empty;
        private static string Literal(string text) => text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        private static string EdgeText(FlowEdge edge, RenderContext context) => Marker(edge.Status, context) + Literal(edge.Label ?? string.Empty);

        // Wrap only at whitespace. An indivisible word wider than the available space becomes an ellipsis,
        // rather than splitting a word or a grapheme across a box border.
        private static List<string> Words(string text, int width) {
            var lines = new List<string>();
            string current = string.Empty;
            foreach (string word in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
                string next = current.Length == 0 ? word : current + " " + word;
                if (DisplayWidth.Measure(next) > width && current.Length > 0) { lines.Add(current); current = string.Empty; }
                string fitting = DisplayWidth.Measure(word) <= width ? word : new string('.', Math.Min(3, width));
                current = current.Length == 0 ? fitting : current + " " + fitting;
            }
            if (current.Length > 0 || lines.Count == 0) lines.Add(current);
            return lines;
        }
    }
}
