using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Headless;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using Xunit;
using Grid = ConsoleMonster.Interactive.Grid;

namespace ConsoleMonster.Tests.Interactive.App {
    // Controls that share a SelectionKey follow each other through app state, without referring to each other; a callback
    // writing the key moves them the same way. Driven through a running app on the headless terminal.
    public sealed class LinkedSelectionTests {
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
        private static readonly double[] Cpu = { 10, 20, 30, 40, 50, 60, 70, 80 };

        private static DataGrid Samples() {
            var grid = new Grid("Samples").AddColumn("Minute").AddColumn("Cpu");
            for (int i = 0; i < Cpu.Length; i++) { grid.AddRow(i, i + 1, Cpu[i]); }
            return new DataGrid("samples", grid) { Height = 6, SelectionKey = "minute" };
        }

        [Fact]
        public async Task GridRowsAndChartPointsFollowEachOther() {
            var chart = new LineChart { Unit = "%" };
            foreach (double value in Cpu) { chart.Add("cpu", value); }
            DataGrid table = Samples();
            var view = new ChartView("load", chart) { SelectionKey = "minute" };
            using var terminal = new HeadlessTerminal(80, 26);
            Task<AppState> run = Run(terminal, new Rows(table, view));
            terminal.WaitForText("#1  cpu 10%", Wait);

            terminal.Send("Down Down Down");
            terminal.WaitForText("#4  cpu 40%", Wait);

            terminal.Send("Tab Right");
            Until(() => table.Cursor == 4);
            (int axis, int plotRow) = Axis(terminal);
            terminal.Send(InputEvent.Pointer(axis + 1, plotRow, MouseAction.Down, button: 1));
            terminal.Send(InputEvent.Pointer(axis + 1, plotRow, MouseAction.Up, button: 1));
            Until(() => table.Cursor == 0);
            terminal.Send("Escape");
            Assert.Equal(0, (await run)["minute"]);
        }

        [Fact]
        public async Task ASelectAndAGridFollowEachOtherAndACallbackWritingTheKey() {
            DataGrid table = Samples();
            var pick = new Select("pick", Enumerable.Range(1, Cpu.Length).Select(i => new Choice(i, "minute " + i))) { SelectionKey = "minute", PageSize = 4 };
            var state = new AppState();
            var options = new ConsoleAppOptions {
                Interactive = new InteractiveOptions { Labels = new PromptLabels() },
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            };
            options.KeyBindings.Add(new KeyBinding("last", () => state["minute"] = Cpu.Length - 1, new KeyChord(ConsoleKey.F5)));
            using var terminal = new HeadlessTerminal(80, 26);
            options.Interactive.Target = terminal;
            Task<AppState> run = ConsoleApp.RunAsync(new Rows(pick, table), state, options);
            terminal.WaitForText("minute 1", Wait);

            terminal.Send("Down Down");
            Until(() => table.Cursor == 2);
            terminal.Send("Tab Down");
            Until(() => Highlighted(pick) == 3);
            terminal.Send("F5");
            Until(() => table.Cursor == Cpu.Length - 1 && Highlighted(pick) == Cpu.Length - 1);
            terminal.Send("Escape");
            await run;
        }

        // A filtered selector keeps its filter while it publishes: it does not follow its own write.
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AFilteredLinkedSelectorKeepsItsFilterWhileItPublishes(bool multiple) {
            Choice[] fruit = { new Choice(0, "apple"), new Choice(1, "beta"), new Choice(2, "berry") };
            SelectionInputBase pick = multiple ? new MultiSelect("pick", fruit) : (SelectionInputBase)new Select("pick", fruit);
            pick.SelectionKey = "position";
            var state = new AppState();
            using var terminal = new HeadlessTerminal(60, 16);
            Task<AppState> run = ConsoleApp.RunAsync(pick, state, Options(terminal));
            terminal.WaitForText("apple", Wait);

            terminal.Send("\"b\"");
            Until(() => !terminal.Snapshot().ToPlainText().Contains("apple"));
            Until(() => Equals(state.Get<object?>("position"), 1));
            terminal.Send("Down");
            Until(() => Equals(state.Get<object?>("position"), 2));
            Assert.DoesNotContain("apple", terminal.Snapshot().ToPlainText());
            terminal.Send("Escape Escape");
            await run;
        }

        [Fact]
        public async Task ClickingADropdownChoicePublishesItAndTheLinkedGridFollows() {
            var pick = new Select("pick", Enumerable.Range(1, Cpu.Length).Select(i => new Choice(i, "minute " + i))) { SelectionKey = "minute", Dropdown = true };
            DataGrid table = Samples();
            var state = new AppState();
            using var terminal = new HeadlessTerminal(80, 30);
            Task<AppState> run = ConsoleApp.RunAsync(new Rows(pick, table), state, Options(terminal));
            terminal.WaitForText("Samples", Wait);
            Click(terminal, "▾");
            terminal.WaitForText("minute 3", Wait);
            Click(terminal, "minute 3");
            Until(() => Equals(state.Get<object?>("minute"), 2) && table.Cursor == 2);
            terminal.Send("Escape");
            await run;
        }

