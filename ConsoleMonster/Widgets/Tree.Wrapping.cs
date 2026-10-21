using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Tree {
        private List<StyledLine> WrappedLines(int width, RenderContext context) {
            List<StyledLine> lines = new List<StyledLine>();
            PresenterContext presenter = Presentation.CreateContext(context);
            foreach (TreeRow row in Flatten(context)) {
                TreeNode node = row.Node;
                List<TextSpan> prefix = new List<TextSpan> { new TextSpan(row.Prefix, context.Style(ThemeToken.Border)) };
                if (node.Status.HasValue) prefix.Add(new TextSpan(context.Glyphs.StatusMarker(node.Status.Value) + " ", context.Style(node.Status.Value)));
                int indent = Math.Min(new StyledLine(prefix).Width, Math.Max(0, width - 4));
                StyledLine firstPrefix = TextWrapper.Truncate(new StyledLine(prefix), indent, string.Empty);
                List<TextSpan> content = new List<TextSpan>();
                AddLines(content, context.Markup(node.Label).Lines);
                if (node.Value != null) {
                    content.Add(new TextSpan("  "));
                    AddLines(content, CellRendering.Content(node.Value, node.Presenter, null, presenter).Lines);
                }
                if (!string.IsNullOrEmpty(node.Note)) content.Add(new TextSpan("  " + node.Note, context.Style(ThemeToken.Muted)));
                IReadOnlyList<StyledLine> wrapped = TextWrapper.Wrap(new StyledLine(content), Math.Max(1, width - indent));
                for (int i = 0; i < wrapped.Count; i++) {
                    List<TextSpan> spans = i == 0 ? new List<TextSpan>(firstPrefix) : new List<TextSpan> { new TextSpan(new string(' ', indent)) };
                    spans.AddRange(wrapped[i]);
                    lines.Add(new StyledLine(spans));
                }
            }
            return lines;
        }

        private static void AddLines(List<TextSpan> spans, IReadOnlyList<StyledLine> lines) {
            for (int i = 0; i < lines.Count; i++) {
                if (i > 0) spans.Add(new TextSpan(" "));
                spans.AddRange(lines[i]);
            }
        }
    }
}
