using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;
using Grid = ConsoleMonster.Interactive.Grid;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>
    /// The mouse does what the keyboard does: each control's pointer action at the app or view boundary, with the key it
    /// stands for.
    /// </summary>
    public sealed class PointerParityTests {
        private static readonly RenderContext Context = Terminal.CreateContext(TerminalProfile.Fixed(80, 24));

        [Fact]
        public void FooterHint_ClickPressesItsKey() {
            int refreshed = 0;
            ConsoleAppOptions options = new ConsoleAppOptions();
            options.KeyBindings.Add(new KeyBinding("refresh", () => refreshed++, new KeyChord(ConsoleKey.R, KeyModifiers.Control)));
            using AppController app = new AppController(new TextInput("query"), new AppState(), Context, 60, options);
            string[] rows = Rows(app.PrepareFrame(80, 12));
            int column = rows[11].IndexOf("ctrl+r", StringComparison.Ordinal);
            Assert.True(column >= 0, rows[11]);
            Click(app, column + 2, 11);
            Assert.Equal(1, refreshed);
        }

        [Fact]
        public void Breadcrumb_ClickReturnsToThatScreen() {
            AppScreens screens = new AppScreens(new AppScreen("home", "Home", TextBlock.Plain("home body")), new AppScreen("detail", "Detail", TextBlock.Plain("detail body")));
            using AppController app = new AppController(new Rows().Add(new HeaderBar("Shop"), LayoutSize.Fixed(1)).Add(screens, LayoutSize.Fraction()), new AppState(), Context, 60);
            screens.Push("detail");
            app.Refresh(null);
            string header = Rows(app.PrepareFrame(80, 12))[0];
            int home = header.IndexOf("Home", StringComparison.Ordinal);
            Assert.True(home >= 0 && header.Contains("Detail"), header);
            Click(app, home + 1, 0);
            Assert.Equal("home", screens.Current.Key);
        }

        [Fact]
        public void ScrollViewBar_DragToTheBottomShowsTheEnd() {
            ScrollView scroll = new ScrollView(new TextBlock(string.Join("\n", Enumerable.Range(0, 40).Select(i => "line " + i))));
            using AppController app = new AppController(new Rows().Add(scroll, LayoutSize.Fixed(8)).Add(new TextInput("query"), LayoutSize.Fixed(3)), new AppState(), Context, 60);
            app.PrepareFrame(60, 14);
            app.Handle(InputEvent.Mouse(59, 0), true);
            app.Handle(InputEvent.Pointer(59, 7, MouseAction.Move, heldButtons: 1), true);
            app.Handle(InputEvent.Mouse(59, 7, pressed: false), true);
            Assert.Equal(32, scroll.ScrollOffset);
            Assert.Contains("line 39", app.PrepareFrame(60, 14).ToPlainText());
        }

        [Fact]
        public void Toggle_ClickFlipsLikeSpace() {
            Toggle agree = new Toggle("agree");
            using AppController app = new AppController(new Rows(new TextInput("name"), agree), new AppState(), Context, 60);
            string[] rows = Rows(app.PrepareFrame(80, 12));
            int row = Array.FindIndex(rows, line => line.Contains("[ ]"));
            Click(app, rows[row].IndexOf("[ ]", StringComparison.Ordinal) + 1, row);
            Assert.True(agree.Value);
            app.Handle(InputEvent.Press(ConsoleKey.Spacebar), true);
            Assert.False(agree.Value);
        }

        [Fact]
        public void TextInput_DragSelectsAndTypingReplacesTheSelection() {
            TextInput name = new TextInput("name", "hello world");
            using AppController app = new AppController(new Rows(new Toggle("other"), name), new AppState(), Context, 60);
            string[] rows = Rows(app.PrepareFrame(80, 12));
            int row = Array.FindIndex(rows, line => line.Contains("hello world"));
            int start = rows[row].IndexOf("world", StringComparison.Ordinal);
            app.Handle(InputEvent.Mouse(start, row), true);
            app.Handle(InputEvent.Pointer(start + 5, row, MouseAction.Move, heldButtons: 1), true);
            app.Handle(InputEvent.Mouse(start + 5, row, pressed: false), true);
            app.Handle(InputEvent.Character('X'), true);
            Assert.Equal("hello X", name.Value);
            // Shift with the arrows selects the same way.
            app.Handle(InputEvent.Press(ConsoleKey.Home, KeyModifiers.Shift), true);
            app.Handle(InputEvent.Character('Y'), true);
            Assert.Equal("Y", name.Value);
        }

        [Fact]
        public void TextArea_DragSelectsOnTheClickedRowAndTypingReplacesIt() {
            TextArea notes = new TextArea("notes", "first line\nsecond line");
            using AppController app = new AppController(new Rows(new Toggle("other"), notes), new AppState(), Context, 60);
            string[] rows = Rows(app.PrepareFrame(80, 16));
            int row = Array.FindIndex(rows, line => line.Contains("second line"));
            int end = rows[row].IndexOf("second", StringComparison.Ordinal) + 6;
            app.Handle(InputEvent.Mouse(rows[row].IndexOf("second", StringComparison.Ordinal), row), true);
            app.Handle(InputEvent.Pointer(end, row, MouseAction.Move, heldButtons: 1), true);
            app.Handle(InputEvent.Mouse(end, row, pressed: false), true);
            app.Handle(InputEvent.Character('X'), true);
            Assert.Equal("first line\nX line", notes.Value);
        }

        [Fact]
        public void DataGrid_DoubleClickOpensTheRowLikeEnter() {
            Grid grid = new Grid("Services");
            grid.AddColumn("Name");
            grid.AddRow("alpha", "alpha"); grid.AddRow("beta", "beta"); grid.AddRow("gamma", "gamma");
            DataGrid services = new DataGrid("services", grid);
            object? opened = null;
            services.Bindings.Add(new KeyBinding("open", () => opened = services.CurrentRow?.Item, new KeyChord(ConsoleKey.Enter)));
            using AppController app = new AppController(services, new AppState(), Context, 60);
            string[] rows = Rows(app.PrepareFrame(80, 14));
            int row = Array.FindIndex(rows, line => line.Contains("beta"));
            Click(app, 4, row);
            Assert.Null(opened);
            Click(app, 4, row);
            Assert.Equal("beta", opened);
        }

        [Fact]
        public void DataGrid_HeaderClickSortsAndReversesLikeF3AndF4() {
            Grid grid = new Grid("Services");
            grid.AddColumn("Name"); grid.AddColumn("State");
            grid.AddRow("b", "bravo", "running"); grid.AddRow("a", "alpha", "stopped");
            DataGrid services = new DataGrid("services", grid);
            using AppController app = new AppController(services, new AppState(), Context, 60);
            string[] rows = Rows(app.PrepareFrame(80, 14));
            int row = Array.FindIndex(rows, line => line.Contains("State"));
            int column = rows[row].IndexOf("State", StringComparison.Ordinal);
            Click(app, column + 1, row);
            Assert.Equal((1, false), (services.SortColumn, services.Descending));
            app.PrepareFrame(80, 14);
            Click(app, column + 1, row);
            Assert.Equal((1, true), (services.SortColumn, services.Descending));
            app.Handle(InputEvent.Press(ConsoleKey.F4), true);
            Assert.Equal((1, false), (services.SortColumn, services.Descending));
        }

        [Fact]
        public void TreeSelect_DoubleClickDrillsInLikeEnter() {
            TreeSelect tree = new TreeSelect("tree", new ObjectExplorer(new { Items = new[] { 1, 2, 3 }, Name = "box" }));
            using AppController app = new AppController(tree, new AppState(), Context, 60);
            string[] rows = Rows(app.PrepareFrame(80, 14));
            int row = Array.FindIndex(rows, line => line.Contains("Items"));
            Click(app, rows[row].IndexOf("Items", StringComparison.Ordinal), row);
            Click(app, rows[row].IndexOf("Items", StringComparison.Ordinal), row);
            Assert.Contains("Items", tree.Root.Path);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ContextMenu_ListsTheBindingsAndRunsTheChosenOne(bool pointer) {
            int refreshed = 0;
            ConsoleAppOptions options = new ConsoleAppOptions();
            options.KeyBindings.Add(new KeyBinding("refresh", () => refreshed++, new KeyChord(ConsoleKey.R, KeyModifiers.Control)));
            using AppController app = new AppController(new TextInput("query"), new AppState(), Context, 60, options);
            app.PrepareFrame(80, 12);
            if (pointer) {
                app.Handle(InputEvent.Mouse(10, 1, 3), true);
                app.Handle(InputEvent.Mouse(10, 1, 3, false), true);
            } else {
                app.Handle(InputEvent.Press(ConsoleKey.F10, KeyModifiers.Shift), true);
            }
            string[] rows = Rows(app.PrepareFrame(80, 12));
            int row = Array.FindIndex(rows, line => line.Contains("refresh") && line.Contains("ctrl+r") && line.IndexOf("refresh", StringComparison.Ordinal) < line.IndexOf("ctrl+r", StringComparison.Ordinal) - 3);
            Assert.True(row >= 0 && row < 11, string.Join("\n", rows));
            if (pointer) { Click(app, rows[row].IndexOf("refresh", StringComparison.Ordinal), row); }
            else { app.Handle(InputEvent.Press(ConsoleKey.Enter), true); }
            Assert.Equal(1, refreshed);
            Assert.DoesNotContain(Rows(app.PrepareFrame(80, 12)).Take(11), line => line.Contains("refresh"));
        }

        [Fact]
        public void PagerFooter_ClickAndWheelPageLikeTheirKeys() {
            WidgetPagerView pager = new WidgetPagerView(new TextBlock(string.Join("\n", Enumerable.Range(0, 60).Select(i => "row " + i))), "Doc", new PromptLabels());
            string[] rows = Rows(FrameComposer.Compose(pager.BuildFrame(new ViewFrame(Context, 60, 12, false, TimeSpan.Zero)), 60, 12, Context));
            int page = rows[11].IndexOf("pgup/pgdn", StringComparison.Ordinal);
            pager.Handle(InputEvent.Mouse(page, 11));
            pager.Handle(InputEvent.Mouse(page, 11, pressed: false));
            Assert.Contains("row 10", Rows(FrameComposer.Compose(pager.BuildFrame(new ViewFrame(Context, 60, 12, false, TimeSpan.Zero)), 60, 12, Context))[1]);
            pager.Handle(InputEvent.Pointer(5, 5, MouseAction.Wheel, wheelDelta: -1));
            Assert.Contains("row 13", Rows(FrameComposer.Compose(pager.BuildFrame(new ViewFrame(Context, 60, 12, false, TimeSpan.Zero)), 60, 12, Context))[1]);
        }

        [Fact]
        public void SelectPrompt_DoubleClickChoosesTheRow() {
            string[] values = { "alpha", "beta", "gamma" };
            ScreenCapture first = new ScreenCapture(60, 12);
            using (ScriptedInput escape = new ScriptedInput("Escape")) {
                new SelectPrompt<string>("Pick", values).TryShow(out _, new InteractiveOptions { Input = escape, Capture = first });
            }
            string[] rows = first.Frames.First().ToPlainText().Split('\n');
            int row = Array.FindIndex(rows, line => line.Contains("gamma"));
            int column = rows[row].IndexOf("gamma", StringComparison.Ordinal);
            ScreenCapture second = new ScreenCapture(60, 12);
            using ScriptedInput clicks = new ScriptedInput(new[] {
                InputEvent.Mouse(column, row), InputEvent.Mouse(column, row, pressed: false),
                InputEvent.Mouse(column, row), InputEvent.Mouse(column, row, pressed: false)
            });
            Assert.Equal("gamma", new SelectPrompt<string>("Pick", values).Show(new InteractiveOptions { Input = clicks, Capture = second }));
        }

        [Fact]
        public void TextPrompt_ClickPlacesTheCaretInThePrefilledText() {
            Func<TextPrompt> prompt = () => new TextPrompt("Edit") { Default = "abcdef", PrefillDefault = true };
            (int column, int row) = Find(capture => prompt().TryShow(out _, new InteractiveOptions { Input = new ScriptedInput("Escape"), Capture = capture }), "abcdef");
            using ScriptedInput input = new ScriptedInput(new[] { InputEvent.Mouse(column, row), InputEvent.Mouse(column, row, pressed: false), InputEvent.Character('X'), InputEvent.Press(ConsoleKey.Enter) });
            Assert.Equal("Xabcdef", prompt().Show(new InteractiveOptions { Input = input, Capture = new ScreenCapture(60, 12) }));
        }

        [Fact]
        public void ConfirmPrompt_ClickOnNoAnswersNo() {
            Func<ConfirmPrompt> prompt = () => new ConfirmPrompt("Delete?") { Default = true };
            (int column, int row) = Find(capture => prompt().TryShow(out _, new InteractiveOptions { Input = new ScriptedInput("Escape"), Capture = capture }), "Yes   No");
            using ScriptedInput input = new ScriptedInput(new[] { InputEvent.Mouse(column + 6, row), InputEvent.Mouse(column + 6, row, pressed: false) });
            Assert.False(prompt().Show(new InteractiveOptions { Input = input, Capture = new ScreenCapture(60, 12) }));
        }

        [Fact]
        public void MouseOff_StandalonePromptIgnoresPointerInput() {
            string[] values = { "alpha", "beta", "gamma" };
            (int column, int row) = Find(capture => new SelectPrompt<string>("Pick", values).TryShow(out _, new InteractiveOptions { Input = new ScriptedInput("Escape"), Capture = capture }), "gamma");
            using ScriptedInput input = new ScriptedInput(new[] {
                InputEvent.Mouse(column, row), InputEvent.Mouse(column, row, pressed: false),
                InputEvent.Mouse(column, row), InputEvent.Mouse(column, row, pressed: false), InputEvent.Press(ConsoleKey.Escape)
            });
            Assert.False(new SelectPrompt<string>("Pick", values).TryShow(out _, new InteractiveOptions { Input = input, Capture = new ScreenCapture(60, 12), Mouse = false }));
        }

        [Fact]
        public void TextArea_HitTestingUsesTheCellsTheGlyphTierDraws() {
            RenderContext ascii = new RenderContext(TerminalProfile.Fixed(80, 16, ColorDepth.None, GlyphTier.Ascii), Theme.Dark);
            TextArea notes = new TextArea("notes", "\U0001F44Dabc");
            using AppController app = new AppController(new Rows(new Toggle("other"), notes), new AppState(), ascii, 60);
            string[] rows = Rows(app.PrepareFrame(80, 16));
            int row = Array.FindIndex(rows, line => line.Contains("abc"));
            Click(app, rows[row].IndexOf("bc", StringComparison.Ordinal), row);
            app.Handle(InputEvent.Character('X'), true);
            Assert.Equal("\U0001F44DaXbc", notes.Value);
        }

        [Fact]
        public void OpenDropdown_TakesClicksOverTheCrumbsItCovers() {
            Select schedule = new Select("schedule", new[] { new Choice("Hourly"), new Choice("Nightly"), new Choice("Weekly") }) { Dropdown = true };
            AppScreens screens = new AppScreens(new AppScreen("home", "Home", TextBlock.Plain("home")), new AppScreen("detail", "Detail", TextBlock.Plain("detail")));
            using AppController app = new AppController(new Rows().Add(schedule, LayoutSize.Fixed(3)).Add(TextBlock.Plain("filler"), LayoutSize.Fixed(3)).Add(new HeaderBar("Shop"), LayoutSize.Fixed(1)).Add(screens, LayoutSize.Fraction()), new AppState(), Context, 60);
            screens.Push("detail");
            app.Refresh(null);
            int header = Array.FindIndex(Rows(app.PrepareFrame(80, 14)), line => line.Contains("Home"));
            Click(app, 5, 1);
            string[] rows = Rows(app.PrepareFrame(80, 14));
            int row = Array.FindIndex(rows, line => line.Contains("Nightly"));
            // The list item is drawn on the row the crumbs were on.
            Assert.Equal(header, row);
            Click(app, rows[row].IndexOf("Nightly", StringComparison.Ordinal), row);
            Assert.Equal("Nightly", schedule.Value);
            Assert.Equal("detail", screens.Current.Key);
        }

        [Fact]
        public void DataGrid_DoubleClickOnAHeaderSortsWithoutOpeningARow() {
            Grid grid = new Grid("Services");
            grid.AddColumn("Name"); grid.AddRow("a", "alpha"); grid.AddRow("b", "bravo");
            DataGrid services = new DataGrid("services", grid);
            int opened = 0;
            services.Bindings.Add(new KeyBinding("open", () => opened++, new KeyChord(ConsoleKey.Enter)));
            using AppController app = new AppController(services, new AppState(), Context, 60);
            string[] rows = Rows(app.PrepareFrame(80, 14));
            int row = Array.FindIndex(rows, line => line.Contains("Name"));
            int column = rows[row].IndexOf("Name", StringComparison.Ordinal);
            Click(app, column, row);
            Click(app, column, row);
            Assert.Equal(0, opened);
            Assert.True(services.Descending);
        }

        [Fact]
        public void ContextMenu_ClosesWhenTheScreenChanges() {
            int home = 0, detail = 0;
            AppScreen first = new AppScreen("home", "Home", new TextInput("query"));
            first.Bindings.Add(new KeyBinding("refresh", () => home++, new KeyChord(ConsoleKey.R, KeyModifiers.Control)));
            AppScreen second = new AppScreen("detail", "Detail", TextBlock.Plain("detail"));
            second.Bindings.Add(new KeyBinding("reload", () => detail++, new KeyChord(ConsoleKey.R, KeyModifiers.Control)));
            AppScreens screens = new AppScreens(first, second);
            using AppController app = new AppController(screens, new AppState(), Context, 60);
            app.PrepareFrame(80, 12);
            app.Handle(InputEvent.Press(ConsoleKey.F10, KeyModifiers.Shift), true);
            Assert.Contains("refresh", app.PrepareFrame(80, 12).ToPlainText());
            screens.Push("detail");
            app.Refresh(null);
            Assert.DoesNotContain("refresh", app.PrepareFrame(80, 12).ToPlainText());
            app.Handle(InputEvent.Press(ConsoleKey.Enter), true);
            Assert.Equal((0, 0), (home, detail));
        }

        [Fact]
        public void ScrollBarRelease_OverAFooterHintEndsTheDragWithoutPressingIt() {
            int refreshed = 0;
            ConsoleAppOptions options = new ConsoleAppOptions();
            options.KeyBindings.Add(new KeyBinding("refresh", () => refreshed++, new KeyChord(ConsoleKey.R, KeyModifiers.Control)));
            ScrollView scroll = new ScrollView(new TextBlock(string.Join("\n", Enumerable.Range(0, 40).Select(i => "line " + i))));
            TextInput query = new TextInput("query");
            using AppController app = new AppController(new Rows().Add(query, LayoutSize.Fixed(3)).Add(scroll, LayoutSize.Fixed(8)), new AppState(), Context, 60, options);
            string[] rows = Rows(app.PrepareFrame(60, 14));
            int hint = rows[13].IndexOf("ctrl+r", StringComparison.Ordinal);
            app.Handle(InputEvent.Mouse(59, 3), true);
            app.Handle(InputEvent.Pointer(hint, 13, MouseAction.Move, heldButtons: 1), true);
            app.Handle(InputEvent.Mouse(hint, 13, pressed: false), true);
            int offset = scroll.ScrollOffset;
            Click(app, 4, 1);
            app.Handle(InputEvent.Character('x'), true);
            Assert.Equal((0, offset, "x"), (refreshed, scroll.ScrollOffset, query.Value));
        }

        [Fact]
        public void RedrawnTarget_ReleaseRunsOnlyWhenItStillStandsForTheSameThing() {
            PointerTargets targets = new PointerTargets(regions: false);
            string ran = string.Empty;
            Rect area = new Rect(2, 2, 6, 1);
            using (targets.Collect()) { PointerTargets.RecordAction(area, () => ran += "old", "step:1"); }
            targets.Route(InputEvent.Mouse(3, 2), 1, out _);
            using (targets.Collect()) { PointerTargets.RecordAction(area, () => ran += "other", "step:2"); }
            targets.Route(InputEvent.Mouse(3, 2, pressed: false), 0, out _);
            Assert.Equal(string.Empty, ran);
            targets.Route(InputEvent.Mouse(3, 2), 1, out _);
            using (targets.Collect()) { PointerTargets.RecordAction(area, () => ran += "current", "step:2"); }
            targets.Route(InputEvent.Mouse(3, 2, pressed: false), 0, out _);
            Assert.Equal("current", ran);
        }

        [Fact]
        public void CustomFooterHint_WithAKeyClicksLikeThatKey() {
            int refreshed = 0;
            ConsoleAppOptions options = new ConsoleAppOptions { Footer = new FooterBar { HintsSource = () => new[] { new KeyHint("f5", "refresh", new KeyChord(ConsoleKey.F5)) } } };
            options.KeyBindings.Add(new KeyBinding("refresh", () => refreshed++, new KeyChord(ConsoleKey.F5)) { InFooter = false });
            using AppController app = new AppController(new TextInput("query"), new AppState(), Context, 60, options);
            app.Refresh(null);
            string[] rows = Rows(app.PrepareFrame(80, 12));
            Click(app, rows[11].IndexOf("f5", StringComparison.Ordinal), 11);
            Assert.Equal(1, refreshed);
        }

        // The cell where a prompt first draws text, from a run that is cancelled straight away.
        private static (int Column, int Row) Find(Action<ScreenCapture> run, string text) {
            ScreenCapture capture = new ScreenCapture(60, 12);
            run(capture);
            string[] rows = capture.Frames.First().ToPlainText().Split('\n');
            int row = Array.FindIndex(rows, line => line.Contains(text));
            Assert.True(row >= 0, "[" + string.Join("|\n", rows) + "]");
            return (rows[row].IndexOf(text, StringComparison.Ordinal), row);
        }
        private static void Click(AppController app, int column, int row) {
            app.Handle(InputEvent.Mouse(column, row), true);
            app.Handle(InputEvent.Mouse(column, row, pressed: false), true);
            app.Refresh(null);
        }

        private static string[] Rows(CellBuffer frame) => frame.ToPlainText().Split('\n');
    }
}
