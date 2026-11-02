using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;
using Xunit;
using Grid = ConsoleMonster.Interactive.Grid;

namespace ConsoleMonster.Tests.Interactive.App {
    public sealed class Phase3RegressionTests {
        private static readonly Canvas.RenderContext Context = Terminal.CreateContext(TerminalProfile.Fixed(80, 24));

        [Fact]
        public void SharedListingChangesAreAdoptedOnlyByExplicitRefreshWithMarkReconciliation() {
            var listing = new Grid("Files"); listing.AddColumn("Name");
            listing.AddRow(new GridRow("old", "old") { Id = 1 });
            using var first = new DataGrid("First", listing);
            using var second = new DataGrid("Second", listing);
            second.ToggleMark();
            first.SetRows(new[] { new GridRow("new", "new") { Id = 2 } });
            second.Sort(0); second.Filter = "old";
            Assert.Equal("old", Assert.Single(second.Selection)); Assert.Equal(1, second.MarkCount);
            second.Filter = ""; second.RefreshRows();
            Assert.Equal("new", Assert.Single(second.Selection)); Assert.Equal(0, second.MarkCount);
        }

        [Fact]
        public void BackingListingCanShrinkAndClearBeforeRefreshWithoutInvalidatingTheOldSelection() {
            var listing = new Grid("Files"); listing.AddColumn("Name");
            listing.AddRow(new GridRow("one", "one") { Id = 1 }); listing.AddRow(new GridRow("two", "two") { Id = 2 });
            using var grid = new DataGrid("Rows", listing);
            grid.Move(1); grid.ToggleMark();
            listing.SetRows(new[] { new GridRow("replacement", "two") { Id = 2 } });
            Assert.Equal("two", Assert.Single(grid.Selection));
            grid.RefreshRows();
            Assert.Equal("replacement", grid.CurrentRow!.Item); Assert.Equal("replacement", Assert.Single(grid.Marks));
            listing.SetRows(Array.Empty<GridRow>());
            grid.RefreshRows();
            Assert.Null(grid.CurrentRow); Assert.Empty(grid.Selection); Assert.Equal(0, grid.VisibleCount);
        }

        [Fact]
        public void HiddenNestedTabHeadersCannotConsumeVisiblePageClicks() {
            var hidden = new Tabs().Add("One", new TextInput("A1")).Add("Two", new TextInput("A2"));
            var visible = new Tabs().Add("One", new TextInput("B1")).Add("Two", new TextInput("B2"));
            var outer = new Tabs().Add("A", hidden).Add("B", visible);
            using var app = new AppController(outer, new AppState(), Context, 60);
            app.PrepareFrame(80, 24);
            outer.SelectedIndex = 1; app.PrepareFrame(80, 24);
            app.Handle(InputEvent.Mouse(8, 1), true); app.Refresh(null);
            Assert.Equal(0, hidden.SelectedIndex); Assert.Equal(1, visible.SelectedIndex);
            Assert.Equal("B2", app.Focus.Current!.Key);
        }

        [Fact]
        public void LongModalSummarySupportsKeyboardAndWheelScrollWhileButtonsRetainFocus() {
            var dialogs = new AppDialogs();
            using var app = new AppController(new TextInput("Underlying"), new AppState(), Context, 60, new ConsoleAppOptions { Dialogs = dialogs });
            var lines = new Rows(Enumerable.Range(0, 70).Select(i => (IWidget)TextBlock.Plain("failure-" + i.ToString("D2"))).ToArray());
            dialogs.Show(new FormDialog<bool>("Summary", new ScrollView(lines), _ => true), _ => { });
            string before = app.PrepareFrame(80, 24).ToPlainText();
            Assert.DoesNotContain("failure-40", before);
            for (int i = 0; i < 8; i++) { app.Handle(InputEvent.Press(ConsoleKey.PageDown, KeyModifiers.Alt), true); app.PrepareFrame(80, 24); }
            string after = app.PrepareFrame(80, 24).ToPlainText();
            Assert.True(after.Contains("failure-40"), after);
            string[] rows = after.Split('\n');
            int y = Array.FindIndex(rows, row => row.Contains("failure-40"));
            app.Handle(InputEvent.Pointer(35, y, MouseAction.Wheel, wheelDelta: 1), true);
            Assert.Contains("failure-37", app.PrepareFrame(80, 24).ToPlainText());
            Assert.True(dialogs.Open);
        }

        [Fact]
        public void PausedLogRetainsItsSurvivingVisibleRowsAcrossMultilineEviction() {
            // Two log rows and the paused state line inside the frame.
            var log = new LogViewport("Output") { Height = 3, Capacity = 8, ShowElapsed = false };
            log.Write(string.Join("\n", Enumerable.Range(0, 8).Select(i => "line-" + i)));
            log.ScrollOffset = 2;
            string[] before = FrameComposer.Compose(log, 40, 5, Context).ToPlainText().Split('\n');
            Assert.Contains("line-4", before[1]); Assert.Contains("line-5", before[2]);
            log.Write("line-8\nline-9\nline-10");
            string[] after = FrameComposer.Compose(log, 40, 5, Context).ToPlainText().Split('\n');
            Assert.Equal(before[1], after[1]); Assert.Equal(before[2], after[2]);
            Assert.Contains("Paused · 3 new", after[3]);
            Assert.Equal(8, log.Entries.Count);
        }

        [Fact]
        public async Task FormCallbackStartsAndCancelsTheSharedActionWorkerThroughItsPublicService() {
            // Own one app thread while xUnit yields its worker for the action's Task.Run.
            await Task.Factory.StartNew(() => {
                var actions = new AppActions(); var dialogs = new AppDialogs();
                var field = new TextInput("Selection", "selected");
                var options = new ConsoleAppOptions { Actions = actions, Dialogs = dialogs, Selection = _ => new object?[] { field.Value } };
                string? captured = null;
                var command = new AppCommand("dynamic", "Dynamic operation", async (context, cancellation) => {
                    Volatile.Write(ref captured, (string?)context.Rows[0]);
                    await Task.Delay(Timeout.Infinite, cancellation);
                }) { ProgressDialog = true };
                using (var app = new AppController(field, new AppState(), Context, 60, options)) {
                    dialogs.Show(new FormDialog<bool>("Options", new Rows(), _ => true) { DefaultAccept = true }, result => { if (result.Accepted) { Assert.True(actions.Invoke(command)); } });
                    app.PrepareFrame(80, 24); app.Handle(InputEvent.Press(ConsoleKey.Enter), true);
                    Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref captured) != null, TimeSpan.FromSeconds(5)));
                    Assert.Equal("selected", captured); Assert.False(actions.Invoke(command));
                    actions.Cancel();
                    Assert.True(SpinWait.SpinUntil(() => !actions.Running, TimeSpan.FromSeconds(5)));
                    app.Refresh(null);
                    Assert.Contains("cancelled", app.PrepareFrame(80, 24).ToPlainText());
                    Assert.Equal("selected", field.Value);
                }
                Assert.Throws<InvalidOperationException>(() => actions.Invoke(command));
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}
