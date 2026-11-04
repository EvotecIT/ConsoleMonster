using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
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

        private static void Click(AppController app, int column, int row) {
            app.Handle(InputEvent.Mouse(column, row), true);
            app.Handle(InputEvent.Mouse(column, row, pressed: false), true);
            app.Refresh(null);
        }

        private static string[] Rows(CellBuffer frame) => frame.ToPlainText().Split('\n');
    }
}
