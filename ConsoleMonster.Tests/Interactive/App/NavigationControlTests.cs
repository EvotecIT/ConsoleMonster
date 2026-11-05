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
        public void AccordionHintsFollowTheAppLanguage() {
            var context = Terminal.CreateContext(TerminalProfile.Fixed(120, 24).WithLanguage(new System.Globalization.CultureInfo("pl-PL")));
            using var app = new AppController(new Accordion(new Collapsible("Details")), new AppState(), context, 60);
            string frame = app.PrepareFrame(120, 24).ToPlainText();
            Assert.Contains("rozwiń wszystko", frame);
            Assert.Contains("zwiń wszystko", frame);
        }

        [Fact]
        public void ResizeKeysMoveTheDividerOnlyWhileAPaneHasFocus() {
            var splitter = new Splitter(new Button("Apply"), TextBlock.Plain("details"));
            bool invoked = false;
            var outside = new TextInput("Outside");
            outside.Bindings.Add(new KeyBinding("outside action", () => invoked = true, new KeyChord(ConsoleKey.LeftArrow, KeyModifiers.Control | KeyModifiers.Shift)));
            using var app = new AppController(new Rows(splitter, outside), new AppState(), Context, 60);
            int before = Divider(app.PrepareFrame(80, 24), "details");
            Press(app, ConsoleKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift);
            int moved = Divider(app.PrepareFrame(80, 24), "details");
            Assert.Equal(before + 2, moved);

            Press(app, ConsoleKey.Tab);
            Assert.Equal("Outside", app.Focus.Current!.Key);
            Press(app, ConsoleKey.LeftArrow, KeyModifiers.Control | KeyModifiers.Shift);
            Assert.Equal(moved, Divider(app.PrepareFrame(80, 24), "details"));
            Assert.True(invoked);
            Assert.Contains("outside action", app.PrepareFrame(120, 24).ToPlainText());
        }

        [Fact]
        public void ResizeKeysLeaveTextEditingToTheFocusedInput() {
            var filter = new TextInput("Filter", "alpha beta");
            var apply = new Button("Apply");
            var splitter = new Splitter(new Rows(filter, apply), TextBlock.Plain("details"));
            using var app = new AppController(splitter, new AppState(), Context, 60);
            int before = Divider(app.PrepareFrame(120, 24), "details");
            Press(app, ConsoleKey.Home);
            app.PrepareFrame(120, 24);
            int home = filter.CurrentCaret!.Value.X;
            Press(app, ConsoleKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift);
            CellBuffer edited = app.PrepareFrame(120, 24);
            Assert.Equal(before, Divider(edited, "details"));
            Assert.DoesNotContain("resize", edited.ToPlainText());
            Assert.True(filter.CurrentCaret!.Value.X > home);

            Press(app, ConsoleKey.Tab);
            Press(app, ConsoleKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift);
            CellBuffer resized = app.PrepareFrame(120, 24);
            Assert.Equal(before + 2, Divider(resized, "details"));
            Assert.Contains("resize", resized.ToPlainText());

            bool invoked = false;
            var custom = new Button("Custom");
            custom.Bindings.Add(new KeyBinding("custom action", () => invoked = true, new KeyChord(ConsoleKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift)));
            using var boundApp = new AppController(new Splitter(custom, TextBlock.Plain("details")), new AppState(), Context, 60);
            CellBuffer bound = boundApp.PrepareFrame(120, 24);
            int boundDivider = Divider(bound, "details");
            Assert.DoesNotContain("resize", bound.ToPlainText());
            Press(boundApp, ConsoleKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift);
            Assert.True(invoked);
            Assert.Equal(boundDivider, Divider(boundApp.PrepareFrame(120, 24), "details"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResizeKeysLeaveScreenAndAppBindingsAvailable(bool screenBinding) {
            var splitter = new Splitter(new Button("Apply"), TextBlock.Plain("details"));
            var screen = new AppScreen("main", "Main", splitter);
            var options = new ConsoleAppOptions();
            bool invoked = false;
            var binding = new KeyBinding("scoped action", () => invoked = true,
                new KeyChord(ConsoleKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift));
            if (screenBinding) { screen.Bindings.Add(binding); }
            else { options.KeyBindings.Add(binding); }
            using var app = new AppController(new AppScreens(screen), new AppState(), Context, 60, options);
            CellBuffer before = app.PrepareFrame(120, 24);
            int divider = Divider(before, "details");
            Assert.Contains("scoped action", before.ToPlainText());
            Press(app, ConsoleKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift);
            Assert.True(invoked);
            CellBuffer after = app.PrepareFrame(120, 24);
            Assert.Equal(divider, Divider(after, "details"));
            Assert.DoesNotContain("resize", after.ToPlainText());
            Press(app, ConsoleKey.LeftArrow, KeyModifiers.Control | KeyModifiers.Shift);
            Assert.Equal(divider - 2, Divider(app.PrepareFrame(120, 24), "details"));
        }

        [Theory]
        [InlineData("next", false)]
        [InlineData("previous", false)]
        [InlineData("quit", false)]
        [InlineData("next", true)]
        public void ResizeKeysLeaveConfiguredHostGesturesAvailable(string gesture, bool editor) {
            InputControl first = editor ? new TextInput("First", "words to select") : new Button("First");
            var splitter = new Splitter(new Rows(first, new Button("Middle"), new Button("Last")), TextBlock.Plain("details"));
            var options = new ConsoleAppOptions();
            var chord = new KeyChord(ConsoleKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift);
            if (gesture == "next") { options.Keys.NextFocus = chord; }
            else if (gesture == "previous") { options.Keys.PreviousFocus = chord; }
            else { options.Keys.Exit = chord; }
            var state = new AppState();
            using var app = new AppController(splitter, state, Context, 60, options);
            CellBuffer before = app.PrepareFrame(120, 24);
            int divider = Divider(before, "details");
            Press(app, ConsoleKey.RightArrow, KeyModifiers.Control | KeyModifiers.Shift);
            Assert.Equal(gesture == "next" ? "Middle" : gesture == "previous" ? "Last" : "First", app.Focus.Current!.Key);
            Assert.Equal(gesture == "quit", state.ExitRequested);
            CellBuffer after = app.PrepareFrame(120, 24);
            Assert.Equal(divider, Divider(after, "details"));
            Assert.DoesNotContain("resize", after.ToPlainText());
            Assert.Contains(gesture == "previous" ? "back" : gesture, before.ToPlainText());
        }

        [Theory]
        [InlineData(ColorDepth.TrueColor)]
        [InlineData(ColorDepth.None)]
        public void FindBarDrawsTheEditorCaretAndScrollsTheQuery(ColorDepth color) {
            var find = new FindBar();
            find.Start();
            foreach (char c in "abc") { find.Handle(InputEvent.Character(c)); }
            find.Handle(InputEvent.Press(ConsoleKey.LeftArrow));
            find.Handle(InputEvent.Press(ConsoleKey.LeftArrow));
            var context = Terminal.CreateContext(TerminalProfile.Fixed(40, 1, color));
            var frame = new CellBuffer(40, 1);
            find.Draw(new Region(frame), context, new PromptLabels());
            Assert.Equal(color == ColorDepth.None ? "_" : "b", frame[7, 0].Glyph);
            if (color != ColorDepth.None) { Assert.True((frame[7, 0].Style.Decoration & TextDecoration.Reverse) != 0); }

            find.Handle(InputEvent.Press(ConsoleKey.End));
            foreach (char c in "defghijklmnopqrstuvwxyz") { find.Handle(InputEvent.Character(c)); }
            frame = new CellBuffer(30, 1);
            find.Draw(new Region(frame), context, new PromptLabels());
            Assert.Contains("pqrstuvwxyz", frame.ToPlainText());
            Assert.Equal(color == ColorDepth.None ? "_" : " ", frame[17, 0].Glyph);
            if (color != ColorDepth.None) { Assert.True((frame[17, 0].Style.Decoration & TextDecoration.Reverse) != 0); }

            find.Handle(InputEvent.Press(ConsoleKey.Home));
            find.Draw(new Region(frame), context, new PromptLabels());
            Assert.Equal(color == ColorDepth.None ? "_" : "a", frame[6, 0].Glyph);
            Assert.Contains("bcdefghijk", frame.ToPlainText());
        }

        [Fact]
        public void LogFindTracksAppendsEvictionsAndFilters() {
            var log = new LogViewport("Output") { ShowElapsed = false, Capacity = 3 };
            log.Write("error-a");
            using var app = new AppController(log, new AppState(), Context, 60);
            app.PrepareFrame(80, 24);
            Type(app, "/error");
            Assert.Contains("1 of 1", app.PrepareFrame(80, 24).ToPlainText());
            log.Write("error-b");
            Assert.Contains("1 of 2", app.PrepareFrame(80, 24).ToPlainText());
            log.Write("other\nlast");
            Assert.Contains("1 of 1", app.PrepareFrame(80, 24).ToPlainText());
            log.Filter = new LogFilter(text: "last");
            Assert.Contains("No matches", app.PrepareFrame(80, 24).ToPlainText());
            Press(app, ConsoleKey.Escape);
            Type(app, "n");
            Assert.Contains("last", app.PrepareFrame(80, 24).ToPlainText());
            Assert.DoesNotContain("error-b", app.PrepareFrame(80, 24).ToPlainText());
        }

        [Fact]
        public void TextAreaFindHighlightsAMatchAcrossSoftWrappedRows() {
            var notes = new TextArea("Notes", "abcdefghijklmnop");
            using var app = new AppController(notes, new AppState(), Context, 60);
            app.PrepareFrame(14, 12);
            Press(app, ConsoleKey.F, KeyModifiers.Control);
            Type(app, "defghijk");
            CellBuffer frame = app.PrepareFrame(14, 12);
            Assert.True(Styled(frame, "defgh", TextDecoration.Reverse));
            Assert.True(Styled(frame, "ijk", TextDecoration.Reverse));
            notes.Value = "short";
            Assert.Contains("No matches", app.PrepareFrame(80, 24).ToPlainText());
        }

        [Theory]
        [InlineData(80)]
        [InlineData(6)]
        public void EmptyTextAreaKeepsItsFindBarVisible(int width) {
            var notes = new TextArea("Notes") { Placeholder = "Type notes here" };
            using var app = new AppController(notes, new AppState(), Context, 60);
            app.PrepareFrame(width, 12);
            Press(app, ConsoleKey.F, KeyModifiers.Control);
            Type(app, "query");
            string frame = app.PrepareFrame(width, 12).ToPlainText();
            Assert.Contains("Find:", frame);
            Assert.Equal(string.Empty, notes.Value);
            if (width == 80) { Assert.Contains("Find: query", frame); }
        }

        [Fact]
        public void LogFindRevealsMatchesBeyondTheRightEdge() {
            var log = new LogViewport("Output");
            log.Write(new string('x', 60) + "needle");
            using var app = new AppController(log, new AppState(), Context, 60);
            app.PrepareFrame(30, 15);
            Type(app, "/needle");
            app.PrepareFrame(30, 15);
            Press(app, ConsoleKey.Escape);
            CellBuffer frame = app.PrepareFrame(30, 15);
            Assert.True(Styled(frame, "needle", TextDecoration.Reverse));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DividerDragEndsWhenFocusIsLostOrTheTerminalResizes(bool resize) {
            var splitter = new Splitter(TextBlock.Plain("left"), TextBlock.Plain("right"));
            using var app = new AppController(splitter, new AppState(), Context, 60);
            int start = Divider(app.PrepareFrame(80, 24), "right");
            app.Handle(InputEvent.Pointer(start, 0, MouseAction.Down, button: 1), true);
            app.Handle(resize ? InputEvent.Resized(80, 24) : InputEvent.Focus(false), true);
            app.Handle(InputEvent.Pointer(20, 0, MouseAction.Move), true);
            Assert.Equal(start, Divider(app.PrepareFrame(80, 24), "right"));
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
        public void ADividerKeepsDraggingWhenABindingRebuildsItsSplitterMidDragAndItsKeyHoldsTheRatio() {
            var state = new AppState();
            using var app = new AppController(new AppBinding(s => new Splitter(TextBlock.Plain("left " + s.Get("Round", 0)), TextBlock.Plain("right")) { Key = "Panes" }), state, Context, 60);
            app.Refresh(null);
            int start = Divider(app.PrepareFrame(80, 24), "right");
            app.Handle(InputEvent.Pointer(start, 0, MouseAction.Down, button: 1), true);
            state["Round"] = 1;
            app.Refresh(null);
            Assert.Contains("left 1", app.PrepareFrame(80, 24).ToPlainText());
            app.Handle(InputEvent.Pointer(20, 0, MouseAction.Move, heldButtons: 1), true);
            app.Handle(InputEvent.Pointer(20, 0, MouseAction.Up, button: 1), true);
            app.Refresh(null);
            Assert.Equal(20, Divider(app.PrepareFrame(80, 24), "right"));
            Assert.IsType<double>(state["Panes"]);

            state["Panes"] = 0.75;
            app.Refresh(null);
            Assert.Equal(60, Divider(app.PrepareFrame(80, 24), "right"));
        }

        [Fact]
        public void AVerticalSplitterIsAsTallAsItsPanes() {
            var splitter = new Splitter(TextBlock.Plain("top"), TextBlock.Plain("bottom\nmore")) { Vertical = true };
            Assert.Equal(4, splitter.Measure(new Constraints(40, 20), Context).Height);
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
            // The bar is the screen's last row, whose last cell the terminal output never writes: the count ends before it.
            string bar = text.Split('\n')[9];
            Assert.EndsWith("1 of 1", bar.TrimEnd());
            Assert.True(bar.TrimEnd().Length < 40);

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
