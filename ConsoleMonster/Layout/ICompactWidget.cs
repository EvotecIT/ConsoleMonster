using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// A widget with a minimum size and a compact rendering used when the screen is smaller: live sessions render the
    /// compact form instead of squeezing the full layout into garbage (for example a dashboard shows a one-line status and
    /// only its highest-priority widgets).
    /// </summary>
    public interface ICompactWidget : IWidget {
        /// <summary>The smallest size the full layout needs.</summary>
        Size MinimumSize { get; }

        /// <summary>Renders the compact form into a region smaller than <see cref="MinimumSize"/>.</summary>
        /// <param name="region">The region (possibly tiny or empty).</param>
        /// <param name="context">The render context.</param>
        void RenderCompact(Region region, RenderContext context);
    }
}
