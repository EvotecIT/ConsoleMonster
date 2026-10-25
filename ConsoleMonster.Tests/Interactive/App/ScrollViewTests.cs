using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Viewport composition, clipping, focus reveal and pointer translation use the same arranged rectangles.</summary>
    public sealed class ScrollViewTests {
        private static readonly RenderContext Context = Terminal.CreateContext(TerminalProfile.Fixed(40, 10));

        [Fact]
        public void ScrollOffset_ClipsContentAndPreservesNeighbours() {
            ScrollView view = new ScrollView(new TextBlock("zero\none\ntwo\nthree\nfour")) { ScrollOffset = 2 };
            IWidget root = new Rows().Add(new TextBlock("header"), LayoutSize.Fixed(1)).Add(view, LayoutSize.Fixed(2)).Add(new TextBlock("footer"), LayoutSize.Fixed(1));
            string frame = FrameComposer.Compose(root, 20, 4, Context).ToPlainText();
            Assert.Contains("header", frame); Assert.Contains("two", frame); Assert.Contains("three", frame); Assert.Contains("footer", frame);
            Assert.DoesNotContain("zero", frame); Assert.DoesNotContain("four", frame);
        }

        [Fact]
        public void NestedViewport_RevealsFocusedControlAndMapsPointerToItsBuffer() {
            TextInput first = new TextInput("first");
            TextInput last = new TextInput("last");
            ScrollView view = new ScrollView(new Rows().Add(first, LayoutSize.Fixed(3)).Add(new TextBlock("spacer"), LayoutSize.Fixed(8)).Add(last, LayoutSize.Fixed(3)));
            using AppController app = new AppController(view, new AppState(), Context, 60);
            app.PrepareFrame(40, 7);
            app.Handle(InputEvent.Press(ConsoleKey.Tab), true);
            CellBuffer frame = app.PrepareFrame(40, 7);
            Assert.True(last.Focused); Assert.Contains("last", frame.ToPlainText()); Assert.True(view.ScrollOffset > 0);
            app.Handle(InputEvent.Mouse(2, 1, 1, true), true);
            app.Handle(InputEvent.Character('x'), true);
            Assert.Equal("x", last.Value); Assert.Equal("", first.Value);
        }
    }
}
