using System;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Building {
    /// <summary>
    /// Base of the fluent vertical-layout builders (<see cref="RowsBuilder"/>, <see cref="DashboardBuilder"/>). Every method returns the builder; <c>out</c> parameters hand back the
    /// typed widgets so callers can create live handles from them after the session starts.
    /// </summary>
    /// <typeparam name="TSelf">The concrete builder type returned by every method.</typeparam>
    public abstract class RowsBuilderBase<TSelf> where TSelf : RowsBuilderBase<TSelf> {
        private readonly Rows _rows = new Rows();

        /// <summary>Blank rows between children.</summary>
        /// <param name="gap">The gap.</param>
        /// <returns>This builder.</returns>
        public TSelf Gap(int gap) {
            _rows.Gap = Math.Max(0, gap);
            return (TSelf)this;
        }

        /// <summary>Adds a widget sized to its content.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="size">Its height specification (default auto).</param>
        /// <returns>This builder.</returns>
        public TSelf Add(IWidget widget, LayoutSize? size = null) {
            _rows.Add(widget, size);
            return (TSelf)this;
        }

        /// <summary>Adds a widget and returns it typed.</summary>
        /// <typeparam name="T">The widget type.</typeparam>
        /// <param name="widget">The widget.</param>
        /// <param name="added">The same widget, typed.</param>
        /// <param name="size">Its height specification.</param>
        /// <returns>This builder.</returns>
        public TSelf Add<T>(T widget, out T added, LayoutSize? size = null) where T : IWidget {
            added = widget;
            return Add(widget, size);
        }

        /// <summary>Adds a widget inside a titled panel.</summary>
        /// <typeparam name="T">The widget type.</typeparam>
        /// <param name="title">The panel title (markup).</param>
        /// <param name="widget">The content.</param>
        /// <param name="added">The content, typed.</param>
        /// <param name="fill">Whether the panel takes the remaining height in a live frame.</param>
        /// <param name="caption">Text on the right of the top edge.</param>
        /// <returns>This builder.</returns>
        public TSelf Panel<T>(string title, T widget, out T added, bool fill = false, string? caption = null) where T : IWidget {
            added = widget;
            return Add(new Panel(widget, title) { Caption = caption }, fill ? LayoutSize.Fraction() : (LayoutSize?)null);
        }

        /// <summary>Adds any content inside a titled panel.</summary>
        /// <param name="title">The panel title (markup).</param>
        /// <param name="content">The content.</param>
        /// <param name="fill">Whether the panel takes the remaining height in a live frame.</param>
        /// <param name="caption">Text on the right of the top edge.</param>
        /// <returns>This builder.</returns>
        public TSelf Panel(string title, IWidget content, bool fill = false, string? caption = null) {
            return Add(new Panel(content, title) { Caption = caption }, fill ? LayoutSize.Fraction() : (LayoutSize?)null);
        }

        /// <summary>
        /// Adds a panel whose content can be replaced while the session runs: the panel frame stays and the content is a
        /// <see cref="Widgets.Slot"/> named <paramref name="name"/> (see <see cref="Live.LiveSession.Region"/>).
        /// </summary>
        /// <param name="title">The panel title.</param>
        /// <param name="name">The region name.</param>
        /// <param name="content">The first content, or null for an empty region.</param>
        /// <param name="fill">Whether the panel takes the remaining height.</param>
        /// <param name="caption">A caption in the bottom border, or null.</param>
        /// <returns>This builder.</returns>
        public TSelf Region(string title, string name, IWidget? content = null, bool fill = false, string? caption = null) {
            return Panel(title, new Widgets.Slot(name, content), fill, caption);
        }

        /// <summary>Adds a row of columns.</summary>
        /// <param name="configure">Configures the columns.</param>
        /// <param name="size">Its height specification (default auto).</param>
        /// <returns>This builder.</returns>
        public TSelf Columns(Action<ColumnsBuilder> configure, LayoutSize? size = null) {
            if (configure == null) {
                throw new ArgumentNullException(nameof(configure));
            }
            ColumnsBuilder columns = new ColumnsBuilder();
            configure(columns);
            return Add(columns.Build(), size);
        }

        /// <summary>Builds the layout.</summary>
        /// <returns>The rows container.</returns>
        public Rows Build() => _rows;
    }
}
