using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>Running many items with a view: input order, failures and timeouts, cancellation and printed lines.</summary>
    public sealed class ConsoleRunTests {
        [Fact]
        public async Task ForEachAsync_ReturnsResultsInInputOrder_AndPrintsALinePerItem() {
            List<string> lines = new List<string>();
            IReadOnlyList<RunResult<int, int>> results = await ConsoleRun.ForEachAsync(Enumerable.Range(1, 20), async (n, token) => {
                await Task.Delay((20 - n) * 3, token);
                return n * 10;
            }, new RunOptions { ThrottleLimit = 5, Interactive = false, Print = line => { lock (lines) { lines.Add(line); } } });
            Assert.Equal(Enumerable.Range(1, 20).Select(n => n * 10), results.Select(r => r.Value));
            Assert.All(results, r => Assert.True(r.Succeeded));
            Assert.Equal(21, lines.Count);
            Assert.StartsWith("20 items: 20 passed, 0 failed", lines[20]);
        }

        [Fact]
        public async Task ForEachAsync_ALabelThatThrows_FallsBackToTheItemTitle() {
            List<string> lines = new List<string>();
            IReadOnlyList<RunResult<int, int>> results = await ConsoleRun.ForEachAsync(new[] { 7, 8 }, (n, token) => Task.FromResult(n), new RunOptions {
                Interactive = false,
                Label = item => throw new InvalidOperationException("bad label"),
                Print = line => { lock (lines) { lines.Add(line); } }
            });
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.True(r.Succeeded));
            Assert.Contains(lines, line => line.Contains("7 ("));
        }

        [Fact]
        public async Task ForEachAsync_NoItems_PrintsNothing() {
            List<string> lines = new List<string>();
            IReadOnlyList<RunResult<int, int>> results = await ConsoleRun.ForEachAsync(Array.Empty<int>(), (n, token) => Task.FromResult(n), new RunOptions { Interactive = false, Title = "empty", Print = lines.Add });
            Assert.Empty(results);
            Assert.Empty(lines);
        }

        [Fact]
        public async Task ForEachAsync_AFailureOrTimeout_FailsThatItemOnly() {
            List<string> lines = new List<string>();
            IReadOnlyList<RunResult<int, int>> results = await ConsoleRun.ForEachAsync(new[] { 1, 2, 3 }, async (n, token) => {
                if (n == 2) {
                    throw new InvalidOperationException("two broke");
                }
                if (n == 3) {
                    await Task.Delay(5000, token);
                }
                return n;
            }, new RunOptions { Timeout = TimeSpan.FromMilliseconds(200), Interactive = false, Print = line => { lock (lines) { lines.Add(line); } } });
            Assert.True(results[0].Succeeded);
            Assert.Equal("two broke", results[1].Error!.Message);
            Assert.True(results[2].TimedOut);
            Assert.Contains(lines, line => line.Contains("two broke"));
            Assert.Contains(lines, line => line.Contains("timed out"));
        }

        [Fact]
        public async Task ForEachAsync_Cancelled_StopsStartingItems() {
            using (CancellationTokenSource stop = new CancellationTokenSource()) {
                int started = 0;
                IReadOnlyList<RunResult<int, int>> results = await ConsoleRun.ForEachAsync(Enumerable.Range(0, 50), async (n, token) => {
                    if (Interlocked.Increment(ref started) == 5) {
                        stop.Cancel();
                    }
                    await Task.Delay(10, token);
                    return n;
                }, new RunOptions { ThrottleLimit = 2, Interactive = false, Print = line => { } }, stop.Token);
                Assert.True(results.Count < 50);
            }
        }

        [Fact]
        public void RunView_ShowsSummaryCellsRunningItemsAndFailures() {
            RunTracker tracker = new RunTracker(6, () => TimeSpan.FromSeconds(10));
            tracker.Finished(0, StatusKind.Pass, "a");
            tracker.Finished(1, StatusKind.Fail, "b", "disk full");
            tracker.Started(2, "c");
            RenderContext context = new RenderContext(TerminalProfile.Fixed(80, 20, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            RunView view = new RunView(tracker);
            Size size = view.Measure(new Constraints(80, 20), context);
            CellBuffer buffer = new CellBuffer(80, size.Height);
            view.Render(new Region(buffer), context);
            string text = buffer.ToPlainText();
            Assert.Contains("2/6", text);
            Assert.Contains("1 failed", text);
            Assert.Contains("#3", text);
            Assert.Contains("c", text);
            Assert.Contains("b: disk full", text);
        }

        [Fact]
        public void Tracker_Complete_CountsItemsNeverStartedAsSkipped() {
            RunTracker tracker = new RunTracker(3);
            tracker.Finished(0, StatusKind.Pass, "a");
            tracker.Started(1, "b");
            tracker.Complete();
            RunSnapshot snapshot = tracker.Snapshot();
            Assert.Equal(1, snapshot.Passed);
            Assert.Equal(2, snapshot.Skipped);
            Assert.True(snapshot.IsFinished);
        }
    }
}
