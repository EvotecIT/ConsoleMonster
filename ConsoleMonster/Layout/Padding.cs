using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>Adds blank space around a content.</summary>
    public sealed class Padding : IContainer, IContentOwner {
        /// <summary>Creates padding.</summary>
        /// <param name="content">The content.</param>
        /// <param name="left">Left cells.</param>
        /// <param name="top">Top rows.</param>
        /// <param name="right">Right cells.</param>
        /// <param name="bottom">Bottom rows.</param>
        public Padding(IWidget? content = null, int left = 1, int top = 0, int right = 1, int bottom = 0) {
            Content = content ?? new Rows();
            Left = Math.Max(0, left);
            Top = Math.Max(0, top);
            Right = Math.Max(0, right);
            Bottom = Math.Max(0, bottom);
        }

        /// <summary>The content.</summary>
        public IWidget Content { get; set; }

        void IContentOwner.ReplaceContent(IWidget[] content) => Content = ContentBuilder.Compose(content);

        /// <summary>Left cells.</summary>
        public int Left { get => _left; set => _left = Math.Max(0, value); }
        private int _left = 1;

        /// <summary>Top rows.</summary>
        public int Top { get => _top; set => _top = Math.Max(0, value); }
        private int _top;

        /// <summary>Right cells.</summary>
        public int Right { get => _right; set => _right = Math.Max(0, value); }
        private int _right = 1;

        /// <summary>Bottom rows.</summary>
        public int Bottom { get => _bottom; set => _bottom = Math.Max(0, value); }
        private int _bottom;

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Content };

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            Size inner = Content.Measure(constraints.Shrink(Left + Right, Top + Bottom), context);
            int width = Math.Min(constraints.MaxWidth, inner.Width + Left + Right);
            int height = inner.Height + Top + Bottom;
            return new Size(width, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            Content.Render(region.Sub(Left, Top, region.Width - Left - Right, region.Height - Top - Bottom), context);
        }
    }
}
