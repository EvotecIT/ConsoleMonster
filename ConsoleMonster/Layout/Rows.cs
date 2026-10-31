using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// Stacks children vertically. In print-once output every child gets its measured height; in a live frame,
    /// <see cref="LayoutSize.Fraction(double, int)"/> rows share the height left after fixed and auto rows (for example a
    /// log region that fills the bottom of the screen).
    /// </summary>
    public sealed class Rows : IContainer, IContentOwner {
        private readonly List<IWidget> _children = new List<IWidget>();
        private readonly List<LayoutSize> _sizes = new List<LayoutSize>();

        /// <summary>Creates rows from children sized to their content.</summary>
        /// <param name="children">The children.</param>
        public Rows(params IWidget[] children) {
            if (children != null) {
                foreach (IWidget child in children) {
                    Add(child);
                }
            }
        }

        /// <summary>Blank rows between children.</summary>
        public int Gap { get; set; }

        // Rows made from a container's several content children; the container's own Gap governs them.
        internal bool ComposedContent { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => _children;

        /// <summary>Size specifications, parallel to <see cref="Children"/>.</summary>
        public IReadOnlyList<LayoutSize> Sizes => _sizes;

        /// <summary>Adds a child.</summary>
        /// <param name="child">The child.</param>
        /// <param name="size">Its height specification (default auto).</param>
        /// <returns>This instance.</returns>
        public Rows Add(IWidget child, LayoutSize? size = null) {
            _children.Add(child ?? throw new ArgumentNullException(nameof(child)));
            _sizes.Add(size ?? (child is Sized sized ? sized.LayoutSize : LayoutSize.Auto));
            return this;
        }

        void IContentOwner.ReplaceContent(IWidget[] content) { _children.Clear(); _sizes.Clear(); foreach (IWidget widget in content) { Add(widget); } }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int width = 0;
            int height = 0;
            for (int i = 0; i < _children.Count; i++) {
                LayoutSize spec = _sizes[i];
                int available = constraints.IsHeightUnbounded ? Constraints.Unbounded : Math.Max(0, constraints.MaxHeight - height);
                Size size = _children[i].Measure(new Constraints(constraints.MaxWidth, spec.Kind == LayoutSizeKind.Fixed ? (int)spec.Value : available), context);
                width = Math.Max(width, size.Width);
                height += spec.Kind == LayoutSizeKind.Fixed ? (int)spec.Value : Math.Max(spec.Min, size.Height);
                if (i > 0) {
                    height += Gap;
                }
            }
            if (!constraints.IsHeightUnbounded) {
                height = Math.Min(height, constraints.MaxHeight);
            }
            return new Size(Math.Min(width, constraints.MaxWidth), height);
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            int[] heights = ComputeHeights(region.Width, region.Height, context);
            if (LayoutNotes.IsRecording) {
                for (int i = 0; i < _children.Count; i++) {
                    LayoutNotes.Record(_children[i], _sizes[i], _children[i].Measure(new Constraints(region.Width), context), new Size(region.Width, heights[i]), true);
                }
            }
            int y = 0;
            for (int i = 0; i < _children.Count; i++) {
                if (heights[i] > 0) {
                    _children[i].Render(region.Sub(0, y, region.Width, heights[i]), context);
                }
                y += heights[i] + Gap;
            }
        }

        private int[] ComputeHeights(int width, int height, RenderContext context) {
            Func<int, int, int> measure = (index, remaining) => _children[index].Measure(new Constraints(width, remaining), context).Height;
            int[] natural = TrackAllocator.Allocate(height, _sizes, Gap, measure, true);
            int used = TrackAllocator.Sum(natural) + (Gap * Math.Max(0, _children.Count - 1));
            int extra = height - used;
            if (extra <= 0) {
                return natural;
            }
            double weights = 0;
            for (int i = 0; i < _sizes.Count; i++) {
                if (_sizes[i].Kind == LayoutSizeKind.Fraction) {
                    weights += _sizes[i].Value;
                }
            }
            if (weights <= 0) {
                return natural;
            }
            int given = 0;
            int last = -1;
            for (int i = 0; i < _sizes.Count; i++) {
                if (_sizes[i].Kind != LayoutSizeKind.Fraction) {
                    continue;
                }
                int share = (int)Math.Floor(extra * (_sizes[i].Value / weights));
                natural[i] += share;
                given += share;
                last = i;
            }
            if (last >= 0) {
                natural[last] += extra - given;
            }
            return natural;
        }
    }
}
