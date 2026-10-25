using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// Renders a widget with a fixed culture for numbers, dates and durations, whatever the terminal's culture is (used by
    /// <c>Show-ConsoleTable -AsWidget -Culture</c>, so the culture travels with the widget into exports and dashboards).
    /// </summary>
    public sealed class CultureScope : IContainer {
        /// <summary>Creates the scope.</summary>
        /// <param name="child">The widget.</param>
        /// <param name="culture">The culture.</param>
        public CultureScope(IWidget child, CultureInfo culture) {
            Child = child ?? throw new ArgumentNullException(nameof(child));
            Culture = culture ?? throw new ArgumentNullException(nameof(culture));
        }

        /// <summary>The widget.</summary>
        public IWidget Child { get; }

        /// <summary>The culture.</summary>
        public CultureInfo Culture { get; }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => new[] { Child };

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Child.Measure(constraints, Scoped(context));

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Child.Render(region, Scoped(context));

        private RenderContext Scoped(RenderContext context) {
            if (Equals(context.Culture, Culture)) {
                return context;
            }
            return new RenderContext(context.Profile.WithCulture(Culture), context.Theme, context.Glyphs, context.Tick, context.Elapsed);
        }
    }
}
