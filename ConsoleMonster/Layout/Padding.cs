using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>Adds blank space around a child.</summary>
    public sealed class Padding : IContainer {
        /// <summary>Creates padding.</summary>
        /// <param name="child">The child.</param>
        /// <param name="left">Left cells.</param>
        /// <param name="top">Top rows.</param>
        /// <param name="right">Right cells.</param>
        /// <param name="bottom">Bottom rows.</param>
        public Padding(IWidget child, int left = 1, int top = 0, int right = 1, int bottom = 0) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Left = Math.Max(0, left);
            Top = Math.Max(0, top);
            Right = Math.Max(0, right);
            Bottom = Math.Max(0, bottom);
        }

        /// <summary>The child.</summary>
        public IWidget Child { get; }

        /// <summary>Left cells.</summary>
        public int Left { get; }

        /// <summary>Top rows.</summary>
        public int Top { get; }

        /// <summary>Right cells.</summary>
        public int Right { get; }

        /// <summary>Bottom rows.</summary>
        public int Bottom { get; }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Child };

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            Size inner = Child.Measure(constraints.Shrink(Left + Right, Top + Bottom), context);
            int width = Math.Min(constraints.MaxWidth, inner.Width + Left + Right);
            int height = inner.Height + Top + Bottom;
            return new Size(width, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            Child.Render(region.Sub(Left, Top, region.Width - Left - Right, region.Height - Top - Bottom), context);
        }
    }
}
