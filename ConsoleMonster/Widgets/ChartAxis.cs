using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    // Sparse labels share the plot's coordinates; endpoints have priority over the midpoint when space is short.
    internal static class ChartAxis {
        internal static void Draw(Region region, int row, int x, int width, IList<string> labels, int count, RenderContext context) {
            int start = 0;
            int last = Math.Min(labels.Count, count) - 1;
            if (width <= 0 || last < 0) return;
            string firstText = TextWrapper.Truncate(labels[start], width, context.Ellipsis);
            string lastText = TextWrapper.Truncate(labels[last], width, context.Ellipsis);
            Style style = context.Style(ThemeToken.Muted);
            int firstEnd = region.Write(x, row, firstText, style);
            int end = x + width;
            int lastX = Math.Max(x, end - DisplayWidth.Measure(lastText));
            if (last > start && lastX > firstEnd) region.Write(lastX, row, lastText, style);
            int middle = start + (last - start + 1) / 2;
            if (middle == start || middle == last) return;
            string text = labels[middle];
            int at = x + (int)((long)middle * (width - 1) / Math.Max(1, last)) - DisplayWidth.Measure(text) / 2;
            if (at > firstEnd && at + DisplayWidth.Measure(text) < lastX) region.Write(at, row, text, style);
        }
    }
}
