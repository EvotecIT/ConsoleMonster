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
                StyledLine guide = StyledLine.From(row.Prefix, context.Style(ThemeToken.Border));
                string? markerText = node.MarkerFor(context);
                StyledLine marker = !string.IsNullOrEmpty(markerText)
                    ? StyledLine.From(markerText + " ", node.MarkerStyle(context))
                    : StyledLine.Empty;
                // Keep the outcome before spending cells on the tree guide; leave at least one cell for the label.
                int markerWidth = Math.Min(marker.Width, Math.Max(0, width - 1));
                int prefixLimit = Math.Max(markerWidth, Math.Max(0, width - 4));
                int guideWidth = Math.Min(guide.Width, Math.Max(0, prefixLimit - markerWidth));
                List<TextSpan> prefix = new List<TextSpan>(TextWrapper.Truncate(guide, guideWidth, string.Empty));
                prefix.AddRange(TextWrapper.Truncate(marker, markerWidth, string.Empty));
                StyledLine firstPrefix = new StyledLine(prefix);
                int indent = firstPrefix.Width;
                List<TextSpan> content = new List<TextSpan>();
                AddLines(content, context.Markup(node.LabelFor(context)).Lines);
                if (node.LabelPadding > 0) { content.Add(new TextSpan(new string(' ', node.LabelPadding))); }
                if (node.Value != null) {
                    content.Add(new TextSpan("  "));
                    AddLines(content, CellRendering.Content(node.Value, node.Presenter, null, presenter).Lines);
                }
                string? note = row.NoteFor(context);
                if (!string.IsNullOrEmpty(note)) content.Add(new TextSpan("  " + note, context.Style(ThemeToken.Muted)));
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
