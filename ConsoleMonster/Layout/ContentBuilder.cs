using System;
using System.Collections.Generic;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Layout {
    // Implemented by core containers whose content can be configured before session ownership.
    internal interface IContentOwner {
        void ReplaceContent(IWidget[] content);
    }

    /// <summary>Collects nested widgets in source order, mirroring a PowerShell Content script block.</summary>
    public sealed class ContentBuilder {
        private readonly List<IWidget> _content = new List<IWidget>();

        /// <summary>Adds a constructed widget and returns this builder.</summary>
        /// <param name="widget">The widget to add.</param>
        /// <returns>This builder.</returns>
        public ContentBuilder Add(IWidget widget) {
            _content.Add(widget ?? throw new ArgumentNullException(nameof(widget)));
            return this;
        }

        /// <summary>Adds a text widget.</summary>
        /// <param name="text">The text, including supported markup.</param>
        /// <returns>This builder.</returns>
        public ContentBuilder Text(string text) => Add(new TextBlock(text));

        /// <summary>Adds a titled panel around nested content.</summary>
        /// <param name="title">The title.</param>
        /// <param name="content">Builds the panel content.</param>
        /// <returns>This builder.</returns>
        public ContentBuilder Panel(string title, Action<ContentBuilder> content) => Add(new Panel(Build(content), title));

        /// <summary>Adds vertically composed nested content.</summary>
        /// <param name="content">Builds the rows.</param>
        /// <returns>This builder.</returns>
        public ContentBuilder Rows(Action<ContentBuilder> content) => Add(new Rows(Collect(content)));

        /// <summary>Adds horizontally composed nested content.</summary>
        /// <param name="content">Builds the columns.</param>
        /// <returns>This builder.</returns>
        public ContentBuilder Columns(Action<ContentBuilder> content) => Add(new Columns(Collect(content)));

        /// <summary>Adds overlaid nested content in painting order.</summary>
        /// <param name="content">Builds the layers.</param>
        /// <returns>This builder.</returns>
        public ContentBuilder Stack(Action<ContentBuilder> content) => Add(new Stack(Collect(content)));

        /// <summary>Adds nested content in a layout grid.</summary>
        /// <param name="columns">The number of columns.</param>
        /// <param name="content">Builds the cells.</param>
        /// <returns>This builder.</returns>
        public ContentBuilder LayoutGrid(int columns, Action<ContentBuilder> content) => Add(new Grid(columns, Collect(content)));

        /// <summary>Composes the collected widgets; no widgets produce empty content.</summary>
        /// <returns>The one widget or a Rows container in source order.</returns>
        public IWidget Build() => Compose(_content.ToArray());

        internal static IWidget Compose(IWidget[] content) => content.Length == 0 ? new Rows() : content.Length == 1 ? content[0] : new Rows(content);
        internal static IWidget Build(Action<ContentBuilder> content) => Compose(Collect(content));
        internal static IWidget[] Collect(Action<ContentBuilder> content) {
            if (content == null) { throw new ArgumentNullException(nameof(content)); }
            var builder = new ContentBuilder();
            content(builder);
            return builder._content.ToArray();
        }

        // Collection and validation finish before changing the container, so a throwing lambda leaves it untouched.
        internal static void Replace(IContentOwner owner, Action<ContentBuilder> content) => owner.ReplaceContent(Collect(content));
        internal static void Replace(IContentOwner owner, IWidget content) => owner.ReplaceContent(new[] { content ?? throw new ArgumentNullException(nameof(content)) });
    }
}
