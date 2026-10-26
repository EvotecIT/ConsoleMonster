using ConsoleMonster.Layout;

namespace ConsoleMonster {
    /// <summary>Layout-sizing endings shared by fluent chains and the PowerShell container adapter.</summary>
    public static class WidgetLayout {
        /// <summary>Lets the widget take a share of the remaining row height or column width.</summary>
        /// <param name="widget">The configured widget.</param><param name="weight">Its relative share.</param><returns>The sized widget.</returns>
        public static Sized Fill(this IWidget widget, double weight = 1) => new Sized(widget, LayoutSize.Fraction(weight));
        /// <summary>Reserves a fixed number of cells for the widget.</summary>
        /// <param name="widget">The configured widget.</param><param name="cells">The reserved cells.</param><returns>The sized widget.</returns>
        public static Sized Fixed(this IWidget widget, int cells) => new Sized(widget, LayoutSize.Fixed(cells));
    }
}
