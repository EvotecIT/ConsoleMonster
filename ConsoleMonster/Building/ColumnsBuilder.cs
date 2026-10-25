using System;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Building {
    /// <summary>Fluent composition of side-by-side columns (used through <c>RowsBuilder.Columns</c>).</summary>
    public sealed class ColumnsBuilder {
        private readonly Columns _columns = new Columns();

        /// <summary>Blank columns between children.</summary>
        /// <param name="gap">The gap.</param>
        /// <returns>This builder.</returns>
        public ColumnsBuilder Gap(int gap) {
            _columns.Gap = Math.Max(0, gap);
            return this;
        }

        /// <summary>Adds a widget with a share of the width.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="weight">Relative width (default 1).</param>
        /// <returns>This builder.</returns>
        public ColumnsBuilder Add(IWidget widget, double weight = 1) {
            _columns.Add(widget, LayoutSize.Fraction(weight));
            return this;
        }

        /// <summary>Adds a widget with a fixed width.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="cells">The width in cells.</param>
        /// <returns>This builder.</returns>
        public ColumnsBuilder AddFixed(IWidget widget, int cells) {
            _columns.Add(widget, LayoutSize.Fixed(cells));
            return this;
        }

        /// <summary>Adds a widget inside a titled panel with a share of the width.</summary>
        /// <typeparam name="T">The widget type.</typeparam>
        /// <param name="title">The panel title (markup).</param>
        /// <param name="widget">The content.</param>
        /// <param name="added">The content, typed.</param>
        /// <param name="weight">Relative width (default 1).</param>
        /// <param name="headerRight">Text on the right of the top edge.</param>
        /// <returns>This builder.</returns>
        public ColumnsBuilder Panel<T>(string title, T widget, out T added, double weight = 1, string? headerRight = null) where T : IWidget {
            added = widget;
            return Add(new Panel(widget, title) { HeaderRight = headerRight }, weight);
        }

        /// <summary>Adds a panel around any content (for example a <see cref="Rows"/> of several widgets).</summary>
        /// <param name="title">The panel title (markup).</param>
        /// <param name="content">The content.</param>
        /// <param name="weight">Relative width.</param>
        /// <param name="headerRight">Text on the right of the top edge.</param>
        /// <returns>This builder.</returns>
        public ColumnsBuilder Panel(string title, IWidget content, double weight = 1, string? headerRight = null) {
            return Add(new Panel(content, title) { HeaderRight = headerRight }, weight);
        }

        /// <summary>Builds the layout.</summary>
        /// <returns>The columns container.</returns>
        public Columns Build() => _columns;
    }
}
