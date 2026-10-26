using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Pointer edits use the arranged rows and avoid rebuilding unrelated displays.</summary>
    public sealed class PointerInteractionTests {
        private static readonly Canvas.RenderContext Context = Terminal.CreateContext(TerminalProfile.Fixed(100, 30));

        [Fact]
        public void UnhandledHover_DoesNotRebuildBindings_ButKeyboardEditsDo() {
            int builds = 0;
            AppBinding detail = new AppBinding(_ => { builds++; return new TextBlock("Details"); });
            using AppController app = new AppController(new Rows(new TextInput("query"), detail), new AppState(), Context, 60);
            app.PrepareFrame(100, 20);
            app.Refresh(null);
            int initial = builds;
            app.Handle(InputEvent.Pointer(90, 18, MouseAction.Move), true);
            app.Refresh(null);
            Assert.Equal(initial, builds);
            app.Handle(InputEvent.Character('x'), true);
            app.Refresh(null);
            Assert.Equal(initial + 1, builds);
        }

        [Fact]
        public void MultiSelectDrag_IncludesThePressedAndReachedChoices() {
            MultiSelect select = new MultiSelect("targets", new[] { new Choice("alpha", "Alpha"), new Choice("beta", "Beta") });
            using AppController app = new AppController(select, new AppState(), Context, 60);
            string[] lines = app.PrepareFrame(100, 15).ToPlainText().Split('\n');
            int alpha = Array.FindIndex(lines, line => line.Contains("Alpha"));
            int beta = Array.FindIndex(lines, line => line.Contains("Beta"));
            Assert.True(alpha >= 0 && beta > alpha);
            app.Handle(InputEvent.Mouse(5, alpha), true);
            app.Handle(InputEvent.Pointer(5, beta, MouseAction.Move, heldButtons: 1), true);
            app.Handle(InputEvent.Mouse(5, beta, pressed: false), true);
            Assert.Equal(new object?[] { "alpha", "beta" }, select.Value);
        }

        [Fact]
        public void CompactChoiceScrollbar_UsesItsDrawnColumnInsideAWideRegion() {
            ChoiceList choices = new ChoiceList(Enumerable.Range(0, 20).Select(index => new Choice(index, "Item " + index)), false) {
                MinRowWidth = 40, MaxRowWidth = 40
            };
            FrameComposer.Compose(choices, 100, 3, Context);
            Assert.True(choices.HandlePointer(InputEvent.Mouse(39, 2)));
            Assert.Equal(13, choices.FocusedChoice!.Value);
        }
    }
}
