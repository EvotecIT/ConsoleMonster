using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets;

public sealed partial class PathSet {
    private List<StyledLine> BranchLines(int width, RenderContext context) {
        List<StyledLine> lines = new List<StyledLine>();
        if (!string.IsNullOrEmpty(Title)) WidgetLines.Wrap(lines, context.Markup(Title!).Lines[0], width);
        WidgetLines.Wrap(lines, Hop(Origin), width);
        int shown = MaxPaths <= 0 ? _paths.Count : Math.Min(MaxPaths, _paths.Count);
        for (int p = 0; p < shown; p++) {
            List<TextSpan> current = new List<TextSpan> { new TextSpan(context.Glyphs.Get(p == shown - 1 ? Glyph.PathLast : Glyph.PathBranch) + " ", context.Style(ThemeToken.Subtle)) };
            for (int h = 1; h < _paths[p].Length; h++) {
                StyledLine hop = Hop(_paths[p][h]);
                string arrow = h > 1 ? " " + context.Glyphs.Get(Glyph.ArrowRight) + " " : string.Empty;
                if (h > 1 && new StyledLine(current).Width + DisplayWidth.Measure(arrow) + hop.Width > width) {
                    WidgetLines.Wrap(lines, new StyledLine(current), width);
                    current.Clear(); current.Add(new TextSpan("    "));
                    arrow = context.Glyphs.Get(Glyph.ArrowRight) + " ";
                }
                if (arrow.Length > 0) current.Add(new TextSpan(arrow, context.Style(ThemeToken.Muted)));
                current.AddRange(hop);
            }
            StyledLine chain = new StyledLine(current);
            if (chain.Width <= width) lines.Add(chain);
            else {
                IReadOnlyList<StyledLine> wrapped = TextWrapper.Wrap(chain, Math.Max(1, width - 4));
                for (int line = 0; line < wrapped.Count; line++) {
                    List<TextSpan> continuation = new List<TextSpan>();
                    if (line > 0) continuation.Add(new TextSpan(new string(' ', Math.Min(4, Math.Max(0, width - 1)))));
                    continuation.AddRange(wrapped[line]); lines.Add(new StyledLine(continuation));
                }
            }
        }
        if (shown < _paths.Count) lines.AddRange(new ViewHint { MoreRows = _paths.Count - shown }.Lines(width, context));
        return lines;

        StyledLine Hop(PathHop hop) {
            List<TextSpan> spans = new List<TextSpan> { new TextSpan(hop.Label, context.Style(hop.Role ?? ThemeToken.Text).With(hop.Bold ? TextDecoration.Bold : TextDecoration.None)) };
            foreach (string chip in hop.Chips) spans.Add(new TextSpan(" " + chip, context.Style(ThemeToken.Muted)));
            if (hop.Status.HasValue) spans.Add(new TextSpan(" " + context.Glyphs.StatusMarker(hop.Status.Value), context.Style(hop.Status.Value)));
            return new StyledLine(spans);
        }
    }
}
