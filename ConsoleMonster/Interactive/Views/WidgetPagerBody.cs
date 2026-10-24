using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>Copies visible cells rather than reinterpreting styled text, preserving wide glyphs and link targets.</summary>
    internal sealed class WidgetPagerBody : IWidget {
        private readonly CellBuffer _document;
        private readonly int _top;

        internal WidgetPagerBody(CellBuffer document, int top) {
            _document = document;
            _top = top;
        }

        public Size Measure(Constraints constraints, RenderContext context) =>
            new Size(Math.Min(constraints.MaxWidth, _document.Width), Math.Min(constraints.MaxHeight, Math.Max(0, _document.Height - _top)));

        public void Render(Region region, RenderContext context) {
            for (int y = 0; y < region.Height && y + _top < _document.Height; y++) {
                for (int x = 0; x < region.Width && x < _document.Width; x++) {
                    Cell cell = _document[x, y + _top];
                    if (cell.Width > 0) {
                        region.Put(x, y, cell.Glyph, cell.Style, cell.Width);
                    }
                }
            }
        }
    }
}
