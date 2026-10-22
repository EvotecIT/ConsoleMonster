using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>Keeps focused inputs visible when a stacked application exceeds the terminal height.</summary>
    internal sealed class AppViewport : IContainer {
        private readonly object _gate = new object();
        private readonly IWidget _content;
        private readonly FocusManager _focus;
        private int _scroll;
        private Rect _bounds;

        internal AppViewport(IWidget content, FocusManager focus) { _content = content; _focus = focus; }
        public IReadOnlyList<IWidget> Children => new[] { _content };

        public Size Measure(Constraints constraints, RenderContext context) {
            lock (_gate) {
                ConstrainInputs(constraints.MaxHeight);
                Size natural = _content.Measure(new Constraints(constraints.MaxWidth), context);
                return new Size(natural.Width, Math.Min(natural.Height, constraints.MaxHeight));
            }
        }

        public void Render(Region region, RenderContext context) {
            lock (_gate) {
                _bounds = region.Bounds;
                if (region.Width == 0 || region.Height == 0) { return; }
                ConstrainInputs(region.Height);
                int height = Math.Max(region.Height, _content.Measure(new Constraints(region.Width), context).Height);
                CellBuffer canvas = new CellBuffer(region.Width, height);
                _content.Render(new Region(canvas), context);
                InputControl? current = _focus.Current;
                if (current != null) {
                    Rect focused = current.Bounds;
                    if (focused.Y < _scroll) { _scroll = focused.Y; }
                    if (focused.Bottom > _scroll + region.Height) { _scroll = focused.Bottom - region.Height; }
                }
                _scroll = Math.Max(0, Math.Min(_scroll, height - region.Height));
                region.Buffer.Blit(canvas, 0, _scroll, region.Width, region.Height, region.Bounds.X, region.Bounds.Y);
            }
        }

        internal bool FocusAt(int column, int row) {
            lock (_gate) {
                if (column < _bounds.X || column >= _bounds.Right || row < _bounds.Y || row >= _bounds.Bottom) { return false; }
                return _focus.FocusAt(column - _bounds.X, row - _bounds.Y + _scroll);
            }
        }

        private void ConstrainInputs(int height) {
            foreach (InputControl control in _focus.Controls) { control.ViewportHeight = height; }
        }
    }
}
