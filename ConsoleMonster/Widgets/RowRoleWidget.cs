using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Widgets {
    internal sealed class RowRoleWidget : IWidget {
        private readonly IWidget _widget;
        private readonly TableRowRole _role;

        internal RowRoleWidget(IWidget widget, TableRowRole role) { _widget = widget; _role = role; }

        public Size Measure(Constraints constraints, RenderContext context) => _widget.Measure(constraints, context);

        public void Render(Region region, RenderContext context) {
            _widget.Render(region, context);
            for (int y = 0; y < region.Height; y++) for (int x = 0; x < region.Width; x++) region.SetStyle(x, y, region.Get(x, y).Style.Combine(_role.Style(context)));
        }
    }
}
