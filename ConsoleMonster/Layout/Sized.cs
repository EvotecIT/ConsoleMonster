using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// Carries a <see cref="LayoutSize"/> with a widget, so declarative surfaces (the PowerShell DSL, JSON layouts) can say
    /// "this panel takes two shares" or "this log fills the rest" without calling container methods. <see cref="Rows"/>,
    /// <see cref="Columns"/> use it as the default size when the child is added without an explicit one. Rendering
    /// passes straight through to the inner widget.
    /// </summary>
    public sealed class Sized : IContainer {
        /// <summary>Wraps a widget with a size.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="size">Its size inside a row or column layout.</param>
        public Sized(IWidget widget, LayoutSize size) {
            Widget = widget ?? throw new ArgumentNullException(nameof(widget));
            LayoutSize = size;
        }

        /// <summary>The wrapped widget.</summary>
        public IWidget Widget { get; }

        /// <summary>The size specification.</summary>
        public LayoutSize LayoutSize { get; }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Widget };

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Widget.Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Widget.Render(region, context);
    }
}
