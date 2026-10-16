using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// The widget contract. A widget is a pure function of its definition, its state, the capabilities and the theme:
    /// the same inputs always produce the same cells. Implement it to get theming, glyph tiers, layout, live updates and
    /// every output mode for free.
    /// </summary>
    public interface IWidget {
        /// <summary>Returns the size the widget wants within the constraints.</summary>
        /// <param name="constraints">The space offered.</param>
        /// <param name="context">Profile, theme and glyphs.</param>
        /// <returns>The desired size, never larger than the constraints.</returns>
        Size Measure(Constraints constraints, RenderContext context);

        /// <summary>Renders into a region. The region may be smaller or larger than the measured size.</summary>
        /// <param name="region">The target region.</param>
        /// <param name="context">Profile, theme and glyphs.</param>
        void Render(Region region, RenderContext context);
    }
}
