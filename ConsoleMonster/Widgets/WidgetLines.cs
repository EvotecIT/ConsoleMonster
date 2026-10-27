using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    internal static class WidgetLines {
        internal static Size Measure(List<StyledLine> lines, Constraints constraints) {
            int width = 0;
            foreach (StyledLine line in lines) width = Math.Max(width, line.Width);
            return new Size(Math.Min(width, constraints.MaxWidth), constraints.IsHeightUnbounded ? lines.Count : Math.Min(lines.Count, constraints.MaxHeight));
        }

        internal static void Render(List<StyledLine> lines, Region region) {
            for (int i = 0; i < lines.Count && i < region.Height; i++) region.Write(0, i, lines[i]);
        }

        internal static void Wrap(List<StyledLine> lines, StyledLine line, int width) {
            lines.AddRange(TextWrapper.Wrap(line, Math.Max(1, width)));
        }
    }
}
