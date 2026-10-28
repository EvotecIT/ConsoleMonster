using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets;

/// <summary>A rich legend laid out in an aligned row-major grid with three-cell gaps.</summary>
public sealed class Legend : IWidget {
    /// <summary>Creates a legend from markup entries. Empty entries are omitted.</summary>
    public Legend(params string[] entries) {
        if (entries != null) { foreach (string entry in entries) { if (!string.IsNullOrWhiteSpace(entry)) { Entries.Add(entry); } } }
    }
    /// <summary>Markup entries in display order.</summary>
    public IList<string> Entries { get; } = new List<string>();
    internal List<StyledLine> Lines(int width, RenderContext context) => Build(width, context);
    private List<StyledLine> Build(int width, RenderContext context) {
        List<StyledLine> entries = new List<StyledLine>();
        foreach (string entry in Entries) { entries.Add(context.Markup(entry).Lines[0]); }
        List<StyledLine> lines = new List<StyledLine>();
        if (entries.Count == 0) { return lines; }
        int columns = entries.Count;
        int[] widths;
        while (true) {
            widths = new int[columns];
            for (int i = 0; i < entries.Count; i++) { widths[i % columns] = Math.Max(widths[i % columns], entries[i].Width); }
            int total = 3 * (columns - 1);
            foreach (int value in widths) { total += value; }
            if (total <= width || columns == 1) { break; }
            columns--;
        }
        for (int start = 0; start < entries.Count; start += columns) {
            List<TextSpan> spans = new List<TextSpan>();
            for (int c = 0; c < columns && start + c < entries.Count; c++) {
                StyledLine entry = entries[start + c];
                foreach (TextSpan span in entry) { spans.Add(span); }
                if (c + 1 < columns && start + c + 1 < entries.Count) { spans.Add(new TextSpan(new string(' ', widths[c] - entry.Width + 3))); }
            }
            lines.Add(TextWrapper.Truncate(new StyledLine(spans), width, context.Ellipsis));
        }
        return lines;
    }
    /// <inheritdoc />
    public Size Measure(Constraints constraints, RenderContext context) {
        List<StyledLine> lines = Build(constraints.MaxWidth, context);
        int width = 0;
        foreach (StyledLine line in lines) { width = Math.Max(width, line.Width); }
        return new Size(width, Math.Min(lines.Count, constraints.MaxHeight));
    }
    /// <inheritdoc />
    public void Render(Region region, RenderContext context) {
        List<StyledLine> lines = Build(region.Width, context);
        for (int y = 0; y < lines.Count && y < region.Height; y++) { region.Write(0, y, lines[y]); }
    }
}
