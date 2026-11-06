using System;
using System.Diagnostics;
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
using Xunit.Abstractions;

namespace ConsoleMonster.Extensions.Tests.Graphics {
    // A ChartForgeX chart explored inside a running app: zoom and pan come from the X zoom bank ChartForgeX prepared once,
    // so its formatters do not run again while navigating; hiding a series prepares that view.
    public sealed class ChartForgeXViewTests {
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(20);

        private readonly ITestOutputHelper _output;

        public ChartForgeXViewTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task ZoomAndPanSelectPreparedLevelsWithoutRunningTheChartAgain() {
            int formatted = 0;
            Chart chart = Chart.Create().WithXAxis("Minute").WithYAxis("Requests")
                .WithXAxisValueFormatter(value => { Interlocked.Increment(ref formatted); return value.ToString("0", System.Globalization.CultureInfo.InvariantCulture); });
            chart.AddSeries(new ChartSeries(new ChartSeriesId("api"), "api", ChartSeriesKind.Line, Enumerable.Range(0, 40).Select(i => new ChartPoint(i, 50 + (30 * Math.Sin(i / 4.0))))));
            chart.AddSeries(new ChartSeries(new ChartSeriesId("web"), "web", ChartSeriesKind.Line, Enumerable.Range(0, 40).Select(i => new ChartPoint(i, 40 + i))));
            var view = new ChartView("requests", chart.ToChartPlot());
            using var terminal = new HeadlessTerminal(90, 22);
            await Run(terminal, view, run => {
                WaitForText(terminal, run, "39: api 40.41, web 79");
                int prepared = Volatile.Read(ref formatted);
                Assert.True(prepared > 0);

                terminal.Send("\"+\" \"+\" Shift+Left Left");
                WaitForText(terminal, run, ", ×4");
                Until(terminal, run, () => view.Pan < 1);
                double panned = view.Pan;
                terminal.Send(InputEvent.Pointer(30, 10, MouseAction.Down, button: 1));
                terminal.Send(InputEvent.Pointer(60, 10, MouseAction.Move, heldButtons: 1));
                terminal.Send(InputEvent.Pointer(60, 10, MouseAction.Up, button: 1));
                Until(terminal, run, () => view.Pan < panned);
                terminal.Send(InputEvent.Pointer(45, 10, MouseAction.Wheel, wheelDelta: 1));
                WaitForText(terminal, run, ", ×8");
                Assert.Equal(prepared, Volatile.Read(ref formatted));

                terminal.Send("\"2\"");
                Until(terminal, run, () => view.IsHidden(1));
                Until(terminal, run, () => Volatile.Read(ref formatted) > prepared);
                terminal.Send("Escape");
            });
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
            await Run(terminal, view, run => {
                WaitForText(terminal, run, "9: api 19, web 29");
                terminal.Send("\"1\" Left");
                WaitForText(terminal, run, "8: api 18, web 28");
                Assert.False(view.IsHidden(0));
                view.SetHidden(0, true);
                Assert.False(view.IsHidden(0));
                terminal.Send("\"+\" Left");
                WaitForText(terminal, run, "7: api 17, web 27");
                Assert.DoesNotContain("render error", terminal.Snapshot().ToPlainText());
                terminal.Send("Escape");
            });
        }

        [Fact]
        public async Task TheCrosshairReadsEverySeriesAtTheSelectedX() {
            Chart chart = Chart.Create();
            chart.AddSeries(new ChartSeries(new ChartSeriesId("a"), "a", ChartSeriesKind.Line, new[] { new ChartPoint(0, 10), new ChartPoint(1, 20) }));
            chart.AddSeries(new ChartSeries(new ChartSeriesId("b"), "b", ChartSeriesKind.Line, new[] { new ChartPoint(1, 100), new ChartPoint(2, 200) }));
            using var terminal = new HeadlessTerminal(90, 22);
            await Run(terminal, new ChartView("pair", chart.ToChartPlot()), run => {
                WaitForText(terminal, run, "1: a 20, b 100");
                terminal.Send("Left");
                WaitForText(terminal, run, "0: a 10, b ");
                Assert.DoesNotContain("b 200", terminal.Snapshot().ToPlainText());
                terminal.Send("Escape");
            });
        }

