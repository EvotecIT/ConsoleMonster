using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Layout {
    // A one-cell gap separates unframed content; borders can provide that separation for framed tiles.
    internal static class LayoutSpacing {
        internal static int Gap(int comfortable, RenderContext context, IReadOnlyList<IWidget>? children = null) {
            if (context.Density != ThemeDensity.Compact || comfortable == 0) { return comfortable; }
            if (comfortable > 1) { return comfortable - 1; }
            if (children == null || children.Count < 2) { return comfortable; }
            foreach (IWidget child in children) {
                if (!HasFrame(child)) { return comfortable; }
            }
            return 0;
        }

        private static bool HasFrame(IWidget widget) {
            while (true) {
                if (widget is Sized sized) { widget = sized.Widget; }
                else if (widget is Prioritized prioritized) { widget = prioritized.Child; }
                else { break; }
            }
            // Tables can switch to unframed records or cards, so their Border setting is not a frame guarantee.
            return widget is Panel panel && panel.Border != BoxStyle.None;
        }
    }
}
