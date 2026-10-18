using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Tests.Live {
    internal sealed class FakeContainer : IContainer {
        internal FakeContainer(params IWidget[] children) { Children = children; }

        public IReadOnlyList<IWidget> Children { get; }

        public Size Measure(Constraints constraints, RenderContext context) => new Size(1, 1);

        public void Render(Region region, RenderContext context) {
            foreach (IWidget child in Children) { child.Render(region, context); }
        }
    }
}
