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
