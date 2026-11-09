using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// Shows one of several layouts by the width it is given: the widest breakpoint that fits, otherwise the fallback.
    /// Use it when a view needs a different shape, not just a narrower one, such as one cell per server on a wide terminal
    /// and one cell per site on a narrow one. Every layout stays a child, so live updates reach the hidden ones too and
    /// a resize shows current values.
    /// </summary>
    public sealed class Responsive : IContainer {
        private readonly List<(int MinWidth, IWidget Content)> _breakpoints = new List<(int, IWidget)>();

        /// <summary>Creates a responsive container.</summary>
        /// <param name="fallback">The layout shown below every breakpoint.</param>
        public Responsive(IWidget fallback) {
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        private IWidget _fallback;

        /// <summary>The layout shown below every breakpoint.</summary>
        public IWidget Fallback { get => _fallback; set => _fallback = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>Adds a layout shown when at least <paramref name="minWidth"/> cells are available; a second layout for the same width replaces the first.</summary>
        /// <param name="minWidth">The narrowest width, in cells, for this layout.</param>
        /// <param name="content">The layout.</param>
        /// <returns>This container.</returns>
        public Responsive When(int minWidth, IWidget content) {
            if (minWidth < 1) throw new ArgumentOutOfRangeException(nameof(minWidth));
            if (content == null) throw new ArgumentNullException(nameof(content));
            _breakpoints.RemoveAll(breakpoint => breakpoint.MinWidth == minWidth);
            int index = _breakpoints.FindIndex(breakpoint => breakpoint.MinWidth < minWidth);
            _breakpoints.Insert(index < 0 ? _breakpoints.Count : index, (minWidth, content));
            return this;
        }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children {
            get {
                List<IWidget> children = new List<IWidget>(_breakpoints.Count + 1);
                foreach (var breakpoint in _breakpoints) children.Add(breakpoint.Content);
                children.Add(Fallback);
                return children;
            }
        }

        /// <inheritdoc />
        /// <remarks>Reports at least the chosen breakpoint's width, so a parent that allocates the measured width keeps the same layout.</remarks>
        public Size Measure(Constraints constraints, RenderContext context) {
            (int minWidth, IWidget content) = Pick(constraints.MaxWidth);
            Size inner = content.Measure(constraints, context);
            return new Size(Math.Min(constraints.MaxWidth, Math.Max(inner.Width, minWidth)), inner.Height);
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) return;
            Pick(region.Width).Content.Render(region, context);
        }

        private (int MinWidth, IWidget Content) Pick(int width) {
            foreach (var breakpoint in _breakpoints) {
                if (width >= breakpoint.MinWidth) return breakpoint;
            }
            return (0, Fallback);
        }
    }
}