        // Points of the first series that share an X keep their own values under the crosshair.
        [Fact]
        public async Task PointsThatShareAnXReadTheirOwnValues() {
            Chart chart = Chart.Create();
            chart.AddSeries(new ChartSeries(new ChartSeriesId("a"), "a", ChartSeriesKind.Line, new[] { new ChartPoint(0, 1), new ChartPoint(1, 10), new ChartPoint(1, 20), new ChartPoint(2, 3) }));
            using var terminal = new HeadlessTerminal(90, 22);
            await Run(terminal, new ChartView("steps", chart.ToChartPlot()), run => {
                WaitForText(terminal, run, "2: a 3");
                terminal.Send("Left");
                WaitForText(terminal, run, "1: a 20");
                terminal.Send("Left");
                WaitForText(terminal, run, "1: a 10");
                terminal.Send("Escape");
            });
        }
        private async Task Run(HeadlessTerminal terminal, IWidget layout, Action<Task<AppState>> exercise) {
            using var cancellation = new CancellationTokenSource();
            Task<AppState> run = ConsoleApp.RunAsync(layout, options: new ConsoleAppOptions {
                Interactive = new InteractiveOptions { Target = terminal, Labels = new PromptLabels() },
                Live = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            }, cancellationToken: cancellation.Token);
            Exception? failure = null;
            try {
                exercise(run);
                await ObserveRun(terminal, run);
            } catch (Exception error) {
                failure = error;
                throw;
            } finally {
                cancellation.Cancel();
                terminal.CompleteInput();
                try {
                    await ObserveRun(terminal, run);
                } catch (OperationCanceledException error) when (error.CancellationToken == cancellation.Token && cancellation.IsCancellationRequested) {
                    // Cancellation is expected only while ending this test's app.
                } catch (Exception error) when (failure != null) {
                    // Keep the assertion/readiness failure primary, with the app's cleanup failure visible in the report.
                    failure.Data["App cleanup failure"] = error;
                    _output.WriteLine("App cleanup failure: " + error);
                }
            }
        }

        private static async Task ObserveRun(HeadlessTerminal terminal, Task<AppState> run) {
            if (await Task.WhenAny(run, Task.Delay(Wait)) != run) {
                // A stuck worker cannot be forcibly stopped. Observe any eventual fault after the bounded wait.
                _ = run.ContinueWith(completed => { _ = completed.Exception; }, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                throw new TimeoutException(Diagnostics(terminal, run, "The app did not stop."));
            }
            await run;
        }

        private static void WaitForText(HeadlessTerminal terminal, Task<AppState> run, string text) {
            Stopwatch waited = Stopwatch.StartNew();
            while (true) {
                TimeSpan left = Wait - waited.Elapsed;
                if (left <= TimeSpan.Zero) {
                    throw new TimeoutException(Diagnostics(terminal, run, "The screen did not show '" + text + "'."), run.Exception);
                }
                try {
                    terminal.WaitForText(text, left < TimeSpan.FromMilliseconds(50) ? left : TimeSpan.FromMilliseconds(50));
                    return;
                } catch (TimeoutException) {
                    if (run.IsCompleted) {
                        throw new InvalidOperationException(Diagnostics(terminal, run, "The app ended before showing '" + text + "'."), run.Exception);
                    }
                }
            }
        }

        private static void Until(HeadlessTerminal terminal, Task<AppState> run, Func<bool> condition) {
            Stopwatch waited = Stopwatch.StartNew();
            while (!condition()) {
                if (run.IsCompleted) {
                    throw new InvalidOperationException(Diagnostics(terminal, run, "The app ended before reaching the expected state."), run.Exception);
                }
                if (waited.Elapsed >= Wait) {
                    throw new TimeoutException(Diagnostics(terminal, run, "The app did not reach the expected state."), run.Exception);
                }
                Thread.Sleep(10);
            }
        }

        private static string Diagnostics(HeadlessTerminal terminal, Task<AppState> run, string message) =>
            message + " App task: " + run.Status + "; fault: " + (run.Exception?.ToString() ?? "none")
                + "; frames: " + terminal.FrameCount + Environment.NewLine + terminal.Snapshot().ToPlainText();
    }
}
