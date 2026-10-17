using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// Declares a widget's priority and minimum size for reduced layouts. It renders its child unchanged; when a dashboard
    /// is too small for its full layout it shows the highest-priority <see cref="Prioritized"/> widgets that fit, in order.
    /// </summary>
    public sealed class Prioritized : IContainer {
        /// <summary>Wraps a widget.</summary>
        /// <param name="child">The widget.</param>
        /// <param name="priority">Higher shows first in reduced layouts.</param>
        /// <param name="minimumHeight">The rows it needs to be useful (default 1).</param>
        /// <param name="minimumWidth">The cells it needs to be useful (default 20).</param>
        public Prioritized(IWidget child, int priority, int minimumHeight = 1, int minimumWidth = 20) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Priority = priority;
            MinimumSize = new Size(Math.Max(1, minimumWidth), Math.Max(1, minimumHeight));
        }

        /// <summary>The widget.</summary>
        public IWidget Child { get; }

        /// <summary>Higher shows first in reduced layouts.</summary>
        public int Priority { get; }

        /// <summary>The size it needs to be useful.</summary>
        public Size MinimumSize { get; }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Child };

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Child.Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Child.Render(region, context);
    }
}
