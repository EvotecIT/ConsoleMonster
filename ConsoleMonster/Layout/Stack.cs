using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// Layers children on top of each other in the same region, in order: later layers paint over earlier ones. A layer
    /// replaces only the cells it writes, so the layers below stay visible around it. Combine with <see cref="Align"/>
    /// to place an overlay, for example a status badge in the top-right corner of a chart. For children placed one
    /// below another, use <see cref="Rows"/>.
    /// </summary>
    public sealed class Stack : IContainer {
        private readonly IWidget[] _layers;

        /// <summary>Creates a stack.</summary>
        /// <param name="layers">The layers, bottom first.</param>
        public Stack(params IWidget[] layers) {
            if (layers == null) {
                throw new ArgumentNullException(nameof(layers));
            }
            foreach (IWidget layer in layers) {
                if (layer == null) {
                    throw new ArgumentException("A stack layer cannot be null.", nameof(layers));
                }
            }
            _layers = (IWidget[])layers.Clone();
        }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children => _layers;

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int width = 0;
            int height = 0;
            foreach (IWidget layer in _layers) {
                Size size = layer.Measure(constraints, context);
                width = Math.Max(width, size.Width);
                height = Math.Max(height, size.Height);
            }
            return new Size(width, height);
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            foreach (IWidget layer in _layers) {
                layer.Render(region, context);
            }
        }
    }
}
