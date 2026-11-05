using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    // The parts the document viewer is built from, used on their own in an app with no document.
    public sealed class NavigationPartsTests {
        private static readonly RenderContext Context = Terminal.CreateContext(TerminalProfile.Fixed(80, 24));

        [Fact]
        public void OutlineOpensItsTargetSectionAndTheBreadcrumbFollowsIt() {
            var general = new Collapsible("General", new TextInput("Name")) { Expanded = true };
            var proxy = new Collapsible("Proxy", new TextInput("Server"));
            var outline = new Outline("Sections") { Items = { new OutlineItem("General") { Target = general }, new OutlineItem("Proxy", 2) { Target = proxy } }, SelectedIndex = 0 };
            var path = new Breadcrumb("Where") { Outline = outline, Ancestors = { "Settings" } };
            var state = new AppState();
            using var app = new AppController(new Rows(path, new Splitter(outline, new ScrollView(new Rows(general, proxy)))), state, Context, 60);
            string first = app.PrepareFrame(80, 24).ToPlainText();
            Assert.Same(outline, app.Focus.Current);
            Assert.StartsWith("Settings › General", first);

            Press(app, ConsoleKey.DownArrow);
            Press(app, ConsoleKey.Enter);
            Assert.True(proxy.Expanded);
            Assert.Same(proxy, app.Focus.Current);
            Assert.Equal("Proxy", state["Sections"]);
            Assert.StartsWith("Settings › General › Proxy", app.PrepareFrame(80, 24).ToPlainText());
        }

        [Fact]
        public void OutlineFollowsItsCurrentEntryUntilFocusedAndOpensOnAClick() {
            var outline = new Outline("Toc");
            foreach (int i in Enumerable.Range(1, 30)) { outline.Items.Add(new OutlineItem("Entry " + i)); }
            using var app = new AppController(new Rows(new TextInput("Before"), outline), new AppState(), Context, 60);
            outline.SelectedIndex = 25;
            string followed = app.PrepareFrame(80, 12).ToPlainText();
            Assert.Contains("Entry 26", followed);
            Assert.DoesNotContain("Entry 1\n", followed);

            string[] rows = followed.Split('\n');
            int row = Array.FindIndex(rows, line => line.Contains("Entry 24"));
            Click(app, 4, row);
            Assert.Equal(23, outline.SelectedIndex);
            Assert.Same(outline, app.Focus.Current);
        }

        [Fact]
        public void TheWheelScrollsAnOutlineAwayFromItsCurrentEntry() {
            var outline = new Outline("Toc");
            foreach (int i in Enumerable.Range(1, 30)) { outline.Items.Add(new OutlineItem("Entry " + i)); }
            outline.SelectedIndex = 0;
            using var app = new AppController(outline, new AppState(), Context, 60);
            Assert.EndsWith("Entry 1", app.PrepareFrame(80, 8).ToPlainText().Split('\n')[0].TrimEnd());
            app.Handle(InputEvent.Pointer(4, 2, MouseAction.Wheel, wheelDelta: -1), true);
            app.Refresh(null);
            Assert.EndsWith("Entry 4", app.PrepareFrame(80, 8).ToPlainText().Split('\n')[0].TrimEnd());
            Assert.EndsWith("Entry 4", app.PrepareFrame(80, 8).ToPlainText().Split('\n')[0].TrimEnd());
        }
        [Fact]
        public void BreadcrumbSegmentsOpenFromTheKeyboardAndShortenFromTheLeft() {
            var path = new Breadcrumb("Where") { Segments = { "Settings", "Network", "Proxy" } };
            var state = new AppState();
            using var app = new AppController(new Rows(path, new TextInput("Name")), state, Context, 60);
            app.PrepareFrame(80, 24);
            Assert.Equal("Name", app.Focus.Current!.Key);

            Press(app, ConsoleKey.Tab, KeyModifiers.Shift);
            Assert.Same(path, app.Focus.Current);
            Press(app, ConsoleKey.LeftArrow);
            Press(app, ConsoleKey.Enter);
            Assert.Equal("Network", state["Where"]);

            string narrow = app.PrepareFrame(18, 24).ToPlainText().Split('\n')[0];
            Assert.StartsWith("…", narrow);
            Assert.Contains("Proxy", narrow);
            Assert.DoesNotContain("Settings", narrow);
        }

        [Fact]
        public void HistoryReturnsBackAndForwardAndForgetsForwardOnANewVisit() {
            var history = new NavigationHistory<string>(capacity: 2);
            history.Visit("a");
            history.Visit("b");
            history.Visit("c");
            Assert.True(history.TryBack("d", out string place)); Assert.Equal("c", place);
            Assert.True(history.TryBack(place, out place)); Assert.Equal("b", place);
            Assert.False(history.TryBack(place, out _));
            Assert.True(history.TryForward("b", out place)); Assert.Equal("c", place);
            history.Visit(place);
            Assert.False(history.CanGoForward);
        }

        [Fact]
        public void KeyedTabsKeepTheActiveTitleInStateAndReportEverySwitch() {
            var tabs = new Tabs("Page").Add("General", new TextInput("Name")).Tab("Network", c => c.Add(new TextInput("Proxy")));
            var changes = new System.Collections.Generic.List<object?>();
            tabs.Changed += (_, change) => changes.Add(change.Value);
            var state = new AppState();
            using var app = new AppController(tabs, state, Context, 60);
            string strip = app.PrepareFrame(80, 24).ToPlainText().Split('\n')[0];
            Assert.Equal("General", state["Page"]);

            Press(app, ConsoleKey.PageDown, KeyModifiers.Control);
            Assert.Equal("Network", state["Page"]);
            app.PrepareFrame(80, 24);
            Click(app, strip.IndexOf("General", StringComparison.Ordinal), 0);
            Assert.Equal("General", state["Page"]);
            tabs.SelectedIndex = 1;
            Assert.Equal("Network", state["Page"]);
            Assert.Equal(new object?[] { "Network", "General", "Network" }, changes);
        }

        private static void Press(AppController app, ConsoleKey key, KeyModifiers modifiers = KeyModifiers.None) {
            app.Handle(InputEvent.Press(key, modifiers), true);
            app.Refresh(null);
            app.PrepareFrame(80, 24);
        }

        private static void Click(AppController app, int column, int row) {
            app.Handle(InputEvent.Pointer(column, row, MouseAction.Down, button: 1), true);
            app.Handle(InputEvent.Pointer(column, row, MouseAction.Up, button: 1), true);
            app.Refresh(null);
        }
    }
}
