using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;
using Grid = ConsoleMonster.Interactive.Grid;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Commands keep selection, confirmation, work and output inside the running app.</summary>
    public sealed class AppCommandTests {
        private static readonly RenderContext Context = Terminal.CreateContext(TerminalProfile.Fixed(100, 30));

        [Fact]
        public void Confirmation_CapturesRowsBeforeWorkAndKeepsAppOpen() {
            List<object?> selected = new List<object?> { "Spooler" };
            using ManualResetEventSlim finished = new ManualResetEventSlim();
            IReadOnlyList<object?>? received = null;
            AppCommand restart = new AppCommand("restart", "Restart", (context, token) => {
                received = context.Rows; context.Write("Restarted " + context.Rows[0]); finished.Set(); return Task.CompletedTask;
            }) { ConfirmPrompt = "Restart {Selection}?", Shortcut = new KeyChord(ConsoleKey.F5) };
            using AppCommandHost host = new AppCommandHost(new[] { restart }, () => selected, new PromptLabels(), default);
            host.Invoke(restart);
            selected[0] = "WinRM";
            Assert.True(host.HasModal);
            Assert.False(host.IsBusy);
            string confirmation = FrameComposer.Compose(host.Wrap(new TextBlock("Services remain open")), 100, 30, Context).ToPlainText();
            Assert.Contains("Restart Spooler?", confirmation);
            Assert.DoesNotContain("Restart WinRM?", confirmation);
            host.Handle(InputEvent.Press(ConsoleKey.LeftArrow));
            host.Handle(InputEvent.Press(ConsoleKey.Enter));
            Assert.True(finished.Wait(TimeSpan.FromSeconds(5)));
            Assert.True(SpinWait.SpinUntil(() => !host.IsBusy, TimeSpan.FromSeconds(5)));
            Assert.Equal("Spooler", Assert.Single(received!));
            Assert.False(host.HasModal);
            string frame = FrameComposer.Compose(host.Wrap(new TextBlock("Services remain open")), 100, 30, Context).ToPlainText();
            Assert.Contains("Services remain open", frame);
            Assert.Contains("Restarted Spooler", frame);
            Assert.Contains("completed", frame);
        }

        [Fact]
        public void PaletteAndConfirmation_HandleAKeyboardBurstBeforeAnotherPaint() {
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            AppCommand command = new AppCommand("restart", "Restart", (_, __) => completion.Task) { ConfirmPrompt = "Restart?" };
            using AppCommandHost host = new AppCommandHost(new[] { command }, () => Array.Empty<object?>(), new PromptLabels(), default);
            FrameComposer.Compose(host.Wrap(new TextBlock("body")), 80, 24, Context);
            host.Handle(InputEvent.Press(ConsoleKey.K, KeyModifiers.Control));
            host.Handle(InputEvent.Press(ConsoleKey.Enter));
            Assert.True(host.HasModal);
            host.Handle(InputEvent.Press(ConsoleKey.LeftArrow));
            host.Handle(InputEvent.Press(ConsoleKey.Enter));
            try {
                Assert.True(host.IsBusy);
                Assert.False(host.HasModal);
            } finally { completion.TrySetResult(true); }
        }

        [Fact]
        public void ModalEscape_LeavesCommandUnstarted() {
            int calls = 0;
            AppCommand command = new AppCommand("delete", "Delete", (_, __) => { calls++; return Task.CompletedTask; }) { ConfirmPrompt = "Delete?" };
            using AppCommandHost host = new AppCommandHost(new[] { command }, () => Array.Empty<object?>(), new PromptLabels(), default);
            host.Invoke(command);
            FrameComposer.Compose(host.Wrap(new TextBlock("body")), 80, 24, Context);
            Assert.True(host.Handle(InputEvent.Press(ConsoleKey.Escape)));
            Assert.False(host.HasModal);
            Assert.Equal(0, calls);
        }

        [Fact]
        public void Cancellation_StreamsOutputAndAllowsAnotherAction() {
            using ManualResetEventSlim started = new ManualResetEventSlim();
            AppCommand command = new AppCommand("query", "Query", async (context, token) => {
                context.Write("Connecting"); started.Set(); await Task.Delay(Timeout.Infinite, token);
            });
            using AppActionSession session = new AppActionSession(default);
            Assert.True(session.Start(command, Array.Empty<object?>()));
            Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
            Assert.False(session.Start(command, Array.Empty<object?>()));
            Assert.Contains(session.Output, entry => entry.Text == "Connecting");
            session.Cancel();
            Assert.True(SpinWait.SpinUntil(() => !session.IsBusy, TimeSpan.FromSeconds(5)));
            Assert.Contains(session.Output, entry => entry.Text.Contains("cancelled"));
            Assert.True(session.Start(new AppCommand("next", "Next", (_, __) => Task.CompletedTask), Array.Empty<object?>()));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AggregatedActionFailure_LogsEveryCauseAndAllowsAnotherAction(bool empty) {
            Exception failure = empty ? new AggregateException("Backend unavailable") : new AggregateException(
                new InvalidOperationException("First service unavailable"), new AggregateException(new InvalidOperationException("Second service unavailable")));
            AppCommand command = new AppCommand("query", "Query", (_, __) => Task.FromException(failure));
            using AppActionSession session = new AppActionSession(default);
            Assert.True(session.Start(command, Array.Empty<object?>()));
            Assert.True(SpinWait.SpinUntil(() => !session.IsBusy, TimeSpan.FromSeconds(5)));
            Assert.Contains(session.Output, entry => entry.Text == (empty ? "Query: Backend unavailable" : "Query: First service unavailable") && entry.Level == "err");
            if (!empty) { Assert.Contains(session.Output, entry => entry.Text == "Query: Second service unavailable" && entry.Level == "err"); }
            Assert.Null(session.CompletedCommand);
            Assert.True(session.Start(new AppCommand("next", "Next", (_, __) => Task.CompletedTask), Array.Empty<object?>()));
        }

        [Theory]
        [InlineData("Ctrl+C")]
        [InlineData("Ctrl+K")]
        [InlineData("Escape")]
        [InlineData("Tab")]
        [InlineData("F6")]
        [InlineData("F8")]
        [InlineData("r")]
        public void ReservedShortcut_IsRejectedBeforeTheHostStarts(string shortcut) {
            AppCommand command = new AppCommand("restart", "Restart", (_, __) => Task.CompletedTask) { Shortcut = KeyChord.Parse(shortcut) };
            Assert.Throws<ArgumentException>(() => new AppCommandHost(new[] { command }, () => Array.Empty<object?>(), new PromptLabels(), default));
        }

        [Fact]
        public void Dispatcher_IsBoundedAndDoesNotLoseAcceptedWork() {
            using AppDispatcher dispatcher = new AppDispatcher(2);
            List<int> calls = new List<int>();
            Assert.True(dispatcher.TryPost(() => calls.Add(1)));
            Assert.True(dispatcher.TryPost(() => calls.Add(2)));
            Assert.False(dispatcher.TryPost(() => calls.Add(3)));
            Assert.Equal(1, dispatcher.Drain(1));
            Assert.Equal(new[] { 1 }, calls);
            Assert.Equal(1, dispatcher.Drain());
            Assert.Equal(new[] { 1, 2 }, calls);
            dispatcher.Dispose();
            Assert.False(dispatcher.TryPost(() => calls.Add(4)));
        }

        [Theory]
        [InlineData("Two", "two")]
        [InlineData("missing", null)]
        public void GridAction_CapturesTheLatestFilterBeforeAnotherPaint(string filter, string? expected) {
            Grid grid = new Grid("Services"); grid.AddColumn("Name"); grid.AddRow("one", "One"); grid.AddRow("two", "Two");
            IReadOnlyList<object?>? captured = null;
            AppCommand command = new AppCommand("run", "Run", (_, __) => Task.CompletedTask) { Shortcut = new KeyChord(ConsoleKey.F5), ConfirmPrompt = "Run selected?" };
            command.CaptureRows = rows => { captured = rows.ToArray(); return captured; };
            GridOptions options = new GridOptions(); options.Commands.Add(command);
            using GridView view = new GridView(grid, options, new PromptLabels());
            FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context);
            foreach (char key in filter) { view.Handle(InputEvent.Character(key)); }
            view.Handle(InputEvent.Press(ConsoleKey.F5));
            Assert.NotNull(captured);
            if (expected == null) { Assert.Empty(captured); }
            else { Assert.Equal(expected, Assert.Single(captured)); }
        }

        [Fact]
        public void BodyClick_ReturnsKeyboardFocusFromTheActionBar() {
            AppCommand command = new AppCommand("run", "Run", (_, __) => Task.CompletedTask);
            using AppCommandHost host = new AppCommandHost(new[] { command }, () => Array.Empty<object?>(), new PromptLabels(), default, commandBar: true);
            FrameComposer.Compose(host.Wrap(new TextBlock("body")), 100, 29, Context);
            host.Handle(InputEvent.Mouse(5, 21, 1, true));
            host.Handle(InputEvent.Mouse(5, 21, 1, false));
            Assert.True(host.HasBarFocus);
            Assert.False(host.Handle(InputEvent.Mouse(5, 5, 1, true)));
            Assert.False(host.HasBarFocus);
            Assert.False(host.Handle(InputEvent.Press(ConsoleKey.Enter)));
        }

        [Fact]
        public void NarrowActionBar_KeepsEveryWrappedPrimaryActionVisible() {
            AppCommand[] commands = new[] { "Restart", "Stop", "Start" }.Select(label => new AppCommand(label, label, (_, __) => Task.CompletedTask)).ToArray();
            using AppCommandHost host = new AppCommandHost(commands, () => Array.Empty<object?>(), new PromptLabels(), default, commandBar: true);
            string frame = FrameComposer.Compose(host.Wrap(new TextBlock("Services")), 30, 12, Context).ToPlainText();
            foreach (AppCommand command in commands) { Assert.Contains("[ " + command.Label + " ]", frame); }
        }

        [Fact]
        public void GridResize_CancelsAPressedActionThroughTheProductionLoop() {
            int calls = 0;
            Grid grid = new Grid("Services"); grid.AddColumn("Name"); grid.AddRow("one", "One");
            GridOptions options = new GridOptions();
            AppCommand command = new AppCommand("run", "Run", (_, __) => Task.CompletedTask);
            command.CaptureRows = rows => { calls++; return rows; };
            options.Commands.Add(command);
            RecordingSurface surface = new RecordingSurface(100, 30, false);
            options.Interactive = PromptHarness.Options("", surface);
            options.Interactive.Profile = TerminalProfile.Fixed(100, 30);
            options.Interactive.Input = new ScriptedInput(new[] {
                InputEvent.Mouse(5, 21, 1, true), InputEvent.Resized(100, 30), InputEvent.Mouse(5, 21, 1, false),
                InputEvent.Press(ConsoleKey.Escape), InputEvent.Press(ConsoleKey.Escape)
            }) { Paced = true };
            PromptHarness.Guard(surface, () => grid.Show(options));
            Assert.Equal(0, calls);
        }

        [Fact]
        public void GridAction_UsesMarkedRowsAndReturnsToGridAfterCompletion() {
            Grid grid = new Grid("Services"); grid.AddColumn("Name"); grid.AddRow("one", "One"); grid.AddRow("two", "Two");
            IReadOnlyList<object?>? received = null;
            using ManualResetEventSlim finished = new ManualResetEventSlim();
            GridOptions options = new GridOptions();
            options.Commands.Add(new AppCommand("refresh", "Refresh", (context, token) => { received = context.Rows; context.Write("Refreshed"); finished.Set(); return Task.CompletedTask; }) { Shortcut = new KeyChord(ConsoleKey.F5) });
            using GridView view = new GridView(grid, options, new PromptLabels());
            FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context);
            view.Handle(InputEvent.Press(ConsoleKey.Spacebar)); view.Handle(InputEvent.Press(ConsoleKey.DownArrow)); view.Handle(InputEvent.Press(ConsoleKey.Spacebar));
            view.Handle(InputEvent.Press(ConsoleKey.F5));
            Assert.True(finished.Wait(TimeSpan.FromSeconds(5)));
            Assert.Equal(new object?[] { "one", "two" }, received);
            Assert.False(view.IsDone);
            string frame = string.Empty;
            Assert.True(SpinWait.SpinUntil(() => {
                view.Poll(TimeSpan.Zero);
                frame = FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context).ToPlainText();
                return frame.Contains("Refresh: completed") && frame.Contains("Action output") && !frame.Contains("F6 cancel action");
            }, TimeSpan.FromSeconds(5)));
            Assert.Contains("Selected row", frame); Assert.Contains("Action output", frame); Assert.Contains("Refreshed", frame);
            // This golden protects the layout, independently of the real action stopwatch.
            frame = Regex.Replace(frame, @"(?<=│ )\d{2}:\d{2}(?= )", "00:00");
            Golden.Match("interactive/grid.actions.txt", frame.TrimEnd() + "\n");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GridRefresh_UpdatesOnlyTheActedOnRowOnTheInputThread(bool succeeds) {
            Grid grid = new Grid("Services"); grid.AddColumn("Name").AddColumn("ProcessId");
            grid.AddRow("one", "One", 2456); grid.AddRow("two", "Two", 1000);
            TaskCompletionSource<bool> finish = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            GridOptions options = new GridOptions();
            options.Commands.Add(new AppCommand("restart", "Restart", (_, __) => finish.Task) { Shortcut = new KeyChord(ConsoleKey.F5) });
            int refreshes = 0;
            int inputThread = Thread.CurrentThread.ManagedThreadId;
            options.RefreshRow = (command, row) => {
                Assert.Equal(Thread.CurrentThread.ManagedThreadId, inputThread);
                Assert.Equal("restart", command.Key);
                Assert.Equal("one", row.Item);
                refreshes++;
                return new GridRow(row.Item, "One", 2556);
            };
            using GridView view = new GridView(grid, options, new PromptLabels());
            FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context);
            view.Handle(InputEvent.Press(ConsoleKey.F5));
            view.Handle(InputEvent.Press(ConsoleKey.DownArrow));
            if (succeeds) { finish.SetResult(true); } else { finish.SetException(new InvalidOperationException("Backend failed")); }
            Assert.True(SpinWait.SpinUntil(() => {
                view.Poll(TimeSpan.Zero);
                string text = FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context).ToPlainText();
                return text.Contains(succeeds ? "Restart: completed" : "Backend failed") && (!succeeds || refreshes == 1);
            }, TimeSpan.FromSeconds(10)));
            view.Poll(TimeSpan.Zero);
            Assert.Equal(succeeds ? 1 : 0, refreshes);
            Assert.Equal(succeeds ? 2556 : 2456, grid.Rows[0].Values[1]);
            Assert.Equal(1000, grid.Rows[1].Values[1]);
            Assert.Equal("two", Assert.Single(view.ChosenItems()));
            Assert.False(view.IsDone);

        }

        [Fact]
        public void CompactActionGrid_ShowsItsVisibleRowRangeWhilePaging() {
            Grid grid = new Grid("Services"); grid.AddColumn("Name");
            for (int i = 1; i <= 10; i++) { grid.AddRow(i, "Service" + i.ToString("00")); }
            GridOptions options = new GridOptions();
            options.Commands.Add(new AppCommand("restart", "Restart", (_, __) => Task.CompletedTask));
            using GridView view = new GridView(grid, options, new PromptLabels());
            string Draw() => FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 80, 24, false, TimeSpan.Zero)), 80, 24, Context).ToPlainText();
            Assert.Matches("1–[1-9] of 10", Draw());
            view.Handle(InputEvent.Press(ConsoleKey.End, KeyModifiers.Control));
            string last = Draw();
            Assert.Contains("Service10", last);
            Assert.Matches("[2-9]–10 of 10", last);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GridRefresh_DoesNotRefreshASiblingWithTheSameItem(bool nullItem) {
            object? item = nullItem ? null : new object();
            Grid grid = new Grid("Services"); grid.AddColumn("Name").AddColumn("ProcessId");
            grid.AddRow(item, "One", 2456); grid.AddRow(item, "Two", 1000);
            GridOptions options = new GridOptions();
            options.Commands.Add(new AppCommand("restart", "Restart", (_, __) => Task.CompletedTask) { Shortcut = new KeyChord(ConsoleKey.F5) });
            int calls = 0;
            options.RefreshRow = (_, row) => { calls++; return new GridRow(row.Item, row.Values[0], 2556); };
            using GridView view = new GridView(grid, options, new PromptLabels());
            FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context);
            view.Handle(InputEvent.Press(ConsoleKey.F5));
            Assert.True(SpinWait.SpinUntil(() => { view.Poll(TimeSpan.Zero); return calls > 0; }, TimeSpan.FromSeconds(10)));
            Assert.Equal(1, calls);
            Assert.Equal(2556, grid.Rows[0].Values[1]);
            Assert.Equal(1000, grid.Rows[1].Values[1]);
        }

        [Fact]
        public void GridRefresh_ReportsEachFailureAndContinuesRefreshingMarkedRows() {
            Grid grid = new Grid("Services"); grid.AddColumn("Name").AddColumn("ProcessId");
            grid.AddRow("one", "One", 2456); grid.AddRow("two", "Two", 1000); grid.AddRow("three", "Three", 3000);
            GridOptions options = new GridOptions();
            options.Commands.Add(new AppCommand("restart", "Restart", (_, __) => Task.CompletedTask) { Shortcut = new KeyChord(ConsoleKey.F5) });
            int calls = 0;
            options.RefreshRow = (_, row) => {
                calls++;
                if (Equals(row.Item, "one")) { throw new InvalidOperationException("First row unavailable"); }
                if (Equals(row.Item, "two")) { throw new InvalidOperationException("Second row unavailable"); }
                return new GridRow(row.Item, row.Values[0], 2556);
            };
            using GridView view = new GridView(grid, options, new PromptLabels());
            FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context);
            view.Handle(InputEvent.Press(ConsoleKey.Spacebar)); view.Handle(InputEvent.Press(ConsoleKey.DownArrow)); view.Handle(InputEvent.Press(ConsoleKey.Spacebar));
            view.Handle(InputEvent.Press(ConsoleKey.DownArrow)); view.Handle(InputEvent.Press(ConsoleKey.Spacebar));
            view.Handle(InputEvent.Press(ConsoleKey.F5));
            Assert.True(SpinWait.SpinUntil(() => { view.Poll(TimeSpan.Zero); return calls > 0; }, TimeSpan.FromSeconds(10)));
            Assert.Equal(3, calls);
            Assert.Equal(2456, grid.Rows[0].Values[1]);
            Assert.Equal(1000, grid.Rows[1].Values[1]);
            Assert.Equal(2556, grid.Rows[2].Values[1]);
            string frame = FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context).ToPlainText();
            Assert.Contains("Refresh: First row unavailable", frame);
            Assert.Contains("Refresh: Second row unavailable", frame);
            Assert.False(view.IsDone);
        }

        [Theory]
        [InlineData(false, "One")]
        [InlineData(true, "2 rows")]
        public void Confirmation_NamesCapturedMarksEvenWhenTheyAreFilteredOut(bool twoMarks, string target) {
            Grid grid = new Grid("Services"); grid.AddColumn("Name");
            grid.AddRow("one", "One"); grid.AddRow("two", "Two"); grid.AddRow("three", "Three");
            IReadOnlyList<object?>? captured = null;
            AppCommand command = new AppCommand("restart", "Restart", (_, __) => Task.CompletedTask) {
                Shortcut = new KeyChord(ConsoleKey.F5), ConfirmPrompt = "Restart {Selection}?"
            };
            command.CaptureRows = rows => { captured = rows; return rows; };
            GridOptions options = new GridOptions(); options.Commands.Add(command);
            using GridView view = new GridView(grid, options, new PromptLabels());
            FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context);
            view.Handle(InputEvent.Press(ConsoleKey.Spacebar));
            if (twoMarks) { view.Handle(InputEvent.Press(ConsoleKey.DownArrow)); view.Handle(InputEvent.Press(ConsoleKey.Spacebar)); }
            foreach (char key in "Three") { view.Handle(InputEvent.Character(key)); }
            view.Handle(InputEvent.Press(ConsoleKey.F5));
            Assert.Equal(twoMarks ? new object?[] { "one", "two" } : new object?[] { "one" }, captured);
            string frame = FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context).ToPlainText();
            Assert.Contains("Restart " + target + "?", frame);
            Assert.DoesNotContain("Restart Three?", frame);
        }
    }
}
