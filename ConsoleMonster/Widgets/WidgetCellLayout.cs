using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>Renders embedded widgets at the cell's allocated width, preserving glyphs and styles.</summary>
    internal static class WidgetCellLayout {
        internal static List<StyledLine> Lines(IWidget widget, int width, RenderContext context) {
            CellBuffer frame = FrameComposer.RenderBlock(widget, Math.Max(1, width), context);
            var lines = new List<StyledLine>();
            for (int y = 0; y < frame.Height; y++) {
                int end = frame.Width;
                while (end > 0 && frame[end - 1, y].Glyph == " " && frame[end - 1, y].Style == Style.Plain) end--;
                var spans = new List<TextSpan>();
                for (int x = 0; x < end; x++) {
                    Cell cell = frame[x, y];
                    if (cell.Width != 0) spans.Add(new TextSpan(cell.Glyph, cell.Style));
                }
                lines.Add(new StyledLine(spans));
            }
            return lines;
        }
    }
}
