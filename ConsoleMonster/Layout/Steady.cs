using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// Keeps the largest size its content has needed, so a live region that loses rows (finished workers, a shorter list)
    /// does not shrink and move everything below it. The size is held by this instance: keep the same <see cref="Steady"/>
    /// across frames and replace or update its content. <see cref="MinHeight"/> reserves rows before the content needs
    /// them, so the layout does not grow either. Call <see cref="Reset"/> when the region starts a new, unrelated phase.
    /// </summary>
    public sealed class Steady : IContainer, IContentOwner {
        private int _height;
        private int _width;

        /// <summary>Creates a steady region.</summary>
        /// <param name="content">The content.</param>
        public Steady(IWidget? content = null) {
            Content = content ?? new Rows();
        }

        /// <summary>The content; replacing it keeps the size held so far.</summary>
        public IWidget Content { get; set; }

        void IContentOwner.ReplaceContent(IWidget[] content) => Content = ContentBuilder.Compose(content);

        /// <summary>Rows reserved from the first frame (default zero).</summary>
        public int MinHeight { get; set; }

        /// <summary>Whether the width is held as well as the height (default false; widths usually follow the parent).</summary>
        public bool HoldWidth { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Content };

        /// <summary>Forgets the held size, so the next frame measures from the content again. In a live session, call it where the content is updated (a live event or the frame callback), like other widget changes.</summary>
        public void Reset() {
            _height = 0;
            _width = 0;
        }

        /// <inheritdoc />
        /// <remarks>Measuring has no side effects; the held size grows only from frames actually rendered.</remarks>
        public Size Measure(Constraints constraints, RenderContext context) {
            Size inner = Content.Measure(constraints, context);
            int height = Math.Max(inner.Height, Math.Max(_height, MinHeight));
            int width = HoldWidth ? Math.Max(inner.Width, _width) : inner.Width;
            return new Size(Math.Min(constraints.MaxWidth, width), constraints.IsHeightUnbounded ? height : Math.Min(constraints.MaxHeight, height));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) return;
            Size inner = Content.Measure(new Constraints(region.Width, region.Height), context);
            int height = Math.Min(region.Height, inner.Height);
            _height = Math.Max(_height, height);
            if (HoldWidth) _width = Math.Max(_width, Math.Min(region.Width, inner.Width));
            Content.Render(region.Sub(0, 0, region.Width, height), context);
        }
    }
}
