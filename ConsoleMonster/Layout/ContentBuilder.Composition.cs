using ConsoleMonster.Widgets;

namespace ConsoleMonster.Layout {
    public sealed partial class ContentBuilder {
        /// <summary>Adds a widget with an explicit size in its containing rows or columns.</summary>
        /// <param name="widget">The widget.</param><param name="size">The track size.</param><returns>This builder.</returns>
        public ContentBuilder Add(IWidget widget, LayoutSize size) => Add(new Sized(widget, size));

        /// <summary>Adds a titled panel, optionally filling the remaining height.</summary>
        /// <param name="title">The panel title.</param><param name="content">The content.</param>
        /// <param name="fill">Whether the panel fills the remaining height.</param>
        /// <param name="headerRight">Text on the right of the top edge.</param><returns>This builder.</returns>
        public ContentBuilder Panel(string title, IWidget content, bool fill = false, string? headerRight = null) {
            var panel = new Panel(content, title) { HeaderRight = headerRight };
            return fill ? Add(panel, LayoutSize.Fraction()) : Add(panel);
        }

        /// <summary>Adds a titled panel with a relative width inside columns.</summary>
        /// <param name="title">The panel title.</param><param name="content">The content.</param>
        /// <param name="weight">The relative width.</param><param name="headerRight">Text on the right of the top edge.</param>
        /// <returns>This builder.</returns>
        public ContentBuilder Panel(string title, IWidget content, double weight, string? headerRight = null) =>
            Add(new Panel(content, title) { HeaderRight = headerRight }, LayoutSize.Fraction(weight));
    }
}
