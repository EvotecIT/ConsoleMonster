using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Rendering {
    /// <summary>Renders widgets into cell buffers: full-screen frames for live sessions and sized blocks for print-once output.</summary>
    internal static class FrameComposer {
        /// <summary>Renders a widget as a full-screen frame.</summary>
        /// <param name="root">The root widget.</param>
        /// <param name="width">Screen width.</param>
        /// <param name="height">Screen height.</param>
        /// <param name="context">The render context.</param>
        /// <param name="reuse">A buffer to reuse (resized when needed), or <c>null</c>.</param>
        /// <returns>The frame.</returns>
        public static CellBuffer Compose(IWidget root, int width, int height, RenderContext context, CellBuffer? reuse = null) {
            if (root == null) {
                throw new ArgumentNullException(nameof(root));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            CellBuffer buffer = reuse ?? new CellBuffer(width, height);
            if (buffer.Width != width || buffer.Height != height) {
                buffer.Resize(width, height);
            } else {
                buffer.Clear();
            }
            if (root is ICompactWidget compact && (width < compact.MinimumSize.Width || height < compact.MinimumSize.Height)) {
                // Too small for the full layout: a compact form instead of a squeezed, unreadable one.
                compact.RenderCompact(new Region(buffer), context);
                return buffer;
            }
            root.Render(new Region(buffer), context);
            return buffer;
        }

        /// <summary>Renders a widget as a block exactly as tall as it measures at the given width.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="width">Available width.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The block.</returns>
        public static CellBuffer RenderBlock(IWidget widget, int width, RenderContext context) {
            if (widget == null) {
                throw new ArgumentNullException(nameof(widget));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            Size size = widget.Measure(new Constraints(width), context);
            CellBuffer buffer = new CellBuffer(width, size.Height);
            widget.Render(new Region(buffer), context);
            return buffer;
        }
    }
}
