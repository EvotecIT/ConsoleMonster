using System;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Widgets {
    public static partial class WidgetsFluent {
        /// <summary>Builds the left (or top) pane.</summary><param name="splitter">The splitter.</param><param name="content">The pane's content.</param><returns>The splitter.</returns>
        public static Splitter First(this Splitter splitter, Action<ContentBuilder> content) {
            if (splitter == null) { throw new ArgumentNullException(nameof(splitter)); }
            splitter.First = ContentBuilder.Build(content ?? throw new ArgumentNullException(nameof(content)));
            return splitter;
        }

        /// <summary>Builds the right (or bottom) pane.</summary><param name="splitter">The splitter.</param><param name="content">The pane's content.</param><returns>The splitter.</returns>
        public static Splitter Second(this Splitter splitter, Action<ContentBuilder> content) {
            if (splitter == null) { throw new ArgumentNullException(nameof(splitter)); }
            splitter.Second = ContentBuilder.Build(content ?? throw new ArgumentNullException(nameof(content)));
            return splitter;
        }
    }
}
