using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ConsoleMonster.Graphics;
using ConsoleMonster.Headless;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using Xunit;

namespace ConsoleMonster.Extensions.Tests.Graphics {
    // A ChartForgeX chart explored inside a running app: zoom and pan come from the X zoom bank ChartForgeX prepared once,
    // so its formatters do not run again while navigating; hiding a series prepares that view.
    public sealed class ChartForgeXViewTests {
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(20);

        [Fact]
        public async Task ZoomAndPanSelectPreparedLevelsWithoutRunningTheChartAgain() {
            int formatted = 0;
            Chart chart = Chart.Create().WithXAxis("Minute").WithYAxis("Requests")
                .WithXAxisValueFormatter(value => { Interlocked.Increment(ref formatted); return value.ToString("0", System.Globalization.CultureInfo.InvariantCulture); });
            chart.AddSeries(new ChartSeries(new ChartSeriesId("api"), "api", ChartSeriesKind.Line, Enumerable.Range(0, 40).Select(i => new ChartPoint(i, 50 + (30 * Math.Sin(i / 4.0))))));
            chart.AddSeries(new ChartSeries(new ChartSeriesId("web"), "web", ChartSeriesKind.Line, Enumerable.Range(0, 40).Select(i => new ChartPoint(i, 40 + i))));
            var view = new ChartView("requests", chart.ToChartPlot());
            using var terminal = new HeadlessTerminal(90, 22);
            Task<AppState> run = ConsoleApp.RunAsync(view, options: new ConsoleAppOptions {
                Interactive = new InteractiveOptions { Target = terminal, Labels = new PromptLabels() },
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            });
            terminal.WaitForText("39: api 40.41, web 79", Wait);
            int prepared = Volatile.Read(ref formatted);
            Assert.True(prepared > 0);

            terminal.Send("\"+\" \"+\" Shift+Left Left");
            terminal.WaitForText(", ×4", Wait);
            Until(() => view.Pan < 1);
            double panned = view.Pan;
            terminal.Send(InputEvent.Pointer(30, 10, MouseAction.Down, button: 1));
            terminal.Send(InputEvent.Pointer(60, 10, MouseAction.Move, heldButtons: 1));
            terminal.Send(InputEvent.Pointer(60, 10, MouseAction.Up, button: 1));
            Until(() => view.Pan < panned);
            terminal.Send(InputEvent.Pointer(45, 10, MouseAction.Wheel, wheelDelta: 1));
            terminal.WaitForText(", ×8", Wait);
            Assert.Equal(prepared, Volatile.Read(ref formatted));

            terminal.Send("\"2\"");
            Until(() => view.IsHidden(1));
            Until(() => Volatile.Read(ref formatted) > prepared);
            terminal.Send("Escape");
            await run;
        }

        // ChartForgeX prepares hidden series only when every series is a Line with its own identity; other charts keep
        // every series shown, and the app keeps drawing after the key.
        [Theory]
        [InlineData("mixed identities")]
        [InlineData("step lines")]
        [InlineData("point legend")]
        public async Task LegendKeysLeaveSeriesShownWhereTheChartCannotHideThem(string shape) {
            Chart chart = Chart.Create();
            ChartPoint[] Points(double offset) => Enumerable.Range(0, 10).Select(i => new ChartPoint(i, offset + i)).ToArray();
            if (shape == "mixed identities") {
                chart.AddSeries(new ChartSeries(new ChartSeriesId("api"), "api", ChartSeriesKind.Line, Points(10)));
                chart.AddLine("web", Points(20));
            } else if (shape == "point legend") {
                chart.WithPointLegend();
                chart.AddSeries(new ChartSeries(new ChartSeriesId("api"), "api", ChartSeriesKind.Line, Points(10)));
                chart.AddSeries(new ChartSeries(new ChartSeriesId("web"), "web", ChartSeriesKind.Line, Points(20)));
            } else {
                chart.AddSeries(new ChartSeries(new ChartSeriesId("api"), "api", ChartSeriesKind.StepLine, Points(10)));
                chart.AddSeries(new ChartSeries(new ChartSeriesId("web"), "web", ChartSeriesKind.StepLine, Points(20)));
            }
            var view = new ChartView("requests", chart.ToChartPlot());
            using var terminal = new HeadlessTerminal(90, 22);
            Task<AppState> run = Run(terminal, view);
            terminal.WaitForText("9: api 19, web 29", Wait);
            terminal.Send("\"1\" Left");
            terminal.WaitForText("8: api 18, web 28", Wait);
            Assert.False(view.IsHidden(0));
            view.SetHidden(0, true);
            Assert.False(view.IsHidden(0));
            terminal.Send("\"+\" Left");
            terminal.WaitForText("7: api 17, web 27", Wait);
            Assert.DoesNotContain("render error", terminal.Snapshot().ToPlainText());
            terminal.Send("Escape");
            await run;
        }

        [Fact]
        public async Task TheCrosshairReadsEverySeriesAtTheSelectedX() {
            Chart chart = Chart.Create();
            chart.AddSeries(new ChartSeries(new ChartSeriesId("a"), "a", ChartSeriesKind.Line, new[] { new ChartPoint(0, 10), new ChartPoint(1, 20) }));
            chart.AddSeries(new ChartSeries(new ChartSeriesId("b"), "b", ChartSeriesKind.Line, new[] { new ChartPoint(1, 100), new ChartPoint(2, 200) }));
            using var terminal = new HeadlessTerminal(90, 22);
            Task<AppState> run = Run(terminal, new ChartView("pair", chart.ToChartPlot()));
            terminal.WaitForText("1: a 20, b 100", Wait);
            terminal.Send("Left");
            terminal.WaitForText("0: a 10, b ", Wait);
            Assert.DoesNotContain("b 200", terminal.Snapshot().ToPlainText());
            terminal.Send("Escape");
            await run;
        }

        // Points of the first series that share an X keep their own values under the crosshair.
        [Fact]
        public async Task PointsThatShareAnXReadTheirOwnValues() {
            Chart chart = Chart.Create();
            chart.AddSeries(new ChartSeries(new ChartSeriesId("a"), "a", ChartSeriesKind.Line, new[] { new ChartPoint(0, 1), new ChartPoint(1, 10), new ChartPoint(1, 20), new ChartPoint(2, 3) }));
            using var terminal = new HeadlessTerminal(90, 22);
            Task<AppState> run = Run(terminal, new ChartView("steps", chart.ToChartPlot()));
            terminal.WaitForText("2: a 3", Wait);
            terminal.Send("Left");
            terminal.WaitForText("1: a 20", Wait);
            terminal.Send("Left");
            terminal.WaitForText("1: a 10", Wait);
            terminal.Send("Escape");
            await run;
        }
        private static Task<AppState> Run(HeadlessTerminal terminal, IWidget layout) =>
            ConsoleApp.RunAsync(layout, options: new ConsoleAppOptions {
                Interactive = new InteractiveOptions { Target = terminal, Labels = new PromptLabels() },
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            });
        private static void Until(Func<bool> condition) {
            DateTime end = DateTime.UtcNow + Wait;
            while (!condition()) {
                if (DateTime.UtcNow > end) { throw new TimeoutException("The app did not reach the expected state."); }
                Thread.Sleep(10);
            }
        }
    }
}
