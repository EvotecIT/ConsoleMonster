using System;
using System.Collections.Generic;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Layout {
    // Implemented by core containers whose content can be configured before session ownership.
    internal interface IContentOwner {
        void ReplaceContent(IWidget[] content);
    }

    /// <summary>Collects nested widgets in source order, mirroring a PowerShell Content script block.</summary>
    public sealed partial class ContentBuilder {
        private readonly List<IWidget> _content = new List<IWidget>();

        /// <summary>Adds a constructed widget and returns this builder.</summary>
        /// <param name="widget">The widget to add.</param>
        /// <returns>This builder.</returns>
        public ContentBuilder Add(IWidget widget) {
            _content.Add(widget ?? throw new ArgumentNullException(nameof(widget)));
            return this;
        }

        /// <summary>Composes the collected widgets; no widgets produce empty content.</summary>
        /// <returns>The one widget or a Rows container in source order.</returns>
        public IWidget Build() => Compose(_content.ToArray());

        internal static IWidget Compose(IWidget[] content) => content.Length == 0 ? new Rows() : content.Length == 1 ? content[0] : new Rows(content) { ComposedContent = true };
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
