using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Headless;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    // A chart explored inside a running app, driven through the headless terminal: each behaviour with its keys and its
    // pointer gesture.
    public sealed class ChartViewTests {
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
        private static readonly double[] Cpu = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 85, 75, 65, 55, 45, 35, 25 };

        private static LineChart Load() {
            var chart = new LineChart { Unit = "%" };
            foreach (double v in Cpu) { chart.Add("cpu", v); }
            for (int i = 0; i < Cpu.Length; i++) { chart.Add("mem", 50 + (2 * i)); }
            return chart;
        }

        [Theory]
        [InlineData(GlyphTier.Full, ColorDepth.TrueColor)]
        [InlineData(GlyphTier.Basic, ColorDepth.Ansi16)]
        [InlineData(GlyphTier.Ascii, ColorDepth.None)]
        public async Task Crosshair_ArrowsAndAClickReadEverySeriesAboveThePlotAndInTheFooter(GlyphTier glyphs, ColorDepth colors) {
            using var terminal = new HeadlessTerminal(80, 20, colors, glyphs);
            Task<AppState> run = Run(terminal, new ChartView("load", Load()) { Label = "Load" });
            terminal.WaitForText("#16: cpu 25%, mem 80%", Wait);
            terminal.Send("Left Left");
            terminal.WaitForText("#14: cpu 45%, mem 76%", Wait);
            Assert.Contains("#14  1 cpu 45%  2 mem 76%", terminal.Snapshot().ToPlainText());

            // The first plot column is the first point.
            (int axis, int plotRow) = Axis(terminal);
            Click(terminal, axis + 1, plotRow);
            terminal.WaitForText("#1: cpu 10%, mem 50%", Wait);
            Assert.Equal(0, (await Quit(terminal, run))["load"]);
        }

        [Fact]
        public async Task Legend_ANumberOrAClickHidesAndShowsASeries() {
            using var terminal = new HeadlessTerminal(80, 20);
            var view = new ChartView("load", Load());
            Task<AppState> run = Run(terminal, view);
            terminal.WaitForText("#16: cpu 25%, mem 80%", Wait);
            terminal.Send("\"1\"");
            terminal.WaitForText("#16: mem 80%", Wait);
            Assert.True(view.IsHidden(0));

            string[] rows = Rows(terminal);
            int legend = Array.FindIndex(rows, row => row.Contains("1 cpu   2 mem"));
            Click(terminal, rows[legend].IndexOf("1 cpu", StringComparison.Ordinal) + 2, legend);
            terminal.WaitForText("#16: cpu 25%, mem 80%", Wait);
            Click(terminal, rows[legend].IndexOf("2 mem", StringComparison.Ordinal) + 2, legend);
            Until(() => view.IsHidden(1));
            Until(() => Footer(terminal).EndsWith("#16: cpu 25%", StringComparison.Ordinal));
            await Quit(terminal, run);
        }

        [Fact]
        public async Task ZoomAndPan_KeysTheWheelAndADragMoveTheWindowAndHomeShowsEverything() {
            using var terminal = new HeadlessTerminal(80, 20);
            var view = new ChartView("load", Load());
            Task<AppState> run = Run(terminal, view);
            terminal.WaitForText("#16: cpu 25%, mem 80%", Wait);
            terminal.Send("\"+\"");
            terminal.WaitForText("mem 80%, ×2", Wait);
            Assert.Equal(1, view.ZoomLevel);
            Assert.Equal(1, view.Pan);

            (_, int plotRow) = Axis(terminal);
            terminal.Send(InputEvent.Pointer(40, plotRow, MouseAction.Wheel, wheelDelta: 1));
            terminal.WaitForText(", ×4", Wait);
            Assert.Equal(2, view.ZoomLevel);

            terminal.Send("Shift+Left");
            Until(() => view.Pan < 1);
            double panned = view.Pan;
            // Dragging the plot to the right brings earlier points into view.
            terminal.Send(InputEvent.Pointer(20, plotRow, MouseAction.Down, button: 1));
            terminal.Send(InputEvent.Pointer(60, plotRow, MouseAction.Move, heldButtons: 1));
            terminal.Send(InputEvent.Pointer(60, plotRow, MouseAction.Up, button: 1));
            Until(() => view.Pan < panned);

            terminal.Send("Home");
            Until(() => view.ZoomLevel == 0);
            Until(() => Footer(terminal).Contains(": cpu") && !Footer(terminal).Contains("×"));
            await Quit(terminal, run);
        }

        [Fact]
        public async Task FixedAxisBound_StaysWhenEveryShownValueIsOutsideIt() {
            var chart = new LineChart { Unit = "%", Minimum = 50 };
            foreach (double v in Cpu) { chart.Add("cpu", Math.Min(v, 45)); }
            using var terminal = new HeadlessTerminal(80, 20);
            Task<AppState> run = Run(terminal, new ChartView("load", chart));
            terminal.WaitForText("#16: cpu 25%", Wait);
            Assert.Contains("50%", terminal.Snapshot().ToPlainText());
            await Quit(terminal, run);
        }
        private static Task<AppState> Run(HeadlessTerminal terminal, IWidget layout) =>
            ConsoleApp.RunAsync(layout, options: new ConsoleAppOptions {
                Interactive = new InteractiveOptions { Target = terminal, Labels = new PromptLabels() },
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            });

        private static async Task<AppState> Quit(HeadlessTerminal terminal, Task<AppState> run) {
            terminal.Send("Escape");
            return await run;
        }

        private static string[] Rows(HeadlessTerminal terminal) => terminal.Snapshot().ToPlainText().Split('\n');

        private static string Footer(HeadlessTerminal terminal) => Rows(terminal).Last().TrimEnd();

        // The y axis column and the plot's bottom row, where the minimum label " 0%" meets the axis.
        private static (int Column, int Row) Axis(HeadlessTerminal terminal) {
            string[] rows = Rows(terminal);
            for (int y = rows.Length - 1; y >= 0; y--) {
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(rows[y], @" 0%[│|]");
                if (match.Success) { return (match.Index + 3, y); }
            }
            throw new InvalidOperationException("No plot axis on screen.");
        }

        private static void Click(HeadlessTerminal terminal, int column, int row) {
            terminal.Send(InputEvent.Pointer(column, row, MouseAction.Down, button: 1));
            terminal.Send(InputEvent.Pointer(column, row, MouseAction.Up, button: 1));
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
