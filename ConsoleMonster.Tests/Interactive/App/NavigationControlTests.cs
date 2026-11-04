using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    // Folding, panes and find, each driven through the app (or pager) that hosts it.
    public sealed class NavigationControlTests {
        private static readonly RenderContext Context = Terminal.CreateContext(TerminalProfile.Fixed(80, 24));

        [Fact]
        public void CollapsedSectionLeavesFocusOrderAndAClickInsideItsContentFocusesTheInput() {
            var name = new TextInput("Name");
            var section = new Collapsible("Details", name) { Summary = "(1 field)" };
            var state = new AppState();
            using var app = new AppController(new Rows(section, new TextInput("After")), state, Context, 60);
            Assert.Contains("Details  (1 field)", app.PrepareFrame(80, 24).ToPlainText());
            Press(app, ConsoleKey.Tab);
            Assert.Equal("After", app.Focus.Current!.Key);

            Press(app, ConsoleKey.Tab, KeyModifiers.Shift);
            Press(app, ConsoleKey.Enter);
            string open = app.PrepareFrame(80, 24).ToPlainText();
            Assert.Equal(true, state["Details"]);
            Assert.DoesNotContain("(1 field)", open);
            int nameRow = Array.FindIndex(open.Split('\n'), row => row.Contains("Name"));
            Click(app, 6, nameRow + 1);
            Assert.Equal("Name", app.Focus.Current!.Key);
            Assert.True(section.Expanded);

            Click(app, 4, 0);
            Assert.Equal(false, state["Details"]);
            Assert.DoesNotContain("Name", app.PrepareFrame(80, 24).ToPlainText());
        }

        [Fact]
        public void AccordionKeepsOneSectionOpenUntilExpandAll() {
            var first = new Collapsible("First", TextBlock.Plain("alpha")) { Expanded = true };
            var second = new Collapsible("Second", TextBlock.Plain("beta"));
            using var app = new AppController(new Accordion(first, second), new AppState(), Context, 60);
            app.PrepareFrame(80, 24);
            Press(app, ConsoleKey.Tab);
            Press(app, ConsoleKey.Enter);
            Assert.False(first.Expanded); Assert.True(second.Expanded);
            Assert.Contains("expand all", app.PrepareFrame(80, 24).ToPlainText());

            Type(app, "+");
            Assert.True(first.Expanded); Assert.True(second.Expanded);
            string all = app.PrepareFrame(80, 24).ToPlainText();
            Assert.Contains("alpha", all); Assert.Contains("beta", all);
            Type(app, "-");
            Assert.False(first.Expanded); Assert.False(second.Expanded);
        }

        [Fact]
        public void ResizeKeysMoveTheDividerOnlyWhileAPaneHasFocus() {
            var splitter = new Splitter(new TextInput("Filter"), TextBlock.Plain("details"));
            using var app = new AppController(new Rows(splitter, new TextInput("Outside")), new AppState(), Context, 60);
            int before = Divider(app.PrepareFrame(80, 24), "details");
            Press(app, ConsoleKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift);
            int moved = Divider(app.PrepareFrame(80, 24), "details");
            Assert.Equal(before + 2, moved);

            Press(app, ConsoleKey.Tab);
            Assert.Equal("Outside", app.Focus.Current!.Key);
            Press(app, ConsoleKey.LeftArrow, KeyModifiers.Control | KeyModifiers.Shift);
            Assert.Equal(moved, Divider(app.PrepareFrame(80, 24), "details"));
        }

        [Fact]
        public void DraggedDividerSurvivesTheScreenRebuildingItsSplitter() {
            var state = new AppState();
            var screens = new AppScreens(new AppScreen("Main", "Main",
                new AppBinding(s => new Splitter(TextBlock.Plain("left " + s.Get("Round", 0)), TextBlock.Plain("right")) { Key = "Panes" })));
            using var app = new AppController(screens, state, Context, 60);
            app.Refresh(null);
            int start = Divider(app.PrepareFrame(80, 24), "right");
            app.Handle(InputEvent.Pointer(start, 3, MouseAction.Down, button: 1), true);
            app.Handle(InputEvent.Pointer(20, 3, MouseAction.Move, heldButtons: 1), true);
            app.Handle(InputEvent.Pointer(20, 3, MouseAction.Up, button: 1), true);
            Assert.Equal(20, Divider(app.PrepareFrame(80, 24), "right"));

            state["Round"] = 1;
            app.Refresh(null);
            CellBuffer rebuilt = app.PrepareFrame(80, 24);
            Assert.Contains("left 1", rebuilt.ToPlainText());
            Assert.Equal(20, Divider(rebuilt, "right"));
        }

        [Fact]
        public void SlashFindsLogLinesHighlightsThemAndStepsThroughMatches() {
            var log = new LogViewport("Output") { ShowElapsed = false };
            log.Write(string.Join("\n", Enumerable.Range(0, 40).Select(i => i == 5 || i == 30 ? "error-" + i : "line-" + i)));
            using var app = new AppController(log, new AppState(), Context, 60);
            Assert.DoesNotContain("error-5", app.PrepareFrame(80, 24).ToPlainText());

            // The first match at or below the top of the view is current; the bar sits on the panel's bottom border.
            Type(app, "/error");
            CellBuffer first = app.PrepareFrame(80, 24);
            string text = first.ToPlainText();
            Assert.Contains("Find: error", text); Assert.Contains("2 of 2", text);
            Assert.True(Styled(first, "error-30", TextDecoration.Reverse));

            Press(app, ConsoleKey.Enter);
            text = app.PrepareFrame(80, 24).ToPlainText();
            Assert.Contains("1 of 2", text); Assert.Contains("error-5", text); Assert.DoesNotContain("error-30", text);

            Press(app, ConsoleKey.Escape);
            Type(app, "n");
            text = app.PrepareFrame(80, 24).ToPlainText();
            Assert.DoesNotContain("Find:", text); Assert.Contains("error-30", text); Assert.DoesNotContain("error-5", text);
        }

        [Fact]
        public void CtrlFInATextAreaMovesTheCaretToTheMatch() {
            var notes = new TextArea("Notes", "alpha\nbeta\ngamma alpha");
            using var app = new AppController(notes, new AppState(), Context, 60);
            app.PrepareFrame(80, 24);
            Press(app, ConsoleKey.F, KeyModifiers.Control);
            Type(app, "alpha");
            Assert.Contains("2 of 2", app.PrepareFrame(80, 24).ToPlainText());
            Press(app, ConsoleKey.Enter);
            Assert.Contains("1 of 2", app.PrepareFrame(80, 24).ToPlainText());

            Press(app, ConsoleKey.Escape);
            Type(app, ">");
            Assert.Equal(">alpha\nbeta\ngamma alpha", notes.Value);
            Assert.DoesNotContain("Find:", app.PrepareFrame(80, 24).ToPlainText());
        }

        [Fact]
        public void PagerFindRevealsMatchesBelowThePage() {
            TextBlock document = TextBlock.Plain(string.Join("\n", Enumerable.Range(0, 60).Select(i => i == 45 ? "the needle" : "row " + i)));
            var view = new WidgetPagerView(document, "Document", new PromptLabels());
            var context = new RenderContext(TerminalProfile.Fixed(40, 10), Theme.Dark);
            var surface = new RecordingSurface(40, 10, false);
            InteractionLoop.Draw(view, surface, context, 0, TimeSpan.Zero);
            foreach (char c in "/needle") { view.Handle(InputEvent.Character(c)); }
            InteractionLoop.Draw(view, surface, context, 1, TimeSpan.Zero);
            string text = surface.Frames.Last().ToPlainText();
            Assert.Contains("the needle", text); Assert.Contains("1 of 1", text);

            view.Handle(InputEvent.Press(ConsoleKey.Escape));
            InteractionLoop.Draw(view, surface, context, 2, TimeSpan.Zero);
            text = surface.Frames.Last().ToPlainText();
            Assert.DoesNotContain("Find:", text); Assert.Contains("the needle", text);
            Assert.False(view.IsDone);
        }

        private static void Press(AppController app, ConsoleKey key, KeyModifiers modifiers = KeyModifiers.None) {
            app.Handle(InputEvent.Press(key, modifiers), true);
            app.Refresh(null);
        }

        private static void Type(AppController app, string text) {
            foreach (char c in text) { app.Handle(InputEvent.Character(c), true); }
            app.Refresh(null);
        }

        private static void Click(AppController app, int column, int row) {
            app.Handle(InputEvent.Pointer(column, row, MouseAction.Down, button: 1), true);
            app.Handle(InputEvent.Pointer(column, row, MouseAction.Up, button: 1), true);
            app.Refresh(null);
        }

        // The divider is the cell before the second pane's text.
        private static int Divider(CellBuffer frame, string second) {
            string row = frame.ToPlainText().Split('\n').First(line => line.Contains(second));
            int at = row.IndexOf(second, StringComparison.Ordinal) - 1;
            Assert.Equal('│', row[at]);
            return at;
        }

        // Whether some cell of the first occurrence of the text carries the decoration.
        private static bool Styled(CellBuffer frame, string text, TextDecoration decoration) {
            string[] rows = frame.ToPlainText().Split('\n');
            int y = Array.FindIndex(rows, row => row.Contains(text));
            if (y < 0) { return false; }
            int x = rows[y].IndexOf(text, StringComparison.Ordinal);
            return Enumerable.Range(x, text.Length).Any(column => (frame[column, y].Style.Decoration & decoration) != 0);
        }
    }
}
