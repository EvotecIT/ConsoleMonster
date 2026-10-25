using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// Places a content at its measured size inside a larger region: left, centre or right, and top, middle or bottom.
    /// Horizontally aligned content claims the full offered width so it can be centred or pushed right; vertical
    /// alignment applies when the parent gives the content more rows than it measured (a fixed height or a fill share).
    /// Children that already measure to the full width, such as progress bars, are unaffected horizontally.
    /// </summary>
    public sealed class Align : IContainer, IContentOwner {
        /// <summary>Creates an alignment wrapper.</summary>
        /// <param name="content">The content.</param>
        /// <param name="horizontal">Horizontal placement (default centre).</param>
        /// <param name="vertical">Vertical placement (default top).</param>
        public Align(IWidget? content = null, HorizontalAlignment horizontal = HorizontalAlignment.Center, VerticalAlignment vertical = VerticalAlignment.Top) {
            Content = content ?? new Rows();
            Horizontal = horizontal;
            Vertical = vertical;
        }

        /// <summary>The content.</summary>
        public IWidget Content { get; set; }

        void IContentOwner.ReplaceContent(IWidget[] content) => Content = ContentBuilder.Compose(content);

        /// <summary>Horizontal placement.</summary>
        public HorizontalAlignment Horizontal { get; set; }

        /// <summary>Vertical placement.</summary>
        public VerticalAlignment Vertical { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Content };

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            Size inner = Content.Measure(constraints, context);
            int width = Horizontal == HorizontalAlignment.Left ? inner.Width : constraints.MaxWidth;
            return new Size(width, inner.Height);
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            Size inner = Content.Measure(new Constraints(region.Width, region.Height), context);
            int width = Math.Min(inner.Width, region.Width);
            int height = Math.Min(inner.Height, region.Height);
            int x = Offset(region.Width - width, Horizontal == HorizontalAlignment.Center, Horizontal == HorizontalAlignment.Right);
            int y = Offset(region.Height - height, Vertical == VerticalAlignment.Middle, Vertical == VerticalAlignment.Bottom);
            Content.Render(region.Sub(x, y, width, height), context);
        }

        private static int Offset(int free, bool centre, bool end) {
            if (centre) {
                return free / 2;
            }
            return end ? free : 0;
        }
    }
}
