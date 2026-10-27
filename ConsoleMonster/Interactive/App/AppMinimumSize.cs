using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>Keeps a too-small app readable without unmounting or discarding its focused control.</summary>
    internal sealed class AppMinimumSize : IContainer {
        private readonly IWidget _content;
        private readonly int _width, _height;
        private readonly Action<bool> _small;
        internal AppMinimumSize(IWidget content, int width, int height, Action<bool> small) { _content = content; _width = width; _height = height; _small = small; }
        public IReadOnlyList<IWidget> Children => new[] { _content };
        public Size Measure(Constraints constraints, RenderContext context) => _content.Measure(constraints, context);
        public void Render(Region region, RenderContext context) {
            bool small = region.Width < _width || region.Height < _height;
            _small(small);
            if (small) { TextBlock.Plain("Make the window larger (" + _width + " columns, " + _height + " rows). Esc closes.", ThemeToken.Muted).Render(region, context); }
            else { _content.Render(region, context); }
        }
    }
}
