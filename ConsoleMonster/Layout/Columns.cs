using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// Places children side by side. Children in a row share their height, so panels line up instead of forming a
    /// staircase. When the width drops below the children's minimum widths the columns stack vertically instead of
    /// squeezing content unreadably.
    /// </summary>
    public sealed class Columns : IContainer, IContentOwner {
        private const int DefaultFractionMin = 16;
        private readonly List<IWidget> _children = new List<IWidget>();
        private readonly List<LayoutSize> _sizes = new List<LayoutSize>();

        /// <summary>Creates columns that share the width equally.</summary>
        /// <param name="children">The children.</param>
        public Columns(params IWidget[] children) {
            if (children != null) {
                foreach (IWidget child in children) {
                    Add(child);
                }
            }
        }

        /// <summary>Blank columns between children (default 1).</summary>
        public int Gap { get; set; } = 1;

        /// <summary>
        /// Width below which the columns stack vertically. -1 (default) uses the sum of the children's minimum widths
        /// (fractions without a minimum count as 16 cells).
        /// </summary>
        public int CollapseBelow { get; set; } = -1;

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => _children;

        /// <summary>Size specifications, parallel to <see cref="Children"/>.</summary>
        public IReadOnlyList<LayoutSize> Sizes => _sizes;

        /// <summary>Adds a child.</summary>
        /// <param name="child">The child.</param>
        /// <param name="size">Its width specification (default one equal fraction).</param>
        /// <returns>This instance.</returns>
        public Columns Add(IWidget child, LayoutSize? size = null) {
            _children.Add(child ?? throw new ArgumentNullException(nameof(child)));
            _sizes.Add(size ?? (child is Sized sized ? sized.LayoutSize : LayoutSize.Fraction()));
            return this;
        }

        void IContentOwner.ReplaceContent(IWidget[] content) { _children.Clear(); _sizes.Clear(); foreach (IWidget widget in content) { Add(widget); } }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            if (IsCollapsed(constraints.MaxWidth)) {
                int stackedHeight = 0;
                foreach (IWidget child in _children) {
                    stackedHeight += child.Measure(new Constraints(constraints.MaxWidth, constraints.MaxHeight), context).Height;
                }
                return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? stackedHeight : Math.Min(stackedHeight, constraints.MaxHeight));
            }
            int[] widths = ComputeWidths(constraints.MaxWidth, constraints.MaxHeight, context);
            int height = 0;
            bool hasFraction = false;
            for (int i = 0; i < _children.Count; i++) {
                hasFraction |= _sizes[i].Kind == LayoutSizeKind.Fraction;
                if (widths[i] <= 0) {
                    continue;
                }
                height = Math.Max(height, _children[i].Measure(new Constraints(widths[i], constraints.MaxHeight), context).Height);
            }
            int width = hasFraction ? constraints.MaxWidth : Math.Min(constraints.MaxWidth, TrackAllocator.Sum(widths) + (Gap * Math.Max(0, _children.Count - 1)));
            return new Size(width, height);
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (IsCollapsed(region.Width)) {
                int y = 0;
                foreach (IWidget child in _children) {
                    int h = child.Measure(new Constraints(region.Width, Math.Max(0, region.Height - y)), context).Height;
                    child.Render(region.Sub(0, y, region.Width, h), context);
                    y += h;
                }
                return;
            }
            int[] widths = ComputeWidths(region.Width, region.Height, context);
            if (LayoutNotes.IsRecording) {
                for (int i = 0; i < _children.Count; i++) {
                    LayoutNotes.Record(_children[i], _sizes[i], _children[i].Measure(new Constraints(region.Width, region.Height), context), new Size(widths[i], region.Height), false);
                }
            }
            int x = 0;
            for (int i = 0; i < _children.Count; i++) {
                if (widths[i] > 0) {
                    _children[i].Render(region.Sub(x, 0, widths[i], region.Height), context);
                }
                x += widths[i] + Gap;
            }
        }

        private bool IsCollapsed(int width) {
            if (_children.Count < 2) {
                return false;
            }
            int threshold = CollapseBelow;
            if (threshold < 0) {
                threshold = Gap * (_children.Count - 1);
                foreach (LayoutSize spec in _sizes) {
                    threshold += spec.Kind == LayoutSizeKind.Fixed ? (int)spec.Value : (spec.Min > 0 ? spec.Min : (spec.Kind == LayoutSizeKind.Fraction ? DefaultFractionMin : 0));
                }
            }
            return width < threshold;
        }

        private int[] ComputeWidths(int width, int maxHeight, RenderContext context) {
            Func<int, int, int> measure = (index, remaining) => _children[index].Measure(new Constraints(remaining, maxHeight), context).Width;
            return TrackAllocator.Allocate(width, _sizes, Gap, measure, false);
        }
    }
}
