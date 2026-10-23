using System;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Testing;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Edits and navigation must use current text even when no renderer frame intervenes.</summary>
    public sealed class TextAreaBetweenFramesTests {
        [Theory]
        [InlineData(ConsoleKey.DownArrow)]
        [InlineData(ConsoleKey.UpArrow)]
        [InlineData(ConsoleKey.End)]
        [InlineData(ConsoleKey.PageDown)]
        public void ClearingRenderedText_AllowsNavigationAndInsertion(ConsoleKey navigation) {
            TextArea area = new TextArea("notes", "one\ntwo\nthree");
            TestTerminal.Render(area, 20);
            area.Value = string.Empty;
            Apply(area, InputEvent.Press(navigation), InputEvent.Character('x'));
            Assert.Equal("x", area.Value);
            Assert.Contains("x", TestTerminal.Render(area, 20).ToPlainText());
        }

        [Fact]
        public void KeyboardOnlyEdits_RebuildRowsBetweenFrames() {
            TextArea area = new TextArea("notes", "one\ntwo\nthree");
            TestTerminal.Render(area, 20);
            Apply(area, InputEvent.Press(ConsoleKey.Home, KeyModifiers.Control),
                InputEvent.Press(ConsoleKey.Delete, KeyModifiers.Control),
                InputEvent.Press(ConsoleKey.Delete, KeyModifiers.Control),
                InputEvent.Press(ConsoleKey.Delete, KeyModifiers.Control),
                InputEvent.Press(ConsoleKey.DownArrow), InputEvent.Character('x'));
            Assert.Equal("x", area.Value);
            Assert.Contains("x", TestTerminal.Render(area, 20).ToPlainText());
        }

        [Fact]
        public void CallbackClearingTextArea_UpdatesNavigationBeforeAnotherFrame() {
            TextArea area = new TextArea("notes", "one\ntwo\nthree");
            Button clear = new Button("clear", "Clear");
            clear.Clicked += (_, __) => { area.Value = string.Empty; area.Focus(); };
            AppState state = new AppState();
            using (AppController app = new AppController(new Rows(area, clear), state, Terminal.CreateContext(), 60)) {
                app.PrepareFrame(80, 24);
                app.Handle(InputEvent.Press(ConsoleKey.Tab), false);
                app.Handle(InputEvent.Press(ConsoleKey.Enter), false);
                app.Handle(InputEvent.Press(ConsoleKey.DownArrow), false);
                app.Handle(InputEvent.Character('x'), false);
                Assert.Equal("x", state["notes"]);
                Assert.Contains("x", app.PrepareFrame(80, 24).ToPlainText());
            }
        }

        [Fact]
        public void ReplacementText_NavigatesAtTheCurrentWrapWidth() {
            TextArea area = new TextArea("notes", "old\nrows");
            TestTerminal.Render(area, 14);
            area.Value = "alpha beta gamma delta";
            Apply(area, InputEvent.Press(ConsoleKey.Home, KeyModifiers.Control),
                InputEvent.Press(ConsoleKey.DownArrow), InputEvent.Character('x'));
            // Public chrome leaves eight content cells: the next row begins after "alpha ".
            Assert.Equal("alpha xbeta gamma delta", area.Value);
        }

        [Fact]
        public void ReplacementText_ResetsThePreferredVerticalColumn() {
            TextArea area = new TextArea("notes", "abcdef\nghijkl");
            TestTerminal.Render(area, 40);
            Apply(area, InputEvent.Press(ConsoleKey.UpArrow));
            area.Value = "123456\nx";
            Apply(area, InputEvent.Press(ConsoleKey.UpArrow), InputEvent.Character('!'));
            Assert.Equal("1!23456\nx", area.Value);
        }

        private static void Apply(TextArea area, params InputEvent[] inputs) {
            foreach (InputEvent input in inputs) { Assert.True(area.HandleInput(input, Terminal.CreateContext())); }
        }
    }
}
