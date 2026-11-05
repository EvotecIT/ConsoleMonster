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

        private static void Until(Func<bool> condition) {
            DateTime end = DateTime.UtcNow + Wait;
            while (!condition()) {
                if (DateTime.UtcNow > end) { throw new TimeoutException("The app did not reach the expected state."); }
                Thread.Sleep(10);
            }
        }
    }
}