        // Opening a dropdown and moving over its list change nothing, so a value a callback wrote stays in state.
        [Fact]
        public async Task InputThatLeavesASelectionWhereItWasDoesNotOverwriteTheSharedValue() {
            var pick = new Select("pick", Enumerable.Range(1, 3).Select(i => new Choice(i, "minute " + i))) { SelectionKey = "minute", Dropdown = true };
            var state = new AppState();
            ConsoleAppOptions options = null!;
            DataGrid below = Samples();
            below.SelectionKey = null;
            using var terminal = new HeadlessTerminal(80, 30);
            options = Options(terminal);
            options.KeyBindings.Add(new KeyBinding("far", () => state["minute"] = 99, new KeyChord(ConsoleKey.F5)));
            // Input is handled in order, so the barrier is set only after the pointer move before it.
            options.KeyBindings.Add(new KeyBinding("barrier", () => state["barrier"] = true, new KeyChord(ConsoleKey.F6)));
            Task<AppState> run = ConsoleApp.RunAsync(new Rows(pick, below), state, options);
            terminal.WaitForText("pick", Wait);
            terminal.Send("F5");
            Until(() => Equals(state.Get<object?>("minute"), 99));
            Click(terminal, "▾");
            terminal.WaitForText("minute 3", Wait);
            string[] rows = terminal.Snapshot().ToPlainText().Split('\n');
            int y = Array.FindLastIndex(rows, row => row.Contains("minute 1"));
            terminal.Send(InputEvent.Pointer(rows[y].IndexOf("minute 1", StringComparison.Ordinal) + 1, y, MouseAction.Move));
            terminal.Send("F6");
            Until(() => Equals(state.Get<object?>("barrier"), true));
            Assert.Equal(99, state.Get<object?>("minute"));
            terminal.Send("Escape Escape");
            await run;
        }

        // A handler that moves the shared key while the grid publishes moves the grid too.
        [Fact]
        public async Task APublisherFollowsANestedWriteToAnotherPosition() {
            DataGrid table = Samples();
            var state = new AppState();
            state.Changed += (_, change) => { if (change.Key == "minute" && Equals(change.Value, 2)) { state["minute"] = 5; } };
            using var terminal = new HeadlessTerminal(80, 20);
            Task<AppState> run = ConsoleApp.RunAsync(table, state, Options(terminal));
            terminal.WaitForText("Samples", Wait);
            terminal.Send("Down Down");
            Until(() => Equals(state.Get<object?>("minute"), 5) && table.Cursor == 5);
            terminal.Send("Escape");
            await run;
        }
        // A control's own Changed handler can move the shared key past what the control shows; the input's selection is
        // not published over that newer value.
        [Fact]
        public async Task AValueWrittenByTheControlsOwnChangedHandlerIsNotOverwritten() {
            var chart = new LineChart();
            foreach (double value in new double[] { 10, 20, 30 }) { chart.Add("cpu", value); }
            var view = new ChartView("load", chart) { SelectionKey = "minute" };
            var state = new AppState();
            view.Changed += (_, change) => { if (Equals(change.Value, 1)) { state["minute"] = 7; } };
            DataGrid table = Samples();
            using var terminal = new HeadlessTerminal(80, 30);
            Task<AppState> run = ConsoleApp.RunAsync(new Rows(view, table), state, Options(terminal));
            terminal.WaitForText("#3  cpu 30", Wait);
            Until(() => table.Cursor == 2);
            terminal.Send("Left");
            Until(() => view.Cursor == 1);
            Until(() => Equals(state.Get<object?>("minute"), 7) && table.Cursor == 7);
            terminal.Send("Escape");
            await run;
        }

        private static ConsoleAppOptions Options(HeadlessTerminal terminal) => new ConsoleAppOptions {
            Interactive = new InteractiveOptions { Target = terminal, Labels = new PromptLabels() },
            Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
        };

        private static void Click(HeadlessTerminal terminal, string text) {
            string[] rows = terminal.Snapshot().ToPlainText().Split('\n');
            int y = Array.FindLastIndex(rows, row => row.Contains(text));
            int x = rows[y].IndexOf(text, StringComparison.Ordinal) + 1;
            terminal.Send(InputEvent.Pointer(x, y, MouseAction.Down, button: 1));
            terminal.Send(InputEvent.Pointer(x, y, MouseAction.Up, button: 1));
        }
        private static int? Highlighted(Select select) => ((ILinkedSelection)select).SelectedIndex;

        private static Task<AppState> Run(HeadlessTerminal terminal, IWidget layout) =>
            ConsoleApp.RunAsync(layout, options: new ConsoleAppOptions {
                Interactive = new InteractiveOptions { Target = terminal, Labels = new PromptLabels() },
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            });

        // The y axis column and the plot's bottom row, where the minimum label " 0%" meets the axis.
        private static (int Column, int Row) Axis(HeadlessTerminal terminal) {
            string[] rows = terminal.Snapshot().ToPlainText().Split('\n');
            for (int y = rows.Length - 1; y >= 0; y--) {
                Match match = Regex.Match(rows[y], @" 0%[│|]");
                if (match.Success) { return (match.Index + 3, y); }
            }
            throw new InvalidOperationException("No plot axis on screen.");
        }

        private static void Until(Func<bool> condition) {
            DateTime end = DateTime.UtcNow + Wait;
            while (!condition()) {
                if (DateTime.UtcNow > end) { throw new TimeoutException("The app did not reach the expected state."); }
                Thread.Sleep(10);
            }
        }
    }
}
