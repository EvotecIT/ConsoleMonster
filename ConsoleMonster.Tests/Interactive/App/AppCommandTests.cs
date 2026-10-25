using System;
using System.Collections.Generic;
using System.Linq;
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
            }) { ConfirmPrompt = "Restart the selected service?", Shortcut = new KeyChord(ConsoleKey.F5) };
            using AppCommandHost host = new AppCommandHost(new[] { restart }, () => selected, new PromptLabels(), default);
            host.Invoke(restart);
            selected[0] = "WinRM";
            Assert.True(host.HasModal);
            Assert.False(host.IsBusy);
            FrameComposer.Compose(host.Wrap(new TextBlock("Services remain open")), 100, 30, Context);
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
            using ManualResetEventSlim finished = new ManualResetEventSlim();
            AppCommand command = new AppCommand("restart", "Restart", (_, __) => { finished.Set(); return Task.CompletedTask; }) { ConfirmPrompt = "Restart?" };
            using AppCommandHost host = new AppCommandHost(new[] { command }, () => Array.Empty<object?>(), new PromptLabels(), default);
            FrameComposer.Compose(host.Wrap(new TextBlock("body")), 80, 24, Context);
            host.Handle(InputEvent.Press(ConsoleKey.K, KeyModifiers.Control));
            host.Handle(InputEvent.Press(ConsoleKey.Enter));
            Assert.True(host.HasModal);
            host.Handle(InputEvent.Press(ConsoleKey.LeftArrow));
            host.Handle(InputEvent.Press(ConsoleKey.Enter));
            Assert.True(finished.Wait(TimeSpan.FromSeconds(5)));
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
            string frame = FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context).ToPlainText();
            Assert.Contains("Selected row", frame); Assert.Contains("Action output", frame); Assert.Contains("Refreshed", frame);
            Golden.Match("interactive/grid.actions.txt", frame.TrimEnd() + "\n");
        }
    }
}
