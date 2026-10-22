using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Keyboard visibility, narrow reflow and mouse hit testing in short application windows.</summary>
    public sealed class AppViewportTests {
        /// <summary>Tab scrolls to an otherwise clipped control while preserving the footer and click coordinates.</summary>
        [Fact]
        public void FocusTraversal_ScrollsInputsAndMapsVisibleMouseCoordinates() {
            TextInput first = new TextInput("First", "alpha");
            TextInput second = new TextInput("Second", "bravo");
            TextInput last = new TextInput("Last", "charlie");
            using (AppController app = Controller(new Rows(first, second, last))) {
                Assert.Contains("alpha", app.PrepareFrame(40, 7).ToPlainText());
                app.Handle(InputEvent.Press(ConsoleKey.Tab), false);
                app.Handle(InputEvent.Press(ConsoleKey.Tab), false);
                CellBuffer frame = app.PrepareFrame(40, 7);
                Assert.Same(last, app.Focus.Current);
                Assert.Contains("charlie", frame.ToPlainText());
                Assert.Contains("esc quit", frame.GetRowText(6));
                int row = FindRow(frame, "bravo");
                app.Handle(InputEvent.Mouse(3, row), true);
                Assert.Same(second, app.Focus.Current);
                app.Handle(InputEvent.Mouse(3, 6), true);
                Assert.Same(second, app.Focus.Current);
            }
        }

        /// <summary>A tall text area uses its existing editor scroll and keeps its caret within the viewport.</summary>
        [Fact]
        public void TallTextArea_UsesAvailableHeightAndKeepsEditedTextVisible() {
            TextArea notes = new TextArea("Notes") { Height = 20 };
            using (AppController app = Controller(new Rows(notes, new Button("Save")))) {
                app.PrepareFrame(40, 7);
                app.Handle(InputEvent.Paste("one\ntwo\nthree\nfour\nfive\nsix"), false);
                string text = app.PrepareFrame(40, 7).ToPlainText();
                Assert.Contains("six", text);
                Assert.Contains("esc quit", text);
                app.Handle(InputEvent.Press(ConsoleKey.Tab), false);
                Assert.Contains("Save", app.PrepareFrame(40, 7).ToPlainText());
            }
        }

        /// <summary>Columns stack below the app threshold, and a resized narrow frame keeps both controls reachable.</summary>
        [Fact]
        public void Columns_ReflowAndRemainKeyboardAndMouseAccessible() {
            TextInput left = new TextInput("Left", "alpha");
            TextInput right = new TextInput("Right", "bravo");
            using (AppController app = Controller(new Columns(left, right))) {
                CellBuffer wide = app.PrepareFrame(120, 10);
                Assert.Equal(FindRow(wide, "alpha"), FindRow(wide, "bravo"));
                CellBuffer narrow = app.PrepareFrame(59, 10);
                Assert.True(FindRow(narrow, "bravo") > FindRow(narrow, "alpha"));
                app.Handle(InputEvent.Mouse(3, FindRow(narrow, "bravo")), true);
                Assert.Same(right, app.Focus.Current);
            }
        }

        /// <summary>No-colour focus markers preserve placeholders, typed text and wide graphemes.</summary>
        [Theory]
        [InlineData("", "Search hosts", "|Search hosts")]
        [InlineData("server", "", "|server")]
        [InlineData("界a", "", "|界a")]
        public void PlainCaret_InsertsVisibleMarkerWithoutReplacingText(string value, string placeholder, string expected) {
            TextInput query = new TextInput("Query", value) { Placeholder = placeholder };
            using (AppController app = Controller(query)) {
                app.Handle(InputEvent.Press(ConsoleKey.Home), false);
                string text = app.PrepareFrame(40, 7).ToPlainText();
                Assert.Contains(expected, text);
                Assert.Equal(value, query.Value);
            }
        }

        private static AppController Controller(IWidget content) => new AppController(content, new AppState(),
            new RenderContext(TerminalProfile.Fixed(120, 24, ColorDepth.None, GlyphTier.Ascii), Theme.Dark), 60);

        private static int FindRow(CellBuffer frame, string text) {
            for (int row = 0; row < frame.Height; row++) {
                if (frame.GetRowText(row).Contains(text)) { return row; }
            }
            Assert.Fail("Expected visible text: " + text);
            return -1;
        }
    }
}
